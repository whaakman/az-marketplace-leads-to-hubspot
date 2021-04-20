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
using System.Net.Http;

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

        log.LogInformation("Processing lead");
	
        // Hubspot API information
	    string hubspotAPIKey = config["hubspotAPIKEY"];
        string baseURI = "https://api.hubapi.com/crm/v3/objects/contacts?limit=10&archived=false&hapikey=";
        string URI = baseURI + hubspotAPIKey;

        // Debug
        log.LogInformation(URI);
        
        // Method: process body from marketplace

        // Create HTTP client
        static HttpClient HTTPClient()
        {
            HttpClient httpClient = new HttpClient();
            return httpClient;
        }

        static async Task<string> WriteLeadHubSpot(string URI)
        {
            HttpClient httpClient = HTTPClient();
            HttpResponseMessage response = await httpClient.GetAsync(URI);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        string result = await WriteLeadHubSpot(URI);

        
        // Method: Write to Hubspot

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        // debug
        log.LogInformation(requestBody);

        log.LogInformation(result);

        // Marketplace can only deal with HTTP status codes. Message doesn't matter
        string responseMessage = "Function was triggered";

        return new OkObjectResult(responseMessage);
        }
    }
}
