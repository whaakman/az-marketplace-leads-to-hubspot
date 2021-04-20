namespace evalan_hubspot
{
    // Lead deserializedMarketplaceLead = JsonConvert.DeserializeObject<Lead>(myJsonResponse); 
    public class UserDetails
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string country { get; set; }
        public string company { get; set; }
        public string title { get; set; }
    }

    public class Lead
    {
        public UserDetails userDetails { get; set; }
        public string leadSource { get; set; }
        public string actionCode { get; set; }
        public string offerTitle { get; set; }
        public string description { get; set; }
    }
}