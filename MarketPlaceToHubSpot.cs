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
using System.Text;
using System.Linq;

namespace marketplaceleadstohubspot
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
        
        // Hubspot API configuration:
        // 
        // hubspotAPIKEY: the old API KEY (not supported anymore after November 30 2022)
        // See https://developers.hubspot.com/changelog/upcoming-api-key-sunset
        // 
        // HUBSPOT_PRIVATE_APP_KEY: a private app key, should start with 
        // See https://developers.hubspot.com/docs/api/migrate-an-api-key-integration-to-a-private-app
        // 
        // HUBSPOT_PROPERTIES: the list of coma-separate properties to be synced
        // they should exist in hubspot (first part is how they are found in the AppSource second is how they should go hubspot). 
        // https://appsource.microsoft.com/en-us/product/office/WXAAAA?mktcmpid=1234
        // Examples:
        // leadSource, mtkcmpid, partnerid
        // leadSource=lead_source, actionCode=action_code, mktcmpid=mktcmpid

        var hubspotConfig = new HubspotConfig {
                APIKEY_DEPRECATED = config["hubspotAPIKEY"],
                HUBSPOT_PRIVATE_APP_KEY = config["HUBSPOT_PRIVATE_APP_KEY"],
                HUBSPOT_PROPERTIES = (config["HUBSPOT_PROPERTIES"] ?? "")
                    .Split(",")
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToDictionary(v => v.Split("=").First(), v => v.Split("=").Last())
            };

        // Process leads from the marketplace
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        Lead deserializedMarketplaceLead = JsonConvert.DeserializeObject<Lead>(requestBody);
  
    
        var description = deserializedMarketplaceLead.description;

        // You can use as much properties as you want, as long as they exist in HubSpot.
        // Do a HTTP get to https://api.hubapi.com/properties/v1/contacts/properties?hapikey=<APIKEY> to check
        var details = new HubSpotContactPoperties();
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
            new Property { property = "message", value = "Offer Title: " + deserializedMarketplaceLead.offerTitle + " Description: " + description },
            };

            // You could use other properties but they need to exists in hubspot (and mapped in the config)
            void TryAddProperty(string appSourceName, string value) 
            {
                if (hubspotConfig.HUBSPOT_PROPERTIES.TryGetValue(appSourceName, out var huspotName))
                {
                    details.properties.Add(new Property { property = huspotName, value = value });
                }
            }

            TryAddProperty("leadSource", deserializedMarketplaceLead.leadSource);
            TryAddProperty("actionCode", deserializedMarketplaceLead.actionCode);

            // if extra parameters are found on AppSource the URL, they are passed in in description as JSON
            // Example link: https://appsource.microsoft.com/en-us/product/office/WXAAAA?mktcmpid=1234&partnerid=98765
            if (!string.IsNullOrEmpty(description) && description.StartsWith("{") && description.EndsWith("}"))
            {
                var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(deserializedMarketplaceLead.description);
                foreach (var kvp in json)
                {
                    TryAddProperty(kvp.Key, kvp.Value);
                }
            }

        // Serialize details to jsonBody
        string jsonBody = JsonConvert.SerializeObject(details);

        // Write Leads to HubSpot
        string result = await WriteLeadHubSpot(jsonBody, hubspotConfig);

        // Methods for HTTP Client, and posting to HubSpot
        // Create HTTP client
        static HttpClient HTTPClient(string bearerToken)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Clear();
            
            // use bearer token for new private app authentication
            if (!string.IsNullOrEmpty(bearerToken))
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + bearerToken);

            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }

        // Write contact to hubspot
        static async Task<string> WriteLeadHubSpot(string jsonBody, HubspotConfig keys)
        {
            // if still using the deprecated API KEY, append it to the URL
            string hapikeyParam = string.IsNullOrEmpty(keys.HUBSPOT_PRIVATE_APP_KEY) ? ("?hapikey=" + keys.APIKEY_DEPRECATED) : string.Empty;
            string URI = "https://api.hubapi.com/contacts/v1/contact/" + hapikeyParam;

            HttpClient httpClient = HTTPClient(keys.HUBSPOT_PRIVATE_APP_KEY);
            StringContent postBody = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(URI, postBody);

            // log huspot error so that we know if there is a problem
            if (!response.IsSuccessStatusCode) {
                if (response.Content.Headers.ContentType.MediaType == "application/json") 
                {
                    string huspotMessage = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(huspotMessage);
                }
            }
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        // Marketplace can only deal with HTTP status codes. Message doesn't matter
        string responseMessage = "Function was triggered";
        
        return new OkObjectResult(responseMessage);
        }
    }
}
