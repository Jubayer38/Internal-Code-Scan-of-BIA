using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.DB_Model
{
    public class BIAToDBSSLog
    {
        public string bi_token_number { get; set; } = string.Empty;
        public string dbss_request_id { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string purpose_number { get; set; } = string.Empty;
        public string user_id { get; set; } = string.Empty;

        public byte[] req_blob { get; set; } = Array.Empty<byte>();
        public byte[] res_blob { get; set; } = Array.Empty<byte>();

        public DateTime req_time { get; set; }
        public DateTime res_time { get; set; }
        public decimal is_success { get; set; }
        public string message { get; set; } = string.Empty;
        public string error_code { get; set; } = string.Empty;
        public string error_source { get; set; } = string.Empty;
        public string method_name { get; set; } = string.Empty;
        public decimal integration_point_from { get; set; }
        public decimal integration_point_to { get; set; }
        public string remarks { get; set; } = string.Empty;
        public string text_req { get; set; } = string.Empty;
        public string text_resp { get; set; } = string.Empty;
    } 

    public class BIAToRaiseComplainLog
    {
        public string bi_token_number { get; set; } = string.Empty;
        public string dbss_request_id { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string purpose_number { get; set; } = string.Empty; 
        public string user_id { get; set; } = string.Empty;

        public decimal complaint_id { get; set; } = 0;

        public byte[] req_blob { get; set; } = Array.Empty<byte>();
        public byte[] res_blob { get; set; } = Array.Empty<byte>();

        public DateTime req_time { get; set; }
        public DateTime res_time { get; set; }
        public decimal is_success { get; set; }
        public string message { get; set; } = string.Empty;
        public string error_code { get; set; } = string.Empty;
        public string error_source { get; set; } = string.Empty;
        public string method_name { get; set; } = string.Empty;
        public decimal integration_point_from { get; set; }
        public decimal integration_point_to { get; set; }
        public string remarks { get; set; } = string.Empty;
    }

    public class BIAToDPELog
    {
        public string order_number { get; set; } = string.Empty;

        public DateTime req_time { get; set; }

        public DateTime res_time { get; set; }

        public string remarks { get; set; } = string.Empty;

        public string message { get; set; } = string.Empty;

        public decimal is_success { get; set; }

        public string method_name { get; set; } = string.Empty;

        public byte[] req_blob { get; set; } = Array.Empty<byte>();

        public byte[] res_blob { get; set; } = Array.Empty<byte>();

        public string user_id { get; set; } = string.Empty;
        public string error_code { get; set; } = string.Empty;

        public string error_source { get; set; } = string.Empty;
    }
}
