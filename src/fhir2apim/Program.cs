using System;
using fhir2apimlib;
using System.Threading.Tasks;

namespace fhir2apim
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            string swagger = await Fhir2Apim.GetSwaggerFromMetadata("http://hapi-wiy7bk64ytbly.azurewebsites.us/hapi-fhir-jpaserver-example/baseDstu3/","metadata");
            System.IO.File.WriteAllText(@".\swagger.json", swagger);
            Console.WriteLine("Done");
        }
    }
}
