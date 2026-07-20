using BIA.Entity.CommonEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    /// <summary>
    ///  This class is used for checking MSISDN number 
    /// </summary>
    public class PaiedMSISDNCheckResponse : RACommonResponse
    {
        /// <summary>
        /// SIM card number (i.e. "981809647747") 
        /// </summary>
        public string sim_number { get; set; } = string.Empty;
        /// <summary>
        /// Subscreiption type code (i.e. "")
        /// </summary>
        public string subscription_type_code { get; set; } = string.Empty;
        /// <summary>
        /// imsi number (i.e. "470037108801557") 
        /// </summary>
        public string imsi { get; set; } = string.Empty;
    }

    public class PaiedMSISDNCheckResponseDataRev
    {
        public PaiedMSISDNCheckResponseRev data { get; set; } = new PaiedMSISDNCheckResponseRev();
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class PaiedMSISDNCheckResponseRev
    {
        /// <summary>
        /// SIM card number (i.e. "981809647747") 
        /// </summary>
        public string sim_number { get; set; } = string.Empty;
        /// <summary>
        /// Subscreiption type code (i.e. "")
        /// </summary>
        public string subscription_type_code { get; set; } = string.Empty;
        /// <summary>
        /// imsi number (i.e. "470037108801557") 
        /// </summary>
        public string imsi { get; set; } = string.Empty;
        public string details_message { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;
        public string offer_name { get; set; } = string.Empty;
    }


    #region Cherish Msisdn

    public class CherishMSISDNCheckResponse : RACommonResponse
    {
        public string retailer_code { get; set; } = string.Empty;

        public string number_category { get; set; } = string.Empty; 
    }

    #endregion



    public class UnpairedMSISDNCheckResponse : RACommonResponse
    {
        public int stock_id { get; set; }
        public string retailer_code { get; set; } = string.Empty;

        public string number_category { get; set; } = string.Empty;
    }

    public class UnpairedMSISDNStartrekCheckResponseV2 : UnpairedMSISDNCheckResponse
    {
        public string retailer_code { get; set; }
        public string reservation_id { get; set; }
        public bool isDesiredCategory { get; set; } = false;
        public string category_name { get; set; }
        public string data_message { get; set; }
    }

    public class CherishedMSISDNCheckResponse : UnpairedMSISDNCheckResponse
    {
        public bool isDesiredCategory { get; set; } = false;
        public string category_name { get; set; } = string.Empty;
        public string data_message { get; set; } = string.Empty;

    }
    public class UnpairedMSISDNStartrekCheckResponse : RACommonResponse
    {
        public int stock_id { get; set; }
        public string retailer_code { get; set; } = string.Empty;

        public string number_category { get; set; } = string.Empty;
        public string reservation_id { get; set; } = string.Empty;
    }

    public class UnpairedMSISDNCheckResponseForMNPPortIn : RACommonResponse
    {
        public bool is_controlled { get; set; }//
    }


    public class OldSIMNnumberResponse : RACommonResponse
    {
        public string old_sim_number { get; set; } = string.Empty;
    }

    public class DBSSNotificationResponse : RACommonResponse
    {
        public string msisdn_reservation_id { get; set; } = string.Empty;       
        public bool is_unreservation_needed { get; set; }

        public string bi_token_number { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;

        // Property for order request Newly added         
        public string bss_request_id { get; set; } = string.Empty; //DBSS Bio Request Id.
        public int purpose_number { get; set; }
        public int sim_category { get; set; }
        public string sim_number { get; set; } = string.Empty;
        public string subscription_code { get; set; } = string.Empty;// subscription  type code
        public string package_code { get; set; } = string.Empty;
        public string dest_doc_type_no { get; set; } = string.Empty;     
        public string dest_doc_id { get; set; } = string.Empty; 
        public string dest_dob { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string flat_number { get; set; } = string.Empty;
        public string house_number { get; set; } = string.Empty;
        public string road_number { get; set; } = string.Empty;
        public string village { get; set; } = string.Empty;
        public string division_Name { get; set; } = string.Empty;
        public string district_Name { get; set; } = string.Empty;
        public string thana_Name { get; set; } = string.Empty;
        public string postal_code { get; set; } = string.Empty;
        public string user_id { get; set; } = string.Empty;
        public string port_in_date { get; set; } = string.Empty;
        public string alt_msisdn { get; set; } = string.Empty;
        public int status { get; set; }
        public long error_id { get; set; }
        public string error_description { get; set; } = string.Empty;
        public string create_date { get; set; } = string.Empty;
        public string dest_id_type_exp_time { get; set; } = string.Empty;
        public string confirmation_code { get; set; } = string.Empty;//DBSS Order Confarmation Code.
        public string email { get; set; } = string.Empty;
        public string salesman_code { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
        public string center_or_distributor_code { get; set; } = string.Empty;
        public string sim_replace_reason { get; set; } = string.Empty;
        public int? is_paired { get; set; }
        public int dbss_subscription_id { get; set; }
        public string old_sim_number { get; set; } = string.Empty;
        public int sim_replacement_type { get; set; }
        public int src_sim_category { get; set; }
        public string port_in_confirmation_code { get; set; } = string.Empty;
        public string payment_type { get; set; } = string.Empty;
        public string poc_number { get; set; } = string.Empty;


    }

    public class OrderConformationResponse : RACommonResponse
    {
        public string order_conformation_code { get; set; } = string.Empty;
    }


    /// <summary>
    /// The implementation of absract class named Data S
    /// </summary>
    public class MSISDNCheckResponseData : Data
    {
    }

    public class OrderInfoResponse : RACommonResponse
    {
        public string alt_msisdn { get; set; } = string.Empty;
        public string sim_number { get; set; } = string.Empty;
        public string village { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public int thana_id { get; set; }
        public string thana_name { get; set; } = string.Empty;
        public string road_number { get; set; } = string.Empty;     
        public string flat_number { get; set; } = string.Empty;
        public string district_name { get; set; } = string.Empty;
        public int district_id { get; set; }
        public string customer_name { get; set; } = string.Empty;
        public string division_name { get; set; } = string.Empty;
        public int division_id { get; set; }
        public string house_number { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string postal_code { get; set; } = string.Empty;
        public string subscription_code { get; set; } = string.Empty;
        public string subscription_type_id { get; set; } = string.Empty;
        public string package_code { get; set; } = string.Empty;
        public int package_id { get; set; }
        public int is_urgent { get; set; }
        public string port_in_date { get; set; } = string.Empty;
    }

    public class OrderInfoResponseDataRev
    {
        public OrderInfoResponseRev data { get; set; } = new OrderInfoResponseRev();
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class OrderInfoResponseRev
    {
        public string alt_msisdn { get; set; } = string.Empty;
        public string sim_number { get; set; } = string.Empty;
        public string village { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public int thana_id { get; set; }
        public string thana_name { get; set; } = string.Empty;
        public string road_number { get; set; } = string.Empty;
        public string flat_number { get; set; } = string.Empty;
        public string district_name { get; set; } = string.Empty;
        public int district_id { get; set; }
        public string customer_name { get; set; } = string.Empty;
        public string division_name { get; set; } = string.Empty;
        public int division_id { get; set; }
        public string house_number { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string postal_code { get; set; } = string.Empty;
        public string subscription_code { get; set; } = string.Empty;
        public string subscription_type_id { get; set; } = string.Empty;
        public string package_code { get; set; } = string.Empty;
        public int package_id { get; set; }
        public int is_urgent { get; set; }
        public string port_in_date { get; set; } = string.Empty;
    }

    public class RAPassLenResponse : RACommonResponse
    {
        public int length { get; set; }
    }

    public class RAUserInfoForForgetPWDResponse : RAPassLenResponse
    {
        public string mobile_number { get; set; } = string.Empty;
    }


    public class RAOTPResponse : RACommonResponse
    {
        public string otp { get; set; } = string.Empty;
    }

    public class RAPassLenResponseV2 
    {
        public bool isError { get; set; } 
        public string message { get; set; } = string.Empty;
        public PasswordLenthData data { get; set; } = new PasswordLenthData();
    }
    public class PasswordLenthData
    {
        public int length { get; set; }
    }
}
