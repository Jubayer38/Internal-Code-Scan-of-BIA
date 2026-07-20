using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class ValidTokenResponse
    {
        public string LoginProviderId { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DistributorCode { get; set; } = string.Empty;
        public string CenterCode { get; set; } = string.Empty;
        public bool IsVallid { get; set; }
        public string Jti { get; set;}
        public string Message { get; set; } = string.Empty; 
    }
}
