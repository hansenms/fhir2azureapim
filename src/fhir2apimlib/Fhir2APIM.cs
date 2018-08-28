using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace fhir2apimlib
{
    public class Fhir2Apim
    {
        public static bool SimpleFunction()
        {
            return true;
        }

        public static string GetFhirUrl(string version = null)
        {
            if (String.IsNullOrEmpty(version))
            {
                return "https://hl7.org/fhir/";
            }
            else
            {
                return $"https://hl7.org/fhir/{version}/";
            }
        }
        public static async Task<JSchema> GetFhirSchema(string version = null)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri(GetFhirUrl(version));
                    HttpResponseMessage response = await client.GetAsync("fhir.schema.json");
                    response.EnsureSuccessStatusCode();
                    string schema = await response.Content.ReadAsStringAsync();

                    //For some reason the official FHIR schema uses typographical/curly quotation marks in some instances.
                    schema = schema.Replace("“", "\\\"");
                    schema = schema.Replace("”", "\\\"");

                    return JSchema.Parse(schema);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }

            return JSchema.Parse("{}");

        }

        public static async Task<string> GetSwaggerFromMetadata(string fhirServerUrl, 
                                                                string metadataEndpoint, 
                                                                string[] interactionList, 
                                                                string schemaVersion = null)
        {

            Uri fhirServer = new Uri(fhirServerUrl);

            dynamic swagger = new JObject();
            swagger.swagger = "2.0";            
            swagger.host = fhirServer.Host;
            swagger.basePath = fhirServer.AbsolutePath;

            swagger.paths = JObject.Parse("{\"/metadata\": {\"get\": {\"summary\": \"Get conformance statement.\", \"produces\": [\"application/json\",\"application/xml\"], \"responses\": { \"200\": { \"description\": \"Success\"}}}}}");

            swagger.schemes = JArray.Parse($"[\"{fhirServer.Scheme}\"]");

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = fhirServer;
                    HttpResponseMessage response = await client.GetAsync(metadataEndpoint);
                    response.EnsureSuccessStatusCode();
                    dynamic conformance = JObject.Parse(await response.Content.ReadAsStringAsync());

                    dynamic info = JObject.Parse("{}");
                    info.description = conformance.implementation.description;
                    info.title = conformance.publisher;
                    info.version = conformance.fhirVersion;
                    swagger.info = info;

                    //TODO: Add patch operation
                    foreach (JObject r in conformance.rest[0].resource)
                    {
                        string typeName = (string)r["type"];
                        var interaction = ((JArray)r["interaction"]).Select(i => (string)i["code"]).ToArray();

                        string typePath = "/" + typeName;
                        string instancePath =  "/" + typeName + "/{id}";


                        swagger.paths[typePath] = JObject.Parse("{}");
                        swagger.paths[instancePath] = JObject.Parse("{}");

                        if (interaction.Contains("search-type") && (interactionList.Contains("all") || interactionList.Contains("search-type")))
                        {
                            JObject searchObj = new JObject();

                            searchObj["parameters"] = new JArray();
                            foreach (JObject s in (JArray)r["searchParam"]) 
                            {
                                JObject p = new JObject();
                                p["name"] = (string)s["name"];
                                p["type"] = GetTypeFromTypeName((string)s["name"]);
                                p["in"] = "query";
                                p["description"] = (string)s["documentation"];

                                if ((string)s["type"] == "date") 
                                {
                                    p["format"] = "date";
                                }

                                ((JArray)searchObj["parameters"]).Add(p);
                            }

                            ((JArray)searchObj["parameters"]).Add(JObject.Parse("{\"name\": \"_format\", \"in\": \"query\", \"type\": \"string\", \"x-consoleDefault\": \"application/json\"}"));
                            
                            if (String.IsNullOrEmpty(schemaVersion))
                            { 
                                searchObj["responses"] =  JObject.Parse("{\"200\": { \"description\": \"Success\"}}");
                            } 
                            else 
                            {
                                searchObj["responses"] =  JObject.Parse("{\"200\": { \"schema\": {$ref: \"#/definitions/" + typeName + "\"}}}");
                            }

                            swagger.paths[typePath]["get"] = searchObj;
                        }
                        
                        if (interaction.Contains("read") && (interactionList.Contains("all") || interactionList.Contains("read"))) 
                        {
                            JObject readobj = JObject.Parse("{}");
                            readobj["parameters"] = JArray.Parse("[{\"in\": \"path\", \"name\": \"id\", \"required\": true, \"type\": \"string\"}]"); 
                            
                            swagger.paths[instancePath]["get"] = readobj;

                            if (String.IsNullOrEmpty(schemaVersion))
                            {
                                swagger.paths[instancePath]["get"]["responses"] = JObject.Parse("{\"200\": { \"description\": \"Success\"}}"); 
                            }
                            else
                            {
                                swagger.paths[instancePath]["get"]["responses"] = JObject.Parse("{\"200\": { \"schema\": {$ref: \"#/definitions/" + typeName + "\"}}}"); 
                            }
                        }
                        
                        if (interaction.Contains("vread")  && (interactionList.Contains("all") || interactionList.Contains("vread")))
                        {
                            JObject historyObj = JObject.Parse("{}");
                            historyObj["parameters"] = JArray.Parse("[{\"in\": \"path\", \"name\": \"id\", \"required\": true, \"type\": \"string\"}]");
                            ((JArray)historyObj["parameters"]).Add(JObject.Parse("{\"in\": \"path\", \"name\": \"vid\", \"required\": true, \"type\": \"string\"}")); 
                            swagger.paths[instancePath + "/_history/{vid}"] = JObject.Parse("{}");
                            swagger.paths[instancePath + "/_history/{vid}"]["get"] = historyObj;

                            if (String.IsNullOrEmpty(schemaVersion))
                            {
                                swagger.paths[instancePath + "/_history/{vid}"]["get"]["responses"] = JObject.Parse("{\"200\": { \"description\": \"Success\"}}"); 
                            }
                            else
                            {
                                swagger.paths[instancePath + "/_history/{vid}"]["get"]["responses"] = JObject.Parse("{\"200\": { \"schema\": {$ref: \"#/definitions/" + typeName + "\"}}}"); 
                            }
                        }

                        if (interaction.Contains("history-instance")  && (interactionList.Contains("all") || interactionList.Contains("history-instance")))
                        {
                            JObject historyObj = JObject.Parse("{}");
                            historyObj["parameters"] = JArray.Parse("[{\"in\": \"path\", \"name\": \"id\", \"required\": true, \"type\": \"string\"}]");
                            ((JArray)historyObj["parameters"]).Add(JObject.Parse("{\"in\": \"query\", \"name\": \"_count\", \"type\": \"string\"}")); 
                            ((JArray)historyObj["parameters"]).Add(JObject.Parse("{\"in\": \"query\", \"name\": \"_since\", \"type\": \"string\"}")); 
                            swagger.paths[instancePath + "/_history"] = JObject.Parse("{}");
                            swagger.paths[instancePath + "/_history"]["get"] = historyObj;
                            swagger.paths[instancePath + "/_history"]["get"]["responses"] = JObject.Parse("{\"200\": { \"description\": \"Success\"}}"); 
                        }


                        if (interaction.Contains("history-type") && (interactionList.Contains("all") || interactionList.Contains("history-type")))
                        {
                            JObject historyObj = JObject.Parse("{}");
                            historyObj["parameters"] = JArray.Parse("[]");
                            ((JArray)historyObj["parameters"]).Add(JObject.Parse("{\"in\": \"query\", \"name\": \"_count\", \"type\": \"string\"}")); 
                            ((JArray)historyObj["parameters"]).Add(JObject.Parse("{\"in\": \"query\", \"name\": \"_since\", \"type\": \"string\"}")); 
                            swagger.paths[typePath + "/_history"] = JObject.Parse("{}");
                            swagger.paths[typePath + "/_history"]["get"] = historyObj;
                            swagger.paths[typePath + "/_history"]["get"]["responses"] = JObject.Parse("{\"200\": { \"description\": \"Success\"}}"); 
                        }

                        if (interaction.Contains("create")  && (interactionList.Contains("all") || interactionList.Contains("create")))
                        {
                            swagger.paths[typePath]["post"] = JObject.Parse("{\"parameters\": [], \"responses\": {\"200\": {\"description\": \"success\"}}}");
                            JObject param = new JObject();
                            param["name"] = "body";
                            param["in"] = "body";
                            if (String.IsNullOrEmpty(schemaVersion))
                            {
                                param["schema"] = JObject.Parse("{\"type\": \"object\"}");
                            }
                            else
                            {
                                param["schema"] = JObject.Parse("{$ref: \"#/definitions/" + typeName + "\"}");
                            }
                            ((JArray)swagger.paths[typePath]["post"]["parameters"]).Add(param);
                        }

                        if (interaction.Contains("update")  && (interactionList.Contains("all") || interactionList.Contains("update")))
                        {
                            swagger.paths[instancePath]["put"] = JObject.Parse("{\"parameters\": [], \"responses\": {\"200\": {\"description\": \"success\"}}}");
                            JObject param = new JObject();
                            param["name"] = "body";
                            param["in"] = "body";
                            if (String.IsNullOrEmpty(schemaVersion))
                            {
                                param["schema"] = JObject.Parse("{\"type\": \"object\"}");
                            }
                            else
                            {
                                param["schema"] = JObject.Parse("{$ref: \"#/definitions/" + typeName + "\"}");
                            }
                            ((JArray)swagger.paths[instancePath]["put"]["parameters"]).Add(param);
                            ((JArray)swagger.paths[instancePath]["put"]["parameters"]).Add(JObject.Parse("{\"in\": \"path\", \"name\": \"id\", \"required\": true, \"type\": \"string\"}"));
                        }

                        if (interaction.Contains("delete")  && (interactionList.Contains("all") || interactionList.Contains("delete")))
                        {
                            swagger.paths[instancePath]["delete"] = JObject.Parse("{\"parameters\": [], \"responses\": {\"200\": {\"description\": \"success\"}}}");
                            swagger.paths[instancePath]["delete"]["parameters"] = JArray.Parse("[{\"in\": \"path\", \"name\": \"id\", \"required\": true, \"type\": \"string\"}]");
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }

            if (!String.IsNullOrEmpty(schemaVersion))
            {
                dynamic schema = JObject.Parse((await GetFhirSchema(schemaVersion)).ToString());
                swagger.definitions = schema.definitions;
                swagger.definitions.Remove("ResourceList");
            }

            return JsonConvert.SerializeObject(swagger, Formatting.Indented);
        }

        private static string GetTypeFromTypeName(string typeName)
        {
            string[] swaggerTypes = { "integer", "number", "string", "boolean", "array", "object" };

            if (swaggerTypes.Contains(typeName))
            {
                return typeName;
            }

            if (typeName == "quantity")
            {
                return "integer";
            }

            return "string";
        }
    }
}
