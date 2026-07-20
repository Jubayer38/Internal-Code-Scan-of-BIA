using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BIA.Entity.CommonEntity
{
    public class CommonRequestClass
    {

    }
    public class LoginRequestModel
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string? Lan { get; set; } = "en";

        [Required]
        public int VersionCode { get; set; } = 0;

        [Required]
        public string VersionName { get; set; } = string.Empty;

        public int? Type { get; set; } = 0;

        public string? OSVersion { get; set; } = "";

        public string? KernelVersion { get; set; } = "";

        public string? FermwareVersion { get; set; }

        public decimal? latitude { get; set; } = 0;

        public decimal? longitude { get; set; } = 0;

        public int? lac { get; set; } = 0;

        public int? cid { get; set; } = 0;

        public string? BPMSISDN { get; set; } = "";

        public string? DeviceModel { get; set; } = "";

        [CustomValidation(allowString: true, allowInt: true)]
        public object? DeviceId { get; set; }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class CustomValidationAttribute : Attribute
    {
        public bool AllowString { get; }
        public bool AllowInt { get; }

        public CustomValidationAttribute(bool allowString = true, bool allowInt = true)
        {
            AllowString = allowString;
            AllowInt = allowInt;
        }
    }
    public class DBSSLoginRequestModel 
    {
     
    }
    public class RetailerAppLoginRequestModel
    {

    }
    public class SimCategoryMigrationRequestModel
    {
        public string customer_name { get; set; } = string.Empty;
        public string flat_number { get; set; } = string.Empty;
        public string road_number { get; set; } = string.Empty;
        public string village { get; set; } = string.Empty;
        public string postal_code { get; set; } = string.Empty;
        public string alt_msisdn { get; set; } = string.Empty;
        public string house_number { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string division_name { get; set; } = string.Empty;
        public int division_id { get; set; }
        public string district_name { get; set; } = string.Empty;
        public int district_id { get; set; }
        public string thana_name { get; set; } = string.Empty;
        public int thana_id { get; set; }
        public string gender { get; set; } = string.Empty;
        public string? package_id { get; set; }
        public bool saf_status { get; set; }
        public string package_code { get; set; } = string.Empty;
        public int right_id { get; set; }
        public string? subscription_type_id { get; set; }
        public string? subscription_code { get; set; }
        public int sim_category { get; set; }
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public int dest_doc_type_no { get; set; } = 0;
        public string dest_doc_id { get; set; } = string.Empty;
        public int? dbss_subscription_id { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string? customer_id { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public string? dest_left_thumb { get; set; }
        public int dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; }
        public int dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int dest_right_index_score { get; set; }
        public string scanner_id { get; set; } = string.Empty;
    }
    public class StarTrekMNPSubmitRequestModel
    {
        public string purpose_number { get; set; } = string.Empty;
        public int is_paired { get; set; }
        public int sim_category { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public long bi_token_number { get; set; }
        public string sim_number { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string? flat_number { get; set; }
        public string? house_number { get; set; }
        public string? road_number { get; set; }
        public string village { get; set; } = string.Empty;
        public int? division_id { get; set; }
        public int? district_id { get; set; }
        public int? thana_id { get; set; }
        public int? postal_code { get; set; }
        public string? alt_msisdn { get; set; }
        public string? email { get; set; }
        public string? subscription_type_id { get; set; }
        public string? subscription_code { get; set; }
        public string? package_id { get; set; }
        public string? package_code { get; set; }
        public string? division_name { get; set; }
        public string? district_name { get; set; }
        public string? thana_name { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public int right_id { get; set; }
        public string? dest_left_thumb { get; set; }
        public int? dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int? dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; }
        public int? dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int? dest_right_index_score { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public string salesman_code { get; set; } = string.Empty;
        public int isBPUser { get; set; }
        public string distributor_code { get; set; } = string.Empty;
        public string center_code { get; set; } = string.Empty; 
    }
    public class IndividualTOSRequestModel
    { 
        public string session_token { get; set; } = string.Empty;
        public long bi_token_number { get; set; }
        public string purpose_number { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string? src_nid { get; set; }
        public string? src_dob { get; set; }
        public string? old_sim_number { get; set; }
        public int? dbss_subscription_id { get; set; }
        public string src_owner_customer_id { get; set; } = string.Empty;
        public string src_user_customer_id { get; set; } = string.Empty;
        public string src_payer_customer_id { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public int? division_id { get; set; }
        public string? division_name { get; set; }
        public int? district_id { get; set; }
        public string? district_name { get; set; }
        public int? thana_id { get; set; }
        public string? thana_name { get; set; }
        public string village { get; set; } = string.Empty;
        public int? postal_code { get; set; }
        public string? road_number { get; set; }
        public string? house_number { get; set; }
        public string? flat_number { get; set; }
        public string? email { get; set; }
        public string? alt_msisdn { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
        public string right_id { get; set; } = string.Empty;
        public int src_sim_category { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public int isBPUser { get; set; }
        public string dest_left_thumb { get; set; } = string.Empty;
        public int dest_left_thumb_score { get; set; }
        public string dest_left_index { get; set; } = string.Empty;
        public int dest_left_index_score { get; set; }
        public string dest_right_thumb { get; set; } = string.Empty;
        public int dest_right_thumb_score { get; set; }
        public string dest_right_index { get; set; } = string.Empty;
        public int dest_right_index_score { get; set; }
        public string src_left_thumb { get; set; } = string.Empty;
        public int src_left_thumb_score { get; set; }
        public string src_left_index { get; set; } = string.Empty;
        public int src_left_index_score { get; set; }
        public string src_right_thumb { get; set; } = string.Empty;
        public int src_right_thumb_score { get; set; }
        public string src_right_index { get; set; } = string.Empty;
        public int src_right_index_score { get; set; }
    }

    public class HomeWifiTOSRequestModel
    {
        public string session_token { get; set; } = string.Empty;
        public long bi_token_number { get; set; }
        public string purpose_number { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string? src_nid { get; set; }
        public string? src_dob { get; set; }
        public string? old_sim_number { get; set; }
        public int? dbss_subscription_id { get; set; }
        public string src_owner_customer_id { get; set; } = string.Empty;
        public string src_user_customer_id { get; set; } = string.Empty;
        public string src_payer_customer_id { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public int? division_id { get; set; }
        public string? division_name { get; set; }
        public int? district_id { get; set; }
        public string? district_name { get; set; }
        public int? thana_id { get; set; }
        public string? thana_name { get; set; }
        public string village { get; set; } = string.Empty;
        public int? postal_code { get; set; }
        public string? road_number { get; set; }
        public string? house_number { get; set; }
        public string? flat_number { get; set; }
        public string? email { get; set; }
        public string? alt_msisdn { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
        public string right_id { get; set; } = string.Empty;
        public int src_sim_category { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public int isBPUser { get; set; }
        public string dest_left_thumb { get; set; } = string.Empty;
        public int dest_left_thumb_score { get; set; }
        public string dest_left_index { get; set; } = string.Empty;
        public int dest_left_index_score { get; set; }
        public string dest_right_thumb { get; set; } = string.Empty;
        public int dest_right_thumb_score { get; set; }
        public string dest_right_index { get; set; } = string.Empty;
        public int dest_right_index_score { get; set; }
        public string src_left_thumb { get; set; } = string.Empty;
        public int src_left_thumb_score { get; set; }
        public string src_left_index { get; set; } = string.Empty;
        public int src_left_index_score { get; set; }
        public string src_right_thumb { get; set; } = string.Empty;
        public int src_right_thumb_score { get; set; }
        public string src_right_index { get; set; } = string.Empty;
        public int src_right_index_score { get; set; }
        public string order_number { get; set; } = string.Empty;
        public string initiator_channel { get; set; } = string.Empty;
        public string order_type { get; set; } = string.Empty;
        public string subscription_type { get; set; } = string.Empty;
        public string simkit_type { get; set; } = string.Empty;
    }
    public class StarTrekSimReplacementRequestModel
    {
        public string purpose_number { get; set; } = string.Empty;
        public string sim_number { get; set; } = string.Empty;
        public bool saf_status { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; }= string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string? flat_number { get; set; }
        public string? house_number { get; set; }
        public string? road_number { get; set; }
        public string village { get; set; } = string.Empty;
        public int? division_id { get; set; }
        public int? district_id { get; set; }
        public int? thana_id { get; set; }
        public string? postal_code { get; set; }
        public string? alt_msisdn { get; set; }
        public string? email { get; set; }
        public string sim_rep_reason_id { get; set; } = string.Empty;
        public string payment_type { get; set; } = string.Empty;
        public string? division_name { get; set; }
        public string? district_name { get; set; }
        public string? thana_name { get; set; }
        public string sim_replc_reason { get; set; } = string.Empty;
        public string dbss_subscription_id { get; set; } = string.Empty;
        public string old_sim_number { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
        public string customer_id { get; set; } = string.Empty;
        public string right_id { get; set; } = string.Empty;
        public string center_code { get; set; } = string.Empty;
        public string distributor_code { get; set; } = string.Empty;
        public string? dest_left_thumb { get; set; }
        public int dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; }
        public int dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int dest_right_index_score { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public int isBPUser { get; set; }
    }
    public class TwoPartyECVerificationRequestModel
    {
        public int purpose_number { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
        public int right_id { get; set; }
        public string? src_nid { get; set; }
        public string? src_dob { get; set; }
        public string otp { get; set; } = string.Empty;
        public string? poc_msisdn_number { get; set; }
        public int src_ec_verifi_reqrd { get; set; }
        public string? src_left_thumb { get; set; }
        public int src_left_thumb_score { get; set; }
        public string? src_left_index { get; set; }
        public int src_left_index_score { get; set; }
        public string? src_right_thumb { get; set; }
        public int src_right_thumb_score { get; set; }
        public string? src_right_index { get; set; }
        public int src_right_index_score { get; set; }
        public string? dest_left_thumb { get; set; }
        public int dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; }
        public int dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int dest_right_index_score { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
    }
    public class StarTrekNewConnectionRequestModel
    {
        public string selected_category { get; set; } = string.Empty;
        public string purpose_number { get; set; } = string.Empty;
        public int is_paired { get; set; }
        public int sim_category { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public long bi_token_number { get; set; }
        public string sim_number { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string? flat_number { get; set; }
        public string? house_number { get; set; }
        public string? road_number { get; set; }
        public string village { get; set; } = string.Empty;
        public int? division_id { get; set; }
        public int? district_id { get; set; }
        public int? thana_id { get; set; }
        public int? postal_code { get; set; }
        public string? alt_msisdn { get; set; }
        public string? email { get; set; }
        public string? subscription_type_id { get; set; }
        public string? subscription_code { get; set; }
        public string? package_id { get; set; }
        public string? package_code { get; set; }
        public string? division_name { get; set; }
        public string? district_name { get; set; }
        public string? thana_name { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public int right_id { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public int isBPUser { get; set; }
        public string? dest_left_thumb { get; set; }
        public int? dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int? dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; }
        public int? dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int? dest_right_index_score { get; set; }
        public string order_id { get; set; } = string.Empty;
    }
    public class SimReplacementRequestModel
    { 
        public string purpose_number { get; set; } = string.Empty;
        public string sim_number { get; set; } = string.Empty;
        public bool saf_status { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string? flat_number { get; set; }
        public string? house_number { get; set; }
        public string? road_number { get; set; }
        public string village { get; set; } = string.Empty;
        public int? division_id { get; set; }
        public int? district_id { get; set; }
        public int? thana_id { get; set; }
        public string? postal_code { get; set; }
        public string? alt_msisdn { get; set; }
        public string? email { get; set; }
        public string sim_rep_reason_id { get; set; } = string.Empty;
        public string payment_type { get; set; } = string.Empty;
        public string? division_name { get; set; }
        public string? district_name { get; set; }
        public string? thana_name { get; set; }
        public string sim_replc_reason { get; set; } = string.Empty;
        public string dbss_subscription_id { get; set; } = string.Empty;
        public string old_sim_number { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
        public string customer_id { get; set; } = string.Empty;
        public string right_id { get; set; } = string.Empty;
        public string center_code { get; set; } = string.Empty;
        public string distributor_code { get; set; } = string.Empty;
        public string? dest_left_thumb { get; set; }
        public int dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; }
        public int dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int dest_right_index_score { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public int isBPUser { get; set; }
    }

    public class HomeWifiSimReplacementRequestModel
    {
        public string purpose_number { get; set; } = string.Empty;
        public string sim_number { get; set; } = string.Empty;
        public bool saf_status { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string? flat_number { get; set; }
        public string? house_number { get; set; }
        public string? road_number { get; set; }
        public string village { get; set; } = string.Empty;
        public int? division_id { get; set; }
        public int? district_id { get; set; }
        public int? thana_id { get; set; }
        public string? postal_code { get; set; }
        public string? alt_msisdn { get; set; }
        public string? email { get; set; }
        public string sim_rep_reason_id { get; set; } = string.Empty;
        public string payment_type { get; set; } = string.Empty;
        public string? division_name { get; set; }
        public string? district_name { get; set; }
        public string? thana_name { get; set; }
        public string sim_replc_reason { get; set; } = string.Empty;
        public string dbss_subscription_id { get; set; } = string.Empty;
        public string old_sim_number { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
        public string customer_id { get; set; } = string.Empty;
        public string right_id { get; set; } = string.Empty;
        public string center_code { get; set; } = string.Empty;
        public string distributor_code { get; set; } = string.Empty;
        public string? dest_left_thumb { get; set; }
        public int dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; }
        public int dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int dest_right_index_score { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public int isBPUser { get; set; }
        public string order_number { get; set; } = string.Empty;
        public string initiator_channel { get; set; } = string.Empty;
        public string order_type { get; set; } = string.Empty;
        public string subscription_type { get; set; } = string.Empty;
        public string simkit_type { get; set; } = string.Empty;
    }
    public class CorpSimReplacementRequestModel
    {
        public string session_token { get; set; } = string.Empty;
        public string retailer_id { get; set; } = string.Empty;
        public int bi_token_number { get; set; }
        public string purpose_number { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string? poc_msisdn_number { get; set; }
        public string sim_number { get; set; } = string.Empty;
        public string? old_sim_number { get; set; }
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string src_dob { get; set; } = string.Empty;
        public string src_nid { get; set; } = string.Empty;
        public int sim_replacement_type { get; set; }
        public int? dbss_subscription_id { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public int right_id { get; set; }
        public string? dest_left_thumb { get; set; }
        public int dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; }
        public int dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int dest_right_index_score { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public int isBPUser { get; set; }
        public string payment_type { get; set; } = string.Empty;
        public string sim_rep_reason_id { get; set; } = string.Empty;
    }
    public class NewConnectionRequestModel
    {
        public string purpose_number { get; set; } = string.Empty;
        public string dest_imsi { get; set; } = string.Empty;
        public int is_paired { get; set; }
        public int sim_category { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public long bi_token_number { get; set; }
        public string sim_number { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string? flat_number { get; set; }
        public string? house_number { get; set; }
        public string? road_number { get; set; }
        public string village { get; set; } = string.Empty;
        public int? division_id { get; set; }
        public int? district_id { get; set; }
        public int? thana_id { get; set; }
        public int? postal_code { get; set; }
        public string? alt_msisdn { get; set; }
        public string? email { get; set; }
        public string? subscription_type_id { get; set; }
        public string? subscription_code { get; set; }
        public string? package_id { get; set; }
        public string? package_code { get; set; }
        public string? division_name { get; set; }
        public string? district_name { get; set; }
        public string? thana_name { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public int right_id { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public int isBPUser { get; set; }
        public string? dest_left_thumb { get; set; }
        public int? dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int? dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; } 
        public int? dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int? dest_right_index_score { get; set; }
        public bool is_lus { get; set; }
        public string bts_code { get; set; } = string.Empty;
        public string selected_category { get; set; } = string.Empty;
    }

    public class HomeWifiNewConnectionRequestModel
    {
        public string purpose_number { get; set; } = string.Empty;
        public string dest_imsi { get; set; } = string.Empty;
        public int is_paired { get; set; }
        public int sim_category { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public long bi_token_number { get; set; }
        public string sim_number { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string? flat_number { get; set; }
        public string? house_number { get; set; }
        public string? road_number { get; set; }
        public string village { get; set; } = string.Empty;
        public int? division_id { get; set; }
        public int? district_id { get; set; }
        public int? thana_id { get; set; }
        public int? postal_code { get; set; }
        public string? alt_msisdn { get; set; }
        public string? email { get; set; }
        public string? subscription_type_id { get; set; }
        public string? subscription_code { get; set; }
        public string? package_id { get; set; }
        public string? package_code { get; set; }
        public string? division_name { get; set; }
        public string? district_name { get; set; }
        public string? thana_name { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public int right_id { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public int isBPUser { get; set; }
        public string? dest_left_thumb { get; set; }
        public int? dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int? dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; }
        public int? dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int? dest_right_index_score { get; set; }
        public bool is_lus { get; set; }
        public string bts_code { get; set; } = string.Empty;
        public string selected_category { get; set; } = string.Empty;
        public string order_number { get; set; } = string.Empty;
        public string initiator_channel { get; set; } = string.Empty;
        public string order_type { get; set; } = string.Empty;   
        public string subscription_type { get; set; } = string.Empty;
        public string simkit_type { get; set; } = string.Empty;
    }

    public class CherishNewConnectionRequestModel
    {
        public string purpose_number { get; set; } = string.Empty;
        public string dest_imsi { get; set; } = string.Empty;
        public int is_paired { get; set; }
        public int sim_category { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public long bi_token_number { get; set; }
        public string sim_number { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string? flat_number { get; set; }
        public string? house_number { get; set; }
        public string? road_number { get; set; }
        public string village { get; set; } = string.Empty;
        public int? division_id { get; set; }
        public int? district_id { get; set; }
        public int? thana_id { get; set; }
        public int? postal_code { get; set; }
        public string? alt_msisdn { get; set; }
        public string? email { get; set; }
        public string? subscription_type_id { get; set; }
        public string? subscription_code { get; set; }
        public string? package_id { get; set; }
        public string? package_code { get; set; }
        public string? division_name { get; set; }
        public string? district_name { get; set; }
        public string? thana_name { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public int right_id { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public int isBPUser { get; set; }
        public string? dest_left_thumb { get; set; }
        public int? dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int? dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; }
        public int? dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int? dest_right_index_score { get; set; }
        public string Selected_category { get; set; } = string.Empty;
        public bool is_lus { get; set; }
        public string bts_code { get; set; } = string.Empty;
    }
    public class FPRegistrationEcVerificationRequestModel
    {
        public string channel_name { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string dest_left_index { get; set; } = string.Empty;
        public int dest_left_index_score { get; set; }
        public string dest_left_thumb { get; set; } = string.Empty;
        public int dest_left_thumb_score { get; set; }
        public string dest_nid { get; set; } = string.Empty;
        public string dest_right_index { get; set; } = string.Empty;
        public int dest_right_index_score { get; set; }
        public string dest_right_thumb { get; set; } = string.Empty;
        public int dest_right_thumb_score { get; set; }
        public string msisdn { get; set; } = string.Empty;
        public string purpose_number { get; set; } = string.Empty;
        public string retailer_id { get; set; } = string.Empty;
        public int right_id { get; set; }
        public string session_token { get; set; } = string.Empty;
    }
    public class POCEcVerificationRequestModel
    {
        public string session_token { get; set; } = string.Empty;
        public string retailer_id { get; set; } = string.Empty;
        public int purpose_number { get; set; }
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
        public int right_id { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public int isBPUser { get; set; }
        public string dest_left_thumb { get; set; } = string.Empty;
        public int dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int dest_left_index_score { get; set; }
        public string dest_right_thumb { get; set; } = string.Empty;
        public int dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int dest_right_index_score { get; set; }
    }
    public class MNPSubmitRequestModel
    {
        public string purpose_number { get; set; } = string.Empty;
        public int is_paired { get; set; }
        public int sim_category { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public long bi_token_number { get; set; }
        public string sim_number { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string? flat_number { get; set; }
        public string? house_number { get; set; }
        public string? road_number { get; set; }
        public string village { get; set; } = string.Empty;
        public int? division_id { get; set; }
        public int? district_id { get; set; }
        public int? thana_id { get; set; }
        public int? postal_code { get; set; }
        public string? alt_msisdn { get; set; }
        public string? email { get; set; }
        public string? subscription_type_id { get; set; }
        public string? subscription_code { get; set; }
        public string? package_id { get; set; }
        public string? package_code { get; set; }
        public string? division_name { get; set; }
        public string? district_name { get; set; }
        public string? thana_name { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public int right_id { get; set; }
        public string? dest_left_thumb { get; set; }
        public int? dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int? dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; }
        public int? dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int? dest_right_index_score { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public string salesman_code { get; set; } = string.Empty;
        public int isBPUser { get; set; }
        public string distributor_code { get; set; } = string.Empty;
        public string center_code { get; set; } = string.Empty;
    }
    public class HomeWifiMNPSubmitRequestModel
    {
        public string purpose_number { get; set; } = string.Empty;
        public int is_paired { get; set; }
        public int sim_category { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public long bi_token_number { get; set; }
        public string sim_number { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string? flat_number { get; set; }
        public string? house_number { get; set; }
        public string? road_number { get; set; }
        public string village { get; set; } = string.Empty;
        public int? division_id { get; set; }
        public int? district_id { get; set; }
        public int? thana_id { get; set; }
        public int? postal_code { get; set; }
        public string? alt_msisdn { get; set; }
        public string? email { get; set; }
        public string? subscription_type_id { get; set; }
        public string? subscription_code { get; set; }
        public string? package_id { get; set; }
        public string? package_code { get; set; }
        public string? division_name { get; set; }
        public string? district_name { get; set; }
        public string? thana_name { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public int right_id { get; set; }
        public string? dest_left_thumb { get; set; }
        public int? dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int? dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; }
        public int? dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int? dest_right_index_score { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public string salesman_code { get; set; } = string.Empty;
        public int isBPUser { get; set; }
        public string distributor_code { get; set; } = string.Empty;
        public string center_code { get; set; } = string.Empty;
        public string order_number { get; set; } = string.Empty;
        public string initiator_channel { get; set; } = string.Empty;
        public string order_type { get; set; } = string.Empty;
        public string subscription_type { get; set; } = string.Empty;
        public string simkit_type { get; set; } = string.Empty;
    }
    public class DMSRetailerSyncRequestModel
    {
        [Required]
        public string userName { get; set; } = string.Empty;
        [Required]
        public string password { get; set; } = string.Empty;
        [Required]
        public string retailerCode { get; set; } = string.Empty;
        public string iTopUpNumber { get; set; } = string.Empty;
        [Required]
        public int isActive { get; set; }
        public string typeName { get; set; } = string.Empty;
    }
    public class DBSSToAppNotificationRequestModel : IValidatableObject
    {
        [Required]
        public string session_token { get; set; } = string.Empty;
        [Required]
        public string bio_request_id { get; set; } = string.Empty;//in DB it is named as BSS_REQUEST_ID.
        [Required, Range(0, 1, ErrorMessage = "Only 0 or 1 is acceptable. 1 for success and 0 for failure.")]
        public int? is_Success { get; set; }
        public string? error_code { get; set; } = string.Empty;
        public string? description { get; set; } = string.Empty;
        public string? error_source { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (is_Success == 0
                && String.IsNullOrEmpty(description))
                yield return new ValidationResult("'description' field is required when the result of 'is_Success' is 0.");
        }
    }
    public class FirstRechargeRequestModel
    {
        public string session_token { get; set; } = string.Empty;
        public string retailerCode { get; set; } = string.Empty;
        public string subscriberNo { get; set; } = string.Empty;
        public string amount { get; set; } = string.Empty;
        public string userPin { get; set; } = string.Empty;
        public string deviceId { get; set; } = string.Empty;
        public int? paymentType { get; set; }
        public double? lat { get; set; }
        public double? lng { get; set; }
        public string? lan { get; set; }
        public string? userId { get; set; } = "0";
        public string bi_token_number { get; set; } = string.Empty;
    }
    public class FirstRechargeAmountRequestModel
    { 
        public string session_token { get; set; } = string.Empty;
        public string retailer_code { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
    }
    public class BioCancelRequestModel
    {
        public string purpose_number { get; set; } = string.Empty;
        public int is_paired { get; set; }
        public int sim_category { get; set; }
        public string retailer_id { get; set; } = string.Empty;
        public long bi_token_number { get; set; }
        public string sim_number { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string? flat_number { get; set; }
        public string? house_number { get; set; }
        public string? road_number { get; set; }
        public string village { get; set; } = string.Empty;
        public int? division_id { get; set; }
        public int? district_id { get; set; }
        public int? thana_id { get; set; }
        public int? postal_code { get; set; }
        public string? alt_msisdn { get; set; }
        public string? email { get; set; }
        public string? subscription_type_id { get; set; }
        public string? subscription_code { get; set; }
        public string? package_id { get; set; }
        public string? package_code { get; set; }
        public string? division_name { get; set; }
        public string? district_name { get; set; }
        public string? thana_name { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public int right_id { get; set; }
        public int lac { get; set; }
        public int cid { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string scanner_id { get; set; } = string.Empty;
        public int isBPUser { get; set; }
        public string? dest_left_thumb { get; set; }
        public int? dest_left_thumb_score { get; set; }
        public string? dest_left_index { get; set; }
        public int? dest_left_index_score { get; set; }
        public string? dest_right_thumb { get; set; }
        public int? dest_right_thumb_score { get; set; }
        public string? dest_right_index { get; set; }
        public int? dest_right_index_score { get; set; }
    }
    public class FPRegistrationRequestModel
    {
        public string user_name { get; set; } = string.Empty;
        public string left_thumb { get; set; } = string.Empty;
        public int left_thumb_score { get; set; }
        public string left_index { get; set; } = string.Empty;
        public int left_index_score { get; set; }
        public string right_thumb { get; set; } = string.Empty;
        public int right_thumb_score { get; set; }
        public string right_index { get; set; } = string.Empty;
        public int right_index_score { get; set; }
        public string mobile_no { get; set; } = string.Empty;
        public string session_token { get; set; } = string.Empty;
    }
    public class FailedResubmitRequestModel
    {
        [Required]
        public string session_token { get; set; } = string.Empty;
        public int? right_id { get; set; }
        public string bi_token_number { get; set; } = string.Empty;
        public string? retailer_id { get; set; }
        public string? distributor_code { get; set; }
        public int isBPUser { get; set; }
        public decimal? latitude { get; set; }
        public decimal? longitude { get; set; }
    }
}
