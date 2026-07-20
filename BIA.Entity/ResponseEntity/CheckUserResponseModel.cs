using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class CheckUserResponseModel
    {
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
        public RespData data { get; set; } = new RespData();
    }

    public class RespData
    {
        public bool is_fp_validation_need { get; set; }
        public bool is_registered { get; set; }
        public string msisdn { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string MinimumScore { get; set; } = string.Empty;
        public string MaximumRetry { get; set; } = string.Empty;
    }

    public class DBResponseModel
    {
        public int is_fp_validation_need { get; set; }
        public int is_registered { get; set; }
        public string msisdn { get; set; } = string.Empty;
        public int is_user_valid { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
 