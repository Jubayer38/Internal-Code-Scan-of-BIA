using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    /// <summary>
    ///This api use to replace corporate SIM.
    /// </summary>
    public class CorporateSimReplacementConfirmRequest
    {
        /// <summary>
        /// POC
        /// </summary>
        public string POC { get; set; } = string.Empty;
        /// <summary>
        /// Moble Number (MSISDN)
        /// </summary>
        public string MSISDN { get; set; } = string.Empty;
        /// <summary>
        /// SIM munber is an unique number which is always paired with a MSISDN.
        /// </summary>
        public string sim_number { get; set; } = string.Empty;
        /// <summary>
        /// Document Type
        /// </summary>
        public string doc_type { get; set; } = string.Empty;
        /// <summary>
        /// Payment Method
        /// </summary>
        public string payment_method { get; set; } = string.Empty;
        /// <summary>
        ///Language that defines in which language user wants to use device.
        /// </summary>
        public string lan { get; set; } = string.Empty;

        /// <summary>
        /// Session Token
        /// </summary>
        public string session_token { get; set; } = string.Empty;


    }
}
