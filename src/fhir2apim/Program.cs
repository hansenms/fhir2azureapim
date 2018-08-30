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
            string outFileName = "out.json";

            string[] interactionList = { "all" };
            string[] resourceList = { "all" };

            if (args.Length > 0) {
                fhirServerUrl = args[0];
            }

            if (args.Length > 1) {
                resourceList = args[1].Split(",");
            }

            if (args.Length > 2) {
                interactionList = args[1].Split(",");
            }

            if (String.IsNullOrEmpty(fhirServerUrl))
            {
                PrintUsage();
                return;
            }

            //string swagger = await Fhir2Apim.GetSwaggerFromMetadata(fhirServerUrl, metadataEndpoint, interactionList);
            string swagger = await Fhir2Apim.GetArmApiFromMetadata(fhirServerUrl, metadataEndpoint, interactionList, resourceList);

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
