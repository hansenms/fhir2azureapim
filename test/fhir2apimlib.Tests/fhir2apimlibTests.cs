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

    }
}
