using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class OtherCDTResModel
    {
        public Object[] data { get; set; } = Array.Empty<Object>();
        public List<Included> included { get; set; } = new List<Included>();
    }
    public class Included
    {
        public Attributes1 attributes { get; set; } = new Attributes1();
        public Relationships1 relationships { get; set; } = new Relationships1();
        public Links31 links { get; set; } = new Links31();
        public string id { get; set; }  = string.Empty;
        public string type { get; set; } = string.Empty;        
    }

    public class Attributes1
    {
        public bool changeongoing { get; set; }
        public object upcominglevel { get; set; } = new object();
        public string subscriptionid { get; set; } = string.Empty;
        public string[] barringlevelids { get; set; } = Array.Empty<string>();
        public object[] upcomingbarringlevelids { get; set; } = Array.Empty<object>();
        public string level { get; set; } = string.Empty;
    }

    public class Relationships1
    {
        public Barring barring { get; set; } = new Barring();
        public BarringLevels barringlevels { get; set; } = new BarringLevels();
    }

    public class Barring
    {
        public Data77 data { get; set; } = new Data77();
        public Links29 links { get; set; } = new Links29();
    }

    public class Data77
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class Links29
    {
        public string related { get; set; } = string.Empty;
    }

    public class BarringLevels
    {
        public Links30 links { get; set; } = new Links30(); 
    }

    public class Links30
    {
        public string related { get; set; } = string.Empty;
    }

    public class Links31
    {
        public string self { get; set; } = string.Empty;
    }
}
