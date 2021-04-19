using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration; 

namespace evalan_hubspot
{
    public static class MarketPlaceToHubSpot
    {
        [FunctionName("MarketPlaceToHubSpot")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            	  var config = new ConfigurationBuilder()
	    .SetBasePath(context.FunctionAppDirectory)
	    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
	    .AddEnvironmentVariables() 
	    .Build();
				
	    //string hubspotAPIKey = config["hubspotAPIKEY"];
        log.LogInformation("Processing lead");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            // debug
            log.LogInformation(requestBody);

            string responseMessage = "Function was triggered";

            return new OkObjectResult(responseMessage);
        }
    }
}
