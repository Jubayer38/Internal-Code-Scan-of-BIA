using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class ActivationCheckRequestModel
    {
        [Required]
        public string mobile_number { get; set; } = "";
        /// <summary>
        /// Language that defines in which language user wants to use device.
        /// </summary>
        public string? lan { get; set; } = "";

        /// <summary>
        /// Reseller user name (id) (i.e. "201949")
        /// </summary>
        [Required]
        public string retailer_id { get; set; } = "";

        /// <summary>
        /// Reseller channel name (i.e. "RESELLER", "Corporate")
        /// </summary>
        [Required]
        public string channel_name { get; set; } = "";
        /// <summary>
        /// Reseller inventory id.
        /// </summary>
        //[Required]
        public int? inventory_id { get; set; } = 0;

        /// <summary>
        /// SIM category (i.e. Prepaid = 1, Postpaid = 2)
        /// </summary> 
        public int? sim_category { get; set; }
        [Required, StringLength(12, ErrorMessage = "SIM number must be 12 digit number.")]
        public string sim_number { get; set; } = "";
        public int channel_id { get; set; } = 0;
        public int is_bp { get; set; }
        public string product_code { get; set; } = string.Empty;
        public string initiator_channel { get; set; } = string.Empty;
        public string order_type { get; set; } = string.Empty;
        public string subscription_type { get; set; } = string.Empty;
        public string simkit_type { get; set; } = string.Empty;
        public int right_id { get; set; }

    }
}
