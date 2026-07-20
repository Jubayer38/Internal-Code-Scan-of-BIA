using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class ExternalUserValidationRespModel
    {
        public string user_id { get; set; } = string.Empty;
        public string user_name { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
        public bool is_valid { get; set;}
        public string message { get; set; } = string.Empty;
    }
}
