using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class GACappingResponse
    {
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;

        public GACappingData2 data { get; set; } = new GACappingData2();

    }

    public class GACappingData2
    {
        public string user_id { get; set; } = string.Empty;
        public bool isEligible { get; set; }
        public int weeklyCount { get; set; }
        public int monthlyCount { get; set; }
        public int lifetimeCount { get; set; }
    }
}
