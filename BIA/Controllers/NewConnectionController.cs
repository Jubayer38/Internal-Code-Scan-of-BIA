///************************************************************************
///	|| Creation History ||
///-----------------------------------------------------------------------
///	Copyright     :	Copyright© NAAS Solutions Limited. All rights reserved.
///	Author	      :	Mohiuddin
///	Purpose	      :	Activation Confirmation Controller for Physical and ESIM for RESELLER, B2C_postpaid, Corporate, SME, PWA_Router channel
///	Creation Date :	10-Jun-2023
/// =======================================================================
///  || Modification History ||
///  ----------------------------------------------------------------------
///  Sl No.	Date:		    Author:			    Ver:	    Area of Change:
///  1.     
///	 ----------------------------------------------------------------------
///	***********************************************************************

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
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Net;
using static BIA.Common.ModelValidation;

namespace BIA.Controllers
{
    [Route("api/NewConnection")]
    [ApiController]
    public class NewConnectionController : ControllerBase
    {
        private readonly BLLRAToDBSSParse _raToDBssParse;
        private readonly BLLDBSSToRAParse _dbssToRaParse;
        private readonly ApiRequest _apiReq;
        private readonly BL_Json _blJson;
        private readonly BLLOrder _orderManager;
        private readonly BLLLog _bllLog;
        private readonly BiometricApiCall _apiCall;
        private readonly BaseController _bio;
        private readonly ApiManager _apiManager;
        private readonly GeoFencingValidation _geo;


        public NewConnectionController(BLLRAToDBSSParse raToDBssParse, BLLDBSSToRAParse dbssToRaParse, ApiRequest apiReq, BL_Json blJson, BaseController bio, BLLOrder orderManager, ApiManager apiManager, BLLLog bllLog, BiometricApiCall apiCall, GeoFencingValidation geo)
        {
            _raToDBssParse = raToDBssParse;
            _dbssToRaParse = dbssToRaParse;
            _apiReq = apiReq;
            _blJson = blJson;
            _bio = bio;
            _orderManager = orderManager;
            _apiManager = apiManager;
            _bllLog = bllLog;
            _apiCall = apiCall;
            _geo = geo;
        }

        /// <summary>
        /// This API is used for NewConnection Physical(Paired, Unpaired) Confirmatio/resubmit request from Biometric App.The request process steps is below-
        /// 1. JWT Session Token validation.
        /// 2. Geo fencing for BP/Arranged Users (If any arranged user cross the area from retailer logged in area configured in appsettings.json then he/she will not able to submit the request).
        /// 3. Check if submitted order is already in process or not.
        /// 4. Insert the request in main table (App end) for the first time
        /// 5. MSISDN and SIM serial validation again (if another user is already taken this number during the process)
        /// 6. Get IMSI number by ICC/SIM number from DBSS via API
        /// 7. Send Bio-verification request to DBSS for the EC validation and CBVMP update through Adapter
        /// 8. If Bio-verification request failed but the MSISDN reserved in DBSS then unreserve that MSISDN
        /// 9. Catch block for any exception occured in the above checking
        /// 10. Update the request in database with the above processed value (status, error, bss_request_id, etc)
        /// 11. Finaly insert a log in App end Log table with BLOB Data
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Order request token id</returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [NewConnectionRequestValidator]
        [Route("NewConnectionSubmitOrderV4")]
        public async Task<IActionResult> NewConnectionSubmitOrderV4([FromBody][Bind("alt_msisdn,bi_token_number,bts_code,channel_name,cid,customer_name,dest_dob,dest_imsi,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,is_lus,is_paired,lac,latitude,longitude,msisdn,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,scanner_id,selected_category,session_token,sim_category,sim_number,subscription_code,subscription_type_id,thana_id,thana_name,village")] NewConnectionRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            string is_error_from_ongoing = string.Empty;
            MessageBuilder messageBuilder = new MessageBuilder();
            string req_id = string.Empty;
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();
            try
            {
                model = populateModel.NewConnwctionRequestPopulateModel(request);

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.CastRequestModel(model);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(requestModelBLOB);

                #region Validate token
                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (security.UserName != model.retailer_id)
                        {
                            string error_message = "User is not exist in the session! Unauthorized.";
                            return GenerateResponse(true, error_message, 0, " ");
                        }
                        else
                        {
                            loginProviderId = security.LoginProviderId;
                            model.distributor_code = security.DistributorCode;
                        }
                    }
                    else
                    {
                        return GenerateResponse(true, security.Message, 0, " ");
                    }
                }
                #endregion
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
                model.prov_id = loginProviderId;

                orderRes = await _orderManager.SubmitOrderV7(model);

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

                    RACommonResponse msisdnValidationResp = await _bio.ValidateUnpairedMSISDN(new UnpairedMSISDNCheckRequest()
                    {
                        mobile_number = model.msisdn,
                        sim_number = model.sim_number,
                        channel_id = channelInfo.Item1,
                        channel_name = model.channel_name,
                        center_code = centerCode,
                        inventory_id = channelInfo.Item2,
                        purpose_number = model.purpose_number,
                        retailer_id = model.retailer_id,
                        sim_category = model.sim_category

                    }, "ValidateUnpairedMSISDN");

                    if (msisdnValidationResp.result == false)
                    {
                        model.status = (int)EnumRAOrderStatus.Failed;
                        orderRes.data = new DataRes()
                        {
                            request_id = "0"
                        };
                        orderRes.isError = true;
                        orderRes.message = msisdnValidationResp.message;
                        model.err_msg = orderRes.message;

                        return GenerateResponse(true, msisdnValidationResp.message, 0, " ");
                    }
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
                    if (!String.IsNullOrEmpty(verifyResp.Reservation_Id))
                    {
                        if (model.is_paired == 0 && Convert.ToInt32(model.purpose_number) == (int)EnumPurposeNumber.NewRegistration)
                        {
                            await _apiCall.UnreserveMSISDNV2(verifyResp.Reservation_Id, model.session_token, "", model.bi_token_number?.ToString() ?? "", model.msisdn, model.retailer_id);
                        }
                    }
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
                log.method_name = "NewConnectionSubmitOrderV4";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = !String.IsNullOrEmpty(model.err_msg) ? model.err_msg : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("NewConnectionSubmitOrderV5")]
        [CherishNewConnectionRequestValidator]
        public async Task<IActionResult> NewConnectionSubmitOrderV5([FromBody][Bind("Selected_category,alt_msisdn,bi_token_number,bts_code,channel_name,cid,customer_name,dest_dob,dest_imsi,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,is_lus,is_paired,lac,latitude,longitude,msisdn,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,scanner_id,session_token,sim_category,sim_number,subscription_code,subscription_type_id,thana_id,thana_name,village")] CherishNewConnectionRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            string is_error_from_ongoing = string.Empty;
            CherishRequest model = new CherishRequest();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            try
            {
                model = populateModel.CherishNewConnwctionRequestPopulateModel(request);
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.CastRequestModelV2(model);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(requestModelBLOB);
                #region Validate token
                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (security.UserName != model.retailer_id)
                        {
                            throw new Exception("User is not exist in the session! Unauthorized.");
                        }
                        else
                        {
                            loginProviderId = security.LoginProviderId;
                            model.distributor_code = security.DistributorCode;
                        }
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }
                #endregion
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
                    //orderRes.data.request_id = "0";
                    is_error_from_ongoing = "ongoing";
                    orderRes.isError = true;
                    orderRes.message = orderValidationResult.message;
                    model.err_msg = orderRes.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);

                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = orderRes.isError,
                        message = orderValidationResult.message,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = " "
                        }
                    });
                }
                #endregion
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;

                orderRes = await _orderManager.SubmitOrderV9(model, loginProviderId);

                if (orderRes.isError)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = orderRes.message,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = " "
                        }
                    });
                }

                model.bi_token_number = Convert.ToDouble(orderRes.data.request_id);


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
                            //orderRes.data.request_id = "0";
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

                    string cherish_category_config = SettingsValues.GetCherishCategory();
                    string[] cofigValue;
                    if (cherish_category_config.Contains(","))
                    {
                        cofigValue = cherish_category_config.Split(',');
                    }
                    else
                    {
                        cofigValue = cherish_category_config.Split(' ');
                    }

                    if (cofigValue.Any(x => x.Equals(model.selected_category)))
                    {
                        RACommonResponseRevampV3 msisdnValidationResp = await _bio.ValidateUnpairedMSISDNV6(new UnpairedMSISDNCheckRequest()
                        {
                            mobile_number = model.msisdn,
                            sim_number = model.sim_number,
                            channel_id = channelInfo.Item1,
                            channel_name = model.channel_name,
                            center_code = centerCode,
                            inventory_id = channelInfo.Item2,
                            purpose_number = model.purpose_number,
                            retailer_id = model.retailer_id,
                            sim_category = model.sim_category

                        }, "ValidateUnpairedMSISDNV6");

                        if (msisdnValidationResp.isError == true)
                        {
                            model.status = (int)EnumRAOrderStatus.Failed;
                            orderRes.data = new DataRes()
                            {
                                request_id = "0"
                            };
                            orderRes.isError = true;
                            orderRes.message = msisdnValidationResp.message;
                            model.err_msg = orderRes.message;

                            return Ok(new RACommonResponseRevamp()
                            {
                                isError = true,
                                message = msisdnValidationResp.message,
                                data = new Datas()
                                {
                                    isEsim = 0,
                                    request_id = " "
                                }
                            });
                        }
                    }
                    else
                    {

                        RACommonResponse msisdnValidationResp = await _bio.ValidateUnpairedMSISDN(new UnpairedMSISDNCheckRequest()
                        {
                            mobile_number = model.msisdn,
                            sim_number = model.sim_number,
                            channel_id = channelInfo.Item1,
                            channel_name = model.channel_name,
                            center_code = centerCode,
                            inventory_id = channelInfo.Item2,
                            purpose_number = model.purpose_number,
                            retailer_id = model.retailer_id,
                            sim_category = model.sim_category

                        }, "ValidateUnpairedMSISDN");

                        if (msisdnValidationResp.result == false)
                        {
                            model.status = (int)EnumRAOrderStatus.Failed;
                            orderRes.data = new DataRes()
                            {
                                request_id = "0"
                            };
                            orderRes.isError = true;
                            orderRes.message = msisdnValidationResp.message;
                            model.err_msg = orderRes.message;

                            return Ok(new RACommonResponseRevamp()
                            {
                                isError = true,
                                message = msisdnValidationResp.message,
                                data = new Datas()
                                {
                                    isEsim = 0,
                                    request_id = " "
                                }
                            });
                        }
                    }
                    #endregion                   
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
                    //orderRes.data.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = imsiResp.message;
                    model.err_msg = orderRes.message;


                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = imsiResp.message,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = " "
                        }
                    });
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
                    model.msisdnReservationId = verifyResp.Reservation_Id;
                }
                else
                {
                    if (verifyResp.Reservation_Id != null)
                    {
                        if (model.is_paired == 0 && Convert.ToInt32(model.purpose_number) == (int)EnumPurposeNumber.NewRegistration)
                        {
                            await _apiCall.UnreserveMSISDNV2(verifyResp.Reservation_Id, model.session_token, "", model.bi_token_number.ToString() ?? "", model.msisdn, model.retailer_id);
                        }
                    }
                    model.status = (int)EnumRAOrderStatus.Failed;
                    model.err_code = verifyResp.err_code;
                    model.err_msg = verifyResp.err_msg;
                    model.error_id = verifyResp.error_Id;
                }
                #endregion

                return Ok(new RACommonResponseRevamp()
                {
                    isError = orderRes.isError,
                    message = orderRes.message,
                    data = new Datas()
                    {
                        isEsim = 0,
                        request_id = orderRes.data != null ? orderRes.data.request_id : "0",
                    }
                });
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
                            await _apiCall.UnreserveMSISDNV2(verifyResp.Reservation_Id, model.session_token, "", model.bi_token_number.ToString() ?? "", model.msisdn, model.retailer_id);
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
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                }
                catch (Exception)
                {
                    orderRes.isError = true;
                    orderRes.message = ex.Message;
                    model.err_msg = ex.Message;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                }

                return Ok(new RACommonResponseRevamp()
                {
                    isError = orderRes.isError,
                    message = orderRes.message,
                    data = new Datas()
                    {
                        isEsim = 0,
                        request_id = orderRes.data != null ? orderRes.data.request_id : "0",
                    }
                });
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
                log.method_name = "NewConnectionSubmitOrderV5";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null
                               && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("NewConnectionSubmitOrderV6")]
        [NewConnectionRequestValidator]
        public async Task<IActionResult> NewConnectionSubmitOrderV6([FromBody][Bind("alt_msisdn,bi_token_number,bts_code,channel_name,cid,customer_name,dest_dob,dest_imsi,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,is_lus,is_paired,lac,latitude,longitude,msisdn,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,scanner_id,selected_category,session_token,sim_category,sim_number,subscription_code,subscription_type_id,thana_id,thana_name,village")] NewConnectionRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            string is_error_from_ongoing = string.Empty;
            MessageBuilder messageBuilder = new MessageBuilder();
            string req_id = string.Empty;
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();
            try
            {
                model = populateModel.NewConnwctionRequestPopulateModel(request);

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.CastRequestModel(model);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(requestModelBLOB);

                #region Validate token
                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (security.UserName != model.retailer_id)
                        {
                            string error_message = "User is not exist in the session! Unauthorized.";
                            return GenerateResponse(true, error_message, 0, " ");
                        }
                        else
                        {
                            loginProviderId = security.LoginProviderId;
                            model.distributor_code = security.DistributorCode;
                        }
                    }
                    else
                    {
                        return GenerateResponse(true, security.Message, 0, " ");
                    }
                }
                #endregion
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
                model.prov_id = loginProviderId;

                orderRes = await _orderManager.SubmitOrderV9(model);

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

                    RACommonResponseRevampV3 msisdnValidationResp = await _bio.ValidateUnpairedMSISDNV6(new UnpairedMSISDNCheckRequest()
                    {
                        mobile_number = model.msisdn,
                        sim_number = model.sim_number,
                        channel_id = channelInfo.Item1,
                        channel_name = model.channel_name,
                        center_code = centerCode,
                        inventory_id = channelInfo.Item2,
                        purpose_number = model.purpose_number,
                        retailer_id = model.retailer_id,
                        sim_category = model.sim_category

                    }, "ValidateUnpairedMSISDN");

                    if (msisdnValidationResp.isError == true)
                    {
                        model.status = (int)EnumRAOrderStatus.Failed;
                        orderRes.data = new DataRes()
                        {
                            request_id = "0"
                        };
                        orderRes.isError = true;
                        orderRes.message = msisdnValidationResp.message;
                        model.err_msg = orderRes.message;

                        return GenerateResponse(true, msisdnValidationResp.message, 0, " ");
                    }
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
                    if (!String.IsNullOrEmpty(verifyResp.Reservation_Id))
                    {
                        if (model.is_paired == 0 && Convert.ToInt32(model.purpose_number) == (int)EnumPurposeNumber.NewRegistration)
                        {
                            await _apiCall.UnreserveMSISDNV2(verifyResp.Reservation_Id, model.session_token, "", model.bi_token_number?.ToString() ?? "", model.msisdn, model.retailer_id);
                        }
                    }
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
                log.method_name = "NewConnectionSubmitOrderV6";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = !String.IsNullOrEmpty(model.err_msg) ? model.err_msg : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }
        }

        /// <summary>
        /// This API is used for NewConnection eSIM (Paired, Unpaired) Confirmation//resubmit request from Biometric App.The request process steps is below-
        /// 1. JWT Session Token validation.
        /// 2. Geo fencing for BP/Arranged Users (If any arranged user cross the area from retailer logged in area configured in appsettings.json then he/she will not able to submit the request).
        /// 3. Check if submitted order is already in process or not.
        /// 4. Insert the request in main table (App end) for the first time
        /// 5. MSISDN and SIM serial validation again (if another user is already taken this number during the process)
        /// 6. Get IMSI number by ICC/SIM number from DBSS via API
        /// 7. Send Bio-verification request to DBSS for the EC validation and CBVMP update through Adapter
        /// 8. If Bio-verification request failed but the MSISDN reserved in DBSS then unreserve that MSISDN
        /// 9. Catch block for any exception occured in the above checking
        /// 10. Update the request in database with the above processed value (status, error, bss_request_id, etc)
        /// 11. Finaly insert a log in App end Log table with BLOB Data
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("NewConnectionSubmitOrder_ESIMV2")]
        [NewConnectionRequestValidator]
        public async Task<IActionResult> NewConnectionSubmitOrder_ESIMV2([FromBody][Bind("alt_msisdn,bi_token_number,bts_code,channel_name,cid,customer_name,dest_dob,dest_imsi,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,is_lus,is_paired,lac,latitude,longitude,msisdn,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,scanner_id,selected_category,session_token,sim_category,sim_number,subscription_code,subscription_type_id,thana_id,thana_name,village")] NewConnectionRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            string is_error_from_ongoing = string.Empty;
            RequiestModelCasting modelCasting = new RequiestModelCasting();
            RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();
            MessageBuilder messageBuilder = new MessageBuilder();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();

            try
            {
                model = populateModel.NewConnwctionRequestPopulateModel(request);

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                TokenValidationService token = new TokenValidationService(secreteKey);

                requestModelBLOB = modelCasting.CastRequestModel(model);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(requestModelBLOB);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (security.UserName != model.retailer_id)
                        {
                            string error_message = "User is not exist in the session! Unauthorized.";
                            return GenerateResponse(true, error_message, 0, " ");
                        }
                        else
                        {
                            loginProviderId = security.LoginProviderId;
                            model.distributor_code = security.DistributorCode;
                        }
                    }
                    else
                    {
                        return GenerateResponse(true, security.Message, 0, " ");
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
                    orderRes.data = new DataRes()
                    {
                        request_id = "0"
                    };
                    orderRes.isError = true;
                    is_error_from_ongoing = "ongoing";
                    orderRes.message = orderValidationResult.message;
                    model.status = (int)EnumRAOrderStatus.Failed;
                    model.err_msg = orderRes.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);

                    return GenerateResponse(orderRes.isError, orderRes.message, 1, " ");
                }
                #endregion
                #region Insert_Order
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;
                model.is_esim = 1;
                model.prov_id = loginProviderId;

                orderRes = await _orderManager.SubmitOrderV7(model);
                if (orderRes.isError)
                {
                    return GenerateResponse(true, orderRes.message, 1, " ");
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
                            model.err_msg = orderRes.message;
                            orderRes.data = new DataRes
                            {
                                request_id = "0"
                            };
                        }
                    }

                    RACommonResponse msisdnValidationResp = await _bio.ValidateUnpairedMSISDNV3(new UnpairedMSISDNCheckRequest()
                    {
                        mobile_number = model.msisdn,
                        sim_number = model.sim_number,
                        channel_id = channelInfo.Item1,
                        channel_name = model.channel_name,
                        center_code = centerCode,
                        inventory_id = channelInfo.Item2,
                        purpose_number = model.purpose_number,
                        retailer_id = model.retailer_id,
                        sim_category = model.sim_category

                    }, "ValidateUnpairedMSISDN_ESIMV2");

                    if (msisdnValidationResp.result == false)
                    {
                        model.status = (int)EnumRAOrderStatus.Failed;
                        orderRes.data = new DataRes
                        {
                            request_id = "0"
                        };
                        orderRes.isError = true;
                        orderRes.message = msisdnValidationResp.message;
                        model.err_msg = orderRes.message;

                        return GenerateResponse(orderRes.isError, orderRes.message, 1, orderRes.data.request_id);
                    }
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
                    orderRes.data.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = imsiResp.message;
                    model.err_msg = orderRes.message;
                    return GenerateResponse(orderRes.isError, orderRes.message, 1, orderRes.data.request_id);
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
                    model.msisdnReservationId = verifyResp.Reservation_Id;
                }
                else
                {

                    if (!String.IsNullOrEmpty(verifyResp.Reservation_Id))
                    {
                        if (model.is_paired == 0 && Convert.ToInt32(model.purpose_number) == (int)EnumPurposeNumber.NewRegistration)
                        {
                            await _apiCall.UnreserveMSISDNV2(verifyResp.Reservation_Id, model.session_token, "", model.bi_token_number?.ToString() ?? "", model.msisdn, model.retailer_id);
                        }
                    }

                    model.status = (int)EnumRAOrderStatus.Failed;
                    model.err_code = verifyResp.err_code;
                    model.err_msg = verifyResp.err_msg;
                    model.error_id = verifyResp.error_Id;
                }

                return GenerateResponse(orderRes.isError, orderRes.message, 1, orderRes.data.request_id);
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
                    if (String.IsNullOrEmpty(verifyResp.Reservation_Id))
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

                    orderRes.data = new DataRes()
                    {
                        request_id = verifyResp != null ? verifyResp.bss_req_id : ""
                    };
                    orderRes.isError = true;
                    orderRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                    model.err_msg = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
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
                return GenerateResponse(true, orderRes.message, 1, " ");
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
                        user_name = model.retailer_id,
                        dest_imsi = model.dest_imsi,
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
                log.method_name = "NewConnectionSubmitOrder_ESIMV2";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null
                               && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }
        }
                
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("NewConnectionSubmitOrder_ESIMV3")]
        [CherishNewConnectionRequestValidator]
        public async Task<IActionResult> NewConnectionSubmitOrder_ESIMV3([FromBody][Bind("Selected_category,alt_msisdn,bi_token_number,bts_code,channel_name,cid,customer_name,dest_dob,dest_imsi,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,is_lus,is_paired,lac,latitude,longitude,msisdn,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,scanner_id,session_token,sim_category,sim_number,subscription_code,subscription_type_id,thana_id,thana_name,village")] CherishNewConnectionRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev(); 
            SendOrderResponse2 response2 = new SendOrderResponse2(); 
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            CherishRequest model = new CherishRequest();
            string is_error_from_ongoing = string.Empty;
            try
            {
                model = populateModel.CherishNewConnwctionRequestPopulateModel(request);
                
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();


                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.CastRequestModelV2(model);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(requestModelBLOB);

                TokenValidationService token = new TokenValidationService(secreteKey);

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
                    orderRes.data = new DataRes()
                    {
                        request_id = "0"
                    };
                    orderRes.isError = true;
                    is_error_from_ongoing = "ongoing";
                    orderRes.message = orderValidationResult.message;
                    model.err_msg = orderRes.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);

                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = orderRes.isError,
                        message = orderRes.message,
                        data = new Datas() { }
                    });
                }
                #endregion
                #region Insert_Order
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;
                model.is_esim = 1;
                model.selected_category = model.selected_category;

                orderRes = await _orderManager.SubmitOrderV9(model, loginProviderId);
                if (orderRes.isError)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = orderRes.message,
                        data = new Datas() { }
                    });
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
                            // orderRes.data.request_id = "0";
                            orderRes.isError = true;
                            orderRes.message = "Retailer's center code not found!";
                            model.err_msg = orderRes.message;
                            orderRes.data = new DataRes
                            {
                                request_id = "0"
                            };
                        }
                    }

                    string cherish_category_config = SettingsValues.GetCherishCategory();
                    string[] cofigValue;
                    if (cherish_category_config.Contains(","))
                    {
                        cofigValue = cherish_category_config.Split(',');
                    }
                    else
                    {
                        cofigValue = cherish_category_config.Split(' ');
                    }

                    if (cofigValue.Any(x => x.Equals(model.selected_category)))
                    {
                        RACommonResponseRevampV3 msisdnValidationResp = await _bio.ValidateUnpairedMSISDNV6(new UnpairedMSISDNCheckRequest()
                        {
                            mobile_number = model.msisdn,
                            sim_number = model.sim_number,
                            channel_id = channelInfo.Item1,
                            channel_name = model.channel_name,
                            center_code = centerCode,
                            inventory_id = channelInfo.Item2,
                            purpose_number = model.purpose_number,
                            retailer_id = model.retailer_id,
                            sim_category = model.sim_category

                        }, "ValidateUnpairedMSISDNV6");

                        if (msisdnValidationResp.isError == true)
                        {
                            model.status = (int)EnumRAOrderStatus.Failed;
                            orderRes.data = new DataRes()
                            {
                                request_id = "0"
                            };
                            orderRes.isError = true;
                            orderRes.message = msisdnValidationResp.message;
                            model.err_msg = orderRes.message;

                            return Ok(new RACommonResponseRevamp()
                            {
                                isError = true,
                                message = msisdnValidationResp.message,
                                data = new Datas()
                                {
                                    isEsim = 0,
                                    request_id = " "
                                }
                            });
                        }
                    }
                    else
                    {
                        RACommonResponseRevamp msisdnValidationResp = await _bio.ValidateUnpairedMSISDNV5(new UnpairedMSISDNCheckRequest()
                        {
                            mobile_number = model.msisdn,
                            sim_number = model.sim_number,
                            channel_id = channelInfo.Item1,
                            channel_name = model.channel_name,
                            center_code = centerCode,
                            inventory_id = channelInfo.Item2,
                            purpose_number = model.purpose_number,
                            retailer_id = model.retailer_id,
                            sim_category = model.sim_category

                        }, "ValidateUnpairedMSISDN_ESIMV2");

                        if (msisdnValidationResp.isError == true)
                        {
                            model.status = (int)EnumRAOrderStatus.Failed;
                            orderRes.data = new DataRes
                            {
                                request_id = "0"
                            };
                            orderRes.isError = true;
                            orderRes.message = msisdnValidationResp.message;
                            model.err_msg = orderRes.message;

                            return Ok(new RACommonResponseRevamp()
                            {
                                isError = orderRes.isError,
                                message = orderRes.message,
                                data = new Datas()
                                {
                                    request_id = orderRes.data.request_id,
                                    isEsim = 1

                                }
                            });
                        }
                    }
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
                    orderRes.data.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = imsiResp.message;
                    model.err_msg = orderRes.message;
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = orderRes.isError,
                        message = orderRes.message,
                        data = new Datas()
                        {
                            request_id = orderRes.data.request_id,
                            isEsim = 1
                        }
                    });
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
                    model.msisdnReservationId = verifyResp.Reservation_Id;
                }
                else
                {

                    if (verifyResp.Reservation_Id != null)
                    {
                        if (model.is_paired == 0 && Convert.ToInt32(model.purpose_number) == (int)EnumPurposeNumber.NewRegistration)
                        {
                            await _apiCall.UnreserveMSISDNV2(verifyResp.Reservation_Id, model.session_token, "", model.bi_token_number.ToString() ?? "", model.msisdn, model.retailer_id);
                        }
                    }

                    model.status = (int)EnumRAOrderStatus.Failed;
                    model.err_code = verifyResp.err_code;
                    model.err_msg = verifyResp.err_msg;
                    model.error_id = verifyResp.error_Id;
                }
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
                    if (String.IsNullOrEmpty(verifyResp.Reservation_Id))
                    {
                        if (model.is_paired == 0 && Convert.ToInt32(model.purpose_number) == (int)EnumPurposeNumber.NewRegistration)
                        {
                            await _apiCall.UnreserveMSISDNV2(verifyResp.Reservation_Id, model.session_token, "", model.bi_token_number.ToString() ?? "", model.msisdn, model.retailer_id);
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

                    orderRes.data = new DataRes()
                    {
                        request_id = verifyResp != null ? verifyResp.bss_req_id : ""
                    };
                    orderRes.isError = true;
                    orderRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                    model.err_msg = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                }
                catch (Exception)
                {
                    orderRes.isError = true;
                    orderRes.message = ex.Message;
                    model.err_msg = ex.Message;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                }
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
                        user_name = model.retailer_id,
                        dest_imsi = model.dest_imsi,
                        status = model.status,
                        bss_reqId = model.bss_reqId,
                        error_id = model.error_id,
                        err_msg = model.err_msg,
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
                log.method_name = "NewConnectionSubmitOrder_ESIMV3";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }

            return Ok(new RACommonResponseRevamp()
            {
                isError = orderRes.isError,
                message = orderRes.message,
                data = new Datas()
                {
                    request_id = orderRes.data.request_id,
                    isEsim = 1
                }
            });
        }

        public BiomerticDataModel bioverifyDataMapp(OrderRequest2 order)
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

        private IActionResult GenerateResponse(bool status, string message, int isEsim, string requestId)
        {
            return Ok(new RACommonResponseRevamp
            {
                isError = status,
                message = message,
                data = new Datas { isEsim = isEsim, request_id = requestId }
            });
        }
    }
}
