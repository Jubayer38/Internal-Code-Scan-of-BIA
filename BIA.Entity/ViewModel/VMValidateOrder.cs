using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ViewModel
{
    public class VMValidateOrder
    {
        public string msisdn { get; set; } = string.Empty;
        public string sim_number { get; set; } = string.Empty;
        public int? purpose_number { get; set; }
        public int? is_corporate { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
    }
}
