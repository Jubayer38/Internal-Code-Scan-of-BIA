using BIA.Entity.ResponseEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class PaiedMSISDNCheckResponseDataRevV1
    {
        public PaiedMSISDNCheckResponseRevV1 data { get; set; }
        public bool isError { get; set; }
        public string message { get; set; }
    }
    public class PaiedMSISDNCheckResponseRevV1
    {
        /// <summary>
        /// SIM card number (i.e. "981809647747") 
        /// </summary>
        public string sim_number { get; set; }
        /// <summary>
        /// Subscreiption type code (i.e. "")
        /// </summary>
        public string subscription_type_code { get; set; }
        /// <summary>
        /// imsi number (i.e. "470037108801557") 
        /// </summary>
        public string imsi { get; set; }
        public string number_category { get; set; }
        public string message { get; set; }
        public string category { get; set; }
        public bool isDesiredCategory { get; set; }
        public string details_message { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;
        public string offer_name { get; set; } = string.Empty;
    }
}
