using BIA.BLL.BLLServices;
using BIA.Common;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Entity.Utility;
using BIA.Entity.ViewModel;
using BIA.JWT;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using BIA.BLL.Utility;
using BIA.Entity.Interfaces;
using System.Net;
using BIA.Helper;
using Serilog;
using BIA.Entity.PopulateModel;

namespace BIA.Controllers
{
    [Route("api/StarTrekSIMReplacement")]
    [ApiController]
    public class StarTrekSIMReplacementController : ControllerBase
    {
        private readonly BLLOrder _orderManager;
        private readonly BLLLog _bllLog;
        private readonly BaseController _bio;
        private readonly BLLCommon _bllCommon;
        private readonly GeoFencingValidation _geo;
        private readonly BLLDBSSToRAParse _dbssToRaParse;
        private readonly ApiRequest _apiReq;
        public StarTrekSIMReplacementController(BLLOrder orderManager, BLLLog bllLog, BaseController bio, BLLCommon bllCommon, GeoFencingValidation geo, BLLDBSSToRAParse dbssToRaParse, ApiRequest apiReq)
        {
            _orderManager = orderManager;
            _bllLog = bllLog;
            _bio = bio;
            _bllCommon = bllCommon;
            _geo = geo;
            _dbssToRaParse = dbssToRaParse;
            _apiReq = apiReq;
        }

        [HttpPost]
        [StarTrekSIMReplacementOrderRequestValidator]
        [Route("SIMReplacementSubmit")]
        public async Task<IActionResult> SIMReplacementSubmitOrder([FromBody][Bind("alt_msisdn,center_code,channel_name,cid,customer_id,customer_name,dbss_subscription_id,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,distributor_code,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,lac,latitude,longitude,msisdn,old_sim_number,payment_type,postal_code,purpose_number,retailer_id,right_id,road_number,saf_status,scanner_id,session_token,sim_number,sim_rep_reason_id,sim_replc_reason,thana_id,thana_name,village")] StarTrekSimReplacementRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            NidDobInfoResponse dobInfoResponse = new NidDobInfoResponse();
            IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest = new IndividualSIMReplsMSISDNCheckRequest();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            BL_Json _blJson = new BL_Json();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();

            try
            {
                model = populateModel.StarTrekSIMReplacementRequestPopulateModel(request);
                
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                TokenValidationService token = new TokenValidationService(secreteKey);
                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.CastRequestModel(model);

                log.req_blob = _blJson.GetGenericJsonData(requestModelBLOB);
                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                        model.distributor_code = security.DistributorCode;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                #region Geo fencing BP user
                if (geoFencEnable == 1)
                {
                    if (model.isBPUser == 1)
                    {
                        RACommonResponseRevamp responseRevamp = await _geo.GeoFencingBPUser(model);

                        if (responseRevamp != null && responseRevamp.isError == true)
                        {
                            return Ok(responseRevamp);
                        }
                    }
                }
                #endregion

                #region Get_Data_from_Validation

                msisdnCheckReqest.mobile_number = model.msisdn;
                msisdnCheckReqest.purpose_number = model.purpose_number;
                msisdnCheckReqest.retailer_id = model.retailer_id;
                msisdnCheckReqest.center_code = model.center_code;
                msisdnCheckReqest.channel_name = model.channel_name;
                msisdnCheckReqest.session_token = model.session_token;
                msisdnCheckReqest.sim_number = model.sim_number;

                dobInfoResponse = await GetNidDob(msisdnCheckReqest);

                if (dobInfoResponse.result == false)
                {
                    //orderRes.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = dobInfoResponse.message;
                    log.remarks = dobInfoResponse.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                else
                {
                    model.dest_nid = dobInfoResponse.dest_nid;
                    model.dest_dob = dobInfoResponse.dest_dob;
                    model.old_sim_number = dobInfoResponse.old_sim_number;
                    
                    var simResp = await _bio.CheckSIMNumberReplacementV2(new SIMNumberCheckRequest()
                    {
                        center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                        distributor_code = "",
                        channel_name = msisdnCheckReqest.channel_name,
                        session_token = msisdnCheckReqest.session_token,
                        sim_number = msisdnCheckReqest.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        product_code = "",
                        inventory_id = 2,
                        msisdn = msisdnCheckReqest.mobile_number,
                        purpose_number = msisdnCheckReqest.purpose_number ?? ""
                    }, (int)EnumPurposeOfSIMCheck.SIMReplacement, null, null, dobInfoResponse.old_sim_type);

                    if (simResp.result == false)
                    {
                        orderRes.isError = true;
                        orderRes.message = orderRes.message;
                        return Ok(orderRes);
                    }                                       
                }

                #endregion                

                #region Check if submitted order is already in process or not.
                var orderValidationResult = await _orderManager.ValidateOrder(new VMValidateOrder
                {
                    msisdn = model.msisdn,
                    sim_number = model.sim_number,
                    purpose_number = Convert.ToInt32(model.purpose_number),
                    is_corporate = 0,
                    retailer_id = model.retailer_id,
                    dest_dob = DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat)
                });
                if (orderValidationResult.result == false)
                {
                    //orderRes.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = orderValidationResult.message;
                    log.remarks = orderValidationResult.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                #endregion
                #region Insert_Order
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;
                model.is_starTrek = 1;
                model.prov_id = loginProviderId;
                orderRes = await _orderManager.SubmitOrderV7(model);

                if (orderRes.isError)
                {
                    orderRes.isError = true;
                    orderRes.message = orderRes.message;
                    return Ok(orderRes);
                }
                else
                {
                    try
                    {
                        model.bi_token_number = Convert.ToDouble(orderRes.data.request_id);
                        #endregion
                        #region Get IMSI
                        var imsiResp = await _bio.GetImsiBySimAsync(new GetImsiReq
                        {
                            purpose_number = model.purpose_number,
                            retailer_id = model.retailer_id,
                            sim = model.sim_number,
                            msisdn = model.msisdn
                        });

                        if (imsiResp.result == false)
                        {
                            model.status = (int)EnumRAOrderStatus.Failed;
                            // orderRes.request_id = "0";
                            orderRes.isError = true;
                            orderRes.message = imsiResp.message;
                            model.err_msg = imsiResp.message;
                            log.remarks = imsiResp.message;
                            return Ok(orderRes);
                        }
                        else
                        {
                            model.dest_imsi = imsiResp.imsi;//[Note: here IMSI is being sent as SIM number as per business requirement]
                        }
                        #endregion
                        #region bio verification

                        var pursedData = await _orderManager.SubmitOrderDataPurseV2(model);
                        BiomerticDataModel dataModel = bioverifyDataMapp(pursedData);

                        verifyResp = await _bio.BssServiceProcessStarTrek(dataModel, model.msisdnReservationId ?? "", model.retailer_id, 0);

                        if (verifyResp.is_success == true)
                        {
                            model.bss_reqId = verifyResp.bss_req_id;
                            model.status = (int)EnumRAOrderStatus.BioVerificationSubmitted;
                        }
                        else
                        {
                            model.status = (int)EnumRAOrderStatus.Failed;
                            model.err_code = verifyResp.err_code;
                            model.err_msg = verifyResp.err_msg;
                            model.error_id = verifyResp.error_Id;
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        ErrorDescription errorDescription = new ErrorDescription();
                        model.status = (int)EnumRAOrderStatus.Failed;
                        errorDescription = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                        model.err_code = errorDescription.error_code;
                        model.error_id = errorDescription.error_id;
                        orderRes.message = String.IsNullOrEmpty(errorDescription.error_custom_msg) ? errorDescription.error_description : errorDescription.error_custom_msg;
                        model.err_msg = String.IsNullOrEmpty(errorDescription.error_custom_msg) ? errorDescription.error_description : errorDescription.error_custom_msg;
                        orderRes.isError = true;
                        return Ok(orderRes);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    isError = true,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                model.status = (int)EnumRAOrderStatus.Failed;
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;

                if (verifyResp != null)
                {
                    orderRes.data = new DataRes()
                    {
                        request_id = verifyResp.bss_req_id
                    };
                }
                else
                {
                    orderRes.data = new DataRes()
                    {
                        request_id = "0"
                    };
                }
                orderRes.isError = true;
                orderRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                model.err_msg = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.res_blob = _blJson.GetGenericJsonData(error);
            }
            finally
            {
                log.purpose_number = model.purpose_number;
                log.msisdn = _bllLog.FormatMSISDN(model.msisdn);
                log.req_time = DateTime.Now;
                if (model.bi_token_number != null && model.bi_token_number > 1)
                {
                    response2 = await _orderManager.UpdateOrder(new RAOrderRequestUpdate
                    {
                        bi_token_number = model.bi_token_number,
                        msidn = model.msisdn,
                        user_name = model.retailer_id,
                        dest_imsi = model.dest_imsi,
                        status = model.status,
                        bss_reqId = model.bss_reqId,
                        error_id = model.error_id,
                        err_msg = model.err_msg,
                    });
                }
                log.res_time = DateTime.Now;
                if (orderRes.data != null)
                {
                    log.is_success = orderRes.data.request_id.Length > 1 ? 1 : 0;
                    log.bi_token_number = orderRes.data.request_id;
                }
                log.res_blob = _blJson.GetGenericJsonData(orderRes);
                log.method_name = "SIMReplacementSubmitStarTrek";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(orderRes);
        }

        [HttpPost]
        [StarTrekSIMReplacementOrderRequestValidator]
        [Route("SIMReplacementSubmit-esim")]
        public async Task<IActionResult> SIMReplacementSubmitOrder_ESIM([FromBody][Bind("alt_msisdn,center_code,channel_name,cid,customer_id,customer_name,dbss_subscription_id,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,distributor_code,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,lac,latitude,longitude,msisdn,old_sim_number,payment_type,postal_code,purpose_number,retailer_id,right_id,road_number,saf_status,scanner_id,session_token,sim_number,sim_rep_reason_id,sim_replc_reason,thana_id,thana_name,village")] StarTrekSimReplacementRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            NidDobInfoResponse dobInfoResponse = new NidDobInfoResponse();
            IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest = new IndividualSIMReplsMSISDNCheckRequest();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            BL_Json _blJson = new BL_Json();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();

            try
            {
                model = populateModel.StarTrekSIMReplacementRequestPopulateModel(request);
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                TokenValidationService token = new TokenValidationService(secreteKey);

                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.CastRequestModel(model);
                log.req_blob = _blJson.GetGenericJsonData(requestModelBLOB);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                        model.distributor_code = security.DistributorCode;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                #region Geo fencing BP user
                if (geoFencEnable == 1)
                {
                    if (model.isBPUser == 1)
                    {
                        RACommonResponseRevamp responseRevamp = await _geo.GeoFencingBPUser(model);

                        if (responseRevamp != null && responseRevamp.isError == true)
                        {
                            return Ok(responseRevamp);
                        }
                    }
                }
                #endregion

                #region Get_Data_from_Validation

                msisdnCheckReqest.mobile_number = model.msisdn;
                msisdnCheckReqest.purpose_number = model.purpose_number;
                msisdnCheckReqest.retailer_id = model.retailer_id;
                msisdnCheckReqest.center_code = model.center_code;
                msisdnCheckReqest.channel_name = model.channel_name;
                msisdnCheckReqest.session_token = model.session_token;
                msisdnCheckReqest.sim_number = model.sim_number;

                dobInfoResponse = await GetNidDob(msisdnCheckReqest);

                if (dobInfoResponse.result == false)
                {
                    // orderRes.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = dobInfoResponse.message;
                    log.remarks = dobInfoResponse.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                else
                {
                    var simResp = await _bio.CheckSIMNumberV3(new SIMNumberCheckRequest()
                    {
                        center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                        distributor_code = "",
                        channel_name = msisdnCheckReqest.channel_name,
                        session_token = msisdnCheckReqest.session_token,
                        sim_number = msisdnCheckReqest.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        product_code = "",
                        inventory_id = 2,
                        msisdn = msisdnCheckReqest.mobile_number,
                        purpose_number = msisdnCheckReqest.purpose_number ?? ""
                    }, (int)EnumPurposeOfSIMCheck.SIMReplacement, null, null, dobInfoResponse.old_sim_type);

                    if (simResp.result == false)
                    {
                        orderRes.isError = true;
                        orderRes.message = simResp.message;

                        return Ok(orderRes);
                    }
                    else
                    {
                        model.dest_nid = dobInfoResponse.dest_nid;
                        model.dest_dob = dobInfoResponse.dest_dob;
                        model.old_sim_number = dobInfoResponse.old_sim_number;
                    }                    
                }

                #endregion

                #region Check if submitted order is already in process or not.
                var orderValidationResult = await _orderManager.ValidateOrder(new VMValidateOrder
                {
                    msisdn = model.msisdn,
                    sim_number = model.sim_number,
                    purpose_number = Convert.ToInt32(model.purpose_number),
                    is_corporate = 0,
                    retailer_id = model.retailer_id,
                    dest_dob = DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat)
                });
                if (orderValidationResult.result == false)
                {
                    orderRes.isError = true;
                    orderRes.message = orderValidationResult.message;
                    log.remarks = orderValidationResult.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                #endregion
                #region Insert_Order
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;
                model.is_esim = 1;
                model.is_starTrek = 1;
                model.prov_id = loginProviderId;
                orderRes = await _orderManager.SubmitOrderV7(model);

                if (orderRes.isError)
                {
                    return Ok(new SendOrderResponseRev()
                    {
                        isError = true,
                        message = orderRes.message
                    });
                }
                model.bi_token_number = Convert.ToDouble(orderRes.data.request_id);
                #endregion
                #region Get IMSI
                var imsiResp = await _bio.GetImsiBySimAsync(new GetImsiReq
                {
                    purpose_number = model.purpose_number,
                    retailer_id = model.retailer_id,
                    sim = model.sim_number,
                    msisdn = model.msisdn
                });

                if (imsiResp.result == false)
                {
                    model.status = (int)EnumRAOrderStatus.Failed;
                    // orderRes.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = imsiResp.message;
                    model.err_msg = imsiResp.message;
                    log.remarks = imsiResp.message;
                    return Ok(orderRes);
                }
                else
                {
                    model.dest_imsi = imsiResp.imsi;//[Note: here IMSI is being sent as SIM number as per business requirement]
                }
                #endregion
                #region bio verification

                var pursedData = await _orderManager.SubmitOrderDataPurseV2(model);
                BiomerticDataModel dataModel = bioverifyDataMapp(pursedData);

                verifyResp = await _bio.BssServiceProcessStarTrek(dataModel, model.msisdnReservationId ?? "", model.retailer_id, 0);

                if (verifyResp.is_success == true)
                {
                    model.bss_reqId = verifyResp.bss_req_id;
                    model.status = (int)EnumRAOrderStatus.BioVerificationSubmitted;
                }
                else
                {
                    model.status = (int)EnumRAOrderStatus.Failed;
                    model.err_code = verifyResp.err_code;
                    model.err_msg = verifyResp.err_msg;
                    model.error_id = verifyResp.error_Id;
                }
                #endregion
                //=====Order submission=====

            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    isError = true,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                model.status = (int)EnumRAOrderStatus.Failed;
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;

                if (verifyResp != null)
                {
                    orderRes.data.request_id = verifyResp.bss_req_id;
                }
                else
                {
                    orderRes.data.request_id = "";
                }

                orderRes.isError = true;
                orderRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                model.err_msg = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.res_blob = _blJson.GetGenericJsonData(error);
            }
            finally
            {
                log.purpose_number = model.purpose_number;
                log.msisdn = _bllLog.FormatMSISDN(model.msisdn);
                log.req_time = DateTime.Now;
                if (model.bi_token_number != null && model.bi_token_number > 1)
                {
                    response2 = await _orderManager.UpdateOrder(new RAOrderRequestUpdate
                    {
                        bi_token_number = model.bi_token_number,
                        msidn = model.msisdn,
                        dest_imsi = model.dest_imsi,
                        status = model.status,
                        bss_reqId = model.bss_reqId,
                        error_id = model.error_id,
                        err_msg = model.err_msg,
                        user_name = model.retailer_id
                    });
                }

                log.res_time = DateTime.Now;
                if (orderRes != null)
                {
                    log.is_success = orderRes.data != null && orderRes.data.request_id.Length > 1 ? 1 : 0;
                    log.bi_token_number = orderRes.data != null ? orderRes.data.request_id : "";
                }
                log.res_blob = _blJson.GetGenericJsonData(orderRes);
                log.method_name = "SIMReplacementSubmitOrder_ESIM_StarTrek";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null
                                && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;
                await _bllLog.RAToDBSSLog(log);

            }
            return Ok(orderRes);
        }

        public async Task<NidDobInfoResponse> GetNidDob(IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest)
        {
            NidDobInfoResponse nidDobInfo = new NidDobInfoResponse();
            BIAToDBSSLog log = new BIAToDBSSLog();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BL_Json _blJson = new BL_Json();
            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingOwnerCustomerUserCustomerSimCardInfo, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetNidDob");
                //JObject dbssResp = JObject.Parse("{\r\n  \"data\": {\r\n    \"attributes\": {\r\n      \"activation-time\": \"2025-05-21T09:42:49+06:00\",\r\n      \"allow-reactivation\": false,\r\n      \"contract-id\": \"403659\",\r\n      \"contract-status\": \"rollover\",\r\n      \"directory-listing\": \"none\",\r\n      \"first-call-date\": null,\r\n      \"language\": \"bn\",\r\n      \"latest-contract-termination-time\": \"2027-05-21T09:42:49+06:00\",\r\n      \"loan-category-id\": \"1\",\r\n      \"loan-category-name\": \"Slab-1\",\r\n      \"monthly-costs\": 0,\r\n      \"msisdn\": \"8801410809934\",\r\n      \"original-contract-confirmation-code\": \"91Q73M785P067\",\r\n      \"payment-type\": \"prepaid\",\r\n      \"status\": \"active\",\r\n      \"termination-time\": \"3000-01-01T00:00:00+06:00\"\r\n    },\r\n    \"id\": \"402147\",\r\n    \"links\": {\r\n      \"self\": \"/api/v1/subscriptions/402147\"\r\n    },\r\n    \"relationships\": {\r\n      \"available-child-products\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/available-child-products\"\r\n        }\r\n      },\r\n      \"available-loan-products\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/available-loan-products\"\r\n        }\r\n      },\r\n      \"available-products\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/available-products\"\r\n        }\r\n      },\r\n      \"available-subscription-types\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/available-subscription-types\"\r\n        }\r\n      },\r\n      \"balances\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/balances\"\r\n        }\r\n      },\r\n      \"barrings\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/barrings\"\r\n        }\r\n      },\r\n      \"billing-accounts\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/billing-accounts\"\r\n        },\r\n        \"data\": []\r\n      },\r\n      \"billing-rate-plan\": {\r\n        \"data\": {\r\n          \"id\": \"1\",\r\n          \"type\": \"billing-rate-plans\"\r\n        },\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/billing-rate-plan\"\r\n        }\r\n      },\r\n      \"billing-usages\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/billing-usages\"\r\n        }\r\n      },\r\n      \"catalog-sim-cards\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/catalog-sim-cards\"\r\n        }\r\n      },\r\n      \"combined-usage-reports\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/combined-usage-reports\"\r\n        }\r\n      },\r\n      \"connected-products\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/connected-products\"\r\n        }\r\n      },\r\n      \"connection-type\": {\r\n        \"data\": {\r\n          \"id\": \"3\",\r\n          \"type\": \"connection-types\"\r\n        },\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/connection-type\"\r\n        }\r\n      },\r\n      \"coordinator-customer\": {\r\n        \"data\": null,\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/coordinator-customer\"\r\n        }\r\n      },\r\n      \"document-validations\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/document-validations\"\r\n        }\r\n      },\r\n      \"gsm-service-usages\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/gsm-service-usages\"\r\n        }\r\n      },\r\n      \"network-services\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/network-services\"\r\n        }\r\n      },\r\n      \"owner-customer\": {\r\n        \"data\": {\r\n          \"id\": \"1085758198\",\r\n          \"type\": \"customers\"\r\n        },\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/owner-customer\"\r\n        }\r\n      },\r\n      \"payer-customer\": {\r\n        \"data\": {\r\n          \"id\": \"1085760063\",\r\n          \"type\": \"customers\"\r\n        },\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/payer-customer\"\r\n        }\r\n      },\r\n      \"porting-requests\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/porting-requests\"\r\n        }\r\n      },\r\n      \"product-usages\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/product-usages\"\r\n        }\r\n      },\r\n      \"products\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/products\"\r\n        }\r\n      },\r\n      \"services\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/services\"\r\n        }\r\n      },\r\n      \"sim-card-orders\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/sim-card-orders\"\r\n        }\r\n      },\r\n      \"sim-cards\": {\r\n        \"data\": [\r\n          {\r\n            \"id\": \"primary-410598\",\r\n            \"type\": \"sim-cards\"\r\n          }\r\n        ],\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/sim-cards\"\r\n        }\r\n      },\r\n      \"subscription-discounts\": {\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/subscription-discounts\"\r\n        }\r\n      },\r\n      \"subscription-type\": {\r\n        \"data\": {\r\n          \"id\": \"132\",\r\n          \"type\": \"subscription-types\"\r\n        },\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/subscription-type\"\r\n        }\r\n      },\r\n      \"user-customer\": {\r\n        \"data\": {\r\n          \"id\": \"1085760063\",\r\n          \"type\": \"customers\"\r\n        },\r\n        \"links\": {\r\n          \"related\": \"/api/v1/subscriptions/402147/user-customer\"\r\n        }\r\n      }\r\n    },\r\n    \"type\": \"subscriptions\"\r\n  },\r\n  \"included\": [\r\n    {\r\n      \"attributes\": {\r\n        \"account-type\": null,\r\n        \"agreement-start-date\": null,\r\n        \"alt-contact-phone\": \"\",\r\n        \"ban\": null,\r\n        \"bank-account-number\": null,\r\n        \"business-uid\": null,\r\n        \"category\": \"consumer\",\r\n        \"contact-phone\": \"\",\r\n        \"coordinator-id\": null,\r\n        \"date-of-birth\": \"2001-10-20\",\r\n        \"email\": \"\",\r\n        \"first-name\": \"Yeasir\",\r\n        \"frame-agreement-ended-at\": null,\r\n        \"frame-agreement-started-at\": null,\r\n        \"gender\": \"\",\r\n        \"id-document-number\": \"8277409689\",\r\n        \"id-document-type\": \"smart_national_id\",\r\n        \"id-expiry\": null,\r\n        \"invoice-delivery-type\": \"sms\",\r\n        \"is-company\": false,\r\n        \"is-coordinator\": false,\r\n        \"is-fleet-manager\": false,\r\n        \"is-loyalty-manager\": false,\r\n        \"language\": \"bn\",\r\n        \"last-name\": \"\",\r\n        \"marketing-own\": true,\r\n        \"marketing-third-party\": true,\r\n        \"middle-name\": null,\r\n        \"nationality\": \"BD\",\r\n        \"occupation\": null,\r\n        \"online-id\": null,\r\n        \"payment-method\": \"bank_payment\",\r\n        \"segmentation-category\": \"001\",\r\n        \"trade-register-id\": null,\r\n        \"vat-usage-code\": \"domestic\"\r\n      },\r\n      \"id\": \"1085758198\",\r\n      \"links\": {\r\n        \"self\": \"/api/v1/customers/1085758198\"\r\n      },\r\n      \"relationships\": {\r\n        \"addresses\": {\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085758198/addresses\"\r\n          }\r\n        },\r\n        \"company-people\": {\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085758198/company-people\"\r\n          }\r\n        },\r\n        \"contact-companies\": {\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085758198/contact-companies\"\r\n          }\r\n        },\r\n        \"coordinator-customer\": {\r\n          \"data\": null,\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085758198/coordinator-customer\"\r\n          }\r\n        },\r\n        \"customer-edit-permission\": {\r\n          \"data\": {\r\n            \"id\": \"1085758198\",\r\n            \"type\": \"customer-edit-permissions\"\r\n          },\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085758198/customer-edit-permission\"\r\n          }\r\n        },\r\n        \"inventory\": {\r\n          \"data\": {\r\n            \"id\": \"1085758198\",\r\n            \"type\": \"inventories\"\r\n          },\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085758198/inventory\"\r\n          }\r\n        },\r\n        \"orders\": {\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085758198/orders\"\r\n          }\r\n        }\r\n      },\r\n      \"type\": \"customers\"\r\n    },\r\n    {\r\n      \"attributes\": {\r\n        \"icc\": \"898803841810275154\",\r\n        \"is-multi-surf\": false,\r\n        \"pin-1\": \"1234\",\r\n        \"pin-2\": \"6979\",\r\n        \"puk-1\": \"33485217\",\r\n        \"puk-2\": \"18851922\",\r\n        \"sim-type\": \"USIM\",\r\n        \"status\": \"primary\"\r\n      },\r\n      \"id\": \"primary-410598\",\r\n      \"links\": {\r\n        \"self\": \"/api/v1/sim-cards/primary-410598\"\r\n      },\r\n      \"relationships\": {\r\n        \"subscription\": {\r\n          \"data\": {\r\n            \"id\": \"402147\",\r\n            \"type\": \"subscriptions\"\r\n          },\r\n          \"links\": {\r\n            \"related\": \"/api/v1/sim-cards/primary-410598/subscription\"\r\n          }\r\n        }\r\n      },\r\n      \"type\": \"sim-cards\"\r\n    },\r\n    {\r\n      \"attributes\": {\r\n        \"account-type\": null,\r\n        \"agreement-start-date\": null,\r\n        \"alt-contact-phone\": \"\",\r\n        \"ban\": null,\r\n        \"bank-account-number\": null,\r\n        \"business-uid\": null,\r\n        \"category\": \"consumer\",\r\n        \"contact-phone\": \"8801410809934\",\r\n        \"coordinator-id\": null,\r\n        \"date-of-birth\": null,\r\n        \"email\": \"\",\r\n        \"first-name\": \"MD. Yasir Arafat\",\r\n        \"frame-agreement-ended-at\": null,\r\n        \"frame-agreement-started-at\": null,\r\n        \"gender\": \"male\",\r\n        \"id-document-number\": \"\",\r\n        \"id-document-type\": \"\",\r\n        \"id-expiry\": null,\r\n        \"invoice-delivery-type\": \"sms\",\r\n        \"is-company\": false,\r\n        \"is-coordinator\": false,\r\n        \"is-fleet-manager\": false,\r\n        \"is-loyalty-manager\": false,\r\n        \"language\": \"en\",\r\n        \"last-name\": \"\",\r\n        \"marketing-own\": true,\r\n        \"marketing-third-party\": true,\r\n        \"middle-name\": null,\r\n        \"nationality\": \"BD\",\r\n        \"occupation\": \"\",\r\n        \"online-id\": null,\r\n        \"payment-method\": \"bank_payment\",\r\n        \"segmentation-category\": \"001\",\r\n        \"trade-register-id\": null,\r\n        \"vat-usage-code\": \"domestic\"\r\n      },\r\n      \"id\": \"1085760063\",\r\n      \"links\": {\r\n        \"self\": \"/api/v1/customers/1085760063\"\r\n      },\r\n      \"relationships\": {\r\n        \"addresses\": {\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085760063/addresses\"\r\n          }\r\n        },\r\n        \"company-people\": {\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085760063/company-people\"\r\n          }\r\n        },\r\n        \"contact-companies\": {\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085760063/contact-companies\"\r\n          }\r\n        },\r\n        \"coordinator-customer\": {\r\n          \"data\": null,\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085760063/coordinator-customer\"\r\n          }\r\n        },\r\n        \"customer-edit-permission\": {\r\n          \"data\": {\r\n            \"id\": \"1085760063\",\r\n            \"type\": \"customer-edit-permissions\"\r\n          },\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085760063/customer-edit-permission\"\r\n          }\r\n        },\r\n        \"inventory\": {\r\n          \"data\": {\r\n            \"id\": \"1085760063\",\r\n            \"type\": \"inventories\"\r\n          },\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085760063/inventory\"\r\n          }\r\n        },\r\n        \"orders\": {\r\n          \"links\": {\r\n            \"related\": \"/api/v1/customers/1085760063/orders\"\r\n          }\r\n        }\r\n      },\r\n      \"type\": \"customers\"\r\n    }\r\n  ]\r\n}");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null || dbssResp["included"] == null)
                {
                    nidDobInfo.result = false;
                    nidDobInfo.message = MessageCollection.SIMReplNoDataFound;
                    return nidDobInfo;
                }
                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.IndividualSIMReplacementMSISDNReqParsingV3(dbssResp);

                if (msisdnResp.result == false)
                {
                    nidDobInfo.result = false;
                    nidDobInfo.message = MessageCollection.SIMReplNoDataFound;
                    return nidDobInfo;
                }
                nidDobInfo.dest_nid = msisdnResp.doc_id_number ?? "";
                nidDobInfo.dest_dob = msisdnResp.dob ?? "";
                nidDobInfo.old_sim_number = msisdnResp.old_sim_number;
                nidDobInfo.old_sim_type = msisdnResp.old_sim_type;
                nidDobInfo.result = true;
                nidDobInfo.message = "";

                return nidDobInfo;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? String.Empty;
                nidDobInfo.result = false;
                nidDobInfo.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.res_blob = _blJson.GetGenericJsonData(nidDobInfo);
                return nidDobInfo;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "GetNidDob";
                await _bllLog.RAToDBSSLog(log);
            }
        }

        public BiomerticDataModel bioverifyDataMapp(OrderRequest2 order)
        {
            BiomerticDataModel resp = new BiomerticDataModel();
            if (order != null) 
            {
                resp.status = order.status;
                resp.create_date = DateTime.Now.ToString();

                if (order.purpose_number != null)
                    resp.purpose_number = (int)order.purpose_number;
                if (order.dest_doc_type_no != null)
                    resp.dest_doc_type_no = order.dest_doc_type_no.ToString() ?? "";
                if (!String.IsNullOrEmpty(order.dest_nid))
                    resp.dest_doc_id = order.dest_nid;
                if (!String.IsNullOrEmpty(order.retailer_id))
                    resp.user_id = order.retailer_id;
                if (!String.IsNullOrEmpty(order.msisdn))
                    resp.msisdn = order.msisdn;
                if (order.dest_ec_verifi_reqrd != null)
                    resp.dest_ec_verification_required = (int)order.dest_ec_verifi_reqrd;
                if (!String.IsNullOrEmpty(order.dest_imsi))
                    resp.dest_imsi = order.dest_imsi;
                if (order.dest_foreign_flag != null)
                    resp.dest_foreign_flag = (int)order.dest_foreign_flag;
                if (order.dbss_subscription_id != 0)
                    resp.dbss_subscription_id = (int?)order.dbss_subscription_id;
                if (order.sim_category != null)
                {
                    resp.sim_category = (int)order.sim_category;
                }
                else
                {
                    resp.sim_category = 0;
                }
                if (!String.IsNullOrEmpty(order.poc_number))
                    resp.poc_number = order.poc_number;

                resp.dest_dob = order.dest_dob;
                resp.dest_left_thumb = order.dest_left_thumb;
                resp.dest_left_index = order.dest_left_index;
                resp.dest_right_thumb = order.dest_right_thumb;
                resp.dest_right_index = order.dest_right_index;

                if (order.src_doc_type_no != null)
                    resp.src_doc_type_no = order.src_doc_type_no.ToString() ?? "";
                if (order.src_ec_verifi_reqrd != null)
                    resp.src_ec_verification_required = (int)order.src_ec_verifi_reqrd;
                if (order.sim_replacement_type != null)
                    resp.sim_replacement_type = (int)order.sim_replacement_type;
                if (!String.IsNullOrEmpty(order.src_dob))
                    resp.src_dob = order.src_dob;
                if (!String.IsNullOrEmpty(order.src_nid))
                    resp.src_doc_id = order.src_nid;
            }

            return resp;
        }
    }
}
