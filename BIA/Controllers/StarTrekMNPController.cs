using BIA.BLL.BLLServices;
using BIA.BLL.Utility;
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
using System.Net;
using BIA.Helper;
using Serilog;
using System.Text.RegularExpressions;
using BIA.Entity.PopulateModel;

namespace BIA.Controllers
{
    [Route("api/StarTrekMNP")]
    [ApiController]
    public class StarTrekMNPController : ControllerBase
    {
        private readonly BLLOrder _orderManager;
        private readonly BLLLog _bllLog;
        private readonly BaseController _bio;
        private readonly GeoFencingValidation _geo;
        private readonly BLLDBSSToRAParse _dbssToRaParse;
        private readonly ApiRequest _apiReq;

        public StarTrekMNPController(BLLOrder orderManager, BLLLog bllLog, BaseController bio, GeoFencingValidation geo, BLLDBSSToRAParse dbssToRaParse, ApiRequest apiReq)
        {
            _orderManager = orderManager;
            _bllLog = bllLog;
            _bio = bio;
            _geo = geo;
            _dbssToRaParse = dbssToRaParse;
            _apiReq = apiReq;
        }

        /// Send Order
        /// <summary>
        /// This API is used for MNP PortIn submit order.
        /// </summary> 
        /// <param name="model"></param>
        /// <returns>Order request token id</returns>
        //[ResponseType(typeof(SendOrderResponse))]
        [HttpPost]
        [Route("MNPPortInSubmitOrder")]
        public async Task<IActionResult> MNPPortInSubmitOrder([FromBody][Bind("alt_msisdn,bi_token_number,center_code,channel_name,cid,customer_name,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,distributor_code,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,is_paired,lac,latitude,longitude,msisdn,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,salesman_code,scanner_id,session_token,sim_category,sim_number,subscription_code,subscription_type_id,thana_id,thana_name,village")] StarTrekMNPSubmitRequestModel request)
        { 
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            BL_Json _blJson = new BL_Json();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            string is_error_from_ongoing = string.Empty;
            RAOrderRequestV2 model = new RAOrderRequestV2();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            try
            {
                model = populateModel.StarTrekMNPSubmitRequestPopulateModel(request);

                string secreteKey = SettingsValues.GetJWTSequrityKey();
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                TokenValidationService token = new TokenValidationService(secreteKey);

                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.CastRequestModel(model);

                log.req_blob = _blJson.GetGenericJsonData(requestModelBLOB);

                security = token.ValidateToken(model.session_token);

                if (security == null || !security.IsVallid)
                {
                    throw new Exception(security?.Message ?? "Invalid token");
                }

                loginProviderId = security.LoginProviderId;
                model.distributor_code = security.DistributorCode;

                if (geoFencEnable == 1 && model.isBPUser == 1)
                {
                    var geoResp = await _geo.GeoFencingBPUser(model);
                    if (geoResp?.isError == true)
                        return Ok(geoResp);
                }

                var orderValidationResult = await _orderManager.ValidateOrder(new VMValidateOrder
                {
                    msisdn = model.msisdn,
                    sim_number = model.sim_number,
                    purpose_number = Convert.ToInt32(model.purpose_number),
                    is_corporate = 0,
                    retailer_id = model.retailer_id,
                    dest_dob = DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat)
                });

                if (!orderValidationResult.result)
                {
                    is_error_from_ongoing = "ongoing";
                    return Ok(new SendOrderResponseRev
                    {
                        isError = true,
                        message = orderValidationResult.message,
                        data = new DataRes { request_id = "0" }
                    });
                }

                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;
                model.prov_id = loginProviderId;
                orderRes = await _orderManager.SubmitOrderV7(model);
                if (orderRes.isError)
                    return Ok(orderRes);

                model.bi_token_number = Convert.ToDouble(orderRes.data?.request_id ?? "0");

                //var channelInfo = await _orderManager.GetInventoryIdByChannelName(model.channel_name);
                //if (SettingsValues.GetRyzeAllowOrNot() == 1)
                //{
                //    var msisdnValidationResp = await MNPValidateMSISDN(new UnpairedMSISDNCheckRequest
                //    {
                //        mobile_number = model.msisdn,
                //        sim_number = model.sim_number,
                //        channel_id = channelInfo.Item1,
                //        channel_name = model.channel_name,
                //        center_code = model.center_code,
                //        inventory_id = channelInfo.Item2,
                //        purpose_number = model.purpose_number,
                //        retailer_id = model.retailer_id,
                //        sim_category = model.sim_category
                //    });

                //    if (!msisdnValidationResp.result)
                //    {
                //        model.status = (int)EnumRAOrderStatus.Failed;
                //        model.err_msg = msisdnValidationResp.message;
                //        orderRes.isError = true;
                //        orderRes.message = msisdnValidationResp.message;
                //        orderRes.data = new DataRes { request_id = "0" };
                //        return Ok(orderRes);
                //    }
                //}
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
                    model.err_msg = imsiResp.message;
                    orderRes.isError = true;
                    orderRes.message = imsiResp.message;
                    orderRes.data = new DataRes { request_id = "0" };
                    return Ok(orderRes);
                }

                model.dest_imsi = imsiResp.imsi;

                var pardedData = await _orderManager.SubmitOrderDataPurseV2(model);
                var dataModel = bioverifyDataMapp(pardedData);
                verifyResp = await _bio.BssServiceProcessV2(dataModel);

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
                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                orderRes.data = new DataRes { request_id = verifyResp?.bss_req_id ?? "0" };
                orderRes.isError = true;
                orderRes.message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
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
                if (orderRes != null)
                    if (orderRes.data != null)
                    {
                        log.is_success = orderRes.data.request_id.Length > 1 ? 1 : 0;
                        log.bi_token_number = orderRes.data.request_id;
                    }
                log.res_blob = _blJson.GetGenericJsonData(orderRes);
                log.method_name = "MNPPortInSubmitOrder";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;
                await _bllLog.RAToDBSSLog(log);
            }

            return Ok(orderRes);
        }

        /// Send Order
        /// <summary>
        /// This API is used for MNP PortIn submit order.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Order request token id</returns>
        //[ResponseType(typeof(SendOrderResponse))]
        [HttpPost]
        [Route("MNPPortInSubmitOrder_ESIM")]
        public async Task<IActionResult> MNPPortInSubmitOrder_ESIM([FromBody][Bind("alt_msisdn,bi_token_number,center_code,channel_name,cid,customer_name,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,distributor_code,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,is_paired,lac,latitude,longitude,msisdn,package_code,package_id,postal_code,purpose_number,retailer_id,right_id,road_number,salesman_code,scanner_id,session_token,sim_category,sim_number,subscription_code,subscription_type_id,thana_id,thana_name,village")] StarTrekMNPSubmitRequestModel request)
        {
            BL_Json _blJson = new BL_Json();
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            string is_error_from_ongoing = string.Empty;
            RAOrderRequestV2 model = new RAOrderRequestV2();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            try
            {

                model = populateModel.StarTrekMNPSubmitRequestPopulateModel(request);

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();
                TokenValidationService token = new TokenValidationService(secreteKey);

                try
                {
                    RequiestModelCasting modelCasting = new RequiestModelCasting();
                    RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                    requestModelBLOB = modelCasting.CastRequestModel(model);

                    log.req_blob = _blJson.GetGenericJsonData(requestModelBLOB);
                }
                catch
                {
                    log.req_blob = _blJson.GetGenericJsonData(model);
                }

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
                    orderRes.data = new DataRes { request_id = "0" };
                    orderRes.isError = true;
                    orderRes.message = orderValidationResult.message;
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
                model.is_esim = 1;
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
                #region unpaired MSISDN validation (MNP)

                //var channelInfo = await _orderManager.GetInventoryIdByChannelName(model.channel_name);

                //RACommonResponseRevamp msisdnValidationResp = await MNPValidateMSISDNESim(new UnpairedMSISDNCheckRequest()
                //{
                //    mobile_number = model.msisdn,
                //    sim_number = model.sim_number,
                //    channel_id = channelInfo.Item1,
                //    channel_name = model.channel_name,
                //    center_code = model.center_code,
                //    inventory_id = channelInfo.Item2,
                //    purpose_number = model.purpose_number,
                //    retailer_id = model.retailer_id,
                //    sim_category = model.sim_category
                //});

                //if (msisdnValidationResp.isError == true)
                //{
                //    orderRes = new SendOrderResponseRev();
                //    model.status = (int)EnumRAOrderStatus.Failed;
                //    orderRes.data = new DataRes { request_id = "0" };
                //    orderRes.isError = true;
                //    orderRes.message = msisdnValidationResp.message;
                //    return Ok(orderRes);
                //}
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
                    orderRes = new SendOrderResponseRev();
                    model.status = (int)EnumRAOrderStatus.Failed;
                    orderRes.data = new DataRes { request_id = "0" };
                    orderRes.isError = true;
                    orderRes.message = imsiResp.message;
                    model.err_msg = imsiResp.message;
                    return Ok(orderRes);
                }
                else
                {
                    model.dest_imsi = imsiResp.imsi;//[Note: here IMSI is being sent as SIM number as per business requirement]
                }
                #endregion

                #region bio verification

                var pardedData = await _orderManager.SubmitOrderDataPurseV2(model);
                BiomerticDataModel dataModel = bioverifyDataMapp(pardedData);
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

                orderRes.data = new DataRes
                {
                    request_id = verifyResp != null ? verifyResp.bss_req_id : "0"
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
                        dest_imsi = model.dest_imsi,
                        msidn = model.msisdn,
                        user_name = model.retailer_id,
                        status = model.status,
                        bss_reqId = model.bss_reqId,
                        error_id = model.error_id,
                        err_msg = model.err_msg,
                    });
                }

                log.res_time = DateTime.Now;
                log.is_success = orderRes.data.request_id.Length > 1 ? 1 : 0;
                log.res_blob = _blJson.GetGenericJsonData(orderRes);
                log.bi_token_number = orderRes.data.request_id;
                log.method_name = "MNPPortInSubmitOrder_ESIM";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;
                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(orderRes);
        }

        public async Task<RACommonResponse> MNPValidateMSISDN(UnpairedMSISDNCheckRequest msisdnCheckRequest)
        {
            var raToDBssParse = new BLLRAToDBSSParse();
            var blJson = new BL_Json();
            var raRespModel = new RACommonResponse();
            var log = new BIAToDBSSLog();
            string apiUrl = "";
            string txtResp = "";

            try
            {
                var dbssReqModel = raToDBssParse.ValidateMSISDNReqParsing(msisdnCheckRequest);

                if (!dbssReqModel.StartsWith(FixedValueCollection.MSISDNCountryCode))
                {
                    dbssReqModel = FixedValueCollection.MSISDNCountryCode + dbssReqModel;
                }

                apiUrl = string.Format(GetAPICollection.UnpairedMSISDNValidation, dbssReqModel);
                log.req_blob = blJson.GetGenericJsonData(apiUrl);

                JObject dbssResp = new JObject();

                try
                {
                    log.req_time = DateTime.Now;
                    dbssResp = (JObject)(await _apiReq.HttpGetRequestForMNPPortIn(apiUrl, "MNPValidateMSISDN"));
                    log.res_time = DateTime.Now;
                }
                catch (WebException webEx)
                {
                    log.res_time = DateTime.Now;
                    txtResp = webEx.InnerException?.Message ?? webEx.Message;
                    log.res_blob = blJson.GetGenericJsonData(dbssResp);

                    if (webEx.Status == WebExceptionStatus.ProtocolError && webEx.Response is HttpWebResponse httpResp && httpResp.StatusCode == HttpStatusCode.NotFound)
                    {
                        log.is_success = 1;
                        var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest
                        {
                            center_code = msisdnCheckRequest.center_code ?? "",
                            distributor_code = "",
                            channel_name = msisdnCheckRequest.channel_name,
                            session_token = msisdnCheckRequest.session_token,
                            sim_number = msisdnCheckRequest.sim_number,
                            retailer_id = msisdnCheckRequest.retailer_id,
                            product_code = "",
                            inventory_id = msisdnCheckRequest.inventory_id,
                            msisdn = msisdnCheckRequest.mobile_number
                        }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckRequest.sim_category, "");

                        raRespModel.result = simResp.result;
                        raRespModel.message = simResp.result ? MessageCollection.MSISDNandSIMBothValid : simResp.message;
                        return raRespModel;
                    }

                    throw; // Rethrow unhandled WebException
                }

                // If DBSS API returned success
                txtResp = dbssResp.ToString();
                log.res_blob = blJson.GetGenericJsonData(dbssResp);
                log.is_success = 1;

                var simResp2 = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest
                {
                    center_code = msisdnCheckRequest.center_code ?? "",
                    distributor_code = "",
                    channel_name = msisdnCheckRequest.channel_name,
                    session_token = msisdnCheckRequest.session_token,
                    sim_number = msisdnCheckRequest.sim_number,
                    retailer_id = msisdnCheckRequest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckRequest.inventory_id
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckRequest.sim_category, "");

                raRespModel.result = simResp2.result;
                raRespModel.message = simResp2.result ? MessageCollection.MSISDNandSIMBothValid : simResp2.message;

                return raRespModel;
            }
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                log.res_blob = blJson.GetGenericJsonData(ex.InnerException?.Message ?? ex.Message);

                try
                {
                    var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest
                    {
                        center_code = msisdnCheckRequest.center_code ?? "",
                        distributor_code = "",
                        channel_name = msisdnCheckRequest.channel_name,
                        session_token = msisdnCheckRequest.session_token,
                        sim_number = msisdnCheckRequest.sim_number,
                        retailer_id = msisdnCheckRequest.retailer_id,
                        product_code = "",
                        inventory_id = msisdnCheckRequest.inventory_id
                    }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckRequest.sim_category, "");

                    if (!simResp.result)
                    {
                        raRespModel.result = false;
                        raRespModel.message = simResp.message;
                        return raRespModel;
                    }

                    raRespModel.result = true;
                    raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                    return raRespModel;
                }
                catch
                {
                    // Optional fallback - do nothing if secondary SIM check also fails
                }

                try
                {
                    var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    log.is_success = 0;
                    log.error_code = error.error_code ?? "";
                    log.error_source = error.error_source ?? "";
                    log.message = error.error_custom_msg ?? "";

                    raRespModel.result = false;
                    raRespModel.message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                    return raRespModel;
                }
                catch
                {
                    raRespModel.result = false;
                    raRespModel.message = ex.Message;
                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckRequest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckRequest.purpose_number ?? "";
                log.user_id = msisdnCheckRequest.retailer_id;
                log.method_name = "MNPValidateMSISDN";

                await _bllLog.RAToDBSSLog(log);
            }
        }


        //public async Task<RACommonResponse> MNPValidateMSISDN(UnpairedMSISDNCheckRequest msisdnCheckReqest)
        //{
        //    BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
        //    BL_Json _blJson = new BL_Json();
        //    RACommonResponse raRespModel = new RACommonResponse();
        //    string apiUrl = "", txtResp = "";
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    try
        //    {
        //        var dbssReqModel = _raToDBssParse.ValidateMSISDNReqParsing(msisdnCheckReqest);

        //        if (dbssReqModel.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
        //        {
        //            dbssReqModel = FixedValueCollection.MSISDNCountryCode + dbssReqModel;
        //        }

        //        apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, dbssReqModel);

        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl);

        //        JObject dbssResp = new JObject();

        //        try
        //        {
        //            log.req_time = DateTime.Now;
        //            dbssResp = (JObject)await _apiReq.HttpGetRequestForMNPPortIn(apiUrl, "MNPValidateMSISDN");
        //            log.res_time = DateTime.Now;
        //        }
        //        catch (WebException ex)
        //        {
        //            log.res_time = DateTime.Now;
        //            txtResp = Convert.ToString(ex.InnerException.Message);
        //            log.res_blob = _blJson.GetGenericJsonData(dbssResp);


        //            if (ex.Status == WebExceptionStatus.ProtocolError)
        //            {
        //                var ErrorResponse = ex.Response as HttpWebResponse;
        //                if (ErrorResponse != null && (int)ErrorResponse.StatusCode == 404)
        //                {
        //                    //var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);
        //                    log.is_success = 1;
        //                    var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest()
        //                    {
        //                        center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //                        distributor_code = "",
        //                        channel_name = msisdnCheckReqest.channel_name,
        //                        session_token = msisdnCheckReqest.session_token,
        //                        sim_number = msisdnCheckReqest.sim_number,
        //                        retailer_id = msisdnCheckReqest.retailer_id,
        //                        product_code = "",
        //                        inventory_id = msisdnCheckReqest.inventory_id,
        //                        msisdn = msisdnCheckReqest.mobile_number
        //                    }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //                    if (simResp.result == false)
        //                    {
        //                        raRespModel.result = false;
        //                        raRespModel.message = simResp.message;
        //                        return raRespModel;
        //                    }

        //                    raRespModel.result = true;
        //                    raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

        //                    return raRespModel;
        //                }
        //                else
        //                {
        //                    throw ex;
        //                }
        //            }
        //            else
        //            {
        //                throw ex;
        //            }
        //        }
        //        //======If DBSS api returnd success==========
        //        txtResp = Convert.ToString(dbssResp);
        //        log.res_blob = _blJson.GetGenericJsonData(dbssResp);

        //        log.is_success = 1;

        //        var simResp2 = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest()
        //        {
        //            center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //            distributor_code = "",
        //            channel_name = msisdnCheckReqest.channel_name,
        //            session_token = msisdnCheckReqest.session_token,
        //            sim_number = msisdnCheckReqest.sim_number,
        //            retailer_id = msisdnCheckReqest.retailer_id,
        //            product_code = "",
        //            inventory_id = msisdnCheckReqest.inventory_id
        //        }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //        if (simResp2.result == false)
        //        {
        //            raRespModel.result = false;
        //            raRespModel.message = simResp2.message;
        //            return raRespModel;
        //        }

        //        raRespModel.result = true;
        //        raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

        //        return raRespModel;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.res_time = DateTime.Now;
        //        log.res_blob = _blJson.GetGenericJsonData(ex.InnerException.Message);
        //        try
        //        {
        //            var simResp2 = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest()
        //            {
        //                center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //                distributor_code = "",
        //                channel_name = msisdnCheckReqest.channel_name,
        //                session_token = msisdnCheckReqest.session_token,
        //                sim_number = msisdnCheckReqest.sim_number,
        //                retailer_id = msisdnCheckReqest.retailer_id,
        //                product_code = "",
        //                inventory_id = msisdnCheckReqest.inventory_id
        //            }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //            if (simResp2.result == false)
        //            {
        //                raRespModel.result = false;
        //                raRespModel.message = simResp2.message;
        //                return raRespModel;
        //            }

        //            raRespModel.result = true;
        //            raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

        //            return raRespModel;
        //        }
        //        catch { }
        //        try
        //        {
        //            ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //            log.is_success = 0;
        //            log.error_code = error.error_code ?? String.Empty;
        //            log.error_source = error.error_source ?? String.Empty;
        //            log.message = error.error_description ?? String.Empty;

        //            raRespModel.result = false;
        //            raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //            return raRespModel;
        //        }
        //        catch (Exception)
        //        {
        //            raRespModel.result = false;
        //            raRespModel.message = ex.Message;
        //            return raRespModel;
        //        }
        //    }
        //    finally
        //    {
        //        log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

        //        log.purpose_number = msisdnCheckReqest.purpose_number;
        //        log.user_id = msisdnCheckReqest.retailer_id;//userName
        //        log.method_name = "MNPValidateMSISDN";

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);

        //    }
        //}

        public BiomerticDataModel bioverifyDataMapp(OrderRequest2 order)
        {
            BiomerticDataModel resp = new BiomerticDataModel();
            if (order != null)
            {
                resp.status = order.status;
                resp.create_date = DateTime.Now.ToString();
                if (order.purpose_number != null)
                    resp.purpose_number = (int)order.purpose_number;
                resp.dest_doc_type_no = order.dest_doc_type_no.ToString() ?? "";
                resp.dest_doc_id = order.dest_nid;
                resp.user_id = order.retailer_id;
                resp.msisdn = order.msisdn;
                if (order.dest_ec_verifi_reqrd != null)
                    resp.dest_ec_verification_required = (int)order.dest_ec_verifi_reqrd;
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
                resp.dest_left_thumb = order.dest_left_thumb;
                resp.dest_left_index = order.dest_left_index;
                resp.dest_right_thumb = order.dest_right_thumb;
                resp.dest_right_index = order.dest_right_index;
                if (order.src_doc_type_no != null)
                    resp.src_doc_type_no = order.src_doc_type_no.ToString() ?? "";
                if (order.src_ec_verifi_reqrd != null)
                    resp.src_ec_verification_required = (int)order.src_ec_verifi_reqrd;
                if (!String.IsNullOrEmpty(order.src_nid))
                    resp.src_doc_id = order.src_nid;
            }            
            return resp;
        }
        public async Task<RACommonResponseRevamp> MNPValidateMSISDNESim(UnpairedMSISDNCheckRequest msisdnCheckReqest)
        {
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                var dbssReqModel = _raToDBssParse.ValidateMSISDNReqParsing(msisdnCheckReqest);

                if (dbssReqModel.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    dbssReqModel = FixedValueCollection.MSISDNCountryCode + dbssReqModel;
                }

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, dbssReqModel);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                JObject dbssResp = new JObject();

                try
                {
                    log.req_time = DateTime.Now;
                    dbssResp = (JObject)await _apiReq.HttpGetRequestForMNPPortIn(apiUrl, "MNPValidateMSISDNESim");
                    log.res_time = DateTime.Now;
                }
                catch (WebException ex)
                {
                    log.res_time = DateTime.Now;
                    txtResp = ex.InnerException?.Message ?? ex.Message;
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response is HttpWebResponse httpResp && httpResp.StatusCode == HttpStatusCode.NotFound)
                    {
                        var ErrorResponse = ex.Response as HttpWebResponse;

                        log.is_success = 1;
                        var simResp = await _bio.CheckSIMNumber4(new SIMNumberCheckRequest()
                        {
                            center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                            distributor_code = "",
                            channel_name = msisdnCheckReqest.channel_name,
                            session_token = msisdnCheckReqest.session_token,
                            sim_number = msisdnCheckReqest.sim_number,
                            retailer_id = msisdnCheckReqest.retailer_id,
                            product_code = "",
                            inventory_id = msisdnCheckReqest.inventory_id,
                            msisdn = msisdnCheckReqest.mobile_number
                        }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

                        if (simResp.result == false)
                        {
                            raRespModel.isError = true;
                            raRespModel.message = simResp.message;
                            return raRespModel;
                        }

                        raRespModel.isError = false;
                        raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

                        return raRespModel;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex2)
                {
                    log.res_time = DateTime.Now;
                    txtResp = ex2.InnerException?.Message ?? ex2.Message;
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    log.is_success = 1;
                    var simResp = await _bio.CheckSIMNumber4(new SIMNumberCheckRequest()
                    {
                        center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                        distributor_code = "",
                        channel_name = msisdnCheckReqest.channel_name,
                        session_token = msisdnCheckReqest.session_token,
                        sim_number = msisdnCheckReqest.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        product_code = "",
                        inventory_id = msisdnCheckReqest.inventory_id,
                        msisdn = msisdnCheckReqest.mobile_number
                    }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

                    if (simResp.result == false)
                    {
                        raRespModel.isError = true;
                        raRespModel.message = simResp.message;
                        return raRespModel;
                    }

                    raRespModel.isError = false;
                    raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

                    return raRespModel;

                }
                //======If DBSS api returnd success==========
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                var msisdnResp2 = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);

                if (msisdnResp2.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.MSISDNAlreadyExists;
                    return raRespModel;
                }

                var simResp2 = await _bio.CheckSIMNumber4(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

                if (simResp2.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = simResp2.message;
                    return raRespModel;
                }

                raRespModel.isError = false;
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

                return raRespModel;
            }
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? String.Empty;

                raRespModel.isError = true;
                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                return raRespModel;

            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = msisdnCheckReqest.purpose_number??"";
                log.user_id = msisdnCheckReqest.retailer_id;//userName
                log.method_name = "MNPValidateMSISDNESim";

                await _bllLog.RAToDBSSLog(log);

            }
        }
    }
}