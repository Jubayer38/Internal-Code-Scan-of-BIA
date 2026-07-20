using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class DMSLoginResponse
    {
        public string Message { get; set; } = string.Empty;
        public int Status { get; set; } 
        public LoginData Data { get; set; } = new LoginData();
        public Errors Errors { get; set; } = new Errors();
    }
    public class LoginData
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string AccessTokenExpireInMinutes { get; set; } = string.Empty;
        public string CODE { get; set; } = string.Empty;
        public string NAME { get; set; } = string.Empty;
        public int MFSID { get; set; } =0;
    }
    public class Errors
    {
    }
}
