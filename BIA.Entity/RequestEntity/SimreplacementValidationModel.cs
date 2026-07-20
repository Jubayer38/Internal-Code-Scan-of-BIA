using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class SimreplacementValidationModel
    {
        /// <summary>
        /// MSISDN number which starts with 880. 
        /// </summary>
        [Required]
        public string mobile_number { get; set; } = "";
        /// <summary>
        /// Language that defines in which language user wants to use device.
        /// </summary>
        public string? lan { get; set; } = "";
        /// <summary>
        /// Define Purpose Number to understand validation type. Currently purpose_number property contains value 
        /// while submitting order for diferrent purpose like new connection, sim replacement.
        /// For validation api request purpose_number inserted 0 from code level while log insert.
        /// </summary>
        [Required]
        public string? purpose_number { get; set; } = "";

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
        /// Reseller center code.
        /// </summary>
        public string? center_code { get; set; } = "";
        /// <summary>
        /// SIM category (i.e. Prepaid = 1, Postpaid = 2)
        /// </summary> 
        public int? sim_category { get; set; }
        public int channel_id { get; set; } = 0;

        [Required, StringLength(12, ErrorMessage = "SIM number must be 12 digit number.")]
        public string sim_number { get; set; } = string.Empty;//in DBSS API it is mapped with serial_no.
        public string initiator_channel { get; set; } = string.Empty;
        public string order_type { get; set; } = string.Empty;
        public string subscription_type { get; set; } = string.Empty;
        public string simkit_type { get; set; } = string.Empty;
    }
}
