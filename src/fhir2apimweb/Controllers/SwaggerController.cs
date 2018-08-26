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
    public class Swagger : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult> Get(string fhirserver, string metadata_endpoint, string interaction_list)
        {
            string metadataEndpoint = "metadata?_format=json";
            string[] interactionList = { "all" };

            if (!String.IsNullOrEmpty(metadata_endpoint))
            {   
                 metadataEndpoint = metadata_endpoint;
            }

            if (!String.IsNullOrEmpty(interaction_list))
            {
                interactionList = interaction_list.Split(",");
            }

            string swagger = await Fhir2Apim.GetSwaggerFromMetadata(fhirserver, metadataEndpoint, interactionList);

            return new ContentResult() {
                Content = swagger,
                StatusCode = 200,
                ContentType = "application/json"
            };
        }
    }
}
