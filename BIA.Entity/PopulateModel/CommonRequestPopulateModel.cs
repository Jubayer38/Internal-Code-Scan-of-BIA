using BIA.Entity.CommonEntity;
using BIA.Entity.RequestEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.PopulateModel
{
    public class CommonRequestPopulateModel
    {
        public LoginRequestsV2 LoginRequestPopulateModel(LoginRequestModel model)
        {
            LoginRequestsV2 loginRequests = new LoginRequestsV2();
            if(model != null)
            {
                loginRequests.UserName = model.UserName;
                loginRequests.Password = model.Password;
                loginRequests.Lan = model.Lan;
                loginRequests.VersionCode = model.VersionCode;
                loginRequests.VersionName = model.VersionName;
                loginRequests.Type = model.Type;
                loginRequests.OSVersion = model.OSVersion;
                loginRequests.KernelVersion = model.KernelVersion;
                loginRequests.FermwareVersion = model.FermwareVersion;
                loginRequests.latitude = model.latitude;
                loginRequests.longitude = model.longitude;
                loginRequests.lac = model.lac;
                loginRequests.cid = model.cid;
                loginRequests.BPMSISDN = model.BPMSISDN;
                loginRequests.DeviceModel = model.DeviceModel;
                loginRequests.DeviceId = model.DeviceId;
            }
            
            return loginRequests;
        }
        public RAOrderRequestV2 SimCategoryMigrationRequestPopulateModel(SimCategoryMigrationRequestModel model)
        {
            RAOrderRequestV2 rAOrder = new RAOrderRequestV2();

            if(model != null)
            {
                rAOrder.customer_name = model.customer_name;
                rAOrder.flat_number = model.flat_number;
                rAOrder.road_number = model.road_number;
                rAOrder.village = model.village;
                rAOrder.postal_code = model.postal_code;
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.house_number = model.house_number;
                rAOrder.email = model.email;
                rAOrder.division_name = model.division_name;
                rAOrder.division_id = model.division_id;
                rAOrder.district_name = model.district_name;
                rAOrder.district_id = model.district_id;
                rAOrder.thana_name = model.thana_name;
                rAOrder.thana_id = model.thana_id;
                rAOrder.gender = model.gender;
                rAOrder.package_id = model.package_id;
                rAOrder.saf_status = model.saf_status;
                rAOrder.package_code = model.package_code;
                rAOrder.right_id = model.right_id;
                rAOrder.subscription_type_id = model.subscription_type_id;
                rAOrder.subscription_code = model.subscription_code;
                rAOrder.sim_category = model.sim_category;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.session_token = model.session_token;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_doc_type_no = model.dest_doc_type_no;
                rAOrder.dbss_subscription_id = model.dbss_subscription_id;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.customer_id = model.customer_id;
                rAOrder.channel_name = model.channel_name;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.scanner_id = model.scanner_id;
            }
            
            return rAOrder;
        }
        public RAOrderRequestV2 StarTrekMNPSubmitRequestPopulateModel(StarTrekMNPSubmitRequestModel model)
        {
            RAOrderRequestV2 rAOrder = new RAOrderRequestV2();

            if(model != null)
            {
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.is_paired = model.is_paired;
                rAOrder.sim_category = model.sim_category;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.bi_token_number = model.bi_token_number;
                rAOrder.sim_number = model.sim_number;
                rAOrder.session_token = model.session_token;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.customer_name = model.customer_name;
                rAOrder.gender = model.gender;
                rAOrder.flat_number = model.flat_number;
                rAOrder.house_number = model.house_number;
                rAOrder.road_number = model.road_number;
                rAOrder.village = model.village;
                rAOrder.division_id = model.division_id;
                rAOrder.district_id = model.district_id;
                rAOrder.thana_id = model.thana_id;
                rAOrder.postal_code = Convert.ToString(model.postal_code);
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.email = model.email;
                rAOrder.subscription_type_id = model.subscription_type_id;
                rAOrder.subscription_code = model.subscription_code;
                rAOrder.package_id = model.package_id;
                rAOrder.package_code = model.package_code;
                rAOrder.division_name = model.division_name;
                rAOrder.district_name = model.district_name;
                rAOrder.thana_name = model.thana_name;
                rAOrder.channel_name = model.channel_name;
                rAOrder.right_id = model.right_id;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
                rAOrder.distributor_code = model.distributor_code;
                rAOrder.center_code = model.center_code;
            }            

            return rAOrder;
        }
        public RAOrderRequestV2 IndividualTOSRequestPopulateModel(IndividualTOSRequestModel model)
        {
            RAOrderRequestV2 rAOrder = new RAOrderRequestV2();

            if(model != null)
            {
                rAOrder.session_token = model.session_token;
                rAOrder.bi_token_number = model.bi_token_number;
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.src_nid = model.src_nid;
                rAOrder.src_dob = model.src_dob;
                rAOrder.old_sim_number = model.old_sim_number;
                rAOrder.dbss_subscription_id = model.dbss_subscription_id;
                rAOrder.src_owner_customer_id = model.src_owner_customer_id;
                rAOrder.src_user_customer_id = model.src_user_customer_id;
                rAOrder.src_payer_customer_id = model.src_payer_customer_id;
                rAOrder.customer_name = model.customer_name;
                rAOrder.gender = model.gender;
                rAOrder.division_id = model.division_id;
                rAOrder.division_name = model.division_name;
                rAOrder.district_id = model.district_id;
                rAOrder.district_name = model.district_name;
                rAOrder.thana_id = model.thana_id;
                rAOrder.thana_name = model.thana_name;
                rAOrder.village = model.village;
                rAOrder.postal_code = Convert.ToString(model.postal_code);
                rAOrder.road_number = model.road_number;
                rAOrder.house_number = model.house_number;
                rAOrder.flat_number = model.flat_number;
                rAOrder.email = model.email;
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.channel_name = model.channel_name;
                try { rAOrder.right_id = Convert.ToInt32(model.right_id); } catch (Exception) { }
                rAOrder.src_sim_category = model.src_sim_category;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.src_left_thumb = model.src_left_thumb;
                rAOrder.src_left_thumb_score = model.src_left_thumb_score;
                rAOrder.src_left_index = model.src_left_index;
                rAOrder.src_left_index_score = model.src_left_index_score;
                rAOrder.src_right_thumb = model.src_right_thumb;
                rAOrder.src_right_thumb_score = model.src_right_thumb_score;
                rAOrder.src_right_index = model.src_right_index;
                rAOrder.src_right_index_score = model.src_right_index_score;
            }
            
            return rAOrder;
        }
        public HomeWifiOrderRequest HomeWifiTOSRequestPopulateModel(HomeWifiTOSRequestModel model)
        {
            HomeWifiOrderRequest rAOrder = new HomeWifiOrderRequest();

            if(model != null)
            {
                rAOrder.session_token = model.session_token;
                rAOrder.bi_token_number = model.bi_token_number;
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.src_nid = model.src_nid;
                rAOrder.src_dob = model.src_dob;
                rAOrder.old_sim_number = model.old_sim_number;
                rAOrder.dbss_subscription_id = model.dbss_subscription_id;
                rAOrder.src_owner_customer_id = model.src_owner_customer_id;
                rAOrder.src_user_customer_id = model.src_user_customer_id;
                rAOrder.src_payer_customer_id = model.src_payer_customer_id;
                rAOrder.customer_name = model.customer_name;
                rAOrder.gender = model.gender;
                rAOrder.division_id = model.division_id;
                rAOrder.division_name = model.division_name;
                rAOrder.district_id = model.district_id;
                rAOrder.district_name = model.district_name;
                rAOrder.thana_id = model.thana_id;
                rAOrder.thana_name = model.thana_name;
                rAOrder.village = model.village;
                rAOrder.postal_code = Convert.ToString(model.postal_code);
                rAOrder.road_number = model.road_number;
                rAOrder.house_number = model.house_number;
                rAOrder.flat_number = model.flat_number;
                rAOrder.email = model.email;
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.channel_name = model.channel_name;
                try { rAOrder.right_id = Convert.ToInt32(model.right_id); } catch (Exception) { }
                rAOrder.src_sim_category = model.src_sim_category;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.src_left_thumb = model.src_left_thumb;
                rAOrder.src_left_thumb_score = model.src_left_thumb_score;
                rAOrder.src_left_index = model.src_left_index;
                rAOrder.src_left_index_score = model.src_left_index_score;
                rAOrder.src_right_thumb = model.src_right_thumb;
                rAOrder.src_right_thumb_score = model.src_right_thumb_score;
                rAOrder.src_right_index = model.src_right_index;
                rAOrder.src_right_index_score = model.src_right_index_score;
                rAOrder.order_number = model.order_number;
                rAOrder.initiator_channel = model.initiator_channel;
                rAOrder.order_type = model.order_type;
                rAOrder.subscription_type = model.subscription_type;
                rAOrder.simkit_type = model.simkit_type;
            }
            
            return rAOrder;
        }
        public RAOrderRequestV2 StarTrekSIMReplacementRequestPopulateModel(StarTrekSimReplacementRequestModel model)
        {
            RAOrderRequestV2 rAOrder = new RAOrderRequestV2();

            if(model != null)
            {
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.sim_number = model.sim_number;
                rAOrder.saf_status = model.saf_status;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.session_token = model.session_token;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.customer_name = model.customer_name;
                rAOrder.gender = model.gender;
                rAOrder.flat_number = model.flat_number;
                rAOrder.house_number = model.house_number;
                rAOrder.road_number = model.road_number;
                rAOrder.village = model.village;
                rAOrder.division_id = model.division_id;
                rAOrder.district_id = model.district_id;
                rAOrder.thana_id = model.thana_id;
                rAOrder.postal_code = model.postal_code;
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.email = model.email;
                rAOrder.sim_rep_reason_id = model.sim_rep_reason_id;
                rAOrder.payment_type = model.payment_type;
                rAOrder.division_name = model.division_name;
                rAOrder.district_name = model.district_name;
                rAOrder.thana_name = model.thana_name;
                rAOrder.sim_replc_reason = model.sim_replc_reason;
                if (!String.IsNullOrEmpty(model.dbss_subscription_id))
                {
                    rAOrder.dbss_subscription_id = Convert.ToInt32(model.dbss_subscription_id);
                }
                rAOrder.channel_name = model.channel_name;
                rAOrder.customer_id = model.customer_id;
                if (!String.IsNullOrEmpty(model.right_id))
                {
                    rAOrder.right_id = Convert.ToInt32(model.right_id);
                }
                rAOrder.center_code = model.center_code;
                rAOrder.distributor_code = model.distributor_code;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
            }
            
            return rAOrder;
        }
        public RAOrderRequestV2 TowPartyECVerificationRequestPopulateModel(TwoPartyECVerificationRequestModel model)
        {
            RAOrderRequestV2 rAOrder = new RAOrderRequestV2();

            if(model != null)
            {
                rAOrder.purpose_number = model.purpose_number.ToString();
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.session_token = model.session_token;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.channel_name = model.channel_name;
                rAOrder.right_id = model.right_id;
                rAOrder.src_nid = model.src_nid;
                rAOrder.src_dob = model.src_dob;
                rAOrder.otp = model.otp;
                rAOrder.poc_msisdn_number = model.poc_msisdn_number;
                rAOrder.src_ec_verifi_reqrd = model.src_ec_verifi_reqrd;
                rAOrder.src_left_thumb = model.src_left_thumb;
                rAOrder.src_left_thumb_score = model.src_left_thumb_score;
                rAOrder.src_left_index = model.src_left_index;
                rAOrder.src_left_index_score = model.src_left_index_score;
                rAOrder.src_right_thumb = model.src_right_thumb;
                rAOrder.src_right_thumb_score = model.src_right_thumb_score;
                rAOrder.src_right_index = model.src_right_index;
                rAOrder.src_right_index_score = model.src_right_index_score;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
            }            

            return rAOrder;
        }
        public RAOrderRequestV2 StarTrekNewConnwctionRequestPopulateModel(StarTrekNewConnectionRequestModel model)
        {
            RAOrderRequestV2 rAOrder = new RAOrderRequestV2();

            if(model != null)
            {
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.is_paired = model.is_paired;
                rAOrder.sim_category = model.sim_category;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.bi_token_number = model.bi_token_number;
                rAOrder.sim_number = model.sim_number;
                rAOrder.session_token = model.session_token;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.customer_name = model.customer_name;
                rAOrder.gender = model.gender;
                rAOrder.flat_number = model.flat_number;
                rAOrder.house_number = model.house_number;
                rAOrder.road_number = model.road_number;
                rAOrder.village = model.village;
                rAOrder.division_id = model.division_id;
                rAOrder.district_id = model.district_id;
                rAOrder.thana_id = model.thana_id;
                rAOrder.postal_code = Convert.ToString(model.postal_code);
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.email = model.email;
                rAOrder.subscription_type_id = model.subscription_type_id;
                rAOrder.subscription_code = model.subscription_code;
                rAOrder.package_id = model.package_id;
                rAOrder.package_code = model.package_code;
                rAOrder.division_name = model.division_name;
                rAOrder.district_name = model.district_name;
                rAOrder.thana_name = model.thana_name;
                rAOrder.channel_name = model.channel_name;
                rAOrder.right_id = model.right_id;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.order_id = model.order_id;
            }           

            return rAOrder;
        }
        public RAOrderRequestV2 SIMReplacementRequestPopulateModel(SimReplacementRequestModel model)
        {
            RAOrderRequestV2 rAOrder = new RAOrderRequestV2();

            if(model != null)
            {
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.sim_number = model.sim_number;
                rAOrder.saf_status = model.saf_status;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.session_token = model.session_token;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.customer_name = model.customer_name;
                rAOrder.gender = model.gender;
                rAOrder.flat_number = model.flat_number;
                rAOrder.house_number = model.house_number;
                rAOrder.road_number = model.road_number;
                rAOrder.village = model.village;
                rAOrder.division_id = model.division_id;
                rAOrder.district_id = model.district_id;
                rAOrder.thana_id = model.thana_id;
                rAOrder.postal_code = model.postal_code;
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.email = model.email;
                rAOrder.sim_rep_reason_id = model.sim_rep_reason_id;
                rAOrder.payment_type = model.payment_type;
                rAOrder.division_name = model.division_name;
                rAOrder.district_name = model.district_name;
                rAOrder.thana_name = model.thana_name;
                rAOrder.sim_replc_reason = model.sim_replc_reason;
                if (!String.IsNullOrEmpty(model.dbss_subscription_id))
                {
                    rAOrder.dbss_subscription_id = Convert.ToInt32(model.dbss_subscription_id);
                }
                rAOrder.channel_name = model.channel_name;
                rAOrder.customer_id = model.customer_id;
                if (!String.IsNullOrEmpty(model.right_id))
                {
                    rAOrder.right_id = Convert.ToInt32(model.right_id);
                }
                rAOrder.center_code = model.center_code;
                rAOrder.distributor_code = model.distributor_code;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
            }
            
            return rAOrder;
        }
        public HomeWifiOrderRequest HomeWifiSIMReplacementRequestPopulateModel(HomeWifiSimReplacementRequestModel model)
        {
            HomeWifiOrderRequest rAOrder = new HomeWifiOrderRequest();

            if(model != null)
            {
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.sim_number = model.sim_number;
                rAOrder.saf_status = model.saf_status;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.session_token = model.session_token;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.customer_name = model.customer_name;
                rAOrder.gender = model.gender;
                rAOrder.flat_number = model.flat_number;
                rAOrder.house_number = model.house_number;
                rAOrder.road_number = model.road_number;
                rAOrder.village = model.village;
                rAOrder.division_id = model.division_id;
                rAOrder.district_id = model.district_id;
                rAOrder.thana_id = model.thana_id;
                rAOrder.postal_code = model.postal_code;
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.email = model.email;
                rAOrder.sim_rep_reason_id = model.sim_rep_reason_id;
                rAOrder.payment_type = model.payment_type;
                rAOrder.division_name = model.division_name;
                rAOrder.district_name = model.district_name;
                rAOrder.thana_name = model.thana_name;
                rAOrder.sim_replc_reason = model.sim_replc_reason;
                if (!String.IsNullOrEmpty(model.dbss_subscription_id))
                {
                    rAOrder.dbss_subscription_id = Convert.ToInt32(model.dbss_subscription_id);
                }
                rAOrder.channel_name = model.channel_name;
                rAOrder.customer_id = model.customer_id;
                if (!String.IsNullOrEmpty(model.right_id))
                {
                    rAOrder.right_id = Convert.ToInt32(model.right_id);
                }
                rAOrder.center_code = model.center_code;
                rAOrder.distributor_code = model.distributor_code;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
                rAOrder.order_number = model.order_number;
                rAOrder.initiator_channel = model.initiator_channel;
                rAOrder.order_type = model.order_type;
                rAOrder.subscription_type = model.subscription_type;
                rAOrder.simkit_type = model.simkit_type;

            }
            
            return rAOrder;
        }
        public RAOrderRequestV2 CorpSIMReplacementRequestPopulateModel(CorpSimReplacementRequestModel model)
        {
            RAOrderRequestV2 rAOrder = new RAOrderRequestV2();

            if(model != null)
            {
                rAOrder.session_token = model.session_token;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.bi_token_number = model.bi_token_number;
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.msisdn = model.msisdn;
                rAOrder.poc_msisdn_number = model.poc_msisdn_number;
                rAOrder.sim_number = model.sim_number;
                rAOrder.old_sim_number = model.old_sim_number;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.src_dob = model.src_dob;
                rAOrder.src_nid = model.src_nid;
                rAOrder.sim_replacement_type = model.sim_replacement_type;
                rAOrder.dbss_subscription_id = model.dbss_subscription_id;
                rAOrder.channel_name = model.channel_name;
                rAOrder.right_id = model.right_id;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
                rAOrder.payment_type = model.payment_type;
                rAOrder.sim_rep_reason_id = model.sim_rep_reason_id;
            }           

            return rAOrder;
        }
        public RAOrderRequestV2 NewConnwctionRequestPopulateModel(NewConnectionRequestModel model)
        {
            RAOrderRequestV2 rAOrder = new RAOrderRequestV2();

            if(model != null)
            {
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.is_paired = model.is_paired;
                rAOrder.sim_category = model.sim_category;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.bi_token_number = model.bi_token_number;
                rAOrder.sim_number = model.sim_number;
                rAOrder.session_token = model.session_token;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.customer_name = model.customer_name;
                rAOrder.gender = model.gender;
                rAOrder.flat_number = model.flat_number;
                rAOrder.house_number = model.house_number;
                rAOrder.road_number = model.road_number;
                rAOrder.village = model.village;
                rAOrder.division_id = model.division_id;
                rAOrder.district_id = model.district_id;
                rAOrder.thana_id = model.thana_id;
                rAOrder.postal_code = Convert.ToString(model.postal_code);
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.email = model.email;
                rAOrder.subscription_type_id = model.subscription_type_id;
                rAOrder.subscription_code = model.subscription_code;
                rAOrder.package_id = model.package_id;
                rAOrder.package_code = model.package_code;
                rAOrder.division_name = model.division_name;
                rAOrder.district_name = model.district_name;
                rAOrder.thana_name = model.thana_name;
                rAOrder.channel_name = model.channel_name;
                rAOrder.right_id = model.right_id;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.dest_imsi = model.dest_imsi;
                rAOrder.is_lus = model.is_lus;
                rAOrder.bts_code= model.bts_code;
                rAOrder.selected_category = model.selected_category;
            }           

            return rAOrder;
        }

        public HomeWifiOrderRequest HomeWifiNewConnwctionRequestPopulateModel(HomeWifiNewConnectionRequestModel model)
        {
            HomeWifiOrderRequest rAOrder = new HomeWifiOrderRequest();

            if (model != null)
            {
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.is_paired = model.is_paired;
                rAOrder.sim_category = model.sim_category;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.bi_token_number = model.bi_token_number;
                rAOrder.sim_number = model.sim_number;
                rAOrder.session_token = model.session_token;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.customer_name = model.customer_name;
                rAOrder.gender = model.gender;
                rAOrder.flat_number = model.flat_number;
                rAOrder.house_number = model.house_number;
                rAOrder.road_number = model.road_number;
                rAOrder.village = model.village;
                rAOrder.division_id = model.division_id;
                rAOrder.district_id = model.district_id;
                rAOrder.thana_id = model.thana_id;
                rAOrder.postal_code = Convert.ToString(model.postal_code);
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.email = model.email;
                rAOrder.subscription_type_id = model.subscription_type_id;
                rAOrder.subscription_code = model.subscription_code;
                rAOrder.package_id = model.package_id;
                rAOrder.package_code = model.package_code;
                rAOrder.division_name = model.division_name;
                rAOrder.district_name = model.district_name;
                rAOrder.thana_name = model.thana_name;
                rAOrder.channel_name = model.channel_name;
                rAOrder.right_id = model.right_id;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.dest_imsi = model.dest_imsi;
                rAOrder.is_lus = model.is_lus;
                rAOrder.bts_code = model.bts_code;
                rAOrder.selected_category = model.selected_category;
                rAOrder.order_number = model.order_number;
                rAOrder.initiator_channel = model.initiator_channel;
                rAOrder.order_type = model.order_type;
                rAOrder.subscription_type = model.subscription_type;
                rAOrder.simkit_type = model.simkit_type;
            }

            return rAOrder;
        }

        public CherishRequest CherishNewConnwctionRequestPopulateModel(CherishNewConnectionRequestModel model)
        {
            CherishRequest rAOrder = new CherishRequest();

            if (model != null)
            {
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.is_paired = model.is_paired;
                rAOrder.sim_category = model.sim_category;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.bi_token_number = model.bi_token_number;
                rAOrder.sim_number = model.sim_number;
                rAOrder.session_token = model.session_token;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.customer_name = model.customer_name;
                rAOrder.gender = model.gender;
                rAOrder.flat_number = model.flat_number;
                rAOrder.house_number = model.house_number;
                rAOrder.road_number = model.road_number;
                rAOrder.village = model.village;
                rAOrder.division_id = model.division_id;
                rAOrder.district_id = model.district_id;
                rAOrder.thana_id = model.thana_id;
                rAOrder.postal_code = Convert.ToString(model.postal_code);
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.email = model.email;
                rAOrder.subscription_type_id = model.subscription_type_id;
                rAOrder.subscription_code = model.subscription_code;
                rAOrder.package_id = model.package_id;
                rAOrder.package_code = model.package_code;
                rAOrder.division_name = model.division_name;
                rAOrder.district_name = model.district_name;
                rAOrder.thana_name = model.thana_name;
                rAOrder.channel_name = model.channel_name;
                rAOrder.right_id = model.right_id;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.selected_category = model.Selected_category;
                rAOrder.dest_imsi = model.dest_imsi;
                rAOrder.bts_code = model.bts_code;
                rAOrder.is_lus = model.is_lus;
                rAOrder.selected_category= model.Selected_category;
            }

            return rAOrder;
        }

        public RAOrderRequestV2 FPRegistrationEcVerificationRequest(FPRegistrationEcVerificationRequestModel model)
        {
            RAOrderRequestV2 rAOrder = new RAOrderRequestV2();
            if(model != null)
            {
                rAOrder.channel_name = model.channel_name;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.msisdn = model.msisdn;
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.right_id = model.right_id;
                rAOrder.session_token = model.session_token;
            }           

            return rAOrder;
        }
        public RAOrderRequestV2 POCEcVerificationRequestPopulateModel(POCEcVerificationRequestModel model)
        {
            RAOrderRequestV2 rAOrder = new RAOrderRequestV2();
            
            if (model != null)
            {
                rAOrder.session_token = model.session_token;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.purpose_number = Convert.ToString(model.purpose_number);
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.channel_name = model.channel_name;
                rAOrder.right_id = model.right_id;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude =Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
            }

            return rAOrder;
        }
        public RAOrderRequestV2 MNPSubmitRequestPopulateModel(MNPSubmitRequestModel model)
        {
            RAOrderRequestV2 rAOrder = new RAOrderRequestV2();
            if(model != null)
            {
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.is_paired = model.is_paired;
                rAOrder.sim_category = model.sim_category;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.bi_token_number = model.bi_token_number;
                rAOrder.sim_number = model.sim_number;
                rAOrder.session_token = model.session_token;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.customer_name = model.customer_name;
                rAOrder.gender = model.gender;
                rAOrder.flat_number = model.flat_number;
                rAOrder.house_number = model.house_number;
                rAOrder.road_number = model.road_number;
                rAOrder.village = model.village;
                rAOrder.division_id = model.division_id;
                rAOrder.district_id = model.district_id;
                rAOrder.thana_id = model.thana_id;
                rAOrder.postal_code = Convert.ToString(model.postal_code);
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.email = model.email;
                rAOrder.subscription_type_id = model.subscription_type_id;
                rAOrder.subscription_code = model.subscription_code;
                rAOrder.package_id = model.package_id;
                rAOrder.package_code = model.package_code;
                rAOrder.division_name = model.division_name;
                rAOrder.district_name = model.district_name;
                rAOrder.thana_name = model.thana_name;
                rAOrder.channel_name = model.channel_name;
                rAOrder.right_id = model.right_id;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
                rAOrder.distributor_code = model.distributor_code;
                rAOrder.center_code = model.center_code;
            }            

            return rAOrder;
        }
        public HomeWifiOrderRequest HomeWifiMNPSubmitRequestPopulateModel(HomeWifiMNPSubmitRequestModel model)
        {
            HomeWifiOrderRequest rAOrder = new HomeWifiOrderRequest();
            if (model != null)
            {
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.is_paired = model.is_paired;
                rAOrder.sim_category = model.sim_category;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.bi_token_number = model.bi_token_number;
                rAOrder.sim_number = model.sim_number;
                rAOrder.session_token = model.session_token;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.customer_name = model.customer_name;
                rAOrder.gender = model.gender;
                rAOrder.flat_number = model.flat_number;
                rAOrder.house_number = model.house_number;
                rAOrder.road_number = model.road_number;
                rAOrder.village = model.village;
                rAOrder.division_id = model.division_id;
                rAOrder.district_id = model.district_id;
                rAOrder.thana_id = model.thana_id;
                rAOrder.postal_code = Convert.ToString(model.postal_code);
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.email = model.email;
                rAOrder.subscription_type_id = model.subscription_type_id;
                rAOrder.subscription_code = model.subscription_code;
                rAOrder.package_id = model.package_id;
                rAOrder.package_code = model.package_code;
                rAOrder.division_name = model.division_name;
                rAOrder.district_name = model.district_name;
                rAOrder.thana_name = model.thana_name;
                rAOrder.channel_name = model.channel_name;
                rAOrder.right_id = model.right_id;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
                rAOrder.distributor_code = model.distributor_code;
                rAOrder.center_code = model.center_code;
                rAOrder.order_number = model.order_number;
                rAOrder.initiator_channel = model.initiator_channel;
                rAOrder.order_type = model.order_type;
                rAOrder.subscription_type = model.subscription_type;
                rAOrder.simkit_type = model.simkit_type;
            }

            return rAOrder;
        }
        public DMSRetailerReqModel DMSRetailerSyncRequestPopulateModel(DMSRetailerSyncRequestModel model)
        {
            DMSRetailerReqModel reqModel = new DMSRetailerReqModel();

            if(model != null)
            {
                reqModel.userName = model.userName;
                reqModel.password = model.password;
                reqModel.retailerCode = model.retailerCode;
                reqModel.iTopUpNumber = model.iTopUpNumber;
                reqModel.isActive = model.isActive;
                reqModel.typeName = model.typeName;
            }

            return reqModel;
        }
        public BIAFinishNotiRequest DBSSToAppNotificationRequestPopulateModel(DBSSToAppNotificationRequestModel model)
        {
            BIAFinishNotiRequest bIAFinish = new BIAFinishNotiRequest();

            if (model != null) 
            {
                bIAFinish.session_token = model.session_token;
                bIAFinish.bio_request_id = model.bio_request_id;
                bIAFinish.is_Success = model.is_Success;
                bIAFinish.error_code = model.error_code;
                bIAFinish.description = model.description;
                bIAFinish.error_source = model.error_source;
            }

            return bIAFinish;
        }
        public RechargeRequestModel FirstRechargeRequestPopulateModel(FirstRechargeRequestModel model)
        {
            RechargeRequestModel requestModel = new RechargeRequestModel();

            if (model != null)
            {
                requestModel.session_token = model.session_token;
                requestModel.retailerCode = model.retailerCode;
                requestModel.subscriberNo = model.subscriberNo;
                requestModel.amount = model.amount;
                requestModel.userPin = model.userPin;
                requestModel.deviceId = model.deviceId;
                requestModel.paymentType = model.paymentType;
                requestModel.lat = model.lat;
                requestModel.lng = model.lng;
                requestModel.lan = model.lan;
                requestModel.userId = model.userId;
                requestModel.bi_token_number = model.bi_token_number;
            }

            return requestModel;
        }
        public RechargeAmountReqModel FirstRechargeAmountRequestPopulateModel(FirstRechargeAmountRequestModel model)
        {
            RechargeAmountReqModel reqModel = new RechargeAmountReqModel();
            if (model != null)
            {
                reqModel.session_token = model.session_token;
                reqModel.retailer_code = model.retailer_code;
                reqModel.channel_name = model.channel_name;
            }
            return reqModel;
        }
        public RAOrderRequestV2 BioCancelRequestPopulateModel(BioCancelRequestModel model)
        {
            RAOrderRequestV2 rAOrder = new RAOrderRequestV2();

            if (model != null)
            {
                rAOrder.purpose_number = model.purpose_number;
                rAOrder.is_paired = model.is_paired;
                rAOrder.sim_category = model.sim_category;
                rAOrder.retailer_id = model.retailer_id;
                rAOrder.bi_token_number = model.bi_token_number;
                rAOrder.sim_number = model.sim_number;
                rAOrder.session_token = model.session_token;
                rAOrder.msisdn = model.msisdn;
                rAOrder.dest_nid = model.dest_nid;
                rAOrder.dest_dob = model.dest_dob;
                rAOrder.customer_name = model.customer_name;
                rAOrder.gender = model.gender;
                rAOrder.flat_number = model.flat_number;
                rAOrder.house_number = model.house_number;
                rAOrder.road_number = model.road_number;
                rAOrder.village = model.village;
                rAOrder.division_id = model.division_id;
                rAOrder.district_id = model.district_id;
                rAOrder.thana_id = model.thana_id;
                rAOrder.postal_code = Convert.ToString(model.postal_code);
                rAOrder.alt_msisdn = model.alt_msisdn;
                rAOrder.email = model.email;
                rAOrder.subscription_type_id = model.subscription_type_id;
                rAOrder.subscription_code = model.subscription_code;
                rAOrder.package_id = model.package_id;
                rAOrder.package_code = model.package_code;
                rAOrder.division_name = model.division_name;
                rAOrder.district_name = model.district_name;
                rAOrder.thana_name = model.thana_name;
                rAOrder.channel_name = model.channel_name;
                rAOrder.right_id = model.right_id;
                rAOrder.lac = model.lac;
                rAOrder.cid = model.cid;
                rAOrder.latitude = Convert.ToDecimal(model.latitude);
                rAOrder.longitude = Convert.ToDecimal(model.longitude);
                rAOrder.scanner_id = model.scanner_id;
                rAOrder.isBPUser = model.isBPUser;
                rAOrder.dest_left_thumb = model.dest_left_thumb;
                rAOrder.dest_left_thumb_score = model.dest_left_thumb_score;
                rAOrder.dest_left_index = model.dest_left_index;
                rAOrder.dest_left_index_score = model.dest_left_index_score;
                rAOrder.dest_right_thumb = model.dest_right_thumb;
                rAOrder.dest_right_thumb_score = model.dest_right_thumb_score;
                rAOrder.dest_right_index = model.dest_right_index;
                rAOrder.dest_right_index_score = model.dest_right_index_score;
            }

            return rAOrder;
        }
        public FPRegistrationModel FPRegistrationRequestPopulateModel(FPRegistrationRequestModel model)
        {
            FPRegistrationModel fPRegistration = new FPRegistrationModel();

            if (model != null)
            {
                fPRegistration.user_name = model.user_name;
                fPRegistration.left_thumb = model.left_thumb;
                fPRegistration.left_thumb_score = model.left_thumb_score;
                fPRegistration.left_index = model.left_index;
                fPRegistration.left_index_score = model.left_index_score;
                fPRegistration.right_thumb = model.right_thumb;
                fPRegistration.right_thumb_score = model.right_thumb_score;
                fPRegistration.right_index = model.right_index;
                fPRegistration.right_index_score = model.right_index_score;
                fPRegistration.mobile_no = model.mobile_no;
                fPRegistration.session_token = model.session_token;
            }

            return fPRegistration;
        }
        public ResubmitReqModel ResubmitRequestPopulateModel(FailedResubmitRequestModel model)
        {
            ResubmitReqModel reqModel = new ResubmitReqModel();

            if (model != null)
            {
                reqModel.session_token = model.session_token;
                reqModel.right_id = model.right_id;
                reqModel.bi_token_number = model.bi_token_number;
                reqModel.retailer_id = model.retailer_id;
                reqModel.distributor_code = model.distributor_code;
                reqModel.isBPUser = model.isBPUser;
                reqModel.latitude = model.latitude;
                reqModel.longitude = model.longitude;
            }
            return reqModel;
        }
    }
}
