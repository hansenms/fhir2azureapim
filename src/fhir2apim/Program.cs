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
            string format = "swagger";

            string[] interactionList = { "all" };
            string[] resourceList = { "all" };

            int arg = 0;
            while (arg < args.Length)
            {
                if (args[arg] == "-s" || args[arg] == "--server")
                {
                    arg++;
                    fhirServerUrl = args[arg++];
                }
                else if (args[arg] == "-m" || args[arg] == "--meta")  
                {
                    arg++;
                    metadataEndpoint = args[arg++]; 
                } 
                else if (args[arg] == "-o" || args[arg] == "--out")
                {
                    arg++;
                    outFileName = args[arg++];
                }
                else if (args[arg] == "-r" || args[arg] == "--resources")
                {
                    arg++;
                    resourceList = args[arg++].Split(",");
                }
                else if (args[arg] == "-i" || args[arg] == "--interactions")
                {
                    arg++;
                    interactionList = args[arg++].Split(",");
                }
                else if (args[arg] == "-f" || args[arg] == "--format")
                {
                    arg++;
                    if (args[arg] != "swagger" && args[arg] != "arm")
                    {
                        Console.WriteLine("Valid formats are 'swagger' or 'arm'");
                        PrintUsage();
                        return;
                    }
                    format = args[arg++];
                }
                else
                {
                    Console.WriteLine($"Unknow command line argument: {args[arg]}");
                    PrintUsage();
                    return;
                }
            }


            if (String.IsNullOrEmpty(fhirServerUrl))
            {
                Console.WriteLine("No FHIR server URL provided");
                PrintUsage();
                return;
            }

            string output = null;

            if (format == "swagger")
            {
                output = await Fhir2Apim.GetSwaggerFromMetadata(fhirServerUrl, metadataEndpoint, interactionList, resourceList);
            }
            else
            {
                output = await Fhir2Apim.GetArmApiFromMetadata(fhirServerUrl, metadataEndpoint, interactionList, resourceList);
            }

            System.IO.File.WriteAllText($"{outFileName}", output);
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  fhir2apim -s|--server <FHIR SERVER URL>");
            Console.WriteLine("            -m|--meta <METADATA ENDPOINT> (default: 'metadata?_format=json')");
            Console.WriteLine("            -o|--out <OUTFILE> (default: 'out.json')");
            Console.WriteLine("            -r|--resources <LIST OF RESOURCES> (default: 'all')");
            Console.WriteLine("            -i|--rinteractions <LIST OF INTERACTIONS> (default: 'all')");
            Console.WriteLine("            -f|--format <swagger|arm> (default: 'swagger')");
        }
    }
}
