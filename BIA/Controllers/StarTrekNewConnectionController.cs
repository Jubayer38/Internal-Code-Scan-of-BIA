///************************************************************************
///	|| Creation History ||
///-----------------------------------------------------------------------
///	Copyright     :	Copyright© NAAS Solutions Limited. All rights reserved.
///	Author	      :	Mohiuddin
///	Purpose	      :	Activation Confirmation Controller for Physical and ESIM for both RYZE In Shop and Online Channel
///	Creation Date :	11-Jun-2024
/// =======================================================================
///  || Modification History ||
///  ----------------------------------------------------------------------
///  Sl No.	Date:		    Author:			    Ver:	    Area of Change:
///  1.     
///	 ----------------------------------------------------------------------
///	***********************************************************************

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
using BIA.Helper;
using Serilog;
using BIA.Entity.PopulateModel;
using System.Text.RegularExpressions;

namespace BIA.Controllers
{
    [Route("api/StarTrekNewConnection")]
    [ApiController]
    public class StarTrekNewConnectionController : ControllerBase
    {
        private readonly BLLOrder _orderManager;
        private readonly BLLLog _bllLog;
        private readonly BiometricApiCall _apiCall;
        private readonly GeoFencingValidation _geo;
        private readonly BaseController _bio;
        private readonly eShopAPICall _eShopAPI;
        private readonly ApiRequest _apiReq;

        public StarTrekNewConnectionController(BLLOrder orderManager, BLLLog bllLog, BiometricApiCall apiCall, GeoFencingValidation geo, BaseController bio, eShopAPICall eShopAPI, ApiRequest apiReq)
        {
            _orderManager = orderManager;
            _bllLog = bllLog;
            _apiCall = apiCall;
            _geo = geo;
            _bio = bio;
            _eShopAPI = eShopAPI;
            _apiReq = apiReq;
        }

        /// <summary>
        /// This api accept the Ryze In Shop Physical SIM activation Confirmation/resubmit request from Biometric App. The request process steps is below
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
        [Route("newConnection-submit")]
        [StarTrekNewConnectionRequestValidator]
        public async Task<IActionResult> NewConnectionSubmitOrder([FromBody][Bind("alt_msisdn,bi_token_number,channel_name,cid,customer_name,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,is_paired,lac,latitude,longitude,msisdn,order_id,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,scanner_id,selected_category,session_token,sim_category,sim_number,subscription_code,subscription_type_id,thana_id,thana_name,village")] StarTrekNewConnectionRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            BL_Json _blJson = new BL_Json();
            string is_error_from_ongoing = string.Empty;
            MessageBuilder messageBuilder = new MessageBuilder();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();
            try
            {
                model = populateModel.StarTrekNewConnwctionRequestPopulateModel(request);

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();


                #region Validate token
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
                model.is_starTrek = 1;
                model.prov_id = loginProviderId;

                orderRes = await _orderManager.SubmitOrderV8(model);
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

                    if (SettingsValues.GetRyzeAllowOrNot() == 1)
                    {
                        RACommonResponseRevamp msisdnValidationResp = await _bio.ValidateUnpairedMSISDNSTartTrek(new UnpairedMSISDNCheckRequest()
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

                        }, "ValidateUnpairedMSISDNSTartTrek");

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
                        RACommonResponseRevamp msisdnValidationResp = await _bio.ValidateUnpairedMSISDNSTartTrekV2(new UnpairedMSISDNCheckRequest()
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

                        }, "ValidateUnpairedMSISDNSTartTrekV2");

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

                verifyResp = await _bio.BssServiceProcessStarTrek(dataModel, model.msisdnReservationId ?? "", model.retailer_id, 0);

                if (verifyResp.is_success == true)
                {
                    orderRes.isError = false;
                    model.bss_reqId = verifyResp.bss_req_id;
                    model.status = (int)EnumRAOrderStatus.BioVerificationSubmitted;
                    model.msisdnReservationId = verifyResp.Reservation_Id;
                }
                else
                {
                    orderRes.isError = true;
                    if (!String.IsNullOrEmpty(verifyResp.Reservation_Id))
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
                    orderRes.message = verifyResp.err_msg;
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
                    log.res_blob = await _blJson.GetGenericJsonDataAsync(orderRes);
                }
                catch (Exception)
                {
                    string errMessage = messageBuilder.GetInnerMessage(ex);
                    orderRes.isError = true;
                    orderRes.message = errMessage;
                    model.err_msg = errMessage;
                    log.res_blob = await _blJson.GetGenericJsonDataAsync(orderRes);

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
                if ((model.bi_token_number != null && model.bi_token_number > 1) && is_error_from_ongoing != "ongoing")
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
                        user_name = model.retailer_id,
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
                log.method_name = "NewConnectionSubmitStarTrek";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("newConnection-submitV2")]
        [StarTrekCherishNewConnectionRequestValidator]
        public async Task<IActionResult> NewConnectionSubmitOrderV2([FromBody][Bind("Selected_category,alt_msisdn,bi_token_number,bts_code,channel_name,cid,customer_name,dest_dob,dest_imsi,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,is_lus,is_paired,lac,latitude,longitude,msisdn,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,scanner_id,session_token,sim_category,sim_number,subscription_code,subscription_type_id,thana_id,thana_name,village")] CherishNewConnectionRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            string is_error_from_ongoing = string.Empty;
            CherishRequest model = new CherishRequest();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RequiestModelCasting modelCasting = new RequiestModelCasting();
            RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();
            try
            {
                model = populateModel.CherishNewConnwctionRequestPopulateModel(request);
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                requestModelBLOB = modelCasting.CastRequestModelV2(model);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(requestModelBLOB);

                #region Validate token
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
                model.is_starTrek = 1;
                orderRes = await _orderManager.SubmitOrderV8(model);
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

                    if (SettingsValues.GetRyzeAllowOrNot() == 1)
                    {
                        RACommonResponseRevamp msisdnValidationResp = await _bio.ValidateUnpairedMSISDNSTartTrek(new UnpairedMSISDNCheckRequest()
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

                        }, "ValidateUnpairedMSISDNSTartTrek");

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
                        RACommonResponseRevamp msisdnValidationResp = await _bio.ValidateUnpairedMSISDNSTartTrekV2(new UnpairedMSISDNCheckRequest()
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

                        }, "ValidateUnpairedMSISDNSTartTrekV2");

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

                verifyResp = await _bio.BssServiceProcessStarTrek(dataModel, model.msisdnReservationId ?? "", model.retailer_id, 0);

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
                if ((model.bi_token_number != null && model.bi_token_number > 1) && is_error_from_ongoing != "ongoing")
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
                log.method_name = "NewConnectionSubmitStarTrek";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null
                               && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }
        }


        /// <summary>
        /// This api accept the Ryze Online Physical SIM activation Confirmation/resubmit request from Biometric App. The request process steps is below
        /// 1. JWT Session Token validation.
        /// 2. Check if submitted order is already in process or not.
        /// 3. Insert the request in main table (App end) for the first time
        /// 4. Order Validation from eShop if the order is cancelled in the mean time, and then MSISDN and SIM serial validation again (if another user is already taken this number during the process)
        /// 5. Get IMSI number by ICC/SIM number from DBSS via API
        /// 6. Send Bio-verification request to DBSS for the EC validation and CBVMP update through Adapter
        /// 7. If Bio-verification request failed but the MSISDN reserved in DBSS then unreserve that MSISDN
        /// 8. Catch block for any exception occured in the above checking
        /// 9. Update the request in database with the above processed value (status, error, bss_request_id, etc)
        /// 10. Finaly insert a log in App end Log table with BLOB Data
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("newConnection-submit-online")]
        [StarTrekNewConnectionRequestValidator]
        public async Task<IActionResult> NewConnectionSubmitOrder_Online([FromBody][Bind("alt_msisdn,bi_token_number,channel_name,cid,customer_name,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,is_paired,lac,latitude,longitude,msisdn,order_id,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,scanner_id,selected_category,session_token,sim_category,sim_number,subscription_code,subscription_type_id,thana_id,thana_name,village")] StarTrekNewConnectionRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            BL_Json _blJson = new BL_Json();
            string is_error_from_ongoing = string.Empty;
            MessageBuilder messageBuilder = new MessageBuilder();
            RequiestModelCasting modelCasting = new RequiestModelCasting();
            RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();

            try
            {
                model = populateModel.StarTrekNewConnwctionRequestPopulateModel(request);

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                #region Validate token
                TokenValidationService token = new TokenValidationService(secreteKey);

                requestModelBLOB = modelCasting.CastRequestModel(model);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(requestModelBLOB);

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
                    model.err_msg = orderRes.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = await _blJson.GetGenericJsonDataAsync(orderRes);

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
                model.is_starTrek = 1;
                model.is_online_sale = 1;
                model.prov_id = loginProviderId;

                orderRes = await _orderManager.SubmitOrderV8(model);
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

                    eShopOrderResponseModel responseModel = new eShopOrderResponseModel();

                    eShopOrderValidationReqModel eShopOrder = new eShopOrderValidationReqModel()
                    {
                        orderId = model.order_id ?? "",
                        msisdn = model.msisdn,
                        retailer_id = model.retailer_id
                    };

                    responseModel = await _eShopAPI.OrderValidation(eShopOrder);

                    if (responseModel != null)
                    {
                        if (responseModel.data != null)
                        {
                            if (responseModel.data.is_reserved == false)
                            {
                                throw new Exception(responseModel.message);
                            }
                            else if (String.IsNullOrEmpty(responseModel.data.reservation_id))
                            {
                                throw new Exception("The reservation_id field is empty!");
                            }
                            else if (!String.IsNullOrEmpty(responseModel.data.reservation_id) && responseModel.data.status_code == 200 && responseModel.data.is_reserved == true)
                            {
                                RACommonResponseRevampResp2 msisdnValidationResp = await _bio.ValidateUnpairedMSISDNSTartTrekTestOnline(new UnpairedMSISDNCheckRequest()
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

                                }, "ValidateUnpairedMSISDNSTartTrekOnline");

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

                                model.msisdnReservationId = msisdnValidationResp.reservationId;

                            }
                            else
                            {
                                throw new Exception("Invalid eShop API response!");
                            }
                        }
                        else
                        {
                            throw new Exception("Invalid eShop API response!");
                        }
                    }
                    else
                    {
                        throw new Exception("Invalid eShop API response!");
                    }
                }
                #endregion

                #region Get IMSI
                var imsiResp = await _bio.GetImsiBySimAsync(new GetImsiReq
                {
                    purpose_number = model.purpose_number,
                    retailer_id = model.retailer_id,
                    sim = Uri.EscapeDataString(model.sim_number),
                    msisdn = Uri.EscapeDataString(model.msisdn)
                });

                if (imsiResp.result == false)
                {
                    model.status = (int)EnumRAOrderStatus.Failed;
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

                verifyResp = await _bio.BssServiceProcessStarTrek(dataModel, model.msisdnReservationId ?? "", model.retailer_id, 1);

                if (verifyResp.is_success == true)
                {
                    orderRes.isError = false;
                    model.bss_reqId = verifyResp.bss_req_id;
                    model.status = (int)EnumRAOrderStatus.BioVerificationSubmitted;
                    model.msisdnReservationId = verifyResp.Reservation_Id;
                }
                else
                {
                    orderRes.isError = true;
                    model.status = (int)EnumRAOrderStatus.Failed;
                    model.err_code = verifyResp.err_code;
                    model.err_msg = verifyResp.err_msg;
                    model.error_id = verifyResp.error_Id;
                    orderRes.message = verifyResp.err_msg;
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
                    log.res_blob = await _blJson.GetGenericJsonDataAsync(orderRes);
                }
                catch (Exception)
                {
                    string errMessage = messageBuilder.GetInnerMessage(ex);
                    orderRes.isError = true;
                    orderRes.message = errMessage;
                    model.err_msg = errMessage;
                    log.res_blob = await _blJson.GetGenericJsonDataAsync(orderRes);
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
                if ((model.bi_token_number != null && model.bi_token_number > 1) && is_error_from_ongoing != "ongoing")
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
                log.method_name = "NewConnectionSubmitStarTrek_Online";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }
        }

        /// <summary>
        /// This api accept the Ryze In Shop eSIM activation Confirmation/resubmit request from Biometric App. The request process steps is below
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
        [Route("newConnection-submit-esim")]
        [StarTrekNewConnectionRequestValidator]
        public async Task<IActionResult> NewConnectionSubmitOrder_ESIM([FromBody][Bind("alt_msisdn,bi_token_number,channel_name,cid,customer_name,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,is_paired,lac,latitude,longitude,msisdn,order_id,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,scanner_id,selected_category,session_token,sim_category,sim_number,subscription_code,subscription_type_id,thana_id,thana_name,village")] StarTrekNewConnectionRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            BL_Json _blJson = new BL_Json();
            string is_error_from_ongoing = string.Empty;
            MessageBuilder messageBuilder = new MessageBuilder();
            RequiestModelCasting modelCasting = new RequiestModelCasting();
            RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();

            try
            {
                model = populateModel.StarTrekNewConnwctionRequestPopulateModel(request);

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                TokenValidationService token = new TokenValidationService(secreteKey);

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
                model.is_starTrek = 1;
                model.prov_id = loginProviderId;

                orderRes = await _orderManager.SubmitOrderV8(model);
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
                            orderRes.isError = true;
                            orderRes.message = "Retailer's center code not found!";
                            model.err_msg = orderRes.message;
                            orderRes.data = new DataRes
                            {
                                request_id = "0"
                            };
                        }
                    }

                    if (SettingsValues.GetRyzeAllowOrNot() == 1)
                    {
                        RACommonResponseRevamp msisdnValidationResp = await _bio.ValidateUnpairedMSISDNESIM(new UnpairedMSISDNCheckRequest()
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
                    else
                    {
                        RACommonResponseRevamp msisdnValidationResp = await _bio.ValidateUnpairedMSISDNESIMV2(new UnpairedMSISDNCheckRequest()
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

                verifyResp = await _bio.BssServiceProcessStarTrek(dataModel, model.msisdnReservationId ?? "", model.retailer_id, 0);


                if (verifyResp.is_success == true)
                {
                    orderRes.isError = false;
                    model.bss_reqId = verifyResp.bss_req_id;
                    model.status = (int)EnumRAOrderStatus.BioVerificationSubmitted;
                    model.msisdnReservationId = verifyResp.Reservation_Id;
                }
                else
                {
                    orderRes.isError = true;
                    if (!String.IsNullOrEmpty(verifyResp.Reservation_Id))
                    {
                        if (model.is_paired == 0 && Convert.ToInt32(model.purpose_number) == (int)EnumPurposeNumber.NewRegistration)
                        {
                            await _apiCall.UnreserveMSISDNV2(verifyResp.Reservation_Id, model.session_token, "", model.bi_token_number.ToString() ?? "", model.msisdn, model.retailer_id);
                        }
                    }

                    model.status = (int)EnumRAOrderStatus.Failed;
                    model.err_code = verifyResp.err_code;
                    model.err_msg = verifyResp.err_msg;
                    orderRes.message = verifyResp.err_msg;
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
                    log.res_blob = await _blJson.GetGenericJsonDataAsync(orderRes);
                }
                catch (Exception)
                {
                    string errMessage = messageBuilder.GetInnerMessage(ex);
                    orderRes.isError = true;
                    orderRes.message = errMessage;
                    model.err_msg = errMessage;
                    log.res_blob = await _blJson.GetGenericJsonDataAsync(orderRes);
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
                log.method_name = "NewConnectionSubmit_ESIM_StarTrek";
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

        /// <summary>
        /// This api accept the Ryze Online eSIM activation Confirmation/resubmit request from Biometric App. The request process steps is below
        /// 1. JWT Session Token validation.
        /// 2. Check if submitted order is already in process or not.
        /// 3. Insert the request in main table (App end) for the first time
        /// 4. Order Validation from eShop if the order is cancelled in the mean time, and then MSISDN and SIM serial validation again (if another user is already taken this number during the process)
        /// 5. Get IMSI number by ICC/SIM number from DBSS via API
        /// 6. Send Bio-verification request to DBSS for the EC validation and CBVMP update through Adapter
        /// 7. If Bio-verification request failed but the MSISDN reserved in DBSS then unreserve that MSISDN
        /// 8. Catch block for any exception occured in the above checking
        /// 9. Update the request in database with the above processed value (status, error, bss_request_id, etc)
        /// 10. Finaly insert a log in App end Log table with BLOB Data
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("newConnection-submit-esim-online")]
        [StarTrekNewConnectionRequestValidator]
        public async Task<IActionResult> NewConnectionSubmitOrder_ESIM_Online([FromBody][Bind("alt_msisdn,bi_token_number,channel_name,cid,customer_name,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,is_paired,lac,latitude,longitude,msisdn,order_id,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,scanner_id,selected_category,session_token,sim_category,sim_number,subscription_code,subscription_type_id,thana_id,thana_name,village")] StarTrekNewConnectionRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            BL_Json _blJson = new BL_Json();
            MessageBuilder messageBuilder = new MessageBuilder();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();
            try
            {
                model = populateModel.StarTrekNewConnwctionRequestPopulateModel(request);

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
                    orderRes.data.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = orderValidationResult.message;
                    model.err_msg = orderRes.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = await _blJson.GetGenericJsonDataAsync(orderRes);

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
                model.is_starTrek = 1;
                model.is_online_sale = 1;
                model.prov_id = loginProviderId;

                orderRes = await _orderManager.SubmitOrderV8(model);
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
                            orderRes.isError = true;
                            orderRes.message = "Retailer's center code not found!";
                            model.err_msg = orderRes.message;
                            orderRes.data = new DataRes
                            {
                                request_id = "0"
                            };
                        }
                    }

                    eShopOrderResponseModel responseModel = new eShopOrderResponseModel();

                    eShopOrderValidationReqModel eShopOrder = new eShopOrderValidationReqModel()
                    {
                        orderId = model.order_id ?? "",
                        msisdn = model.msisdn,
                        retailer_id = model.retailer_id
                    };

                    responseModel = await _eShopAPI.OrderValidation(eShopOrder);

                    if (responseModel != null)
                    {
                        if (responseModel.data != null)
                        {
                            if (responseModel.data.is_reserved == false)
                            {
                                throw new Exception(responseModel.message);
                            }
                            else if (String.IsNullOrEmpty(responseModel.data.reservation_id))
                            {
                                throw new Exception("The reservation_id field is empty!");
                            }
                            else if (!String.IsNullOrEmpty(responseModel.data.reservation_id) && responseModel.data.status_code == 200 && responseModel.data.is_reserved == true)
                            {
                                RACommonResponseRevampResp2 msisdnValidationResp = await _bio.ValidateUnpairedMSISDNSTartTrekTestOnline(new UnpairedMSISDNCheckRequest()
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

                                }, "ValidateUnpairedMSISDNSTartTrekOnline");

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

                                model.msisdnReservationId = msisdnValidationResp.reservationId;
                            }
                            else
                            {
                                throw new Exception("Invalid eShop API response!");
                            }
                        }
                        else
                        {
                            throw new Exception("Invalid eShop API response!");
                        }
                    }
                    else
                    {
                        throw new Exception("Invalid eShop API response!");
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

                verifyResp = await _bio.BssServiceProcessStarTrek(dataModel, model.msisdnReservationId ?? "", model.retailer_id, 1);

                if (verifyResp.is_success == true)
                {
                    orderRes.isError = false;
                    model.bss_reqId = verifyResp.bss_req_id;
                    model.status = (int)EnumRAOrderStatus.BioVerificationSubmitted;
                    model.msisdnReservationId = verifyResp.Reservation_Id;
                }
                else
                {
                    orderRes.isError = true;
                    model.status = (int)EnumRAOrderStatus.Failed;
                    model.err_code = verifyResp.err_code;
                    model.err_msg = verifyResp.err_msg;
                    orderRes.message = verifyResp.err_msg;
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
                catch (Exception)
                {
                    string errMessage = messageBuilder.GetInnerMessage(ex);
                    orderRes.isError = true;
                    orderRes.message = errMessage;
                    model.err_msg = errMessage;
                    log.res_blob = await _blJson.GetGenericJsonDataAsync(orderRes);
                }
            }
            finally
            {
                log.purpose_number = model.purpose_number;
                log.msisdn = _bllLog.FormatMSISDN(model.msisdn);
                log.req_time = DateTime.Now;
                if (model.bi_token_number != null || model.bi_token_number > 1)
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
                log.method_name = "NewConnectionSubmit_ESIM_StarTrek_Online";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null
                               && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

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

    }
}
