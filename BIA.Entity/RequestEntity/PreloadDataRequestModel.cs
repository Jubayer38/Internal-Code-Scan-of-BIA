using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class PreloadDataRequestModel
    {
        public int channel_id { get; set; }
        public string lan { get; set; } = "en";
        public string channel_name { get; set; } = string.Empty;
        public string retailer_id { get; set; } = string.Empty;
    }
}
