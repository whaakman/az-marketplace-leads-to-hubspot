# HTTPS Endpoint for Azure Marketplace Leads to HubSpot
This Azure Function accepts leads from Azure Marketplace and passes them to HubSpot. This creates a contact in HubSpot. Depending ont he HubSpot features you have available you could write them somewhere else. You can use the same code as below but you need to modify the call to the HubSpot API.

## Requirements
- Azure Function App
- [Hubspot API Key](https://knowledge.hubspot.com/integrations/how-do-i-get-my-hubspot-api-key?_ga=2.86242584.1279156532.1618828099-380446826.1615450408)

## Configuration
1. Make sure you have an Azure Function App running on Windows and .Net Core [Howto](https://docs.microsoft.com/en-us/azure/azure-functions/functions-get-started?pivots=programming-language-csharp&WT.mc_id=AZ-MVP-5003649)

2. Add the *hubspotAPIKey* to your app settings. Add the HubSpot API Key as a value
3. Grab the Function URL from the Azure Portal
4. Go to your [Azure Marketplace offers](https://partner.microsoft.com/en-us/dashboard/commercial-marketplace/offers) and click on your offering
5. Go to "Offer Setup" and under "Customer Leads" configure the HTTPS endpoint for your leads. Be aware that you include the Function key.

## Important
HubSpot supports many, many fields (properties) that you can use. As long as the property exists in HubSpot you can pass it along with any value you want, or with a value that comes from the Azure Marketplace. The properties need to match exactly. You can query the endpoint: https://api.hubapi.com/properties/v1/contacts/properties?hapikey=APIKEY to verify the existence of a property.

```CSharp
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
            // You could use deserializedMarketplaceLead.leadSource here but it needs to exist in Hubspot!
            new Property { property = "lead_source", value = "Microsoft.com Referral"},
            new Property { property = "message", value = "Offer Title: " + deserializedMarketplaceLead.offerTitle }
        };
// Serialize details to jsonBody
string jsonBody = JsonConvert.SerializeObject(details);
```` 


## To Do
- Add Oauth support
- Error handling
- Checking if values exist
