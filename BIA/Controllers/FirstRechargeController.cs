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
using BIA.JWET;
using BIA.JWT;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Configuration;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Controllers
{
    [Route("api/FirstRecharge")]
    [ApiController]
    public class FirstRechargeController : ControllerBase
    {
        private readonly BLLFirstRecharge _frManager;
        private readonly BaseController _bio;
        private readonly BLLLog _bllLog;
        private readonly BLLFTRRestriction _bLLFTR;
        private readonly FTR_Restrict _ftr_restrict;
        private readonly BL_Json _blJson;
        private readonly BLLCommon _bLLCommon; 
        private readonly ApiRequest _apiRequest;

        public FirstRechargeController(BLLFirstRecharge frManager, BaseController bio, BLLLog bllLog, BLLFTRRestriction bLLFTR, FTR_Restrict ftr_restrict, BL_Json blJson, BLLCommon bLLCommon, ApiRequest apiRequest)
        {
            _frManager = frManager;
            _bio = bio;
            _bllLog = bllLog;
            _bLLFTR = bLLFTR;
            _ftr_restrict = ftr_restrict;
            _blJson = blJson;
            _bLLCommon = bLLCommon;
            _apiRequest = apiRequest;
        }

        [HttpPost]
        [Route("SubmitFirstRecharge")]
        public async Task<IActionResult> SubmitFirstRecharge([FromBody][Bind("amount,bi_token_number,deviceId,lan,lat,lng,paymentType,retailerCode,session_token,subscriberNo,userId,userPin")] FirstRechargeRequestModel request)
        {
            string apiUrl = RetailerAPI.RechargeAPI;
            ErrorDescription error = new ErrorDescription();
            RechargeResponseModel? apiResponse = new RechargeResponseModel();
            BL_Json _blJson = new BL_Json();
            BIAToDBSSLog log = new BIAToDBSSLog();
            RechargeResponseModel rechargeResponse = new RechargeResponseModel();
            string responseContent = String.Empty;
            RechargeReqModel reqModel = new RechargeReqModel();
            BLLRAToDBSSParse dBSSParse = new BLLRAToDBSSParse();
            JWETToken jWETToken = new JWETToken();
            ValidTokenResponse security = new ValidTokenResponse();
            FTRDBUpdateModel fTRDBUpdateModel = new FTRDBUpdateModel();
            FTRAirResponseModel fTRAir = new FTRAirResponseModel();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RechargeRequestModel model = new RechargeRequestModel();
            try
            {
                model = populateModel.FirstRechargeRequestPopulateModel(request);

                string loginProviderId = string.Empty;
                double balance = 0;
                int isFtrFeatureOn = SettingsValues.GetisFtrFeatureOn();
                string loginProviderIdRet = SettingsValues.GetJWETLoginProvider();
                int addMinutes = SettingsValues.GetaddMinutesForJWET();
                int substractMinutes = SettingsValues.GetsubstarctMinutesForJWET();
                string secreteKey = SettingsValues.GetJWTSequrityKey();
                string channelName = string.Empty;
                string userName = string.Empty;
                
                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (model.userId != null)
                        {
                            if (!model.userId.Equals(security.UserName))
                            {
                                throw new Exception(SettingsValues.GetSessionMessage());
                            }
                        }

                        loginProviderId = security.LoginProviderId;
                        channelName = security.ChannelName;
                        userName = security.UserName;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (isFtrFeatureOn == 1)
                {
                    RetailerBalanceRespModel balanceAmount = await _ftr_restrict.CheckEVBalance(model.retailerCode, model.subscriberNo, model.userPin, model.bi_token_number, model.subscriberNo);

                    try
                    {
                        if (balanceAmount.Balance != null && balanceAmount.Balance == "0")
                        {
                            return Ok(new RechargeResponseModel()
                            {
                                isError = true,
                                message = balanceAmount.message
                            });
                        }
                        else if (!String.IsNullOrEmpty(balanceAmount.message) && balanceAmount.message.Contains("Invalid"))
                        {
                            return Ok(new RechargeResponseModel()
                            {
                                isError = true,
                                message = balanceAmount.message
                            });
                        }
                        else
                        {
                            balance = Convert.ToDouble(balanceAmount.Balance);
                        }
                    }
                    catch
                    {
                        balance = 0;
                    }
                    if (balance >= Convert.ToDouble(model.amount))
                    {
                        fTRAir = await _ftr_restrict.FTRRestrictionRequsetToAIR(model, channelName, userName);
                        if (fTRAir != null)
                        {
                            if (fTRAir.responseCode == 0)
                            {
                                await Task.Delay(500);
                                string retailer_code = string.Empty;
                                if (model.retailerCode != null && model.retailerCode.Substring(0, 1) != "R")
                                {
                                    retailer_code = "R" + model.retailerCode;
                                    model.userId = model.retailerCode;
                                }
                                else
                                {
                                    retailer_code = model.retailerCode ?? "";
                                    model.userId = model.retailerCode ?? "".Substring(0, 1);
                                }
                                if (String.IsNullOrEmpty(model.deviceId))
                                {
                                    model.deviceId = "BL-Smartpos-app";
                                }

                                model.sessionToken = jWETToken.GenerateJWETToken(model.subscriberNo, retailer_code, model.deviceId, loginProviderIdRet??"", model.userId, 0);

                                log.req_time = DateTime.Now;

                                reqModel = dBSSParse.RechargeReqPargeModel(model);

                                log.req_blob = _blJson.GetGenericJsonData(reqModel);

                                JObject responseData = (JObject)await _apiRequest.HttpPostRequestFirstRecharge(reqModel, apiUrl, "SubmitFirstRecharge");

                                apiResponse = JsonConvert.DeserializeObject<RechargeResponseModel>(responseData.ToString());

                                if (apiResponse != null)
                                {
                                    if (apiResponse.isError == true && apiResponse.message.Contains("Invalid session token"))
                                    {
                                        model.sessionToken = jWETToken.GenerateJWETToken(model.subscriberNo, retailer_code, model.deviceId, loginProviderIdRet ?? "", model.userId, substractMinutes);

                                        responseData = (JObject)await _apiRequest.HttpPostRequestFirstRecharge(reqModel, apiUrl, "SubmitFirstRecharge");

                                        apiResponse = JsonConvert.DeserializeObject<RechargeResponseModel>(responseData.ToString());

                                        if (apiResponse != null)
                                        {
                                            if (apiResponse.isError == true && apiResponse.message.Contains("Invalid session token"))
                                            {
                                                model.sessionToken = jWETToken.GenerateJWETToken(model.subscriberNo, retailer_code, model.deviceId, loginProviderIdRet ?? "", model.userId, addMinutes);

                                                responseData = (JObject)await _apiRequest.HttpPostRequestFirstRecharge(reqModel, apiUrl, "SubmitFirstRecharge");

                                                apiResponse = JsonConvert.DeserializeObject<RechargeResponseModel>(responseData.ToString());

                                                if (apiResponse != null && apiResponse.isError == false)
                                                {
                                                    #region Update_Bi_Request_Raise_Complaint_Flag
                                                    await _frManager.UpdateOrderFirstRechargeStatus(model.bi_token_number);

                                                    #endregion
                                                    return Ok(new RechargeResponseModel()
                                                    {
                                                        isError = false,
                                                        message = apiResponse.message
                                                    });
                                                }
                                                else if (apiResponse != null && apiResponse.isError == true && apiResponse.message.Contains("Invalid session token"))
                                                {
                                                    return Ok(new RechargeResponseModel()
                                                    {
                                                        isError = true,
                                                        message = "Retailer App API: " + apiResponse.message
                                                    });
                                                }
                                            }
                                            if (apiResponse != null && apiResponse.isError == true)
                                            {
                                                return Ok(new RechargeResponseModel()
                                                {
                                                    isError = true,
                                                    message = apiResponse.message
                                                });
                                            }
                                            if (apiResponse != null && apiResponse.isError == false)
                                            {
                                                await _frManager.UpdateOrderFirstRechargeStatus(model.bi_token_number);
                                                return Ok(new RechargeResponseModel()
                                                {
                                                    isError = false,
                                                    message = apiResponse.message
                                                });
                                            }
                                        }
                                        if (apiResponse != null && apiResponse.isError == false)
                                        {
                                            #region Update_Bi_Request_Raise_Complaint_Flag
                                            await _frManager.UpdateOrderFirstRechargeStatus(model.bi_token_number);

                                            #endregion
                                            return Ok(new RechargeResponseModel()
                                            {
                                                isError = false,
                                                message = apiResponse.message
                                            });
                                        }
                                    }
                                    if (apiResponse != null && apiResponse.isError == true)
                                    {
                                        return Ok(new RechargeResponseModel()
                                        {
                                            isError = true,
                                            message = apiResponse != null ? apiResponse.message : " "
                                        });
                                    }
                                }
                                if (apiResponse != null && apiResponse.isError == false)
                                {
                                    #region Update_Bi_Request_Raise_Complaint_Flag
                                    await _frManager.UpdateOrderFirstRechargeStatus(model.bi_token_number);
                                    #endregion
                                    return Ok(new RechargeResponseModel()
                                    {
                                        isError = false,
                                        message = apiResponse.message
                                    });
                                }
                                else
                                {
                                    return Ok(new RechargeResponseModel()
                                    {
                                        isError = true,
                                        message = apiResponse?.message ?? "Invalid API response or casting issue"
                                    });
                                }

                            }
                            else if (fTRAir.responseCode == 102)
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = "Air responseCode: " + fTRAir.responseCode + ". Subscriber not found"
                                });

                            }
                            else if (fTRAir.responseCode == 136)
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = "Air responseCode: " + fTRAir.responseCode + ". Date adjustment error"
                                });

                            }
                            else if (fTRAir.responseCode == 104)
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = "Air responseCode: " + fTRAir.responseCode + ". Temporary blocked"
                                });

                            }
                            else if (fTRAir.responseCode == 165)
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = "Air responseCode: " + fTRAir.responseCode + ". Offer not found"
                                });

                            }
                            else if (fTRAir.responseCode == 260)
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = "Air responseCode: " + fTRAir.responseCode + ". Capability not available"
                                });

                            }
                            else if (fTRAir.responseCode == 247)
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = "Air responseCode: " + fTRAir.responseCode + ". Product not found"
                                });

                            }
                            else if (fTRAir.responseCode == 238)
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = "Air responseCode: " + fTRAir.responseCode + ". Not allowed to create a provider account offer without providing a Provider ID."
                                });

                            }
                            else if (fTRAir.responseCode == 237)
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = "Air responseCode: " + fTRAir.responseCode + ". Not allowed to add a Provider ID to another offer type than provider account offer."
                                });

                            }
                            else if (fTRAir.responseCode == 230)
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = "Air responseCode: " + fTRAir.responseCode + ". Not allowed to convert to other type of lifetime(1)"
                                });

                            }
                            else if (fTRAir.responseCode == 227)
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = "Air responseCode: " + fTRAir.responseCode + ". Invalid PAM Period Relative Dates Expiry PAM Period Indicator"
                                });

                            }
                            else if (fTRAir.responseCode == 225)
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = "Air responseCode: " + fTRAir.responseCode + ". The offer start date can not be changed because the offer is already active.(PC:08204)"
                                });

                            }
                            else if (fTRAir.responseCode == 224)
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = "Air responseCode: " + fTRAir.responseCode + ". The old offer date provided in the request did not match the current date.(PC:08204)"
                                });

                            }
                            else if (fTRAir.responseCode == 223)
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = "Air responseCode: " + fTRAir.responseCode + ". Service failed because new offer date provided in the request was incorrect.(PC:08204)"
                                });

                            }
                            else
                            {
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = true,
                                    message = fTRAir != null ? "Air responseCode: " + fTRAir.responseCode + ". " + fTRAir.Message : apiResponse.message
                                });
                            }
                        }
                        else
                        {
                            return Ok(new RechargeResponseModel()
                            {
                                isError = true,
                                message = "FTR Restriction response is not valid!"
                            });
                        }

                    }
                    else
                    {
                        return Ok(new RechargeResponseModel()
                        {
                            isError = true,
                            message = "You have not sufficient balance to recharge!"
                        });
                    }
                }
                else
                {
                    string retailer_code = string.Empty;
                    if (model.retailerCode != null && model.retailerCode.Substring(0, 1) != "R")
                    {
                        retailer_code = "R" + model.retailerCode;
                        model.userId = model.retailerCode;
                    }
                    else
                    {
                        retailer_code = model.retailerCode ?? "";
                        model.userId = model.retailerCode ?? "".Substring(0, 1);
                    }
                    if (String.IsNullOrEmpty(model.deviceId))
                    {
                        model.deviceId = "BL-Smartpos-app";
                    }

                    model.sessionToken = jWETToken.GenerateJWETToken(model.subscriberNo, retailer_code, model.deviceId, loginProviderIdRet ?? "", model.userId, 0);


                    reqModel = dBSSParse.RechargeReqPargeModel(model);                    

                    log.req_blob = _blJson.GetGenericJsonData(reqModel);

                    JObject responseData = (JObject)await _apiRequest.HttpPostRequestFirstRecharge(reqModel, apiUrl, "SubmitFirstRecharge");

                    apiResponse = JsonConvert.DeserializeObject<RechargeResponseModel>(responseData.ToString());


                    if (apiResponse != null)
                    {
                        if (apiResponse.isError == true && apiResponse.message.Contains("Invalid session token"))
                        {
                            model.sessionToken = jWETToken.GenerateJWETToken(model.subscriberNo, retailer_code, model.deviceId, loginProviderIdRet ?? "", model.userId, substractMinutes);

                            responseData = (JObject)await _apiRequest.HttpPostRequestFirstRecharge(reqModel, apiUrl, "SubmitFirstRecharge");

                            apiResponse = JsonConvert.DeserializeObject<RechargeResponseModel>(responseData.ToString());

                            if (apiResponse != null)
                            {
                                if (apiResponse.isError == true && apiResponse.message.Contains("Invalid session token"))
                                {
                                    model.sessionToken = jWETToken.GenerateJWETToken(model.subscriberNo, retailer_code, model.deviceId, loginProviderIdRet ?? "", model.userId, addMinutes);

                                    responseData = (JObject)await _apiRequest.HttpPostRequestFirstRecharge(reqModel, apiUrl, "SubmitFirstRecharge");

                                    apiResponse = JsonConvert.DeserializeObject<RechargeResponseModel>(responseData.ToString());

                                    if (apiResponse != null && apiResponse.isError == false)
                                    {
                                        #region Update_Bi_Request_Raise_Complaint_Flag
                                        await _frManager.UpdateOrderFirstRechargeStatus(model.bi_token_number);

                                        #endregion
                                        return Ok(new RechargeResponseModel()
                                        {
                                            isError = false,
                                            message = apiResponse.message
                                        });
                                    }
                                    else if (apiResponse != null && apiResponse.isError == true && apiResponse.message.Contains("Invalid session token"))
                                    {
                                        return Ok(new RechargeResponseModel()
                                        {
                                            isError = true,
                                            message = apiResponse.message
                                        });
                                    }
                                }
                                if (apiResponse != null && apiResponse.isError == true)
                                {
                                    return Ok(new RechargeResponseModel()
                                    {
                                        isError = true,
                                        message = apiResponse.message
                                    });
                                }
                                if (apiResponse != null && apiResponse.isError == false)
                                {
                                    await _frManager.UpdateOrderFirstRechargeStatus(model.bi_token_number);
                                    return Ok(new RechargeResponseModel()
                                    {
                                        isError = false,
                                        message = apiResponse.message
                                    });
                                }
                            }
                            if (apiResponse != null && apiResponse.isError == false)
                            {
                                #region Update_Bi_Request_Raise_Complaint_Flag
                                await _frManager.UpdateOrderFirstRechargeStatus(model.bi_token_number);

                                #endregion
                                return Ok(new RechargeResponseModel()
                                {
                                    isError = false,
                                    message = apiResponse.message
                                });
                            }
                        }
                        if (apiResponse != null && apiResponse.isError == true)
                        {
                            return Ok(new RechargeResponseModel()
                            {
                                isError = true,
                                message = apiResponse.message
                            });
                        }
                        if (apiResponse != null && apiResponse.isError == false)
                        {
                            #region Update_Bi_Request_Raise_Complaint_Flag
                            await _frManager.UpdateOrderFirstRechargeStatus(model.bi_token_number);

                            #endregion
                            return Ok(new RechargeResponseModel()
                            {
                                isError = false,
                                message = apiResponse.message
                            });
                        }
                        else
                        {
                            return Ok(new RechargeResponseModel()
                            {
                                isError = false,
                                message = apiResponse?.message ?? "Invalid API response or casting issue"
                            });
                        }
                    }
                    else
                    {
                        return Ok(new RechargeResponseModel()
                        {
                            isError = true,
                            message = "Invalid API Response (Retailer API)"
                        });
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
                log.res_time = DateTime.Now;
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.InnerException?.Message);

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    rechargeResponse.isError = true;

                    rechargeResponse.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                    return Ok(rechargeResponse);
                }
                catch (Exception ex2)
                {
                    rechargeResponse.isError = true;
                    rechargeResponse.message = ex.InnerException?.Message ?? ex2.Message;

                    return Ok(rechargeResponse);
                }
            }
            finally
            {
                log.res_time = DateTime.Now;
                log.message = rechargeResponse.message;
                log.msisdn = _bllLog.FormatMSISDN(model.subscriberNo);
                log.res_blob = _blJson.GetGenericJsonData(responseContent);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailerCode;
                log.method_name = "FirstRecharge";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [Route("SubmitFirstRechargeV2")]
        public async Task<IActionResult> SubmitFirstRechargeV2([FromBody][Bind("amount,bi_token_number,deviceId,lan,lat,lng,paymentType,retailerCode,session_token,subscriberNo,userId,userPin")] FirstRechargeRequestModel request)
        {
            string apiUrl = RetailerAPI.RechargeAPI;
            var log = new BIAToDBSSLog { req_time = DateTime.Now };
            var rechargeResponse = new RechargeResponseModel();
            string responseContent = string.Empty;

            try
            {
                log.req_blob = _blJson.GetGenericJsonData(request); 
                var populateModel = new CommonRequestPopulateModel();
                var model = populateModel.FirstRechargeRequestPopulateModel(request);

                // ✅ Token validation
                var tokenService = new TokenValidationService(SettingsValues.GetJWTSequrityKey());
                var security = tokenService.ValidateToken(model.session_token)
                    ?? throw new Exception("Token validation failed");

                if (!security.IsVallid || (model.userId != null && !model.userId.Equals(security.UserName)))
                    throw new Exception(SettingsValues.GetSessionMessage());

                string loginProviderIdRet = SettingsValues.GetJWETLoginProvider();
                string channelName = security.ChannelName;
                string userName = security.UserName;

                if (SettingsValues.GetisFtrFeatureOn() == 1)
                {
                    // ✅ Check balance
                    var balanceResp = await _ftr_restrict.CheckEVBalance(model.retailerCode, model.subscriberNo, model.userPin, model.bi_token_number, model.subscriberNo);
                    if (!IsValidBalance(balanceResp, out double balance))
                        return Ok(ErrorResponse(balanceResp.message));

                    if (balance < Convert.ToDouble(model.amount))
                        return Ok(ErrorResponse("You have not sufficient balance to recharge!"));

                    // ✅ Restriction API call

                    int isLUS = await _bLLCommon.GetLUSEligibleStatusfromBIA(model.bi_token_number);

                    if (isLUS > 0)
                    {
                        var ftrAir = await _ftr_restrict.FTRRestrictionRequsetToAIR(model, channelName, userName);
                        if (ftrAir == null)
                            return Ok(ErrorResponse("FTR Restriction response is not valid!"));

                        if (ftrAir.responseCode != 0)
                            return Ok(ErrorResponse(MapFtrError(ftrAir)));


                        var ftrAirLUS = await _ftr_restrict.FTRRestrictionRequsetToAIRLUS(model, channelName, userName);
                        if (ftrAirLUS == null)
                            return Ok(ErrorResponse("LUS Restriction response is not valid!"));

                        if (ftrAirLUS.responseCode != 0)
                            return Ok(ErrorResponse(MapFtrError(ftrAirLUS)));
                    }
                    else
                    {
                        var ftrAir = await _ftr_restrict.FTRRestrictionRequsetToAIR(model, channelName, userName);
                        if (ftrAir == null)
                            return Ok(ErrorResponse("FTR Restriction response is not valid!"));

                        if (ftrAir.responseCode != 0)
                            return Ok(ErrorResponse(MapFtrError(ftrAir)));
                    }

                    // ✅ Process Recharge
                    log.res_blob = _blJson.GetGenericJsonData(await ProcessRechargeAsync(model, loginProviderIdRet));
                    return await ProcessRechargeAsync(model, loginProviderIdRet);
                }
                else
                {
                    log.res_blob = _blJson.GetGenericJsonData(await ProcessRechargeAsync(model, loginProviderIdRet));
                    // ✅ Process Recharge directly
                    return await ProcessRechargeAsync(model, loginProviderIdRet);
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
                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                rechargeResponse.isError = true;
                rechargeResponse.message = string.IsNullOrEmpty(error.error_custom_msg)
                    ? error.error_description
                    : error.error_custom_msg;

                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = rechargeResponse.message;
                log.res_blob = _blJson.GetGenericJsonData(rechargeResponse);

                return Ok(rechargeResponse);
            }
            finally
            {
                log.res_time = DateTime.Now;
                log.message = rechargeResponse.message;
                log.msisdn = _bllLog.FormatMSISDN(request.subscriberNo);
                log.res_blob = _blJson.GetGenericJsonData(responseContent);
                log.integration_point_from = (decimal)IntegrationPoints.RA;
                log.integration_point_to = (decimal)IntegrationPoints.BSS;
                log.user_id = request.retailerCode;
                log.method_name = "FirstRecharge";
                log.bi_token_number= request.bi_token_number;

                //await _bllLog.RAToDBSSLog(log);
            }
        }

        #region 🔹 Helper Methods

        private bool IsValidBalance(RetailerBalanceRespModel balanceResp, out double balance)
        {
            balance = 0;
            if (balanceResp == null) return false;

            if (balanceResp.Balance == "0" || balanceResp.message?.Contains("Invalid") == true)
                return false;

            return double.TryParse(balanceResp.Balance, out balance);
        }

        private async Task<IActionResult> ProcessRechargeAsync(RechargeRequestModel model, string loginProviderIdRet)
        {
            var log = new BIAToDBSSLog();
            byte[] response = Array.Empty<byte>(); // More memory efficient than new byte[0]

            try
            {
                var jWETToken = new JWETToken();
                string retailerCode = NormalizeRetailerCode(model);
                if (string.IsNullOrEmpty(model.deviceId))
                    model.deviceId = "BL-Smartpos-app";
                model.sessionToken = jWETToken.GenerateJWETToken(
                    model.subscriberNo,
                    retailerCode,
                    model.deviceId,
                    loginProviderIdRet,
                    model.userId ?? "",
                    0);
                var reqModel = new BLLRAToDBSSParse().RechargeReqPargeModel(model);

                log.req_time = DateTime.Now;
                log.user_id = model.retailerCode;
                log.msisdn = model.subscriberNo;
                log.req_blob = _blJson.GetGenericJsonData(reqModel);
                // FIX 1: Retrieve the tuple using a single variable to avoid re-declaring 'response'
                var result = await ExecuteRechargeWithRetries(reqModel);
                var apiResponse = result.Response;
                response = result.Blob; // Assign directly to the outer 'response' byte[] variable
                log.res_time = DateTime.Now;

                // FIX 2: Assign directly because 'response' is already a byte[]
                log.res_blob = response;
                if (apiResponse.isError)
                    return Ok(ErrorResponse(apiResponse.message));
                await _frManager.UpdateOrderFirstRechargeStatus(model.bi_token_number);
                return Ok(SuccessResponse(apiResponse.message));
            }
            catch (Exception ex)
            {
                // FIX 3: Decode byte[] to string safely instead of calling .ToString()
                string responseText = response != null && response.Length > 0
                    ? Encoding.UTF8.GetString(response)
                    : string.Empty;
                string resp = $"{ex.Message} | Response Payload: {responseText}";
                log.res_blob = _blJson.GetGenericJsonData(resp);
                throw;
            }
            finally
            {
                // Defensive check: prevent crash in finally block if 'model' was null
                if (model != null)
                {
                    log.msisdn = _bllLog.FormatMSISDN(model.subscriberNo);
                    log.user_id = model.retailerCode;
                    log.bi_token_number = model.bi_token_number;
                }

                log.integration_point_from = (decimal)IntegrationPoints.RA;
                log.integration_point_to = (decimal)IntegrationPoints.BSS;
                log.method_name = "ProcessRecharge_R_API";
                
                await _bllLog.RAToDBSSLog(log);
            }
        }

        //private async Task<IActionResult> ProcessRechargeAsync(RechargeRequestModel model, string loginProviderIdRet)
        //{
        //    var log = new BIAToDBSSLog();
        //    byte[] response = new byte[0];
        //    try
        //    {
        //        var jWETToken = new JWETToken();
        //        string retailerCode = NormalizeRetailerCode(model);
        //        if (string.IsNullOrEmpty(model.deviceId)) model.deviceId = "BL-Smartpos-app";

        //        model.sessionToken = jWETToken.GenerateJWETToken(model.subscriberNo, retailerCode, model.deviceId, loginProviderIdRet, model.userId ?? "", 0);

        //        var reqModel = new BLLRAToDBSSParse().RechargeReqPargeModel(model);

        //        log.req_time = DateTime.Now;
        //        log.user_id = model.retailerCode;
        //        log.msisdn = model.subscriberNo;

        //        log.req_blob = _blJson.GetGenericJsonData(reqModel);

        //        var (apiResponse, response) = await ExecuteRechargeWithRetries(reqModel);

        //        log.res_time = DateTime.Now;

        //        log.res_blob = _blJson.GetGenericJsonData(response);


        //        if (apiResponse.isError) return Ok(ErrorResponse(apiResponse.message));

        //        await _frManager.UpdateOrderFirstRechargeStatus(model.bi_token_number);
        //        return Ok(SuccessResponse(apiResponse.message));
        //    }
        //    catch (Exception ex)
        //    {
        //        string resp = ex.Message + response.ToString();
        //        log.res_blob = _blJson.GetGenericJsonData(resp);
        //        throw;
        //    }
        //    finally
        //    {
        //        log.msisdn = _bllLog.FormatMSISDN(model.subscriberNo);                
        //        log.integration_point_from = (decimal)IntegrationPoints.RA;
        //        log.integration_point_to = (decimal)IntegrationPoints.BSS;
        //        log.user_id = model.retailerCode;
        //        log.method_name = "ProcessRecharge";

        //        await _bllLog.RAToDBSSLog(log);
        //    }            
        //}

        private string NormalizeRetailerCode(RechargeRequestModel model)
        {
            if (!string.IsNullOrEmpty(model.retailerCode) && !model.retailerCode.StartsWith("R"))
            {
                model.userId = model.retailerCode;
                return "R" + model.retailerCode;
            }
            model.userId = model.retailerCode?.Substring(0, 1) ?? string.Empty;
            return model.retailerCode ?? string.Empty;
        }

        private async Task<(RechargeResponseModel Response, byte[] Blob)> ExecuteRechargeWithRetries(RechargeReqModel reqModel)
        {
            JObject responseData = new JObject();
            byte[] response_blob = Array.Empty<byte>(); // More memory efficient than new byte[0]
            RechargeResponseModel apiResponse = new RechargeResponseModel();

            try
            {
                var jWETToken = new JWETToken();
                string apiUrl = RetailerAPI.RechargeAPI;
                string[] retryModes = { "0", "-1", "1" }; // original, substractMinutes, addMinutes                

                foreach (var mode in retryModes)
                {
                    // Perform post request
                    var result = await _apiRequest.HttpPostRequestFirstRecharge(reqModel, apiUrl, "SubmitFirstRechargeV2");
                    responseData = (JObject)result;

                    if (responseData != null)
                    {
                        response_blob = _blJson.GetGenericJsonData(responseData);
                    }

                    apiResponse = JsonConvert.DeserializeObject<RechargeResponseModel>(responseData?.ToString() ?? string.Empty);

                    // If success, return the Tuple
                    if (apiResponse != null && (!apiResponse.isError || !apiResponse.message.Contains("Invalid session token")))
                    {
                        return (apiResponse, response_blob);
                    }
                }

                // Return error model with final response_blob
                var errorResponse = new RechargeResponseModel
                {
                    isError = true,
                    message = "Invalid session token after retries"
                };
                return (errorResponse, response_blob);
            }
            catch (Exception ex)
            {
                // Safe string representation (prevents secondary NullReferenceException if responseData is null)
                string blob_res = ex.Message + (responseData?.ToString() ?? string.Empty);

                if (responseData != null)
                {
                    response_blob = _blJson.GetGenericJsonData(blob_res);
                }
                apiResponse = new RechargeResponseModel()
                {
                    isError = true,
                    message = ex.Message.ToString()
                };

                return (apiResponse, response_blob);
            }
        }

        private string MapFtrError(FTRAirResponseModel ftrAir)
        {
            var errorMap = new Dictionary<int, string>
    {
        { 102, "Subscriber not found" },
        { 136, "Date adjustment error" },
        { 104, "Temporary blocked" },
        { 165, "Offer not found" },
        { 260, "Capability not available" },
        { 247, "Product not found" },
        { 238, "Not allowed to create provider account offer without Provider ID" },
        { 237, "Not allowed to add Provider ID to another offer type" },
        { 230, "Not allowed to convert to other type of lifetime(1)" },
        { 227, "Invalid PAM Period Relative Dates Expiry Indicator" },
        { 225, "Offer start date cannot be changed (already active)" },
        { 224, "Old offer date mismatch" },
        { 223, "Incorrect new offer date" }
    };

            return errorMap.TryGetValue(ftrAir.responseCode, out var message)
                ? $"Air responseCode: {ftrAir.responseCode}. {message}"
                : $"Air responseCode: {ftrAir.responseCode}. {ftrAir.Message}";
        }

        private RechargeResponseModel ErrorResponse(string message) =>
            new RechargeResponseModel { isError = true, message = message };

        private RechargeResponseModel SuccessResponse(string message) =>
            new RechargeResponseModel { isError = false, message = message };

        #endregion


        [HttpPost]
        [Route("GetRechargeAmount")]
        public async Task<IActionResult> GetRechargeAmount([FromBody][Bind("channel_name,retailer_code,session_token")] FirstRechargeAmountRequestModel request)
        {
            RechargeAmountData amountData = new RechargeAmountData();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RechargeAmountReqModel model = new RechargeAmountReqModel();
            try
            {
                model = populateModel.FirstRechargeAmountRequestPopulateModel(request);

                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                string userName = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                        userName = security.UserName;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                amountData = await _bLLCommon.GetRechargeAmount(model, userName);

                return Ok(amountData);
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
                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                    });
                }
                catch (Exception ex2)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = ex.InnerException?.Message ?? ex2.Message
                    });
                }
            }
        }

        [HttpPost]
        [Route("GetRechargeAmountV2")]
        public async Task<IActionResult> GetRechargeAmountV2([FromBody][Bind("bi_token_number,channel_name,retailer_code,session_token")] RechargeAmountReqModelRev model)
        {
            RechargeAmountData amountData = new RechargeAmountData();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                string userName = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                        userName = security.UserName;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                amountData = await _bLLCommon.GetRechargeAmountV2(model, userName);

                return Ok(amountData);
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
                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                    });
                }
                catch (Exception)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = ex.Message
                    });
                }
            }
        }

        [HttpPost]
        [Route("GetRechargeAmountV3")]
        public async Task<IActionResult> GetRechargeAmountV3([FromBody][Bind("bi_token_number,channel_name,is_lus,retailer_code,rightId,session_token")] RechargeAmountReqModelRevV3 model)
        {
            RechargeAmountData amountData = new RechargeAmountData();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                string userName = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                        userName = security.UserName;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                amountData = await _bLLCommon.GetRechargeAmountV3(model, userName);

                return Ok(amountData);
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
                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                    });
                }
                catch (Exception)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = ex.Message
                    });
                }
            }
        }
    }
}
