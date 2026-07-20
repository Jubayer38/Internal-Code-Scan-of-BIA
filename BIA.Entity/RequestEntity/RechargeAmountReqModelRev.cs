using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class RechargeAmountReqModelRev
    {
        [Required]
        public string session_token { get; set; } = string.Empty;
        public string retailer_code { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;

        [Required]
        public string bi_token_number { get; set; } = string.Empty;
    }
}
