using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class DMSDBSessionResponse
    {
        public string SESSIONTOKEN { get; set; } = string.Empty;
        public DateTime CREATE_DATE { get; set; }
        public int SESSIONTIME { get; set; }
    }
}
