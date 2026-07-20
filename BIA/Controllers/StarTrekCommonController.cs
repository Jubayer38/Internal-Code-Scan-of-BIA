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
using System.Net;
using System.Text;
using System.Collections;
using Serilog;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace BIA.Controllers
{
    [Route("api/StarTrekCommon")]
    [ApiController]
    public class StarTrekCommonController : ControllerBase
    {
        private readonly BaseController _bio;
        private readonly eShopAPICall _eShopAPI;
        private readonly BLLDBSSToRAParse _dbssToRaParse;
        private readonly BLLLog _bllLog;
        private readonly BLLCommon _bllCommon;
        private readonly ApiRequest _apiReq;

        public StarTrekCommonController(BaseController bio, eShopAPICall eShopAPI, BLLDBSSToRAParse dbssToRaParse, BLLLog bllLog, BLLCommon bllCommon, ApiRequest apiReq)
        {
            _bio = bio;
            _eShopAPI = eShopAPI;
            _dbssToRaParse = dbssToRaParse;
            _bllLog = bllLog;
            _bllCommon = bllCommon;
            _apiReq = apiReq;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("validate-msisdn")]
        public async Task<IActionResult> ValidateUnpairedMSISDN([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequest msisdnCheckReqest)
        {
            try
            {
                RACommonResponseRevamp response = new RACommonResponseRevamp();

                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (SettingsValues.GetRyzeAllowOrNot() == 1)
                {
                    response = await _bio.ValidateUnpairedMSISDNSTartTrek(msisdnCheckReqest, "ValidateUnpairedMSISDNSTartTrek");
                }
                else
                {
                    response = await _bio.ValidateUnpairedMSISDNSTartTrekV2(msisdnCheckReqest, "ValidateUnpairedMSISDNSTartTrekV2");
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
                
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg:error.error_description,
                    data = new Datas()
                    {
                        isEsim = 0,
                        request_id = "0"
                    }

                });
            }
        }

        [HttpPost]
        [Route("validate-msisdnv2")]
        public async Task<IActionResult> ValidateUnpairedMSISDNV2([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequest msisdnCheckReqest)
        {
            try
            {
                RACommonResponseRevampV3 response = new RACommonResponseRevampV3();

                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (SettingsValues.GetRyzeAllowOrNot() == 1)
                {
                    response = await _bio.ValidateUnpairedMSISDNSTartTrekV4(msisdnCheckReqest, "ValidateUnpairedMSISDNSTartTrekV4");
                }
                else
                {
                    response = await _bio.ValidateUnpairedMSISDNSTartTrekV3(msisdnCheckReqest, "ValidateUnpairedMSISDNSTartTrekV3");
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
                    message = ex.Message,
                    data = new Datas()
                    {
                        isEsim = 0,
                        request_id = "0"
                    }

                });
            }
        }


        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("validate-msisdn-online")]
        public async Task<IActionResult> ValidateUnpairedMSISDNOnline([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,order_id,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequestOnline msisdnCheckReqest)
        {
            try
            {
                RACommonResponseRevamp response = new RACommonResponseRevamp();
                eShopOrderResponseModel responseModel = new eShopOrderResponseModel();
                string reservation_id = string.Empty;
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                eShopOrderValidationReqModel eShopOrder = new eShopOrderValidationReqModel()
                {
                    orderId = msisdnCheckReqest.order_id??"",
                    msisdn = msisdnCheckReqest.mobile_number,
                    retailer_id = msisdnCheckReqest.retailer_id
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
                        else if (string.IsNullOrEmpty(responseModel.data.reservation_id))
                        {
                            throw new Exception("The reservation_id field is empty!");
                        }
                        else if (!string.IsNullOrEmpty(responseModel.data.reservation_id) && responseModel.data.status_code == 200 && responseModel.data.is_reserved == true)
                        {
                            reservation_id = responseModel.data.reservation_id;
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

                response = await _bio.ValidateUnpairedMSISDNSTartTrekOnline(msisdnCheckReqest, reservation_id, "ValidateUnpairedMSISDNOnline");

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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas()
                    {
                        isEsim = 0,
                        request_id = "0"
                    }
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("Validate-msisdn-esim")]
        public async Task<IActionResult> ValidateUnpairedMSISDN_ESIM([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequest msisdnCheckReqest)
        {
            RACommonResponseRevamp rACommonResponse = new RACommonResponseRevamp();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (SettingsValues.GetRyzeAllowOrNot() == 1)
                {
                    rACommonResponse = await _bio.ValidateUnpairedMSISDNESIM(msisdnCheckReqest, "ValidateUnpairedMSISDNESIM");
                }
                else
                {
                    rACommonResponse = await _bio.ValidateUnpairedMSISDNESIMV2(msisdnCheckReqest, "ValidateUnpairedMSISDNESIMV2");
                }

                return Ok(rACommonResponse);
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas()
                    {
                        request_id = "0",
                        isEsim = 1
                    }
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("Validate-msisdn-esim-online")]
        public async Task<IActionResult> ValidateUnpairedMSISDN_ESIM_Online([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,order_id,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequestOnline msisdnCheckReqest)
        {
            RACommonResponseRevamp rACommonResponse = new RACommonResponseRevamp();
            eShopOrderResponseModel responseModel = new eShopOrderResponseModel();
            string reservation_id = string.Empty;
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                eShopOrderValidationReqModel eShopOrder = new eShopOrderValidationReqModel()
                {
                    orderId = msisdnCheckReqest.order_id ?? "",
                    msisdn = msisdnCheckReqest.mobile_number,
                    retailer_id = msisdnCheckReqest.retailer_id
                };

                responseModel = await _eShopAPI.OrderValidation(eShopOrder);

                if (responseModel != null)
                {
                    if (string.IsNullOrEmpty(responseModel.data.reservation_id))
                    {
                        throw new Exception("MSISDN isn't reserved in eShop yet!");
                    }
                    else if (!string.IsNullOrEmpty(responseModel.data.reservation_id) && responseModel.data.status_code == 200)
                    {
                        reservation_id = responseModel.data.reservation_id;
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

                rACommonResponse = await _bio.ValidateUnpairedMSISDNESIM_Online(msisdnCheckReqest, reservation_id, "ValidateUnpairedMSISDNESIM");

                return Ok(rACommonResponse);
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas()
                    {
                        request_id = "0",
                        isEsim = 1
                    }
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("validate-sim-replacement")]
        public async Task<IActionResult> STarTrekValidateSIMReplacement([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest)
        {
            BIAToDBSSLog log = new BIAToDBSSLog();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            IndividualSIMReplacementMSISDNCheckResponseRevamp response = new IndividualSIMReplacementMSISDNCheckResponseRevamp();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (SettingsValues.GetRyzeAllowOrNot() == 1)
                {
                    response = await _bio.STarTrekValidateSIMForReplacement(msisdnCheckReqest);
                }
                else
                {
                    response = await _bio.STarTrekValidateSIMForReplacementV2(msisdnCheckReqest);
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas()
                    {
                        isEsim = 0,
                        request_id = "0"
                    }
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("validate-sim-replacement-online")]
        public async Task<IActionResult> STarTrekValidateSIMReplacementOnline([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,order_id,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] IndividualSIMReplsMSISDNCheckRequestOnline msisdnCheckReqest)      
        {
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            IndividualSIMReplacementMSISDNCheckResponseRevamp response = new IndividualSIMReplacementMSISDNCheckResponseRevamp();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = string.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingOwnerCustomerUserCustomerSimCardInfo, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                JObject dbssResp = new JObject();
                try
                {
                    dbssResp = await _apiReq.HttpGetRequest(apiUrl, "STarTrekValidateSIMReplacementOnline");

                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("Not Found"))
                    {
                        throw new Exception("Invalid MSISDN input for SIM Replacement.");
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

                var msisdnResp = _bio.StarTrekSIMReplacementParsing(dbssResp);

                if (msisdnResp.result == false)
                {
                    return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = FixedValueCollection.MSISDNError + msisdnResp.message
                    });
                }

                var simResp = await _bio.CheckSIMNumberReplacement(new SIMNumberCheckRequest()
                {
                    center_code = string.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
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
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_custom_msg ?? string.Empty;

                return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                });
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "STarTrekValidateSIMReplacement";
                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("validate-sim-replacement-esim")]
        public async Task<IActionResult> StarTrekValidateSIMReplacement_ESIM([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest)
        {
            BIAToDBSSLog log = new BIAToDBSSLog();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            IndividualSIMReplacementMSISDNCheckResponseRevamp response = new IndividualSIMReplacementMSISDNCheckResponseRevamp();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }
                if (string.IsNullOrWhiteSpace(msisdnCheckReqest.retailer_id) || msisdnCheckReqest.retailer_id.Length > 13)
                {
                    throw new ArgumentException("Invalid retailer_id.");
                }

                if (SettingsValues.GetRyzeAllowOrNot() == 1)
                {
                    // NOSONAR: retailer_id is not used in any network call
                    response = await _bio.StarTrekValidateSIMForReplacement_ESIM(msisdnCheckReqest);
                }
                else
                {
                    // NOSONAR: retailer_id is not used in any network call
                    response = await _bio.StarTrekValidateSIMForReplacement_ESIMV2(msisdnCheckReqest);
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas()
                    {
                        isEsim = 0,
                        request_id = "0"
                    }
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("validate-sim-replacement-esim-online")]
        public async Task<IActionResult> StarTrekValidateSIMReplacement_ESIMOnline([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,order_id,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] IndividualSIMReplsMSISDNCheckRequestOnline msisdnCheckReqest)
        {
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = string.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingOwnerCustomerUserCustomerSimCardInfo, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                JObject dbssResp = new JObject();
                try
                {
                    dbssResp = await _apiReq.HttpGetRequest(apiUrl, "StarTrekValidateSIMReplacement_ESIMOnline");

                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("Not Found"))
                    {
                        throw new Exception("Invalid MSISDN input for E-SIM Replacement.");
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
                    return Ok(new IndividualSIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = MessageCollection.SIMReplNoDataFound,
                    });
                }

                log.is_success = 1;

                var msisdnResp = _bio.StarTrekSIMReplacementParsing(dbssResp);

                if (msisdnResp.result == false)
                {
                    return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = FixedValueCollection.MSISDNError + msisdnResp.message
                    });
                }

                var simResp = await _bio.CheckSIMNumber4(new SIMNumberCheckRequest()
                {
                    center_code = string.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
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
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_custom_msg ?? string.Empty;

                return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                });
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "StarTrekValidateSIMReplacement_ESIM";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("cyn-online")]
        public async Task<IActionResult> GetUnpairedMSISDNLOnline([FromBody][Bind("FWA_channel_name,channel_name,is_fwa,msisdn,retailer_id,right_id,session_token")] UnpairedMSISDNListReqModel model)
        {
            List<ReponseDataRev> raRespData = new List<ReponseDataRev>();
            UnpairedMSISDNDataRev raResp = new UnpairedMSISDNDataRev();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (string.IsNullOrEmpty(model.msisdn))
                {
                    model.msisdn = await _bllCommon.GetUnpairedMSISDNSearchDefaultValueV2(model);

                    if (string.IsNullOrEmpty(model.msisdn))
                    {
                        return Ok(raResp);
                    }
                    if (model.msisdn.Substring(0, 4) != FixedValueCollection.MSISDNFixedValue)
                    {
                        model.msisdn = FixedValueCollection.MSISDNFixedValue + model.msisdn;
                    }
                    if (model.msisdn.Substring(0, 1) == "0")
                    {
                        model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                    }
                }
                else
                {
                    if (model.msisdn.Substring(0, 4) != FixedValueCollection.MSISDNFixedValue)
                    {
                        model.msisdn = FixedValueCollection.MSISDNFixedValue + model.msisdn;
                    }
                    if (model.msisdn.Substring(0, 1) == "0")
                    {
                        model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                    }
                }

                string channelIdFromConfig = SettingsValues.GetChannelId();                
                string stockIdFromConfig = SettingsValues.GetChannelStockId();
                string stockIdByDefault = SettingsValues.GetChannelStockDefault();
                string[] arrChannelId = Array.Empty<string>();
                string[] arrStockId = Array.Empty<string>();
                string channelId = string.Empty;
                int arrIndexChannel = 0;
                string stockIdValue = string.Empty;
                
                if (channelIdFromConfig.Contains(","))
                {
                    arrChannelId = channelIdFromConfig.Split(',');
                }
                else
                {
                    arrChannelId = channelIdFromConfig.Split(' ');
                }

                if (stockIdFromConfig.Contains(","))
                {
                    arrStockId = stockIdFromConfig.Split(',');
                }
                else
                {
                    arrStockId = stockIdFromConfig.Split(' ');
                }

                channelId = await _dbssToRaParse.GetStockResponses(model.channel_name);

                if (arrChannelId.Contains(channelId))
                {
                    arrIndexChannel = Array.IndexOf(arrChannelId, channelId);
                    stockIdValue = arrStockId[arrIndexChannel];
                }
                else
                {
                    stockIdValue = stockIdByDefault;
                }

                apiUrl = string.Format(UnpairedMSISDNList.GetCYNListOnline, 1, 10, model.msisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetUnpairedMSISDNLOnline");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                if (dbssResp != null)
                {
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    log.is_success = 1;
                    UnpairedMSISDNRootData? dbssRespModel = JsonConvert.DeserializeObject<UnpairedMSISDNRootData>(dbssResp.ToString());
                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel.data != null)
                        {
                            var result = ((IEnumerable<object>)dbssRespModel.data).ToList();

                            raRespData = _dbssToRaParse.UnpairedMSISDNListDataParsingV2(result);

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
                            raResp.message = "DBSS API doesn't contains any Unpaired MSISDN list.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "DBSS API doesn't contains any Unpaired MSISDN list.";
                    }
                }
                else
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = "Unable to load data from DBSS API.";
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
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                log.is_success = 0;
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_custom_msg ?? string.Empty;

                raResp.data = raRespData;
                raResp.isError = true;
                raResp.message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.method_name = "GetUnpairedMSISDNLOnline";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }
                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(raResp);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("cyn-physical")]
        public async Task<IActionResult> GetUnpairedMSISDNPhysical([FromBody][Bind("msisdn,retailer_id,channel_name,is_fwa,FWA_channel_name,session_token,right_id")] UnpairedMSISDNListReqModel model)
        {
            List<ReponseDataRev> raRespData = new List<ReponseDataRev>();
            UnpairedMSISDNDataRev raResp = new UnpairedMSISDNDataRev();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (string.IsNullOrEmpty(model.msisdn))
                {
                    model.msisdn = await _bllCommon.GetUnpairedMSISDNSearchDefaultValueV2(model);

                    if (string.IsNullOrEmpty(model.msisdn))
                    {
                        return Ok(raResp);
                    }
                    if (model.msisdn.Substring(0, 4) != FixedValueCollection.MSISDNFixedValue)
                    {
                        model.msisdn = FixedValueCollection.MSISDNFixedValue + model.msisdn;
                    }
                    if (model.msisdn.Substring(0, 1) == "0")
                    {
                        model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                    }
                }
                else
                {
                    if (model.msisdn.Substring(0, 4) != FixedValueCollection.MSISDNFixedValue)
                    {
                        model.msisdn = FixedValueCollection.MSISDNFixedValue + model.msisdn;
                    }
                    if (model.msisdn.Substring(0, 1) == "0")
                    {
                        model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                    }
                }
                string channelIdFromConfig = SettingsValues.GetChannelId();
                string stockIdFromConfig = SettingsValues.GetChannelStockId();
                string stockIdByDefault = SettingsValues.GetChannelStockDefault();
                string[] arrStockId = Array.Empty<string>();
                string[] arrChannelId = Array.Empty<string>();
                string channelId = string.Empty;
                int arrIndexChannel = 0;
                string stockIdValue = string.Empty;  

                if (channelIdFromConfig.Contains(","))
                {
                    arrChannelId = channelIdFromConfig.Split(',');
                }
                else
                {
                    arrChannelId = channelIdFromConfig.Split(' ');
                }

                if (stockIdFromConfig.Contains(","))
                {
                    arrStockId = stockIdFromConfig.Split(',');
                }
                else
                {
                    arrStockId = stockIdFromConfig.Split(' ');
                }

                channelId = await _dbssToRaParse.GetStockResponses(model.channel_name);

                if (arrChannelId.Contains(channelId))
                {
                    arrIndexChannel = Array.IndexOf(arrChannelId, channelId);
                    stockIdValue = arrStockId[arrIndexChannel];
                }
                else
                {
                    stockIdValue = stockIdByDefault;
                }

                raResp = await _bio.GetCYNdatafromDBBS(model, stockIdValue);
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
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_custom_msg ?? string.Empty;

                raResp.data = raRespData;
                raResp.isError = true;
                raResp.message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.method_name = "GetUnpairedMSISDNPhysical";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }
                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(raResp);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("validate-msisdn-mnp")]
        public async Task<IActionResult> StarTrekValidateMSISDNMNP([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequest msisdnCheckReqest)
        {
            try
            {
                RACommonResponseRevamp response = new RACommonResponseRevamp();

                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (SettingsValues.GetRyzeAllowOrNot() == 1)
                {
                    response = await _bio.ValidateUnpairedMSISDNMNPSTartTrek(msisdnCheckReqest, "ValidateUnpairedMSISDNMNPSTartTrek");
                }
                else
                {
                    response = await _bio.ValidateUnpairedMSISDNMNPSTartTrekV2(msisdnCheckReqest, "ValidateUnpairedMSISDNMNPSTartTrekV2");
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg,
                    data = new Datas()
                    {
                        isEsim = 0,
                        request_id = "0"
                    }

                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("Validate-msisdn-mnp-esim")]
        public async Task<IActionResult> StarTrekValidateMSISDNMNP_ESIM([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequest msisdnCheckReqest)
        {
            RACommonResponseRevamp rACommonResponse = new RACommonResponseRevamp();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }
                if (string.IsNullOrWhiteSpace(msisdnCheckReqest.retailer_id) || msisdnCheckReqest.retailer_id.Length > 13)
                {
                    throw new ArgumentException("Invalid retailer_id.");
                }

                if (SettingsValues.GetRyzeAllowOrNot() == 1)
                {
                    // NOSONAR: retailer_id is not used in any network call
                    rACommonResponse = await _bio.ValidateUnpairedMSISDNMNPSTartTrekesim(msisdnCheckReqest, "ValidateUnpairedMSISDNMNPSTartTrekesim");
                }
                else
                {
                    // NOSONAR: retailer_id is not used in any network call
                    rACommonResponse = await _bio.ValidateUnpairedMSISDNMNPSTartTrekesimV2(msisdnCheckReqest, "ValidateUnpairedMSISDNMNPSTartTrekesimV2");
                }

                return Ok(rACommonResponse);
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
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg,
                    data = new Datas()
                    {
                        isEsim = 1,
                        request_id = "0"
                    }

                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("validate-msisdn-online-resubmit")]
        public async Task<IActionResult> ValidateUnpairedMSISDNOnlineV2([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,order_id,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequestOnline msisdnCheckReqest)
        {
            try
            {
                RACommonResponseRevamp response = new RACommonResponseRevamp();
                eShopOrderResponseModel responseModel = new eShopOrderResponseModel();
                string reservation_id = string.Empty;
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                eShopOrderValidationReqModel eShopOrder = new eShopOrderValidationReqModel()
                {
                    orderId = msisdnCheckReqest.order_id ?? "",
                    msisdn = msisdnCheckReqest.mobile_number,
                    retailer_id = msisdnCheckReqest.retailer_id
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
                        else if (string.IsNullOrEmpty(responseModel.data.reservation_id))
                        {
                            throw new Exception("The reservation_id field is empty!");
                        }
                        else if (!string.IsNullOrEmpty(responseModel.data.reservation_id) && responseModel.data.status_code == 200 && responseModel.data.is_reserved == true)
                        {
                            reservation_id = responseModel.data.reservation_id;
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

                response = await _bio.ValidateUnpairedMSISDNSTartTrekOnlineV2(msisdnCheckReqest, reservation_id, "ValidateUnpairedMSISDNOnlineV2");

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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg,
                    data = new Datas()
                    {
                        isEsim = 0,
                        request_id = "0"
                    }
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("Validate-msisdn-esim-online-resubmit")]
        public async Task<IActionResult> ValidateUnpairedMSISDN_ESIM_OnlineV2([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,order_id,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequestOnline msisdnCheckReqest)
        {
            RACommonResponseRevamp rACommonResponse = new RACommonResponseRevamp();
            eShopOrderResponseModel responseModel = new eShopOrderResponseModel();
            string reservation_id = string.Empty;
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                eShopOrderValidationReqModel eShopOrder = new eShopOrderValidationReqModel()
                {
                    orderId = msisdnCheckReqest.order_id ?? "",
                    msisdn = msisdnCheckReqest.mobile_number
                };

                responseModel = await _eShopAPI.OrderValidation(eShopOrder);

                if (responseModel != null)
                {
                    if (string.IsNullOrEmpty(responseModel.data.reservation_id))
                    {
                        throw new Exception("MSISDN isn't reserved in eShop yet!");
                    }
                    else if (!string.IsNullOrEmpty(responseModel.data.reservation_id) && responseModel.data.status_code == 200)
                    {
                        reservation_id = responseModel.data.reservation_id;
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

                rACommonResponse = await _bio.ValidateUnpairedMSISDNESIM_OnlineV2(msisdnCheckReqest, reservation_id, "ValidateUnpairedMSISDNESIM");

                return Ok(rACommonResponse);
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg,
                    data = new Datas()
                    {
                        request_id = "0",
                        isEsim = 1
                    }
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("validate-msisdn-cherish")]
        public async Task<IActionResult> ValidateChrishMSISDN([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,selected_category,session_token,sim_category,sim_number")] CherishMSISDNCheckRequest msisdnCheckReqest)
        {
            try
            {
                RACommonResponseRevamp response = new RACommonResponseRevamp();

                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (SettingsValues.GetRyzeAllowOrNot() == 1)
                {
                    response = await _bio.ValidateMSISDNSTartTrekCherish(msisdnCheckReqest, "ValidateMSISDNSTartTrekCherish");
                }
                else
                {
                    response = await _bio.ValidateMSISDNSTartTrekCherishV2(msisdnCheckReqest, "ValidateMSISDNSTartTrekCherishV2");
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
                    message = ex.Message,
                    data = new Datas()
                    {
                        isEsim = 0,
                        request_id = "0"
                    }

                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("Validate-msisdn-cherish-esim")]
        public async Task<IActionResult> ValidateChrishMSISDN_ESIM([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,selected_category,session_token,sim_category,sim_number")] CherishMSISDNCheckRequest msisdnCheckReqest)
        {
            RACommonResponseRevamp rACommonResponse = new RACommonResponseRevamp();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (SettingsValues.GetRyzeAllowOrNot() == 1)
                {
                    rACommonResponse = await _bio.ValidateCherishMSISDNESIM(msisdnCheckReqest, "ValidateUnpairedMSISDNESIM");
                }
                else
                {
                    rACommonResponse = await _bio.ValidateUnpairedMSISDNESIMV3(msisdnCheckReqest, "ValidateUnpairedMSISDNESIMV2");
                }

                return Ok(rACommonResponse);
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
                    message = ex.Message,
                    data = new Datas()
                    {
                        request_id = "0",
                        isEsim = 1
                    }
                });
            }
        }

    }
}
