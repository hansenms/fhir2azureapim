using System;
using Xunit;
using fhir2apimlib;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace fhir2apimlib.Tests
{
    public class fhir2apimlibTests
    {
        [Fact]
        public void ArmParameterStringDefaultValuue()
        {
            JObject p = Fhir2Apim.ArmParameter("string", "test string");
            Assert.Equal("test string", (string)p["defaultValue"]);
        }

        [Fact]
        public void ArmApiValidForHapi()
        {
            string serviceUrl = "http://hapi.fhir.org/baseDstu3/";
            Uri fhirServer = new Uri(serviceUrl);
            JObject armApi = Fhir2Apim.ArmApimApi(serviceUrl);
            
            Assert.Equal("2017-03-01", (string)armApi["apiVersion"]);
            Assert.Equal("http", (string)armApi["properties"]["protocols"][0]);
        }

        [Theory]
        [InlineData("/Patient","GET")]
        [InlineData("/Patient","POST")]
        public void ArmApiOperation(string urlTemplate, string method)
        {
            JObject op = Fhir2Apim.ArmApiOperation(urlTemplate, method);
            Assert.Equal("2017-03-01", (string)op["apiVersion"]);
            Assert.Equal(method, (string)op["properties"]["method"]);
            Assert.Equal($"{urlTemplate} - {method}", (string)op["properties"]["displayName"]);
            Assert.Equal(urlTemplate, (string)op["properties"]["urlTemplate"]);
        }

        [Theory]
        [InlineData("patient", "string", "Patient name")]
        public void ArmApiQueryParameter(string name, string typeName, string description)
        {
            JObject parm = Fhir2Apim.ArmApiOperationQueryParameter(name, typeName, description);

            Assert.Equal(typeName, (string)parm["type"]);
            Assert.Equal(name, (string)parm["name"]);
            Assert.Equal(description, (string)parm["description"]);
        }

        [Theory]
        [InlineData("Body")]
        public void ArmApiOperationRepresentation(string typeName)
        {
            JObject rep = Fhir2Apim.ArmApiOperationRepresentation("Body");

            Assert.Equal("application/json", (string)rep["contentType"]);
            Assert.Equal(typeName, (string)rep["typeName"]);
        }


        [Theory]
        [InlineData("id","string",true)]
        public void ArmApiOperationTemplateParameter(string name, string typeName, bool required)
        {
            JObject templateParameter = Fhir2Apim.ArmApiOperationTemplateParameter(name, typeName, required);

            Assert.Equal(typeName, (string)templateParameter["type"]);
            Assert.Equal(required, (bool)templateParameter["required"]);
        }

        [Theory]
        [InlineData("http://hapi.fhir.org/baseDstu3/", "HAPI FHIR Server")]
        [InlineData("http://vonk.fire.ly/", "Vonk")]        
        public async Task ArmApiGeneration(string fhirServerUrl, string description)
        {
            string [] interactionList = { "all "};
            string [] resourceList = { "all "};
            string metadataEndpoint = "metadata?_format=json";

            string armApiString = await Fhir2Apim.GetArmApiFromMetadata(fhirServerUrl, metadataEndpoint, interactionList, resourceList);

            JObject armApi = JObject.Parse(armApiString);

            Assert.Equal(description, (string)armApi["resources"][0]["properties"]["description"]);
        }

        [Fact]
        public void SwaggerOperationIsValid()
        {
            JObject swagop = Fhir2Apim.SwaggerOperation();

            Assert.Equal("Success", (string)swagop["responses"]["200"]["description"]);
        }

        [Theory]
        [InlineData("id","path","string",null)]
        [InlineData("patient","query","string", "Patient identifier")]
        public void SwaggerParameterIsValid(string name, string inName, string typeName, string description)
        {
            JObject parm = Fhir2Apim.SwaggerParameter(name, inName, typeName, description);

            Assert.Equal(name, (string)parm["name"]);
            Assert.Equal(inName, (string)parm["in"]);
            Assert.Equal(typeName, (string)parm["type"]);
            if (description != null) 
            {
                Assert.Equal(description, (string)parm["description"]);
            }
            else
            {
                Assert.Null(parm["description"]);
            }
        }

        [Fact]
        public void SwaggerBaseApiIsValid()
        {
            string serviceUrl = "http://hapi.fhir.org/baseDstu3/";
            Uri fhirServer = new Uri(serviceUrl);
            JObject swaggerApi = Fhir2Apim.SwaggerApi(fhirServer);

            Assert.Equal("2.0", swaggerApi["swagger"]);
            Assert.Equal("Get conformance statement.", swaggerApi["paths"]["/metadata"]["get"]["summary"]);
        }

        [Theory]
        [InlineData("http://hapi.fhir.org/baseDstu3/", "HAPI FHIR Server")]
        [InlineData("http://vonk.fire.ly/", "Vonk")]        
        public async Task GetSwaggerApiReturnsDefinition(string fhirServerUrl, string description)
        {
            string [] interactionList = { "all" };
            string [] resourceList = { "all" };
            string metadataEndpoint = "metadata?_format=json";

            string swaggerApiString = await Fhir2Apim.GetSwaggerFromMetadata(fhirServerUrl, metadataEndpoint, interactionList, resourceList);

            JObject swaggerApi = JObject.Parse(swaggerApiString);

            Assert.Equal("2.0", swaggerApi["swagger"]);
            Assert.Equal(description, (string)swaggerApi["info"]["description"]);
            Assert.Equal("Get conformance statement.", swaggerApi["paths"]["/metadata"]["get"]["summary"]);
            Assert.NotNull(swaggerApi["paths"]["/Patient"]);
            Assert.NotNull(swaggerApi["paths"]["/Patient"]["get"]["parameters"]);
        }


        [Theory]
        [InlineData("http://hapi.fhir.org/baseDstu3/", "HAPI FHIR Server")]
        [InlineData("http://vonk.fire.ly/", "Vonk")]        
        public async Task GetSwaggerApiReturnsDefinitionWithExclusions(string fhirServerUrl, string description)
        {
            string [] interactionList = { "delete" };
            string [] resourceList = { "Account" };
            string metadataEndpoint = "metadata?_format=json";

            string swaggerApiString = await Fhir2Apim.GetSwaggerFromMetadata(fhirServerUrl, metadataEndpoint, interactionList, resourceList);

            JObject swaggerApi = JObject.Parse(swaggerApiString);

            Assert.Equal("2.0", swaggerApi["swagger"]);
            Assert.Equal(description, (string)swaggerApi["info"]["description"]);
            Assert.Equal("Get conformance statement.", swaggerApi["paths"]["/metadata"]["get"]["summary"]);
            Assert.Null(swaggerApi["paths"]["/Patient"]);
            Assert.NotNull(swaggerApi["paths"]["/Account/{id}"]["delete"]);
        }

    }
}
