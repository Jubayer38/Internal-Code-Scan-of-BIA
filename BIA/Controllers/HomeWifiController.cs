using BIA.BLL.BLLServices;
using BIA.BLL.Utility;
using BIA.Common;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.PopulateModel;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Entity.Utility;
using BIA.Entity.ViewModel;
using BIA.Helper;
using BIA.JWT;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Serilog;

namespace BIA.Controllers
{
    [Route("api/HomeWifi")]
    [ApiController]
    public class HomeWifiController : ControllerBase
    {
        private readonly BLLHomeWifiService _bllHomeWifiService;
        private readonly BL_Json _blJson;
        private readonly BLLLog _bllLog;
        private readonly BaseController _bio;
        private readonly ApiManager _apiManager;
        private readonly BLLOrder _orderManager;
        private readonly GeoFencingValidation _geo;
        private readonly BiometricApiCall _apiCall;
        private readonly ApiRequest _apiReq;
        private readonly BLLDBSSToRAParse _dbssToRaParse;
        private readonly BLLOrder _bLLOrder;

        public HomeWifiController(BLLHomeWifiService bllHomeWifiService, BL_Json blJson, BLLLog bllLog, BaseController bio, ApiManager apiManager, BLLOrder orderManager, GeoFencingValidation geo, BiometricApiCall apiCall, ApiRequest apiReq, BLLDBSSToRAParse dbssToRaParse, BLLOrder bLLOrder)
        {
            _bllHomeWifiService = bllHomeWifiService;
            _blJson = blJson;
            _bllLog = bllLog;
            _bio = bio;
            _apiManager = apiManager;
            _orderManager = orderManager;
            _geo = geo;
            _apiCall = apiCall;
            _apiReq = apiReq;
            _dbssToRaParse = dbssToRaParse;
            _bLLOrder = bLLOrder;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("HomeWifiLeadList")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> LeadList([FromBody][Bind("retailer_code")] HomeWifiLeadListRequestModel model)
        {
            if (model == null)
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Request body is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.retailer_code))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "retailer_code is required.",
                    data = null
                });
            }

            var response = await _bllHomeWifiService.BLLHomeWifiLeadList(model);

            return Ok(response);
        }


        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("HomeWifiLeadDetails")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> LeadDetails([FromBody][Bind("order_number,retailer_code")] HomeWifiLeadDetailsRequestModel model)
        {
           

            if (model == null)
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Request body is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.retailer_code))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "retailer_code is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.order_number))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "order_number is required.",
                    data = null
                });
            }

            var response = await _bllHomeWifiService.BLLHomeWifiLeadDetails(model);

            return Ok(response);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetHomeWifiCancelReasons")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> GetCancelReasons()
        {
            
            var response = await _bllHomeWifiService.BLLGetDPECancelReasons();

            return Ok(response);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("HomeWifiSubmitOrder")]
        [CustomAuthorizationFilterInternal]
        [HomeWifiSubmitOrderRequestValidator]
        public async Task<IActionResult> HomeWifiSubmitOrder([FromBody][Bind("alternate_mobile,appointment_date,area,cancelation_reason,customer_name,delivery_address,devices,district,email,imei_device_name,initiator_channel,is_activation_done,is_canceled,is_imei_updated,is_payment_method_changed,is_payslip_uploaded,mobile,new_identifier,nw_assess_id,nw_assess_status,offer_code,offer_name,old_identifier,order_assigned_at,order_date,order_number,order_status,order_type,ordered_msisdn,payment_status,payment_type,remarks,retailer_code,simkit_type,subscription_type,total_amount")] HomeWifiDEPOrderRequestModel model)
        {
            try
            {
                var response = await _bllHomeWifiService.BLLUpsertDEPOrder(model);

                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "UpsertDEPOrder Controller Exception");

                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Unexpected error occurred.",
                    data = null
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("HomeWifiNewConnectionSubmitOrder")]
        [CustomAuthorizationFilterInternal]
        [HomeWifiNewConnectionRequestValidator]
        public async Task<IActionResult> HomeWifiNewConnectionSubmitOrder([FromBody][Bind("alt_msisdn,bi_token_number,bts_code,channel_name,cid,customer_name,dest_dob,dest_imsi,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,initiator_channel,isBPUser,is_lus,is_paired,lac,latitude,longitude,msisdn,order_number,order_type,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,scanner_id,selected_category,session_token,sim_category,sim_number,simkit_type,subscription_code,subscription_type,subscription_type_id,thana_id,thana_name,village")] HomeWifiNewConnectionRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            string is_error_from_ongoing = string.Empty;
            MessageBuilder messageBuilder = new MessageBuilder();
            string req_id = string.Empty;
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            HomeWifiOrderRequest model = new HomeWifiOrderRequest();
            try
            {
                model = populateModel.HomeWifiNewConnwctionRequestPopulateModel(request);

                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.HomeWifiCastRequestModel(model);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(requestModelBLOB);

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
                    is_error_from_ongoing = "ongoing";
                    orderRes.message = orderValidationResult.message;
                    model.status = (int)EnumRAOrderStatus.Failed; model.err_msg = orderRes.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = await _blJson.GetGenericJsonDataAsync(orderRes);

                    return GenerateResponse(orderRes.isError, orderValidationResult.message, 0, " ");
                }
                #endregion
                #region First insert the request
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;

                model.order_booking_flag = 800;
             
                orderRes = await _orderManager.HomeWifiNewConnectionSubmitOrder(model);

                if (orderRes.isError)
                {
                    model.status = (int)EnumRAOrderStatus.Failed;
                    model.err_msg = orderRes.message;
                    return GenerateResponse(true, orderRes.message, 0, " ");
                }

                model.bi_token_number = Convert.ToDouble(orderRes.data.request_id);
                #endregion
                #region unpaired MSISDN validation

                if (model.is_paired == 0)
                {
                    var channelInfo = await _orderManager.GetInventoryIdByChannelName(model.channel_name);

                    string centerCode = "";

                    if (channelInfo.Item2 == (int)EnumInventoryId.POS) //for channel POS, eshop.
                    {
                        centerCode = await _orderManager.GetCenterCodeByUserName(model.retailer_id);//here retailer_id==userName

                        if (String.IsNullOrEmpty(centerCode))
                        {
                            orderRes.isError = true;
                            orderRes.message = "Retailer's center code not found!";
                            orderRes.data = new DataRes
                            {
                                isEsim = 0,
                                request_id = "0"
                            };
                            model.err_msg = orderRes.message;
                        }
                    }

                    //RACommonResponseRevampV3 msisdnValidationResp = await _bio.ValidateUnpairedMSISDNV6(new UnpairedMSISDNCheckRequest()
                    //{
                    //    mobile_number = model.msisdn,
                    //    sim_number = model.sim_number,
                    //    channel_id = channelInfo.Item1,
                    //    channel_name = model.channel_name,
                    //    center_code = centerCode,
                    //    inventory_id = channelInfo.Item2,
                    //    purpose_number = model.purpose_number,
                    //    retailer_id = model.retailer_id,
                    //    sim_category = model.sim_category

                    //}, "ValidateUnpairedMSISDN");

                    //if (msisdnValidationResp.isError == true)
                    //{
                    //    model.status = (int)EnumRAOrderStatus.Failed;
                    //    orderRes.data = new DataRes()
                    //    {
                    //        request_id = "0"
                    //    };
                    //    orderRes.isError = true;
                    //    orderRes.message = msisdnValidationResp.message;
                    //    model.err_msg = orderRes.message;

                    //    return GenerateResponse(true, msisdnValidationResp.message, 0, " ");
                    //}
                }
                else
                {
                    var subscriptionValues = SettingsValues.GetSubscriptionCode();

                    var configValues = subscriptionValues.Contains(',') ? subscriptionValues.Split(',') : new string[] { subscriptionValues };

                    if (configValues.Any(x => x == model.subscription_code?.ToLower()))
                    {
                        #region ICC checking from DMS 
                        ICCDetailsRequestModel modelICC = new ICCDetailsRequestModel()
                        {
                            center_code = model.center_code,
                            icc = model.sim_number,
                            retailer_id = model.retailer_id,
                            mobile_number = model.msisdn
                        };

                        ICCDetailsResponse? iccData = await _apiManager.CheckICCfromDMS(modelICC);

                        if (iccData != null && iccData.result)
                        {
                            model.package_code = iccData.offer_name;
                        }
                        else
                        {
                            model.status = (int)EnumRAOrderStatus.Failed;
                            orderRes.data.request_id = "0";
                            orderRes.isError = true;
                            orderRes.message = iccData?.message;
                            model.err_msg = orderRes.message;
                            return GenerateResponse(true, iccData?.message ?? "Unknown error", 0, " ");
                        }
                        #endregion
                    }
                }

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
                    orderRes.isError = true;
                    orderRes.message = imsiResp.message;
                    model.err_msg = orderRes.message;

                    return GenerateResponse(true, imsiResp.message, 0, " ");
                }
                else
                {
                    model.dest_imsi = imsiResp.imsi;//[Note: here IMSI is being sent as SIM number as per business requirement]
                }
                #endregion
                #region bio verification

                //if (!String.IsNullOrEmpty(verifyResp.Reservation_Id))
                //{
                    if (model.is_paired == 0 && Convert.ToInt32(model.purpose_number) == (int)EnumPurposeNumber.NewRegistration)
                    {
                        await _apiCall.UnreserveMSISDNV2(verifyResp.Reservation_Id, model.session_token, "", model.bi_token_number?.ToString() ?? "", model.msisdn, model.retailer_id);
                    }
                //}

                var pursedData = await _orderManager.SubmitOrderDataPurseV2(model);
                BiomerticDataModel dataModel = bioverifyDataMapp(pursedData);

                verifyResp = await _bio.BssServiceProcessV2(dataModel);

                if (verifyResp.is_success == true)
                {
                    model.bss_reqId = verifyResp.bss_req_id;
                    model.status = (int)EnumRAOrderStatus.BioVerificationSubmitted;
                    model.msisdnReservationId = verifyResp.Reservation_Id;
                }
                else
                {
                    await _apiCall.MSISDNReservation(dataModel);
                    model.status = (int)EnumRAOrderStatus.Failed;
                    model.err_code = verifyResp.err_code;
                    model.err_msg = verifyResp.err_msg;
                    model.error_id = verifyResp.error_Id;
                }
                req_id = orderRes.data != null ? orderRes.data.request_id : "0";
                return GenerateResponse(orderRes.isError, orderRes.message, 0, req_id);
                #endregion
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
                if (verifyResp != null)
                {
                    if (!String.IsNullOrEmpty(verifyResp.Reservation_Id))
                    {
                        if (model.is_paired == 0 && Convert.ToInt32(model.purpose_number) == (int)EnumPurposeNumber.NewRegistration)
                        {
                            await _apiCall.UnreserveMSISDNV2(verifyResp.Reservation_Id, model.session_token, "", model.bi_token_number?.ToString() ?? "", model.msisdn, model.retailer_id);
                        }
                    }
                }
                log.res_time = DateTime.Now;
                ErrorDescription error;
                log.is_success = 0;
                model.status = (int)EnumRAOrderStatus.Failed;
                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    if (verifyResp != null)
                    {
                        orderRes.data = new DataRes
                        {
                            request_id = verifyResp.bss_req_id != null ? verifyResp.bss_req_id : "0"
                        };
                    }
                    orderRes.isError = true;
                    orderRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                    model.err_msg = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                    model.err_code = error.error_code;
                    model.error_id = error.error_id;
                    log.res_blob = await _blJson.GetGenericJsonDataAsync(orderRes);
                }
                catch
                {
                    string errMessage = messageBuilder.GetInnerMessage(ex);
                    orderRes.isError = true;
                    orderRes.message = errMessage;
                    model.err_msg = errMessage;
                    log.res_blob = await _blJson.GetGenericJsonDataAsync(orderRes);
                }

                return GenerateResponse(orderRes.isError, orderRes.message, 0, req_id);
            }
            finally
            {
                log.purpose_number = model.purpose_number;
                log.msisdn = _bllLog.FormatMSISDN(model.msisdn);
                log.req_time = DateTime.Now;

                if ((model.bi_token_number != null || model.bi_token_number > 1) && is_error_from_ongoing != "ongoing")
                {
                    response2 = await _orderManager.UpdateOrder(new RAOrderRequestUpdate
                    {
                        bi_token_number = model.bi_token_number,
                        msidn = model.msisdn,
                        dest_imsi = model.dest_imsi,
                        user_name = model.retailer_id,
                        status = model.status,
                        bss_reqId = model.bss_reqId,
                        error_id = model.error_id,
                        err_msg = model.err_msg,
                        msisdnReservationId = model.msisdnReservationId
                    });
                }
                if (orderRes != null)
                {
                    if (orderRes.data != null)
                    {
                        log.bi_token_number = orderRes.data.request_id;
                        log.is_success = orderRes.data.request_id.Length > 1 ? 1 : 0;
                    }
                }
                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(orderRes);
                log.method_name = "HomeWifiNewConnectionSubmitOrder";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = !String.IsNullOrEmpty(model.err_msg) ? model.err_msg : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [CustomAuthorizationFilterInternal]
        [HomeWifiSIMReplacementOrderRequestValidator]
        [Route("HomeWifiSIMReplacementSubmitOrder")]
        public async Task<IActionResult> HomeWifiSIMReplacementSubmitOrder([FromBody][Bind("alt_msisdn,center_code,channel_name,cid,customer_id,customer_name,dbss_subscription_id,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,distributor_code,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,initiator_channel,isBPUser,lac,latitude,longitude,msisdn,old_sim_number,order_number,order_type,payment_type,postal_code,purpose_number,retailer_id,right_id,road_number,saf_status,scanner_id,session_token,sim_number,sim_rep_reason_id,sim_replc_reason,simkit_type,subscription_type,thana_id,thana_name,village")] HomeWifiSimReplacementRequestModel request)
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
            string is_error_from_ongoing = string.Empty;
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            HomeWifiOrderRequest model = new HomeWifiOrderRequest();
            try
            {
                model = populateModel.HomeWifiSIMReplacementRequestPopulateModel(request);

              
                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.HomeWifiCastRequestModel(model);

                log.req_blob = _blJson.GetGenericJsonData(requestModelBLOB);


               
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
                    var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest()
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
                        return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                        {
                            isError = true,
                            message = simResp.message
                        });
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

                VMValidateOrder vMValidateOrder = new VMValidateOrder()
                {
                    msisdn = model.msisdn,
                    sim_number = model.sim_number,
                    purpose_number = Convert.ToInt32(model.purpose_number),
                    is_corporate = 0,
                    retailer_id = model.retailer_id,
                    dest_dob = DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat)
                };

                var orderValidationResult = await _orderManager.ValidateOrder(vMValidateOrder);
                if (orderValidationResult.result == false)
                {
                    //orderRes.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = orderValidationResult.message;
                    log.remarks = orderValidationResult.message;
                    is_error_from_ongoing = "ongoing";
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                #endregion

                #region Insert_Order
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;
              
                orderRes = await _orderManager.HomeWifiSubmitOrderV2(model);
                #endregion

                if (orderRes.isError)
                {
                    return Ok(new SendOrderResponse()
                    {
                        is_success = false,
                        message = orderRes.message
                    });
                }
                else
                {
                    try
                    {
                        model.bi_token_number = Convert.ToDouble(orderRes.data.request_id);
               
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

                        verifyResp = await _bio.BssServiceProcessV2(dataModel);

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
                model.err_msg = orderRes.message;
                log.res_blob = _blJson.GetGenericJsonData(error);
            }
            finally
            {
                log.purpose_number = model.purpose_number;
                log.msisdn = _bllLog.FormatMSISDN(model.msisdn);
                log.req_time = DateTime.Now;
                if (model.bi_token_number != null && model.bi_token_number > 1 && is_error_from_ongoing != "ongoing")
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
                log.method_name = "HomeWifiSIMReplacementSubmitOrder";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null
                                && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(orderRes);
        }


        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("HomeWifiTOSSubmitOrder")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> HomeWifiTOSSubmitOrder([FromBody][Bind("alt_msisdn,bi_token_number,channel_name,cid,customer_name,dbss_subscription_id,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,initiator_channel,isBPUser,lac,latitude,longitude,msisdn,old_sim_number,order_number,order_type,postal_code,purpose_number,retailer_id,right_id,road_number,scanner_id,session_token,simkit_type,src_dob,src_left_index,src_left_index_score,src_left_thumb,src_left_thumb_score,src_nid,src_owner_customer_id,src_payer_customer_id,src_right_index,src_right_index_score,src_right_thumb,src_right_thumb_score,src_sim_category,src_user_customer_id,subscription_type,thana_id,thana_name,village")] HomeWifiTOSRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            NidDobInfoResponse nidDobInfo = new NidDobInfoResponse();
            TosNidToNidMsisdnCheckRequest tosNidToNid = new TosNidToNidMsisdnCheckRequest();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            HomeWifiOrderRequest model = new HomeWifiOrderRequest();
            string is_error_from_ongoing = string.Empty;

            try
            {
                model = populateModel.HomeWifiTOSRequestPopulateModel(request);

                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.HomeWifiCastRequestModel(model);

                log.req_blob = _blJson.GetGenericJsonData(requestModelBLOB);

                #region Get NID DOB
                tosNidToNid.mobile_number = model.msisdn;
                tosNidToNid.purpose_number = model.purpose_number;
                tosNidToNid.retailer_id = model.retailer_id;

                nidDobInfo = await GetNidDobForTOS(tosNidToNid);

                if (nidDobInfo.result == false)
                {
                    orderRes.data = new DataRes()
                    {
                        request_id = "0",
                        isEsim = 0
                    };
                    orderRes.isError = true;
                    orderRes.message = nidDobInfo.message;
                    model.err_msg = orderRes.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                else
                {
                    model.src_nid = nidDobInfo.src_nid;
                    model.src_dob = nidDobInfo.src_dob;
                    model.src_user_customer_id = nidDobInfo.src_user_customer_id;
                    model.src_owner_customer_id = nidDobInfo.src_owner_customer_id;
                    model.src_payer_customer_id = nidDobInfo.src_payer_customer_id;
                    model.dbss_subscription_id = Convert.ToInt32(nidDobInfo.dbss_subscription_id);
                    model.old_sim_number = nidDobInfo.old_sim_number;
                    model.src_sim_category = nidDobInfo.src_sim_category;
                }
                #endregion

                //Order Model Validation_New
                var validateResponse = modelValidation.OrderSubmitModelValidation(new ValidationPropertiesModel
                {
                    purpose_number = model.purpose_number,
                    msisdn = model.msisdn,
                    customer_name = model.customer_name,
                    gender = model.gender,
                    division_id = model.division_id,
                    district_id = model.district_id,
                    thana_id = model.thana_id,
                    village = model.village
                });
                if (!validateResponse.result)
                {
                    return Ok(new SendOrderResponseRev()
                    {
                        isError = true,
                        message = validateResponse.message
                    });
                }
                //=== Ordedr model validation ===
                BLLRAReqModelValidation bLLRAReqModelValidation = new BLLRAReqModelValidation();
                bLLRAReqModelValidation.ValidateOrderReqV2(model);


                #region Check if submitted order is already in process or not.
                var orderValidationResult = await _orderManager.ValidateOrder(new VMValidateOrder
                {
                    msisdn = model.msisdn,
                    purpose_number = Convert.ToInt16(model.purpose_number),
                    is_corporate = 0,
                    retailer_id = model.retailer_id,
                    dest_dob = model.dest_dob
                });

                if (orderValidationResult.result == false)
                {
                    is_error_from_ongoing = "ongoing";
                    orderRes.data = new DataRes()
                    {
                        request_id = "0",
                        isEsim = 0
                    };
                    is_error_from_ongoing = "ongoing";
                    orderRes.isError = true;
                    orderRes.message = orderValidationResult.message;
                    model.err_msg = orderRes.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                #endregion
                #region Insert_Order
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;
                

                orderRes = await _orderManager.HomeWifiSubmitOrderV2(model);
                if (orderRes.isError)
                {
                    return Ok(new SendOrderResponseRev()
                    {
                        isError = true,
                        message = orderRes.message
                    });
                }
                model.bi_token_number = orderRes.data != null ? Convert.ToDouble(orderRes.data.request_id) : 0;
                #endregion
                try
                {

                    #region Get IMSI
                    var imsiResp = await _bio.GetImsiBySimAsync(new GetImsiReq
                    {
                        purpose_number = model.purpose_number,
                        retailer_id = model.retailer_id,
                        sim = model.old_sim_number,
                        msisdn = model.msisdn
                    });

                    if (imsiResp.result == false)
                    {
                        model.status = (int)EnumRAOrderStatus.Failed;
                        orderRes.data = new DataRes()
                        {
                            request_id = "0",
                            isEsim = 0
                        };
                        orderRes.isError = true;
                        orderRes.message = imsiResp.message;
                        model.err_msg = orderRes.message;
                        return Ok(orderRes);
                    }
                    else
                    {
                        model.dest_imsi = imsiResp.imsi;//[Note: here IMSI is being sent as SIM number as per business requirement]
                    }
                    #endregion

                    #region bio verification 

                    var parsedData = await _orderManager.SubmitOrderDataPurseV2(model);
                    BiomerticDataModel dataModel = bioverifyDataMappTOS(parsedData);
                    verifyResp = await _bio.BssServiceProcessV2(dataModel);

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
                    Log.Error(ex, "ExMessage");
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

                orderRes.data = new DataRes()
                {
                    request_id = "",
                    isEsim = 0
                };
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

                if (model.bi_token_number != null && model.bi_token_number > 1 && is_error_from_ongoing != "ongoing")
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
                log.method_name = "HomeWifiTOSSubmitOrder";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(orderRes);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [HomeWifiMNPRequestValidator]
        [CustomAuthorizationFilterInternal]
        [Route("HomeWifiMNPPortInSubmitOrder")]
        public async Task<IActionResult> HomeWifiMNPPortInSubmitOrder([FromBody][Bind("alt_msisdn,bi_token_number,center_code,channel_name,cid,customer_name,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,distributor_code,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,initiator_channel,isBPUser,is_paired,lac,latitude,longitude,msisdn,order_number,order_type,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,salesman_code,scanner_id,session_token,sim_category,sim_number,simkit_type,subscription_code,subscription_type,subscription_type_id,thana_id,thana_name,village")] HomeWifiMNPSubmitRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            string is_error_from_ongoing = string.Empty;
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            HomeWifiOrderRequest model = new HomeWifiOrderRequest();
            try
            {
                model = populateModel.HomeWifiMNPSubmitRequestPopulateModel(request);
                var _blJson = new BL_Json();
                var modelValidation = new ModelValidation();
                

                

                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.HomeWifiCastRequestModel(model);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(requestModelBLOB);
                

                // Check for duplicate order
                var orderValidation = await _bLLOrder.ValidateOrder(new VMValidateOrder
                {
                    msisdn = model.msisdn,
                    sim_number = model.sim_number,
                    purpose_number = Convert.ToInt32(model.purpose_number),
                    is_corporate = 0,
                    retailer_id = model.retailer_id,
                    dest_dob = DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat)
                });

                if (!orderValidation.result)
                {
                    orderRes.data = new DataRes { request_id = "0" };
                    orderRes.isError = true;
                    orderRes.message = orderValidation.message;
                    is_error_from_ongoing = "ongoing";
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    log.res_time = DateTime.Now;
                    log.is_success = 0;
                    return Ok(orderRes);
                }

                // Insert order
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;
                
                orderRes = await _bLLOrder.HomeWifiSubmitOrderV2(model);

                if (orderRes.isError)
                    return Ok(new SendOrderResponseRev { isError = true, message = orderRes.message });

                if (orderRes.data?.request_id != null && double.TryParse(orderRes.data.request_id.ToString(), out var requestId))
                    model.bi_token_number = requestId;
                else
                    model.bi_token_number = 0;

                // MSISDN validation (MNP)
                var channelInfo = await _bLLOrder.GetInventoryIdByChannelName(model.channel_name);

               


                // Get IMSI
                var imsiResp = await _bio.GetImsiBySimAsync(new GetImsiReq
                {
                    purpose_number = model.purpose_number,
                    retailer_id = model.retailer_id,
                    sim = model.sim_number,
                    msisdn = model.msisdn
                });

                if (!imsiResp.result)
                {
                    model.status = (int)EnumRAOrderStatus.Failed;
                    orderRes.data = new DataRes { request_id = "0" };
                    orderRes.isError = true;
                    orderRes.message = imsiResp.message;
                    model.err_msg = imsiResp.message;
                    return Ok(orderRes);
                }

                model.dest_imsi = imsiResp.imsi;

                // Bio verification
                var parsedData = await _bLLOrder.SubmitOrderDataPurseV2(model);
                var bioModel = bioverifyDataMapp(parsedData);
                verifyResp = await _bio.BssServiceProcessV2(bioModel);

                if (verifyResp.is_success)
                {
                    model.bss_reqId = verifyResp.bss_req_id;
                    model.status = (int)EnumRAOrderStatus.BioVerificationSubmitted;
                }
                else
                {
                    model.status = (int)EnumRAOrderStatus.Failed;
                    model.err_code = verifyResp.err_code;
                    model.err_msg = verifyResp.err_msg;
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
                log.is_success = 0;

                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                orderRes.data = new DataRes { request_id = verifyResp?.bss_req_id ?? "0" };
                orderRes.isError = true;
                orderRes.message = error.error_custom_msg ?? error.error_description;
                model.err_msg = error.error_custom_msg ?? error.error_description;
                log.res_blob = new BL_Json().GetGenericJsonData(orderRes);
            }
            finally
            {
                log.purpose_number = model.purpose_number;
                log.msisdn = _bllLog.FormatMSISDN(model.msisdn);
                log.req_time = DateTime.Now;

                if (model.bi_token_number > 1 && is_error_from_ongoing != "ongoing")
                {
                    response2 = await _bLLOrder.UpdateOrder(new RAOrderRequestUpdate
                    {
                        bi_token_number = model.bi_token_number,
                        msidn = model.msisdn,
                        dest_imsi = model.dest_imsi,
                        user_name = model.retailer_id,
                        status = model.status,
                        bss_reqId = model.bss_reqId,
                        error_id = model.error_id,
                        err_msg = model.err_msg,
                        msisdnReservationId = model.msisdnReservationId
                    });
                }
                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(orderRes);
                log.method_name = "HomeWifiMNPPortInSubmitOrder";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = !String.IsNullOrEmpty(model.err_msg) ? model.err_msg : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }

            return Ok(orderRes);
        }


        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("HomeWifiOrderStatus")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> GetDEPOrderStatus([FromBody][Bind("retailer_code")] HomeWifiLeadListRequestModel model)
        {
            if (model == null)
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Request body is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.retailer_code))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "retailer_code is required.",
                    data = null
                });
            }

            try
            {
               
                var response =
                    await _bllHomeWifiService.BLLGetDEPOrderStatus(
                        model.retailer_code
                    );

                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetDEPOrderStatus Controller Exception");

                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Unexpected error occurred.",
                    data = null
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("HomeWifiNetworkAssessment")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> HomeWifiNetworkAssessment([FromBody][Bind("nw_assess_id,nw_assess_status,order_number,order_type,retailer_code")] HomeWifiNetworkAssessmentRequestModel model)
        {
            if (model == null)
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Request body is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.order_number))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "order_number is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.retailer_code))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "retailer_code is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.nw_assess_id))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "nw_assess_id is required.",
                    data = null
                });
            }

            if (model.nw_assess_status == null || string.IsNullOrWhiteSpace(model.nw_assess_status.ToString()))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "nw_assess_status is required.",
                    data = null
                });
            }

            var response =
                await _bllHomeWifiService.BLLHomeWifiNetworkAssessment(model);

            return Ok(response);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("HomeWifiPayslipUpload")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> HomeWifiPayslipUpload([FromForm] HomeWifiPayslipUploadRequestModel model)
        {
            if (model == null)
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Request body is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.order_number))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "order_number is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.retailer_code))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "retailer_code is required.",
                    data = null
                });
            }

            //------------------------------------------------
            // PAYSLIP IMAGE REQUIRED
            //------------------------------------------------
            if (model.payslip_image == null ||
                model.payslip_image.Length == 0)
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "payslip_image is required.",
                    data = null
                });
            }

            //------------------------------------------------
            // VALID EXTENSION CHECK
            //------------------------------------------------
            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            string extension =
                Path.GetExtension(model.payslip_image.FileName)?
                    .ToLower() ?? string.Empty;

            if (!allowedExtensions.Contains(extension))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Accepted file formats are jpg, jpeg, png, webp.",
                    data = null
                });
            }

            //------------------------------------------------
            // MAX SIZE 1MB
            //------------------------------------------------
            long maxFileSize = 1 * 1024 * 1024;

            if (model.payslip_image.Length > maxFileSize)
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Maximum allowed file size is 1MB.",
                    data = null
                });
            }

            //------------------------------------------------
            // CONTENT TYPE VALIDATION
            //------------------------------------------------
            string[] allowedContentTypes =
            {
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/webp"
             };

            string contentType =
                model.payslip_image.ContentType?.ToLower()
                ?? string.Empty;

            if (!allowedContentTypes.Contains(contentType))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Invalid file content type.",
                    data = null
                });
            }

            //------------------------------------------------
            // EXECUTE
            //------------------------------------------------
            var response =
                await _bllHomeWifiService.BLLHomeWifiPayslipUpload(model);

            return Ok(response);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("HomeWifiPaymentMethodChange")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> HomeWifiPaymentMethodChange([FromBody][Bind("order_number,payment_type,retailer_code")] HomeWifiPaymentMethodChangeRequestModel model)
        {
            if (model == null)
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Request body is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.order_number))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "order_number is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.retailer_code))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "retailer_code is required.",
                    data = null
                });
            }

            var response =
                await _bllHomeWifiService.BLLHomeWifiPaymentMethodChange(model);

            return Ok(response);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("HomeWifiIMEIUpdate")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> HomeWifiIMEIUpdate([FromBody][Bind("device_name,new_identifier,old_identifier,order_number,ordered_msisdn,retailer_code")] HomeWifiIMEIUpdateRequestModel model)
        {
            if (model == null)
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Request body is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.order_number))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "order_number is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.retailer_code))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "retailer_code is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.old_identifier))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "old_identifier is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.new_identifier))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "new_identifier is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.device_name))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "device_name is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.ordered_msisdn))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "ordered_msisdn is required.",
                    data = null
                });
            }

            var response = await _bllHomeWifiService.BLLHomeWifiIMEIUpdate(model);
            return Ok(response);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("HomeWifiCheckPayment")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> HomeWifiCheckPayment([FromBody][Bind("order_number,retailer_code")] HomeWifiLeadDetailsRequestModel model)
        {
            if (model == null)
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Request body is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.retailer_code))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "retailer_code is required.",
                    data = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.order_number))
            {
                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "order_number is required.",
                    data = null
                });
            }

            var response = await _bllHomeWifiService.BLLHomeWifiCheckPayment(model);

            return Ok(response);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("Refer")]
        [CustomAuthorizationFilterInternal]
        [HomeWifiReferSubmitOrderRequestValidator]
        public async Task<IActionResult> HomeWifiReferSubmitOrder([FromBody][Bind("alternate_mobile,appointment_date,area_code,channel_name,customer_name,delivery_address,device_code,device_name,district_code,email,mobile,nationality,nid_number,package_code,plan_code,plan_name,remarks,retailer_id,subscription_code")] HomeWifiReferOrderRequest model)
        {
            try
            {
                var response = await _bllHomeWifiService.BLLHomeWifiRefer(model);

                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "UpsertDEPOrder Controller Exception");

                return Ok(new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Unexpected error occurred.",
                    data = null
                });
            }
        }


        #region Helper Methods

        private IActionResult GenerateResponse(bool status, string message, int isEsim, string requestId)
        {
            return Ok(new RACommonResponseRevamp
            {
                isError = status,
                message = message,
                data = new Datas { isEsim = isEsim, request_id = requestId }
            });
        }
        public BiomerticDataModel bioverifyDataMapp(HomeWifiOrderRequest3 order)
        {
            BiomerticDataModel resp = new BiomerticDataModel();
            if (order != null)
            {
                if (order.purpose_number != null)
                    resp.purpose_number = (int)order.purpose_number;
                if (order.dest_doc_type_no != null)
                    resp.dest_doc_type_no = order.dest_doc_type_no.ToString() ?? "";
                if (!String.IsNullOrEmpty(order.dest_nid))
                    resp.dest_doc_id = order.dest_nid;
                if (!String.IsNullOrEmpty(order.retailer_id))
                    resp.user_id = order.retailer_id;
                resp.msisdn = order.msisdn;
                if (order.dest_ec_verifi_reqrd != null)
                    resp.dest_ec_verification_required = (int)order.dest_ec_verifi_reqrd;
                if (!String.IsNullOrEmpty(order.dest_imsi))
                    resp.dest_imsi = order.dest_imsi;
                resp.dest_foreign_flag = 0;
                resp.status = order.status;
                if (order.sim_category != null)
                    resp.sim_category = (int)order.sim_category;
                resp.dest_dob = order.dest_dob;
                resp.create_date = DateTime.Now.ToString();
                resp.dest_left_thumb = order.dest_left_thumb;
                resp.dest_left_index = order.dest_left_index;
                resp.dest_right_thumb = order.dest_right_thumb;
                resp.dest_right_index = order.dest_right_index;
                if (!String.IsNullOrEmpty(order.sim_number))
                    resp.sim_number = order.sim_number;

                if (order.is_paired != null)
                {
                    resp.is_paired = (int)order.is_paired;
                }
                else
                {
                    resp.is_paired = 0;
                }

                if (order.src_doc_type_no != null)
                    resp.src_doc_type_no = order.src_doc_type_no.ToString() ?? "";
            }

            return resp;
        }
        public async Task<NidDobInfoResponse> GetNidDob(IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest)
        {
            NidDobInfoResponse nidDobInfo = new NidDobInfoResponse();
            BIAToDBSSLog log = new BIAToDBSSLog();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
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
                //JObject dbssResp = JObject.Parse("{\"data\":{\"attributes\":{\"activation-time\":\"2025-06-03T09:59:32+06:00\",\"allow-reactivation\":false,\"contract-id\":\"403716\",\"contract-status\":\"rollover\",\"directory-listing\":\"none\",\"first-call-date\":null,\"language\":\"bn\",\"latest-contract-termination-time\":\"2027-06-03T09:59:32+06:00\",\"loan-category-id\":\"1\",\"loan-category-name\":\"Slab-1\",\"monthly-costs\":0,\"msisdn\":\"8801953020377\",\"original-contract-confirmation-code\":\"O3M4P936N2Q5\",\"payment-type\":\"postpaid\",\"status\":\"active\",\"termination-time\":\"3000-01-01T00:00:00+06:00\"},\"id\":\"402204\",\"links\":{\"self\":\"/api/v1/subscriptions/402204\"},\"relationships\":{\"available-child-products\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/available-child-products\"}},\"available-loan-products\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/available-loan-products\"}},\"available-products\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/available-products\"}},\"available-subscription-types\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/available-subscription-types\"}},\"balances\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/balances\"}},\"barrings\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/barrings\"}},\"billing-accounts\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/billing-accounts\"},\"data\":[{\"id\":\"408417\",\"type\":\"billing-accounts\"},{\"id\":\"408420\",\"type\":\"billing-accounts\"}]},\"billing-rate-plan\":{\"data\":{\"id\":\"60\",\"type\":\"billing-rate-plans\"},\"links\":{\"related\":\"/api/v1/subscriptions/402204/billing-rate-plan\"}},\"billing-usages\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/billing-usages\"}},\"catalog-sim-cards\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/catalog-sim-cards\"}},\"combined-usage-reports\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/combined-usage-reports\"}},\"connected-products\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/connected-products\"}},\"connection-type\":{\"data\":{\"id\":\"2\",\"type\":\"connection-types\"},\"links\":{\"related\":\"/api/v1/subscriptions/402204/connection-type\"}},\"coordinator-customer\":{\"data\":null,\"links\":{\"related\":\"/api/v1/subscriptions/402204/coordinator-customer\"}},\"document-validations\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/document-validations\"}},\"gsm-service-usages\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/gsm-service-usages\"}},\"network-services\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/network-services\"}},\"owner-customer\":{\"data\":{\"id\":\"1085760171\",\"type\":\"customers\"},\"links\":{\"related\":\"/api/v1/subscriptions/402204/owner-customer\"}},\"payer-customer\":{\"data\":{\"id\":\"1085760174\",\"type\":\"customers\"},\"links\":{\"related\":\"/api/v1/subscriptions/402204/payer-customer\"}},\"porting-requests\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/porting-requests\"}},\"product-usages\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/product-usages\"}},\"products\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/products\"}},\"services\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/services\"}},\"sim-card-orders\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/sim-card-orders\"}},\"sim-cards\":{\"data\":[{\"id\":\"primary-410658\",\"type\":\"sim-cards\"}],\"links\":{\"related\":\"/api/v1/subscriptions/402204/sim-cards\"}},\"subscription-discounts\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/subscription-discounts\"}},\"subscription-type\":{\"data\":{\"id\":\"129\",\"type\":\"subscription-types\"},\"links\":{\"related\":\"/api/v1/subscriptions/402204/subscription-type\"}},\"user-customer\":{\"data\":{\"id\":\"1085760174\",\"type\":\"customers\"},\"links\":{\"related\":\"/api/v1/subscriptions/402204/user-customer\"}}},\"type\":\"subscriptions\"},\"included\":[{\"attributes\":{\"account-type\":null,\"agreement-start-date\":null,\"alt-contact-phone\":\"unknown\",\"ban\":null,\"bank-account-number\":null,\"business-uid\":null,\"category\":\"consumer\",\"contact-phone\":\"unknown\",\"coordinator-id\":null,\"date-of-birth\":\"1994-10-11\",\"email\":\"unknown@gmail.com\",\"first-name\":\"unknown\",\"frame-agreement-ended-at\":null,\"frame-agreement-started-at\":null,\"gender\":\"notitle\",\"id-document-number\":\"7306264289\",\"id-document-type\":\"smart_national_id\",\"id-expiry\":null,\"invoice-delivery-type\":\"delivery_not_needed\",\"is-company\":false,\"is-coordinator\":false,\"is-fleet-manager\":false,\"is-loyalty-manager\":false,\"language\":\"bn\",\"last-name\":\"unknown\",\"marketing-own\":true,\"marketing-third-party\":true,\"middle-name\":null,\"nationality\":\"BD\",\"occupation\":\"unknown\",\"online-id\":null,\"payment-method\":\"bank_payment\",\"segmentation-category\":\"001\",\"trade-register-id\":null,\"vat-usage-code\":\"domestic\"},\"id\":\"1085760171\",\"links\":{\"self\":\"/api/v1/customers/1085760171\"},\"relationships\":{\"addresses\":{\"links\":{\"related\":\"/api/v1/customers/1085760171/addresses\"}},\"company-people\":{\"links\":{\"related\":\"/api/v1/customers/1085760171/company-people\"}},\"contact-companies\":{\"links\":{\"related\":\"/api/v1/customers/1085760171/contact-companies\"}},\"coordinator-customer\":{\"data\":null,\"links\":{\"related\":\"/api/v1/customers/1085760171/coordinator-customer\"}},\"customer-edit-permission\":{\"data\":{\"id\":\"1085760171\",\"type\":\"customer-edit-permissions\"},\"links\":{\"related\":\"/api/v1/customers/1085760171/customer-edit-permission\"}},\"inventory\":{\"data\":{\"id\":\"1085760171\",\"type\":\"inventories\"},\"links\":{\"related\":\"/api/v1/customers/1085760171/inventory\"}},\"orders\":{\"links\":{\"related\":\"/api/v1/customers/1085760171/orders\"}}},\"type\":\"customers\"},{\"attributes\":{\"icc\":\"898803991653949847\",\"is-multi-surf\":false,\"pin-1\":\"1234\",\"pin-2\":\"7152\",\"puk-1\":\"59293335\",\"puk-2\":\"24332788\",\"sim-type\":\"USIM\",\"status\":\"primary\"},\"id\":\"primary-410658\",\"links\":{\"self\":\"/api/v1/sim-cards/primary-410658\"},\"relationships\":{\"subscription\":{\"data\":{\"id\":\"402204\",\"type\":\"subscriptions\"},\"links\":{\"related\":\"/api/v1/sim-cards/primary-410658/subscription\"}}},\"type\":\"sim-cards\"},{\"attributes\":{\"account-type\":null,\"agreement-start-date\":null,\"alt-contact-phone\":\"\",\"ban\":null,\"bank-account-number\":null,\"business-uid\":null,\"category\":\"consumer\",\"contact-phone\":\"8801953020377\",\"coordinator-id\":null,\"date-of-birth\":null,\"email\":\"\",\"first-name\":\"rizaul\",\"frame-agreement-ended-at\":null,\"frame-agreement-started-at\":null,\"gender\":\"male\",\"id-document-number\":\"\",\"id-document-type\":\"\",\"id-expiry\":null,\"invoice-delivery-type\":\"post\",\"is-company\":false,\"is-coordinator\":false,\"is-fleet-manager\":false,\"is-loyalty-manager\":false,\"language\":\"en\",\"last-name\":\"\",\"marketing-own\":true,\"marketing-third-party\":true,\"middle-name\":null,\"nationality\":\"BD\",\"occupation\":\"\",\"online-id\":null,\"payment-method\":\"bank_payment\",\"segmentation-category\":\"001\",\"trade-register-id\":null,\"vat-usage-code\":\"domestic\"},\"id\":\"1085760174\",\"links\":{\"self\":\"/api/v1/customers/1085760174\"},\"relationships\":{\"addresses\":{\"links\":{\"related\":\"/api/v1/customers/1085760174/addresses\"}},\"company-people\":{\"links\":{\"related\":\"/api/v1/customers/1085760174/company-people\"}},\"contact-companies\":{\"links\":{\"related\":\"/api/v1/customers/1085760174/contact-companies\"}},\"coordinator-customer\":{\"data\":null,\"links\":{\"related\":\"/api/v1/customers/1085760174/coordinator-customer\"}},\"customer-edit-permission\":{\"data\":{\"id\":\"1085760174\",\"type\":\"customer-edit-permissions\"},\"links\":{\"related\":\"/api/v1/customers/1085760174/customer-edit-permission\"}},\"inventory\":{\"data\":{\"id\":\"1085760174\",\"type\":\"inventories\"},\"links\":{\"related\":\"/api/v1/customers/1085760174/inventory\"}},\"orders\":{\"links\":{\"related\":\"/api/v1/customers/1085760174/orders\"}}},\"type\":\"customers\"}]}");
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

                var msisdnResp = _dbssToRaParse.HomeWifiSIMReplacementMSISDNReqParsing(dbssResp);

                if (msisdnResp.result == false)
                {
                    nidDobInfo.result = false;
                    nidDobInfo.message = MessageCollection.SIMReplNoDataFound;
                    return nidDobInfo;
                }

                nidDobInfo.dest_nid = msisdnResp.doc_id_number ?? "";
                nidDobInfo.dest_dob = msisdnResp.dob ?? "";
                nidDobInfo.old_sim_type = msisdnResp.old_sim_type;
                nidDobInfo.old_sim_number = msisdnResp.old_sim_number;
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
        public async Task<NidDobInfoResponse> GetNidDobForTOS(TosNidToNidMsisdnCheckRequest msisdnCheckReqest)
        {
            NidDobInfoResponse nidDobInfo = new NidDobInfoResponse();
            string? apiUrl = string.Empty, txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                string srcMsisdn = msisdnCheckReqest.mobile_number;

                if (srcMsisdn.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    srcMsisdn = FixedValueCollection.MSISDNCountryCode + srcMsisdn;
                }

                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingSimCardsPayerCustomerOwnerCustomerUserCustomer, srcMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetNidDobForTOS");

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null || dbssResp["included"] == null)
                {
                    nidDobInfo.result = false;
                    nidDobInfo.message = MessageCollection.SIMReplNoDataFound;
                    return nidDobInfo;
                }

                TosNidToNidMSISDNCheckResponse msisdnValidationResp = _dbssToRaParse.TosNidToNidMSISDNReqParsingV1(dbssResp);

                if (msisdnValidationResp.result == false)
                {
                    nidDobInfo.result = false;
                    nidDobInfo.message = FixedValueCollection.MSISDNError + msisdnValidationResp.message;
                    return nidDobInfo;
                }

                nidDobInfo.result = true;
                nidDobInfo.src_nid = msisdnValidationResp.doc_id_number;
                nidDobInfo.src_dob = msisdnValidationResp.dob;
                nidDobInfo.src_user_customer_id = msisdnValidationResp.src_user_customer_id;
                nidDobInfo.src_owner_customer_id = msisdnValidationResp.src_owner_customer_id;
                nidDobInfo.src_payer_customer_id = msisdnValidationResp.src_payer_customer_id;
                nidDobInfo.dbss_subscription_id =
                nidDobInfo.dbss_subscription_id = msisdnValidationResp.dbss_subscription_id;
                nidDobInfo.old_sim_number = msisdnValidationResp.old_sim_number;
                nidDobInfo.old_sim_type = msisdnValidationResp.old_sim_type;
                nidDobInfo.src_sim_category = msisdnValidationResp.src_sim_category;

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
                return nidDobInfo;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number;
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "GetNidDobForTOS";

                await _bllLog.RAToDBSSLog(log);
            }
        }
        public BiomerticDataModel bioverifyDataMappTOS(HomeWifiOrderRequest3 order)
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
                resp.msisdn = order.msisdn;
                if (order.dest_ec_verifi_reqrd != null)
                    resp.dest_ec_verification_required = (int)order.dest_ec_verifi_reqrd;
                if (!String.IsNullOrEmpty(order.dest_imsi))
                    resp.dest_imsi = order.dest_imsi;
                if (order.dest_foreign_flag != null)
                    resp.dest_foreign_flag = (int)order.dest_foreign_flag;
                if (order.sim_category != null)
                {
                    resp.sim_category = (int)order.sim_category;
                }
                else
                {
                    resp.sim_category = 0;
                }
                resp.dest_dob = order.dest_dob;
                if (!String.IsNullOrEmpty(order.src_dob))
                    resp.src_dob = order.src_dob;
                if (order.src_sim_category != null)
                    resp.src_sim_category = (int)order.src_sim_category;

                resp.dest_left_thumb = order.dest_left_thumb;
                resp.dest_left_index = order.dest_left_index;
                resp.dest_right_thumb = order.dest_right_thumb;
                resp.dest_right_index = order.dest_right_index;
                resp.src_left_index = order.src_left_index;
                resp.src_left_thumb = order.src_left_thumb;
                resp.src_right_index = order.src_right_index;
                resp.src_right_thumb = order.src_right_thumb;

                if (order.src_doc_type_no != null)
                    resp.src_doc_type_no = order.src_doc_type_no.ToString() ?? "";
                if (order.src_ec_verifi_reqrd != null)
                    resp.src_ec_verification_required = (int)order.src_ec_verifi_reqrd;
                if (!String.IsNullOrEmpty(order.src_nid))
                    resp.src_doc_id = order.src_nid;
            }
            return resp;

        }
        #endregion



        

    }
}