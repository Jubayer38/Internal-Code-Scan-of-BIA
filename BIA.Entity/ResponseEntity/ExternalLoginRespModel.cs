using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class ExternalLoginRespModel
    {
        public string sessiontoken { get; set; } = string.Empty;
        public bool isValid { get; set; }
        public string message { get; set; } = string.Empty;
        public int validity_time { get; set; }
        public string validity { get; set; }= string.Empty;
    }
}
