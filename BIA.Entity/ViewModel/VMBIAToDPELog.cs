using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ViewModel
{
    public class VMBIAToDPELog
    {
        public string order_number { get; set; } = string.Empty;

        public string username { get; set; } = string.Empty;

        public byte[] req_blob { get; set; } = Array.Empty<byte>();

        public byte[] res_blob { get; set; } = Array.Empty<byte>();

        public DateTime req_time { get; set; }

        public DateTime res_time { get; set; }

        public decimal is_success { get; set; }

        public string message { get; set; } = string.Empty;

        public string error_code { get; set; } = string.Empty;

        public string error_source { get; set; } = string.Empty;

        public string method_name { get; set; } = string.Empty;

        public string remarks { get; set; } = string.Empty;

        public string server_name { get; set; } = string.Empty;
    }
}
