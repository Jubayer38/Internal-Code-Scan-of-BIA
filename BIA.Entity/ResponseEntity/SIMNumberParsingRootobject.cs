using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class SIMNumberParsingRootobject
    {
        public List<SIMNumberParsingDatum> data { get; set; } = new List<SIMNumberParsingDatum>();
    }

    public class SIMNumberParsingDatum
    {
        public SIMNumberParsingAttributes attributes { get; set; } = new SIMNumberParsingAttributes();
        public SIMNumberParsingRelationships relationships { get; set; } = new SIMNumberParsingRelationships();
        public SIMNumberParsingLinks1 links { get; set; } = new SIMNumberParsingLinks1();
        public string id { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
    }

    public class SIMNumberParsingAttributes
    {
        public string puk1 { get; set; } = string.Empty;
        public bool ismultisurf { get; set; } = new bool();
        public string pin1 { get; set; } = string.Empty;
        public string icc { get; set; } = string.Empty;
        public string puk2 { get; set; } = string.Empty;
        public string pin2 { get; set; } = string.Empty;        
        public string simtype { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
    }

    public class SIMNumberParsingRelationships
    {
        public SIMNumberParsingSubscription subscription { get; set; } = new SIMNumberParsingSubscription();
    }

    public class SIMNumberParsingSubscription
    {
        public SIMNumberParsingData data { get; set; } = new SIMNumberParsingData();
        public SIMNumberParsingLinks links { get; set; } = new SIMNumberParsingLinks(); 
    }

    public class SIMNumberParsingData
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class SIMNumberParsingLinks
    {
        public string related { get; set; } = string.Empty;
    }

    public class SIMNumberParsingLinks1
    {
        public string self { get; set; } = string.Empty;
    }
}
