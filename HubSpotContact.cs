using System.Collections.Generic;

namespace evalan_hubspot
{
        public class Property
    {
        public string property { get; set; }
        public string value { get; set; }
    }

    public class HubSpotContactPoperties
    {
        public List<Property> properties { get; set; }
    }
}