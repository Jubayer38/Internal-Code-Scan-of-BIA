using BIA.Entity.CommonEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class OTPResponse : RACommonResponse
    {
        public bool is_otp_valid { get; set; }
    }

    public class OTPResponseRev
    {
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
        public OTPRespData data { get; set; } = new OTPRespData();
    }
    public class OTPRespData
    {
        public bool is_otp_valid { get; set; }
    }

    public class DBSSOTPResponseRootobject
    {
        public DBSSOTPResponseRootobjectData data { get; set; } = new DBSSOTPResponseRootobjectData();
    }

    public class DBSSOTPResponseRootobjectData
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;  
        public DBSSOTPResponseRootobjectAttributes attributes { get; set; } = new DBSSOTPResponseRootobjectAttributes();        
    }

    public class DBSSOTPResponseRootobjectAttributes
    {
        public string msisdn { get; set; } = string.Empty;
        public int purpose { get; set; }
        public string identifier { get; set; } = string.Empty;
        public string otp { get; set; } = string.Empty;
    }
}
