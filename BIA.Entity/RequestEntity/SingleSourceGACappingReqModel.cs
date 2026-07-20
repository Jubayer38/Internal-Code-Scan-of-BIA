using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class SingleSourceGACappingReqModel
    {
        public string nid { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string retailer_id { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
        public string dob { get; set; } = string.Empty;
        public string mobile_number { get; set; } = string.Empty;
    }
}
