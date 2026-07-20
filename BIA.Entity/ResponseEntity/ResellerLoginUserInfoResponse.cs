using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class ResellerLoginUserInfoResponse
    {
        public string user_id { get; set; } = string.Empty;
        public string user_name { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string role_id { get; set; } = string.Empty;
        public string role_name { get; set; } = string.Empty;
        public int? is_role_active { get; set; }
        public int? channel_id { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public int? is_activedirectory_user { get; set; }
        public string role_access { get; set; } = string.Empty;
        public string distributor_code { get; set; } = string.Empty;
        public int inventory_id { get; set; } 
        public string center_code { get; set; } = string.Empty;
        public string itopUpNumber { get; set; } = string.Empty;    
    }
}
