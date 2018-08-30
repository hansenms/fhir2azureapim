using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using fhir2apimlib;

namespace fhir2apimweb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Arm : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult> Get(string fhirserver, string metadata_endpoint, 
                                            string interaction_list, string resource_list)
        {
            string metadataEndpoint = "metadata?_format=json";
            string[] interactionList = { "all" };
            string[] resourceList = { "all" };

            if (!String.IsNullOrEmpty(metadata_endpoint))
            {   
                 metadataEndpoint = metadata_endpoint;
            }

            if (!String.IsNullOrEmpty(interaction_list))
            {
                interactionList = interaction_list.Split(",");
            }

            if (!String.IsNullOrEmpty(resource_list))
            {
                resourceList = resource_list.Split(",");
            }

            string arm = await Fhir2Apim.GetArmApiFromMetadata(fhirserver, metadataEndpoint, interactionList, resourceList);

            return new ContentResult() {
                Content = arm,
                StatusCode = 200,
                ContentType = "application/json"
            };
        }
    }
}
