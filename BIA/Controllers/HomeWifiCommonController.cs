using BIA.BLL.BLLServices;
using BIA.BLL.Utility;
using BIA.Common;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.Interfaces;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Helper;
using BIA.JWT;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Collections;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Reflection;

namespace BIA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeWifiCommonController : ControllerBase
    {
        private BLLDBSSToRAParse _dbssToRaParse;
        private ApiRequest _apiReq;
        private BLLCommon _bllCommon;
        private BL_Json _blJson;
        private readonly ApiManager _apiManager;
        private readonly BLLLog _bllLog;
        private readonly BaseController _bio;
        private readonly BLLRAToDBSSParse _raToDBssParse;
        private readonly BLLHomeWifiService _bllHomeWifiService;
        private readonly ApiCall _apiCall;
        public HomeWifiCommonController(BLLDBSSToRAParse dbssToRaParse, BLLRAToDBSSParse raToDBssParse, ApiRequest apiReq, BL_Json blJson, BLLCommon bllCommon, ApiManager apiManager, BLLLog bllLog, BaseController bio, BLLHomeWifiService bllHomeWifiService, ApiCall apiCall)
        {
            this._bllCommon = bllCommon;
            this._dbssToRaParse = dbssToRaParse;
            this._apiReq = apiReq;
            this._blJson = blJson;
            this._apiManager = apiManager;
            this._bllLog = bllLog;
            this._bio = bio;
            this._raToDBssParse = raToDBssParse;
            this._bllHomeWifiService = bllHomeWifiService;
            this._apiCall = apiCall;
        }
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ValidateActivation")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> ValidateUnpairedMSISDN([FromBody][Bind("channel_id,channel_name,initiator_channel,inventory_id,is_bp,lan,mobile_number,order_type,product_code,retailer_id,right_id,sim_category,sim_number,simkit_type,subscription_type")] ActivationCheckRequestModel msisdnCheckReqest)
        {
            try
            {
                SIMProductMapResponse productMapResponse = new SIMProductMapResponse();
                var response = new RACommonResponseRevampV3();

                #region ICC checking from DMS 
                ICCDetailsRequestModel model = new ICCDetailsRequestModel()
                {
                    center_code = "",
                    icc = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    mobile_number = msisdnCheckReqest.mobile_number
                };

                ICCDetailsResponse? iccData = await _apiManager.CheckICCfromDMS(model);

                if (iccData != null && iccData.result)
                {
                    var reqmodel = new SIMProductMappingReqModelV2()
                    {
                        channel_id = msisdnCheckReqest.channel_id,
                        channel_name = msisdnCheckReqest.channel_name,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        is_bp = msisdnCheckReqest.is_bp,
                        mobile_number = msisdnCheckReqest.mobile_number,
                        product_code = iccData.product_name,
                        right_id = msisdnCheckReqest.right_id,
                        ext_action_type = msisdnCheckReqest.order_type,
                        ext_channel_type = msisdnCheckReqest.initiator_channel,
                        ext_sim_type = msisdnCheckReqest.subscription_type,
                        ext_storage_type = msisdnCheckReqest.simkit_type
                    };

                    productMapResponse = await _bllCommon.CeckSIMProductMappingV2(reqmodel);

                    if (productMapResponse != null && productMapResponse.is_success && productMapResponse.message.ToLower() == "valid")
                    {
                        // ICC/SIM mapping validated successfully; `response` is fully
                        // rebuilt below from ValidateHomeWifiD2DWithMapping, so no
                        // further action is needed here.
                    }
                    else
                    {
                        if (productMapResponse != null && !String.IsNullOrEmpty(productMapResponse.message))
                        {
                            response.isError = true;
                            response.message = productMapResponse.message;
                            return Ok(response);
                        }
                        else
                        {
                            response.isError = true;
                            response.message = "Error while checking the SIM Mapping!";
                            return Ok(response);
                        }
                    }
                }
                else
                {
                    response.isError = true;
                    response.message = iccData?.message ?? "Unknown error";
                    return Ok(response);
                }
                #endregion
                response = await _bio.ValidateHomeWifiD2DWithMapping(msisdnCheckReqest, "ValidateUnpairedMSISDNV8")
                    ?? new RACommonResponseRevampV3 { isError = true, message = "Unknown error" };

                if (!response.isError)
                {
                    response.isError = false;
                    response.data.offer_name = iccData?.offer_name ?? string.Empty;
                    response.data.product_name = iccData?.product_name ?? string.Empty;
                    response.data.details_message = iccData?.offer_description ?? string.Empty;
                }
                else
                {
                    response.isError = true;
                }

                if (response.isError == false && response.data.isDesiredCategory == true)
                {
                    var product_category_config = SettingsValues.GetMMSTDProduct();

                    var configValues = product_category_config.Contains(',') ? product_category_config.Split(',') : new string[] { product_category_config };

                    if (configValues.Any(x => x == response.data.product_name))
                    {
                        response.isError = true;
                        response.message = "You are not authorised for this connection!";
                        return Ok(response);
                    }
                }

                return Ok(response);
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
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                return Ok(new RACommonResponseRevamp
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas
                    {
                        isEsim = 0,
                        request_id = "0"
                    }
                });
            }
        }

        /// <summary>
        /// This API is used for MSISDN validation for paired
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        //[Authorize]
        //[ResponseType(typeof(IndividualSIMReplacementMSISDNCheckResponse))]
        [HttpPost]
        [HomeWifiSIMReplacementModelValidator]
        [Route("ValidateSIMReplacement")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> ValidateMSISDNForIndividualSIMReplacementV3([FromBody][Bind("center_code,channel_id,channel_name,initiator_channel,inventory_id,lan,mobile_number,order_type,purpose_number,retailer_id,sim_category,sim_number,simkit_type,subscription_type")] SimreplacementValidationModel msisdnCheckReqest)
        {
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            IndividualSIMReplacementMSISDNCheckResponseRevamp response = new IndividualSIMReplacementMSISDNCheckResponseRevamp();

            try
            {
                SIMProductMapResponse productMapResponse = new SIMProductMapResponse();
                var responseTemp = new RACommonResponseRevampV3();

                #region ICC checking from DMS 
                ICCDetailsRequestModel model = new ICCDetailsRequestModel()
                {
                    center_code = "",
                    icc = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    mobile_number = msisdnCheckReqest.mobile_number
                };

                ICCDetailsResponse? iccData = await _apiManager.CheckICCfromDMS(model);

                if (iccData != null && iccData.result)
                {
                    var reqmodel = new SIMProductMappingReqModelV2()
                    {
                        channel_id = msisdnCheckReqest.channel_id,
                        channel_name = msisdnCheckReqest.channel_name,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        is_bp = 0,
                        mobile_number = msisdnCheckReqest.mobile_number,
                        product_code = iccData.product_name,
                        right_id = 0,
                        ext_action_type = msisdnCheckReqest.order_type,
                        ext_channel_type = msisdnCheckReqest.initiator_channel,
                        ext_sim_type = msisdnCheckReqest.subscription_type,
                        ext_storage_type = msisdnCheckReqest.simkit_type
                    };

                    productMapResponse = await _bllCommon.CeckSIMProductMappingV2(reqmodel);

                    if (productMapResponse != null && productMapResponse.is_success && productMapResponse.message.ToLower() == "valid")
                    {
                        responseTemp.isError = false;
                    }
                    else
                    {
                        if (productMapResponse != null && !String.IsNullOrEmpty(productMapResponse.message))
                        {
                            responseTemp.isError = true;
                            responseTemp.message = productMapResponse.message;
                            return Ok(responseTemp);
                        }
                        else
                        {
                            responseTemp.isError = true;
                            responseTemp.message = "Error while checking the SIM Mapping!";
                            return Ok(responseTemp);
                        }
                    }
                }
                else
                {
                    responseTemp.isError = true;
                    responseTemp.message = iccData?.message ?? "Unknown error";
                    return Ok(responseTemp);
                }
                #endregion

                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingOwnerCustomerUserCustomerSimCardInfo, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                JObject dbssResp = new JObject();
                try
                {
                    dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateSIMReplacement");
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("Not Found"))
                    {
                        return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                        {
                            isError = true,
                            message = FixedValueCollection.DBSSError + "Invalid MSISDN input for SIM Replacement."
                        });
                    }
                    else
                    {
                        throw;
                    }
                }

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null || dbssResp["included"] == null)
                {
                    return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = MessageCollection.SIMReplNoDataFound,
                    });
                }

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.IndividualSIMReplacementMSISDNReqParsingV3(dbssResp);

                if (msisdnResp.result == false)
                {
                    return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = FixedValueCollection.MSISDNError + msisdnResp.message
                    });
                }

                var simResp = await _bio.CheckSIMNumberForReplacement(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? "",
                    sim_type = msisdnCheckReqest.subscription_type,
                    storage_type = msisdnCheckReqest.simkit_type
                }, (int)EnumPurposeOfSIMCheck.SIMReplacement, null, null, msisdnResp.old_sim_type);

                if (simResp.result == false)
                {
                    return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = simResp.message
                    });
                }

                var resp = new IndividualSIMReplacementMSISDNCheckResponse()
                {
                    dbss_subscription_id = msisdnResp.dbss_subscription_id,
                    old_sim_number = msisdnResp.old_sim_number,
                    doc_id_number = "**********",
                    dob = "**/**/****",
                    result = true,
                    message = MessageCollection.MSISDNandSIMBothValid,
                    saf_status = msisdnResp.saf_status,
                    customer_id = msisdnResp.customer_id
                };
                return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                {
                    isError = false,
                    message = resp.message,
                    data = resp
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
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? String.Empty;

                return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                {
                    isError = true,
                    message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                });
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);

                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidateSIMReplacement";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [Route("ValidateMNP")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> MNPValidateMSISDN([FromBody][Bind("center_code,channel_id,channel_name,initiator_channel,inventory_id,is_bp,lan,mobile_number,order_type,product_code,purpose_number,retailer_id,sim_category,sim_number,simkit_type,subscription_type")] MNPValidationRequestModel msisdnCheckReqest)
        {
            SIMProductMapResponse productMapResponse = new SIMProductMapResponse();
            RACommonResponseRevamp response = new RACommonResponseRevamp();
            try
            {
                #region ICC checking from DMS 
                ICCDetailsRequestModel model = new ICCDetailsRequestModel()
                {
                    center_code = msisdnCheckReqest.center_code,
                    icc = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    mobile_number = msisdnCheckReqest.mobile_number
                };

                ICCDetailsResponse? iccData = await _apiManager.CheckICCfromDMS(model);

                if (iccData != null && iccData.result)
                {
                    var reqmodel = new SIMProductMappingReqModelV2()
                    {
                        channel_id = msisdnCheckReqest.channel_id,
                        channel_name = msisdnCheckReqest.channel_name,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        is_bp = msisdnCheckReqest.is_bp,
                        mobile_number = msisdnCheckReqest.mobile_number,
                        product_code = iccData.product_name,
                        right_id = 0,
                        ext_action_type = msisdnCheckReqest.order_type,
                        ext_channel_type = msisdnCheckReqest.initiator_channel,
                        ext_sim_type = msisdnCheckReqest.subscription_type,
                        ext_storage_type = msisdnCheckReqest.simkit_type
                    };

                    productMapResponse = await _bllCommon.CeckSIMProductMappingV2(reqmodel);

                    if (productMapResponse != null && productMapResponse.is_success && productMapResponse.message.ToLower() == "valid")
                    {
                        response.isError = false;
                    }
                    else
                    {
                        if (productMapResponse != null && !String.IsNullOrEmpty(productMapResponse.message))
                        {
                            response.isError = true;
                            response.message = productMapResponse.message;
                            return Ok(response);
                        }
                        else
                        {
                            response.isError = true;
                            response.message = "Error while checking the SIM Mapping!";
                            return Ok(response);
                        }
                    }
                }
                else
                {
                    response.isError = true;
                    response.message = iccData?.message ?? "Unknown error";
                    return Ok(response);
                }
                #endregion

                response = await MNPValidateMSISDNHomeWififorMNPPortIn(msisdnCheckReqest);

                if (response.isError == false)
                {
                    response.isError = false;
                    response.data.offer_name = iccData.offer_name ?? string.Empty;
                    response.data.product_name = iccData.product_name ?? string.Empty;
                    response.data.details_message = iccData.offer_description ?? string.Empty;
                }
                else
                {
                    response.isError = true;
                }

                return Ok(response);
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = ex.InnerException?.Message ?? ex.Message
                });
            }
        }
        public async Task<RACommonResponseRevamp> MNPValidateMSISDNHomeWififorMNPPortIn(MNPValidationRequestModel msisdnCheckReqest)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            JObject dbssResp = new JObject();
            try
            {
                var dbssReqModel = _raToDBssParse.ValidateMSISDNReqParsingHomeWifi(msisdnCheckReqest);

                if (dbssReqModel.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    dbssReqModel = FixedValueCollection.MSISDNCountryCode + dbssReqModel;
                }

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, dbssReqModel);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                try
                {
                    log.req_time = DateTime.Now;
                    dbssResp = (JObject)await _apiReq.HttpGetRequestForMNPPortIn(apiUrl, "MNPValidateMSISDNHomeWififorMNPPortIn");
                    log.res_time = DateTime.Now;
                }
                catch (WebException ex)
                {
                    log.res_time = DateTime.Now;
                    txtResp = Convert.ToString(ex.InnerException?.Message);
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        var ErrorResponse = ex.Response as HttpWebResponse;
                        if (ErrorResponse != null && (int)ErrorResponse.StatusCode == 404)
                        {
                            //var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);
                            log.is_success = 1;

                            var simResp = await _bio.CheckSIMNumberHomeWifiD2D(new SIMNumberCheckRequest()
                            {
                                //center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                                distributor_code = string.Empty,
                                channel_name = msisdnCheckReqest.channel_name,
                                //session_token = msisdnCheckReqest.session_token,
                                sim_number = msisdnCheckReqest.sim_number,
                                retailer_id = msisdnCheckReqest.retailer_id,
                                product_code = string.Empty,
                                inventory_id = msisdnCheckReqest.inventory_id,
                                msisdn = msisdnCheckReqest.mobile_number,
                                //purpose_number = msisdnCheckReqest.purpose_number ?? ""
                            }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "", msisdnCheckReqest.subscription_type, msisdnCheckReqest.simkit_type);

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
                    else
                    {
                        throw;
                    }
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

                var simResp2 = await _bio.CheckSIMNumberHomeWifiD2D(new SIMNumberCheckRequest()
                {
                    //center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    //session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    //purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "", msisdnCheckReqest.subscription_type, msisdnCheckReqest.simkit_type);

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
                Log.Error(ex, "ExMessage");
                JObject jsonObject = JObject.Parse(ex.InnerException?.Message ?? ex.Message);
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(ex.InnerException?.Message);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                string? statusValue = jsonObject?["errors"]?["status"]?.ToString();
                string? title = jsonObject?["errors"]?["title"]?.ToString();

                if (!String.IsNullOrEmpty(statusValue) && (statusValue == "7001" || title == "Msisdn Not Found"))
                {
                    var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);
                    log.is_success = 1;
                    var simResp = await _bio.CheckSIMNumberHomeWifiD2D(new SIMNumberCheckRequest()
                    {
                        //center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                        distributor_code = string.Empty,
                        channel_name = msisdnCheckReqest.channel_name,
                        //session_token = msisdnCheckReqest.session_token,
                        sim_number = msisdnCheckReqest.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        product_code = string.Empty,
                        inventory_id = msisdnCheckReqest.inventory_id,
                        msisdn = msisdnCheckReqest.mobile_number,
                        //purpose_number = msisdnCheckReqest.purpose_number ?? ""
                    }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "", msisdnCheckReqest.subscription_type, msisdnCheckReqest.simkit_type);

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
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(ex.InnerException?.Message);

                    try
                    {
                        ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                        log.is_success = 0;
                        log.error_code = error.error_code ?? String.Empty;
                        log.error_source = error.error_source ?? String.Empty;
                        log.message = error.error_custom_msg ?? error.error_description;

                        raRespModel.isError = true;
                        raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                        return raRespModel;
                    }
                    catch (Exception ex2)
                    {
                        raRespModel.isError = true;
                        raRespModel.message = ex.InnerException?.Message ?? ex2.Message;
                        return raRespModel;
                    }
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;//userName
                log.method_name = "MNPValidateMSISDNHomeWififorMNPPortIn";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [ValidateModel]
        [Route("ValidateTOS")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> ValidateMSISDNForTosHomeWifi([FromBody][Bind("center_code,channel_name,initiator_channel,lan,mobile_number,order_type,purpose_number,retailer_id,simkit_type,subscription_type")] ValidateMSISDNForTOSRequestModel msisdnCheckReqest)
        {
            string? apiUrl = string.Empty, txtResp = string.Empty;
            string loanCheckApiUrl = string.Empty;
            string debtCheckApi = string.Empty;
            string NinetyDaysLockCheckApi = string.Empty;
            string SubIdForDebtCheckApi = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            TOSLoanStatusResponse loanStatusResponse = new TOSLoanStatusResponse();
            TOSDebtStatusResponse debtStatusResponse = new TOSDebtStatusResponse();
            TOSDebtStatusResponse SubStatusResponse = new TOSDebtStatusResponse();
            ValidTokenResponse security = new ValidTokenResponse();
            DateTime? NinetyDayCheckTime = DateTime.Now;

            try
            {
                string srcMsisdn = msisdnCheckReqest.mobile_number;

                if (srcMsisdn.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    srcMsisdn = FixedValueCollection.MSISDNCountryCode + srcMsisdn;
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingSimCardsPayerCustomerOwnerCustomerUserCustomer, srcMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateMSISDNForTosNidToNidV3");

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null || dbssResp["included"] == null)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = MessageCollection.SIMReplNoDataFound,
                    });
                }
                TosNidToNidMSISDNCheckResponse msisdnValidationResp = new TosNidToNidMSISDNCheckResponse();
                try
                {
                    msisdnValidationResp = _dbssToRaParse.TosNidToNidMSISDNReqParsingV1(dbssResp);
                }
                catch { }
                msisdnValidationResp.dob = "**/**/****";
                msisdnValidationResp.doc_id_number = "**********";
                if (msisdnValidationResp.result == false)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = msisdnValidationResp.message
                    });
                }
                else
                {
                    NinetyDayCheckTime = await _bio.CheckActivationDateV2(msisdnCheckReqest, srcMsisdn);

                    if (NinetyDayCheckTime != null)
                    {
                        int configDate = SettingsValues.GetTOSValidationTime();

                        TimeSpan diff = DateTime.Now - NinetyDayCheckTime.Value;

                        if (diff.TotalDays < configDate)
                        {
                            return Ok(new RACommonResponseRevamp()
                            {
                                isError = true,
                                message = "TOS Not Eligible within " + configDate + " days of activation",
                            });
                        }
                    }
                    if (msisdnValidationResp.src_sim_category == (int)EnumSimCategory.Postpaid) //For Postpaid
                    {
                        try
                        {
                            string tosMsisdn = msisdnCheckReqest.mobile_number;

                            if (tosMsisdn.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                            {
                                tosMsisdn = FixedValueCollection.MSISDNCountryCode + tosMsisdn;
                            }
                            SubIdForDebtCheckApi = String.Format(GetAPICollection.SubIdDebtCheckApi, tosMsisdn);
                            JObject respForDebt = await GetCombinedUsageDetailsForTOS(SubIdForDebtCheckApi, msisdnCheckReqest);
                            ParsedBillingAccountInfo? billingInfo = _dbssToRaParse.ParseBillingAccountInfo(respForDebt);

                            if (billingInfo == null)
                            {
                                return Ok(new TosNidToNidMSISDNCheckResponseRevamp
                                {
                                    isError = true,
                                    message = "Combined Billing account info not found.",
                                    data = msisdnValidationResp
                                });
                            }
                            if (billingInfo.isError == true)
                            {
                                return Ok(new TosNidToNidMSISDNCheckResponseRevamp
                                {
                                    isError = true,
                                    message = billingInfo.message,
                                    data = msisdnValidationResp
                                });
                            }
                            else
                            {
                                TOSBillingReportResponse report = new TOSBillingReportResponse();

                                report = await _bio.FetchTOSBillingReportsV2(msisdnCheckReqest, billingInfo.SubscriptionId ?? "", billingInfo.BillingAccountId ?? "");

                                if (report == null)
                                {
                                    return Ok(new TosNidToNidMSISDNCheckResponseRevamp
                                    {
                                        isError = true,
                                        message = "Billing account info not found.",
                                        data = msisdnValidationResp
                                    });
                                }
                                if (report.Result == false)
                                {
                                    return Ok(new TosNidToNidMSISDNCheckResponseRevamp
                                    {
                                        isError = true,
                                        message = "Billing account info not found.",
                                        data = msisdnValidationResp
                                    });
                                }
                                else
                                {
                                    var tosFeeResponse = await _bllCommon.GetTOSFeeFromDB(msisdnCheckReqest.channel_name, msisdnValidationResp.src_sim_category);

                                    decimal tosFee = tosFeeResponse.FeeAmount;

                                    decimal totalDebt = report.Debt + report.Unbilled;

                                    if (report.Deposit < (totalDebt + tosFee))
                                    {
                                        return Ok(new TosNidToNidMSISDNCheckResponseRevamp
                                        {
                                            isError = true,
                                            message = "Insufficient Credit Limit, Please pay your due bill to do the TOS, Thank You!",
                                            data = msisdnValidationResp
                                        });
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                    else
                    {
                        try
                        {
                            loanCheckApiUrl = String.Format(GetAPICollection.GetLoanStatusForTOSV2, msisdnValidationResp.dbss_subscription_id);
                            JObject dbssRespForLoan = await GetLoanStatusForTOS(loanCheckApiUrl, msisdnCheckReqest);
                            loanStatusResponse = _dbssToRaParse.TosNiDtoNIDLoanStatusCheckParsing(dbssRespForLoan);

                            if (loanStatusResponse.result == false)
                            {
                                return Ok(new TosNidToNidMSISDNCheckResponseRevamp()
                                {
                                    isError = true,
                                    message = loanStatusResponse.message,
                                    data = msisdnValidationResp
                                });
                            }
                            #region ignore balance checking part
                            //else
                            //{
                            //    string prepaidApiUrl = string.Format(GetAPICollection.PrepaidBalanceCheck, srcMsisdn);
                            //    JObject prepaidResp = await GetPrepaidBalanceDetailsForTOS(prepaidApiUrl, msisdnCheckReqest);
                            //    ParsedPrepaidBalanceInfo? parsedPrepaidInfo = _dbssToRaParse.ParsePrepaidBalanceResponse(prepaidResp);

                            //    if (parsedPrepaidInfo != null && parsedPrepaidInfo.SupervisionExpiryDate.HasValue &&
                            //        DateTime.Now >= parsedPrepaidInfo.SupervisionExpiryDate.Value)
                            //    {
                            //        return Ok(new TosNidToNidMSISDNCheckResponseRevamp
                            //        {
                            //            isError = true,
                            //            message = "Supervision expiry has passed. TOS not eligible.",
                            //            data = msisdnValidationResp
                            //        });
                            //    }

                            //    var tosFeeResp = await _bllCommon.GetTOSFeeFromDB(msisdnCheckReqest.channel_name, msisdnValidationResp.src_sim_category);
                            //    decimal tosFee = tosFeeResp.FeeAmount;

                            //    // Check MA Balance
                            //    if (parsedPrepaidInfo != null && parsedPrepaidInfo.Amount < tosFee)
                            //    {
                            //        return Ok(new TosNidToNidMSISDNCheckResponseRevamp
                            //        {
                            //            isError = true,
                            //            message = "You don’t have sufficient balance for TOS, Thank you!",
                            //            data = msisdnValidationResp
                            //        });
                            //    }
                            //}
                            #endregion                            
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }

                    #region Recharge Check
                    string retailer_ev_number = await _bllCommon.GetEVTransactionNumber(msisdnCheckReqest.retailer_id);
                    string api_url = SettingsValues.GetTransactionDetails();
                    string tossErrorMessage = SettingsValues.GetTOSRechargeNotFoundMessage();
                    string userName = msisdnCheckReqest.retailer_id;
                    var requestModel = new PretupsRequestModel
                    {
                        MSISDN = "",
                        PIN = "",
                        LOGINID = "",
                        PASSWORD = "",
                        EXTCODE = retailer_ev_number,
                        RECEIVER_MSISDN = msisdnCheckReqest.mobile_number
                    };

                    var transactions = await _bio.GetValidC2STransactions(requestModel, api_url, userName);

                    if (!transactions.Any())
                    {
                        int isExist = await _bllCommon.GetMSISDNStatusForTOS(msisdnCheckReqest.mobile_number);

                        if (isExist == 0)
                        {
                            return Ok(new TosNidToNidMSISDNCheckResponseRevamp()
                            {
                                isError = true,
                                message = tossErrorMessage, // "Please recharge Tk. 350 from this retailer and complete TOS within 1 Hour",
                                data = msisdnValidationResp
                            });
                        }
                    }
                    #endregion
                }

                return Ok(new TosNidToNidMSISDNCheckResponseRevamp()
                {
                    isError = false,
                    message = msisdnValidationResp.message,
                    data = msisdnValidationResp
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
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? String.Empty;

                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                });
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number;
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidateMSISDNForTosNidToNidV3";
                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<JObject> GetLoanStatusForTOS(string apiUrl, ValidateMSISDNForTOSRequestModel msisdnCheckRequest)
        {
            string? txtResp = string.Empty;
            string loanCheckApiUrl = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            JObject dbssResp = new JObject();
            try
            {

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetLoanStatusForTOS");

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                return dbssResp;
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
                log.message = error.error_description ?? String.Empty;

                return dbssResp;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckRequest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckRequest.purpose_number;
                log.user_id = msisdnCheckRequest.retailer_id;
                log.method_name = "GetLoanStatusForTOS";

                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<JObject> GetCombinedUsageDetailsForTOS(string apiurl, ValidateMSISDNForTOSRequestModel msisdnCheckRequest)
        {
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            JObject dbssResp = new JObject();

            try
            {
                log.req_blob = _blJson.GetGenericJsonData(apiurl);
                log.req_time = DateTime.Now;

                dbssResp = await _apiReq.HttpGetRequest(apiurl, "GetCombinedUsageDetailsForTOS");

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                return dbssResp;
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
                log.message = error.error_description ?? String.Empty;

                return dbssResp;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckRequest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckRequest.purpose_number;
                log.user_id = msisdnCheckRequest.retailer_id;
                log.method_name = "GetCombinedUsageDetailsForTOS";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetSubscriptionOld")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> GetSubscriptionTypesOld([FromBody][Bind("channel_name,ext_subscription_type,initiator_channel,is_bp,lan,order_number,order_type,retailer_id,right_id,simkit_type,subscription_type")] RASubscriptionTypeReqWithMappingV2 model)
        {
            List<SubscriptionTypeReponseDataRev> raRespData = new List<SubscriptionTypeReponseDataRev>();
            SubscriptionTypeReponseRev raResp = new SubscriptionTypeReponseRev();
            string? apiUrl = string.Empty, txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            List<SubscriptionMappingResponse> subscriptionMapping = new List<SubscriptionMappingResponse>();
            HomeWifiLeadDetailsRequestModel reqModel = new HomeWifiLeadDetailsRequestModel();
            HomeWifiCommonResponseModel resp = new HomeWifiCommonResponseModel();

            try
            {
                reqModel = new HomeWifiLeadDetailsRequestModel()
                {
                    order_number = model.order_number,
                    retailer_code = model.retailer_id
                };

                resp = await _bllHomeWifiService.BLLHomeWifiLeadDetails(reqModel);

                if (resp != null)
                {
                    if (resp.data != null)
                    {
                        var data = (HomeWifiLeadDetailsResponseData)resp.data;

                        var lead = data.lead_details;

                        var dict = lead as IDictionary<string, object>;

                        var offerCode = dict?["offer_code"]?.ToString();
                        var subscriptionCode = dict?["subscription_code"]?.ToString();

                        if (!string.IsNullOrEmpty(subscriptionCode))
                        {
                            raResp.data = new List<SubscriptionTypeReponseDataRev>
                                {
                                    new SubscriptionTypeReponseDataRev
                                    {
                                        subscription_id = "11111",
                                        subscription_name = subscriptionCode
                                    }
                                };

                            if (raResp.data != null)
                            {
                                raResp.isError = false;
                                raResp.message = MessageCollection.Success;
                            }
                            else
                            {
                                raResp.isError = true;
                                raResp.message = "Dex: No subscription code mapped here!";
                            }
                        }
                        else
                        {
                            raResp.data = new List<SubscriptionTypeReponseDataRev>
                                {
                                    new SubscriptionTypeReponseDataRev
                                    {
                                        subscription_id = "",
                                        subscription_name = ""
                                    }
                                };

                            raResp.isError = true;
                            raResp.message = "Dex: No subscription code mapped here!";
                        }

                    }
                }
                else
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = "Unable to load data from Dex API!";
                }
                return Ok(raResp);
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
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? error.error_description;
                raResp.data = raRespData;
                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                return Ok(raResp);
            }
            finally
            {
                log.method_name = "GetSubscriptionTypesV4";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;
                string rspStr = string.Empty;
                if (txtResp != null)
                {
                    rspStr = txtResp;
                }
                await _bllLog.RAToDBSSLog(log);
            }
        }


        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetSubscription")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> GetSubscriptionTypes([FromBody][Bind("channel_name,ext_subscription_type,initiator_channel,is_bp,lan,order_number,order_type,retailer_id,right_id,simkit_type,subscription_type")] RASubscriptionTypeReqWithMappingV2 model)
        {
            List<SubscriptionTypeReponseDataRev> raRespData = new List<SubscriptionTypeReponseDataRev>();
            SubscriptionTypeReponseRev raResp = new SubscriptionTypeReponseRev();

            string? apiUrl = string.Empty, txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            List<SubscriptionMappingResponse> subscriptionMapping = new List<SubscriptionMappingResponse>();

            HomeWifiLeadDetailsRequestModel reqModel = new HomeWifiLeadDetailsRequestModel();
            HomeWifiCommonResponseModel resp = new HomeWifiCommonResponseModel();

            try
            {
                if(model.subscription_type == null || model.subscription_type == "")
                {
                    model.subscription_type = model.ext_subscription_type;
                }
                /*
                 =====================================================
                 CASE 1: FWA → DBSS ONLY
                 =====================================================
                */
                if (model.initiator_channel == "FWA")
                {
                    apiUrl = String.Format(
                        GetAPICollection.GetSubscriptionTypes,
                        model.subscription_type,
                        model.channel_name
                    );

                    log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                    log.req_time = DateTime.Now;

                    var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetSubscriptionTypes");

                    log.res_time = DateTime.Now;
                    txtResp = Convert.ToString(dbssResp);

                    if (dbssResp != null)
                    {
                        log.res_blob = _blJson.GetGenericJsonData(dbssResp);
                        log.is_success = 1;

                        SubscriptionTypeRootData? dbssRespModel =
                            JsonConvert.DeserializeObject<SubscriptionTypeRootData>(dbssResp.ToString());

                        if (dbssRespModel?.data != null)
                        {
                            var result = ((IEnumerable)dbssRespModel.data).Cast<object>().ToList();
                            raRespData = _dbssToRaParse.SubscripTypesReqParsingV2(result);

                            subscriptionMapping = await _bllCommon.GetSubscriptionMappingV2(model);

                            if (subscriptionMapping.Count > 0)
                            {
                                var filteredRaRespData = raRespData
                                    .Where(r => subscriptionMapping.Any(m =>
                                        m.subscription_code == r.subscription_name))
                                    .ToList();

                                if (filteredRaRespData.Any())
                                {
                                    raResp.data = filteredRaRespData;
                                    raResp.isError = false;
                                    raResp.message = MessageCollection.Success;
                                }
                                else
                                {
                                    raResp.data = null;
                                    raResp.isError = true;
                                    raResp.message = MessageCollection.NoDataFound;
                                }
                            }
                            else
                            {
                                raResp.data = null;
                                raResp.isError = true;
                                raResp.message = "Data not found in Mapping!";
                            }
                        }
                        else
                        {
                            raResp.data = raRespData;
                            raResp.isError = true;
                            raResp.message = "DBSS API doesn't contains any subscription types data.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "Unable to load data from DBSS API.";
                    }

                    return Ok(raResp);
                }

                /*
                 =====================================================
                 CASE 2: NON-FWA → DEX FLOW ONLY
                 =====================================================
                */

                reqModel = new HomeWifiLeadDetailsRequestModel()
                {
                    order_number = model.order_number,
                    retailer_code = model.retailer_id
                };

                resp = await _bllHomeWifiService.BLLHomeWifiLeadDetails(reqModel);

                if (resp != null && resp.data != null)
                {
                    var data = (HomeWifiLeadDetailsResponseData)resp.data;

                    var lead = data.lead_details;
                    var dict = lead as IDictionary<string, object>;

                    var subscriptionCode = dict?["subscription_code"]?.ToString();

                    if (!string.IsNullOrEmpty(subscriptionCode))
                    {
                        raResp.data = new List<SubscriptionTypeReponseDataRev>
                {
                    new SubscriptionTypeReponseDataRev
                    {
                        subscription_id = "11111",
                        subscription_name = subscriptionCode
                    }
                };

                        raResp.isError = false;
                        raResp.message = MessageCollection.Success;
                    }
                    else
                    {
                        raResp.data = new List<SubscriptionTypeReponseDataRev>
                {
                    new SubscriptionTypeReponseDataRev
                    {
                        subscription_id = "",
                        subscription_name = ""
                    }
                };

                        raResp.isError = true;
                        raResp.message = "Dex: No subscription code mapped here!";
                    }
                }
                else
                {
                    raResp.data = null;
                    raResp.isError = true;
                    raResp.message = "Unable to load data from Dex API!";
                }

                return Ok(raResp);
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

                ErrorDescription error =
                    await _bllLog.ManageException(ex, ex.HResult, "BIA");

                log.res_blob = _blJson.GetGenericJsonData(error);

                log.is_success = 0;
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_custom_msg ?? error.error_description;

                raResp.data = raRespData;
                raResp.isError = true;
                raResp.message = string.IsNullOrEmpty(error.error_custom_msg)
                    ? error.error_description
                    : error.error_custom_msg;

                return Ok(raResp);
            }
            finally
            {
                log.method_name = "GetSubscriptionTypes";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [HomeWifiPackageReqeustValidator]
        [CustomAuthorizationFilterInternal]
        [Route("GetPackagesOld")]
        public async Task<IActionResult> GetPackagesV6Old([FromBody][Bind("category_name,channel_name,ext_package_name,initiator_channel,is_bp,lan,offer_name,order_number,order_type,retailer_id,right_id,simkit_type,subscription_id,subscription_name,subscription_type")] PackagesFetchedRequestModel model)
        {
            //Step-0 :
            List<PackagesReponseDataRev> raRespData = new List<PackagesReponseDataRev>();
            List<PackageCodeMappingRespModel> packageCodes = new List<PackageCodeMappingRespModel>();
            PackagesResponseRev raResp = new PackagesResponseRev();
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            PackageRootData? dbssRespModel = new PackageRootData();
            HomeWifiLeadDetailsRequestModel reqModel = new HomeWifiLeadDetailsRequestModel();
            HomeWifiCommonResponseModel resp = new HomeWifiCommonResponseModel();
            try
            {
                reqModel = new HomeWifiLeadDetailsRequestModel()
                {
                    order_number = model.order_number,
                    retailer_code = model.retailer_id
                };

                resp = await _bllHomeWifiService.BLLHomeWifiLeadDetails(reqModel);

                if (resp != null)
                {
                    if (resp.data != null)
                    {
                        var data = (HomeWifiLeadDetailsResponseData)resp.data;

                        var lead = data.lead_details;

                        var dict = lead as IDictionary<string, object>;

                        var offerCode = dict?["offer_code"]?.ToString();
                        var subscriptionCode = dict?["subscription_code"]?.ToString();

                        if (!string.IsNullOrEmpty(offerCode))
                        {
                            raResp.data = new List<PackagesReponseDataRev>
                                {
                                    new PackagesReponseDataRev
                                    {
                                        package_id = "11111",
                                        package_name = offerCode
                                    }
                                };

                            if (raResp.data != null)
                            {
                                raResp.isError = false;
                                raResp.message = MessageCollection.Success;
                            }
                            else
                            {
                                raResp.isError = true;
                                raResp.message = "Dex: No package/offer code mapped here!";
                            }
                        }
                        else
                        {
                            raResp.data = new List<PackagesReponseDataRev>
                                {
                                    new PackagesReponseDataRev
                                    {
                                        package_id = "",
                                        package_name = ""
                                    }
                                };

                            raResp.isError = true;
                            raResp.message = "Dex: No package/offer code mapped here!";
                        }
                    }
                }
                else
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = "Unable to load data from Dex API!";
                }
                return Ok(raResp);
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
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return Ok(raResp);
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.user_id = model.retailer_id;
                log.method_name = "GetPackagesV6";
                string rspStr = string.Empty;
                if (txtResp != null)
                {
                    rspStr = txtResp;
                }
                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [HomeWifiPackageReqeustValidator]
        [CustomAuthorizationFilterInternal]
        [Route("GetPackages")]
        public async Task<IActionResult> GetPackagesV6([FromBody][Bind("category_name,channel_name,ext_package_name,initiator_channel,is_bp,lan,offer_name,order_number,order_type,retailer_id,right_id,simkit_type,subscription_id,subscription_name,subscription_type")] PackagesFetchedRequestModel model)
        {
            //Step-0 :
            List<PackagesReponseDataRev> raRespData = new List<PackagesReponseDataRev>();
            List<PackageCodeMappingRespModel> packageCodes = new List<PackageCodeMappingRespModel>();
            PackagesResponseRev raResp = new PackagesResponseRev();
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            PackageRootData? dbssRespModel = new PackageRootData();
            HomeWifiLeadDetailsRequestModel reqModel = new HomeWifiLeadDetailsRequestModel();
            HomeWifiCommonResponseModel resp = new HomeWifiCommonResponseModel();

            try
            {
                /*
                 =====================================================
                 CASE 1: FWA → DBSS ONLY
                 =====================================================
                */
                if (model.initiator_channel == "FWA")
                {
                    apiUrl = String.Format(GetAPICollection.GetPackagesBySubscriptionTypeId, model.subscription_id);

                    log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                    log.user_id = model.retailer_id;
                    log.req_time = DateTime.Now;

                    var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetPackagesV6");

                    log.res_time = DateTime.Now;
                    txtResp = Convert.ToString(dbssResp);

                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    if (dbssResp != null)
                    {
                        log.is_success = 1;

                        try
                        {
                            dbssRespModel = JsonConvert.DeserializeObject<PackageRootData>(dbssResp.ToString());
                        }
                        catch
                        {
                            raResp.isError = true;
                            raResp.message = MessageCollection.NoDataFound;
                            return Ok(raResp);
                        }

                        if (dbssRespModel != null)
                        {
                            if (dbssRespModel.included != null && dbssRespModel.included is IEnumerable enumerable)
                            {
                                var result = enumerable.Cast<object>().ToList();

                                if (result.Count > 0)
                                {
                                    raRespData = _dbssToRaParse.PackagesParsingV2(result);

                                    if (raRespData.Count > 0)
                                    {
                                        packageCodes = await _bllCommon.GetPackageMappingV2(model);

                                        if (packageCodes.Count > 0)
                                        {
                                            var filteredRaRespData = raRespData
                                                .Where(r => packageCodes.Any(m => m.package_code == r.package_name))
                                                .ToList();

                                            if (filteredRaRespData.Any())
                                            {
                                                raResp.data = filteredRaRespData;
                                                raResp.isError = false;
                                                raResp.message = MessageCollection.Success;
                                            }
                                            else
                                            {
                                                raResp.data = null;
                                                raResp.isError = true;
                                                raResp.message = MessageCollection.NoDataFound;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        raResp.data = raRespData;
                                        raResp.isError = true;
                                        raResp.message = MessageCollection.NoDataFound;
                                    }
                                }
                                else
                                {
                                    raResp.data = new List<PackagesReponseDataRev>();
                                    raResp.isError = true;
                                    raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                                }
                            }
                            else
                            {
                                raResp.data = new List<PackagesReponseDataRev>();
                                raResp.isError = true;
                                raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                            }
                        }
                        else
                        {
                            raResp.data = raRespData;
                            raResp.isError = true;
                            raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                        }
                    }

                    return Ok(raResp);
                }

                /*
                 =====================================================
                 CASE 2: NON-FWA → DEX ONLY
                 =====================================================
                */
                reqModel = new HomeWifiLeadDetailsRequestModel()
                {
                    order_number = model.order_number,
                    retailer_code = model.retailer_id
                };

                resp = await _bllHomeWifiService.BLLHomeWifiLeadDetails(reqModel);

                if (resp != null)
                {
                    if (resp.data != null)
                    {
                        var data = (HomeWifiLeadDetailsResponseData)resp.data;

                        var lead = data.lead_details;

                        var dict = lead as IDictionary<string, object>;

                        var offerCode = dict?["offer_code"]?.ToString();
                        var subscriptionCode = dict?["subscription_code"]?.ToString();

                        if (!string.IsNullOrEmpty(offerCode))
                        {
                            raResp.data = new List<PackagesReponseDataRev>
                    {
                        new PackagesReponseDataRev
                        {
                            package_id = "11111",
                            package_name = offerCode
                        }
                    };

                            if (raResp.data != null)
                            {
                                raResp.isError = false;
                                raResp.message = MessageCollection.Success;
                            }
                            else
                            {
                                raResp.isError = true;
                                raResp.message = "Dex: No package/offer code mapped here!";
                            }
                        }
                        else
                        {
                            raResp.data = new List<PackagesReponseDataRev>
                    {
                        new PackagesReponseDataRev
                        {
                            package_id = "",
                            package_name = ""
                        }
                    };

                            raResp.isError = true;
                            raResp.message = "Dex: No package/offer code mapped here!";
                        }
                    }
                }
                else
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = "Unable to load data from Dex API!";
                }

                return Ok(raResp);
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

                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg)
                    ? error.error_description
                    : error.error_custom_msg;

                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return Ok(raResp);
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.user_id = model.retailer_id;
                log.method_name = "GetPackagesV6";

                string rspStr = string.Empty;
                if (txtResp != null)
                {
                    rspStr = txtResp;
                }

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetPreloadedData")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> GetPreloadedData([FromBody][Bind("channel_id,channel_name,lan,retailer_id")] PreloadDataRequestModel model)
        {
            List<SubscriptionTypeReponseDataRev> raRespData = new List<SubscriptionTypeReponseDataRev>();
            SubscriptionTypeReponseRev raResp = new SubscriptionTypeReponseRev();
            PreloadDataListResponse response = new PreloadDataListResponse();
            string? apiUrl = string.Empty, txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            List<SubscriptionMappingResponse> subscriptionMapping = new List<SubscriptionMappingResponse>();
            PreloadDataReq reqModel = new PreloadDataReq();
            HomeWifiCommonResponseModel resp = new HomeWifiCommonResponseModel();
            string endpoint = string.Empty;

            try
            {
                endpoint = SettingsValues.GetDPELeadPreloadAPIendPoint();

                log.req_blob = _blJson.GetGenericJsonData(new
                {
                    apiUrl = $"{SettingsValues.GetDPEBaseUrl()?.TrimEnd('/')}{endpoint}"
                });

                var responseObj = await _apiCall.HTTPGetRequestPreloadData(endpoint, model.retailer_id, "GetPreloadedData","FWA");

                // Validate before use; previously this was dereferenced first and only
                // null-checked afterwards.
                if (responseObj == null)
                {
                    throw new Exception("Preload data response is null.");
                }

                var plans = responseObj.payload.data.plans;
                var devices = responseObj.payload.data.devices;
                var coverage = responseObj.payload.data.coverage;
                var nationality = responseObj.payload.data.nationality;

                var deviceResponse = devices.Select(device => new DeviceResponse
                {
                    device_code = device.device_code,
                    name = device.name,
                    price = device.price,

                    plan_code_list = plans
                        .Where(p => device.plan_code_list.Contains(p.plan_code))
                        .ToList()

                }).ToList();

                log.res_blob = _blJson.GetGenericJsonData(responseObj);

                response = new PreloadDataListResponse
                {
                    isError = responseObj.status != "SUCCESS",
                    message = "Preload data fetched successfully.",
                    data = new AppResponsePreloadData
                    {
                        devices = deviceResponse,
                        coverage = coverage,
                        nationality = nationality
                    }
                };

                return Ok(response);
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
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? error.error_description;
                raResp.data = raRespData;
                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                return Ok(raResp);
            }
            finally
            {
                log.method_name = "GetPreloadedData";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;
                string rspStr = string.Empty;
                if (txtResp != null)
                {
                    rspStr = txtResp;
                }
                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [Route("GetUnpairedSIMlistHomeWifi")]
        [CustomAuthorizationFilterInternal]
        public async Task<IActionResult> GetUnpairedSIMlistHomeWifi(UnpairedSIMsearchReqModelV2 model)
        {
            List<SIMReponseDataRev> raRespData = new List<SIMReponseDataRev>();
            UnpairedSIMDataRev raResp = new UnpairedSIMDataRev();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse dMSParse = new BLLRAToDBSSParse();
            ProductInfoRespModel productInfo = new ProductInfoRespModel();

            string userName = string.Empty;
            string password = string.Empty;
            string sim_s = string.Empty;
            try
            {
                try
                {
                    
                    try
                    {
                        productInfo = await _bllCommon.GetProductValueForSIMSearching(model);

                        model.product_code = productInfo.product_code;
                        model.product_category = productInfo.product_category;

                        if (model.sim_serial.Length < 4)
                        {
                            string msg = "sim_serial must be last 4 digits!";
                            raResp.isError = true;
                            raResp.message = msg;
                            return Ok(raResp);
                        }
                        else if (model.sim_serial.Length > 4)
                        {
                            sim_s = model.sim_serial.Substring(model.sim_serial.Length - Math.Min(4, model.sim_serial.Length));
                            model.sim_serial = sim_s;
                        }
                    }
                    catch (Exception)
                    {
                        string keyNotFound = "sim_serial is Mandatory!";
                        raResp.isError = true;
                        raResp.message = keyNotFound;
                        return Ok(raResp);
                    }
                    try { model.user_name = SettingsValues.GetDMSUserName(); }
                    catch (Exception) { throw new Exception("userName is not found in appsettings.json"); }

                    try { model.password = SettingsValues.GetDMSPassword(); }
                    catch (Exception) { throw new Exception("dms_pas is not found in appsettings.json"); }

                }
                catch (Exception ex)
                {
                    string keyNotFound = ex.Message;
                    raResp.isError = true;
                    raResp.message = keyNotFound;
                    return Ok(raResp);
                }

                apiUrl = String.Format(UnpairedMSISDNList.CheckUnpairedSIM);
                UnpairedSIMreqRootModel reqValue = dMSParse.UnpairedSIMReqModelParseV2(model);
                log.req_blob = _blJson.GetGenericJsonData(reqValue);
                log.req_time = DateTime.Now;

                JObject dmsResp = (JObject)await _apiReq.HttpPostRequestSIMSerial(reqValue, apiUrl, "GetUnpairedSIMlistHomeWifi");

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dmsResp);
                log.res_blob = _blJson.GetGenericJsonData(dmsResp);

                if (dmsResp != null)
                {
                    log.res_blob = _blJson.GetGenericJsonData(dmsResp);

                    log.is_success = 1;

                    UnpairedSIMRespRootData? dbssRespModel = JsonConvert.DeserializeObject<UnpairedSIMRespRootData>(dmsResp.ToString());

                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel.data != null)
                        {
                            var result = ((IEnumerable)dbssRespModel.data).Cast<object>().ToList();

                            raRespData = _dbssToRaParse.UnpairedSIMListDataParsingV2(result);

                            if (raRespData.Count > 0)
                            {
                                raResp.data = raRespData;
                                raResp.isError = false;
                                raResp.message = MessageCollection.Success;
                            }
                            else
                            {
                                raResp.data = raRespData;
                                raResp.isError = true;
                                raResp.message = MessageCollection.NoDataFound;
                            }
                        }
                        else
                        {
                            raResp.data = raRespData;
                            raResp.isError = true;
                            raResp.message = "DMS API doesn't return any SIM.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "DMS API doesn't return any SIM.";
                    }
                }
                else
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = "Unable to load data from DMS API.";
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
                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(ex.Message);

                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    log.is_success = 0;
                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                }
                catch (Exception)
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = ex.Message;
                }
            }
            finally
            {
                log.method_name = "GetUnpairedSIMlistHomeWifi";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_code;
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }

                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(raResp);
        }
    }
}
