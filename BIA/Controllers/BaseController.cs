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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Globalization;
using System.Net;
using System.Reflection;

namespace BIA.Controllers
{
    public class BaseController
    {
        private readonly BLLRAToDBSSParse _raToDBssParse;
        private readonly BLLDBSSToRAParse _dbssToRaParse;
        private readonly ApiRequest _apiReq;
        private readonly BLLCommon _bllCommon;
        private readonly BL_Json _blJson;
        private readonly BLLLog _bllLog;
        private readonly BiometricApiCall _apiCall;
        public BaseController(BLLRAToDBSSParse raToDBssParse, BLLDBSSToRAParse dbssToRaParse, ApiRequest apiReq, BLLCommon bllCommon, BL_Json blJson, BLLLog bllLog, BiometricApiCall apiCall)
        {
            _bllCommon = bllCommon;
            _raToDBssParse = raToDBssParse;
            _dbssToRaParse = dbssToRaParse;
            _apiReq = apiReq;
            _blJson = blJson;
            _bllLog = bllLog;
            _apiCall = apiCall;
        }

        #region  MSISDN validation Unpaired
        /// <summary>
        /// This method is used for MSISDN validation for unpaired
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        public async Task<RACommonResponse> ValidateUnpairedMSISDN(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponse raRespModel = new RACommonResponse();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }
                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);
                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDN");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = await _blJson.GetGenericJsonDataAsync(dbssResp);

                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.result = false;
                    raRespModel.message = "DBSS Error: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingV2(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.result = false;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.result = false;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }

                var simResp = await CheckSIMNumber3(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");


                if (simResp.result == false)
                {
                    raRespModel.result = false;
                    raRespModel.message = simResp.message;
                    return raRespModel;
                }
                raRespModel.result = true;
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                return raRespModel;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                raRespModel.result = false;
                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidateUnpairedMSISDN";

                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<RACommonResponse> ValidateUnpairedMSISDNV2(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponse raRespModel = new RACommonResponse();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNV2");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.result = false;
                    raRespModel.message = "DBSS Error: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingV2(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.result = false;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.result = false;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }


                var simResp = await CheckSIMNumber3(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");


                if (simResp.result == false)
                {
                    raRespModel.result = false;
                    raRespModel.message = simResp.message;
                    return raRespModel;
                }
                raRespModel.result = true;
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                return raRespModel;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raRespModel.result = false;
                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNV4(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            var raRespModel = new RACommonResponseRevamp();
            var log = new BIAToDBSSLog();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;

            try
            {
                // Ensure mobile number starts with country code
                if (!msisdnCheckReqest.mobile_number.StartsWith(FixedValueCollection.MSISDNCountryCode))
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);

                apiUrl = string.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);
                log.req_blob = await _blJson.GetGenericJsonDataAsync(apiUrl);
                log.req_time = DateTime.Now;

                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNV4");

                log.res_time = DateTime.Now;
                txtResp = dbssResp?.ToString();
                log.res_blob = await _blJson.GetGenericJsonDataAsync(dbssResp);

                if (dbssResp?["data"] == null)
                {
                    return PrepareError(log, raRespModel, "DBSS Error: " + MessageCollection.NoDataFound, 0);
                }

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingV2(dbssResp, msisdnCheckReqest.retailer_id);
                if (!msisdnResp.result)
                {
                    return PrepareError(log, raRespModel, msisdnResp.message);
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));
                if (!stockCheck)
                {
                    return PrepareError(log, raRespModel, MessageCollection.StockIDMismatch);
                }

                var simResp = await CheckSIMNumber3(new SIMNumberCheckRequest
                {
                    center_code = msisdnCheckReqest.center_code ?? string.Empty,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

                if (!simResp.result)
                {
                    return PrepareError(log, raRespModel, simResp.message);
                }

                raRespModel.isError = false;
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                return raRespModel;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                raRespModel.isError = true;
                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        private RACommonResponseRevamp PrepareError(BIAToDBSSLog log, RACommonResponseRevamp response, string message, int successFlag = 0)
        {
            log.is_success = successFlag;
            response.isError = true;
            response.message = message;
            return response;
        }
        //private async Task<RACommonResponseRevamp> HandleWebException(WebException ex, BIAToDBSSLog log, RACommonResponseRevamp response)
        //{
        //    log.res_time = DateTime.Now;
        //    log.is_success = 0;
        //    ErrorDescription error = new ErrorDescription();

        //    string responseText = ex.Response != null
        //        ? new StreamReader(ex.Response.GetResponseStream()).ReadToEnd()
        //        : null;

        //    string finalMessage = ex.Message;

        //    if (!string.IsNullOrEmpty(responseText))
        //    {
        //        try
        //        {
        //            JObject respObj = JsonConvert.DeserializeObject<JObject>(responseText);
        //            log.res_blob = _blJson.GetGenericJsonData(respObj);

        //            string errorMsg = respObj["errors"]?["title"]?.ToString() ?? ex.Message;
        //            error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

        //            finalMessage = isDBSSErrorOccurred(ex)
        //                ? FixedValueCollection.DBSSError + (error?.error_custom_msg ?? error?.error_description)
        //                : error?.error_custom_msg ?? error?.error_description;

        //            UpdateLogErrorDetails(log, error, finalMessage);
        //        }
        //        catch (Exception ex2)
        //        {
        //            finalMessage = ex2.Message;
        //        }
        //    }
        //    else
        //    {
        //        error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        finalMessage = error?.error_custom_msg ?? error?.error_description;
        //        log.res_blob = _blJson.GetGenericJsonData(finalMessage);
        //        UpdateLogErrorDetails(log, error, finalMessage);
        //    }

        //    response.isError = true;
        //    response.message = finalMessage;
        //    return response;
        //}

        //private async Task<RACommonResponseRevamp> HandleGeneralException(Exception ex, BIAToDBSSLog log, RACommonResponseRevamp response)
        //{
        //    log.res_time = DateTime.Now;
        //    log.is_success = 0;
        //    log.res_blob = _blJson.GetGenericJsonData(ex.Message);

        //    try
        //    {
        //        var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        log.res_blob = _blJson.GetGenericJsonData(error?.error_description);
        //        UpdateLogErrorDetails(log, error, error?.error_custom_msg ?? error?.error_description);
        //        response.isError = true;
        //        response.message = error?.error_custom_msg ?? error?.error_description;
        //    }
        //    catch
        //    {
        //        response.isError = true;
        //        response.message = ex.Message;
        //    }

        //    return response;
        //}

        //private void UpdateLogErrorDetails(BIAToDBSSLog log, ErrorDescription error, string message)
        //{
        //    log.error_code = error?.error_code ?? string.Empty;
        //    log.error_source = error?.error_source ?? string.Empty;
        //    log.message = message;
        //}


        //public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNV4(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        //{
        //    RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
        //    JObject dbssResp = null;
        //    string apiUrl = string.Empty, txtResp = string.Empty;
        //    BIAToDBSSLog log = new BIAToDBSSLog();

        //    try
        //    {
        //        if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
        //        {
        //            msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
        //        }

        //        apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, msisdnCheckReqest.mobile_number);

        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl);

        //        log.req_time = DateTime.Now;
        //        dbssResp = await _apiReq.HttpGetRequest(apiUrl);
        //        log.res_time = DateTime.Now;

        //        txtResp = Convert.ToString(dbssResp);

        //        log.res_blob = _blJson.GetGenericJsonData(dbssResp);


        //        if (dbssResp["data"] == null)
        //        {
        //            log.is_success = 0;
        //            raRespModel.isError = true;
        //            raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
        //            return raRespModel;
        //        }

        //        log.is_success = 1;

        //        var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingV2(dbssResp, msisdnCheckReqest.retailer_id);

        //        if (msisdnResp.result == false)
        //        {
        //            raRespModel.isError = true;
        //            raRespModel.message = msisdnResp.message;
        //            return raRespModel;
        //        }

        //        var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

        //        if (stockCheck == false)
        //        {
        //            raRespModel.isError = true;
        //            raRespModel.message = MessageCollection.StockIDMismatch;
        //            return raRespModel;
        //        }


        //        var simResp = await CheckSIMNumber3(new SIMNumberCheckRequest()
        //        {
        //            center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //            distributor_code = string.Empty,
        //            channel_name = msisdnCheckReqest.channel_name,
        //            session_token = msisdnCheckReqest.session_token,
        //            sim_number = msisdnCheckReqest.sim_number,
        //            retailer_id = msisdnCheckReqest.retailer_id,
        //            product_code = string.Empty,
        //            inventory_id = msisdnCheckReqest.inventory_id,
        //            msisdn = msisdnCheckReqest.mobile_number,
        //            purpose_number = msisdnCheckReqest.purpose_number
        //        }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");


        //        if (simResp.result == false)
        //        {
        //            raRespModel.isError = true;
        //            raRespModel.message = simResp.message;
        //            return raRespModel;
        //        }
        //        raRespModel.isError = false;
        //        raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
        //        return raRespModel;
        //    }
        //    catch (WebException ex)
        //    {
        //        log.res_time = DateTime.Now;
        //        ErrorDescription error = null;
        //        log.is_success = 0;

        //        if (isDBSS500ErrorOccurred(ex))
        //        {
        //            log.res_blob = _blJson.GetGenericJsonData(ex.Message);
        //            try
        //            {
        //                error = await _bllLog.ManageException(ex.Message, ex.HResult, "BIA");

        //                raRespModel.isError = true;
        //                if (isDBSSErrorOccurred(ex))
        //                {
        //                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : FixedValueCollection.DBSSError + error.error_custom_msg;
        //                }
        //                else
        //                {
        //                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //                }

        //                log.error_code = error.error_code ?? String.Empty;
        //                log.error_source = error.error_source ?? String.Empty;
        //                log.message = error.error_description ?? String.Empty;
        //                log.res_blob = _blJson.GetGenericJsonData(raRespModel.message);

        //                return raRespModel;
        //            }
        //            catch (Exception)
        //            {
        //                raRespModel.isError = true;
        //                raRespModel.message = ex.Message;

        //                log.res_blob = _blJson.GetGenericJsonData(raRespModel.message);

        //                return raRespModel;
        //            }
        //        }

        //        string resp = string.Empty;
        //        if (ex.Response != null)
        //            resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();

        //        if (!String.IsNullOrEmpty(resp))
        //        {
        //            log.res_blob = _blJson.GetGenericJsonData(resp);

        //            try
        //            {
        //                JObject respObj1 = (JObject)JsonConvert.DeserializeObject<Object>(resp);
        //                log.res_blob = _blJson.GetGenericJsonData(respObj1);

        //                error = await _bllLog.ManageException(respObj1?["errors"]?["title"] != null
        //                                            && respObj1?["errors"]?["title"]?.ToString() != "" ? respObj1?["errors"]?["title"]?.ToString() : ex.Message, ex.HResult, "BIA");

        //                raRespModel.isError = true;
        //                if (isDBSSErrorOccurred(ex))
        //                {
        //                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : FixedValueCollection.DBSSError + error.error_custom_msg;
        //                }
        //                else
        //                {
        //                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //                }

        //                log.error_code = error.error_code ?? String.Empty;
        //                log.error_source = error.error_source ?? String.Empty;
        //                log.message = error.error_description ?? String.Empty;

        //                return raRespModel;
        //            }
        //            catch (Exception ex2)
        //            {
        //                try
        //                {
        //                    error = await _bllLog.ManageException(ex2.Message, ex2.HResult, "BIA");

        //                    raRespModel.isError = true;
        //                    if (isDBSSErrorOccurred(ex))
        //                    {
        //                        raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : FixedValueCollection.DBSSError + error.error_custom_msg;
        //                    }
        //                    else
        //                    {
        //                        raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //                    }
        //                    log.error_code = error.error_code ?? String.Empty;
        //                    log.error_source = error.error_source ?? String.Empty;
        //                    log.message = error.error_description ?? String.Empty;

        //                    return raRespModel;
        //                }
        //                catch (Exception)
        //                {
        //                    raRespModel.isError = true;
        //                    raRespModel.message = ex.Message;

        //                    log.error_code = error != null ? error.error_code : String.Empty;
        //                    log.error_source = error != null ? error.error_source : String.Empty;
        //                    log.message = error != null ? error.error_description : String.Empty;

        //                    return raRespModel;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            try
        //            {
        //                error = await _bllLog.ManageException(ex.Message, ex.HResult, "BIA");

        //                raRespModel.isError = true;
        //                if (isDBSSErrorOccurred(ex))
        //                {
        //                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : FixedValueCollection.DBSSError + error.error_custom_msg;
        //                }
        //                else
        //                {
        //                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //                }
        //                log.error_code = error.error_code ?? String.Empty;
        //                log.error_source = error.error_source ?? String.Empty;
        //                log.message = error.error_description ?? String.Empty;
        //                log.res_blob = _blJson.GetGenericJsonData(raRespModel.message);

        //                return raRespModel;
        //            }
        //            catch (Exception)
        //            {
        //                raRespModel.isError = true;
        //                raRespModel.message = ex.Message;

        //                log.error_code = error != null ? error.error_code : String.Empty;
        //                log.error_source = error != null ? error.error_source : String.Empty;
        //                log.message = error != null ? error.error_description : String.Empty;
        //                log.res_blob = _blJson.GetGenericJsonData(raRespModel.message);

        //                return raRespModel;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        log.res_time = DateTime.Now;
        //        ErrorDescription error = null;
        //        log.is_success = 0;
        //        log.res_blob = _blJson.GetGenericJsonData(ex.Message);
        //        log.res_time = DateTime.Now;

        //        try
        //        {
        //            error = await _bllLog.ManageException(ex.Message, ex.HResult, "BIA");

        //            raRespModel.isError = true;

        //            raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


        //            log.error_code = error.error_code ?? String.Empty;
        //            log.error_source = error.error_source ?? String.Empty;
        //            log.message = error.error_description ?? String.Empty;

        //            return raRespModel;
        //        }
        //        catch (Exception)
        //        {
        //            raRespModel.isError = true;
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
        //        log.user_id = msisdnCheckReqest.retailer_id;
        //        log.method_name = apiName;

        //        //Thread logThread = new Thread(() => bllLog.RAToDBSSLog(log, apiUrl, txtResp));
        //        //logThread.Start();

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //}
        #endregion
        #region Cherish MSISDN check and validation Unpaired
        /// <summary>
        /// This method is used for MSISDN validation for unpaired
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        public async Task<RACommonResponse> ValidateUnpairedMSISDNV3(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponse raRespModel = new RACommonResponse();
            JObject dbssResp = new JObject();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNV3");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.result = false;
                    raRespModel.message = "DBSS Error: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsing(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.result = false;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.result = false;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }


                var simResp = await CheckSIMNumber4(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");


                if (simResp.result == false)
                {
                    raRespModel.result = false;
                    raRespModel.message = simResp.message;
                    return raRespModel;
                }
                raRespModel.result = true;
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                return raRespModel;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                raRespModel.result = false;
                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        /// <summary>
        /// This method is used for MSISDN validation for unpaired
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param> 
        /// <returns>Success/ Failure</returns>
        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNV5(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);
                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNV5");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "DBSS Error: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingV2(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }
                
                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));
                                
                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }


                var simResp = await CheckSIMNumber4(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
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
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raRespModel.isError = true;

                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidateUnpairedMSISDNV5";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNDuplicateDialESIM(unpairedMSISDNCheckReq msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);
                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNV5");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "DBSS Error: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingV2(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }

                var simResp = await CheckSIMNumberDuplicateDialESIM(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
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
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raRespModel.isError = true;

                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidateUnpairedMSISDNV5";

                await _bllLog.RAToDBSSLog(log);
            }
        }


        public async Task<RACommonResponseRevampV3> ValidateUnpairedMSISDNV6(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevampV3 raRespModel = new RACommonResponseRevampV3();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNV6");

                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = await _dbssToRaParse.UnpairedMSISDNReqParsingV3(dbssResp, msisdnCheckReqest.retailer_id, msisdnCheckReqest.channel_name);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    raRespModel.data = new DesiredCategoryData()
                    {
                        isDesiredCategory = msisdnResp.isDesiredCategory,
                        category = msisdnResp.category_name,
                        message = msisdnResp.data_message,
                    };
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    raRespModel.data = new DesiredCategoryData()
                    {
                        isDesiredCategory = msisdnResp.isDesiredCategory,
                        category = msisdnResp.category_name,
                        message = msisdnResp.data_message,
                    };
                    return raRespModel;
                }


                var simResp = await CheckSIMNumber3(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");


                if (simResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = simResp.message;
                    raRespModel.data = new DesiredCategoryData()
                    {
                        isDesiredCategory = msisdnResp.isDesiredCategory,
                        category = msisdnResp.category_name,
                        message = msisdnResp.data_message,
                    };
                    return raRespModel;
                }
                raRespModel.isError = false;
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                raRespModel.data = new DesiredCategoryData()
                {
                    isDesiredCategory = msisdnResp.isDesiredCategory,
                    category = msisdnResp.category_name,
                    message = msisdnResp.data_message,
                };
                return raRespModel;
            }
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                ErrorDescription error = new ErrorDescription();
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.Message);
                log.res_time = DateTime.Now;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    raRespModel.isError = true;

                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    return raRespModel;
                }
                catch (Exception)
                {
                    raRespModel.isError = true;
                    raRespModel.message = ex.Message;

                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }

        public async Task<RACommonResponseRevampV3> ValidateUnpairedMSISDNWithMapping(unpairedMSISDNCheckReq msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevampV3 raRespModel = new RACommonResponseRevampV3();
            JObject dbssResp = new JObject(); 
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNWithMapping");

                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = await _dbssToRaParse.UnpairedMSISDNReqParsingV3(dbssResp, msisdnCheckReqest.retailer_id, msisdnCheckReqest.channel_name);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    raRespModel.data = new DesiredCategoryData()
                    {
                        isDesiredCategory = msisdnResp.isDesiredCategory,
                        category = msisdnResp.category_name,
                        message = msisdnResp.data_message,
                    };
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    raRespModel.data = new DesiredCategoryData()
                    {
                        isDesiredCategory = msisdnResp.isDesiredCategory,
                        category = msisdnResp.category_name,
                        message = msisdnResp.data_message,
                    };
                    return raRespModel;
                }

                var simResp = await CheckSIMNumberDuplicateDial(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");


                if (simResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = simResp.message;
                    raRespModel.data = new DesiredCategoryData()
                    {
                        isDesiredCategory = msisdnResp.isDesiredCategory,
                        category = msisdnResp.category_name,
                        message = msisdnResp.data_message,
                    };
                    return raRespModel;
                }
                raRespModel.isError = false;
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                raRespModel.data = new DesiredCategoryData()
                {
                    isDesiredCategory = msisdnResp.isDesiredCategory,
                    category = msisdnResp.category_name,
                    message = msisdnResp.data_message,
                };
                return raRespModel;
            }
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                ErrorDescription error = new ErrorDescription();
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.Message);
                log.res_time = DateTime.Now;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    raRespModel.isError = true;

                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    return raRespModel;
                }
                catch (Exception)
                {
                    raRespModel.isError = true;
                    raRespModel.message = ex.Message;

                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }

        public async Task<RACommonResponseRevampV3> ValidateHomeWifiD2DWithMapping(ActivationCheckRequestModel msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevampV3 raRespModel = new RACommonResponseRevampV3();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateHomeWifiD2DWithMapping");

                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = await _dbssToRaParse.UnpairedHomeWifiMSISDNReqParsing(dbssResp, msisdnCheckReqest.retailer_id, msisdnCheckReqest.channel_name, msisdnCheckReqest.initiator_channel);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    raRespModel.data = new DesiredCategoryData()
                    {
                        isDesiredCategory = msisdnResp.isDesiredCategory,
                        category = msisdnResp.category_name,
                        message = msisdnResp.data_message,
                    };
                    return raRespModel;
                }

                //var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                //if (stockCheck == false)
                //{
                //    raRespModel.isError = true;
                //    raRespModel.message = MessageCollection.StockIDMismatch;
                //    raRespModel.data = new DesiredCategoryData()
                //    {
                //        isDesiredCategory = msisdnResp.isDesiredCategory,
                //        category = msisdnResp.category_name,
                //        message = msisdnResp.data_message,
                //    };
                //    return raRespModel;
                //}

                var simResp = await CheckSIMNumberHomeWifiD2D(new SIMNumberCheckRequest()
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
                    raRespModel.data = new DesiredCategoryData()
                    {
                        isDesiredCategory = msisdnResp.isDesiredCategory,
                        category = msisdnResp.category_name,
                        message = msisdnResp.data_message,
                    };
                    return raRespModel;
                }
                raRespModel.isError = false;
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                raRespModel.data = new DesiredCategoryData()
                {
                    isDesiredCategory = msisdnResp.isDesiredCategory,
                    category = msisdnResp.category_name,
                    message = msisdnResp.data_message,
                };
                return raRespModel;
            }
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                ErrorDescription error = new ErrorDescription();
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.Message);
                log.res_time = DateTime.Now;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    raRespModel.isError = true;

                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    return raRespModel;
                }
                catch (Exception)
                {
                    raRespModel.isError = true;
                    raRespModel.message = ex.Message;

                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }


        public async Task<RACommonResponse> CheckSIMNumberForCherish(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type, int channel_id)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = _blJson.GetGenericJsonData(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumberForCherish");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }
                raResp = _dbssToRaParse.SIMValidationParsingForCherish(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type, channel_id);
            }
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(ex.InnerException?.Message);

                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    raResp.result = false;
                    raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description
                                                                                    : error.error_custom_msg;
                }
                catch (Exception)
                {
                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    raResp.result = false;
                    raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description
                                                                                    : error.error_custom_msg;
                }
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumber3";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);


                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }

        public async Task<RACommonResponse> CheckCherishMSISDNParseForTos(BiomerticDataModel msisdnCheckReqest, string apiName)
        {
            RACommonResponse raRespModel = new RACommonResponse();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            string number_category = string.Empty;

            try
            {
                if (msisdnCheckReqest.msisdn.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.msisdn = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.msisdn;
                }
                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.msisdn);
                apiUrl = String.Format(GetAPICollection.CherishMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "CheckCherishMSISDNParseForTos");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingForTOS(dbssResp, msisdnCheckReqest.user_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.result = false;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }
                raRespModel.result = true;
                raRespModel.message = msisdnResp.message;

                return raRespModel;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raRespModel.result = false;

                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.msisdn);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number.ToString();
                log.user_id = msisdnCheckReqest.user_id;
                log.method_name = "CheckCherishMSISDNParseForTos";

                await _bllLog.RAToDBSSLog(log);
            }
        }
        #endregion
        #region Cherish MSISDN check and validation Paired
        /// <summary>
        /// This method is used for MSISDN validation for unpaired
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns> 
        public async Task<RACommonResponse> CheckCherishedNumber(PairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponse raRespModel = new RACommonResponse();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            string number_category = string.Empty;

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.CherishMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "CheckCherishedNumber");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.CherishMSISDNReqParsing(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.result = false;
                    raRespModel.message = msisdnResp.message;

                    return raRespModel;
                }
                raRespModel.result = true;
                raRespModel.message = msisdnResp.message;

                return raRespModel;

            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raRespModel.result = true;

                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "CheckCherishedNumber";

                await _bllLog.RAToDBSSLog(log);
            }
        }
        #endregion
        #region Validate-Corporate-MSISDN
        internal async Task<SIMReplacementMSISDNCheckResponse> ValidateCorporateMSISDN(CorporateMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            SIMReplacementMSISDNCheckResponse raRespModel = new SIMReplacementMSISDNCheckResponse();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }
                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);
                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingCustomerInfo, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                JObject dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateCorporateMSISDN");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null || dbssResp["included"] == null)
                {
                    return raRespModel = new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = "DBSS Error: " + MessageCollection.SIMReplNoDataFound
                    };
                }

                log.is_success = 1;

                CorporateSIMReplacementCheckResponseWithCustomerId msisdnResp = _dbssToRaParse.CorporateSIMReplacementMSISDNReqParsing2(dbssResp);

                if (msisdnResp.result == false)
                {
                    return new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = msisdnResp.message
                    };
                }

                SIMReplacementMSISDNCheckResponse customerResp = await GetCoordicatorCustomerInfo(msisdnResp.customer_id, msisdnCheckReqest.poc_msisdn_number, msisdnCheckReqest.purpose_number ?? "", msisdnCheckReqest.retailer_id);

                if (customerResp.result == false)
                {
                    return new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = customerResp.message
                    };
                }

                RACommonResponse simResp = await CheckSIMNumber3(new SIMNumberCheckRequest
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
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
                    return new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = simResp.message
                    };
                }

                return new SIMReplacementMSISDNCheckResponse()
                {
                    dbss_subscription_id = msisdnResp.dbss_subscription_id,
                    old_sim_number = msisdnResp.old_sim_number,
                    doc_id_number = "**********",//customerResp.doc_id_number masking for data leak issue,
                    dob = "**/**/****",//customerResp.dob masking for data leak issue,
                    result = true,
                    message = MessageCollection.MSISDNandSIMBothValid
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raRespModel.result = false;

                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        internal async Task<SIMReplacementMSISDNCheckResponseDataRev> ValidateCorporateMSISDNV3(CorporateMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            SIMReplacementMSISDNCheckResponseDataRev raRespModel = new SIMReplacementMSISDNCheckResponseDataRev();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);

                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingCustomerInfo, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                JObject dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateCorporateMSISDNV3");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null || dbssResp["included"] == null)
                {
                    return raRespModel = new SIMReplacementMSISDNCheckResponseDataRev()
                    {
                        isError = true,
                        message = "DBSS Error: " + MessageCollection.SIMReplNoDataFound
                    };
                }

                log.is_success = 1;

                CorporateSIMReplacementCheckResponseWithCustomerId msisdnResp = _dbssToRaParse.CorporateSIMReplacementMSISDNReqParsing2(dbssResp);

                if (msisdnResp.result == false)
                {
                    return new SIMReplacementMSISDNCheckResponseDataRev()
                    {
                        isError = true,
                        message = msisdnResp.message
                    };
                }

                SIMReplacementMSISDNCheckResponse customerResp = await GetCoordicatorCustomerInfo(msisdnResp.customer_id, msisdnCheckReqest.poc_msisdn_number, msisdnCheckReqest.purpose_number ?? "", msisdnCheckReqest.retailer_id);

                if (customerResp.result == false)
                {
                    return new SIMReplacementMSISDNCheckResponseDataRev()
                    {
                        isError = true,
                        message = customerResp.message
                    };
                }

                RACommonResponse simResp = await CheckSIMNumber3(new SIMNumberCheckRequest
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
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
                    return new SIMReplacementMSISDNCheckResponseDataRev()
                    {
                        isError = true,
                        message = simResp.message
                    };
                }

                raRespModel.data = new SIMReplacementMSISDNCheckResponseRev()
                {
                    dbss_subscription_id = msisdnResp.dbss_subscription_id,
                    old_sim_number = msisdnResp.old_sim_number,
                    doc_id_number = "**********",//customerResp.doc_id_number masking for data leak issue,
                    dob = "**/**/****",//customerResp.dob masking for data leak issue
                };

                return new SIMReplacementMSISDNCheckResponseDataRev()
                {
                    data = raRespModel.data,
                    isError = true,
                    message = MessageCollection.MSISDNandSIMBothValid
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raRespModel.isError = true;

                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        internal async Task<SIMReplacementMSISDNCheckResponse> ValidateCorporateMSISDNV1(CorporateMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            SIMReplacementMSISDNCheckResponse raRespModel = new SIMReplacementMSISDNCheckResponse();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);

                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingCustomerInfo, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                JObject dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateCorporateMSISDNV1");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null || dbssResp["included"] == null)
                {
                    return raRespModel = new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = "DBSS Error: " + MessageCollection.SIMReplNoDataFound
                    };
                }

                log.is_success = 1;

                CorporateSIMReplacementCheckResponseWithCustomerId msisdnResp = _dbssToRaParse.CorporateSIMReplacementMSISDNReqParsing2(dbssResp);

                if (msisdnResp.result == false)
                {
                    return new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = msisdnResp.message
                    };
                }

                SIMReplacementMSISDNCheckResponse customerResp = await GetCoordicatorCustomerInfo(msisdnResp.customer_id, msisdnCheckReqest.poc_msisdn_number, msisdnCheckReqest.purpose_number ?? "", msisdnCheckReqest.retailer_id);

                if (customerResp.result == false)
                {
                    return new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = customerResp.message
                    };
                }

                RACommonResponse simResp = await CheckSIMNumber3(new SIMNumberCheckRequest
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
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
                    return new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = simResp.message
                    };
                }

                return new SIMReplacementMSISDNCheckResponse()
                {
                    dbss_subscription_id = msisdnResp.dbss_subscription_id,
                    old_sim_number = msisdnResp.old_sim_number,
                    doc_id_number = customerResp.doc_id_number,
                    dob = customerResp.dob,
                    result = true,
                    message = MessageCollection.MSISDNandSIMBothValid
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raRespModel.result = true;

                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        internal async Task<SIMReplacementMSISDNCheckResponse> ValidateCorporateMSISDNV2(CorporateMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            SIMReplacementMSISDNCheckResponse raRespModel = new SIMReplacementMSISDNCheckResponse();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }
                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);
                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingCustomerInfo, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                JObject dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateCorporateMSISDNV2");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null || dbssResp["included"] == null)
                {
                    return raRespModel = new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = "DBSS Error: " + MessageCollection.SIMReplNoDataFound
                    };
                }

                log.is_success = 1;

                CorporateSIMReplacementCheckResponseWithCustomerId msisdnResp = _dbssToRaParse.CorporateSIMReplacementMSISDNReqParsing2(dbssResp);

                if (msisdnResp.result == false)
                {
                    return new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = msisdnResp.message
                    };
                }

                SIMReplacementMSISDNCheckResponse customerResp = await GetCoordicatorCustomerInfo(msisdnResp.customer_id, msisdnCheckReqest.poc_msisdn_number, msisdnCheckReqest.purpose_number ?? "", msisdnCheckReqest.retailer_id);

                if (customerResp.result == false)
                {
                    return new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = customerResp.message
                    };
                }

                RACommonResponse simResp = await CheckSIMNumber4(new SIMNumberCheckRequest
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
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
                    return new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = simResp.message
                    };
                }

                return new SIMReplacementMSISDNCheckResponse()
                {
                    dbss_subscription_id = msisdnResp.dbss_subscription_id,
                    old_sim_number = msisdnResp.old_sim_number,
                    doc_id_number = customerResp.doc_id_number,
                    //doc_id_number = "**********",
                    dob = customerResp.dob,
                    //dob = "**/**/****",
                    result = true,
                    message = MessageCollection.MSISDNandSIMBothValid
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;

                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;
                throw;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        #endregion
        #region Get-Coordicator-Customer-Info
        public async Task<SIMReplacementMSISDNCheckResponse> GetCoordicatorCustomerInfo(string customerId, string pocMsisdnNo, string purposeNumber, string username)
        {
            SIMReplacementMSISDNCheckResponse raRespModel = new SIMReplacementMSISDNCheckResponse();
            string apiUrl = "";
            string? txtResp = null;
            BIAToDBSSLog log = new BIAToDBSSLog();
            object? dbssResp = null;
            try
            {
                apiUrl = String.Format(GetAPICollection.GetCustomerInfoById, customerId);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetCoordicatorCustomerInfo");
                log.res_time = DateTime.Now;

                // Ensure response is string before deserialization
                string? jsonResponse = dbssResp as string;
                if (string.IsNullOrEmpty(jsonResponse) && dbssResp != null)
                {
                    jsonResponse = JsonConvert.SerializeObject(dbssResp);
                }

                txtResp = jsonResponse;

                CorporateSIMReplacemnetCustomerInfoRootobject? dbssRespModel = null;
                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    dbssRespModel = JsonConvert.DeserializeObject<CorporateSIMReplacemnetCustomerInfoRootobject>(jsonResponse);
                }

                if (dbssRespModel != null)
                {
                    log.is_success = 1;
                    raRespModel = _dbssToRaParse.CorporateSIMReplacementCustomerInfoReqParsing(dbssRespModel, pocMsisdnNo);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;

                log.is_success = 0;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_description ?? string.Empty;
            }
            finally
            {
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = purposeNumber;
                log.user_id = username;
                log.method_name = nameof(GetCoordicatorCustomerInfo);

                await _bllLog.RAToDBSSLog(log);
            }
            return raRespModel;
        }
        //public async Task<SIMReplacementMSISDNCheckResponse> GetCoordicatorCustomerInfo(string customerId, string pocMsisdnNo, string purposeNumber, string username)
        //{
        //    SIMReplacementMSISDNCheckResponse raRespModel = new SIMReplacementMSISDNCheckResponse();
        //    string apiUrl = "";
        //    string? txtResp = "";
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    object dbssResp = new object();
        //    try
        //    {
        //        apiUrl = String.Format(GetAPICollection.GetCustomerInfoById, customerId);

        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl);
        //        log.req_time = DateTime.Now;

        //        dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetCoordicatorCustomerInfo");
        //        log.res_time = DateTime.Now;
        //        txtResp = Convert.ToString(dbssResp);
        //        CorporateSIMReplacemnetCustomerInfoRootobject? dbssRespModel = JsonConvert.DeserializeObject<CorporateSIMReplacemnetCustomerInfoRootobject>(dbssResp.ToString());

        //        if (dbssRespModel != null)
        //        {
        //            log.is_success = 1;
        //            raRespModel = _dbssToRaParse.CorporateSIMReplacementCustomerInfoReqParsing(dbssRespModel, pocMsisdnNo);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        log.res_time = DateTime.Now;

        //        log.is_success = 0;
        //        ErrorDescription error = new ErrorDescription();
        //        error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        log.res_blob = _blJson.GetGenericJsonData(error);
        //        log.error_code = error.error_code ?? String.Empty;
        //        log.error_source = error.error_source ?? String.Empty;
        //        log.message = error.error_description ?? String.Empty;
        //    }
        //    finally
        //    {
        //        log.res_blob = _blJson.GetGenericJsonData(dbssResp);
        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

        //        log.purpose_number = purposeNumber;
        //        log.user_id = username;
        //        log.method_name = "GetCoordicatorCustomerInfo";

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //    return raRespModel;
        //}
        #endregion
        #region SIM Number validation v2
        /// <summary>
        /// This method is used for SIM Number validation
        /// </summary>
        /// <param name="simNumberCheckReqest"></param>
        /// <returns>Success/ Failure</returns>
        protected async Task<RACommonResponse> CheckSIMNumber2(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumber2");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = await _blJson.GetGenericJsonDataAsync(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }
                raResp = _dbssToRaParse.SIMValidationParsing2(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumber2";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }
        /// <summary>
        /// This method is used for SIM Number validation
        /// </summary>
        /// <param name="simNumberCheckReqest"></param>
        /// <returns>Success/ Failure</returns>
        public async Task<RACommonResponse> CheckSIMNumber3(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumber3");

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = await _blJson.GetGenericJsonDataAsync(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }
                raResp = _dbssToRaParse.SIMValidationParsing3(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;

                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumber3";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }

        public async Task<RACommonResponse> CheckSIMNumberForReplacement(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumber3");

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = await _blJson.GetGenericJsonDataAsync(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }
                raResp = _dbssToRaParse.SIMValidationHomeWifiParsing(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type, simNumberCheckReqest.sim_type, simNumberCheckReqest.storage_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;

                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumberForReplacement";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }

        public async Task<RACommonResponse> CheckSIMNumberMNPPortIn(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumber3");

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = await _blJson.GetGenericJsonDataAsync(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }
                raResp = _dbssToRaParse.SIMValidationParsingMNPPortIn(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;

                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumberMNPPortIn";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }


        public async Task<RACommonResponse> CheckSIMNumberDuplicateDial(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumberDuplicateDial");

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = await _blJson.GetGenericJsonDataAsync(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }
                raResp = _dbssToRaParse.DuplicateDialSIMValidationParsing(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;

                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumber3";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }

        public async Task<RACommonResponse> CheckSIMNumberHomeWifiD2D(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type,string sim_type, string storage_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumberDuplicateDial");

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = await _blJson.GetGenericJsonDataAsync(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }
                raResp = _dbssToRaParse.DuplicateDialHomeWifiSIMValidationParsing(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type, sim_type, storage_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;

                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumber3";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }


        /// <summary>
        /// This method is used for SIM Number validation
        /// </summary>
        /// <param name="simNumberCheckReqest"></param>
        /// <returns>Success/ Failure</returns>
        public async Task<RACommonResponse> CheckSIMNumber4(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = _blJson.GetGenericJsonData(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumber4");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }

                raResp = _dbssToRaParse.SIMValidationParsing4(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumber4";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }

        public async Task<RACommonResponse> CheckSIMNumberDuplicateDialESIM(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = _blJson.GetGenericJsonData(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumberDuplicateDialESIM");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }

                raResp = _dbssToRaParse.SIMValidationParsingDuplicateDialESIM(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumber4";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }

        public async Task<RACommonResponse> CheckSIMNumberMNPESIM(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = _blJson.GetGenericJsonData(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumberMNPESIM");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }

                raResp = _dbssToRaParse.SIMValidationParsingMNPESIM(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumber4";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }


        #endregion
        #region Order validation
        private bool orderValidation(RAOrderRequest model)
        {
            switch (Convert.ToInt32(model.purpose_number))
            {
                case (int)EnumPurposeNumber.NewRegistration:
                    break;

                case (int)EnumPurposeNumber.SIMReplacement:

                    break;

                case (int)EnumPurposeNumber.MNPRegistration:

                    break;

                case (int)EnumPurposeNumber.MNPEmergencyReturn:

                    break;

                case (int)EnumPurposeNumber.MNPDeRegistration:

                    break;


                case (int)EnumPurposeNumber.IndividualToCorporateTransfer:

                    break;


                case (int)EnumPurposeNumber.CorporateToIndividualTransfer:

                    break;


                case (int)EnumPurposeNumber.SIMTransfer:

                    break;


                default:

                    break;
            }
            return false;
        }
        #endregion
        #region get old SIM
        //internal async Task<OldSIMNnumberResponse> GetOldSIMumber(string sIMCardsApiUrl, string username, string purposeNo)
        //{
        //    OldSIMNnumberResponse osnResp = new OldSIMNnumberResponse();
        //    string apiUrl = "", txtResp = "";
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    try
        //    {
        //        apiUrl = AppSettingsWrapper.ApiBaseUrl + apiUrl;
        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl);
        //        log.req_time = DateTime.Now;

        //        object dbssResp = _apiReq.HttpGetRequest(apiUrl, "GetOldSIMumber");
        //        txtResp = JsonConvert.SerializeObject(dbssResp);
        //        log.res_blob = _blJson.GetGenericJsonData(dbssResp);

        //        var dbssRespModel = JsonConvert.DeserializeObject<SIMNumberParsingRootobject>(dbssResp.ToString());
        //        log.res_time = DateTime.Now;
        //        log.is_success = 1;

        //        if (dbssRespModel == null)
        //        {
        //            osnResp.result = false;
        //            osnResp.message = MessageCollection.NoDataFound;
        //            return osnResp;
        //        }
        //        osnResp = _dbssToRaParse.OldSIMNumberParsing(dbssRespModel);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        log.res_time = DateTime.Now;
        //        log.res_blob = _blJson.GetGenericJsonData(ex.InnerException.Message);

        //        log.is_success = 0;
        //        ErrorDescription error = new ErrorDescription();
        //        try
        //        {
        //            error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

        //            log.error_code = error.error_code ?? String.Empty;
        //            log.error_source = error.error_source ?? String.Empty;
        //            log.message = error.error_description ?? String.Empty;
        //        }
        //        catch (Exception)
        //        {
        //            error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //            log.is_success = 0;
        //            log.error_code = error.error_code ?? String.Empty;
        //            log.error_source = error.error_source ?? String.Empty;
        //            log.message = error.error_description ?? String.Empty;
        //        }

        //        osnResp.result = false;
        //        osnResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //    }

        //    finally
        //    {

        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

        //        log.purpose_number = purposeNo;
        //        log.user_id = username;
        //        log.method_name = "GetOldSIMumber";

        //        //Thread logThread = new Thread(() => _bllLog.RAToDBSSLog(log, apiUrl, txtResp));
        //        //logThread.Start();

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //    return osnResp;

        //}
        #endregion
        #region Submit Order Request Binding For Log
        //internal object SubmitOrderRequestBindingForLog(RAOrderRequest model)
        //{
        //    return new
        //    {
        //        bi_token_number = model.bi_token_number,
        //        purpose_number = model.purpose_number,
        //        msisdn = model.msisdn,
        //        sim_category = model.sim_category,
        //        sim_number = model.sim_number,
        //        subscription_type_id = model.subscription_type_id,
        //        subscription_code = model.subscription_code,
        //        package_id = model.sim_category,
        //        package_code = model.package_code,
        //        dest_doc_type_no = model.dest_nid,
        //        src_nid = model.src_nid,
        //        dest_dob = model.dest_dob,
        //        src_doc_type_no = model.src_doc_type_no,
        //        src_dob = model.src_dob,
        //        platform_id = model.package_id,
        //        customer_name = model.customer_name,
        //        gender = model.gender,
        //        flat_number = model.flat_number,
        //        house_number = model.house_number,
        //        road_number = model.road_number,
        //        village = model.village,
        //        division_id = model.division_id,
        //        district_id = model.district_id,
        //        thana_id = model.thana_id,
        //        postal_code = model.postal_code,
        //        email = model.email,
        //        retailer_code = model.retailer_id,
        //        retailer_id = model.retailer_id,
        //        port_in_date = model.port_in_date,
        //        alt_msisdn = model.alt_msisdn,
        //        poc_number = model.poc_msisdn_number,
        //        is_urgent = model.is_urgent,
        //        optional1 = model.optional1,
        //        optional2 = model.optional2,
        //        optional3 = model.optional3,
        //        optional4 = model.optional4,
        //        optional5 = model.optional5,
        //        optional6 = model.optional6,
        //        note = model.note,
        //        sim_rep_reason_id = model.sim_rep_reason_id,
        //        payment_type = model.payment_type,
        //        is_paired = model.is_paired,
        //        cahnnel_id = model.channel_id,
        //        division_name = model.division_name,
        //        district_name = model.district_name,
        //        thana_name = model.thana_name,
        //        center_code = model.center_code,
        //        distributor_code = model.distributor_code,
        //        sim_replc_reason = model.sim_replc_reason,
        //        channel_name = model.channel_name,
        //        right_id = model.right_id,
        //        sim_replacement_type = model.sim_replacement_type,
        //        old_sim_number = model.old_sim_number,
        //        src_sim_category = model.src_sim_category,
        //        port_in_confirmation_code = model.port_in_confirmation_code,
        //        dest_ec_verifi_reqrd = model.dest_ec_verifi_reqrd,
        //        src_ec_verifi_reqrd = model.src_ec_verifi_reqrd,
        //        dest_foreign_flag = model.dest_foreign_flag,
        //        dbss_subscription_id = model.dbss_subscription_id,
        //        saf_status = model.saf_status,
        //        customer_id = model.customer_id
        //    };
        //}
        internal object SubmitOrderRequestBindingForLogV2(RAOrderRequestV2 model)
        {
            return new
            {
                bi_token_number = model.bi_token_number,
                purpose_number = model.purpose_number,
                msisdn = model.msisdn,
                sim_category = model.sim_category,
                sim_number = model.sim_number,
                subscription_type_id = model.subscription_type_id,
                subscription_code = model.subscription_code,
                package_id = model.sim_category,
                package_code = model.package_code,
                dest_doc_type_no = model.dest_nid,
                src_nid = model.src_nid,
                dest_dob = model.dest_dob,
                src_doc_type_no = model.src_doc_type_no,
                src_dob = model.src_dob,
                platform_id = model.package_id,
                customer_name = model.customer_name,
                gender = model.gender,
                flat_number = model.flat_number,
                house_number = model.house_number,
                road_number = model.road_number,
                village = model.village,
                division_id = model.division_id,
                district_id = model.district_id,
                thana_id = model.thana_id,
                postal_code = model.postal_code,
                email = model.email,
                retailer_code = model.retailer_id,
                retailer_id = model.retailer_id,
                port_in_date = model.port_in_date,
                alt_msisdn = model.alt_msisdn,
                poc_number = model.poc_msisdn_number,
                is_urgent = model.is_urgent,
                optional1 = model.optional1,
                optional2 = model.optional2,
                optional3 = model.optional3,
                optional4 = model.optional4,
                optional5 = model.optional5,
                optional6 = model.optional6,
                note = model.note,
                sim_rep_reason_id = model.sim_rep_reason_id,
                payment_type = model.payment_type,
                is_paired = model.is_paired,
                cahnnel_id = model.channel_id,
                division_name = model.division_name,
                district_name = model.district_name,
                thana_name = model.thana_name,
                center_code = model.center_code,
                distributor_code = model.distributor_code,
                sim_replc_reason = model.sim_replc_reason,
                channel_name = model.channel_name,
                right_id = model.right_id,
                sim_replacement_type = model.sim_replacement_type,
                old_sim_number = model.old_sim_number,
                src_sim_category = model.src_sim_category,
                port_in_confirmation_code = model.port_in_confirmation_code,
                dest_ec_verifi_reqrd = model.dest_ec_verifi_reqrd,
                src_ec_verifi_reqrd = model.src_ec_verifi_reqrd,
                dest_foreign_flag = model.dest_foreign_flag,
                dbss_subscription_id = model.dbss_subscription_id,
                saf_status = model.saf_status,
                customer_id = model.customer_id,
                lac = model.lac,
                cid = model.cid,
                latitude = model.latitude,
                longitude = model.longitude
            };
        }
        #endregion
        #region Is DBSS Error 
        //internal bool isDBSSErrorOccurred(WebException exception)
        //{
        //    try
        //    {
        //        var error = exception.Response as HttpWebResponse;
        //        if (error != null)
        //        {
        //            return error.StatusCode == HttpStatusCode.BadRequest ? false : true;
        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
        #endregion
        #region Is DBSS 500 Error (internal server error)
        internal bool isDBSS500ErrorOccurred(WebException exception)
        {
            try
            {
                var error = exception.Response as HttpWebResponse;
                if (error != null)
                    return error.StatusCode == HttpStatusCode.InternalServerError ? true : false;

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
        #region Validate-OTP
        public async Task<OTPResponse> ValidateOTP(DBSSOTPValidationRequest model, string username)
        {
            OTPResponse otpResp = new OTPResponse();
            string apiUrl = string.Empty;
            string txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();

            try
            {
                // Build the API URL
                apiUrl = string.Format(PatchAPICollection.VerifyOTP, model.otp);

                // Prepare request model
                DBSSOTPValidationRequestRootobject validateOTPReqModel = _raToDBssParse.DBSSOTPValidationReqParsing(model);

                // Log request
                log.req_blob = _blJson.GetGenericJsonData(apiUrl + JsonConvert.SerializeObject(validateOTPReqModel));
                log.req_time = DateTime.Now;

                // Call PATCH API
                var dbssRespObject = await _apiReq.HttpPatchRequest(validateOTPReqModel, apiUrl, "ValidateOTP");

                // Safely cast or throw
                string jsonResp = dbssRespObject as string ?? throw new InvalidOperationException("Invalid response from API");

                // Log response
                txtResp = jsonResp;
                log.res_blob = _blJson.GetGenericJsonData(txtResp);
                log.res_time = DateTime.Now;

                // Deserialize response
                var dbssRespModel = JsonConvert.DeserializeObject<DBSSOTPResponseRootobject>(jsonResp);

                // Check for waiting state in raw response (if needed)
                if (txtResp.Contains("\"Status\":\"WaitingForActivation\"", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("DBSS Error: WaitingForActivation");
                }

                // Check for null or empty data
                if (dbssRespModel?.data == null)
                {
                    log.is_success = 1;
                    return new OTPResponse
                    {
                        is_otp_valid = false,
                        result = false,
                        message = MessageCollection.InvalidOTP
                    };
                }

                // Parse and return success response
                log.is_success = 1;
                otpResp = _dbssToRaParse.OTPRespParsing(dbssRespModel);
                return otpResp;
            }
            catch (Exception ex)
            {
                // Log exception
                Log.Error(ex, "ExMessage");
                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                log.is_success = 0;
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_custom_msg ?? error.error_description ?? "Unknown error";

                // Set error response
                otpResp.is_otp_valid = false;
                otpResp.result = false;
                otpResp.message = log.message;
                return otpResp;
            }
            finally
            {
                // Final logging
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = Convert.ToString(model.purpose);
                log.user_id = username;
                log.method_name = "ValidateOTP";
                log.msisdn = _bllLog.FormatMSISDN(model.auth_msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<OTPResponseRev> ValidateOTPV2(DBSSOTPValidationRequest model, string username)
        {
            var otpResp = new OTPResponseRev();
            string apiUrl = string.Empty;
            string txtResp = string.Empty;
            var log = new BIAToDBSSLog();

            try
            {
                apiUrl = string.Format(PatchAPICollection.VerifyOTP, model.otp);
                var validateOTPReqModel = _raToDBssParse.DBSSOTPValidationReqParsing(model);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl + JsonConvert.SerializeObject(validateOTPReqModel));
                log.req_time = DateTime.Now;

                var dbssResp = await _apiReq.HttpPatchRequest(validateOTPReqModel, apiUrl, "ValidateOTPV2");

                txtResp = dbssResp?.ToString() ?? string.Empty;
                log.res_blob = _blJson.GetGenericJsonData(txtResp);
                log.res_time = DateTime.Now;

                if (txtResp.Contains("\"Status\":\"WaitingForActivation\"", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("DBSS Error: WaitingForActivation");
                }

                var dbssRespModel = JsonConvert.DeserializeObject<DBSSOTPResponseRootobject>(txtResp);

                if (dbssRespModel?.data == null)
                {
                    log.is_success = 1;
                    return new OTPResponseRev
                    {
                        isError = true,
                        message = MessageCollection.InvalidOTP,
                        data = new OTPRespData
                        {
                            is_otp_valid = false
                        }
                    };
                }

                log.is_success = 1;
                otpResp = _dbssToRaParse.OTPRespParsingV2(dbssRespModel);
                return otpResp;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                log.is_success = 0;
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description;

                return new OTPResponseRev
                {
                    isError = true,
                    message = log.message,
                    data = new OTPRespData
                    {
                        is_otp_valid = false
                    }
                };
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = Convert.ToString(model.purpose);
                log.user_id = username;
                log.method_name = "ValidateOTPV2";
                log.msisdn = _bllLog.FormatMSISDN(model.auth_msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="customerId"></param>
        /// <returns></returns>
        //public async Task<OTPResponse> ValidateOTP(DBSSOTPValidationRequest model, string username)
        //{
        //    OTPResponse OtpResp = new OTPResponse();
        //    string apiUrl = "";
        //    string? txtResp = "";
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    try
        //    {
        //        apiUrl = String.Format(PatchAPICollection.VerifyOTP, model.otp);
        //        DBSSOTPValidationRequestRootobject vaidateOTPReqModel = _raToDBssParse.DBSSOTPValidationReqParsing(model);

        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl + JsonConvert.SerializeObject(vaidateOTPReqModel));
        //        log.req_time = DateTime.Now;

        //        object dbssResp = await _apiReq.HttpPatchRequest(vaidateOTPReqModel, apiUrl, "ValidateOTP");

        //        txtResp = Convert.ToString(dbssResp);
        //        log.res_blob = _blJson.GetGenericJsonData(txtResp);
        //        log.res_time = DateTime.Now;
        //        System.Reflection.PropertyInfo pi = dbssResp.GetType().GetProperty("Status");
        //        String name = (String)(pi?.GetValue(dbssResp, null));

        //        if (name == "WaitingForActivation")
        //            throw new Exception("DBSS Error: " + name);

        //        var dbssRespModel = JsonConvert.DeserializeObject<DBSSOTPResponseRootobject>(dbssResp.ToString());

        //        if (dbssRespModel != null && dbssRespModel.data == null)
        //        {
        //            log.is_success = 1;
        //            return new OTPResponse()
        //            {
        //                is_otp_valid = false,
        //                result = false,
        //                message = MessageCollection.InvalidOTP
        //            };
        //        }

        //        log.is_success = 1;
        //        OtpResp = _dbssToRaParse.OTPRespParsing(dbssRespModel);
        //        return OtpResp;
        //    }            
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        log.res_blob = _blJson.GetGenericJsonData(error);
        //        log.res_time = DateTime.Now;
        //        log.is_success = 0;
        //        log.error_code = error.error_code ?? String.Empty;
        //        log.error_source = error.error_source ?? String.Empty;
        //        log.message = error.error_custom_msg ?? String.Empty;

        //        OtpResp.is_otp_valid = false;
        //        OtpResp.result = false;
        //        OtpResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //        return OtpResp;
        //    }
        //    finally
        //    {
        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
        //        log.purpose_number = Convert.ToString(model.purpose);
        //        log.user_id = username;
        //        log.method_name = "ValidateOTP";
        //        log.msisdn = _bllLog.FormatMSISDN(model.auth_msisdn);

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //}

        //public async Task<OTPResponseRev> ValidateOTPV2(DBSSOTPValidationRequest model, string username)
        //{
        //    OTPResponseRev OtpResp = new OTPResponseRev();
        //    string apiUrl = "", txtResp = "";
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    try
        //    {
        //        apiUrl = String.Format(PatchAPICollection.VerifyOTP, model.otp);
        //        DBSSOTPValidationRequestRootobject vaidateOTPReqModel = _raToDBssParse.DBSSOTPValidationReqParsing(model);

        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl + JsonConvert.SerializeObject(vaidateOTPReqModel));
        //        log.req_time = DateTime.Now;

        //        object dbssResp = await _apiReq.HttpPatchRequest(vaidateOTPReqModel, apiUrl, "ValidateOTPV2");

        //        txtResp = Convert.ToString(dbssResp);
        //        log.res_blob = _blJson.GetGenericJsonData(txtResp);
        //        log.res_time = DateTime.Now;
        //        System.Reflection.PropertyInfo pi = dbssResp.GetType().GetProperty("Status");
        //        String name = (String)(pi?.GetValue(dbssResp, null));

        //        if (name == "WaitingForActivation")
        //            throw new Exception("DBSS Error: " + name);

        //        var dbssRespModel = JsonConvert.DeserializeObject<DBSSOTPResponseRootobject>(dbssResp.ToString());

        //        if (dbssRespModel != null && dbssRespModel.data == null)
        //        {
        //            log.is_success = 1;
        //            return new OTPResponseRev()
        //            {
        //                isError = true,
        //                message = MessageCollection.InvalidOTP,
        //                data = new OTPRespData()
        //                {
        //                    is_otp_valid = false
        //                }
        //            };
        //        }

        //        log.is_success = 1;
        //        OtpResp = _dbssToRaParse.OTPRespParsingV2(dbssRespModel);
        //        return OtpResp;
        //    }            
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        log.res_blob = _blJson.GetGenericJsonData(error);
        //        log.res_time = DateTime.Now;

        //        log.is_success = 0;
        //        log.error_code = error.error_code ?? String.Empty;
        //        log.error_source = error.error_source ?? String.Empty;
        //        log.message = error.error_description ?? String.Empty;

        //        OtpResp.data = new OTPRespData()
        //        {
        //            is_otp_valid = false
        //        };
        //        OtpResp.isError = true;
        //        OtpResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //        return OtpResp;
        //    }
        //    finally
        //    {
        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
        //        log.purpose_number = Convert.ToString(model.purpose);
        //        log.user_id = username;
        //        log.method_name = "ValidateOTPV2";
        //        log.msisdn = _bllLog.FormatMSISDN(model.auth_msisdn);

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //}
        #endregion
        #region  MSISDN validation Unpaired
        /// <summary>
        /// This method is used for MSISDN validation for unpaired
        /// </summary>
        /// <param name="imsiCheckReq">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        internal async Task<GetImsiRespObj> GetImsiBySimAsync(GetImsiReq imsiCheckReq)
        {
            GetImsiRespObj imsiResp = new GetImsiRespObj();
            JObject dbssResp = new JObject();
            string apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();

            try
            {
                if (imsiCheckReq?.sim != null && imsiCheckReq.sim.Substring(0, 6) != FixedValueCollection.SIMCode)
                {
                    imsiCheckReq.sim = FixedValueCollection.SIMCode + imsiCheckReq.sim;
                }
                else if (imsiCheckReq?.sim != null && imsiCheckReq?.sim.Substring(0, 6) == FixedValueCollection.SIMCode)
                {
                    imsiCheckReq.sim = imsiCheckReq.sim;
                }
                var encodedSim = Uri.EscapeDataString(imsiCheckReq?.sim ?? "");
                apiUrl = String.Format(GetAPICollection.GetImsiBySim, encodedSim);
                log.req_blob = await _blJson.GetGenericJsonDataAsync(apiUrl);
                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetImsiBySimAsync");
                log.res_time = DateTime.Now;
                txtResp = dbssResp.ToString(Newtonsoft.Json.Formatting.Indented);
                log.res_blob = await _blJson.GetGenericJsonDataAsync(dbssResp);
                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    imsiResp.result = false;
                    imsiResp.message = MessageCollection.NoDataFound;
                    return imsiResp;
                }

                imsiResp = _dbssToRaParse.GetImsiRespParsingAsync(dbssResp);

                if (imsiResp.result == false)
                {
                    log.is_success = 0;
                    imsiResp.result = false;
                    imsiResp.message = imsiResp.message;
                    return imsiResp;
                }

                imsiResp.result = true;
                imsiResp.message = MessageCollection.Success;
                return imsiResp;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                imsiResp.result = false;
                imsiResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;
                return imsiResp;
            }
            finally
            {
                log.msisdn = imsiCheckReq?.msisdn ?? "";
                log.purpose_number = imsiCheckReq?.purpose_number ?? "";
                log.user_id = imsiCheckReq?.retailer_id ?? "";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.method_name = "GetImsiBySim";
                await _bllLog.RAToDBSSLog(log);
            }
        }
        #endregion
        #region bioverification process
        public async Task<BioVerifyResp> BssServiceProcess(BiomerticDataModel item)
        {
            LogModel log = new LogModel();
            BiometricPopulateModel pltApiObj = new BiometricPopulateModel();
            BioVerifyResp verifyResp = new BioVerifyResp();
            string meathodUrl = "/api/v1/biometric";
            GetImsiRespObj imsiResp = new GetImsiRespObj();
            BL_Json byteArrayConverter = new BL_Json();

            if (item.status == (int)EnumRAOrderStatus.BioVerificationSubmitted)
            {
                try
                {
                    if (item.purpose_number == (int)EnumPurposeNumber.NewRegistration)
                    {
                        object reqModel = new object();

                        if (string.IsNullOrEmpty(item.poc_number))
                            reqModel = pltApiObj.PopulateNewRegReqModel(item);
                        else
                            reqModel = pltApiObj.PopulateCorpNewRegReqModel(item);

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBss(item, reqModel, meathodUrl);
                        log.res_time = DateTime.Now;
                    }

                    else if (item.purpose_number == (int)EnumPurposeNumber.DeRegistration)
                    {
                        object reqModel = new object();

                        if (string.IsNullOrEmpty(item.poc_number))
                            reqModel = pltApiObj.PopulateDeRegReqModel(item);
                        else
                            reqModel = pltApiObj.PopulateCorpDeRegReqModel(item);

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBss(item, reqModel, meathodUrl);
                        log.res_time = DateTime.Now;
                    }

                    else if (item.purpose_number == (int)EnumPurposeNumber.SIMReplacement)
                    {
                        object reqModel = new object();

                        if (string.IsNullOrEmpty(item.poc_number))
                            reqModel = pltApiObj.PopulateSimRepRegReqModel(item);
                        else
                        {
                            if (item.sim_replacement_type == (int)EnumSIMReplacementType.ByPOC)
                                reqModel = pltApiObj.PopulateCorpSimReplacebyPocReqModel(item);
                            else if (item.sim_replacement_type == (int)EnumSIMReplacementType.ByAuthPerson)
                                reqModel = pltApiObj.PopulateCorpSimReplacebyAuthPerReqModel(item);
                            else if (item.sim_replacement_type == (int)EnumSIMReplacementType.BulkSIMReplacment)
                                reqModel = pltApiObj.PopulateCorpSimReplacebyBulkReqModel(item);
                        }

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBss(item, reqModel, meathodUrl);
                        log.res_time = DateTime.Now;
                    }

                    else if (item.purpose_number == (int)EnumPurposeNumber.MNPRegistration)
                    {
                        object reqModel = new object();

                        if (string.IsNullOrEmpty(item.poc_number))
                            reqModel = pltApiObj.PopulateMnpRegReqModel(item);
                        else
                            reqModel = pltApiObj.PopulateCorpMnpPortInReqModel(item);

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBss(item, reqModel, meathodUrl);
                        log.res_time = DateTime.Now;
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.MNPEmergencyReturn)
                    {
                        object reqModel = new object();

                        if (string.IsNullOrEmpty(item.poc_number))
                            reqModel = pltApiObj.PopulateMnpEmgRtnRegReqModel(item);
                        else
                            reqModel = pltApiObj.PopulateCorpMnpEmerReturnReqModel(item);

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBss(item, reqModel, meathodUrl);
                        log.res_time = DateTime.Now;
                    }

                    else if (item.purpose_number == (int)EnumPurposeNumber.SIMTransfer)
                    {
                        object reqModel = new object();
                        if (item.src_ec_verification_required == 1 && String.IsNullOrEmpty(item.poc_number))
                            reqModel = pltApiObj.PopulateSimTransferNidToNidBioReqModel(item);

                        else if (item.src_ec_verification_required == 1)
                            reqModel = pltApiObj.PopulateSimTransferBioReqModel(item);

                        else
                            reqModel = pltApiObj.PopulateSimTransferWithoutSrcBioReqModel(item);

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBss(item, reqModel, meathodUrl);
                        log.res_time = DateTime.Now;
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.CorporateToIndividualTransfer)
                    {
                        object reqModel = new object();

                        if (item.src_ec_verification_required == 1)
                            reqModel = pltApiObj.PopulateSimTransferBioReqModel(item);
                        else if (!string.IsNullOrEmpty(item.otp_no))
                            reqModel = pltApiObj.PopulateCorpSimTransferWithOTPBioReqModel(item);
                        else
                            reqModel = pltApiObj.PopulateCorpSimTransferBioReqModel(item);
                        //object reqModel = pltObj.PopulateSimTransferBioReqModel(item);//as per siful vai instructin, use tos request model here.

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBss(item, reqModel, meathodUrl);
                        log.res_time = DateTime.Now;
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.IndividualToCorporateTransfer)
                    {
                        object reqModel = pltApiObj.PopulateSimTransferBioReqModel(item);//as per siful vai instructin, use tos request model here.

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBss(item, reqModel, meathodUrl);
                        log.res_time = DateTime.Now;
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.mnp_port_in_cancel)
                    {
                        PortInCnlRegReqModel reqModel = pltApiObj.PopulatePortCnlRegReqModel(item);

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBss(item, reqModel, meathodUrl);
                        log.res_time = DateTime.Now;
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.SIMCategoryMigration)
                    {
                        object reqModel = new object();
                        if (!string.IsNullOrEmpty(item.poc_number))
                        {
                            reqModel = pltApiObj.PopulateCorpNewRegReqModel(item);
                        }
                        else
                        {
                            reqModel = pltApiObj.PopulatePreToPostMigrationReqModel(item);
                        }

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBss(item, reqModel, meathodUrl);
                        log.res_time = DateTime.Now;

                    }
                    return verifyResp;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            return verifyResp;
        }
        public async Task<BioVerifyResp> BssServiceProcessV2(BiomerticDataModel item)
        {
            LogModel log = new LogModel();
            BiometricPopulateModel pltApiObj = new BiometricPopulateModel();
            BioVerifyResp verifyResp = new BioVerifyResp();
            string meathodUrl = "/api/v1/biometric";
            GetImsiRespObj imsiResp = new GetImsiRespObj();
            BL_Json byteArrayConverter = new BL_Json();

            if (item.status == (int)EnumRAOrderStatus.BioVerificationSubmitted)
            {
                try
                {
                    if (item.purpose_number == (int)EnumPurposeNumber.NewRegistration)
                    {
                        object reqModel = new object();
                        object blob_data = new object();

                        if (string.IsNullOrEmpty(item.poc_number))
                        {
                            reqModel = pltApiObj.PopulateNewRegReqModel(item);

                            try
                            {
                                blob_data = pltApiObj.PopulateNewRegReqModelForBLOB(item);
                            }
                            catch { }
                        }
                        else
                        {
                            reqModel = pltApiObj.PopulateCorpNewRegReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateCorpNewRegReqModelForBLOB(item);
                            }
                            catch
                            { }
                        }

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBssV2(item, reqModel, meathodUrl, blob_data);
                        log.res_time = DateTime.Now;
                    }

                    else if (item.purpose_number == (int)EnumPurposeNumber.DeRegistration)
                    {
                        object reqModel = new object();
                        object blob_data = new object();

                        if (string.IsNullOrEmpty(item.poc_number))
                        {
                            reqModel = pltApiObj.PopulateDeRegReqModel(item);

                            try
                            {
                                blob_data = pltApiObj.PopulateDeRegReqModelForBLOB(item);
                            }
                            catch { }
                        }
                        else
                        {
                            reqModel = pltApiObj.PopulateCorpDeRegReqModel(item);

                            try
                            {
                                blob_data = pltApiObj.PopulateCorpDeRegReqModelForBLOB(item);
                            }
                            catch { }
                        }

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBssV2(item, reqModel, meathodUrl, blob_data);
                        log.res_time = DateTime.Now;
                    }

                    else if (item.purpose_number == (int)EnumPurposeNumber.SIMReplacement)
                    {
                        object reqModel = new object();
                        object blob_data = new object();

                        if (string.IsNullOrEmpty(item.poc_number))
                        {
                            reqModel = pltApiObj.PopulateSimRepRegReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateSimRepRegReqModelForBLOB(item);
                            }
                            catch { }
                        }
                        else
                        {
                            if (item.sim_replacement_type == (int)EnumSIMReplacementType.ByPOC)
                            {
                                reqModel = pltApiObj.PopulateCorpSimReplacebyPocReqModel(item);
                                try
                                {
                                    blob_data = pltApiObj.PopulateCorpSimReplacebyPocReqModelForBLOB(item);
                                }
                                catch { }

                            }
                            else if (item.sim_replacement_type == (int)EnumSIMReplacementType.ByAuthPerson)
                            {
                                reqModel = pltApiObj.PopulateCorpSimReplacebyAuthPerReqModel(item);
                                try
                                {
                                    blob_data = pltApiObj.PopulateCorpSimReplacebyAuthPerReqModelForBLOB(item);
                                }
                                catch { }
                            }
                            else if (item.sim_replacement_type == (int)EnumSIMReplacementType.BulkSIMReplacment)
                            {
                                reqModel = pltApiObj.PopulateCorpSimReplacebyBulkReqModel(item);
                                try
                                {
                                    blob_data = pltApiObj.PopulateCorpSimReplacebyBulkReqModelForBLOB(item);
                                }
                                catch { }
                            }
                        }

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBssV2(item, reqModel, meathodUrl, blob_data);
                        log.res_time = DateTime.Now;
                    }

                    else if (item.purpose_number == (int)EnumPurposeNumber.MNPRegistration)
                    {
                        object reqModel = new object();
                        object blob_data = new object();

                        if (string.IsNullOrEmpty(item.poc_number))
                        {
                            reqModel = pltApiObj.PopulateMnpRegReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateMnpRegReqModelForBLOB(item);
                            }
                            catch { }
                        }
                        else
                        {
                            reqModel = pltApiObj.PopulateCorpMnpPortInReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateCorpMnpPortInReqModelForBLOB(item);
                            }
                            catch { }
                        }

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBssV2(item, reqModel, meathodUrl, blob_data);
                        log.res_time = DateTime.Now;
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.MNPEmergencyReturn)
                    {
                        object reqModel = new object();
                        object blob_data = new object();

                        if (string.IsNullOrEmpty(item.poc_number))
                        {
                            reqModel = pltApiObj.PopulateMnpEmgRtnRegReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateMnpEmgRtnRegReqModelForBLOB(item);
                            }
                            catch { }
                        }
                        else
                        {
                            reqModel = pltApiObj.PopulateCorpMnpEmerReturnReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateCorpMnpEmerReturnReqModelForBLOB(item);
                            }
                            catch { }
                        }

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBssV2(item, reqModel, meathodUrl, blob_data);
                        log.res_time = DateTime.Now;
                    }

                    else if (item.purpose_number == (int)EnumPurposeNumber.SIMTransfer)
                    {
                        object reqModel = new object();
                        object blob_data = new object();
                        if (item.src_ec_verification_required == 1 && String.IsNullOrEmpty(item.poc_number))
                        {
                            reqModel = pltApiObj.PopulateSimTransferNidToNidBioReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateSimTransferNidToNidBioReqModelForBLOB(item);
                            }
                            catch { }
                        }
                        else if (item.src_ec_verification_required == 1)
                        {
                            reqModel = pltApiObj.PopulateSimTransferBioReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateSimTransferBioReqModelForBLOB(item);
                            }
                            catch { }
                        }
                        else
                        {
                            reqModel = pltApiObj.PopulateSimTransferWithoutSrcBioReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateSimTransferWithoutSrcBioReqModelForBLOB(item);
                            }
                            catch { }
                        }

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBssV2(item, reqModel, meathodUrl, blob_data);
                        log.res_time = DateTime.Now;
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.CorporateToIndividualTransfer)
                    {
                        object reqModel = new object();
                        object blob_data = new object();

                        if (item.src_ec_verification_required == 1)
                        {
                            reqModel = pltApiObj.PopulateSimTransferBioReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateSimTransferBioReqModelForBLOB(item);
                            }
                            catch { }
                        }
                        else if (!string.IsNullOrEmpty(item.otp_no))
                        {
                            reqModel = pltApiObj.PopulateCorpSimTransferWithOTPBioReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateCorpSimTransferWithOTPBioReqModelForBLOB(item);
                            }
                            catch { }
                        }
                        else
                        {
                            reqModel = pltApiObj.PopulateCorpSimTransferBioReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateCorpSimTransferBioReqModelForBLOB(item);
                            }
                            catch { }
                        }

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBssV2(item, reqModel, meathodUrl, blob_data);
                        log.res_time = DateTime.Now;
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.IndividualToCorporateTransfer)
                    {
                        object blob_data = new object();
                        object reqModel = pltApiObj.PopulateSimTransferBioReqModel(item);//as per siful vai instructin, use tos request model here.

                        try
                        {
                            blob_data = pltApiObj.PopulateSimTransferBioReqModelForBLOB(item);
                        }
                        catch { }

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBssV2(item, reqModel, meathodUrl, blob_data);
                        log.res_time = DateTime.Now;
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.mnp_port_in_cancel)
                    {
                        object blob_data = new object();
                        object reqModel = pltApiObj.PopulatePortCnlRegReqModel(item);

                        try
                        {
                            blob_data = pltApiObj.PopulatePortCnlRegReqModelForBLOB(item);
                        }
                        catch { }

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBssV2(item, reqModel, meathodUrl, blob_data);
                        log.res_time = DateTime.Now;
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.SIMCategoryMigration)
                    {
                        object reqModel = new object();
                        object blob_data = new object();
                        if (!string.IsNullOrEmpty(item.poc_number))
                        {
                            reqModel = pltApiObj.PopulateCorpNewRegReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateCorpNewRegReqModelForBLOB(item);
                            }
                            catch { }
                        }
                        else
                        {
                            reqModel = pltApiObj.PopulatePreToPostMigrationReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulatePreToPostMigrationReqModelForBLOB(item);
                            }
                            catch { }
                        }

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBssV2(item, reqModel, meathodUrl, blob_data);
                        log.res_time = DateTime.Now;

                    }
                    return verifyResp;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            return verifyResp;
        }
        public async Task<BioVerifyResp> BssServiceProcessStarTrek(BiomerticDataModel item, string reservationId, string userName, int isOnline)
        {
            LogModel log = new LogModel();
            BiometricPopulateModel pltApiObj = new BiometricPopulateModel();
            BioVerifyResp verifyResp = new BioVerifyResp();
            string meathodUrl = "/api/v1/biometric";
            GetImsiRespObj imsiResp = new GetImsiRespObj();
            BL_Json byteArrayConverter = new BL_Json();
            RACommonResponse response = new RACommonResponse();

            if (item.status == (int)EnumRAOrderStatus.BioVerificationSubmitted)
            {
                try
                {
                    if (item.purpose_number == (int)EnumPurposeNumber.NewRegistration)
                    {
                        object reqModel = new object();
                        object blob_data = new object();

                        if (string.IsNullOrEmpty(item.poc_number))
                        {
                            reqModel = pltApiObj.PopulateNewRegReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateNewRegReqModelForBLOB(item);
                            }
                            catch { }
                        }
                        else
                        {
                            reqModel = pltApiObj.PopulateCorpNewRegReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateCorpNewRegReqModelForBLOB(item);
                            }
                            catch { }
                        }

                        if (isOnline == 1)
                        {
                            response = await _apiCall.UnreserveMSISDNStarTrek(reservationId, userName, "", "", item.msisdn);

                            if (response.result == false)
                            {
                                verifyResp.is_success = false;
                                verifyResp.err_msg = response.message;
                                return verifyResp;
                            }

                            log.req_time = DateTime.Now;
                            verifyResp = await _apiCall.BioVerificationReqToBssV3(item, reqModel, meathodUrl, blob_data);
                            log.res_time = DateTime.Now;
                        }
                        else
                        {
                            log.req_time = DateTime.Now;
                            verifyResp = await _apiCall.BioVerificationReqToBssV2(item, reqModel, meathodUrl, blob_data);
                            log.res_time = DateTime.Now;
                        }
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.SIMReplacement)
                    {
                        object reqModel = new object();
                        object blob_data = new object();

                        if (string.IsNullOrEmpty(item.poc_number))
                        {
                            reqModel = pltApiObj.PopulateSimRepRegReqModel(item);
                            try
                            {
                                blob_data = pltApiObj.PopulateSimRepRegReqModelForBLOB(item);
                            }
                            catch { }
                        }
                        else
                        {
                            if (item.sim_replacement_type == (int)EnumSIMReplacementType.ByPOC)
                            {
                                reqModel = pltApiObj.PopulateCorpSimReplacebyPocReqModel(item);
                                try
                                {
                                    blob_data = pltApiObj.PopulateCorpSimReplacebyPocReqModelForBLOB(item);
                                }
                                catch { }
                            }
                            else if (item.sim_replacement_type == (int)EnumSIMReplacementType.ByAuthPerson)
                            {
                                reqModel = pltApiObj.PopulateCorpSimReplacebyAuthPerReqModel(item);
                                try
                                {
                                    blob_data = pltApiObj.PopulateCorpSimReplacebyAuthPerReqModelForBLOB(item);
                                }
                                catch { }
                            }
                            else if (item.sim_replacement_type == (int)EnumSIMReplacementType.BulkSIMReplacment)
                            {
                                reqModel = pltApiObj.PopulateCorpSimReplacebyBulkReqModel(item);
                                try
                                {
                                    blob_data = pltApiObj.PopulateCorpSimReplacebyBulkReqModelForBLOB(item);
                                }
                                catch { }
                            }
                        }

                        log.req_time = DateTime.Now;
                        verifyResp = await _apiCall.BioVerificationReqToBssV2(item, reqModel, meathodUrl, blob_data);
                        log.res_time = DateTime.Now;
                    }
                    return verifyResp;
                }
                catch (Exception)
                {
                    throw;
                }

            }
            return verifyResp;
        }
        #endregion
        #region Get Decrypted Security Token
        internal string GetDecryptedSecurityToken(string encryptedToken)
        {
            string decriptedSecurityToken = string.Empty;
            string loginProviderId = string.Empty;
            try
            {
                decriptedSecurityToken = AESCryptography.Decrypt(encryptedToken);

                if (decriptedSecurityToken.Equals("InvalidSessionToken"))
                {
                    decriptedSecurityToken = string.Empty;
                    decriptedSecurityToken = Cryptography.Decrypt(encryptedToken, true);
                    loginProviderId = _bllCommon.GetDataFromSecurityTokenV3(decriptedSecurityToken, (int)EnumSecurityTokenPropertyIndex.prov_id);

                }
                else
                {
                    loginProviderId = _bllCommon.GetDataFromSecurityTokenV2(decriptedSecurityToken, (int)EnumSecurityTokenPropertyIndex.prov_id);
                }

                return loginProviderId;
            }
            catch (Exception)
            {
                try
                {
                    decriptedSecurityToken = Cryptography.Decrypt(encryptedToken, true);
                    loginProviderId = _bllCommon.GetDataFromSecurityTokenV3(decriptedSecurityToken, (int)EnumSecurityTokenPropertyIndex.prov_id);

                    return loginProviderId;
                }
                catch (Exception)
                {
                    return "Fail";
                }
            }
        }
        #endregion
        #region Star Trek (Ryze Part)
        public async Task<RACommonResponse> CheckSIMNumberReplacement(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = _blJson.GetGenericJsonData(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumberReplacement");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }
                raResp = SIMValidationParsingSIMReplacement(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                log.is_success = 0;

                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumberReplacement";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }
        public async Task<RACommonResponse> CheckSIMNumberReplacementV2(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = _blJson.GetGenericJsonData(dbssReqModel);
                log.req_time = DateTime.Now;

                //JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumberReplacementV2");
                JObject dbssResp = JObject.Parse("{\r\n  \"data\": {\r\n    \"status\": \"success\",\r\n    \"logical_inventory_status\": \"unpaired\",\r\n    \"physical_inventory_status\": \"RYZ-Prepaid\"\r\n  }\r\n}");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }
                raResp = SIMValidationParsingSIMReplacementV2(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;

                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumberReplacementV2";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }
        public RACommonResponse SIMValidationParsingSIMReplacement(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            var response = new RACommonResponse();

            try
            {
                var data = dbssResp?["data"];
                string? status = data?["status"]?.ToString();
                string? logicalStatus = data?["logical_inventory_status"]?.ToString();
                string? physicalStatus = data?["physical_inventory_status"]?.ToString();

                if (string.IsNullOrEmpty(status) && string.IsNullOrEmpty(logicalStatus) && string.IsNullOrEmpty(physicalStatus))
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }

                if (string.Equals(physicalStatus, FixedValueCollection.PaymentTypeESimStarTrek, StringComparison.OrdinalIgnoreCase))
                {
                    response.result = false;
                    response.message = "This is not Physical SIM.";
                    return response;
                }

                if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
                {
                    string? errorMessage = data?["error_message"]?.ToString();
                    response.result = false;
                    response.message = !string.IsNullOrWhiteSpace(errorMessage)
                        ? errorMessage
                        : MessageCollection.SIMIsNotInInventory;
                    return response;
                }

                if (string.Equals(logicalStatus, "used", StringComparison.OrdinalIgnoreCase))
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }

                // ---------------- SIMReplacement ----------------
                if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.SIMReplacement && !string.IsNullOrWhiteSpace(oldSimType))
                {
                    if (string.Equals(oldSimType, FixedValueCollection.SIMTypeUSIM, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(physicalStatus, FixedValueCollection.PaymentTypePrepaidStarTrek, StringComparison.OrdinalIgnoreCase))
                        {
                            response.result = true;
                            response.message = MessageCollection.SIMValid;
                            return response;
                        }
                        else
                        {
                            response.result = false;
                            response.message = MessageCollection.NotASwapSIMStarTrek;
                            return response;
                        }
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMTypeIsNotSIMOrUSIM;
                        return response;
                    }
                }

                // Default case
                response.result = false;
                response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
                return response;
            }
            catch (Exception)
            {
                throw;
            }
        }
        //public RACommonResponse SIMValidationParsingSIMReplacement(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        //{
        //    RACommonResponse response = new RACommonResponse();
        //    try
        //    {
        //        if (dbssResp?["data"]?["status"] == null
        //            && dbssResp?["data"]?["logical_inventory_status"] == null
        //            && dbssResp?["data"]?["physical_inventory_status"] == null
        //            && String.IsNullOrEmpty(dbssResp?["data"]?["status"]?.ToString())
        //            && String.IsNullOrEmpty(dbssResp?["data"]?["logical_inventory_status"]?.ToString())
        //            && String.IsNullOrEmpty(dbssResp?["data"]?["physical_inventory_status"]?.ToString()))
        //        {
        //            response.result = false;
        //            response.message = MessageCollection.DataNotFound;
        //            return response;
        //        }
        //        else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESimStarTrek.ToLower() /*ryz-esim*/)
        //        {
        //            {
        //                response.result = false;
        //                response.message = "This is not Physical SIM.";
        //                return response;
        //            }
        //        }
        //        else if (dbssResp?["data"]?["status"]?.ToString().ToLower() == "failed")
        //        {
        //            response.result = false;
        //            response.message = dbssResp?["data"]?["error_message"] != null
        //                                && dbssResp?["data"]?["error_message"]?.ToString() != "" ? dbssResp?["data"]?["error_message"]?.ToString() : MessageCollection.SIMIsNotInInventory;
        //            return response;
        //        }

        //        else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
        //        {
        //            response.result = false;
        //            response.message = MessageCollection.SIMIsUsed;
        //            return response;
        //        }                //----------------SIMReplacement--------------
        //        else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.SIMReplacement
        //            && !String.IsNullOrEmpty(oldSimType))
        //        {
        //            if (oldSimType.ToLower() == FixedValueCollection.SIMTypeUSIM /*"usim"*/)
        //            {
        //                if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaidStarTrek.ToLower() /*"ryze-prepaid"*/)
        //                {
        //                    response.result = true;
        //                    response.message = MessageCollection.SIMValid;
        //                    return response;
        //                }
        //                else
        //                {
        //                    response.result = false;
        //                    response.message = MessageCollection.NotASwapSIMStarTrek;
        //                    return response;
        //                }
        //            }
        //            else
        //            {
        //                response.result = false;
        //                response.message = MessageCollection.SIMTypeIsNotSIMOrUSIM;
        //                return response;
        //            }
        //        }
        //        else
        //        {
        //            response.result = false;
        //            response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
        //            return response;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
        //public RACommonResponse SIMValidationParsingSIMReplacementV2(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        //{
        //    RACommonResponse response = new RACommonResponse();
        //    try
        //    {
        //        if (dbssResp?["data"]?["status"] == null
        //            && dbssResp?["data"]?["logical_inventory_status"] == null
        //            && dbssResp?["data"]?["physical_inventory_status"] == null
        //            && String.IsNullOrEmpty(dbssResp?["data"]?["status"]?.ToString())
        //            && String.IsNullOrEmpty(dbssResp?["data"]?["logical_inventory_status"]?.ToString())
        //            && String.IsNullOrEmpty(dbssResp?["data"]?["physical_inventory_status"]?.ToString()))
        //        {
        //            response.result = false;
        //            response.message = MessageCollection.DataNotFound;
        //            return response;
        //        }
        //        else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESimStarTrek.ToLower() /*ryz-esim*/
        //            || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower() /*E-SIM*/
        //            || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower()/*e_sim_swap*/)
        //        {
        //            response.result = false;
        //            response.message = "This is not Physical SIM.";
        //            return response;
        //        }
        //        else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower()/*postpaid*/)
        //        {
        //            response.result = false;
        //            response.message = "Incorrect Product!";
        //            return response;
        //        }

        //        else if (dbssResp?["data"]?["status"]?.ToString().ToLower() == "failed")
        //        {
        //            response.result = false;
        //            response.message = dbssResp?["data"]?["error_message"] != null
        //                                && dbssResp?["data"]?["error_message"]?.ToString() != "" ? dbssResp?["data"]?["error_message"]?.ToString() : MessageCollection.SIMIsNotInInventory;
        //            return response;
        //        }

        //        else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
        //        {
        //            response.result = false;
        //            response.message = MessageCollection.SIMIsUsed;
        //            return response;
        //        }                //----------------SIMReplacement--------------
        //        else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.SIMReplacement
        //            && !String.IsNullOrEmpty(oldSimType))
        //        {
        //            if (oldSimType.ToLower() == FixedValueCollection.SIMTypeUSIM /*"usim"*/ || oldSimType.ToLower() == FixedValueCollection.SIMTypePLI /*"pli"*/)
        //            {
        //                if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLower() /*"sim_swap"*/
        //                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaidStarTrek.ToLower()/*ryz-prepaid*/)
        //                {
        //                    response.result = true;
        //                    response.message = MessageCollection.SIMValid;
        //                    return response;
        //                }
        //                else
        //                {
        //                    response.result = false;
        //                    response.message = MessageCollection.NotASwapSIMStarTrek;
        //                    return response;
        //                }
        //            }
        //            else
        //            {
        //                response.result = false;
        //                response.message = MessageCollection.SIMTypeIsNotUSIM;
        //                return response;
        //            }
        //        }
        //        else
        //        {
        //            response.result = false;
        //            response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
        //            return response;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}
        public RACommonResponse SIMValidationParsingSIMReplacementV2(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            var response = new RACommonResponse();
            try
            {
                var data = dbssResp?["data"];
                string? status = data?["status"]?.ToString();
                string? logicalStatus = data?["logical_inventory_status"]?.ToString();
                string? physicalStatus = data?["physical_inventory_status"]?.ToString();
                string? errorMessage = data?["error_message"]?.ToString();

                if (string.IsNullOrEmpty(status) && string.IsNullOrEmpty(logicalStatus) && string.IsNullOrEmpty(physicalStatus))
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }

                if (physicalStatus?.ToLowerInvariant() == FixedValueCollection.PaymentTypeESimStarTrek.ToLowerInvariant()
                    || physicalStatus?.ToLowerInvariant() == FixedValueCollection.PaymentTypeESim.ToLowerInvariant()
                    || physicalStatus?.ToLowerInvariant() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLowerInvariant())
                {
                    response.result = false;
                    response.message = "This is not Physical SIM.";
                    return response;
                }

                if (physicalStatus?.ToLowerInvariant() == FixedValueCollection.PaymentTypePostpaid.ToLowerInvariant())
                {
                    response.result = false;
                    response.message = "Incorrect Product!";
                    return response;
                }

                if (status?.ToLowerInvariant() == "failed")
                {
                    response.result = false;
                    response.message = !string.IsNullOrEmpty(errorMessage) ? errorMessage : MessageCollection.SIMIsNotInInventory;
                    return response;
                }

                if (logicalStatus?.ToLowerInvariant() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }

                // SIM Replacement logic
                if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.SIMReplacement && !string.IsNullOrEmpty(oldSimType))
                {
                    if (oldSimType.ToLowerInvariant() == FixedValueCollection.SIMTypeUSIM.ToLowerInvariant()
                        || oldSimType.ToLowerInvariant() == FixedValueCollection.SIMTypePLI.ToLowerInvariant())
                    {
                        if (physicalStatus?.ToLowerInvariant() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLowerInvariant()
                            || physicalStatus?.ToLowerInvariant() == FixedValueCollection.PaymentTypePrepaidStarTrek.ToLowerInvariant())
                        {
                            response.result = true;
                            response.message = MessageCollection.SIMValid;
                            return response;
                        }
                        else
                        {
                            response.result = false;
                            response.message = MessageCollection.NotASwapSIMStarTrek;
                            return response;
                        }
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMTypeIsNotUSIM;
                        return response;
                    }
                }

                response.result = false;
                response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
                return response;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<IndividualSIMReplacementMSISDNCheckResponseRevamp> STarTrekValidateSIMForReplacement(IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest)
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
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }
                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);

                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingOwnerCustomerUserCustomerSimCardInfo, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                JObject dbssResp = new JObject();
                try
                {
                    dbssResp = await _apiReq.HttpGetRequest(apiUrl, "STarTrekValidateSIMForReplacement");

                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("Not Found"))
                    {
                        response.isError = true;
                        response.message = "Invalid MSISDN input for SIM Replacement.";
                        return response;
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
                    response.isError = true;
                    response.message = MessageCollection.SIMReplNoDataFound;
                    return response;
                }

                log.is_success = 1;

                var msisdnResp = StarTrekSIMReplacementParsing(dbssResp);

                if (msisdnResp.result == false)
                {
                    response.isError = true;
                    response.message = FixedValueCollection.MSISDNError + msisdnResp.message;
                    return response;
                }

                var simResp = await CheckSIMNumberReplacement(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
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
                    response.isError = true;
                    response.message = simResp.message;
                    return response;
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
                response.isError = false;
                response.message = resp.message;
                response.data = resp;
                return response;
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

                response.isError = true;
                response.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                return response;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "STarTrekValidateSIMForReplacement";
                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<IndividualSIMReplacementMSISDNCheckResponseRevamp> STarTrekValidateSIMForReplacementV2(IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest)
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
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }
                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);
                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingOwnerCustomerUserCustomerSimCardInfo, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                JObject dbssResp = new JObject();
                try
                {
                    dbssResp = await _apiReq.HttpGetRequest(apiUrl, "STarTrekValidateSIMForReplacementV2");
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("Not Found"))
                    {
                        response.isError = true;
                        response.message = "Invalid MSISDN input for SIM Replacement.";
                        return response;
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
                    response.isError = true;
                    response.message = MessageCollection.SIMReplNoDataFound;
                    return response;
                }

                log.is_success = 1;

                var msisdnResp = StarTrekSIMReplacementParsing(dbssResp);

                if (msisdnResp.result == false)
                {
                    response.isError = true;
                    response.message = FixedValueCollection.MSISDNError + msisdnResp.message;
                    return response;
                }

                var simResp = await CheckSIMNumberReplacementV2(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
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
                    response.isError = true;
                    response.message = simResp.message;
                    return response;
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
                response.isError = false;
                response.message = resp.message;
                response.data = resp;
                return response;
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

                response.isError = true;
                response.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                return response;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "STarTrekValidateSIMForReplacementV2";
                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<IndividualSIMReplacementMSISDNCheckResponseRevamp> StarTrekValidateSIMForReplacement_ESIM(IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest)
        {
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            IndividualSIMReplacementMSISDNCheckResponseRevamp response = new IndividualSIMReplacementMSISDNCheckResponseRevamp();
            try
            {

                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);

                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingOwnerCustomerUserCustomerSimCardInfo, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                JObject dbssResp = new JObject();
                try
                {
                    dbssResp = await _apiReq.HttpGetRequest(apiUrl, "StarTrekValidateSIMForReplacement_ESIM");
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("Not Found"))
                    {
                        response.isError = true;
                        response.message = "Invalid MSISDN input for E-SIM Replacement.";
                        return response;
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
                    response.isError = true;
                    response.message = MessageCollection.SIMReplNoDataFound;
                    return response;
                }

                log.is_success = 1;

                var msisdnResp = StarTrekSIMReplacementParsing(dbssResp);

                if (msisdnResp.result == false)
                {
                    response.isError = true;
                    response.message = FixedValueCollection.MSISDNError + msisdnResp.message;
                    return response;
                }

                var simResp = await CheckSIMNumber(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
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
                    response.isError = true;
                    response.message = simResp.message;
                    return response;
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
                response.isError = false;
                response.message = resp.message;
                response.data = resp;
                return response;
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

                response.isError = true;
                response.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                return response;
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
        public async Task<IndividualSIMReplacementMSISDNCheckResponseRevamp> StarTrekValidateSIMForReplacement_ESIMV2(IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest)
        {
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            IndividualSIMReplacementMSISDNCheckResponseRevamp response = new IndividualSIMReplacementMSISDNCheckResponseRevamp();
            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);

                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingOwnerCustomerUserCustomerSimCardInfo, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                JObject dbssResp = new JObject();
                try
                {
                    dbssResp = await _apiReq.HttpGetRequest(apiUrl, "StarTrekValidateSIMForReplacement_ESIMV2");

                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("Not Found"))
                    {
                        response.isError = true;
                        response.message = "Invalid MSISDN input for E-SIM Replacement.";
                        return response;
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
                    response.isError = true;
                    response.message = MessageCollection.SIMReplNoDataFound;
                    return response;
                }

                log.is_success = 1;

                var msisdnResp = StarTrekSIMReplacementParsing(dbssResp);

                if (msisdnResp.result == false)
                {
                    response.isError = true;
                    response.message = FixedValueCollection.MSISDNError + msisdnResp.message;
                    return response;
                }

                var simResp = await CheckSIMNumberV3(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
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
                    response.isError = true;
                    response.message = simResp.message;
                    return response;
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
                response.isError = false;
                response.message = resp.message;
                response.data = resp;
                return response;
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

                response.isError = true;
                response.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                return response;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "StarTrekValidateSIMForReplacement_ESIMV2";

                await _bllLog.RAToDBSSLog(log);
            }
        }
        //public IndividualSIMReplacementMSISDNCheckResponse StarTrekSIMReplacementParsing(JObject dbssRespObj)
        //{
        //    IndividualSIMReplacementMSISDNCheckResponse raResp = new IndividualSIMReplacementMSISDNCheckResponse();
        //    try
        //    {
        //        if (!dbssRespObj["data"].HasValues
        //            || dbssRespObj["data"].Count() <= 0)
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.SIMReplNoDataFound;
        //            return raResp;
        //        }

        //        if (String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["status"]))
        //        {
        //            raResp.result = false;
        //            raResp.message = "Msisdn status not found!";
        //            return raResp;
        //        }

        //        if ((string)dbssRespObj["data"]["attributes"]["status"] == "terminated")
        //        {
        //            raResp.result = false;
        //            raResp.message = "Msisdn is not valid for SIM replacemnt!";
        //            return raResp;
        //        }

        //        if ((string)dbssRespObj["data"]["attributes"]["status"] != "active"
        //             && (string)dbssRespObj["data"]["attributes"]["status"] != "idle")
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.MSISDNStatusNotActiveOrIdle;
        //            raResp.dob = null;
        //            raResp.doc_id_number = null;
        //            raResp.saf_status = false;
        //            return raResp;
        //        }

        //        if (!dbssRespObj["included"].HasValues
        //            || (dbssRespObj["included"].Count() != 2
        //            && dbssRespObj["included"].Count() != 3))
        //        {
        //            raResp.result = false;
        //            raResp.message = "Data not found in include field!";
        //            raResp.dob = null;
        //            raResp.doc_id_number = null;
        //            raResp.saf_status = false;
        //            return raResp;
        //        }

        //        if (dbssRespObj["data"]["id"] == null)
        //        {
        //            raResp.result = false;
        //            raResp.message = "Subscription ID field empty!";
        //            return raResp;
        //        }
        //        if (dbssRespObj["included"][0]["attributes"] == null
        //            || dbssRespObj["included"][1]["attributes"] == null)
        //        {
        //            raResp.result = false;
        //            raResp.message = "Data not found in include field!";
        //            return raResp;
        //        }
        //        if (String.IsNullOrEmpty((string)dbssRespObj["included"][1]["attributes"]["icc"]))
        //        {
        //            raResp.result = false;
        //            raResp.message = "Old SIM number not found!";
        //            return raResp;
        //        }
        //        if (String.IsNullOrEmpty((string)dbssRespObj["included"][1]["attributes"]["sim-type"]))
        //        {
        //            raResp.result = false;
        //            raResp.message = "sim-type not found!";
        //            return raResp;
        //        }

        //        if (dbssRespObj["included"][0]["attributes"]["is-company"] == null)
        //        {
        //            raResp.result = false;
        //            raResp.message = "Company information not found!";
        //            raResp.dob = null;
        //            raResp.doc_id_number = null;
        //            raResp.saf_status = false;
        //            return raResp;
        //        }

        //        if (dbssRespObj["included"][0]["attributes"]["id-document-type"] == null
        //             || String.IsNullOrEmpty((string)dbssRespObj["included"][0]["attributes"]["id-document-type"]))
        //        {
        //            raResp.result = false;
        //            raResp.message = "id-document-type not found!";
        //            raResp.dob = null;
        //            raResp.doc_id_number = null;
        //            raResp.saf_status = false;
        //            return raResp;
        //        }

        //        string idDocumentType = (string)dbssRespObj["included"][0]["attributes"]["id-document-type"];

        //        if (idDocumentType != "national_id"
        //            && idDocumentType != "smart_national_id")
        //        {
        //            raResp.result = false;
        //            raResp.message = "Customer is not registered with National ID!";
        //            raResp.dob = null;
        //            raResp.doc_id_number = null;
        //            raResp.saf_status = false;
        //            return raResp;
        //        }
        //        else if ((bool)dbssRespObj["included"][0]["attributes"]["is-company"] == true)
        //        {
        //            raResp.result = false;
        //            raResp.message = "This MSISDN is not eligible for individual SIM replacement.";
        //            raResp.dob = null;
        //            raResp.doc_id_number = null;
        //            raResp.saf_status = false;
        //            return raResp;
        //        }
        //        else
        //        {
        //            raResp.saf_status = true;//[Has_SAF] By deafult this value is true. At this moment we are not checking saf status because DBSS API   
        //            raResp.customer_id = String.Empty;
        //            raResp.dob = (string)dbssRespObj["included"][0]["attributes"]["date-of-birth"];
        //            raResp.doc_id_number = (string)dbssRespObj["included"][0]["attributes"]["id-document-number"];
        //            raResp.dbss_subscription_id = (int)dbssRespObj["data"]["id"];
        //            raResp.old_sim_number = (string)dbssRespObj["included"][1]["attributes"]["icc"];
        //            raResp.old_sim_type = (string)dbssRespObj["included"][1]["attributes"]["sim-type"];
        //            raResp.result = true;
        //            raResp.message = MessageCollection.MSISDNValid;
        //            return raResp;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
        public IndividualSIMReplacementMSISDNCheckResponse StarTrekSIMReplacementParsing(JObject dbssRespObj)
        {
            var raResp = new IndividualSIMReplacementMSISDNCheckResponse();
            try
            {
                var data = dbssRespObj["data"];
                if (data == null || !data.HasValues)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.SIMReplNoDataFound;
                    return raResp;
                }

                var attributes = data["attributes"];
                string? status = (string?)attributes?["status"];

                if (string.IsNullOrEmpty(status))
                {
                    raResp.result = false;
                    raResp.message = "Msisdn status not found!";
                    return raResp;
                }

                if (status == "terminated")
                {
                    raResp.result = false;
                    raResp.message = "Msisdn is not valid for SIM replacemnt!";
                    return raResp;
                }

                if (status != "active" && status != "idle")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNStatusNotActiveOrIdle;
                    return raResp;
                }

                var included = dbssRespObj["included"];
                if (included == null || included.Count() is not (2 or 3))
                {
                    raResp.result = false;
                    raResp.message = "Data not found in include field!";
                    return raResp;
                }

                if (data["id"] == null)
                {
                    raResp.result = false;
                    raResp.message = "Subscription ID field empty!";
                    return raResp;
                }

                var attr0 = included[0]?["attributes"];
                var attr1 = included[1]?["attributes"];

                if (attr0 == null || attr1 == null)
                {
                    raResp.result = false;
                    raResp.message = "Data not found in include field!";
                    return raResp;
                }

                if (string.IsNullOrEmpty((string?)attr1["icc"]))
                {
                    raResp.result = false;
                    raResp.message = "Old SIM number not found!";
                    return raResp;
                }

                if (string.IsNullOrEmpty((string?)attr1["sim-type"]))
                {
                    raResp.result = false;
                    raResp.message = "sim-type not found!";
                    return raResp;
                }

                if (attr0["is-company"] == null || attr0["id-document-type"] == null)
                {
                    raResp.result = false;
                    raResp.message = "Company or ID document type info not found!";
                    return raResp;
                }

                string idDocumentType = (string?)attr0["id-document-type"] ?? "";
                bool isCompany = (bool?)attr0["is-company"] ?? false;

                if (idDocumentType != "national_id" && idDocumentType != "smart_national_id")
                {
                    raResp.result = false;
                    raResp.message = "Customer is not registered with National ID!";
                    return raResp;
                }

                if (isCompany)
                {
                    raResp.result = false;
                    raResp.message = "This MSISDN is not eligible for individual SIM replacement.";
                    return raResp;
                }

                // ✅ Success case
                raResp.saf_status = true;
                raResp.customer_id = string.Empty;
                raResp.dob = (string?)attr0["date-of-birth"] ?? "";
                raResp.doc_id_number = (string?)attr0["id-document-number"] ?? "";
                raResp.dbss_subscription_id = (int?)data["id"] ?? 0;
                raResp.old_sim_number = (string?)attr1["icc"] ?? "";
                raResp.old_sim_type = (string?)attr1["sim-type"] ?? "";
                raResp.result = true;
                raResp.message = MessageCollection.MSISDNValid;
                return raResp;
            }
            catch (Exception)
            {
                throw; // Preserves the stack trace
            }
        }
        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNSTartTrekOnline(UnpairedMSISDNCheckRequestOnline msisdnCheckReqest, string reservation_id, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }
                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);
                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNSTartTrekOnline");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "DBSS Error: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = UnpairedMSISDNResPargingOnline(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }
                else
                {
                    if (!msisdnResp.reservation_id.Equals(reservation_id))
                    {
                        raRespModel.isError = true;
                        raRespModel.message = "The reservation id is not matched with DBSS!";
                        return raRespModel;
                    }
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }


                var simResp = await StarTrekCheckSIMNumber(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");


                if (simResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = simResp.message;
                    return raRespModel;
                }

                Datas datas = new Datas();
                datas.isEsim = 0;
                datas.request_id = " ";
                datas.reservation_id = reservation_id;

                raRespModel.isError = false;
                raRespModel.data = new Datas()
                {
                    reservation_id = reservation_id
                };
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                return raRespModel;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                raRespModel.isError = true;
                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNSTartTrek(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNSTartTrek");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "DBSS Error: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = UnpairedMSISDNReqParsing(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }


                var simResp = await StarTrekCheckSIMNumber(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
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
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                raRespModel.isError = true;
                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNSTartTrekV2(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }
                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);
                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNSTartTrekV2");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "DBSS Error: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = UnpairedMSISDNReqParsingV2(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }

                var simResp = await StarTrekCheckSIMNumberV2(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
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
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                raRespModel.isError = true;
                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        public UnpairedMSISDNStartrekCheckResponse UnpairedMSISDNReqParsing(JObject dbssRespObj, string retailer_id)
        {
            UnpairedMSISDNStartrekCheckResponse raResp = new UnpairedMSISDNStartrekCheckResponse();
            try
            {
                string? status = string.Empty;
                string? reserved_for = string.Empty;
                int stockId = 0;

                if (dbssRespObj["data"]?["attributes"] != null)
                {
                    status = dbssRespObj["data"]?["attributes"]?["status"]?.Value<string>();
                    stockId = dbssRespObj["data"]?["attributes"]?["stock"]?.Value<int>() ?? 0;
                    reserved_for = dbssRespObj["data"]?["attributes"]?["reserved-for"]?.Value<string>();
                }

                if (stockId != 33)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StockIDMismatch;
                    return raResp;
                }
                if (string.IsNullOrEmpty(reserved_for))
                {
                    if (status == "available")
                    {
                        raResp = ValidateCherishedNumer(dbssRespObj, retailer_id);
                        raResp.stock_id = stockId;
                        raResp.reservation_id = "";
                        return raResp;
                    }
                    else if (status == "in_use")
                    {
                        raResp.result = false;
                        raResp.message = MessageCollection.MSISDNInUse;
                        return raResp;
                    }
                    else
                    {
                        raResp.result = false;
                        raResp.message = "MSISDN is invalid.";
                        return raResp;
                    }
                }
                else
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StarTrekNotEligible;
                    return raResp;

                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public UnpairedMSISDNStartrekCheckResponse UnpairedMSISDNReqParsingV2(JObject dbssRespObj, string retailer_id)
        {
            var raResp = new UnpairedMSISDNStartrekCheckResponse();
            try
            {
                string? status = string.Empty;
                string? reserved_for = string.Empty;
                int stockId = 0;
                string category_config = SettingsValues.GetStockNotAllowFromRyze();
                string[] configValue = category_config.Contains(",")
                    ? category_config.Split(',')
                    : category_config.Split(' ');

                status = dbssRespObj["data"]?["attributes"]?["status"]?.Value<string>();
                stockId = dbssRespObj["data"]?["attributes"]?["stock"]?.Value<int>() ?? 0;
                reserved_for = dbssRespObj["data"]?["attributes"]?["reserved-for"]?.Value<string>();

                if (configValue.Any(x => x == stockId.ToString()))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StockIDMismatch;
                    return raResp;
                }

                if (!string.IsNullOrEmpty(reserved_for))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StarTrekNotEligible;
                    return raResp;
                }

                if (string.IsNullOrEmpty(reserved_for) && status == "available")
                {
                    raResp = ValidateCherishedNumer(dbssRespObj, retailer_id);
                    raResp.stock_id = stockId;
                    raResp.reservation_id = "";
                    return raResp;
                }
                else if (status == "in_use")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNInUse;
                    return raResp;
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "MSISDN is invalid.";
                    return raResp;
                }
            }
            catch (Exception)
            {
                throw; // Keep stack trace
            }
        }
        //public UnpairedMSISDNStartrekCheckResponse UnpairedMSISDNReqParsing(JObject dbssRespObj, string retailer_id)
        //{
        //    UnpairedMSISDNStartrekCheckResponse raResp = new UnpairedMSISDNStartrekCheckResponse();
        //    try
        //    {
        //        string status = String.Empty;
        //        string reserved_for = String.Empty;
        //        int stockId = 0;
        //        string retailer_code = String.Empty;
        //        string number_category = String.Empty;
        //        string category_config = String.Empty;
        //        string[] cofigValue = null;

        //        if (dbssRespObj["data"] != null)
        //        {
        //            if (dbssRespObj["data"]["attributes"] != null)
        //            {
        //                if (!String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["status"])
        //                    && !String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["stock"]))
        //                {
        //                    status = (string)dbssRespObj["data"]["attributes"]["status"];
        //                    stockId = (int)dbssRespObj["data"]["attributes"]["stock"];
        //                    reserved_for = (string)dbssRespObj["data"]["attributes"]["reserved-for"];
        //                }
        //            }
        //        }
        //        if (stockId != 33)
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.StockIDMismatch;
        //            return raResp;
        //        }
        //        if (!String.IsNullOrEmpty(reserved_for))
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.StarTrekNotEligible;
        //            return raResp;
        //        }
        //        if (String.IsNullOrEmpty(reserved_for) && status == "available")
        //        {
        //            raResp = ValidateCherishedNumer(dbssRespObj, retailer_id);
        //            raResp.stock_id = stockId;
        //            raResp.reservation_id = reserved_for;
        //            return raResp;
        //        }
        //        else if (status == "in_use")
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.MSISDNInUse;
        //            return raResp;
        //        }
        //        else
        //        {
        //            raResp.result = false;
        //            raResp.message = "MSISDN is invalid.";
        //            return raResp;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //}
        //public UnpairedMSISDNStartrekCheckResponse UnpairedMSISDNReqParsingV2(JObject dbssRespObj, string retailer_id)
        //{
        //    UnpairedMSISDNStartrekCheckResponse raResp = new UnpairedMSISDNStartrekCheckResponse();
        //    try
        //    {
        //        string status = String.Empty;
        //        string reserved_for = String.Empty;
        //        int stockId = 0;
        //        string retailer_code = String.Empty;
        //        string number_category = String.Empty;
        //        string category_config = String.Empty;
        //        string[] cofigValue = null;

        //        category_config = SettingsValues.GetStockNotAllowFromRyze();

        //        if (category_config.Contains(","))
        //        {
        //            cofigValue = category_config.Split(',');
        //        }
        //        else
        //        {
        //            cofigValue = category_config.Split(' ');
        //        }

        //        if (dbssRespObj["data"] != null)
        //        {
        //            if (dbssRespObj["data"]["attributes"] != null)
        //            {
        //                if (!String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["status"])
        //                    && !String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["stock"]))
        //                {
        //                    status = (string)dbssRespObj["data"]["attributes"]["status"];
        //                    stockId = (int)dbssRespObj["data"]["attributes"]["stock"];
        //                    reserved_for = (string)dbssRespObj["data"]["attributes"]["reserved-for"];
        //                }
        //            }
        //        }
        //        if (cofigValue.Any(x => x == stockId.ToString()))
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.StockIDMismatch;
        //            return raResp;
        //        }
        //        if (!String.IsNullOrEmpty(reserved_for))
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.StarTrekNotEligible;
        //            return raResp;
        //        }
        //        if (String.IsNullOrEmpty(reserved_for) && status == "available")
        //        {
        //            raResp = ValidateCherishedNumer(dbssRespObj, retailer_id);
        //            raResp.stock_id = stockId;
        //            raResp.reservation_id = reserved_for;
        //            return raResp;
        //        }
        //        else if (status == "in_use")
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.MSISDNInUse;
        //            return raResp;
        //        }
        //        else
        //        {
        //            raResp.result = false;
        //            raResp.message = "MSISDN is invalid.";
        //            return raResp;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //}
        public async Task<RACommonResponseRevampResp2> ValidateUnpairedMSISDNSTartTrekTestOnline(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevampResp2 raRespModel = new RACommonResponseRevampResp2();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);
                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNSTartTrekTestOnline");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "DBSS Error: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = UnpairedMSISDNResPargingOnline(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }
                raRespModel.reservationId = msisdnResp.reservation_id;

                return raRespModel;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                raRespModel.isError = true;
                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        //public UnpairedMSISDNStartrekCheckResponse UnpairedMSISDNResPargingOnline(JObject dbssRespObj, string retailer_id)
        //{
        //    UnpairedMSISDNStartrekCheckResponse raResp = new UnpairedMSISDNStartrekCheckResponse();
        //    try
        //    {
        //        string status = String.Empty;
        //        string reserved_for = String.Empty;
        //        int stockId = 0;
        //        string retailer_code = String.Empty;
        //        string number_category = String.Empty;
        //        string category_config = String.Empty;
        //        string[] cofigValue = null;

        //        if (dbssRespObj["data"] != null)
        //        {
        //            if (dbssRespObj["data"]["attributes"] != null)
        //            {
        //                if (!String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["status"])
        //                    && !String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["stock"]))
        //                {
        //                    status = (string)dbssRespObj["data"]["attributes"]["status"];
        //                    stockId = (int)dbssRespObj["data"]["attributes"]["stock"];
        //                    reserved_for = (string)dbssRespObj["data"]["attributes"]["reserved-for"];
        //                }
        //            }
        //        }
        //        if (stockId != 33)
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.StockIDMismatch;
        //            return raResp;
        //        }
        //        if (String.IsNullOrEmpty(reserved_for))
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.StarTrekNotEligibleOnline;
        //            return raResp;
        //        }
        //        if (!String.IsNullOrEmpty(reserved_for) && status == "available")
        //        {
        //            raResp = ValidateCherishedNumer(dbssRespObj, retailer_id);
        //            raResp.stock_id = stockId;
        //            raResp.reservation_id = reserved_for;
        //            return raResp;
        //        }
        //        else if (status == "in_use")
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.MSISDNInUse;
        //            return raResp;
        //        }
        //        else
        //        {
        //            raResp.result = false;
        //            raResp.message = "MSISDN is invalid.";
        //            return raResp;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //}
        public UnpairedMSISDNStartrekCheckResponse UnpairedMSISDNResPargingOnline(JObject dbssRespObj, string retailer_id)
        {
            var raResp = new UnpairedMSISDNStartrekCheckResponse();

            try
            {
                string? status = dbssRespObj["data"]?["attributes"]?["status"]?.Value<string>();
                int stockId = dbssRespObj["data"]?["attributes"]?["stock"]?.Value<int>() ?? 0;
                string? reserved_for = dbssRespObj["data"]?["attributes"]?["reserved-for"]?.Value<string>();

                if (stockId != 33)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StockIDMismatch;
                    return raResp;
                }

                if (string.IsNullOrEmpty(reserved_for))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StarTrekNotEligibleOnline;
                    return raResp;
                }

                if (!string.IsNullOrEmpty(reserved_for) && status == "available")
                {
                    raResp = ValidateCherishedNumer(dbssRespObj, retailer_id);
                    raResp.stock_id = stockId;
                    raResp.reservation_id = reserved_for;
                    return raResp;
                }
                else if (status == "in_use")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNInUse;
                    return raResp;
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "MSISDN is invalid.";
                    return raResp;
                }
            }
            catch (Exception)
            {
                throw; // Keeps stack trace
            }
        }
        //public UnpairedMSISDNStartrekCheckResponse ValidateCherishedNumer(JObject dbssRespObj, string retailer_id)
        //{

        //    UnpairedMSISDNStartrekCheckResponse raResp = new UnpairedMSISDNStartrekCheckResponse();

        //    string status = String.Empty;
        //    int stockId = 0;
        //    string retailer_code = String.Empty;
        //    string number_category = String.Empty;
        //    string category_config = SettingsValues.GetNumberCategory();
        //    string[] cofigValue = null;

        //    try
        //    {
        //        if (dbssRespObj["data"] != null)
        //        {
        //            if (dbssRespObj["data"]["attributes"] != null)
        //            {
        //                if (category_config.Contains(","))
        //                {
        //                    cofigValue = category_config.Split(',');
        //                }
        //                else
        //                {
        //                    cofigValue = category_config.Split(' ');
        //                }

        //                if (dbssRespObj["data"]["attributes"]["number-category"] != null)
        //                {
        //                    retailer_code = dbssRespObj["data"]["attributes"]["salesman-id"].ToString();
        //                    number_category = dbssRespObj["data"]["attributes"]["number-category"].ToString();

        //                    if (!String.IsNullOrEmpty(retailer_code))
        //                    {
        //                        if (retailer_code.Length < 6)
        //                        {
        //                            char pad = '0';
        //                            retailer_code = retailer_code.PadLeft(6, pad);
        //                        }
        //                    }

        //                    if (!String.IsNullOrEmpty(retailer_code) && !String.IsNullOrEmpty(number_category) && cofigValue.Any(x => x != number_category)) // from Web.config 
        //                    {
        //                        if (retailer_id.Equals(retailer_code))
        //                        {
        //                            raResp.result = true;
        //                            raResp.message = MessageCollection.ValidCherishedNumber;
        //                        }
        //                        else
        //                        {
        //                            raResp.result = false;
        //                            raResp.message = MessageCollection.InvalidCherishedNumber;
        //                        }
        //                    }
        //                    else if (String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x == number_category))
        //                    {
        //                        raResp.result = true;
        //                        raResp.message = MessageCollection.ValidCherishedNumber;
        //                    }
        //                    else if (!String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x == number_category))
        //                    {
        //                        raResp.result = true;
        //                        raResp.message = MessageCollection.ValidCherishedNumber; ;
        //                    }
        //                    else if (String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x != number_category))
        //                    {
        //                        raResp.result = false;
        //                        raResp.message = "MSISDN not tagged with this Retailer (ID: " + retailer_id + ")";
        //                    }
        //                    else
        //                    {
        //                        raResp.result = false;
        //                        raResp.message = "MSISDN is not Valid.";
        //                    }
        //                }
        //                else
        //                {
        //                    raResp.result = false;
        //                    raResp.message = "Invalid MSISDN Category!";
        //                }
        //            }
        //            else
        //            {
        //                raResp.result = false;
        //                raResp.message = "No Data found!";
        //            }
        //        }
        //        else
        //        {
        //            raResp.result = false;
        //            raResp.message = "No Data found!";
        //        }

        //        return raResp;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
        public UnpairedMSISDNStartrekCheckResponse ValidateCherishedNumer(JObject dbssRespObj, string retailer_id)
        {
            var raResp = new UnpairedMSISDNStartrekCheckResponse();

            try
            {
                var attributes = dbssRespObj["data"]?["attributes"];
                if (attributes == null)
                {
                    raResp.result = false;
                    raResp.message = "No Data found in DBSS API!";
                    return raResp;
                }

                string? number_category = attributes["number-category"]?.Value<string>();
                string? retailer_code = attributes["salesman-id"]?.Value<string>();

                if (string.IsNullOrEmpty(number_category))
                {
                    raResp.result = false;
                    raResp.message = "Invalid MSISDN Category!";
                    return raResp;
                }

                // Clean and parse configured categories
                string category_config = SettingsValues.GetNumberCategory();
                var configValues = category_config.Contains(",")
                    ? category_config.Split(',')
                    : category_config.Split(' ');

                if (!string.IsNullOrEmpty(retailer_code) && retailer_code.Length < 6)
                {
                    retailer_code = retailer_code.PadLeft(6, '0');
                }

                bool isInConfigList = configValues.Any(x => x == number_category);
                bool isNotInConfigList = configValues.Any(x => x != number_category);

                // Main validation logic
                if (!string.IsNullOrEmpty(retailer_code) && isNotInConfigList)
                {
                    if (retailer_id.Equals(retailer_code))
                    {
                        raResp.result = true;
                        raResp.message = MessageCollection.ValidCherishedNumber;
                    }
                    else
                    {
                        raResp.result = false;
                        raResp.message = MessageCollection.InvalidCherishedNumber;
                    }
                }
                else if (isInConfigList)
                {
                    raResp.result = true;
                    raResp.message = MessageCollection.ValidCherishedNumber;
                }
                else if (string.IsNullOrEmpty(retailer_code) && isNotInConfigList)
                {
                    raResp.result = false;
                    raResp.message = $"MSISDN not tagged with this Retailer (ID: {retailer_id})";
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "MSISDN is not Valid.";
                }

                return raResp;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<RACommonResponse> StarTrekCheckSIMNumber(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = _blJson.GetGenericJsonData(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "StarTrekCheckSIMNumber");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }
                raResp = SIMValidationParsing(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;

                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "StarTrekCheckSIMNumber";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }
        public async Task<RACommonResponse> StarTrekCheckSIMNumberV2(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = _blJson.GetGenericJsonData(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "StarTrekCheckSIMNumberV2");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }
                raResp = SIMValidationParsingV2(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "StarTrekCheckSIMNumberV2";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }
        public RACommonResponse SIMValidationParsing(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null
                    && String.IsNullOrEmpty(dbssResp?["data"]?["status"]?.ToString())
                    && String.IsNullOrEmpty(dbssResp?["data"]?["logical_inventory_status"]?.ToString())
                    && String.IsNullOrEmpty(dbssResp?["data"]?["physical_inventory_status"]?.ToString()))
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (dbssResp?["data"]?["status"]?.ToString().ToLower() == "failed")
                {
                    var errorMessage = dbssResp?["data"]?["error_message"]?.ToString();
                    response.result = false;
                    response.message = !string.IsNullOrWhiteSpace(errorMessage) ? errorMessage : MessageCollection.SIMIsNotInInventory;

                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower()
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower()) /*ryz-esim*/
                {
                    response.result = false;
                    response.message = "Incorrect Product!";
                    return response;
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaidStarTrek.ToLower() /*"ryz-prepaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                return response;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public RACommonResponse SIMValidationParsingV2(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null
                    && String.IsNullOrEmpty(dbssResp?["data"]?["status"]?.ToString())
                    && String.IsNullOrEmpty(dbssResp?["data"]?["logical_inventory_status"]?.ToString())
                    && String.IsNullOrEmpty(dbssResp?["data"]?["physical_inventory_status"]?.ToString()))
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (dbssResp?["data"]?["status"]?.ToString().ToLower() == "failed")
                {
                    var errorMessage = dbssResp?["data"]?["error_message"]?.ToString();
                    response.result = false;
                    response.message = !string.IsNullOrWhiteSpace(errorMessage) ? errorMessage : MessageCollection.SIMIsNotInInventory;
                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESimStarTrek.ToLower()
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower()
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower())
                {
                    response.result = false;
                    response.message = "This is not physical SIM!";
                    return response;
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower()
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLower())
                {
                    response.result = false;
                    response.message = "Incorrect Product!";
                    return response;
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower()
                        && (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower()
                        || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaidStarTrek.ToLower()))
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }

                // ✅ Fallback path for future safety
                response.result = false;
                response.message = MessageCollection.SIMInvalid;
                return response;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public RACommonResponse SIMValidationParsingESIM(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null
                    && String.IsNullOrEmpty(dbssResp?["data"]?["status"]?.ToString())
                    && String.IsNullOrEmpty(dbssResp?["data"]?["logical_inventory_status"]?.ToString())
                    && String.IsNullOrEmpty(dbssResp?["data"]?["physical_inventory_status"]?.ToString()))
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (dbssResp?["data"]?["status"]?.ToString().ToLower() == "failed")
                {
                    var errorMessage = dbssResp?["data"]?["error_message"]?.ToString();
                    response.result = false;
                    response.message = !string.IsNullOrWhiteSpace(errorMessage) ? errorMessage : MessageCollection.SIMIsNotInInventory;

                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaidStarTrek.ToLower()) /*ryz-prepaid*/
                {
                    response.result = false;
                    response.message = "Incorrect Product!";
                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/
                    && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESimStarTrek.ToLower() /*"ryz-esim"*/)
                {
                    response.result = true;
                    response.message = MessageCollection.SIMValid;
                    return response;
                }
                else
                {
                    response.result = false;
                    response.message = MessageCollection.SIMInvalid;
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public RACommonResponse SIMValidationParsingESIMV2(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null
                    && String.IsNullOrEmpty(dbssResp?["data"]?["status"]?.ToString())
                    && String.IsNullOrEmpty(dbssResp?["data"]?["logical_inventory_status"]?.ToString())
                    && String.IsNullOrEmpty(dbssResp?["data"]?["physical_inventory_status"]?.ToString()))
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (dbssResp?["data"]?["status"]?.ToString().ToLower() == "failed")
                {
                    var errorMsg = dbssResp?["data"]?["error_message"]?.ToString();
                    response.result = false;
                    response.message = string.IsNullOrWhiteSpace(errorMsg)
                        ? MessageCollection.SIMIsNotInInventory
                        : errorMsg;

                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower() /*postpaid*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaidStarTrek.ToLower() /*ryz-prepaid*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower() /*Prepaid*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLower()) /*SIM_SWAP*/
                {
                    response.result = false;
                    response.message = "This is not eSIM!";
                    return response;
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower() /*E_SIM_SWAP*/)
                {
                    response.result = false;
                    response.message = "Incorrect Product!";
                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/
                    && (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower() /*"esim"*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESimStarTrek.ToLower() /*ryz-esim*/))
                {
                    response.result = true;
                    response.message = MessageCollection.SIMValid;
                    return response;
                }
                else
                {
                    response.result = false;
                    response.message = MessageCollection.SIMInvalid;
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public RACommonResponse SIMValidationParsingESIMV3(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null
                    && String.IsNullOrEmpty(dbssResp?["data"]?["status"]?.ToString())
                    && String.IsNullOrEmpty(dbssResp?["data"]?["logical_inventory_status"]?.ToString())
                    && String.IsNullOrEmpty(dbssResp?["data"]?["physical_inventory_status"]?.ToString()))
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (dbssResp?["data"]?["status"]?.ToString().ToLower() == "failed")
                {
                    var errorMsg = dbssResp?["data"]?["error_message"]?.ToString();
                    response.result = false;
                    response.message = string.IsNullOrWhiteSpace(errorMsg)
                        ? MessageCollection.SIMIsNotInInventory
                        : errorMsg;

                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaidStarTrek.ToLower()/*ryz-prepaid*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLower() /*sim_swap*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower()/*Prepaid*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower()/*Postpaid*/)
                {
                    response.result = false;
                    response.message = "This is not eSIM!";
                    return response;
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower()/*e-sim*/)
                {
                    response.result = false;
                    response.message = "Incorrect Product!";
                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/
                    && (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower() /*"e_sim_swap"*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESimStarTrek.ToLower() /*ryze_esim*/))
                {
                    response.result = true;
                    response.message = MessageCollection.SIMValid;
                    return response;
                }
                else
                {
                    response.result = false;
                    response.message = MessageCollection.SIMInvalid;
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNESIM(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNESIM");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "DBSS Error: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = UnpairedMSISDNReqParsing(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }

                var simResp = await CheckSIMNumber(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
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
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                raRespModel.isError = true;
                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "StartTrekValidateUnpairedMSISDN_ESIM";

                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNESIMV2(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);
                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNESIMV2");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = UnpairedMSISDNReqParsingV2(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }

                var simResp = await CheckSIMNumberV2(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
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
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                raRespModel.isError = true;
                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "StartTrekValidateUnpairedMSISDNESIMV2";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        public async Task<RACommonResponse> CheckSIMNumber(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = _blJson.GetGenericJsonData(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumber");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }

                raResp = SIMValidationParsingESIM(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumberStarTrek";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }
        public async Task<RACommonResponse> CheckSIMNumberV2(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = _blJson.GetGenericJsonData(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumberV2");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }

                raResp = SIMValidationParsingESIMV2(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;

                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description
                                                                                : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumberStarTrekV2";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }
        public async Task<RACommonResponse> CheckSIMNumberV3(SIMNumberCheckRequest simNumberCheckReqest, int purposeOfSIMCheck, bool? isPaired, int? simCategory, string old_sim_type)
        {
            RACommonResponse raResp = new RACommonResponse();
            string apiUrl = "";
            string? txtResp = "";
            SIMValidationRequestRootobject dbssReqModel = new SIMValidationRequestRootobject();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();
            try
            {
                dbssReqModel = _raToDBssParse.ValidateSIMReqParsing2(simNumberCheckReqest);

                apiUrl = String.Format(PostAPICollection.CheckSIM);

                log.req_blob = _blJson.GetGenericJsonData(dbssReqModel);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpPostRequest(dbssReqModel, apiUrl, "CheckSIMNumberV3");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                if (dbssResp["data"] == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                }

                raResp = SIMValidationParsingESIMV3(dbssResp, purposeOfSIMCheck, simCategory == null ? null : simCategory, isPaired, old_sim_type);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;

                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                raResp.result = false;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? FixedValueCollection.DBSSError + error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = simNumberCheckReqest.purpose_number;
                log.user_id = simNumberCheckReqest.retailer_id;
                log.method_name = "CheckSIMNumberStarTrekV3";
                log.msisdn = _bllLog.FormatMSISDN(simNumberCheckReqest.msisdn);

                await _bllLog.RAToDBSSLog(log);
            }
            return raResp;
        }
        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNESIM_Online(UnpairedMSISDNCheckRequestOnline msisdnCheckReqest, string reservation_id, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);
                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNESIM_Online");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = UnpairedMSISDNResPargingOnline(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }
                else
                {
                    if (!msisdnResp.reservation_id.Equals(reservation_id))
                    {
                        raRespModel.isError = true;
                        raRespModel.message = "The reservation id is not matched with DBSS!";
                        return raRespModel;
                    }
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }

                var simResp = await CheckSIMNumber(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

                if (simResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = simResp.message;
                    return raRespModel;
                }
                raRespModel.isError = false;
                raRespModel.data = new Datas() { reservation_id = msisdnResp.reservation_id };
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                return raRespModel;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raRespModel.isError = true;
                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "StartTrekValidateUnpairedMSISDN_ESIM";

                await _bllLog.RAToDBSSLog(log);
            }
        }
        //public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNMNPSTartTrek(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        //{
        //    RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
        //    string apiUrl = "";
        //    string? txtResp = "";
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    JObject dbssResp = new JObject();
        //    BL_Json _blJson = new BL_Json();
        //    BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
        //    try
        //    {
        //        var dbssReqModel = _raToDBssParse.ValidateMSISDNReqParsing(msisdnCheckReqest);

        //        if (dbssReqModel.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
        //        {
        //            dbssReqModel = FixedValueCollection.MSISDNCountryCode + dbssReqModel;
        //        }

        //        apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, dbssReqModel);

        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl);

        //        try
        //        {
        //            log.req_time = DateTime.Now;
        //            dbssResp = (JObject)await _apiReq.HttpGetRequestForMNPPortIn(apiUrl, "ValidateUnpairedMSISDNMNPSTartTrek");
        //            log.res_time = DateTime.Now;
        //        }
        //        catch (WebException ex)
        //        {
        //            Log.Error(ex, "ExMessage");
        //            log.res_time = DateTime.Now;
        //            txtResp = Convert.ToString(ex.Message);
        //            log.res_blob = _blJson.GetGenericJsonData(dbssResp);


        //            if (ex.Status == WebExceptionStatus.ProtocolError)
        //            {
        //                var ErrorResponse = ex.Response as HttpWebResponse;
        //                if (ErrorResponse != null && (int)ErrorResponse.StatusCode == 404)
        //                {
        //                    //var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);
        //                    log.is_success = 1;
        //                    var simResp = await StarTrekCheckSIMNumber(new SIMNumberCheckRequest()
        //                    {
        //                        center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //                        distributor_code = string.Empty,
        //                        channel_name = msisdnCheckReqest.channel_name,
        //                        session_token = msisdnCheckReqest.session_token,
        //                        sim_number = msisdnCheckReqest.sim_number,
        //                        retailer_id = msisdnCheckReqest.retailer_id,
        //                        product_code = string.Empty,
        //                        inventory_id = msisdnCheckReqest.inventory_id,
        //                        msisdn = msisdnCheckReqest.mobile_number,
        //                        purpose_number = msisdnCheckReqest.purpose_number ?? ""
        //                    }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //                    if (simResp.result == false)
        //                    {
        //                        raRespModel.isError = true;
        //                        raRespModel.message = simResp.message;
        //                        return raRespModel;
        //                    }

        //                    raRespModel.isError = false;
        //                    raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

        //                    return raRespModel;
        //                }
        //                else
        //                {
        //                    throw;
        //                }
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        //======If DBSS api returnd success==========
        //        txtResp = Convert.ToString(dbssResp);
        //        log.res_blob = _blJson.GetGenericJsonData(dbssResp);

        //        log.is_success = 1;

        //        var msisdnResp2 = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);

        //        if (msisdnResp2.result == false)
        //        {
        //            raRespModel.isError = true;
        //            raRespModel.message = MessageCollection.MSISDNAlreadyExists;
        //            return raRespModel;
        //        }

        //        var simResp2 = await StarTrekCheckSIMNumber(new SIMNumberCheckRequest()
        //        {
        //            center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //            distributor_code = string.Empty,
        //            channel_name = msisdnCheckReqest.channel_name,
        //            session_token = msisdnCheckReqest.session_token,
        //            sim_number = msisdnCheckReqest.sim_number,
        //            retailer_id = msisdnCheckReqest.retailer_id,
        //            product_code = string.Empty,
        //            inventory_id = msisdnCheckReqest.inventory_id,
        //            msisdn = msisdnCheckReqest.mobile_number,
        //            purpose_number = msisdnCheckReqest.purpose_number ?? ""
        //        }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //        if (simResp2.result == false)
        //        {
        //            raRespModel.isError = true;
        //            raRespModel.message = simResp2.message;
        //            return raRespModel;
        //        }

        //        raRespModel.isError = false;
        //        raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

        //        return raRespModel;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        JObject jsonObject = JObject.Parse(ex.InnerException.Message);
        //        log.res_time = DateTime.Now;
        //        txtResp = Convert.ToString(ex.InnerException.Message);
        //        log.res_blob = _blJson.GetGenericJsonData(dbssResp);

        //        string statusValue = jsonObject?["errors"]?["status"]?.ToString();
        //        string title = jsonObject?["errors"]?["title"]?.ToString();

        //        if (!String.IsNullOrEmpty(statusValue) && (statusValue == "7001" || title == "Msisdn Not Found"))
        //        {
        //            var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);
        //            log.is_success = 1;
        //            var simResp = await StarTrekCheckSIMNumber(new SIMNumberCheckRequest()
        //            {
        //                center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //                distributor_code = string.Empty,
        //                channel_name = msisdnCheckReqest.channel_name,
        //                session_token = msisdnCheckReqest.session_token,
        //                sim_number = msisdnCheckReqest.sim_number,
        //                retailer_id = msisdnCheckReqest.retailer_id,
        //                product_code = string.Empty,
        //                inventory_id = msisdnCheckReqest.inventory_id,
        //                msisdn = msisdnCheckReqest.mobile_number,
        //                purpose_number = msisdnCheckReqest.purpose_number ?? ""
        //            }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //            if (simResp.result == false)
        //            {
        //                raRespModel.isError = true;
        //                raRespModel.message = simResp.message;
        //                return raRespModel;
        //            }

        //            raRespModel.isError = false;
        //            raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

        //            return raRespModel;
        //        }
        //        else
        //        {
        //            log.res_time = DateTime.Now;
        //            log.res_blob = _blJson.GetGenericJsonData(ex.InnerException.Message);

        //            try
        //            {
        //                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //                log.is_success = 0;
        //                log.error_code = error.error_code ?? String.Empty;
        //                log.error_source = error.error_source ?? String.Empty;
        //                log.message = error.error_description ?? String.Empty;

        //                raRespModel.isError = true;
        //                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //                return raRespModel;
        //            }
        //            catch (Exception)
        //            {
        //                raRespModel.isError = true;
        //                raRespModel.message = ex.InnerException.Message;
        //                return raRespModel;
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

        //        log.purpose_number = msisdnCheckReqest.purpose_number;
        //        log.user_id = msisdnCheckReqest.retailer_id;//userName
        //        log.method_name = "ValidateUnpairedMSISDNMNPSTartTrek";

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //}

        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNMNPSTartTrek(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            string apiUrl = "";
            string txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            JObject dbssResp = new JObject();
            BL_Json _blJson = new BL_Json();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();

            try
            {
                var dbssReqModel = _raToDBssParse.ValidateMSISDNReqParsing(msisdnCheckReqest);

                if (!string.IsNullOrEmpty(dbssReqModel) && !dbssReqModel.StartsWith(FixedValueCollection.MSISDNCountryCode))
                {
                    dbssReqModel = FixedValueCollection.MSISDNCountryCode + dbssReqModel;
                }

                var encodedMsisdn = Uri.EscapeDataString(dbssReqModel);
                apiUrl = string.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);
                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                try
                {
                    log.req_time = DateTime.Now;
                    dbssResp = (JObject)await _apiReq.HttpGetRequestForMNPPortIn(apiUrl, "ValidateUnpairedMSISDNMNPSTartTrek");
                    log.res_time = DateTime.Now;
                }
                catch (WebException ex)
                {
                    Log.Error(ex, "ExMessage");
                    log.res_time = DateTime.Now;
                    txtResp = ex.Message;
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        if (ex.Response is HttpWebResponse errorResponse && (int)errorResponse.StatusCode == 404)
                        {
                            log.is_success = 1;
                            return await HandleSIMCheck(msisdnCheckReqest, raRespModel);
                        }
                        throw;
                    }
                    throw;
                }

                // If DBSS response is successful
                txtResp = dbssResp.ToString();
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);
                log.is_success = 1;

                var msisdnResp2 = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);
                if (!msisdnResp2.result)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.MSISDNAlreadyExists;
                    return raRespModel;
                }

                return await HandleSIMCheck(msisdnCheckReqest, raRespModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;

                string jsonMessage = ex.InnerException?.Message ?? ex.Message;
                txtResp = jsonMessage;
                log.res_blob = _blJson.GetGenericJsonData(jsonMessage);

                try
                {
                    var jsonObject = JObject.Parse(jsonMessage);
                    string? statusValue = jsonObject?["errors"]?["status"]?.ToString();
                    string? title = jsonObject?["errors"]?["title"]?.ToString();

                    if (!string.IsNullOrEmpty(statusValue) && (statusValue == "7001" || title == "Msisdn Not Found"))
                    {
                        log.is_success = 1;
                        return await HandleSIMCheck(msisdnCheckReqest, raRespModel);
                    }
                }
                catch
                {
                    // If parsing fails, proceed to log as a general exception
                }

                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    log.is_success = 0;
                    log.error_code = error.error_code ?? string.Empty;
                    log.error_source = error.error_source ?? string.Empty;
                    log.message = error.error_custom_msg ?? error.error_description;

                    raRespModel.isError = true;
                    raRespModel.message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                    return raRespModel;
                }
                catch
                {
                    raRespModel.isError = true;
                    raRespModel.message = jsonMessage;
                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = nameof(ValidateUnpairedMSISDNMNPSTartTrek);

                await _bllLog.RAToDBSSLog(log);
            }
        }

        private async Task<RACommonResponseRevamp> HandleSIMCheck(UnpairedMSISDNCheckRequest request, RACommonResponseRevamp response)
        {
            var simResp = await StarTrekCheckSIMNumber(new SIMNumberCheckRequest
            {
                center_code = string.IsNullOrEmpty(request.center_code) ? "" : request.center_code,
                distributor_code = string.Empty,
                channel_name = request.channel_name,
                session_token = request.session_token,
                sim_number = request.sim_number,
                retailer_id = request.retailer_id,
                product_code = string.Empty,
                inventory_id = request.inventory_id,
                msisdn = request.mobile_number,
                purpose_number = request.purpose_number ?? ""
            }, (int)EnumPurposeOfSIMCheck.NewConnection, false, request.sim_category, "");

            if (!simResp.result)
            {
                response.isError = true;
                response.message = simResp.message;
                return response;
            }

            response.isError = false;
            response.message = MessageCollection.MSISDNandSIMBothValid;
            return response;
        }

        //public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNMNPSTartTrekV2(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        //{
        //    RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
        //    string apiUrl = "", txtResp = "";
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    JObject dbssResp = new JObject();
        //    BL_Json _blJson = new BL_Json();
        //    BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
        //    try
        //    {
        //        var dbssReqModel = _raToDBssParse.ValidateMSISDNReqParsing(msisdnCheckReqest);

        //        if (dbssReqModel.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
        //        {
        //            dbssReqModel = FixedValueCollection.MSISDNCountryCode + dbssReqModel;
        //        }

        //        apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, dbssReqModel);

        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl);

        //        try
        //        {
        //            log.req_time = DateTime.Now;
        //            dbssResp = (JObject)await _apiReq.HttpGetRequestForMNPPortIn(apiUrl, "ValidateUnpairedMSISDNMNPSTartTrekV2");
        //            log.res_time = DateTime.Now;
        //        }
        //        catch (WebException ex)
        //        {
        //            Log.Error(ex, "ExMessage");
        //            log.res_time = DateTime.Now;
        //            txtResp = Convert.ToString(ex.InnerException.Message);
        //            log.res_blob = _blJson.GetGenericJsonData(dbssResp);

        //            if (ex.Status == WebExceptionStatus.ProtocolError)
        //            {
        //                var ErrorResponse = ex.Response as HttpWebResponse;
        //                if (ErrorResponse != null && (int)ErrorResponse.StatusCode == 404)
        //                {
        //                    log.is_success = 1;
        //                    var simResp = await StarTrekCheckSIMNumberV2(new SIMNumberCheckRequest()
        //                    {
        //                        center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //                        distributor_code = string.Empty,
        //                        channel_name = msisdnCheckReqest.channel_name,
        //                        session_token = msisdnCheckReqest.session_token,
        //                        sim_number = msisdnCheckReqest.sim_number,
        //                        retailer_id = msisdnCheckReqest.retailer_id,
        //                        product_code = string.Empty,
        //                        inventory_id = msisdnCheckReqest.inventory_id,
        //                        msisdn = msisdnCheckReqest.mobile_number,
        //                        purpose_number = msisdnCheckReqest.purpose_number
        //                    }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //                    if (simResp.result == false)
        //                    {
        //                        raRespModel.isError = true;
        //                        raRespModel.message = simResp.message;
        //                        return raRespModel;
        //                    }

        //                    raRespModel.isError = false;
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

        //        var msisdnResp2 = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);

        //        if (msisdnResp2.result == false)
        //        {
        //            raRespModel.isError = true;
        //            raRespModel.message = MessageCollection.MSISDNAlreadyExists;
        //            return raRespModel;
        //        }

        //        var simResp2 = await StarTrekCheckSIMNumberV2(new SIMNumberCheckRequest()
        //        {
        //            center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //            distributor_code = string.Empty,
        //            channel_name = msisdnCheckReqest.channel_name,
        //            session_token = msisdnCheckReqest.session_token,
        //            sim_number = msisdnCheckReqest.sim_number,
        //            retailer_id = msisdnCheckReqest.retailer_id,
        //            product_code = string.Empty,
        //            inventory_id = msisdnCheckReqest.inventory_id,
        //            msisdn = msisdnCheckReqest.mobile_number,
        //            purpose_number = msisdnCheckReqest.purpose_number
        //        }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //        if (simResp2.result == false)
        //        {
        //            raRespModel.isError = true;
        //            raRespModel.message = simResp2.message;
        //            return raRespModel;
        //        }

        //        raRespModel.isError = false;
        //        raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

        //        return raRespModel;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        JObject jsonObject = JObject.Parse(ex.InnerException.Message);
        //        log.res_time = DateTime.Now;
        //        txtResp = Convert.ToString(ex.InnerException.Message);
        //        log.res_blob = _blJson.GetGenericJsonData(dbssResp);

        //        string statusValue = jsonObject?["errors"]?["status"]?.ToString();
        //        string title = jsonObject?["errors"]?["title"]?.ToString();

        //        if (!String.IsNullOrEmpty(statusValue) && (statusValue == "7001" || title == "Msisdn Not Found"))
        //        {
        //            var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);
        //            log.is_success = 1;
        //            var simResp = await StarTrekCheckSIMNumberV2(new SIMNumberCheckRequest()
        //            {
        //                center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //                distributor_code = string.Empty,
        //                channel_name = msisdnCheckReqest.channel_name,
        //                session_token = msisdnCheckReqest.session_token,
        //                sim_number = msisdnCheckReqest.sim_number,
        //                retailer_id = msisdnCheckReqest.retailer_id,
        //                product_code = string.Empty,
        //                inventory_id = msisdnCheckReqest.inventory_id,
        //                msisdn = msisdnCheckReqest.mobile_number,
        //                purpose_number = msisdnCheckReqest.purpose_number
        //            }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //            if (simResp.result == false)
        //            {
        //                raRespModel.isError = true;
        //                raRespModel.message = simResp.message;
        //                return raRespModel;
        //            }

        //            raRespModel.isError = false;
        //            raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

        //            return raRespModel;
        //        }
        //        else
        //        {
        //            log.res_time = DateTime.Now;
        //            log.res_blob = _blJson.GetGenericJsonData(ex.InnerException.Message);

        //            try
        //            {
        //                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //                log.is_success = 0;
        //                log.error_code = error.error_code ?? String.Empty;
        //                log.error_source = error.error_source ?? String.Empty;
        //                log.message = error.error_description ?? String.Empty;

        //                raRespModel.isError = true;
        //                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //                return raRespModel;
        //            }
        //            catch (Exception)
        //            {
        //                raRespModel.isError = true;
        //                raRespModel.message = ex.InnerException.Message;
        //                return raRespModel;
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

        //        log.purpose_number = msisdnCheckReqest.purpose_number;
        //        log.user_id = msisdnCheckReqest.retailer_id;//userName
        //        log.method_name = "ValidateUnpairedMSISDNMNPSTartTrekV2";

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //}

        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNMNPSTartTrekV2(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            string apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            JObject dbssResp = new JObject();
            BL_Json _blJson = new BL_Json();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();

            try
            {
                var dbssReqModel = _raToDBssParse.ValidateMSISDNReqParsing(msisdnCheckReqest);

                if (!dbssReqModel.StartsWith(FixedValueCollection.MSISDNCountryCode))
                {
                    dbssReqModel = FixedValueCollection.MSISDNCountryCode + dbssReqModel;
                }

                var encodedMsisdn = Uri.EscapeDataString(dbssReqModel);
                apiUrl = string.Format(GetAPICollection.UnpairedMSISDNValidation, dbssReqModel);
                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                try
                {
                    log.req_time = DateTime.Now;
                    dbssResp = (JObject)await _apiReq.HttpGetRequestForMNPPortIn(apiUrl, "ValidateUnpairedMSISDNMNPSTartTrekV2");
                    log.res_time = DateTime.Now;
                }
                catch (WebException ex)
                {
                    Log.Error(ex, "WebException");
                    log.res_time = DateTime.Now;
                    txtResp = ex.Message;
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    if (ex.Status == WebExceptionStatus.ProtocolError &&
                        ex.Response is HttpWebResponse errorResponse &&
                        (int)errorResponse.StatusCode == 404)
                    {
                        log.is_success = 1;
                        return await HandleSIMValidation(msisdnCheckReqest, raRespModel);
                    }

                    throw;
                }

                // If DBSS API returned success
                txtResp = dbssResp.ToString();
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);
                log.is_success = 1;

                var msisdnResp2 = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);

                if (!msisdnResp2.result)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.MSISDNAlreadyExists;
                    return raRespModel;
                }

                return await HandleSIMValidation(msisdnCheckReqest, raRespModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "General Exception");
                log.res_time = DateTime.Now;

                string exMessage = ex.InnerException?.Message ?? ex.Message;
                txtResp = exMessage;
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                try
                {
                    var jsonObject = JObject.Parse(exMessage);
                    string? statusValue = jsonObject?["errors"]?["status"]?.ToString();
                    string? title = jsonObject?["errors"]?["title"]?.ToString();

                    if (!string.IsNullOrEmpty(statusValue) && (statusValue == "7001" || title == "Msisdn Not Found"))
                    {
                        log.is_success = 1;
                        return await HandleSIMValidation(msisdnCheckReqest, raRespModel);
                    }
                }
                catch (JsonException)
                {
                    // Invalid JSON in exception, continue to general handler
                }

                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    log.is_success = 0;
                    log.error_code = error.error_code ?? string.Empty;
                    log.error_source = error.error_source ?? string.Empty;
                    log.message = error.error_custom_msg ?? error.error_description;

                    raRespModel.isError = true;
                    raRespModel.message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                    return raRespModel;
                }
                catch
                {
                    raRespModel.isError = true;
                    raRespModel.message = exMessage;
                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidateUnpairedMSISDNMNPSTartTrekV2";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        private async Task<RACommonResponseRevamp> HandleSIMValidation(UnpairedMSISDNCheckRequest msisdnCheckReqest, RACommonResponseRevamp raRespModel)
        {
            var simResp = await StarTrekCheckSIMNumberV2(new SIMNumberCheckRequest
            {
                center_code = msisdnCheckReqest.center_code ?? "",
                distributor_code = string.Empty,
                channel_name = msisdnCheckReqest.channel_name,
                session_token = msisdnCheckReqest.session_token,
                sim_number = msisdnCheckReqest.sim_number,
                retailer_id = msisdnCheckReqest.retailer_id,
                product_code = string.Empty,
                inventory_id = msisdnCheckReqest.inventory_id,
                msisdn = msisdnCheckReqest.mobile_number,
                purpose_number = msisdnCheckReqest.purpose_number ?? ""
            }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

            if (!simResp.result)
            {
                raRespModel.isError = true;
                raRespModel.message = simResp.message;
                return raRespModel;
            }

            raRespModel.isError = false;
            raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
            return raRespModel;
        }


        //public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNMNPSTartTrekesim(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        //{
        //    RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
        //    string apiUrl = "", txtResp = "";
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    JObject dbssResp = new JObject();
        //    BL_Json _blJson = new BL_Json();
        //    BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
        //    try
        //    {
        //        var dbssReqModel = _raToDBssParse.ValidateMSISDNReqParsing(msisdnCheckReqest);

        //        if (dbssReqModel.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
        //        {
        //            dbssReqModel = FixedValueCollection.MSISDNCountryCode + dbssReqModel;
        //        }

        //        apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, dbssReqModel);

        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl);

        //        try
        //        {
        //            log.req_time = DateTime.Now;
        //            dbssResp = (JObject)await _apiReq.HttpGetRequestForMNPPortIn(apiUrl, "ValidateUnpairedMSISDNMNPSTartTrekesim");
        //            log.res_time = DateTime.Now;
        //        }
        //        catch (WebException ex)
        //        {
        //            Log.Error(ex, "ExMessage");
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
        //                    var simResp = await CheckSIMNumber(new SIMNumberCheckRequest()
        //                    {
        //                        center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //                        distributor_code = "",
        //                        channel_name = msisdnCheckReqest.channel_name,
        //                        session_token = msisdnCheckReqest.session_token,
        //                        sim_number = msisdnCheckReqest.sim_number,
        //                        retailer_id = msisdnCheckReqest.retailer_id,
        //                        product_code = "",
        //                        inventory_id = msisdnCheckReqest.inventory_id,
        //                        msisdn = msisdnCheckReqest.mobile_number,
        //                        purpose_number = msisdnCheckReqest.purpose_number
        //                    }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //                    if (simResp.result == false)
        //                    {
        //                        raRespModel.isError = true;
        //                        raRespModel.message = simResp.message;
        //                        return raRespModel;
        //                    }

        //                    raRespModel.isError = false;
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

        //        var msisdnResp2 = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);

        //        if (msisdnResp2.result == false)
        //        {
        //            raRespModel.isError = true;
        //            raRespModel.message = MessageCollection.MSISDNAlreadyExists;
        //            return raRespModel;
        //        }

        //        var simResp2 = await CheckSIMNumber(new SIMNumberCheckRequest()
        //        {
        //            center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //            distributor_code = "",
        //            channel_name = msisdnCheckReqest.channel_name,
        //            session_token = msisdnCheckReqest.session_token,
        //            sim_number = msisdnCheckReqest.sim_number,
        //            retailer_id = msisdnCheckReqest.retailer_id,
        //            product_code = "",
        //            inventory_id = msisdnCheckReqest.inventory_id,
        //            msisdn = msisdnCheckReqest.mobile_number,
        //            purpose_number = msisdnCheckReqest.purpose_number
        //        }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //        if (simResp2.result == false)
        //        {
        //            raRespModel.isError = true;
        //            raRespModel.message = simResp2.message;
        //            return raRespModel;
        //        }

        //        raRespModel.isError = false;
        //        raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

        //        return raRespModel;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        JObject jsonObject = JObject.Parse(ex.InnerException.Message);
        //        log.res_time = DateTime.Now;
        //        txtResp = Convert.ToString(ex.Message);
        //        log.res_blob = _blJson.GetGenericJsonData(dbssResp);

        //        string statusValue = jsonObject?["errors"]?["status"]?.ToString();
        //        string title = jsonObject?["errors"]?["title"]?.ToString();

        //        if (!String.IsNullOrEmpty(statusValue) && (statusValue == "7001" || title == "Msisdn Not Found"))
        //        {
        //            var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);
        //            log.is_success = 1;
        //            var simResp = await CheckSIMNumber(new SIMNumberCheckRequest()
        //            {
        //                center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //                distributor_code = "",
        //                channel_name = msisdnCheckReqest.channel_name,
        //                session_token = msisdnCheckReqest.session_token,
        //                sim_number = msisdnCheckReqest.sim_number,
        //                retailer_id = msisdnCheckReqest.retailer_id,
        //                product_code = "",
        //                inventory_id = msisdnCheckReqest.inventory_id,
        //                msisdn = msisdnCheckReqest.mobile_number,
        //                purpose_number = msisdnCheckReqest.purpose_number
        //            }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //            if (simResp.result == false)
        //            {
        //                raRespModel.isError = true;
        //                raRespModel.message = simResp.message;
        //                return raRespModel;
        //            }

        //            raRespModel.isError = false;
        //            raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

        //            return raRespModel;
        //        }
        //        else
        //        {
        //            log.res_time = DateTime.Now;
        //            log.res_blob = _blJson.GetGenericJsonData(ex.InnerException.Message);

        //            try
        //            {
        //                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //                log.is_success = 0;
        //                log.error_code = error.error_code ?? String.Empty;
        //                log.error_source = error.error_source ?? String.Empty;
        //                log.message = error.error_description ?? String.Empty;

        //                raRespModel.isError = true;
        //                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //                return raRespModel;
        //            }
        //            catch (Exception)
        //            {
        //                raRespModel.isError = true;
        //                raRespModel.message = ex.InnerException.Message;
        //                return raRespModel;
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

        //        log.purpose_number = msisdnCheckReqest.purpose_number;
        //        log.user_id = msisdnCheckReqest.retailer_id;//userName
        //        log.method_name = "ValidateUnpairedMSISDNMNPSTartTrekesim";

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);

        //    }
        //}

        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNMNPSTartTrekesim(UnpairedMSISDNCheckRequest msisdnCheckRequest, string apiName)
        {
            var raRespModel = new RACommonResponseRevamp();
            var log = new BIAToDBSSLog();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty, txtResp = string.Empty;

            try
            {
                string dbssReqModel = new BLLRAToDBSSParse().ValidateMSISDNReqParsing(msisdnCheckRequest);

                if (!dbssReqModel.StartsWith(FixedValueCollection.MSISDNCountryCode))
                    dbssReqModel = FixedValueCollection.MSISDNCountryCode + dbssReqModel;

                var encodedMsisdn = Uri.EscapeDataString(dbssReqModel);
                apiUrl = string.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);
                log.req_blob = new BL_Json().GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                try
                {
                    dbssResp = (JObject)await _apiReq.HttpGetRequestForMNPPortIn(apiUrl, apiName);
                    log.res_time = DateTime.Now;
                    log.res_blob = new BL_Json().GetGenericJsonData(dbssResp);
                    log.is_success = 1;
                }
                catch (WebException ex)
                {
                    log.res_time = DateTime.Now;
                    txtResp = ex.InnerException?.Message ?? ex.Message;
                    log.res_blob = new BL_Json().GetGenericJsonData(dbssResp);

                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response is HttpWebResponse errorResponse && (int)errorResponse.StatusCode == 404)
                    {
                        return await HandleSIMValidation(msisdnCheckRequest, raRespModel, log);
                    }

                    throw;
                }

                var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);
                if (!msisdnResp.result)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.MSISDNAlreadyExists;
                    return raRespModel;
                }

                return await HandleSIMValidation(msisdnCheckRequest, raRespModel, log);
            }
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                txtResp = ex.Message;
                string innerMessage = ex.InnerException?.Message ?? ex.Message;

                try
                {
                    JObject errorJson = !string.IsNullOrEmpty(innerMessage) ? JObject.Parse(innerMessage) : new JObject();
                    string? statusValue = errorJson["errors"]?["status"]?.ToString();
                    string? title = errorJson["errors"]?["title"]?.ToString();

                    if (!string.IsNullOrEmpty(statusValue) && (statusValue == "7001" || title == "Msisdn Not Found"))
                    {
                        log.is_success = 1;
                        return await HandleSIMValidation(msisdnCheckRequest, raRespModel, log);
                    }

                    log.res_blob = new BL_Json().GetGenericJsonData(innerMessage);

                    var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    log.is_success = 0;
                    log.error_code = error.error_code ?? string.Empty;
                    log.error_source = error.error_source ?? string.Empty;
                    log.message = error.error_custom_msg ?? error.error_description;

                    raRespModel.isError = true;
                    raRespModel.message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                    return raRespModel;
                }
                catch
                {
                    raRespModel.isError = true;
                    raRespModel.message = innerMessage ?? ex.Message;
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
                log.method_name = nameof(ValidateUnpairedMSISDNMNPSTartTrekesim);

                await _bllLog.RAToDBSSLog(log);
            }
        }

        private async Task<RACommonResponseRevamp> HandleSIMValidation(UnpairedMSISDNCheckRequest msisdnCheckRequest, RACommonResponseRevamp responseModel, BIAToDBSSLog log)
        {
            var simCheckRequest = new SIMNumberCheckRequest
            {
                center_code = string.IsNullOrEmpty(msisdnCheckRequest.center_code) ? "" : msisdnCheckRequest.center_code,
                distributor_code = "",
                channel_name = msisdnCheckRequest.channel_name,
                session_token = msisdnCheckRequest.session_token,
                sim_number = msisdnCheckRequest.sim_number,
                retailer_id = msisdnCheckRequest.retailer_id,
                product_code = "",
                inventory_id = msisdnCheckRequest.inventory_id,
                msisdn = msisdnCheckRequest.mobile_number,
                purpose_number = msisdnCheckRequest.purpose_number ?? ""
            };

            var simResp = await CheckSIMNumber(simCheckRequest, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckRequest.sim_category, "");

            if (!simResp.result)
            {
                responseModel.isError = true;
                responseModel.message = simResp.message;
                return responseModel;
            }

            responseModel.isError = false;
            responseModel.message = MessageCollection.MSISDNandSIMBothValid;
            return responseModel;
        }

        //public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNMNPSTartTrekesimV2(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        //{
        //    RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
        //    string apiUrl = "", txtResp = "";
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    JObject dbssResp = new JObject();
        //    BL_Json _blJson = new BL_Json();
        //    BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
        //    try
        //    {
        //        var dbssReqModel = _raToDBssParse.ValidateMSISDNReqParsing(msisdnCheckReqest);

        //        if (dbssReqModel.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
        //        {
        //            dbssReqModel = FixedValueCollection.MSISDNCountryCode + dbssReqModel;
        //        }

        //        apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, dbssReqModel);

        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl);

        //        try
        //        {
        //            log.req_time = DateTime.Now;
        //            dbssResp = (JObject)await _apiReq.HttpGetRequestForMNPPortIn(apiUrl, "ValidateUnpairedMSISDNMNPSTartTrekesimV2");
        //            log.res_time = DateTime.Now;
        //        }
        //        catch (WebException ex)
        //        {
        //            Log.Error(ex, "ExMessage");
        //            log.res_time = DateTime.Now;
        //            txtResp = Convert.ToString(ex.InnerException.Message);
        //            log.res_blob = _blJson.GetGenericJsonData(dbssResp);


        //            if (ex.Status == WebExceptionStatus.ProtocolError)
        //            {
        //                var ErrorResponse = ex.Response as HttpWebResponse;
        //                if (ErrorResponse != null && (int)ErrorResponse.StatusCode == 404)
        //                {
        //                    log.is_success = 1;
        //                    var simResp = await CheckSIMNumberV2(new SIMNumberCheckRequest()
        //                    {
        //                        center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //                        distributor_code = "",
        //                        channel_name = msisdnCheckReqest.channel_name,
        //                        session_token = msisdnCheckReqest.session_token,
        //                        sim_number = msisdnCheckReqest.sim_number,
        //                        retailer_id = msisdnCheckReqest.retailer_id,
        //                        product_code = "",
        //                        inventory_id = msisdnCheckReqest.inventory_id,
        //                        msisdn = msisdnCheckReqest.mobile_number,
        //                        purpose_number = msisdnCheckReqest.purpose_number
        //                    }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //                    if (simResp.result == false)
        //                    {
        //                        raRespModel.isError = true;
        //                        raRespModel.message = simResp.message;
        //                        return raRespModel;
        //                    }

        //                    raRespModel.isError = false;
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

        //        var msisdnResp2 = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);

        //        if (msisdnResp2.result == false)
        //        {
        //            raRespModel.isError = true;
        //            raRespModel.message = MessageCollection.MSISDNAlreadyExists;
        //            return raRespModel;
        //        }

        //        var simResp2 = await CheckSIMNumberV2(new SIMNumberCheckRequest()
        //        {
        //            center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //            distributor_code = "",
        //            channel_name = msisdnCheckReqest.channel_name,
        //            session_token = msisdnCheckReqest.session_token,
        //            sim_number = msisdnCheckReqest.sim_number,
        //            retailer_id = msisdnCheckReqest.retailer_id,
        //            product_code = "",
        //            inventory_id = msisdnCheckReqest.inventory_id,
        //            msisdn = msisdnCheckReqest.mobile_number,
        //            purpose_number = msisdnCheckReqest.purpose_number
        //        }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //        if (simResp2.result == false)
        //        {
        //            raRespModel.isError = true;
        //            raRespModel.message = simResp2.message;
        //            return raRespModel;
        //        }

        //        raRespModel.isError = false;
        //        raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

        //        return raRespModel;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        JObject jsonObject = JObject.Parse(ex.InnerException.Message);
        //        log.res_time = DateTime.Now;
        //        txtResp = Convert.ToString(ex.InnerException.Message);
        //        log.res_blob = _blJson.GetGenericJsonData(dbssResp);

        //        string statusValue = jsonObject?["errors"]?["status"]?.ToString();
        //        string title = jsonObject?["errors"]?["title"]?.ToString();

        //        if (!String.IsNullOrEmpty(statusValue) && (statusValue == "7001" || title == "Msisdn Not Found"))
        //        {
        //            var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);
        //            log.is_success = 1;
        //            var simResp = await CheckSIMNumberV2(new SIMNumberCheckRequest()
        //            {
        //                center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //                distributor_code = "",
        //                channel_name = msisdnCheckReqest.channel_name,
        //                session_token = msisdnCheckReqest.session_token,
        //                sim_number = msisdnCheckReqest.sim_number,
        //                retailer_id = msisdnCheckReqest.retailer_id,
        //                product_code = "",
        //                inventory_id = msisdnCheckReqest.inventory_id,
        //                msisdn = msisdnCheckReqest.mobile_number,
        //                purpose_number = msisdnCheckReqest.purpose_number
        //            }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

        //            if (simResp.result == false)
        //            {
        //                raRespModel.isError = true;
        //                raRespModel.message = simResp.message;
        //                return raRespModel;
        //            }

        //            raRespModel.isError = false;
        //            raRespModel.message = MessageCollection.MSISDNandSIMBothValid;

        //            return raRespModel;
        //        }
        //        else
        //        {
        //            log.res_time = DateTime.Now;
        //            log.res_blob = _blJson.GetGenericJsonData(ex.InnerException.Message);

        //            try
        //            {
        //                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //                log.is_success = 0;
        //                log.error_code = error.error_code ?? String.Empty;
        //                log.error_source = error.error_source ?? String.Empty;
        //                log.message = error.error_description ?? String.Empty;

        //                raRespModel.isError = true;
        //                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //                return raRespModel;
        //            }
        //            catch (Exception)
        //            {
        //                raRespModel.isError = true;
        //                raRespModel.message = ex.InnerException.Message;
        //                return raRespModel;
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

        //        log.purpose_number = msisdnCheckReqest.purpose_number;
        //        log.user_id = msisdnCheckReqest.retailer_id;//userName
        //        log.method_name = "ValidateUnpairedMSISDNMNPSTartTrekesimV2";

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //}

        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNMNPSTartTrekesimV2(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            var raRespModel = new RACommonResponseRevamp();
            var log = new BIAToDBSSLog();
            var _blJson = new BL_Json();
            var _raToDBssParse = new BLLRAToDBSSParse();
            JObject dbssResp = new JObject();
            string apiUrl = "", txtResp = "";

            try
            {
                string dbssReqModel = _raToDBssParse.ValidateMSISDNReqParsing(msisdnCheckReqest);
                if (!dbssReqModel.StartsWith(FixedValueCollection.MSISDNCountryCode))
                    dbssReqModel = FixedValueCollection.MSISDNCountryCode + dbssReqModel;

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);
                apiUrl = string.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);
                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                try
                {
                    log.req_time = DateTime.Now;
                    dbssResp = (JObject)await _apiReq.HttpGetRequestForMNPPortIn(apiUrl, apiName);
                    log.res_time = DateTime.Now;
                }
                catch (WebException ex)
                {
                    log.res_time = DateTime.Now;
                    txtResp = ex.InnerException?.Message ?? ex.Message;
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response is HttpWebResponse errorResp && (int)errorResp.StatusCode == 404)
                    {
                        log.is_success = 1;
                        return await HandleSIMValidationV2(msisdnCheckReqest, raRespModel);
                    }

                    throw;
                }

                txtResp = dbssResp.ToString();
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);
                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.UnpairedMSISDNReqParsingForMNPProtIn(dbssResp);
                if (!msisdnResp.result)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.MSISDNAlreadyExists;
                    return raRespModel;
                }

                return await HandleSIMValidationV2(msisdnCheckReqest, raRespModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                txtResp = ex.InnerException?.Message ?? ex.Message;
                log.res_time = DateTime.Now;

                JObject errorJson;
                try
                {
                    errorJson = JObject.Parse(txtResp);
                }
                catch
                {
                    errorJson = new JObject();
                }

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                string? status = errorJson?["errors"]?["status"]?.ToString();
                string? title = errorJson?["errors"]?["title"]?.ToString();

                if (!string.IsNullOrEmpty(status) && (status == "7001" || title == "Msisdn Not Found"))
                {
                    log.is_success = 1;
                    return await HandleSIMValidationV2(msisdnCheckReqest, raRespModel);
                }

                try
                {
                    var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    log.is_success = 0;
                    log.error_code = error.error_code ?? "";
                    log.error_source = error.error_source ?? "";
                    log.message = error.error_custom_msg ?? error.error_description;

                    raRespModel.isError = true;
                    raRespModel.message = !string.IsNullOrEmpty(error.error_custom_msg)
                        ? error.error_custom_msg
                        : error.error_description;
                }
                catch
                {
                    raRespModel.isError = true;
                    raRespModel.message = txtResp;
                }

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidateUnpairedMSISDNMNPSTartTrekesimV2";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        private async Task<RACommonResponseRevamp> HandleSIMValidationV2(UnpairedMSISDNCheckRequest req, RACommonResponseRevamp raRespModel)
        {
            var simResp = await CheckSIMNumberV2(new SIMNumberCheckRequest
            {
                center_code = string.IsNullOrEmpty(req.center_code) ? "" : req.center_code,
                distributor_code = "",
                channel_name = req.channel_name,
                session_token = req.session_token,
                sim_number = req.sim_number,
                retailer_id = req.retailer_id,
                product_code = "",
                inventory_id = req.inventory_id,
                msisdn = req.mobile_number,
                purpose_number = req.purpose_number ?? ""
            }, (int)EnumPurposeOfSIMCheck.NewConnection, false, req.sim_category, "");

            if (!simResp.result)
            {
                raRespModel.isError = true;
                raRespModel.message = simResp.message;
            }
            else
            {
                raRespModel.isError = false;
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
            }

            return raRespModel;
        }

        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNSTartTrekOnlineV2(UnpairedMSISDNCheckRequestOnline msisdnCheckReqest, string reservation_id, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);
                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNSTartTrekOnlineV2");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = UnpairedMSISDNResPargingOnline(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }


                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }

                var simResp = await StarTrekCheckSIMNumber(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

                if (simResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = simResp.message;
                    return raRespModel;
                }

                Datas datas = new Datas();
                datas.isEsim = 0;
                datas.request_id = "Test";
                datas.reservation_id = reservation_id;

                raRespModel.isError = false;
                raRespModel.data = new Datas()
                {
                    reservation_id = reservation_id
                };
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                return raRespModel;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                raRespModel.isError = true;

                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;

            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNESIM_OnlineV2(UnpairedMSISDNCheckRequestOnline msisdnCheckReqest, string reservation_id, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }
                var encodedMsisdn = Uri.EscapeDataString(msisdnCheckReqest.mobile_number);

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, encodedMsisdn);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNESIM_OnlineV2");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "DBSS Error: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = UnpairedMSISDNResPargingOnline(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }

                var simResp = await CheckSIMNumber(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");

                if (simResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = simResp.message;
                    return raRespModel;
                }
                raRespModel.isError = false;
                raRespModel.data = new Datas() { reservation_id = msisdnResp.reservation_id };
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                return raRespModel;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raRespModel.isError = true;
                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidateUnpairedMSISDNESIM_OnlineV2";

                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<UnpairedMSISDNDataRev> GetCYNdatafromDBBS(UnpairedMSISDNListReqModel model, string stockValue)
        {
            UnpairedMSISDNDataRev raResp = new UnpairedMSISDNDataRev();
            List<ReponseDataRev> raRespData = new List<ReponseDataRev>();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BL_Json _blJson = new BL_Json();
            try
            {
                var encodedMsisdn = Uri.EscapeDataString(model.msisdn);
                if (SettingsValues.GetRyzeAllowOrNot() == 1)
                {
                    apiUrl = String.Format(UnpairedMSISDNList.GetCYNListPhysical, 1, 10, encodedMsisdn);
                }
                else
                {
                    apiUrl = String.Format(UnpairedMSISDNList.GetCYNListPhysicalStock16, 1, 10, encodedMsisdn, stockValue);
                }

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetCYNdatafromDBBS");
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
                                raResp.data = new List<ReponseDataRev>();
                                raResp.isError = true;
                                raResp.message = MessageCollection.DataNotFound;
                            }
                        }
                        else
                        {
                            raResp.data = new List<ReponseDataRev>();
                            raResp.isError = true;
                            raResp.message = "DBSS API doesn't contains any Unpaired MSISDN list.";
                        }
                    }
                    else
                    {
                        raResp.data = new List<ReponseDataRev>();
                        raResp.isError = true;
                        raResp.message = "DBSS API doesn't contains any Unpaired MSISDN list.";
                    }
                }
                else
                {
                    raResp.data = new List<ReponseDataRev>();
                    raResp.isError = true;
                    raResp.message = "Unable to load data from DBSS API.";
                }
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
            }
            finally
            {
                log.method_name = "GetCYNdatafromDBBS";
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
            return raResp;
        }

        public async Task<RACommonResponseRevamp> ValidateMSISDNSTartTrekCherishV2(CherishMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty, txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateMSISDNSTartTrekCherishV2");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = CherishMSISDNReqParsingV2(dbssResp, msisdnCheckReqest.retailer_id, msisdnCheckReqest.selected_category);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }

                var simResp = await StarTrekCheckSIMNumberV2(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number
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
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                ErrorDescription error = null;
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.Message);
                log.res_time = DateTime.Now;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    raRespModel.isError = true;

                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    return raRespModel;
                }
                catch (Exception)
                {
                    raRespModel.isError = true;
                    raRespModel.message = ex.Message;

                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number;
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }

        public UnpairedMSISDNStartrekCheckResponse CherishMSISDNReqParsingV2(JObject dbssRespObj, string retailer_id, string selectedCategory)
        {
            UnpairedMSISDNStartrekCheckResponse raResp = new UnpairedMSISDNStartrekCheckResponse();
            try
            {
                string status = String.Empty;
                string reserved_for = String.Empty;
                int stockId = 0;
                string retailer_code = String.Empty;
                string number_category = String.Empty;
                string category_config = String.Empty;
                string[] cofigValue = null;

                category_config = SettingsValues.GetStockNotAllowFromRyze();

                if (category_config.Contains(","))
                {
                    cofigValue = category_config.Split(',');
                }
                else
                {
                    cofigValue = category_config.Split(' ');
                }

                if (dbssRespObj["data"] != null)
                {
                    if (dbssRespObj["data"]["attributes"] != null)
                    {
                        if (!String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["status"])
                            && !String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["stock"]))
                        {
                            status = (string)dbssRespObj["data"]["attributes"]["status"];
                            stockId = (int)dbssRespObj["data"]["attributes"]["stock"];
                            reserved_for = (string)dbssRespObj["data"]["attributes"]["reserved-for"];
                            number_category = (string)dbssRespObj["data"]["attributes"]["number-category"];
                        }
                    }
                }
                if (selectedCategory.ToLower() != number_category.ToLower())
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.CherishCategoryMismatch;
                    return raResp;
                }
                if (cofigValue.Any(x => x == stockId.ToString()))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StockIDMismatch;
                    return raResp;
                }
                if (!String.IsNullOrEmpty(reserved_for))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StarTrekNotEligible;
                    return raResp;
                }
                if (String.IsNullOrEmpty(reserved_for) && status == "available")
                {
                    raResp.result = true;
                    raResp.stock_id = stockId;
                    raResp.reservation_id = reserved_for;
                    return raResp;
                }
                else if (status == "in_use")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNInUse;
                    return raResp;
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "MSISDN is invalid.";
                    return raResp;
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task<RACommonResponseRevamp> ValidateUnpairedMSISDNESIMV3(CherishMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = null;
            string apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNESIMV3");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = UnpairedMSISDNReqParsingV4(dbssResp, msisdnCheckReqest.retailer_id, msisdnCheckReqest.selected_category);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }

                var simResp = await CheckSIMNumberV2(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number
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
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                ErrorDescription error = null;
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.Message);
                log.res_time = DateTime.Now;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    raRespModel.isError = true;

                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    return raRespModel;
                }
                catch (Exception)
                {
                    raRespModel.isError = true;
                    raRespModel.message = ex.Message;

                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number;
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "StartTrekValidateUnpairedMSISDNESIMV2";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        public UnpairedMSISDNStartrekCheckResponse UnpairedMSISDNReqParsingV4(JObject dbssRespObj, string retailer_id, string selectedCategory)
        {
            UnpairedMSISDNStartrekCheckResponse raResp = new UnpairedMSISDNStartrekCheckResponse();
            try
            {
                string status = String.Empty;
                string reserved_for = String.Empty;
                int stockId = 0;
                string retailer_code = String.Empty;
                string number_category = String.Empty;
                string category_config = String.Empty;
                string[] cofigValue = null;
                string[] cofigValueStock = null;

                category_config = SettingsValues.GetStockNotAllowFromRyze();

                if (category_config.Contains(","))
                {
                    cofigValueStock = category_config.Split(',');
                }
                else
                {
                    cofigValueStock = category_config.Split(' ');
                }

                if (dbssRespObj["data"] != null)
                {
                    if (dbssRespObj["data"]["attributes"] != null)
                    {
                        if (!String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["status"])
                            && !String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["stock"]))
                        {
                            status = (string)dbssRespObj["data"]["attributes"]["status"];
                            stockId = (int)dbssRespObj["data"]["attributes"]["stock"];
                            reserved_for = (string)dbssRespObj["data"]["attributes"]["reserved-for"];
                            number_category = (string)dbssRespObj["data"]["attributes"]["number-category"];
                        }
                    }
                }
                if (selectedCategory.ToLower() != number_category.ToLower())
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.CherishCategoryMismatch;
                    return raResp;
                }
                if (cofigValueStock.Any(x => x == stockId.ToString()))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StockIDMismatch;
                    return raResp;
                }
                if (!String.IsNullOrEmpty(reserved_for))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StarTrekNotEligible;
                    return raResp;
                }
                if (String.IsNullOrEmpty(reserved_for) && status == "available")
                {
                    raResp.result = true;
                    raResp.stock_id = stockId;
                    raResp.reservation_id = reserved_for;
                    return raResp;
                }
                else if (status == "in_use")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNInUse;
                    return raResp;
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "MSISDN is invalid.";
                    return raResp;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        #endregion
        #region Cherish Number Sell
        public async Task<RACommonResponseRevamp> ValidateMSISDNVAndSIM(CherishMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateMSISDNVAndSIM");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.MSISDNReqParsingCherish(dbssResp, msisdnCheckReqest.retailer_id, msisdnCheckReqest.selected_category);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }


                var simResp = await CheckSIMNumberForCherish(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "", msisdnCheckReqest.channel_id);


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
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                ErrorDescription error = new ErrorDescription();
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.InnerException?.Message);
                log.res_time = DateTime.Now;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    raRespModel.isError = true;

                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    return raRespModel;
                }
                catch (Exception)
                {
                    raRespModel.isError = true;
                    raRespModel.message = ex.Message;

                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }

        public async Task<RACommonResponseRevamp> ValidateMSISDNVAndSIMV2(CherishMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = "";
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateMSISDNVAndSIMV2");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.MSISDNReqParsingCherish(dbssResp, msisdnCheckReqest.retailer_id, msisdnCheckReqest.selected_category);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }


                var simResp = await CheckSIMNumber4(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
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
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                ErrorDescription error = new ErrorDescription();
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.Message);
                log.res_time = DateTime.Now;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    raRespModel.isError = true;

                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    return raRespModel;
                }
                catch (Exception)
                {
                    raRespModel.isError = true;
                    raRespModel.message = ex.Message;

                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidateUnpairedMSISDNV5";

                await _bllLog.RAToDBSSLog(log);

            }
        }

        public async Task<RACommonResponseRevamp> ValidateMSISDNSTartTrekCherish(CherishMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty, txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateMSISDNSTartTrekCherish");
                log.res_time = DateTime.Now;

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = CherishMSISDNReqParsing(dbssResp, msisdnCheckReqest.retailer_id, msisdnCheckReqest.selected_category);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }


                var simResp = await StarTrekCheckSIMNumber(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
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
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                ErrorDescription error = new ErrorDescription();
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.Message);
                log.res_time = DateTime.Now;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    raRespModel.isError = true;

                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    return raRespModel;
                }
                catch (Exception)
                {
                    raRespModel.isError = true;
                    raRespModel.message = ex.Message;

                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }

        public UnpairedMSISDNStartrekCheckResponse CherishMSISDNReqParsing(JObject dbssRespObj, string retailer_id, string selectedCategory)
        {
            UnpairedMSISDNStartrekCheckResponse raResp = new UnpairedMSISDNStartrekCheckResponse();
            try
            {
                string status = String.Empty;
                string reserved_for = String.Empty;
                int stockId = 0;
                string retailer_code = String.Empty;
                string number_category = String.Empty;
                string category_config = String.Empty;
                string[] cofigValue = null;

                if (dbssRespObj["data"] != null)
                {
                    if (dbssRespObj["data"]["attributes"] != null)
                    {
                        if (!String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["status"])
                            && !String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["stock"]))
                        {
                            status = (string)dbssRespObj["data"]["attributes"]["status"];
                            stockId = (int)dbssRespObj["data"]["attributes"]["stock"];
                            reserved_for = (string)dbssRespObj["data"]["attributes"]["reserved-for"];
                            number_category = (string)dbssRespObj["data"]["attributes"]["number-category"];
                        }
                    }
                }
                if (selectedCategory.ToLower() != number_category.ToLower())
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.CherishCategoryMismatch;
                    return raResp;
                }
                if (stockId != 33)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StockIDMismatch;
                    return raResp;
                }
                if (!String.IsNullOrEmpty(reserved_for))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StarTrekNotEligible;
                    return raResp;
                }
                if (String.IsNullOrEmpty(reserved_for) && status == "available")
                {
                    //raResp = ValidateCherishedNumer(dbssRespObj, retailer_id);
                    raResp.stock_id = stockId;
                    raResp.reservation_id = reserved_for;
                    return raResp;
                }
                else if (status == "in_use")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNInUse;
                    return raResp;
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "MSISDN is invalid.";
                    return raResp;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<RACommonResponseRevamp> ValidateCherishMSISDNESIM(CherishMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevamp raRespModel = new RACommonResponseRevamp();
            JObject dbssResp = null;
            string apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateCherishMSISDNESIM");
                log.res_time = DateTime.Now;

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = CherishMSISDNReqParsing(dbssResp, msisdnCheckReqest.retailer_id, msisdnCheckReqest.selected_category);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }

                var simResp = await CheckSIMNumber(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
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
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                ErrorDescription error = null;
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.InnerException?.Message);
                log.res_time = DateTime.Now;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    raRespModel.isError = true;

                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    return raRespModel;
                }
                catch (Exception)
                {
                    raRespModel.isError = true;
                    raRespModel.message = ex.Message;

                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidateCherishMSISDNESIM";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        public async Task<RACommonResponseRevampV3> ValidateUnpairedMSISDNSTartTrekV4(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevampV3 raRespModel = new RACommonResponseRevampV3();
            JObject dbssResp = null;
            string apiUrl = string.Empty, txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNSTartTrekV4");
                log.res_time = DateTime.Now;


                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = UnpairedMSISDNReqParsing(dbssResp, msisdnCheckReqest.retailer_id);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    return raRespModel;
                }


                var simResp = await StarTrekCheckSIMNumber(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ??""
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
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                ErrorDescription error = null;
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.Message);
                log.res_time = DateTime.Now;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    raRespModel.isError = true;

                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    return raRespModel;
                }
                catch (Exception)
                {
                    raRespModel.isError = true;
                    raRespModel.message = ex.Message;

                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }

        public async Task<RACommonResponseRevampV3> ValidateUnpairedMSISDNSTartTrekV3(UnpairedMSISDNCheckRequest msisdnCheckReqest, string apiName)
        {
            RACommonResponseRevampV3 raRespModel = new RACommonResponseRevampV3();
            JObject dbssResp = new JObject();
            string apiUrl = string.Empty, txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse _raToDBssParse = new BLLRAToDBSSParse();
            BL_Json _blJson = new BL_Json();

            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.UnpairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateUnpairedMSISDNSTartTrekV3");
                log.res_time = DateTime.Now;

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null)
                {
                    log.is_success = 0;
                    raRespModel.isError = true;
                    raRespModel.message = "MSISDN: " + MessageCollection.NoDataFound;
                    return raRespModel;
                }

                log.is_success = 1;

                var msisdnResp = await UnpairedMSISDNReqParsingV3(dbssResp, msisdnCheckReqest.retailer_id, msisdnCheckReqest.channel_name);

                if (msisdnResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = msisdnResp.message;
                    raRespModel.data = new DesiredCategoryData()
                    {
                        message = msisdnResp.data_message,
                        isDesiredCategory = msisdnResp.isDesiredCategory,
                        category = msisdnResp.category_name
                    };
                    return raRespModel;
                }

                var stockCheck = await _bllCommon.IsStockAvailable(msisdnResp.stock_id, Convert.ToInt32(msisdnCheckReqest.channel_id));

                if (stockCheck == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = MessageCollection.StockIDMismatch;
                    raRespModel.data = new DesiredCategoryData()
                    {
                        message = msisdnResp.data_message,
                        isDesiredCategory = msisdnResp.isDesiredCategory,
                        category = msisdnResp.category_name
                    };
                    return raRespModel;
                }

                var simResp = await StarTrekCheckSIMNumberV2(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = string.Empty,
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = string.Empty,
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, false, msisdnCheckReqest.sim_category, "");


                if (simResp.result == false)
                {
                    raRespModel.isError = true;
                    raRespModel.message = simResp.message;
                    raRespModel.data = new DesiredCategoryData()
                    {
                        message = msisdnResp.data_message,
                        isDesiredCategory = msisdnResp.isDesiredCategory,
                        category = msisdnResp.category_name
                    };
                    return raRespModel;
                }

                raRespModel.isError = false;
                raRespModel.data = new DesiredCategoryData()
                {
                    message = msisdnResp.data_message,
                    isDesiredCategory = msisdnResp.isDesiredCategory,
                    category = msisdnResp.category_name
                };
                raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                return raRespModel;
            }
            catch (Exception ex)
            {
                log.res_time = DateTime.Now;
                ErrorDescription error = new ErrorDescription();
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.Message);
                log.res_time = DateTime.Now;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    raRespModel.isError = true;

                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    return raRespModel;
                }
                catch (Exception)
                {
                    raRespModel.isError = true;
                    raRespModel.message = ex.Message;

                    return raRespModel;
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = apiName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<UnpairedMSISDNStartrekCheckResponseV2> UnpairedMSISDNReqParsingV3(JObject dbssRespObj, string retailer_id, string channel_name)
        {
            UnpairedMSISDNStartrekCheckResponseV2 raResp = new UnpairedMSISDNStartrekCheckResponseV2();
            try
            {
                string status = String.Empty;
                string reserved_for = String.Empty;
                int stockId = 0;
                string retailer_code = String.Empty;
                string number_category = String.Empty;
                string category_config = String.Empty;
                string[] cofigValue = null;
                string[] cofigValueStock = null;
                string cherish_category_config = string.Empty;

                category_config = SettingsValues.GetStockNotAllowFromRyze();

                if (category_config.Contains(","))
                {
                    cofigValueStock = category_config.Split(',');
                }
                else
                {
                    cofigValueStock = category_config.Split(' ');
                }

                if (dbssRespObj["data"] != null)
                {
                    if (dbssRespObj["data"]["attributes"] != null)
                    {
                        if (!String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["status"])
                            && !String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["stock"]))
                        {
                            status = (string)dbssRespObj["data"]["attributes"]["status"];
                            stockId = (int)dbssRespObj["data"]["attributes"]["stock"];
                            reserved_for = (string)dbssRespObj["data"]["attributes"]["reserved-for"];
                            number_category = (string)dbssRespObj["data"]["attributes"]["number-category"];
                        }
                    }
                }
                if (cofigValueStock.Any(x => x == stockId.ToString()))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StockIDMismatch;
                    return raResp;
                }
                if (!String.IsNullOrEmpty(reserved_for))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StarTrekNotEligible;
                    return raResp;
                }
                if (String.IsNullOrEmpty(reserved_for) && status == "available")
                {
                    cherish_category_config = SettingsValues.GetCherishCategory();

                    if (cherish_category_config.Contains(","))
                    {
                        cofigValue = cherish_category_config.Split(',');
                    }
                    else
                    {
                        cofigValue = cherish_category_config.Split(' ');
                    }

                    if (cofigValue.Any(x => x == number_category))
                    {
                        var category = cofigValue.Where(x => x.Equals(number_category)).FirstOrDefault();
                        if (category != null)
                        {
                            var catInfo = await _bllCommon.GetDesiredCategoryMessage(category, channel_name);
                            if (catInfo != null)
                            {
                                raResp.data_message = catInfo.message;
                                raResp.message = MessageCollection.MSISDNValid;
                                raResp.category_name = catInfo.name;
                                raResp.isDesiredCategory = true;
                                raResp.result = true;
                            }
                            else
                            {
                                raResp.data_message = "No amount is configured for " + category + " category";
                                raResp.category_name = category;
                                raResp.isDesiredCategory = false;
                                raResp.result = false;
                                raResp.message = "No amount is configured for " + category + " category";
                            }
                        }

                    }
                    else
                    {
                        raResp = ValidateCherishedNumerV2(dbssRespObj, retailer_id);
                    }
                    raResp.stock_id = stockId;
                    raResp.reservation_id = reserved_for;
                    return raResp;
                }
                else if (status == "in_use")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNInUse;
                    return raResp;
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "MSISDN is invalid.";
                    return raResp;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public UnpairedMSISDNStartrekCheckResponseV2 ValidateCherishedNumerV2(JObject dbssRespObj, string retailer_id)
        {
            UnpairedMSISDNStartrekCheckResponseV2 raResp = new UnpairedMSISDNStartrekCheckResponseV2();

            string status = String.Empty;
            int stockId = 0;
            string retailer_code = String.Empty;
            string number_category = String.Empty;
            string category_config = String.Empty;
            string[] cofigValue = null;

            try
            {
                if (dbssRespObj["data"] != null)
                {
                    if (dbssRespObj["data"]["attributes"] != null)
                    {
                        category_config = SettingsValues.GetNumberCategory();

                        if (category_config.Contains(","))
                        {
                            cofigValue = category_config.Split(',');
                        }
                        else
                        {
                            cofigValue = category_config.Split(' ');
                        }

                        if (dbssRespObj["data"]["attributes"]["number-category"] != null)
                        {
                            retailer_code = dbssRespObj["data"]["attributes"]["salesman-id"].ToString();
                            number_category = dbssRespObj["data"]["attributes"]["number-category"].ToString();

                            if (!String.IsNullOrEmpty(retailer_code))
                            {
                                if (retailer_code.Length < 6)
                                {
                                    char pad = '0';
                                    retailer_code = retailer_code.PadLeft(6, pad);
                                }
                            }

                            if (!String.IsNullOrEmpty(retailer_code) && !String.IsNullOrEmpty(number_category) && cofigValue.Any(x => x != number_category)) // from Web.config 
                            {
                                if (retailer_id.Equals(retailer_code))
                                {
                                    raResp.result = true;
                                    raResp.message = MessageCollection.ValidCherishedNumber;
                                }
                                else
                                {
                                    raResp.result = false;
                                    raResp.message = MessageCollection.InvalidCherishedNumber;
                                }
                            }
                            else if (String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x == number_category))
                            {
                                raResp.result = true;
                                raResp.message = MessageCollection.ValidCherishedNumber;
                            }
                            else if (!String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x == number_category))
                            {
                                raResp.result = true;
                                raResp.message = MessageCollection.ValidCherishedNumber; ;
                            }
                            else if (String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x != number_category))
                            {
                                raResp.result = false;
                                raResp.message = "MSISDN not tagged with this Retailer (ID: " + retailer_id + ")";
                            }
                            else
                            {
                                raResp.result = false;
                                raResp.message = "MSISDN is not Valid.";
                            }
                        }
                        else
                        {
                            raResp.result = false;
                            raResp.message = "Invalid MSISDN Category!";
                        }
                    }
                    else
                    {
                        raResp.result = false;
                        raResp.message = "No Data found!";
                    }
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "No Data found!";
                }

                return raResp;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
        #region TOS automation
        public async Task<DateTime?> CheckActivationDate(TosNidToNidMsisdnCheckRequest model, string srcMsisdn)
        {
            BIAToDBSSLog log = new BIAToDBSSLog();
            DateTime? activation_date = DateTime.MinValue;
            try
            {
                string NinetyDaysLockCheckApi = string.Empty;
                //90 Days Lock Checking
                NinetyDaysLockCheckApi = string.Format(GetAPICollection.GetNinetyDaysChecking, srcMsisdn);
                log.req_blob = _blJson.GetGenericJsonData(NinetyDaysLockCheckApi);
                log.req_time = DateTime.Now;

                JObject NinetyDaysLockResp = await _apiReq.HttpGetRequest(NinetyDaysLockCheckApi, "NinetyDaysLockCheck");

                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(NinetyDaysLockResp);

                activation_date = _dbssToRaParse.ParseActivationDateFromNinetyDaysApi(NinetyDaysLockResp);

                return activation_date;
            }
            catch (Exception ex )
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? String.Empty;
                throw;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(model.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = model.purpose_number;
                log.user_id = model.retailer_id;
                log.method_name = "CheckActivationDate";
                await _bllLog.RAToDBSSLog(log);
            }
        }

        public async Task<DateTime?> CheckActivationDateV2(ValidateMSISDNForTOSRequestModel model, string srcMsisdn)
        {
            BIAToDBSSLog log = new BIAToDBSSLog();
            DateTime? activation_date = DateTime.MinValue;
            try
            {
                string NinetyDaysLockCheckApi = string.Empty;
                //90 Days Lock Checking
                NinetyDaysLockCheckApi = string.Format(GetAPICollection.GetNinetyDaysChecking, srcMsisdn);
                log.req_blob = _blJson.GetGenericJsonData(NinetyDaysLockCheckApi);
                log.req_time = DateTime.Now;

                JObject NinetyDaysLockResp = await _apiReq.HttpGetRequest(NinetyDaysLockCheckApi, "NinetyDaysLockCheck");

                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(NinetyDaysLockResp);

                activation_date = _dbssToRaParse.ParseActivationDateFromNinetyDaysApi(NinetyDaysLockResp);

                return activation_date;
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
                throw;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(model.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = model.purpose_number;
                log.user_id = model.retailer_id;
                log.method_name = "CheckActivationDate";
                await _bllLog.RAToDBSSLog(log);
            }
        }

        public async Task<TOSBillingReportResponse> FetchTOSBillingReports(TosNidToNidMsisdnCheckRequest model, string subscriptionId, string billingAccountId)
        {
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                string billingReportApi = string.Format(GetAPICollection.BillingReportForDebt, subscriptionId);

                log.req_blob = _blJson.GetGenericJsonData(billingReportApi);
                log.req_time = DateTime.Now;

                JObject billingResp = await _apiReq.HttpGetRequest(billingReportApi, "FetchTOSBillingReports");

                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(billingResp);

                var report = _dbssToRaParse.ParseTOSBillingReport(billingResp, billingAccountId);

                return report;
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
                throw;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(model.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = model.purpose_number;
                log.user_id = model.retailer_id;
                log.method_name = "FetchTOSBillingReports";
                await _bllLog.RAToDBSSLog(log);
            }
        }

        public async Task<TOSBillingReportResponse> FetchTOSBillingReportsV2(ValidateMSISDNForTOSRequestModel model, string subscriptionId, string billingAccountId)
        {
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                string billingReportApi = string.Format(GetAPICollection.BillingReportForDebt, subscriptionId);

                log.req_blob = _blJson.GetGenericJsonData(billingReportApi);
                log.req_time = DateTime.Now;

                JObject billingResp = await _apiReq.HttpGetRequest(billingReportApi, "FetchTOSBillingReports");

                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(billingResp);

                var report = _dbssToRaParse.ParseTOSBillingReport(billingResp, billingAccountId);

                return report;
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
                throw;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(model.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = model.purpose_number;
                log.user_id = model.retailer_id;
                log.method_name = "FetchTOSBillingReports";
                await _bllLog.RAToDBSSLog(log);
            }
        }

        public async Task<List<Dictionary<string, string>>> GetValidC2STransactions(PretupsRequestModel requestModel, string apiUrl, string userName)
        {
            BIAToDBSSLog log = new BIAToDBSSLog();
            int bypassMinute = SettingsValues.GetTOSByPassTimeTime();
            string transactionNumber = string.Empty;
            try
            {
                transactionNumber = requestModel.RECEIVER_MSISDN;
                
                if (!string.IsNullOrEmpty(requestModel.EXTCODE))
                {
                    if (requestModel.EXTCODE.StartsWith("8801"))
                    {
                        // Remove "880"
                        requestModel.EXTCODE = requestModel.EXTCODE.Substring(3);
                    }
                    else if (requestModel.EXTCODE.StartsWith("01"))
                    {
                        // Remove leading "0"
                        requestModel.EXTCODE = requestModel.EXTCODE.Substring(1);
                    }
                }
                if (!string.IsNullOrEmpty(requestModel.RECEIVER_MSISDN))
                {
                    if (requestModel.RECEIVER_MSISDN.StartsWith("8801"))
                    {
                        // Remove "880"
                        requestModel.RECEIVER_MSISDN = requestModel.RECEIVER_MSISDN.Substring(3);
                    }
                    else if (requestModel.RECEIVER_MSISDN.StartsWith("01"))
                    {
                        // Remove leading "0"
                        requestModel.RECEIVER_MSISDN = requestModel.RECEIVER_MSISDN.Substring(1);
                    }
                }

                string xmlData = $@"<?xml version=""1.0""?>
                            <!DOCTYPE COMMAND PUBLIC ""-//Ocam//DTD XML Command 1.0//EN"" ""xml/command.dtd"">
                            <COMMAND>
                            <TYPE>{requestModel.TYPE}</TYPE>
                            <DATE></DATE>
                            <EXTNWCODE>{requestModel.EXTNWCODE}</EXTNWCODE>
                            <MSISDN>{requestModel.MSISDN}</MSISDN>
                            <PIN>{requestModel.PIN}</PIN>
                            <LOGINID>{requestModel.LOGINID}</LOGINID>
                            <PASSWORD>{requestModel.PASSWORD}</PASSWORD>
                            <EXTCODE>{requestModel.EXTCODE}</EXTCODE>
                            <EXTREFNUM>{requestModel.EXTREFNUM}</EXTREFNUM>
                            <LANGUAGE1>{requestModel.LANGUAGE1}</LANGUAGE1>
                            <NUMBER_OF_LAST_X_TXN>{requestModel.NUMBER_OF_LAST_X_TXN}</NUMBER_OF_LAST_X_TXN>
                            <RECEIVER_MSISDN>{requestModel.RECEIVER_MSISDN}</RECEIVER_MSISDN>
                            </COMMAND>";

                log.req_blob = _blJson.GetGenericJsonData(xmlData);
                log.req_time = DateTime.Now;

                // Step 2: Call the HTTP XML Post method
                var response = await _apiReq.HttpPostRequestXML(xmlData, apiUrl, "GetValidC2STransactions");
                string xmlResponse = response?.ToString() ?? string.Empty;

                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(xmlResponse);

                if (string.IsNullOrEmpty(xmlResponse))
                    throw new Exception("Empty response from Pretups API.");

                // Step 3: Parse XML response
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(xmlResponse);

                var txnDetailsNodes = xmlDoc.GetElementsByTagName("TXNDETAIL");
                List<Dictionary<string, string>> validTransactions = new();

                foreach (System.Xml.XmlNode txn in txnDetailsNodes)
                {
                    string trfType = txn["TRFTYPE"]?.InnerText ?? "";
                    string txnStatus = txn["TXNSTATUS"]?.InnerText ?? "";
                    string txnAmount = txn["TXNAMOUNT"]?.InnerText ?? "";
                    string txnDateTime = txn["TXNDATETIME"]?.InnerText ?? "";
                    string[] formats = { "dd/MM/yy HH:mm:ss", "dd/MM/yyyy HH:mm:ss" }; // support both 2-digit & 4-digit years

                    if (DateTime.TryParseExact(txnDateTime, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime txnDate))
                    {
                        DateTime now = DateTime.Now;
                        TimeSpan timeDiff = now - txnDate;

                        //bool isWithinOneHour = timeDiff.TotalHours <= 1 && timeDiff.TotalHours >= 0;
                        bool isWithinOneHour = timeDiff.TotalMinutes <= bypassMinute && timeDiff.TotalMinutes >= 0;

                        bool isMatch = trfType.Equals("C2S", StringComparison.OrdinalIgnoreCase)
                                       && txnStatus.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase)
                                       && txnAmount == "350"
                                       && isWithinOneHour;

                        if (isMatch)
                        {
                            validTransactions.Add(new Dictionary<string, string>
        {
            { "TXNDATETIME", txnDateTime },
            { "TRFTYPE", trfType },
            { "TXNSTATUS", txnStatus },
            { "TXNAMOUNT", txnAmount }
        });
                        }
                    }
                    else
                    {
                        Log.Warning($"Date parse failed for value: {txnDateTime}");
                    }
                    //        if (DateTime.TryParse(txnDateTime, out DateTime txnDate))
                    //        {
                    //            DateTime now = DateTime.Now;
                    //            TimeSpan timeDiff = now - txnDate;

                    //            bool isWithinOneHour = timeDiff.TotalHours <= 1 && timeDiff.TotalHours >= 0;

                    //            bool isMatch = trfType.Equals("C2S", StringComparison.OrdinalIgnoreCase)
                    //                           && txnStatus.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase)
                    //                           && txnAmount == "350"
                    //                           && isWithinOneHour;

                    //            if (isMatch)
                    //            {
                    //                validTransactions.Add(new Dictionary<string, string>
                    //{
                    //    { "TXNDATETIME", txnDateTime },
                    //    { "TRFTYPE", trfType },
                    //    { "TXNSTATUS", txnStatus },
                    //    { "TXNAMOUNT", txnAmount }
                    //});
                    //            }
                    //        }
                }                

                return validTransactions;
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
                throw;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(transactionNumber);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = userName;
                log.method_name = "GetValidC2STransactions";
                await _bllLog.RAToDBSSLog(log);
            }
        }
        #endregion
    }
}
