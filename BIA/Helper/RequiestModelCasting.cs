using BIA.Entity.RequestEntity;

namespace BIA.Helper
{
    public class RequiestModelCasting
    {
        public RequestModelBLOBConversion CastRequestModel(RAOrderRequestV2 model)
        {
            RequestModelBLOBConversion conversion = new RequestModelBLOBConversion();

            conversion.session_token = model.session_token;
            conversion.right_id = model.right_id;
            conversion.bi_token_number = model.bi_token_number;
            conversion.purpose_number = model.purpose_number;
            conversion.msisdn = model.msisdn;
            conversion.sim_category = model.sim_category;
            conversion.sim_number = model.sim_number;
            conversion.subscription_type_id = model.subscription_type_id;
            conversion.subscription_code = model.subscription_code;
            conversion.package_id = model.package_id;
            conversion.package_code = model.package_code;
            conversion.dest_nid = model.dest_nid;
            conversion.src_nid = model.src_nid;
            conversion.dest_dob = model.dest_dob;
            conversion.dest_doc_type_no = model.dest_doc_type_no;
            conversion.src_dob = model.src_dob;
            conversion.src_doc_type_no = model.src_doc_type_no;
            conversion.platform_id = model.platform_id;
            conversion.customer_name = model.customer_name;
            conversion.gender = model.gender;
            conversion.flat_number = model.flat_number;
            conversion.house_number = model.house_number;
            conversion.road_number = model.road_number;
            conversion.village = model.village;
            conversion.division_id = model.division_id;
            conversion.district_id = model.district_id;
            conversion.thana_id = model.thana_id;
            conversion.postal_code = model.postal_code;
            conversion.email = model.email;

            if (model.dest_left_thumb != null)
                conversion.dest_left_thumb = null;
            if (model.dest_left_index != null)
                conversion.dest_left_index = null;
            if (model.dest_right_thumb != null)
                conversion.dest_right_thumb = null;
            if (model.dest_right_index != null)
                conversion.dest_right_index = null;
            if (model.src_left_index != null)
                conversion.src_left_index = null;
            if (model.src_left_thumb != null)
                conversion.src_left_thumb = null;
            if (model.src_right_index != null)
                conversion.src_right_index = null;
            if (model.src_right_thumb != null)
                conversion.src_right_thumb = null;

            conversion.dest_left_thumb_score = model.dest_left_thumb_score;
            conversion.dest_left_index_score = model.dest_left_index_score;
            conversion.dest_right_thumb_score = model.dest_right_thumb_score;
            conversion.dest_right_index_score = model.dest_right_index_score;
            conversion.src_left_thumb_score = model.src_left_thumb_score;
            conversion.src_left_index_score = model.src_left_index_score;
            conversion.src_right_thumb_score = model.src_right_thumb_score;
            conversion.src_right_index_score = model.src_right_index_score;
            conversion.retailer_id = model.retailer_id;
            conversion.port_in_date = model.port_in_date;
            conversion.alt_msisdn = model.alt_msisdn;
            conversion.poc_msisdn_number = model.poc_msisdn_number;
            conversion.sim_rep_reason_id = model.sim_rep_reason_id;
            conversion.payment_type = model.payment_type;
            conversion.is_paired = model.is_paired;
            conversion.channel_id = model.channel_id;
            conversion.division_name = model.division_name;
            conversion.district_name = model.district_name;
            conversion.thana_name = model.thana_name;
            conversion.center_code = model.center_code;
            conversion.distributor_code = model.distributor_code;
            conversion.sim_replc_reason = model.sim_replc_reason;
            conversion.channel_name = model.channel_name;
            conversion.sim_replacement_type = model.sim_replacement_type;
            conversion.old_sim_number = model.old_sim_number;
            conversion.src_sim_category = model.src_sim_category;
            conversion.port_in_confirmation_code = model.port_in_confirmation_code;
            conversion.dest_ec_verifi_reqrd = model.dest_ec_verifi_reqrd;
            conversion.src_ec_verifi_reqrd = model.src_ec_verifi_reqrd;
            conversion.dest_foreign_flag = model.dest_foreign_flag;
            conversion.dbss_subscription_id = model.dbss_subscription_id;
            conversion.otp = model.otp;
            conversion.lac = model.lac;
            conversion.cid = model.cid;
            conversion.latitude = model.latitude;
            conversion.longitude = model.longitude;
            conversion.scanner_id = model.scanner_id;
            conversion.order_id = model.order_id; 
            conversion.selected_category = model.selected_category;

            return conversion;
        }

        public RequestModelBLOBConversion HomeWifiCastRequestModel(HomeWifiOrderRequest model)
        {
            RequestModelBLOBConversion conversion = new RequestModelBLOBConversion();

            conversion.session_token = model.session_token;
            conversion.right_id = model.right_id;
            conversion.bi_token_number = model.bi_token_number;
            conversion.purpose_number = model.purpose_number;
            conversion.msisdn = model.msisdn;
            conversion.sim_category = model.sim_category;
            conversion.sim_number = model.sim_number;
            conversion.subscription_type_id = model.subscription_type_id;
            conversion.subscription_code = model.subscription_code;
            conversion.package_id = model.package_id;
            conversion.package_code = model.package_code;
            conversion.dest_nid = model.dest_nid;
            conversion.src_nid = model.src_nid;
            conversion.dest_dob = model.dest_dob;
            conversion.dest_doc_type_no = model.dest_doc_type_no;
            conversion.src_dob = model.src_dob;
            conversion.src_doc_type_no = model.src_doc_type_no;
            conversion.platform_id = model.platform_id;
            conversion.customer_name = model.customer_name;
            conversion.gender = model.gender;
            conversion.flat_number = model.flat_number;
            conversion.house_number = model.house_number;
            conversion.road_number = model.road_number;
            conversion.village = model.village;
            conversion.division_id = model.division_id;
            conversion.district_id = model.district_id;
            conversion.thana_id = model.thana_id;
            conversion.postal_code = model.postal_code;
            conversion.email = model.email;

            if (model.dest_left_thumb != null)
                conversion.dest_left_thumb = null;
            if (model.dest_left_index != null)
                conversion.dest_left_index = null;
            if (model.dest_right_thumb != null)
                conversion.dest_right_thumb = null;
            if (model.dest_right_index != null)
                conversion.dest_right_index = null;
            if (model.src_left_index != null)
                conversion.src_left_index = null;
            if (model.src_left_thumb != null)
                conversion.src_left_thumb = null;
            if (model.src_right_index != null)
                conversion.src_right_index = null;
            if (model.src_right_thumb != null)
                conversion.src_right_thumb = null;

            conversion.dest_left_thumb_score = model.dest_left_thumb_score;
            conversion.dest_left_index_score = model.dest_left_index_score;
            conversion.dest_right_thumb_score = model.dest_right_thumb_score;
            conversion.dest_right_index_score = model.dest_right_index_score;
            conversion.src_left_thumb_score = model.src_left_thumb_score;
            conversion.src_left_index_score = model.src_left_index_score;
            conversion.src_right_thumb_score = model.src_right_thumb_score;
            conversion.src_right_index_score = model.src_right_index_score;
            conversion.retailer_id = model.retailer_id;
            conversion.port_in_date = model.port_in_date;
            conversion.alt_msisdn = model.alt_msisdn;
            conversion.poc_msisdn_number = model.poc_msisdn_number;
            conversion.sim_rep_reason_id = model.sim_rep_reason_id;
            conversion.payment_type = model.payment_type;
            conversion.is_paired = model.is_paired;
            conversion.channel_id = model.channel_id;
            conversion.division_name = model.division_name;
            conversion.district_name = model.district_name;
            conversion.thana_name = model.thana_name;
            conversion.center_code = model.center_code;
            conversion.distributor_code = model.distributor_code;
            conversion.sim_replc_reason = model.sim_replc_reason;
            conversion.channel_name = model.channel_name;
            conversion.sim_replacement_type = model.sim_replacement_type;
            conversion.old_sim_number = model.old_sim_number;
            conversion.src_sim_category = model.src_sim_category;
            conversion.port_in_confirmation_code = model.port_in_confirmation_code;
            conversion.dest_ec_verifi_reqrd = model.dest_ec_verifi_reqrd;
            conversion.src_ec_verifi_reqrd = model.src_ec_verifi_reqrd;
            conversion.dest_foreign_flag = model.dest_foreign_flag;
            conversion.dbss_subscription_id = model.dbss_subscription_id;
            conversion.otp = model.otp;
            conversion.lac = model.lac;
            conversion.cid = model.cid;
            conversion.latitude = model.latitude;
            conversion.longitude = model.longitude;
            conversion.scanner_id = model.scanner_id;
            conversion.order_id = model.order_id;
            conversion.selected_category = model.selected_category;
            conversion.order_number = model.order_number;
            conversion.initiator_channel = model.initiator_channel;
            conversion.order_type = model.order_type;
            conversion.subscription_type = model.subscription_type;
            conversion.simkit_type = model.simkit_type;

            return conversion;
        }

        public RequestModelBLOBConversion CastRequestModelV2(CherishRequest model)
        {
            RequestModelBLOBConversion conversion = new RequestModelBLOBConversion();

            conversion.session_token = model.session_token;
            conversion.right_id = model.right_id;
            conversion.bi_token_number = model.bi_token_number;
            conversion.purpose_number = model.purpose_number;
            conversion.msisdn = model.msisdn;
            conversion.sim_category = model.sim_category;
            conversion.sim_number = model.sim_number;
            conversion.subscription_type_id = model.subscription_type_id;
            conversion.subscription_code = model.subscription_code;
            conversion.package_id = model.package_id;
            conversion.package_code = model.package_code;
            conversion.dest_nid = model.dest_nid;
            conversion.src_nid = model.src_nid;
            conversion.dest_dob = model.dest_dob;
            conversion.dest_doc_type_no = model.dest_doc_type_no;
            conversion.src_dob = model.src_dob;
            conversion.src_doc_type_no = model.src_doc_type_no;
            conversion.platform_id = model.platform_id;
            conversion.customer_name = model.customer_name;
            conversion.gender = model.gender;
            conversion.flat_number = model.flat_number;
            conversion.house_number = model.house_number;
            conversion.road_number = model.road_number;
            conversion.village = model.village;
            conversion.division_id = model.division_id;
            conversion.district_id = model.district_id;
            conversion.thana_id = model.thana_id;
            conversion.postal_code = model.postal_code;
            conversion.email = model.email;

            if (model.dest_left_thumb != null)
                conversion.dest_left_thumb = null;
            if (model.dest_left_index != null)
                conversion.dest_left_index = null;
            if (model.dest_right_thumb != null)
                conversion.dest_right_thumb = null;
            if (model.dest_right_index != null)
                conversion.dest_right_index = null;
            if (model.src_left_index != null)
                conversion.src_left_index = null;
            if (model.src_left_thumb != null)
                conversion.src_left_thumb = null;
            if (model.src_right_index != null)
                conversion.src_right_index = null;
            if (model.src_right_thumb != null)
                conversion.src_right_thumb = null;

            conversion.dest_left_thumb_score = model.dest_left_thumb_score;
            conversion.dest_left_index_score = model.dest_left_index_score;
            conversion.dest_right_thumb_score = model.dest_right_thumb_score;
            conversion.dest_right_index_score = model.dest_right_index_score;
            conversion.src_left_thumb_score = model.src_left_thumb_score;
            conversion.src_left_index_score = model.src_left_index_score;
            conversion.src_right_thumb_score = model.src_right_thumb_score;
            conversion.src_right_index_score = model.src_right_index_score;
            conversion.retailer_id = model.retailer_id;
            conversion.port_in_date = model.port_in_date;
            conversion.alt_msisdn = model.alt_msisdn;
            conversion.poc_msisdn_number = model.poc_msisdn_number;
            conversion.sim_rep_reason_id = model.sim_rep_reason_id;
            conversion.payment_type = model.payment_type;
            conversion.is_paired = model.is_paired;
            conversion.channel_id = model.channel_id;
            conversion.division_name = model.division_name;
            conversion.district_name = model.district_name;
            conversion.thana_name = model.thana_name;
            conversion.center_code = model.center_code;
            conversion.distributor_code = model.distributor_code;
            conversion.sim_replc_reason = model.sim_replc_reason;
            conversion.channel_name = model.channel_name;
            conversion.sim_replacement_type = model.sim_replacement_type;
            conversion.old_sim_number = model.old_sim_number;
            conversion.src_sim_category = model.src_sim_category;
            conversion.port_in_confirmation_code = model.port_in_confirmation_code;
            conversion.dest_ec_verifi_reqrd = model.dest_ec_verifi_reqrd;
            conversion.src_ec_verifi_reqrd = model.src_ec_verifi_reqrd;
            conversion.dest_foreign_flag = model.dest_foreign_flag;
            conversion.dbss_subscription_id = model.dbss_subscription_id;
            conversion.otp = model.otp;
            conversion.lac = model.lac;
            conversion.cid = model.cid;
            conversion.latitude = model.latitude;
            conversion.longitude = model.longitude;
            conversion.scanner_id = model.scanner_id;
            conversion.order_id = model.order_id;
            conversion.selected_category = model.selected_category;
            conversion.dest_imsi = model.dest_imsi;

            return conversion;
        }
    }
}
