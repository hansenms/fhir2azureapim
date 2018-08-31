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
        public static JObject ArmParameter(string typeName, string defaultValue = null)
        {
            JObject parm = JObject.Parse("{ \"type\": \"" + typeName + "\"}");
            if (!String.IsNullOrEmpty(defaultValue))
            {
                parm["defaultValue"] = defaultValue;
            }
            return parm;
        }

        public static JObject ArmApimApi(string serviceUrl)
        {
            Uri fhirServer = new Uri(serviceUrl);
            JObject api = JObject.Parse("{\"apiVersion\": \"2017-03-01\", \"type\": \"Microsoft.ApiManagement/service/apis\"}");
            api["name"] = "[concat(parameters('apimInstanceName'), '/fhirapi')]";
            api["properties"] = JObject.Parse("{\"displayName\": \"[parameters('displayName')]\"}");
            api["properties"]["path"] = "[parameters('apiPath')]";
            api["properties"]["serviceUrl"] = serviceUrl;
            api["properties"]["protocols"] = JArray.Parse($"[\"{fhirServer.Scheme}\"]");
            api["resources"] = JArray.Parse("[]");
            return api;
        }

        public static JObject ArmApiOperation(string urlTemplate, string method, string previousOpName = null)
        {
            JObject op = new JObject();
            op["apiVersion"] = "2017-03-01";
            op["type"] = "operations";
            op["name"] = Guid.NewGuid();
            op["dependsOn"] = JArray.Parse("[\"[resourceId(resourceGroup().name, 'Microsoft.ApiManagement/service/apis', parameters('apimInstanceName'), 'fhirapi')]\"]");

            if (!String.IsNullOrEmpty(previousOpName))
            {
                ((JArray)op["dependsOn"]).Add("[concat('Microsoft.ApiManagement/service/', parameters('apimInstanceName'), '/apis/fhirapi/operations/" + previousOpName + "')]");
            }

            op["properties"] =  new JObject();
            op["properties"]["urlTemplate"] = urlTemplate;
            op["properties"]["templateParameters"] = new JArray();
            op["properties"]["method"] = method;
            op["properties"]["displayName"] = $"{urlTemplate} - {method}";
            op["properties"]["request"] = JObject.Parse("{\"queryParameters\": [], \"representations\": []}");
            op["properties"]["responses"] = JArray.Parse("[{\"statusCode\": 200, \"description\": \"Success\"}]");
            return op;
        }

        public static JObject ArmApiOperationQueryParameter(string name, string typeName, string description = "")
        {
            JObject parm = new JObject();
            parm["name"] = name;
            parm["type"] = typeName;
            parm["description"] = description;
            return parm;
        }

        public static JObject ArmApiOperationRepresentation(string typeName = "Body")
        {
            JObject rep = new JObject();
            rep["contentType"] = "application/json";
            rep["typeName"] = typeName;
            return rep;
        }

        public static JObject ArmApiOperationTemplateParameter(string name, string typeName = "string", bool required = true)
        {
            JObject templateParm = JObject.Parse("{}");
            templateParm["name"] = name;
            templateParm["type"] = typeName;
            templateParm["required"] = required;

            return templateParm;
        }

        public static async Task<string> GetArmApiFromMetadata(string fhirServerUrl,
                                                               string metadataEndpoint,
                                                               string[] interactionList,
                                                               string[] resourceList)
        {
            JObject template = JObject.Parse("{\"$schema\": \"http://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#\", \"contentVersion\": \"1.0.0.0\", \"parameters\": {}, \"resources\": []}");

            template["parameters"]["apimInstanceName"] = ArmParameter("string", "myapim");
            template["parameters"]["displayName"] = ArmParameter("string", "FHIRAPI");
            template["parameters"]["apiPath"] = ArmParameter("string", "fhir");

            var api = ArmApimApi(fhirServerUrl);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri(fhirServerUrl);
                    HttpResponseMessage response = await client.GetAsync(metadataEndpoint);
                    response.EnsureSuccessStatusCode();
                    dynamic conformance = JObject.Parse(await response.Content.ReadAsStringAsync());

                    api["properties"]["description"] = conformance.software.name;

                    string previousOpName = null;

                    //TODO: Add patch operation
                    foreach (JObject r in conformance.rest[0].resource)
                    {

                        string typeName = (string)r["type"];

                        if (!(resourceList.Contains("all") || resourceList.Contains(typeName)))
                        {
                            continue;
                        }

                        var interaction = ((JArray)r["interaction"]).Select(i => (string)i["code"]).ToArray();

                        string typePath = "/" + typeName;
                        string instancePath = "/" + typeName + "/{id}";

                        if (interaction.Contains("search-type") && (interactionList.Contains("all") || interactionList.Contains("search-type")))
                        {
                            var op = ArmApiOperation(typePath, "GET", previousOpName);
                            if (r["searchParam"] != null) 
                            {
                                foreach (JObject s in (JArray)r["searchParam"])
                                {
                                    ((JArray)op["properties"]["request"]["queryParameters"]).Add(ArmApiOperationQueryParameter((string)s["name"], GetTypeFromTypeName((string)s["name"]), (string)s["documentation"]));
                                }
                            }
                            ((JArray)api["resources"]).Add(op);
                            previousOpName = (string)op["name"];
                        }

                        if (interaction.Contains("read") && (interactionList.Contains("all") || interactionList.Contains("read")))
                        {
                            var op = ArmApiOperation(instancePath, "GET", previousOpName);
                            ((JArray)op["properties"]["templateParameters"]).Add(ArmApiOperationTemplateParameter("id"));
                            ((JArray)api["resources"]).Add(op);
                            previousOpName = (string)op["name"];
                        }


                        if (interaction.Contains("vread") && (interactionList.Contains("all") || interactionList.Contains("vread")))
                        {
                            var op = ArmApiOperation(instancePath + "/_history/{vid}", "GET", previousOpName);
                            ((JArray)op["properties"]["templateParameters"]).Add(ArmApiOperationTemplateParameter("id"));
                            ((JArray)op["properties"]["templateParameters"]).Add(ArmApiOperationTemplateParameter("vid"));
                            ((JArray)api["resources"]).Add(op);
                            previousOpName = (string)op["name"];
                        }

                        if (interaction.Contains("history-instance") && (interactionList.Contains("all") || interactionList.Contains("history-instance")))
                        {
                            var op = ArmApiOperation(instancePath + "/_history", "GET", previousOpName);
                            ((JArray)op["properties"]["templateParameters"]).Add(ArmApiOperationTemplateParameter("id"));
                            ((JArray)op["properties"]["request"]["queryParameters"]).Add(ArmApiOperationQueryParameter("_count", "string"));
                            ((JArray)op["properties"]["request"]["queryParameters"]).Add(ArmApiOperationQueryParameter("_since", "string"));
                            ((JArray)api["resources"]).Add(op);
                            previousOpName = (string)op["name"];
                        }


                        if (interaction.Contains("history-type") && (interactionList.Contains("all") || interactionList.Contains("history-type")))
                        {
                            var op = ArmApiOperation(typePath + "/_history", "GET", previousOpName);
                            ((JArray)op["properties"]["request"]["queryParameters"]).Add(ArmApiOperationQueryParameter("_count", "string"));
                            ((JArray)op["properties"]["request"]["queryParameters"]).Add(ArmApiOperationQueryParameter("_since", "string"));
                            ((JArray)api["resources"]).Add(op);
                            previousOpName = (string)op["name"];
                        }

                        if (interaction.Contains("create") && (interactionList.Contains("all") || interactionList.Contains("create")))
                        {
                            var op = ArmApiOperation(typePath, "POST", previousOpName);
                            ((JArray)op["properties"]["request"]["representations"]).Add(ArmApiOperationRepresentation());
                            ((JArray)api["resources"]).Add(op);
                            previousOpName = (string)op["name"];
                        }

                        if (interaction.Contains("update") && (interactionList.Contains("all") || interactionList.Contains("update")))
                        {
                            var op = ArmApiOperation(instancePath, "PUT", previousOpName);
                            ((JArray)op["properties"]["templateParameters"]).Add(ArmApiOperationTemplateParameter("id"));
                            ((JArray)op["properties"]["request"]["representations"]).Add(ArmApiOperationRepresentation());
                            ((JArray)api["resources"]).Add(op);
                            previousOpName = (string)op["name"];
                        }

                        if (interaction.Contains("delete") && (interactionList.Contains("all") || interactionList.Contains("delete")))
                        {
                            var op = ArmApiOperation(instancePath, "DELETE", previousOpName);
                            ((JArray)op["properties"]["templateParameters"]).Add(ArmApiOperationTemplateParameter("id"));
                            ((JArray)api["resources"]).Add(op);
                            previousOpName = (string)op["name"];
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }

            ((JArray)template["resources"]).Add(api);

            return JsonConvert.SerializeObject(template, Formatting.Indented);
        }

        public static JObject SwaggerOperation()
        {
            JObject op = new JObject();
            op["parameters"] = new JArray();
            op["responses"] = JObject.Parse("{\"200\": { \"description\": \"Success\"}}");
            return op;
        }

        public static JObject SwaggerParameter(
            string name,
            string inName,
            string typeName = null,
            string description = null,
            bool required = false,
            string format = null)
        {
            JObject parm = new JObject();
            parm["name"] = name;

            if (!String.IsNullOrEmpty(typeName))
            {
                parm["type"] = typeName;
            }

            parm["in"] = inName;

            if (!String.IsNullOrEmpty(description))
            {
                parm["description"] = description;
            }

            if (required)
            {
                parm["required"] = required;
            }

            if (!String.IsNullOrEmpty(format))
            {
                parm["format"] = format;
            }

            return parm;
        }

        public static JObject SwaggerApi(Uri fhirServer)
        {
            JObject swagger = new JObject();
            swagger["swagger"] = "2.0";
            swagger["host"] = fhirServer.Host;
            swagger["basePath"] = fhirServer.AbsolutePath;
            swagger["paths"] = new JObject();
            swagger["info"] = new JObject();
            swagger["paths"]["/metadata"] = JObject.Parse("{\"get\": {\"summary\": \"Get conformance statement.\", \"produces\": [\"application/json\",\"application/xml\"], \"responses\": { \"200\": { \"description\": \"Success\"}}}}");
            swagger["schemes"] = JArray.Parse($"[\"{fhirServer.Scheme}\"]");
            return swagger;
        }

        public static async Task<string> GetSwaggerFromMetadata(string fhirServerUrl,
                                                                string metadataEndpoint,
                                                                string[] interactionList,
                                                                string[] resourceList)
        {

            Uri fhirServer = new Uri(fhirServerUrl);

            JObject swagger = SwaggerApi(fhirServer);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = fhirServer;
                    HttpResponseMessage response = await client.GetAsync(metadataEndpoint);
                    response.EnsureSuccessStatusCode();
                    dynamic conformance = JObject.Parse(await response.Content.ReadAsStringAsync());

                    swagger["info"]["description"] = conformance.implementation.description;
                    swagger["info"]["title"] = conformance.publisher;
                    swagger["info"]["version"] = conformance.fhirVersion;

                    //TODO: Add patch operation
                    foreach (JObject r in conformance.rest[0].resource)
                    {
                        string typeName = (string)r["type"];

                        if (!(resourceList.Contains("all") || resourceList.Contains(typeName)))
                        {
                            continue;
                        }

                        var interaction = ((JArray)r["interaction"]).Select(i => (string)i["code"]).ToArray();
                        string typePath = "/" + typeName;
                        string instancePath = "/" + typeName + "/{id}";

                        swagger["paths"][typePath] = new JObject();
                        swagger["paths"][instancePath] = new JObject();

                        if (interaction.Contains("search-type") && (interactionList.Contains("all") || interactionList.Contains("search-type")))
                        {
                            JObject searchObj = SwaggerOperation();

                            foreach (JObject s in (JArray)r["searchParam"])
                            {
                                JObject p = SwaggerParameter(
                                    (string)s["name"],
                                    "query",
                                    GetTypeFromTypeName((string)s["name"]),
                                    (string)s["documentation"], false,
                                    (string)s["type"] == "date" ? "date" : null
                                    );

                                ((JArray)searchObj["parameters"]).Add(p);
                            }

                            JObject formatParameter = SwaggerParameter("_format", "query", "string", "Output formatting");
                            formatParameter["x-consoleDefault"] = "application/json";
                            ((JArray)searchObj["parameters"]).Add(formatParameter);

                            swagger["paths"][typePath]["get"] = searchObj;
                        }

                        if (interaction.Contains("read") && (interactionList.Contains("all") || interactionList.Contains("read")))
                        {
                            JObject readobj = SwaggerOperation();
                            ((JArray)readobj["parameters"]).Add(SwaggerParameter("id", "path", "string", "id of resource", true));
                            swagger["paths"][instancePath]["get"] = readobj;
                        }

                        if (interaction.Contains("vread") && (interactionList.Contains("all") || interactionList.Contains("vread")))
                        {
                            JObject historyObj = SwaggerOperation();
                            ((JArray)historyObj["parameters"]).Add(SwaggerParameter("id", "path", "string", "id of resource", true));
                            ((JArray)historyObj["parameters"]).Add(SwaggerParameter("vid", "path", "string", "version id of resource", true));
                            swagger["paths"][instancePath + "/_history/{vid}"] = new JObject();
                            swagger["paths"][instancePath + "/_history/{vid}"]["get"] = historyObj;
                        }

                        if (interaction.Contains("history-instance") && (interactionList.Contains("all") || interactionList.Contains("history-instance")))
                        {
                            JObject historyObj = SwaggerOperation();
                            ((JArray)historyObj["parameters"]).Add(SwaggerParameter("id", "path", "string", "id of resource", true));
                            ((JArray)historyObj["parameters"]).Add(SwaggerParameter("_count", "query", "string", "number to return"));
                            ((JArray)historyObj["parameters"]).Add(SwaggerParameter("_since", "query", "string", "how far back"));
                            swagger["paths"][instancePath + "/_history"] = new JObject();
                            swagger["paths"][instancePath + "/_history"]["get"] = historyObj;
                        }

                        if (interaction.Contains("history-type") && (interactionList.Contains("all") || interactionList.Contains("history-type")))
                        {
                            JObject historyObj = SwaggerOperation();
                            ((JArray)historyObj["parameters"]).Add(SwaggerParameter("_count", "query", "string", "number to return"));
                            ((JArray)historyObj["parameters"]).Add(SwaggerParameter("_since", "query", "string", "how far back"));
                            swagger["paths"][typePath + "/_history"] = new JObject();
                            swagger["paths"][typePath + "/_history"]["get"] = historyObj;
                        }

                        if (interaction.Contains("create") && (interactionList.Contains("all") || interactionList.Contains("create")))
                        {
                            swagger["paths"][typePath]["post"] = SwaggerOperation();
                            JObject bodyParam = SwaggerParameter("body", "body");
                            bodyParam["schema"] = JObject.Parse("{\"type\": \"object\"}");
                            ((JArray)swagger["paths"][typePath]["post"]["parameters"]).Add(bodyParam);
                        }

                        if (interaction.Contains("update") && (interactionList.Contains("all") || interactionList.Contains("update")))
                        {
                            swagger["paths"][instancePath]["put"] = SwaggerOperation();
                            JObject bodyParam = SwaggerParameter("body", "body");
                            bodyParam["schema"] = JObject.Parse("{\"type\": \"object\"}");
                            ((JArray)swagger["paths"][instancePath]["put"]["parameters"]).Add(bodyParam);
                            ((JArray)swagger["paths"][instancePath]["put"]["parameters"]).Add(SwaggerParameter("id", "path", "string", null, true));
                        }

                        if (interaction.Contains("delete") && (interactionList.Contains("all") || interactionList.Contains("delete")))
                        {
                            swagger["paths"][instancePath]["delete"] = SwaggerOperation();
                            ((JArray)swagger["paths"][instancePath]["delete"]["parameters"]).Add(SwaggerParameter("id", "path", "string", null, true));
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
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
