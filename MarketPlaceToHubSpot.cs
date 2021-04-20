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
using System.Collections.Generic;

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

        // Method to process body from marketplace
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        Lead deserializedMarketplaceLead = JsonConvert.DeserializeObject<Lead>(requestBody);
	
        // Hubspot API information
	    string hubspotAPIKey = config["hubspotAPIKEY"];
        string baseURI = "https://api.hubapi.com/crm/v3/objects/contacts?limit=10&archived=false&hapikey=";
        string URI = baseURI + hubspotAPIKey;
        var details = new HubSpotContactPoperties();

        // You can use as much properties as you want, as long as they exist in HubSpot.
        // Do a HTTP get to https://api.hubapi.com/properties/v1/contacts/properties?hapikey=<APIKEY> to check
        details.properties = new List<Property>
        {
            new Property { property = "firstname", value = deserializedMarketplaceLead.userDetails.firstName },
            new Property { property = "lastName", value = deserializedMarketplaceLead.userDetails.lastName },
            new Property { property = "email", value = deserializedMarketplaceLead.userDetails.email },
            new Property { property = "website", value = "NotProvidedFromAzureMarketPlace" },
            new Property { property = "company", value = deserializedMarketplaceLead.userDetails.company },
            new Property { property = "phone", value = deserializedMarketplaceLead.userDetails.phone },
            new Property { property = "address", value = "NotProvidedFromAzureMarketPlace"},
            new Property { property = "city", value = "NotProvidedFromAzureMarketPlace" },
            new Property { property = "state", value = "NotProvidedFromAzureMarketPlace" },
            new Property { property = "zip", value = "NotProvidedFromAzureMarketPlace" },
            // You could use deserializedMarketplaceLead.leadSource here but it needs to exist in Hubspot!
            new Property { property = "lead_source", value = "Microsoft.com Referral"},
            new Property { property = "message", value = "Offer Title: " + deserializedMarketplaceLead.offerTitle }
        };

        string postBody = JsonConvert.SerializeObject(details);
        //log.LogInformation(postBody);



        // Debug
        //log.LogInformation(URI);
        

        // Create HTTP client
        static HttpClient HTTPClient()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }


        // Write contact to hubspot
        static async Task<string> WriteLeadHubSpot(string URI, string postBody)
        {
            HttpClient httpClient = HTTPClient();
            HttpResponseMessage response = await httpClient.PostAsJsonAsync(URI, postBody);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        string result = await WriteLeadHubSpot(URI, postBody);
        log.LogInformation(result);

        // Marketplace can only deal with HTTP status codes. Message doesn't matter
        string responseMessage = "Function was triggered";

        return new OkObjectResult(responseMessage);
        }
    }
}
