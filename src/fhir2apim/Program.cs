using System;
using fhir2apimlib;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace fhir2apim
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            string fhirServerUrl = null;
            string metadataEndpoint = "metadata?_format=json";
            string outFileName = "swagger.json";

            string[] interactionList = { "all" };

            if (args.Length > 0) {
                fhirServerUrl = args[0];
            }

            if (args.Length > 1) {
                interactionList = args[1].Split(",");
            }

            if (String.IsNullOrEmpty(fhirServerUrl))
            {
                PrintUsage();
                return;
            }

            string swagger = await Fhir2Apim.GetSwaggerFromMetadata(fhirServerUrl, metadataEndpoint, interactionList);

            System.IO.File.WriteAllText($"{outFileName}", swagger);
            Console.WriteLine("Done");
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  fhir2apim <fhirServerUrl>");
        }
    }
}
