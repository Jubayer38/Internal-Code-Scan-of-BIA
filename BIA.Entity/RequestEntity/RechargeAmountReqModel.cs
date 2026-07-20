using BIA.Entity.CommonEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class RechargeAmountReqModel
    {
        /// <summary>
        /// Security token for validating if the user is valid to get access to the api.
        /// </summary>
        [Required]
        public string session_token { get; set; } = string.Empty;
        public string retailer_code { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
    } 
}
