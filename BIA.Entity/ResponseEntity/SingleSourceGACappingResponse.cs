using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class SingleSourceGACappingResponse
    {
        public List<GACappingData> data { get; set; } = new List<GACappingData>();
        public bool is_success { get; set; }
        public string message { get; set; } = string.Empty;
    }
    public class GACappingData
    {
        public string msisdns { get; set; } = string.Empty;
        public DateTime reg_date { get; set; }
        public string document_id { get; set; } = string.Empty;
        public string nid { get; set; } = string.Empty;
        public string smart_nid { get; set; } = string.Empty;
        public bool is_active { get; set; }
    }
}
