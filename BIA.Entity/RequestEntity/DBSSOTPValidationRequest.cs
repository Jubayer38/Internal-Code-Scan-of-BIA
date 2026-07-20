using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class DBSSOTPValidationRequest
    {
        public string otp { get; set; } = string.Empty;
        public string poc_msisdn { get; set; } = string.Empty;
        public string auth_msisdn { get; set; } = string.Empty;
        public int purpose { get; set; }
    }
    public class DBSSOTPValidationRequestRootobject
    {
        public DBSSOTPValidationRequestData data { get; set; } = new DBSSOTPValidationRequestData();
    }
    public class DBSSOTPValidationRequestData
    {
        public int id { get; set; }
        public string type { get; set; } = string.Empty;
        public DBSSOTPValidationRequestAttributes attributes { get; set; } = new DBSSOTPValidationRequestAttributes();
    }
    public class DBSSOTPValidationRequestAttributes
    {
        public string otp { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string identifier { get; set; } = string.Empty;      
        public int purpose { get; set; }
    }
}
