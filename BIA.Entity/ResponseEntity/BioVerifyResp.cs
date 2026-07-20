using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class BioVerifyResp
    {
        public bool is_success { get; set; }
        public string err_code { get; set; } = string.Empty;
        public string err_msg { get; set; } = string.Empty;
        public string bss_req_id { get; set; } = string.Empty;
        public string Reservation_Id { get; set; }  = string.Empty; 
        public long error_Id { get; set; }
    }
}
