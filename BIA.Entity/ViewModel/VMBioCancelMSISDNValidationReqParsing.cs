using BIA.Entity.CommonEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ViewModel
{
    public class VMBioCancelMSISDNValidationReqParsing : RACommonResponse
    {
        public string? dob { get; set; } = string.Empty;
        public string? nid { get; set; } = string.Empty;
        public long subscription_id { get; set; }
        public int dest_sim_category { get; set; }
    }
}
