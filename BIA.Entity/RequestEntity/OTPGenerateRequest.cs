using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class OTPGenerateRequest
    {
        [Required]
        public string mobile_number { get; set; } = string.Empty;
        [Required]
        public string user_name { get; set; } = string.Empty;
        [Required]
        public string module_name { get; set; } = string.Empty;
        public string lan { get; set; } = string.Empty;
    }


    public class ValidateOTPAndChangePWDRequest
    {
        [Required]
        public string otp { get; set; } = string.Empty;
        [Required]
        public string user_name { get; set; } = string.Empty;
        [Required]
        public string new_pwd { get; set; } = string.Empty;
    }
}
