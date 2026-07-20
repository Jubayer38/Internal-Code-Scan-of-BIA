using BIA.Entity.CommonEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class AppInfoUpdateReqModel :RACommonRequest
    {
        public string app_version_code { get; set; } = string.Empty;    
        public string app_version_name { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
        public string user_name { get; set; } = string.Empty;
        public string distributor_code { get; set; } = string.Empty;
        public string center_code { get; set; } = string.Empty;
    }
}
 