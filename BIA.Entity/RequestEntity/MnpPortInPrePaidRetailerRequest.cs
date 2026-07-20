using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    /// <summary>
    /// MNP Port In for Per paid Retailer Request
    /// </summary>
    public class MnpPortInPrePaidRetailerRequest
    {
        /// <summary>
        /// Unique ID which is used to tarck the new connection orders activation status. 
        /// </summary>
        public decimal token_id { get; set; }
        /// <summary>
        /// Customer's NID (Bangladeshi) which is unique and identical for each user.
        /// </summary>
        public string nid { get; set; } = string.Empty; 
        /// <summary>
        /// Newly ordered MSISDN 
        /// </summary>
        public string mobile_number { get; set; } = string.Empty;
        /// <summary>
        /// Date of birth of new customer
        /// </summary>
        public string dob { get; set; } = string.Empty;
        /// <summary>
        /// SIM munber is an unique number which is always paired with a MSISDN.
        /// </summary>
        public string sim_number { get; set; } = string.Empty;
        /// <summary>
        /// The name of the owner of the newly ordered MSISDN.
        /// </summary>
        public string customer_name { get; set; } = string.Empty;
        /// <summary>
        /// Gender of customer
        /// </summary>
        public string gender { get; set; } = string.Empty;
        /// <summary>
        /// Flat number of customers house.
        /// </summary>
        public string flat_number { get; set; } = string.Empty;
        /// <summary>
        /// House number of customer.
        /// </summary>
        public string house_number { get; set; } = string.Empty;
        /// <summary>
        /// Road number of customer's receident.
        /// </summary>
        public string road_number { get; set; } = string.Empty;
        /// <summary>
        /// Customers village name.
        /// </summary>
        public string village { get; set; } = string.Empty;
        /// <summary>
        /// Customer's district ID number. 
        /// </summary>
        public string district_id { get; set; } = string.Empty;
        /// <summary>
        /// Customer's Thana ID number. 
        /// </summary>
        public string thana_id { get; set; }    = string.Empty;
        /// <summary>
        /// Postal code of customer's living area.
        /// </summary>
        public string postal_code { get; set; } = string.Empty;
        /// <summary>
        /// The score of customer's left thumb finger, which is taken using FP device. 
        /// </summary>
        public int left_thumb_score { get; set; }
        /// <summary>
        /// Encripted data of customer's left thumb.
        /// </summary>
        public string left_thumb { get; set; } = string.Empty;
        /// <summary>
        /// The score of customer's left index finger, which is taken using FP device. 
        /// </summary>
        public int left_index_score { get; set; }
        /// <summary>
        /// Encripted data of customer's left index finger.
        /// </summary>
        public string left_index { get; set; } = string.Empty;
        /// <summary>
        /// The score of customer's right thumb finger, which is taken using FP device. 
        /// </summary>
        public int right_thumb_score { get; set; }
        /// <summary>
        /// Encripted data of customer's right thumb finger.
        /// </summary>
        public string right_thumb { get; set; } = string.Empty;
        /// <summary>
        /// The score of customer's right index finger, which is taken using FP device. 
        /// </summary>
        public int right_index_score { get; set; }
        /// <summary>
        /// Encripted data of customer's right index finger.
        /// </summary>
        public string right_index { get; set; } = string.Empty;
        /// <summary>
        /// Identification identity type (e.g. NID,Passport)
        /// </summary>
        public int id_type { get; set; }
        /// <summary>
        /// Id type is NID then true else false
        /// </summary>
        public int is_nid { get; set; }
        /// <summary>
        /// Varified token id
        /// </summary>
        public string varified_token_id { get; set; } = string.Empty;
        /// <summary>
        /// SIM category type (e.g. )
        /// </summary>
        public int sim_category { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string expiry_date { get; set; } = string.Empty;     
        /// <summary>
        /// 
        /// </summary>
        public int mnp_process_id { get; set; }
        /// <summary>
        /// if urgent then true else false
        /// </summary>
        public int is_urgent { get; set; }
    }
}
