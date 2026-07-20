using BIA.Entity.CommonEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class UnpairedMSISDNListReqModel : RACommonRequest
    {
        public string msisdn { get; set; } = string.Empty;
        public string retailer_id { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
        public int is_fwa { get; set; }
        public string? FWA_channel_name { get; set; } = string.Empty;
    }
     
    public class PairedMSISDNReqModel : RACommonRequest
    {
        public string sim_serial { get; set; } = string.Empty;
        public string retailer_id { get; set; } = string.Empty;  
    }

}
