using BIA.BLL.BLLServices;
using BIA.Entity.Collections;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BIA.BLL.Utility;

public partial class DataPopulateModel
{
        private readonly BLLCommon _bllCommon;        

        public DataPopulateModel(BLLCommon bllCommon)
        {
            _bllCommon = bllCommon;
        }
        public async Task<OrderRequest3> populateOrderData(RAOrderRequestV2 model)
        {
            OrderRequest3 order = new OrderRequest3();
            BL_Json blJson = new BL_Json();
            try
            {
                if (model.msisdn.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                }

                order.bi_token_number = model.bi_token_number == null ? 0 : model.bi_token_number;
                order.purpose_number = String.IsNullOrEmpty(model.purpose_number) ? null : Convert.ToDecimal(model.purpose_number.Trim());
                order.msisdn = model.msisdn;

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_category = model.src_sim_category;
                else
                    order.sim_category = model.sim_category;

                if (!String.IsNullOrEmpty(model.sim_number))
                {
                    if (model.sim_number.Substring(0, 6) != FixedValueCollection.SIMCode)
                    {
                        model.sim_number = FixedValueCollection.SIMCode + model.sim_number;
                    }
                }

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_number = model.old_sim_number != null ? model.old_sim_number.Trim() : "";
                else if (!String.IsNullOrEmpty(model.sim_number))
                    order.sim_number = model.sim_number.Trim();
                else
                    order.sim_number = model.sim_number;

                order.subscription_type_id = String.IsNullOrEmpty(model.subscription_type_id) ? null : Convert.ToDecimal(model.subscription_type_id.Trim());
                order.subscription_code = model.subscription_code;
                order.package_id = String.IsNullOrEmpty(model.package_id) ? null : Convert.ToDecimal(model.package_id.Trim());
                order.package_code = model.package_code;
                if (!String.IsNullOrEmpty(model.dest_nid) && !String.IsNullOrEmpty(model.dest_dob))
                {
                    order.dest_nid = (model.dest_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.dest_dob).Year + model.dest_nid : model.dest_nid;
                }

                if (!String.IsNullOrEmpty(model.src_nid) && !String.IsNullOrEmpty(model.src_dob))
                {
                    order.src_nid = (model.src_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.src_dob).Year + model.src_nid : model.src_nid;
                }

                order.dest_dob = String.IsNullOrEmpty(model.dest_dob) ? String.Empty : DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.dest_nid))
                {
                    order.dest_doc_type_no = model.dest_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;
                }

                order.src_dob = String.IsNullOrEmpty(model.src_dob) ? String.Empty : DateTime.Parse(model.src_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.src_nid))
                {
                    order.src_doc_type_no = model.src_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;//(int)DOCTypeNo.nid;//1 for nid. /*model.src_doc_type_no;*/
                }
                else
                {
                    order.src_doc_type_no = null;
                }
                order.platform_id = model.platform_id;

                if (!String.IsNullOrEmpty(model.customer_name) &&
                        isCustomerNameValid(model.customer_name))
                {
                    order.customer_name = model.customer_name;
                }

                if (!String.IsNullOrEmpty(model.gender))
                {
                    order.gender = model.gender.ToLower() == "others" ? "notitle" : model.gender.ToLower();
                }

                order.flat_number = model.flat_number;
                order.house_number = model.house_number;
                order.road_number = model.road_number;
                order.village = model.village;
                order.division_id = model.division_id;
                order.district_id = model.district_id;
                order.thana_id = model.thana_id;
                order.postal_code = String.IsNullOrEmpty(model.postal_code) ? "0" : model.postal_code;

                //=====temp modelfication (as RA apk is unable to update [15-12-19])============
                if (!String.IsNullOrEmpty(model.email))
                {
                    order.email = isEmailValid(model.email.ToLower()) == true ? model.email : String.Empty;
                }
                else
                {
                    order.email = String.Empty;
                }
                //===================

                order.retailer_id = model.retailer_id;
                //note: For retailer App dest_doc_id and src_doc_id is dest_nid and src_nid namely. 
                order.dest_left_thumb_score = model.dest_left_thumb_score.Equals(null) ? (decimal?)null : model.dest_left_thumb_score;
                order.dest_left_thumb = Array.Empty<byte>();
                order.dest_left_index_score = model.dest_left_index_score.Equals(null) ? (decimal?)null : model.dest_left_index_score;
                order.dest_left_index = Array.Empty<byte>();
                order.dest_right_thumb_score = model.dest_right_thumb_score.Equals(null) ? (decimal?)null : model.dest_right_thumb_score;
                order.dest_right_index = Array.Empty<byte>();
                order.dest_right_index_score = model.dest_right_index_score.Equals(null) ? (decimal?)null : model.dest_right_index_score;
                order.dest_right_thumb = Array.Empty<byte>();
                order.src_left_thumb_score = model.src_left_thumb_score.Equals(null) ? (decimal?)null : model.src_left_thumb_score;
                order.src_left_thumb = Array.Empty<byte>();
                order.src_left_index_score = model.src_left_index_score.Equals(null) ? (decimal?)null : model.src_left_index_score;
                order.src_left_index = Array.Empty<byte>();
                order.src_right_thumb_score = model.src_right_thumb_score.Equals(null) ? (decimal?)null : model.src_right_thumb_score;
                order.src_right_thumb = Array.Empty<byte>();
                order.src_right_index_score = model.src_right_index_score.Equals(null) ? (decimal?)null : model.src_right_index_score;
                order.src_right_index = Array.Empty<byte>();
                order.retailer_code = (model.channel_name == FixedValueCollection.ResellerChannel
                                                            || model.channel_name == FixedValueCollection.CorporateChannel
                                                            || model.channel_name == FixedValueCollection.SMEChannel)
                                                            ? FixedValueCollection.ResellerCodeText + model.retailer_id : model.retailer_id;
                order.port_in_date = String.IsNullOrEmpty(model.port_in_date) ? null : DateTime.Parse(model.port_in_date);
                order.alt_msisdn = model.alt_msisdn;

                if (!String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    if (model.poc_msisdn_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                    {
                        order.poc_number = FixedValueCollection.MSISDNCountryCode + model.poc_msisdn_number;
                    }
                }
                order.is_urgent = model.is_urgent;
                order.optional1 = model.optional1;
                order.optional2 = model.optional2;
                order.optional3 = model.otp; // For any kind of activation is processed through OTP then OTP will be insert in option3.
                order.optional4 = String.IsNullOrEmpty(model.optional4) ? null : Convert.ToDecimal(model.optional4.Trim());
                order.optional5 = String.IsNullOrEmpty(model.optional5) ? null : Convert.ToDecimal(model.optional5.Trim());
                order.optional6 = String.IsNullOrEmpty(model.optional6) ? null : Convert.ToDecimal(model.optional6.Trim());
                order.note = model.note;
                order.sim_rep_reason_id = String.IsNullOrEmpty(model.sim_rep_reason_id) ? null : Convert.ToDecimal(model.sim_rep_reason_id.Trim());
                order.payment_type = model.payment_type ?? "";
                order.is_paired = model.is_paired;
                order.cahnnel_id = model.channel_id;

                order.division_name = String.IsNullOrEmpty(model.division_name) ? model.division_name : ConverterHelper.UpperLowerWithSpaceConverter(model.division_name);
                order.district_name = String.IsNullOrEmpty(model.district_name) ? model.district_name : ConverterHelper.UpperLowerWithSpaceConverter(model.district_name);
                order.thana_name = String.IsNullOrEmpty(model.thana_name) ? model.thana_name : ConverterHelper.UpperLowerWithSpaceConverter(model.thana_name);

                //here distributor code is mapped with "P_CENTER_OR_DISTRIBUTOR_CODE" in DB. 
                order.distributor_code = model.distributor_code; // String.IsNullOrEmpty(model.distributor_code) ? await _bllCommon.GetDistributorCodeFromSessionTokenV2(model.session_token, model.retailer_id) : model.distributor_code;
                order.sim_replc_reason = model.sim_replc_reason;
                order.channel_name = model.channel_name;
                order.right_id = model.right_id;

                order.sim_replacement_type = model.sim_replacement_type;

                order.old_sim_number = String.IsNullOrEmpty(model.old_sim_number) ? null : model.old_sim_number;

                order.src_sim_category = model.src_sim_category;
                order.port_in_confirmation_code = model.port_in_confirmation_code;

                order.dest_ec_verifi_reqrd = !String.IsNullOrEmpty(model.dest_nid) ? 1 : 0;// 

                //If any trunsection porpuse have done by OTP, then the bio verification for src customer is not needed.
                //For example: For "two party EC verification" if the purpose is "B2B to B2C" with OTP, then the value "src_ec_verifi_reqrd" will 0.    
                //Here reseller app is sending OTP in "optional3" for "Corporate To Individual Transfer with OTP" [03 MAY 2020]
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.CorporateToIndividualTransfer
                    && !String.IsNullOrEmpty(model.otp))
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();
                }
                else
                {
                    order.src_ec_verifi_reqrd = !String.IsNullOrEmpty(model.src_nid) ? 1 : 0;
                }
                #region One Party EC Verification
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer
                   && model.src_ec_verifi_reqrd == 0)
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();
                }
                #endregion
                order.dest_foreign_flag = model.dest_foreign_flag;
                order.dbss_subscription_id = model.dbss_subscription_id;
                order.customer_id = model.customer_id;
                order.order_confirmation_code = model.order_confirmation_code;
                order.server_name = Environment.MachineName;

                //for sim repalcement customer update is enabled here.
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMReplacement &&
                    model.saf_status.HasValue)
                {
                    order.saf_status = model.saf_status == false ? 1 : 0;
                }
                else if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                            String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    order.saf_status = 1;
                }
                else
                {
                    order.saf_status = 0;
                }
                if (!String.IsNullOrEmpty(model.msisdnReservationId))
                {
                    order.msisdnReservationId = model.msisdnReservationId;
                }

                order.src_owner_customer_id = model.src_owner_customer_id;
                order.src_user_customer_id = model.src_user_customer_id;
                order.src_payer_customer_id = model.src_payer_customer_id;
                order.dest_imsi = model.dest_imsi;
                order.status = model.status;
                order.bss_reqId = model.bss_reqId;
                order.error_id = model.error_id;
                order.error_description = model.err_msg;

                order.loginAttemptId = LoginProviderInfo.login_attempt_id;
                order.latitude = model.latitude;
                order.longitude = model.longitude;

                if (model.lac != null)
                {
                    order.lac = (int)model.lac;
                }

                if (model.cid != null)
                {
                    order.cid = (int)model.cid;
                }

                order.scanner_id = model.scanner_id;

                if (model.order_booking_flag > 0 || model.order_booking_flag > -1)
                {
                    order.order_booking_flag = model.order_booking_flag;
                }
                order.prov_id = model.prov_id;

                return order;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<OrderRequest3> populateOrderDataV2(RAOrderRequestV2 model)
        {
            OrderRequest3 order = new OrderRequest3();
            BL_Json blJson = new BL_Json();
            try
            {
                if (model.msisdn.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                }
                order.bi_token_number = model.bi_token_number == null ? 0 : model.bi_token_number;
                order.purpose_number = String.IsNullOrEmpty(model.purpose_number) ? null : Convert.ToDecimal(model.purpose_number.Trim());
                order.msisdn = model.msisdn;

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_category = model.src_sim_category;
                else
                    order.sim_category = model.sim_category;

                if (!String.IsNullOrEmpty(model.sim_number))
                {
                    if (model.sim_number.Substring(0, 6) != FixedValueCollection.SIMCode)
                    {
                        model.sim_number = FixedValueCollection.SIMCode + model.sim_number;
                    }
                }

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_number = model.old_sim_number != null ? model.old_sim_number.Trim() : "";
                else if (!String.IsNullOrEmpty(model.sim_number))
                    order.sim_number = model.sim_number.Trim();
                else
                    order.sim_number = model.sim_number;

                order.subscription_type_id = String.IsNullOrEmpty(model.subscription_type_id) ? null : Convert.ToDecimal(model.subscription_type_id.Trim());
                order.subscription_code = model.subscription_code;
                order.package_id = String.IsNullOrEmpty(model.package_id) ? null : Convert.ToDecimal(model.package_id.Trim());
                order.package_code = model.package_code;
                if (!String.IsNullOrEmpty(model.dest_nid) && !String.IsNullOrEmpty(model.dest_dob))
                {
                    order.dest_nid = (model.dest_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.dest_dob).Year + model.dest_nid : model.dest_nid;
                }

                if (!String.IsNullOrEmpty(model.src_nid) && !String.IsNullOrEmpty(model.src_dob))
                {
                    order.src_nid = (model.src_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.src_dob).Year + model.src_nid : model.src_nid;
                }

                order.dest_dob = String.IsNullOrEmpty(model.dest_dob) ? String.Empty : DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.dest_nid))
                {
                    order.dest_doc_type_no = model.dest_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;
                }

                order.src_dob = String.IsNullOrEmpty(model.src_dob) ? String.Empty : DateTime.Parse(model.src_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.src_nid))
                {
                    order.src_doc_type_no = model.src_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;//(int)DOCTypeNo.nid;//1 for nid. /*model.src_doc_type_no;*/
                }
                else
                {
                    order.src_doc_type_no = null;
                }
                order.platform_id = model.platform_id;

                if (!String.IsNullOrEmpty(model.customer_name) &&
                        isCustomerNameValid(model.customer_name))
                {
                    order.customer_name = model.customer_name;
                }

                if (!String.IsNullOrEmpty(model.gender))
                {
                    order.gender = model.gender.ToLower() == "others" ? "notitle" : model.gender.ToLower();
                }

                order.flat_number = model.flat_number;
                order.house_number = model.house_number;
                order.road_number = model.road_number;
                order.village = model.village;
                order.division_id = model.division_id;
                order.district_id = model.district_id;
                order.thana_id = model.thana_id;
                order.postal_code = String.IsNullOrEmpty(model.postal_code) ? "0" : model.postal_code;

                //=====temp modelfication (as RA apk is unable to update [15-12-19])============
                if (!String.IsNullOrEmpty(model.email))
                {
                    order.email = isEmailValid(model.email.ToLower()) == true ? model.email : String.Empty;
                }
                else
                {
                    order.email = String.Empty;
                }
                //===================

                order.retailer_id = model.retailer_id;
                //note: For retailer App dest_doc_id and src_doc_id is dest_nid and src_nid namely. 
                order.dest_left_thumb_score = model.dest_left_thumb_score.Equals(null) ? (decimal?)null : model.dest_left_thumb_score;
                order.dest_left_thumb = Array.Empty<byte>();
                order.dest_left_index_score = model.dest_left_index_score.Equals(null) ? (decimal?)null : model.dest_left_index_score;
                order.dest_left_index = Array.Empty<byte>();
                order.dest_right_thumb_score = model.dest_right_thumb_score.Equals(null) ? (decimal?)null : model.dest_right_thumb_score;
                order.dest_right_index = Array.Empty<byte>();
                order.dest_right_index_score = model.dest_right_index_score.Equals(null) ? (decimal?)null : model.dest_right_index_score;
                order.dest_right_thumb = Array.Empty<byte>();
                order.src_left_thumb_score = model.src_left_thumb_score.Equals(null) ? (decimal?)null : model.src_left_thumb_score;
                order.src_left_thumb = Array.Empty<byte>();
                order.src_left_index_score = model.src_left_index_score.Equals(null) ? (decimal?)null : model.src_left_index_score;
                order.src_left_index = Array.Empty<byte>();
                order.src_right_thumb_score = model.src_right_thumb_score.Equals(null) ? (decimal?)null : model.src_right_thumb_score;
                order.src_right_thumb = Array.Empty<byte>();
                order.src_right_index_score = model.src_right_index_score.Equals(null) ? (decimal?)null : model.src_right_index_score;
                order.src_right_index = Array.Empty<byte>();
                order.retailer_code = (model.channel_name == FixedValueCollection.ResellerChannel
                                                            || model.channel_name == FixedValueCollection.CorporateChannel
                                                            || model.channel_name == FixedValueCollection.SMEChannel)
                                                            ? FixedValueCollection.ResellerCodeText + model.retailer_id : model.retailer_id;
                order.port_in_date = String.IsNullOrEmpty(model.port_in_date) ? null : DateTime.Parse(model.port_in_date);
                order.alt_msisdn = model.alt_msisdn;

                if (!String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    if (model.poc_msisdn_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                    {
                        order.poc_number = FixedValueCollection.MSISDNCountryCode + model.poc_msisdn_number;
                    }
                    else
                    {
                        order.poc_number = model.poc_msisdn_number;
                    }
                }
                order.is_urgent = model.is_urgent;
                order.optional1 = model.optional1;
                order.optional2 = model.optional2;
                order.optional3 = model.otp; // For any kind of activation is processed through OTP then OTP will be insert in option3.
                order.optional4 = String.IsNullOrEmpty(model.optional4) ? null : Convert.ToDecimal(model.optional4.Trim());
                order.optional5 = String.IsNullOrEmpty(model.optional5) ? null : Convert.ToDecimal(model.optional5.Trim());
                order.optional6 = String.IsNullOrEmpty(model.optional6) ? null : Convert.ToDecimal(model.optional6.Trim());
                order.note = model.note;
                order.sim_rep_reason_id = String.IsNullOrEmpty(model.sim_rep_reason_id) ? null : Convert.ToDecimal(model.sim_rep_reason_id.Trim());
                order.payment_type = model.payment_type ?? "";
                order.is_paired = model.is_paired.Equals(null) ? null : model.is_paired;
                order.cahnnel_id = model.channel_id.Equals(null) ? null : model.channel_id;

                order.division_name = String.IsNullOrEmpty(model.division_name) ? model.division_name : ConverterHelper.UpperLowerWithSpaceConverter(model.division_name);
                order.district_name = String.IsNullOrEmpty(model.district_name) ? model.district_name : ConverterHelper.UpperLowerWithSpaceConverter(model.district_name);
                order.thana_name = String.IsNullOrEmpty(model.thana_name) ? model.thana_name : ConverterHelper.UpperLowerWithSpaceConverter(model.thana_name);

                //here distributor code is mapped with "P_CENTER_OR_DISTRIBUTOR_CODE" in DB. 
                order.distributor_code = model.distributor_code; // String.IsNullOrEmpty(model.distributor_code) ? await _bllCommon.GetDistributorCodeFromSessionTokenV2(model.session_token, model.retailer_id) : model.distributor_code;
                order.sim_replc_reason = model.sim_replc_reason;
                order.channel_name = model.channel_name;
                order.right_id = model.right_id.Equals(null) ? null : model.right_id;

                order.sim_replacement_type = model.sim_replacement_type;

                order.old_sim_number = String.IsNullOrEmpty(model.old_sim_number) ? null : model.old_sim_number;

                order.src_sim_category = model.src_sim_category;
                order.port_in_confirmation_code = model.port_in_confirmation_code;

                order.dest_ec_verifi_reqrd = !String.IsNullOrEmpty(model.dest_nid) ? 1 : 0;// 

                //If any trunsection porpuse have done by OTP, then the bio verification for src customer is not needed.
                //For example: For "two party EC verification" if the purpose is "B2B to B2C" with OTP, then the value "src_ec_verifi_reqrd" will 0.    
                //Here reseller app is sending OTP in "optional3" for "Corporate To Individual Transfer with OTP" [03 MAY 2020]
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.CorporateToIndividualTransfer
                    && !String.IsNullOrEmpty(model.otp))
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();
                }
                else
                {
                    order.src_ec_verifi_reqrd = !String.IsNullOrEmpty(model.src_nid) ? 1 : 0;
                }
                #region One Party EC Verification
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer
                   && model.src_ec_verifi_reqrd == 0)
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();

                }
                #endregion
                order.dest_foreign_flag = model.dest_foreign_flag;
                order.dbss_subscription_id = model.dbss_subscription_id.Equals(null) ? null : model.dbss_subscription_id;
                order.customer_id = model.customer_id;
                order.order_confirmation_code = model.order_confirmation_code;
                order.server_name = Environment.MachineName;

                //for sim repalcement customer update is enabled here.
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMReplacement &&
                    model.saf_status.HasValue)
                {
                    order.saf_status = model.saf_status == false ? 1 : 0;
                }
                else if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                            String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    order.saf_status = 1;
                }
                else
                {
                    order.saf_status = 0;
                }
                if (!String.IsNullOrEmpty(model.msisdnReservationId))
                {
                    order.msisdnReservationId = model.msisdnReservationId;
                }

                order.src_owner_customer_id = model.src_owner_customer_id;
                order.src_user_customer_id = model.src_user_customer_id;
                order.src_payer_customer_id = model.src_payer_customer_id;
                order.dest_imsi = model.dest_imsi;
                order.status = model.status;
                order.bss_reqId = model.bss_reqId;
                order.error_id = model.error_id;
                order.error_description = model.err_msg;

                order.loginAttemptId = LoginProviderInfo.login_attempt_id;
                order.latitude = model.latitude;
                order.longitude = model.longitude;

                if (model.lac != null)
                {
                    order.lac = (int)model.lac;
                }

                if (model.cid != null)
                {
                    order.cid = (int)model.cid;
                }

                order.scanner_id = model.scanner_id;

                if (model.order_booking_flag > 0 || model.order_booking_flag > -1)
                {
                    order.order_booking_flag = model.order_booking_flag;
                }

                order.is_esim = model.is_esim;
                order.prov_id = model.prov_id;


                return order;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<HomeWifiOrderRequest2> HomeWifiPopulateOrderData(HomeWifiOrderRequest model)
        {
            HomeWifiOrderRequest2 order = new HomeWifiOrderRequest2();
            BL_Json blJson = new BL_Json();
            try
            {
                if (model.msisdn.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                }
                order.bi_token_number = model.bi_token_number == null ? 0 : model.bi_token_number;
                order.purpose_number = String.IsNullOrEmpty(model.purpose_number) ? null : Convert.ToDecimal(model.purpose_number.Trim());
                order.msisdn = model.msisdn;

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_category = model.src_sim_category;
                else
                    order.sim_category = model.sim_category;

                if (!String.IsNullOrEmpty(model.sim_number))
                {
                    if (model.sim_number.Substring(0, 6) != FixedValueCollection.SIMCode)
                    {
                        model.sim_number = FixedValueCollection.SIMCode + model.sim_number;
                    }
                }

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_number = model.old_sim_number != null ? model.old_sim_number.Trim() : "";
                else if (!String.IsNullOrEmpty(model.sim_number))
                    order.sim_number = model.sim_number.Trim();
                else
                    order.sim_number = model.sim_number;

                order.subscription_type_id = String.IsNullOrEmpty(model.subscription_type_id) ? null : Convert.ToDecimal(model.subscription_type_id.Trim());
                order.subscription_code = model.subscription_code;
                order.package_id = String.IsNullOrEmpty(model.package_id) ? null : Convert.ToDecimal(model.package_id.Trim());
                order.package_code = model.package_code;
                if (!String.IsNullOrEmpty(model.dest_nid) && !String.IsNullOrEmpty(model.dest_dob))
                {
                    order.dest_nid = (model.dest_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.dest_dob).Year + model.dest_nid : model.dest_nid;
                }

                if (!String.IsNullOrEmpty(model.src_nid) && !String.IsNullOrEmpty(model.src_dob))
                {
                    order.src_nid = (model.src_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.src_dob).Year + model.src_nid : model.src_nid;
                }

                order.dest_dob = String.IsNullOrEmpty(model.dest_dob) ? String.Empty : DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.dest_nid))
                {
                    order.dest_doc_type_no = model.dest_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;
                }

                order.src_dob = String.IsNullOrEmpty(model.src_dob) ? String.Empty : DateTime.Parse(model.src_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.src_nid))
                {
                    order.src_doc_type_no = model.src_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;//(int)DOCTypeNo.nid;//1 for nid. /*model.src_doc_type_no;*/
                }
                else
                {
                    order.src_doc_type_no = null;
                }
                order.platform_id = model.platform_id;

                if (!String.IsNullOrEmpty(model.customer_name) &&
                        isCustomerNameValid(model.customer_name))
                {
                    order.customer_name = model.customer_name;
                }

                if (!String.IsNullOrEmpty(model.gender))
                {
                    order.gender = model.gender.ToLower() == "others" ? "notitle" : model.gender.ToLower();
                }

                order.flat_number = model.flat_number;
                order.house_number = model.house_number;
                order.road_number = model.road_number;
                order.village = model.village;
                order.division_id = model.division_id;
                order.district_id = model.district_id;
                order.thana_id = model.thana_id;
                order.postal_code = String.IsNullOrEmpty(model.postal_code) ? "0" : model.postal_code;

                //=====temp modelfication (as RA apk is unable to update [15-12-19])============
                if (!String.IsNullOrEmpty(model.email))
                {
                    order.email = isEmailValid(model.email.ToLower()) == true ? model.email : String.Empty;
                }
                else
                {
                    order.email = String.Empty;
                }
                //===================

                order.retailer_id = model.retailer_id;
                //note: For retailer App dest_doc_id and src_doc_id is dest_nid and src_nid namely. 
                order.dest_left_thumb_score = model.dest_left_thumb_score.Equals(null) ? (decimal?)null : model.dest_left_thumb_score;
                order.dest_left_thumb = Array.Empty<byte>();
                order.dest_left_index_score = model.dest_left_index_score.Equals(null) ? (decimal?)null : model.dest_left_index_score;
                order.dest_left_index = Array.Empty<byte>();
                order.dest_right_thumb_score = model.dest_right_thumb_score.Equals(null) ? (decimal?)null : model.dest_right_thumb_score;
                order.dest_right_index = Array.Empty<byte>();
                order.dest_right_index_score = model.dest_right_index_score.Equals(null) ? (decimal?)null : model.dest_right_index_score;
                order.dest_right_thumb = Array.Empty<byte>();
                order.src_left_thumb_score = model.src_left_thumb_score.Equals(null) ? (decimal?)null : model.src_left_thumb_score;
                order.src_left_thumb = Array.Empty<byte>();
                order.src_left_index_score = model.src_left_index_score.Equals(null) ? (decimal?)null : model.src_left_index_score;
                order.src_left_index = Array.Empty<byte>();
                order.src_right_thumb_score = model.src_right_thumb_score.Equals(null) ? (decimal?)null : model.src_right_thumb_score;
                order.src_right_thumb = Array.Empty<byte>();
                order.src_right_index_score = model.src_right_index_score.Equals(null) ? (decimal?)null : model.src_right_index_score;
                order.src_right_index = Array.Empty<byte>();
                order.retailer_code = (model.channel_name == FixedValueCollection.ResellerChannel
                                                            || model.channel_name == FixedValueCollection.CorporateChannel
                                                            || model.channel_name == FixedValueCollection.SMEChannel)
                                                            ? FixedValueCollection.ResellerCodeText + model.retailer_id : model.retailer_id;
                order.port_in_date = String.IsNullOrEmpty(model.port_in_date) ? null : DateTime.Parse(model.port_in_date);
                order.alt_msisdn = model.alt_msisdn;

                if (!String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    if (model.poc_msisdn_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                    {
                        order.poc_number = FixedValueCollection.MSISDNCountryCode + model.poc_msisdn_number;
                    }
                    else
                    {
                        order.poc_number = model.poc_msisdn_number;
                    }
                }
                order.is_urgent = model.is_urgent;
                order.optional1 = model.optional1;
                order.optional2 = model.optional2;
                order.optional3 = model.otp; // For any kind of activation is processed through OTP then OTP will be insert in option3.
                order.optional4 = String.IsNullOrEmpty(model.optional4) ? null : Convert.ToDecimal(model.optional4.Trim());
                order.optional5 = String.IsNullOrEmpty(model.optional5) ? null : Convert.ToDecimal(model.optional5.Trim());
                order.optional6 = String.IsNullOrEmpty(model.optional6) ? null : Convert.ToDecimal(model.optional6.Trim());
                order.note = model.note;
                order.sim_rep_reason_id = String.IsNullOrEmpty(model.sim_rep_reason_id) ? null : Convert.ToDecimal(model.sim_rep_reason_id.Trim());
                order.payment_type = model.payment_type ?? "";
                order.is_paired = model.is_paired.Equals(null) ? null : model.is_paired;
                order.cahnnel_id = model.channel_id.Equals(null) ? null : model.channel_id;

                order.division_name = String.IsNullOrEmpty(model.division_name) ? model.division_name : ConverterHelper.UpperLowerWithSpaceConverter(model.division_name);
                order.district_name = String.IsNullOrEmpty(model.district_name) ? model.district_name : ConverterHelper.UpperLowerWithSpaceConverter(model.district_name);
                order.thana_name = String.IsNullOrEmpty(model.thana_name) ? model.thana_name : ConverterHelper.UpperLowerWithSpaceConverter(model.thana_name);

                //here distributor code is mapped with "P_CENTER_OR_DISTRIBUTOR_CODE" in DB. 
                order.distributor_code = model.distributor_code; // String.IsNullOrEmpty(model.distributor_code) ? await _bllCommon.GetDistributorCodeFromSessionTokenV2(model.session_token, model.retailer_id) : model.distributor_code;
                order.sim_replc_reason = model.sim_replc_reason;
                order.channel_name = model.channel_name;
                order.right_id = model.right_id.Equals(null) ? null : model.right_id;

                order.sim_replacement_type = model.sim_replacement_type;

                order.old_sim_number = String.IsNullOrEmpty(model.old_sim_number) ? null : model.old_sim_number;

                order.src_sim_category = model.src_sim_category;
                order.port_in_confirmation_code = model.port_in_confirmation_code;

                order.dest_ec_verifi_reqrd = !String.IsNullOrEmpty(model.dest_nid) ? 1 : 0;// 

                //If any trunsection porpuse have done by OTP, then the bio verification for src customer is not needed.
                //For example: For "two party EC verification" if the purpose is "B2B to B2C" with OTP, then the value "src_ec_verifi_reqrd" will 0.    
                //Here reseller app is sending OTP in "optional3" for "Corporate To Individual Transfer with OTP" [03 MAY 2020]
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.CorporateToIndividualTransfer
                    && !String.IsNullOrEmpty(model.otp))
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();
                }
                else
                {
                    order.src_ec_verifi_reqrd = !String.IsNullOrEmpty(model.src_nid) ? 1 : 0;
                }
                #region One Party EC Verification
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer
                   && model.src_ec_verifi_reqrd == 0)
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();

                }
                #endregion
                order.dest_foreign_flag = model.dest_foreign_flag;
                order.dbss_subscription_id = model.dbss_subscription_id.Equals(null) ? null : model.dbss_subscription_id;
                order.customer_id = model.customer_id;
                order.order_confirmation_code = model.order_confirmation_code;
                order.server_name = Environment.MachineName;

                //for sim repalcement customer update is enabled here.
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMReplacement &&
                    model.saf_status.HasValue)
                {
                    order.saf_status = model.saf_status == false ? 1 : 0;
                }
                else if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                            String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    order.saf_status = 1;
                }
                else
                {
                    order.saf_status = 0;
                }
                if (!String.IsNullOrEmpty(model.msisdnReservationId))
                {
                    order.msisdnReservationId = model.msisdnReservationId;
                }

                order.src_owner_customer_id = model.src_owner_customer_id;
                order.src_user_customer_id = model.src_user_customer_id;
                order.src_payer_customer_id = model.src_payer_customer_id;
                order.dest_imsi = model.dest_imsi;
                order.status = model.status;
                order.bss_reqId = model.bss_reqId;
                order.error_id = model.error_id;
                order.error_description = model.err_msg;

                order.loginAttemptId = LoginProviderInfo.login_attempt_id;
                order.latitude = model.latitude;
                order.longitude = model.longitude;

                if (model.lac != null)
                {
                    order.lac = (int)model.lac;
                }

                if (model.cid != null)
                {
                    order.cid = (int)model.cid;
                }

                order.scanner_id = model.scanner_id;

                if (model.order_booking_flag > 0 || model.order_booking_flag > -1)
                {
                    order.order_booking_flag = model.order_booking_flag;
                }

                order.is_esim = model.is_esim;
                order.prov_id = model.prov_id;
                order.initiator_channel = model.initiator_channel;
                order.order_type = model.order_type;
                order.subscription_type = model.subscription_type;
                order.simkit_type = model.simkit_type;
                order.order_number = model.order_number;

                return order;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<OrderRequest3> populateOrderDataV5(RAOrderRequestV2 model)
        {
            OrderRequest3 order = new OrderRequest3();
            BL_Json blJson = new BL_Json();
            try
            {
                if (model.msisdn.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                }
                order.bi_token_number = model.bi_token_number == null ? 0 : model.bi_token_number;
                order.purpose_number = String.IsNullOrEmpty(model.purpose_number) ? null : Convert.ToDecimal(model.purpose_number.Trim());
                order.msisdn = model.msisdn;

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_category = model.src_sim_category;
                else
                    order.sim_category = model.sim_category;

                if (!String.IsNullOrEmpty(model.sim_number))
                {
                    if (model.sim_number.Substring(0, 6) != FixedValueCollection.SIMCode)
                    {
                        model.sim_number = FixedValueCollection.SIMCode + model.sim_number;
                    }
                }

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_number = model.old_sim_number != null ? model.old_sim_number.Trim() : "";
                else if (!String.IsNullOrEmpty(model.sim_number))
                    order.sim_number = model.sim_number.Trim();
                else
                    order.sim_number = model.sim_number;

                order.subscription_type_id = String.IsNullOrEmpty(model.subscription_type_id) ? null : Convert.ToDecimal(model.subscription_type_id.Trim());
                order.subscription_code = model.subscription_code;
                order.package_id = String.IsNullOrEmpty(model.package_id) ? null : Convert.ToDecimal(model.package_id.Trim());
                order.package_code = model.package_code;
                if (!String.IsNullOrEmpty(model.dest_nid) && !String.IsNullOrEmpty(model.dest_dob))
                {
                    order.dest_nid = (model.dest_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.dest_dob).Year + model.dest_nid : model.dest_nid;
                }

                if (!String.IsNullOrEmpty(model.src_nid) && !String.IsNullOrEmpty(model.src_dob))
                {
                    order.src_nid = (model.src_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.src_dob).Year + model.src_nid : model.src_nid;
                }

                order.dest_dob = String.IsNullOrEmpty(model.dest_dob) ? String.Empty : DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.dest_nid))
                {
                    order.dest_doc_type_no = model.dest_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;
                }

                order.src_dob = String.IsNullOrEmpty(model.src_dob) ? String.Empty : DateTime.Parse(model.src_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.src_nid))
                {
                    order.src_doc_type_no = model.src_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;//(int)DOCTypeNo.nid;//1 for nid. /*model.src_doc_type_no;*/
                }
                else
                {
                    order.src_doc_type_no = null;
                }
                order.platform_id = model.platform_id;

                if (!String.IsNullOrEmpty(model.customer_name) &&
                        isCustomerNameValid(model.customer_name))
                {
                    order.customer_name = model.customer_name;
                }

                if (!String.IsNullOrEmpty(model.gender))
                {
                    order.gender = model.gender.ToLower() == "others" ? "notitle" : model.gender.ToLower();
                }

                order.flat_number = model.flat_number;
                order.house_number = model.house_number;
                order.road_number = model.road_number;
                order.village = model.village;
                order.division_id = model.division_id;
                order.district_id = model.district_id;
                order.thana_id = model.thana_id;
                order.postal_code = String.IsNullOrEmpty(model.postal_code) ? "0" : model.postal_code;

                //=====temp modelfication (as RA apk is unable to update [15-12-19])============
                if (!String.IsNullOrEmpty(model.email))
                {
                    order.email = isEmailValid(model.email.ToLower()) == true ? model.email : String.Empty;
                }
                else
                {
                    order.email = String.Empty;
                }
                //===================

                order.retailer_id = model.retailer_id;
                //note: For retailer App dest_doc_id and src_doc_id is dest_nid and src_nid namely. 
                order.dest_left_thumb_score = model.dest_left_thumb_score.Equals(null) ? (decimal?)null : model.dest_left_thumb_score;
                order.dest_left_thumb = Array.Empty<byte>();
                order.dest_left_index_score = model.dest_left_index_score.Equals(null) ? (decimal?)null : model.dest_left_index_score;
                order.dest_left_index = Array.Empty<byte>();
                order.dest_right_thumb_score = model.dest_right_thumb_score.Equals(null) ? (decimal?)null : model.dest_right_thumb_score;
                order.dest_right_index = Array.Empty<byte>();
                order.dest_right_index_score = model.dest_right_index_score.Equals(null) ? (decimal?)null : model.dest_right_index_score;
                order.dest_right_thumb = Array.Empty<byte>();
                order.src_left_thumb_score = model.src_left_thumb_score.Equals(null) ? (decimal?)null : model.src_left_thumb_score;
                order.src_left_thumb = Array.Empty<byte>();
                order.src_left_index_score = model.src_left_index_score.Equals(null) ? (decimal?)null : model.src_left_index_score;
                order.src_left_index = Array.Empty<byte>();
                order.src_right_thumb_score = model.src_right_thumb_score.Equals(null) ? (decimal?)null : model.src_right_thumb_score;
                order.src_right_thumb = Array.Empty<byte>();
                order.src_right_index_score = model.src_right_index_score.Equals(null) ? (decimal?)null : model.src_right_index_score;
                order.src_right_index = Array.Empty<byte>();
                order.retailer_code = (model.channel_name == FixedValueCollection.ResellerChannel
                                                            || model.channel_name == FixedValueCollection.CorporateChannel
                                                            || model.channel_name == FixedValueCollection.SMEChannel)
                                                            ? FixedValueCollection.ResellerCodeText + model.retailer_id : model.retailer_id;
                order.port_in_date = String.IsNullOrEmpty(model.port_in_date) ? null : DateTime.Parse(model.port_in_date);
                order.alt_msisdn = model.alt_msisdn;

                if (!String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    if (model.poc_msisdn_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                    {
                        order.poc_number = FixedValueCollection.MSISDNCountryCode + model.poc_msisdn_number;
                    }
                    else
                    {
                        order.poc_number = model.poc_msisdn_number;
                    }
                }
                order.is_urgent = model.is_urgent;
                order.optional1 = model.optional1;
                order.optional2 = model.optional2;
                order.optional3 = model.otp; // For any kind of activation is processed through OTP then OTP will be insert in option3.
                order.optional4 = String.IsNullOrEmpty(model.optional4) ? null : Convert.ToDecimal(model.optional4.Trim());
                order.optional5 = String.IsNullOrEmpty(model.optional5) ? null : Convert.ToDecimal(model.optional5.Trim());
                order.optional6 = String.IsNullOrEmpty(model.optional6) ? null : Convert.ToDecimal(model.optional6.Trim());
                order.note = model.note;
                order.sim_rep_reason_id = String.IsNullOrEmpty(model.sim_rep_reason_id) ? null : Convert.ToDecimal(model.sim_rep_reason_id.Trim());
                order.payment_type = model.payment_type ?? "";
                order.is_paired = model.is_paired.Equals(null) ? null : model.is_paired;
                order.cahnnel_id = model.channel_id.Equals(null) ? null : model.channel_id;

                order.division_name = String.IsNullOrEmpty(model.division_name) ? model.division_name : ConverterHelper.UpperLowerWithSpaceConverter(model.division_name);
                order.district_name = String.IsNullOrEmpty(model.district_name) ? model.district_name : ConverterHelper.UpperLowerWithSpaceConverter(model.district_name);
                order.thana_name = String.IsNullOrEmpty(model.thana_name) ? model.thana_name : ConverterHelper.UpperLowerWithSpaceConverter(model.thana_name);

                //here distributor code is mapped with "P_CENTER_OR_DISTRIBUTOR_CODE" in DB. 
                order.distributor_code = model.distributor_code; // String.IsNullOrEmpty(model.distributor_code) ? await _bllCommon.GetDistributorCodeFromSessionTokenV2(model.session_token, model.retailer_id) : model.distributor_code;
                order.sim_replc_reason = model.sim_replc_reason;
                order.channel_name = model.channel_name;
                order.right_id = model.right_id.Equals(null) ? null : model.right_id;

                order.sim_replacement_type = model.sim_replacement_type;

                order.old_sim_number = String.IsNullOrEmpty(model.old_sim_number) ? null : model.old_sim_number;

                order.src_sim_category = model.src_sim_category;
                order.port_in_confirmation_code = model.port_in_confirmation_code;

                order.dest_ec_verifi_reqrd = !String.IsNullOrEmpty(model.dest_nid) ? 1 : 0;// 

                //If any trunsection porpuse have done by OTP, then the bio verification for src customer is not needed.
                //For example: For "two party EC verification" if the purpose is "B2B to B2C" with OTP, then the value "src_ec_verifi_reqrd" will 0.    
                //Here reseller app is sending OTP in "optional3" for "Corporate To Individual Transfer with OTP" [03 MAY 2020]
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.CorporateToIndividualTransfer
                    && !String.IsNullOrEmpty(model.otp))
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();
                }
                else
                {
                    order.src_ec_verifi_reqrd = !String.IsNullOrEmpty(model.src_nid) ? 1 : 0;
                }
                #region One Party EC Verification
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer
                   && model.src_ec_verifi_reqrd == 0)
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();

                }
                #endregion
                order.dest_foreign_flag = model.dest_foreign_flag;
                order.dbss_subscription_id = model.dbss_subscription_id.Equals(null) ? null : model.dbss_subscription_id;
                order.customer_id = model.customer_id;
                order.order_confirmation_code = model.order_confirmation_code;
                order.server_name = Environment.MachineName;

                //for sim repalcement customer update is enabled here.
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMReplacement &&
                    model.saf_status.HasValue)
                {
                    order.saf_status = model.saf_status == false ? 1 : 0;
                }
                else if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                            String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    order.saf_status = 1;
                }
                else
                {
                    order.saf_status = 0;
                }
                if (!String.IsNullOrEmpty(model.msisdnReservationId))
                {
                    order.msisdnReservationId = model.msisdnReservationId;
                }

                order.src_owner_customer_id = model.src_owner_customer_id;
                order.src_user_customer_id = model.src_user_customer_id;
                order.src_payer_customer_id = model.src_payer_customer_id;
                order.dest_imsi = model.dest_imsi;
                order.status = model.status;
                order.bss_reqId = model.bss_reqId;
                order.error_id = model.error_id;
                order.error_description = model.err_msg;

                order.loginAttemptId = LoginProviderInfo.login_attempt_id;
                order.latitude = model.latitude;
                order.longitude = model.longitude;

                if (model.lac != null)
                {
                    order.lac = (int)model.lac;
                }

                if (model.cid != null)
                {
                    order.cid = (int)model.cid;
                }

                order.scanner_id = model.scanner_id;

                if (model.order_booking_flag > 0 || model.order_booking_flag > -1)
                {
                    order.order_booking_flag = model.order_booking_flag;
                }

                order.is_esim = model.is_esim;
                order.prov_id = model.prov_id;
                if (model.is_lus == true)
                {
                    order.is_lus = 1;
                }
                else
                {
                    order.is_lus = 0;
                }
                order.btsCode = model.bts_code;
                order.selected_category = model.selected_category;
                return order;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<HomeWifiOrderRequest2> HomeWifiNewConnectionPopulateOrderData(HomeWifiOrderRequest model)
        {            

            HomeWifiOrderRequest2 order = new HomeWifiOrderRequest2();
            BL_Json blJson = new BL_Json();
            try
            {
                if (model.msisdn.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                }
                order.bi_token_number = model.bi_token_number == null ? 0 : model.bi_token_number;
                order.purpose_number = String.IsNullOrEmpty(model.purpose_number) ? null : Convert.ToDecimal(model.purpose_number.Trim());
                order.msisdn = model.msisdn;

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_category = model.src_sim_category;
                else
                    order.sim_category = model.sim_category;

                if (!String.IsNullOrEmpty(model.sim_number))
                {
                    if (model.sim_number.Substring(0, 6) != FixedValueCollection.SIMCode)
                    {
                        model.sim_number = FixedValueCollection.SIMCode + model.sim_number;
                    }
                }

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_number = model.old_sim_number != null ? model.old_sim_number.Trim() : "";
                else if (!String.IsNullOrEmpty(model.sim_number))
                    order.sim_number = model.sim_number.Trim();
                else
                    order.sim_number = model.sim_number;

                order.subscription_type_id = String.IsNullOrEmpty(model.subscription_type_id) ? null : Convert.ToDecimal(model.subscription_type_id.Trim());
                order.subscription_code = model.subscription_code;
                order.package_id = String.IsNullOrEmpty(model.package_id) ? null : Convert.ToDecimal(model.package_id.Trim());
                order.package_code = model.package_code;
                if (!String.IsNullOrEmpty(model.dest_nid) && !String.IsNullOrEmpty(model.dest_dob))
                {
                    order.dest_nid = (model.dest_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.dest_dob).Year + model.dest_nid : model.dest_nid;
                }

                if (!String.IsNullOrEmpty(model.src_nid) && !String.IsNullOrEmpty(model.src_dob))
                {
                    order.src_nid = (model.src_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.src_dob).Year + model.src_nid : model.src_nid;
                }

                order.dest_dob = String.IsNullOrEmpty(model.dest_dob) ? String.Empty : DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.dest_nid))
                {
                    order.dest_doc_type_no = model.dest_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;
                }

                order.src_dob = String.IsNullOrEmpty(model.src_dob) ? String.Empty : DateTime.Parse(model.src_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.src_nid))
                {
                    order.src_doc_type_no = model.src_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;//(int)DOCTypeNo.nid;//1 for nid. /*model.src_doc_type_no;*/
                }
                else
                {
                    order.src_doc_type_no = null;
                }
                order.platform_id = model.platform_id;

                if (!String.IsNullOrEmpty(model.customer_name) &&
                        isCustomerNameValid(model.customer_name))
                {
                    order.customer_name = model.customer_name;
                }

                if (!String.IsNullOrEmpty(model.gender))
                {
                    order.gender = model.gender.ToLower() == "others" ? "notitle" : model.gender.ToLower();
                }

                order.flat_number = model.flat_number;
                order.house_number = model.house_number;
                order.road_number = model.road_number;
                order.village = model.village;
                order.division_id = model.division_id;
                order.district_id = model.district_id;
                order.thana_id = model.thana_id;
                order.postal_code = String.IsNullOrEmpty(model.postal_code) ? "0" : model.postal_code;

                //=====temp modelfication (as RA apk is unable to update [15-12-19])============
                if (!String.IsNullOrEmpty(model.email))
                {
                    order.email = isEmailValid(model.email.ToLower()) == true ? model.email : String.Empty;
                }
                else
                {
                    order.email = String.Empty;
                }
                //===================

                order.retailer_id = model.retailer_id;
                //note: For retailer App dest_doc_id and src_doc_id is dest_nid and src_nid namely. 
                order.dest_left_thumb_score = model.dest_left_thumb_score.Equals(null) ? (decimal?)null : model.dest_left_thumb_score;
                order.dest_left_thumb = Array.Empty<byte>();
                order.dest_left_index_score = model.dest_left_index_score.Equals(null) ? (decimal?)null : model.dest_left_index_score;
                order.dest_left_index = Array.Empty<byte>();
                order.dest_right_thumb_score = model.dest_right_thumb_score.Equals(null) ? (decimal?)null : model.dest_right_thumb_score;
                order.dest_right_index = Array.Empty<byte>();
                order.dest_right_index_score = model.dest_right_index_score.Equals(null) ? (decimal?)null : model.dest_right_index_score;
                order.dest_right_thumb = Array.Empty<byte>();
                order.src_left_thumb_score = model.src_left_thumb_score.Equals(null) ? (decimal?)null : model.src_left_thumb_score;
                order.src_left_thumb = Array.Empty<byte>();
                order.src_left_index_score = model.src_left_index_score.Equals(null) ? (decimal?)null : model.src_left_index_score;
                order.src_left_index = Array.Empty<byte>();
                order.src_right_thumb_score = model.src_right_thumb_score.Equals(null) ? (decimal?)null : model.src_right_thumb_score;
                order.src_right_thumb = Array.Empty<byte>();
                order.src_right_index_score = model.src_right_index_score.Equals(null) ? (decimal?)null : model.src_right_index_score;
                order.src_right_index = Array.Empty<byte>();
                order.retailer_code = (model.channel_name == FixedValueCollection.ResellerChannel
                                                            || model.channel_name == FixedValueCollection.CorporateChannel
                                                            || model.channel_name == FixedValueCollection.SMEChannel)
                                                            ? FixedValueCollection.ResellerCodeText + model.retailer_id : model.retailer_id;
                order.port_in_date = String.IsNullOrEmpty(model.port_in_date) ? null : DateTime.Parse(model.port_in_date);
                order.alt_msisdn = model.alt_msisdn;

                if (!String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    if (model.poc_msisdn_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                    {
                        order.poc_number = FixedValueCollection.MSISDNCountryCode + model.poc_msisdn_number;
                    }
                    else
                    {
                        order.poc_number = model.poc_msisdn_number;
                    }
                }
                order.is_urgent = model.is_urgent;
                order.optional1 = model.optional1;
                order.optional2 = model.optional2;
                order.optional3 = model.otp; // For any kind of activation is processed through OTP then OTP will be insert in option3.
                order.optional4 = String.IsNullOrEmpty(model.optional4) ? null : Convert.ToDecimal(model.optional4.Trim());
                order.optional5 = String.IsNullOrEmpty(model.optional5) ? null : Convert.ToDecimal(model.optional5.Trim());
                order.optional6 = String.IsNullOrEmpty(model.optional6) ? null : Convert.ToDecimal(model.optional6.Trim());
                order.note = model.note;
                order.sim_rep_reason_id = String.IsNullOrEmpty(model.sim_rep_reason_id) ? null : Convert.ToDecimal(model.sim_rep_reason_id.Trim());
                order.payment_type = model.payment_type ?? "";
                order.is_paired = model.is_paired.Equals(null) ? null : model.is_paired;
                order.cahnnel_id = model.channel_id.Equals(null) ? null : model.channel_id;

                order.division_name = String.IsNullOrEmpty(model.division_name) ? model.division_name : ConverterHelper.UpperLowerWithSpaceConverter(model.division_name);
                order.district_name = String.IsNullOrEmpty(model.district_name) ? model.district_name : ConverterHelper.UpperLowerWithSpaceConverter(model.district_name);
                order.thana_name = String.IsNullOrEmpty(model.thana_name) ? model.thana_name : ConverterHelper.UpperLowerWithSpaceConverter(model.thana_name);

                //here distributor code is mapped with "P_CENTER_OR_DISTRIBUTOR_CODE" in DB. 
                order.distributor_code = model.distributor_code;//String.IsNullOrEmpty(model.distributor_code) ? await _bllCommon.GetDistributorCodeFromSessionTokenV2(model.session_token, model.retailer_id) : model.distributor_code;
                order.sim_replc_reason = model.sim_replc_reason;
                order.channel_name = model.channel_name;
                order.right_id = model.right_id.Equals(null) ? null : model.right_id;

                order.sim_replacement_type = model.sim_replacement_type;

                order.old_sim_number = String.IsNullOrEmpty(model.old_sim_number) ? null : model.old_sim_number;

                order.src_sim_category = model.src_sim_category;
                order.port_in_confirmation_code = model.port_in_confirmation_code;

                order.dest_ec_verifi_reqrd = !String.IsNullOrEmpty(model.dest_nid) ? 1 : 0;// 

                //If any trunsection porpuse have done by OTP, then the bio verification for src customer is not needed.
                //For example: For "two party EC verification" if the purpose is "B2B to B2C" with OTP, then the value "src_ec_verifi_reqrd" will 0.    
                //Here reseller app is sending OTP in "optional3" for "Corporate To Individual Transfer with OTP" [03 MAY 2020]
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.CorporateToIndividualTransfer
                    && !String.IsNullOrEmpty(model.otp))
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();
                }
                else
                {
                    order.src_ec_verifi_reqrd = !String.IsNullOrEmpty(model.src_nid) ? 1 : 0;
                }
                #region One Party EC Verification
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer
                   && model.src_ec_verifi_reqrd == 0)
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();

                }
                #endregion
                order.dest_foreign_flag = model.dest_foreign_flag;
                order.dbss_subscription_id = model.dbss_subscription_id.Equals(null) ? null : model.dbss_subscription_id;
                order.customer_id = model.customer_id;
                order.order_confirmation_code = model.order_confirmation_code;
                order.server_name = Environment.MachineName;

                //for sim repalcement customer update is enabled here.
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMReplacement &&
                    model.saf_status.HasValue)
                {
                    order.saf_status = model.saf_status == false ? 1 : 0;
                }
                else if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                            String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    order.saf_status = 1;
                }
                else
                {
                    order.saf_status = 0;
                }
                if (!String.IsNullOrEmpty(model.msisdnReservationId))
                {
                    order.msisdnReservationId = model.msisdnReservationId;
                }

                order.src_owner_customer_id = model.src_owner_customer_id;
                order.src_user_customer_id = model.src_user_customer_id;
                order.src_payer_customer_id = model.src_payer_customer_id;
                order.dest_imsi = model.dest_imsi;
                order.status = model.status;
                order.bss_reqId = model.bss_reqId;
                order.error_id = model.error_id;
                order.error_description = model.err_msg;

                order.loginAttemptId = LoginProviderInfo.login_attempt_id;
                order.latitude = model.latitude;
                order.longitude = model.longitude;

                if (model.lac != null)
                {
                    order.lac = (int)model.lac;
                }

                if (model.cid != null)
                {
                    order.cid = (int)model.cid;
                }

                order.scanner_id = model.scanner_id;

                if (model.order_booking_flag > 0 || model.order_booking_flag > -1)
                {
                    order.order_booking_flag = model.order_booking_flag;
                }

                order.is_esim = model.is_esim;
                order.prov_id = model.prov_id;
                if (model.is_lus == true)
                {
                    order.is_lus = 1;
                }
                else
                {
                    order.is_lus = 0;
                }
                order.btsCode = model.bts_code;
                order.selected_category = model.selected_category;
                order.order_number = model.order_number;
                order.initiator_channel = model.initiator_channel;
                order.order_type = model.order_type;
                order.subscription_type = model.subscription_type;
                order.simkit_type = model.simkit_type;

                return order;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<OrderRequest3> populateOrderDataV3(RAOrderRequestV2 model)
        {
            OrderRequest3 order = new OrderRequest3();
            BL_Json blJson = new BL_Json();
            try
            {
                if (model.msisdn.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                }
                order.bi_token_number = model.bi_token_number == null ? 0 : model.bi_token_number;
                order.purpose_number = String.IsNullOrEmpty(model.purpose_number) ? null : Convert.ToDecimal(model.purpose_number.Trim());
                order.msisdn = model.msisdn;

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_category = model.src_sim_category;
                else
                    order.sim_category = model.sim_category;

                if (!String.IsNullOrEmpty(model.sim_number))
                {
                    if (model.sim_number.Substring(0, 6) != FixedValueCollection.SIMCode)
                    {
                        model.sim_number = FixedValueCollection.SIMCode + model.sim_number;
                    }
                }

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_number = model.old_sim_number != null ? model.old_sim_number.Trim() : "";
                else if (!String.IsNullOrEmpty(model.sim_number))
                    order.sim_number = model.sim_number.Trim();
                else
                    order.sim_number = model.sim_number;

                order.subscription_type_id = String.IsNullOrEmpty(model.subscription_type_id) ? null : Convert.ToDecimal(model.subscription_type_id.Trim());
                order.subscription_code = model.subscription_code;
                order.package_id = String.IsNullOrEmpty(model.package_id) ? null : Convert.ToDecimal(model.package_id.Trim());
                order.package_code = model.package_code;
                if (!String.IsNullOrEmpty(model.dest_nid) && !String.IsNullOrEmpty(model.dest_dob))
                {
                    order.dest_nid = (model.dest_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.dest_dob).Year + model.dest_nid : model.dest_nid;
                }

                if (!String.IsNullOrEmpty(model.src_nid) && !String.IsNullOrEmpty(model.src_dob))
                {
                    order.src_nid = (model.src_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.src_dob).Year + model.src_nid : model.src_nid;
                }

                order.dest_dob = String.IsNullOrEmpty(model.dest_dob) ? String.Empty : DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.dest_nid))
                {
                    order.dest_doc_type_no = model.dest_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;
                }

                order.src_dob = String.IsNullOrEmpty(model.src_dob) ? String.Empty : DateTime.Parse(model.src_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.src_nid))
                {
                    order.src_doc_type_no = model.src_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;//(int)DOCTypeNo.nid;//1 for nid. /*model.src_doc_type_no;*/
                }
                else
                {
                    order.src_doc_type_no = null;
                }
                order.platform_id = model.platform_id;

                if (!String.IsNullOrEmpty(model.customer_name) &&
                        isCustomerNameValid(model.customer_name))
                {
                    order.customer_name = model.customer_name;
                }

                if (!String.IsNullOrEmpty(model.gender))
                {
                    order.gender = model.gender.ToLower() == "others" ? "notitle" : model.gender.ToLower();
                }

                order.flat_number = model.flat_number;
                order.house_number = model.house_number;
                order.road_number = model.road_number;
                order.village = model.village;
                order.division_id = model.division_id;
                order.district_id = model.district_id;
                order.thana_id = model.thana_id;
                order.postal_code = String.IsNullOrEmpty(model.postal_code) ? "0" : model.postal_code;

                //=====temp modelfication (as RA apk is unable to update [15-12-19])============
                if (!String.IsNullOrEmpty(model.email))
                {
                    order.email = isEmailValid(model.email.ToLower()) == true ? model.email : String.Empty;
                }
                else
                {
                    order.email = String.Empty;
                }
                //===================

                order.retailer_id = model.retailer_id;
                //note: For retailer App dest_doc_id and src_doc_id is dest_nid and src_nid namely. 
                order.dest_left_thumb_score = model.dest_left_thumb_score.Equals(null) ? (decimal?)null : model.dest_left_thumb_score;
                order.dest_left_thumb = Array.Empty<byte>();
                order.dest_left_index_score = model.dest_left_index_score.Equals(null) ? (decimal?)null : model.dest_left_index_score;
                order.dest_left_index = Array.Empty<byte>();
                order.dest_right_thumb_score = model.dest_right_thumb_score.Equals(null) ? (decimal?)null : model.dest_right_thumb_score;
                order.dest_right_index = Array.Empty<byte>();
                order.dest_right_index_score = model.dest_right_index_score.Equals(null) ? (decimal?)null : model.dest_right_index_score;
                order.dest_right_thumb = Array.Empty<byte>();
                order.src_left_thumb_score = model.src_left_thumb_score.Equals(null) ? (decimal?)null : model.src_left_thumb_score;
                order.src_left_thumb = Array.Empty<byte>();
                order.src_left_index_score = model.src_left_index_score.Equals(null) ? (decimal?)null : model.src_left_index_score;
                order.src_left_index = Array.Empty<byte>();
                order.src_right_thumb_score = model.src_right_thumb_score.Equals(null) ? (decimal?)null : model.src_right_thumb_score;
                order.src_right_thumb = Array.Empty<byte>();
                order.src_right_index_score = model.src_right_index_score.Equals(null) ? (decimal?)null : model.src_right_index_score;
                order.src_right_index = Array.Empty<byte>();
                order.retailer_code = (model.channel_name == FixedValueCollection.ResellerChannel
                                                            || model.channel_name == FixedValueCollection.CorporateChannel
                                                            || model.channel_name == FixedValueCollection.SMEChannel)
                                                            ? FixedValueCollection.ResellerCodeText + model.retailer_id : model.retailer_id;
                order.port_in_date = String.IsNullOrEmpty(model.port_in_date) ? null : DateTime.Parse(model.port_in_date);
                order.alt_msisdn = model.alt_msisdn;

                if (!String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    if (model.poc_msisdn_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                    {
                        order.poc_number = FixedValueCollection.MSISDNCountryCode + model.poc_msisdn_number;
                    }
                    else
                    {
                        order.poc_number = model.poc_msisdn_number;
                    }
                }
                order.is_urgent = model.is_urgent;
                order.optional1 = model.optional1;
                order.optional2 = model.optional2;
                order.optional3 = model.otp; // For any kind of activation is processed through OTP then OTP will be insert in option3.
                order.optional4 = String.IsNullOrEmpty(model.optional4) ? null : Convert.ToDecimal(model.optional4.Trim());
                order.optional5 = String.IsNullOrEmpty(model.optional5) ? null : Convert.ToDecimal(model.optional5.Trim());
                order.optional6 = String.IsNullOrEmpty(model.optional6) ? null : Convert.ToDecimal(model.optional6.Trim());
                order.note = model.note;
                order.sim_rep_reason_id = String.IsNullOrEmpty(model.sim_rep_reason_id) ? null : Convert.ToDecimal(model.sim_rep_reason_id.Trim());
                order.payment_type = model.payment_type ?? "";
                order.is_paired = model.is_paired;
                order.cahnnel_id = model.channel_id;

                order.division_name = String.IsNullOrEmpty(model.division_name) ? model.division_name : ConverterHelper.UpperLowerWithSpaceConverter(model.division_name);
                order.district_name = String.IsNullOrEmpty(model.district_name) ? model.district_name : ConverterHelper.UpperLowerWithSpaceConverter(model.district_name);
                order.thana_name = String.IsNullOrEmpty(model.thana_name) ? model.thana_name : ConverterHelper.UpperLowerWithSpaceConverter(model.thana_name);

                //here distributor code is mapped with "P_CENTER_OR_DISTRIBUTOR_CODE" in DB. 
                order.distributor_code = model.distributor_code; // String.IsNullOrEmpty(model.distributor_code) ? await _bllCommon.GetDistributorCodeFromSessionTokenV2(model.session_token, model.retailer_id) : model.distributor_code;
                order.sim_replc_reason = model.sim_replc_reason;
                order.channel_name = model.channel_name;
                order.right_id = model.right_id;

                order.sim_replacement_type = model.sim_replacement_type;

                order.old_sim_number = String.IsNullOrEmpty(model.old_sim_number) ? null : model.old_sim_number;

                order.src_sim_category = model.src_sim_category;
                order.port_in_confirmation_code = model.port_in_confirmation_code;

                order.dest_ec_verifi_reqrd = !String.IsNullOrEmpty(model.dest_nid) ? 1 : 0;// 

                //If any trunsection porpuse have done by OTP, then the bio verification for src customer is not needed.
                //For example: For "two party EC verification" if the purpose is "B2B to B2C" with OTP, then the value "src_ec_verifi_reqrd" will 0.    
                //Here reseller app is sending OTP in "optional3" for "Corporate To Individual Transfer with OTP" [03 MAY 2020]
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.CorporateToIndividualTransfer
                    && !String.IsNullOrEmpty(model.otp))
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();
                }
                else
                {
                    order.src_ec_verifi_reqrd = !String.IsNullOrEmpty(model.src_nid) ? 1 : 0;
                }
                #region One Party EC Verification
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer
                   && model.src_ec_verifi_reqrd == 0)
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();
                }
                #endregion
                order.dest_foreign_flag = model.dest_foreign_flag;
                order.dbss_subscription_id = model.dbss_subscription_id;
                order.customer_id = model.customer_id;
                order.order_confirmation_code = model.order_confirmation_code;
                order.server_name = Environment.MachineName;

                //for sim repalcement customer update is enabled here.
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMReplacement &&
                    model.saf_status.HasValue)
                {
                    order.saf_status = model.saf_status == false ? 1 : 0;
                }
                else if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                            String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    order.saf_status = 1;
                }
                else
                {
                    order.saf_status = 0;
                }
                if (!String.IsNullOrEmpty(model.msisdnReservationId))
                {
                    order.msisdnReservationId = model.msisdnReservationId;
                }

                order.src_owner_customer_id = model.src_owner_customer_id;
                order.src_user_customer_id = model.src_user_customer_id;
                order.src_payer_customer_id = model.src_payer_customer_id;
                order.dest_imsi = model.dest_imsi;
                order.status = model.status;
                order.bss_reqId = model.bss_reqId;
                order.error_id = model.error_id;
                order.error_description = model.err_msg;

                order.loginAttemptId = LoginProviderInfo.login_attempt_id;
                order.latitude = model.latitude;
                order.longitude = model.longitude;

                if (model.lac != null)
                {
                    order.lac = (int)model.lac;
                }

                if (model.cid != null)
                {
                    order.cid = (int)model.cid;
                }

                order.scanner_id = model.scanner_id;

                if (model.order_booking_flag > 0 || model.order_booking_flag > -1)
                {
                    order.order_booking_flag = model.order_booking_flag;
                }

                order.is_esim = model.is_esim;
                order.prov_id = model.prov_id;

                return order;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<OrderRequest3> populateOrderDataV4(RAOrderRequestV2 model)
        {
            OrderRequest3 order = new OrderRequest3();
            BL_Json blJson = new BL_Json();
            try
            {
                if (model.msisdn.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                }

                order.bi_token_number = model.bi_token_number == null ? 0 : model.bi_token_number;
                order.purpose_number = String.IsNullOrEmpty(model.purpose_number) ? null : Convert.ToDecimal(model.purpose_number.Trim());
                order.msisdn = model.msisdn;

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_category = model.src_sim_category;
                else
                    order.sim_category = model.sim_category;

                if (!String.IsNullOrEmpty(model.sim_number))
                {
                    if (model.sim_number.Substring(0, 6) != FixedValueCollection.SIMCode)
                    {
                        model.sim_number = FixedValueCollection.SIMCode + model.sim_number;
                    }
                }

                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                      String.IsNullOrEmpty(model.poc_msisdn_number))
                    order.sim_number = model.old_sim_number != null ? model.old_sim_number.Trim() : "";
                else if (!String.IsNullOrEmpty(model.sim_number))
                    order.sim_number = model.sim_number.Trim();
                else
                    order.sim_number = model.sim_number;

                order.subscription_type_id = String.IsNullOrEmpty(model.subscription_type_id) ? null : Convert.ToDecimal(model.subscription_type_id.Trim());
                order.subscription_code = model.subscription_code;
                order.package_id = String.IsNullOrEmpty(model.package_id) ? null : Convert.ToDecimal(model.package_id.Trim());
                order.package_code = model.package_code;
                if (!String.IsNullOrEmpty(model.dest_nid) && !String.IsNullOrEmpty(model.dest_dob))
                {
                    order.dest_nid = (model.dest_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.dest_dob).Year + model.dest_nid : model.dest_nid;
                }

                if (!String.IsNullOrEmpty(model.src_nid) && !String.IsNullOrEmpty(model.src_dob))
                {
                    order.src_nid = (model.src_nid.Length == (int)NIDLength.Length_13) ? DateTime.Parse(model.src_dob).Year + model.src_nid : model.src_nid;
                }

                order.dest_dob = String.IsNullOrEmpty(model.dest_dob) ? String.Empty : DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.dest_nid))
                {
                    order.dest_doc_type_no = model.dest_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;
                }

                order.src_dob = String.IsNullOrEmpty(model.src_dob) ? String.Empty : DateTime.Parse(model.src_dob).ToString(StringFormatCollection.DBSSDOBFormat);

                if (!String.IsNullOrEmpty(model.src_nid))
                {
                    order.src_doc_type_no = model.src_nid.Length == (int)NIDLength.Length_10 ? (int)DOCTypeIDNoNIDLengthWise.NID_Length_10 : (int)DOCTypeIDNoNIDLengthWise.NID_Length_Others;//(int)DOCTypeNo.nid;//1 for nid. /*model.src_doc_type_no;*/
                }
                else
                {
                    order.src_doc_type_no = null;
                }
                order.platform_id = model.platform_id;

                if (!String.IsNullOrEmpty(model.customer_name) &&
                        isCustomerNameValid(model.customer_name))
                {
                    order.customer_name = model.customer_name;
                }

                if (!String.IsNullOrEmpty(model.gender))
                {
                    order.gender = model.gender.ToLower() == "others" ? "notitle" : model.gender.ToLower();
                }

                order.flat_number = model.flat_number;
                order.house_number = model.house_number;
                order.road_number = model.road_number;
                order.village = model.village;
                order.division_id = model.division_id;
                order.district_id = model.district_id;
                order.thana_id = model.thana_id;
                order.postal_code = String.IsNullOrEmpty(model.postal_code) ? "0" : model.postal_code;

                //=====temp modelfication (as RA apk is unable to update [15-12-19])============
                if (!String.IsNullOrEmpty(model.email))
                {
                    order.email = isEmailValid(model.email.ToLower()) == true ? model.email : String.Empty;
                }
                else
                {
                    order.email = String.Empty;
                }
                //===================

                order.retailer_id = model.retailer_id;
                //note: For retailer App dest_doc_id and src_doc_id is dest_nid and src_nid namely. 
                order.dest_left_thumb_score = model.dest_left_thumb_score.Equals(null) ? (decimal?)null : model.dest_left_thumb_score;
                order.dest_left_thumb = Array.Empty<byte>();
                order.dest_left_index_score = model.dest_left_index_score.Equals(null) ? (decimal?)null : model.dest_left_index_score;
                order.dest_left_index = Array.Empty<byte>();
                order.dest_right_thumb_score = model.dest_right_thumb_score.Equals(null) ? (decimal?)null : model.dest_right_thumb_score;
                order.dest_right_index = Array.Empty<byte>();
                order.dest_right_index_score = model.dest_right_index_score.Equals(null) ? (decimal?)null : model.dest_right_index_score;
                order.dest_right_thumb = Array.Empty<byte>();
                order.src_left_thumb_score = model.src_left_thumb_score.Equals(null) ? (decimal?)null : model.src_left_thumb_score;
                order.src_left_thumb = Array.Empty<byte>();
                order.src_left_index_score = model.src_left_index_score.Equals(null) ? (decimal?)null : model.src_left_index_score;
                order.src_left_index = Array.Empty<byte>();
                order.src_right_thumb_score = model.src_right_thumb_score.Equals(null) ? (decimal?)null : model.src_right_thumb_score;
                order.src_right_thumb = Array.Empty<byte>();
                order.src_right_index_score = model.src_right_index_score.Equals(null) ? (decimal?)null : model.src_right_index_score;
                order.src_right_index = Array.Empty<byte>();
                order.retailer_code = (model.channel_name == FixedValueCollection.ResellerChannel
                                                            || model.channel_name == FixedValueCollection.CorporateChannel
                                                            || model.channel_name == FixedValueCollection.SMEChannel)
                                                            ? FixedValueCollection.ResellerCodeText + model.retailer_id : model.retailer_id;
                order.port_in_date = String.IsNullOrEmpty(model.port_in_date) ? null : DateTime.Parse(model.port_in_date);
                order.alt_msisdn = model.alt_msisdn;

                if (!String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    if (model.poc_msisdn_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                    {
                        order.poc_number = FixedValueCollection.MSISDNCountryCode + model.poc_msisdn_number;
                    }
                    else
                    {
                        order.poc_number = model.poc_msisdn_number;
                    }
                }
                order.is_urgent = model.is_urgent;
                order.optional1 = model.optional1;
                order.optional2 = model.optional2;
                order.optional3 = model.otp; // For any kind of activation is processed through OTP then OTP will be insert in option3.
                order.optional4 = String.IsNullOrEmpty(model.optional4) ? null : Convert.ToDecimal(model.optional4.Trim());
                order.optional5 = String.IsNullOrEmpty(model.optional5) ? null : Convert.ToDecimal(model.optional5.Trim());
                order.optional6 = String.IsNullOrEmpty(model.optional6) ? null : Convert.ToDecimal(model.optional6.Trim());
                order.note = model.note;
                order.sim_rep_reason_id = String.IsNullOrEmpty(model.sim_rep_reason_id) ? null : Convert.ToDecimal(model.sim_rep_reason_id.Trim());
                order.payment_type = model.payment_type ?? "";
                order.is_paired = model.is_paired;
                order.cahnnel_id = model.channel_id;

                order.division_name = String.IsNullOrEmpty(model.division_name) ? model.division_name : ConverterHelper.UpperLowerWithSpaceConverter(model.division_name);
                order.district_name = String.IsNullOrEmpty(model.district_name) ? model.district_name : ConverterHelper.UpperLowerWithSpaceConverter(model.district_name);
                order.thana_name = String.IsNullOrEmpty(model.thana_name) ? model.thana_name : ConverterHelper.UpperLowerWithSpaceConverter(model.thana_name);

                //here distributor code is mapped with "P_CENTER_OR_DISTRIBUTOR_CODE" in DB. 
                order.distributor_code = model.distributor_code; // String.IsNullOrEmpty(model.distributor_code) ? await _bllCommon.GetDistributorCodeFromSessionTokenV2(model.session_token, model.retailer_id) : model.distributor_code;
                order.sim_replc_reason = model.sim_replc_reason;
                order.channel_name = model.channel_name;
                order.right_id = model.right_id;

                order.sim_replacement_type = model.sim_replacement_type;

                order.old_sim_number = String.IsNullOrEmpty(model.old_sim_number) ? null : model.old_sim_number;

                order.src_sim_category = model.src_sim_category;
                order.port_in_confirmation_code = model.port_in_confirmation_code;

                order.dest_ec_verifi_reqrd = !String.IsNullOrEmpty(model.dest_nid) ? 1 : 0;// 

                //If any trunsection porpuse have done by OTP, then the bio verification for src customer is not needed.
                //For example: For "two party EC verification" if the purpose is "B2B to B2C" with OTP, then the value "src_ec_verifi_reqrd" will 0.    
                //Here reseller app is sending OTP in "optional3" for "Corporate To Individual Transfer with OTP" [03 MAY 2020]
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.CorporateToIndividualTransfer
                    && !String.IsNullOrEmpty(model.otp))
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();
                }
                else
                {
                    order.src_ec_verifi_reqrd = !String.IsNullOrEmpty(model.src_nid) ? 1 : 0;
                }
                #region One Party EC Verification
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer
                   && model.src_ec_verifi_reqrd == 0)
                {
                    order.src_ec_verifi_reqrd = 0;
                    //If src customer is validated by OTP, then we keep a note in DB column.
                    order.note = SettingsValues.GeB2BtoB2CTwoPaertyValidationOTPNote();
                }
                #endregion
                order.dest_foreign_flag = model.dest_foreign_flag;
                order.dbss_subscription_id = model.dbss_subscription_id.Equals(null) ? null : model.dbss_subscription_id;
                order.customer_id = model.customer_id;
                order.order_confirmation_code = model.order_confirmation_code;
                order.server_name = Environment.MachineName;

                //for sim repalcement customer update is enabled here.
                if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMReplacement &&
                    model.saf_status.HasValue)
                {
                    order.saf_status = model.saf_status == false ? 1 : 0;
                }
                else if (Convert.ToInt16(model.purpose_number) == (int)EnumPurposeNumber.SIMTransfer &&
                            String.IsNullOrEmpty(model.poc_msisdn_number))
                {
                    order.saf_status = 1;
                }
                else
                {
                    order.saf_status = 0;
                }
                if (!String.IsNullOrEmpty(model.msisdnReservationId))
                {
                    order.msisdnReservationId = model.msisdnReservationId;
                }

                order.src_owner_customer_id = model.src_owner_customer_id;
                order.src_user_customer_id = model.src_user_customer_id;
                order.src_payer_customer_id = model.src_payer_customer_id;
                order.dest_imsi = model.dest_imsi;
                order.status = model.status;
                order.bss_reqId = model.bss_reqId;
                order.error_id = model.error_id;
                order.error_description = model.err_msg;

                order.loginAttemptId = LoginProviderInfo.login_attempt_id;
                order.latitude = model.latitude;
                order.longitude = model.longitude;

                if (model.lac != null)
                {
                    order.lac = (int)model.lac;
                }

                if (model.cid != null)
                {
                    order.cid = (int)model.cid;
                }

                order.scanner_id = model.scanner_id;

                if (model.order_booking_flag > 0 || model.order_booking_flag > -1)
                {
                    order.order_booking_flag = model.order_booking_flag;
                }

                order.is_esim = model.is_esim;

                if (model.is_starTrek != null)
                {
                    order.is_starTrek = model.is_starTrek;
                }
                order.order_id = model.order_id;

                if (model.is_online_sale != null)
                {
                    order.is_online_sale = model.is_online_sale;
                }
                order.prov_id = model.prov_id;

                return order;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private bool isCustomerNameValid(string name)
        {
            bool result = false;

            if (name.Length < 2)
                throw new Exception("Customer name must have minimum 2 characters.");
            else if (name.Length >= 2 && name.Trim().Length < 2)
                throw new Exception("Invalid customer name! Customer name must have minimum 2 characters.");

            result = true;
            return result;
        }
    [GeneratedRegex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    private bool isEmailValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            return EmailRegex().IsMatch(email);
        }
        catch
        {
            return false;
        }
    }
}
