using BIA.Entity.CommonEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ViewModel
{
    public class VMActivityLog : RACommonResponse
    {
        public string token_id { get; set; } = string.Empty;
        public string time { get; set; } = string.Empty;
        public string mobile_number { get; set; } = string.Empty;
        public string nid { get; set; } = string.Empty;
        public string dob { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public int is_re_submittable { get; set; }
        public int re_submit_expire_time { get; set; }
        public string re_submit_error_message { get; set; } = string.Empty;
        public int right_id { get; set; }
        public string is_bp_user { get; set; } = string.Empty;
        public string bp_msisdn { get; set; } = string.Empty;
        public string action_point { get; set; } = string.Empty;
        public string designation { get; set; } = string.Empty;

    }
    public class VMActivityLogRevamp
    {
        public string token_id { get; set; } = string.Empty;
        public string time { get; set; } = string.Empty;
        public string mobile_number { get; set; } = string.Empty;
        public string nid { get; set; } = string.Empty;
        public string dob { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public int is_re_submittable { get; set; }
        public int re_submit_expire_time { get; set; }
        public string re_submit_error_message { get; set; } = string.Empty;
        public int right_id { get; set; }
        public string is_bp_user { get; set; } = string.Empty;
        public string bp_msisdn { get; set; } = string.Empty;
        public string action_point { get; set; } = string.Empty;
        public string designation { get; set; } = string.Empty;
        public string recharge_status { get; set; } = string.Empty;     
        public int is_recharge_done { get; set; }

    }
}
