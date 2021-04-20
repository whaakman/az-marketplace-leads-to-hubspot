using System.Collections.Generic;

namespace marketplacetohubspotleads
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