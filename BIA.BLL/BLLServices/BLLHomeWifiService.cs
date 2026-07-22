using BIA.BLL.Utility;
using BIA.DAL.Repositories;
using BIA.Entity;
using BIA.Entity.Collections;
using BIA.Entity.DB_Model;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BIA.BLL.BLLServices
{
    public class BLLHomeWifiService
    {
        private readonly IConfiguration _configuration;
        private readonly BLLLog _bllLog;
        private readonly BL_Json _blJson;
        private readonly ApiCall _apiCall;
        private readonly DALBiometricRepo _dataManager;

        public BLLHomeWifiService(IConfiguration configuration, BLLLog bllLog, BL_Json blJson, ApiCall apiCall, DALBiometricRepo dataManager)
        {
            _configuration = configuration;
            _blJson = blJson;
            _bllLog = bllLog;
            _apiCall = apiCall;
            _dataManager = dataManager;
        }

        

        public async Task<HomeWifiCommonResponseModel> BLLGetDPECancelReasons()
        {
            try
            {
                Log.ForContext("LogTag", "ApiRequest")
                    .Information("BLLGetDPECancelReasons Request");

                var reasons = await _dataManager.GetDPECancelReasons();

                var response = new HomeWifiCommonResponseModel
                {
                    isError = false,
                    message = "Cancel reasons fetched successfully.",
                    data = reasons
                };

                Log.ForContext("LogTag", "ApiRequest")
                    .Information("BLLGetDPECancelReasons Response: {@Response}", response);

                return response;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "BLLGetDPECancelReasons Exception");

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = ex.Message,
                    data = null
                };
            }
        }

        private async Task<List<object>> GetPageMappingFromLeadDetails(JToken? responseData, bool includePayslipUploadPage = true)
        {
            try
            {
                string order_number =
                    responseData?["order_number"]?.ToString() ?? string.Empty;

                string initiator_channel =
                    responseData?["initiator_channel"]?.ToString() ?? string.Empty;

                string order_type =
                    responseData?["order_type"]?.ToString() ?? string.Empty;

                string subscription_type =
                    responseData?["subscription_type"]?.ToString() ?? string.Empty;

                string simkit_type =
                    responseData?["simkit_type"]?.ToString() ?? string.Empty;

                string nwAssessStatus =
                    responseData?["nw_assess_status"]?.ToString() ?? string.Empty;

                string paymentMethod =
                    responseData?["payment_type"]?.ToString() ?? string.Empty;

                Log.ForContext("LogTag", "ApiRequest")
                    .Information(
                        "GetPageMappingFromLeadDetails Request: {@Request}",
                        new
                        {
                            order_number = order_number,
                            initiator_channel = initiator_channel,
                            order_type = order_type,
                            subscription_type = subscription_type,
                            simkit_type = simkit_type,
                            nw_assess_status = nwAssessStatus,
                            payment_method = paymentMethod,
                            include_payslip_upload_page = includePayslipUploadPage
                        });

                var pageList = new List<object>();

                using (DataTable dtPages =
                    await _dataManager.GetDEPPageMapping(
                        order_number,
                        initiator_channel,
                        order_type,
                        subscription_type,
                        simkit_type,
                        nwAssessStatus,
                        paymentMethod
                    ))
                {
                    if (dtPages != null && dtPages.Rows.Count > 0)
                    {
                        foreach (DataRow row in dtPages.Rows)
                        {
                            string pageName =
                                row["PAGE_NAME"]?.ToString() ?? string.Empty;

                            /*
                                For Lead Details response, Payslip Upload should not be returned in pages[].
                                Order Status by Retailer will remain unaffected.
                            */
                            if (!includePayslipUploadPage &&
                                pageName.Equals("Payslip Upload", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            pageList.Add(new
                            {
                                page_name = pageName,

                                title =
                                    row["TITLE"]?.ToString() ?? string.Empty,

                                description =
                                    row["DESCRIPTION"]?.ToString() ?? string.Empty,

                                action_name =
                                    row["ACTION_NAME"]?.ToString() ?? string.Empty
                            });
                        }
                    }
                }

                Log.ForContext("LogTag", "ApiRequest")
                    .Information(
                        "GetPageMappingFromLeadDetails Response: {@Response}",
                        pageList);

                return pageList;
            }
            catch (Exception ex)
            {
                Log.ForContext("LogTag", "ApiRequest")
                    .Error(
                        ex,
                        "GetPageMappingFromLeadDetails Exception");

                return new List<object>();
            }
        }

        private async Task<object> GetRequiredPagesFromLeadDetails(JToken? responseData)
        {
            try
            {
                string orderNumber =
                    responseData?["order_number"]?.ToString() ?? string.Empty;

                string initiatorChannel =
                    responseData?["initiator_channel"]?.ToString() ?? string.Empty;

                string orderType =
                    responseData?["order_type"]?.ToString() ?? string.Empty;

                string subscriptionType =
                    responseData?["subscription_type"]?.ToString() ?? string.Empty;

                string simkitType =
                    responseData?["simkit_type"]?.ToString() ?? string.Empty;

                string paymentType =
                    responseData?["payment_type"]?.ToString() ?? string.Empty;


                bool isImeiRequired = false;
                bool isNetworkTestRequired = false;
                bool isPayslipUploadRequired = false;
                bool isPaymentMethodChangeRequired = false;
                bool isActivationRequired = true; // Default true

                using (DataTable dtRequiredPages =
                    await _dataManager.GetDEPRequiredPages(
                        orderNumber,
                        initiatorChannel,
                        orderType,
                        subscriptionType,
                        simkitType,
                        paymentType
                    ))
                {
                    if (dtRequiredPages != null && dtRequiredPages.Rows.Count > 0)
                    {
                        DataRow row = dtRequiredPages.Rows[0];

                        isImeiRequired =
                            row["IS_IMEI_REQUIRED"] != DBNull.Value &&
                            Convert.ToInt32(row["IS_IMEI_REQUIRED"]) == 1;


                        isNetworkTestRequired =
                            row["IS_NETWORK_TEST_REQUIRED"] != DBNull.Value &&
                            Convert.ToInt32(row["IS_NETWORK_TEST_REQUIRED"]) == 1;


                        isPayslipUploadRequired =
                            row["IS_PAYSLIP_UPLOAD_REQUIRED"] != DBNull.Value &&
                            Convert.ToInt32(row["IS_PAYSLIP_UPLOAD_REQUIRED"]) == 1;


                        isPaymentMethodChangeRequired =
                            row.Table.Columns.Contains("IS_PAYMENT_METHOD_CHANGE_REQUIRED") &&
                            row["IS_PAYMENT_METHOD_CHANGE_REQUIRED"] != DBNull.Value &&
                            Convert.ToInt32(row["IS_PAYMENT_METHOD_CHANGE_REQUIRED"]) == 1;


                        isActivationRequired =
                            row.Table.Columns.Contains("IS_ACTIVATION_REQUIRED") &&
                            row["IS_ACTIVATION_REQUIRED"] != DBNull.Value &&
                            Convert.ToInt32(row["IS_ACTIVATION_REQUIRED"]) == 1;
                    }
                }


                return new
                {
                    is_imei_required = isImeiRequired,
                    is_network_test_required = isNetworkTestRequired,
                    is_payslip_upload_required = isPayslipUploadRequired,
                    is_payment_method_change_required = isPaymentMethodChangeRequired,
                    is_activation_required = isActivationRequired
                };
            }
            catch
            {
                return new
                {
                    is_imei_required = false,
                    is_network_test_required = false,
                    is_payslip_upload_required = false,
                    is_payment_method_change_required = false,
                    is_activation_required = true
                };
            }
        }

        public async Task<HomeWifiCommonResponseModel> BLLUpsertDEPOrderOLD(HomeWifiDEPOrderRequestModel model)
        {
            if (model == null)
            {
                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Request model cannot be null.",
                    data = null
                };
            }

            var log = new BIAToDPELog();

            bool dexLogCaptured = false;
            string dexRawResponseText = string.Empty;

            try
            {
                JObject? dexJson = null;

                string operationType = model.is_canceled == 1
                    ? "CANCEL_ORDER"
                    : "SUBMIT_ORDER";

                if (model.is_canceled == 1)
                {
                    string endpoint = SettingsValues.GetDPEUpdateOrderEndPoint();

                    if (string.IsNullOrWhiteSpace(endpoint))
                    {
                        return new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = "Dex Portal Cancel Order End Point is not configured.",
                            data = null
                        };
                    }

                    string finalEndPoint = $"{endpoint}/{model?.order_number}";

                    var dexRequest = new
                    {
                        order_status = "CANCELED",
                        cancellation_reason = model.cancelation_reason,
                        remarks = model.remarks
                    };

                    log.req_time = DateTime.Now;
                    log.req_blob = _blJson.GetGenericJsonData(dexRequest);

                    Log.ForContext("LogTag", "ApiCall")
                        .Information("HomeWifi CancelOrder Request: {@Request}", new
                        {
                            apiUrl = $"{SettingsValues.GetDPEBaseUrl()?.TrimEnd('/')}{finalEndPoint}",
                            retailerCode = model.retailer_code,
                            requestBody = dexRequest
                        });

                    string dexResponseText = string.Empty;

                    #region Final Code for UAT and Production

                    var dexResponseObj = await _apiCall.HTTPPatchRequestHomeWifi(
                        finalEndPoint,
                        model.retailer_code,
                        dexRequest,
                        "BLLUpsertDEPOrder_CancelOrder",
                        model.order_type,
                        model.order_number,
                        actualDexRequest =>
                        {
                            log.req_time = DateTime.Now;
                            log.req_blob = _blJson.GetGenericJsonData(actualDexRequest);
                        },
                        rawDexResponse =>
                        {
                            dexLogCaptured = true;
                            dexRawResponseText = rawDexResponse ?? string.Empty;

                            log.res_time = DateTime.Now;
                            log.res_blob = Encoding.UTF8.GetBytes(dexRawResponseText);
                        }
                    );

                    dexResponseText = JsonConvert.SerializeObject(dexResponseObj);
                    dexJson = JsonConvert.DeserializeObject<JObject>(dexResponseText);

                    #endregion

                    #region Mock Response for Testing only

                    //try
                    //{
                    //    JObject mockDexResponse = GetMockDexPortalCancelOrderResponse(model);
                    //
                    //    dexResponseText = JsonConvert.SerializeObject(mockDexResponse);
                    //    dexJson = JsonConvert.DeserializeObject<JObject>(dexResponseText);
                    //}
                    //catch
                    //{
                    //    JObject mockDexResponse = GetMockDexPortalCancelOrderResponse(model);
                    //
                    //    dexResponseText = JsonConvert.SerializeObject(mockDexResponse);
                    //    dexJson = JsonConvert.DeserializeObject<JObject>(dexResponseText);
                    //}

                    #endregion

                    Log.ForContext("LogTag", "ApiCall")
                        .Information("HomeWifi CancelOrder Response: {Response}", dexResponseText);

                    if (dexJson == null)
                    {
                        return new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = "Invalid response from Dex Portal API.",
                            data = null
                        };
                    }

                    string dexStatus = dexJson["status"]?.ToString() ?? string.Empty;

                    if (!string.Equals(dexStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                    {
                        log.res_time = DateTime.Now;

                        if (!dexLogCaptured)
                        {
                            log.res_blob = Encoding.UTF8.GetBytes(dexResponseText ?? string.Empty);
                        }

                        log.is_success = 0;

                        string dexMessage = dexJson["message"]?.ToString()
                                            ?? dexJson["error"]?["description"]?.ToString()
                                            ?? "Cancel order failed from Dex Portal.";

                        log.message = dexMessage.StartsWith("Dex:", StringComparison.OrdinalIgnoreCase)
                            ? dexMessage
                            : $"Dex: {dexMessage}";

                        return new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = log.message,
                            data = ConvertJTokenToObject(dexJson)
                        };
                    }

                    model.order_status = "CANCELED";
                    model.remarks ??= model.cancelation_reason;
                }
                else
                {
                    log.req_time = DateTime.Now;
                    log.req_blob = _blJson.GetGenericJsonData(model);
                }

                var dbResponse = await _dataManager.UpsertDEPOrder(model, operationType);

                log.res_time = DateTime.Now;
                log.is_success = dbResponse.isError ? 0 : 1;
                log.message = dbResponse.message;

                if (dbResponse.isError)
                {
                    if (!dexLogCaptured)
                    {
                        log.res_blob = _blJson.GetGenericJsonData(dbResponse);
                    }

                    var errorResponse = new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = dbResponse.message,
                        data = dbResponse.data
                    };

                    Log.ForContext("LogTag", "ApiRequest")
                        .Information("BLLUpsertDEPOrder Response: {@Response}", errorResponse);

                    return errorResponse;
                }

                /*
                    SUCCESS RESPONSE:
                    - If DeX returns payload.data, bind it like LeadDetails API.
                    - If DeX does not return payload.data, build response data from APP request model.
                    - Return data with lead_details, pages, and required.
                */
                JToken responseData = GetSubmitOrderResponseData(model, dexJson);

                var pages = await GetPageMappingFromLeadDetails(responseData);

                var requiredPages = await GetRequiredPagesFromLeadDetails(responseData);

                var finalData = new HomeWifiLeadDetailsResponseData
                {
                    lead_details = ConvertJTokenToObject(responseData),
                    pages = pages,
                    required_pages = requiredPages
                };

                var finalResponse = new HomeWifiCommonResponseModel
                {
                    isError = false,
                    message = dbResponse.message,
                    data = finalData
                };

                if (!dexLogCaptured)
                {
                    log.res_blob = _blJson.GetGenericJsonData(finalResponse);
                }

                Log.ForContext("LogTag", "ApiRequest")
                    .Information("BLLUpsertDEPOrder Response: {@Response}", finalResponse);

                return finalResponse;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "BLLUpsertDEPOrder Exception");

                log.res_time = DateTime.Now;
                log.is_success = 0;

                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_custom_msg ?? error.error_description;

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg)
                        ? error.error_description
                        : error.error_custom_msg,
                    data = null
                };
            }
            finally
            {
                log.user_id = model?.retailer_code ?? string.Empty;
                log.order_number = model?.order_number ?? string.Empty;
                log.method_name = "UpsertDEPOrder";

                await _bllLog.BIAToDPELog(log);
            }
        }
        public async Task<HomeWifiCommonResponseModel> BLLUpsertDEPOrder(HomeWifiDEPOrderRequestModel model)
        {
            var log = new BIAToDPELog();

            bool dexLogCaptured = false;
            string dexRawResponseText = string.Empty;

            try
            {
                JObject? dexJson = null;

                string operationType = model.is_canceled == 1
                    ? "CANCEL_ORDER"
                    : "SUBMIT_ORDER";

                async Task InsertDexApiLogAsync(BIAToDPELog apiLog, string methodName, string defaultMessage)
                {
                    apiLog.user_id = model?.retailer_code ?? string.Empty;
                    apiLog.order_number = model?.order_number ?? string.Empty;
                    apiLog.method_name = methodName;

                    apiLog.req_time = apiLog.req_time == default ? DateTime.Now : apiLog.req_time;
                    apiLog.res_time = apiLog.res_time == default ? DateTime.Now : apiLog.res_time;

                    if (string.IsNullOrWhiteSpace(apiLog.message))
                        apiLog.message = defaultMessage;

                    await _bllLog.BIAToDPELog(apiLog);
                }

                /*
                 =====================================================
                 STEP 1: GET LEAD DETAILS
                 =====================================================
                */
                JObject? latestDexJson = await GetLeadDetails(
                    model.order_number,
                    model.retailer_code,
                    req =>
                    {
                        log.req_time = DateTime.Now;
                        log.req_blob = _blJson.GetGenericJsonData(req);
                    },
                    res =>
                    {
                        log.res_time = DateTime.Now;
                        log.res_blob = Encoding.UTF8.GetBytes(res ?? "");
                    }
                );

                JToken latestData =
                    latestDexJson?["payload"]?["data"] != null
                        ? HomeWifiLeadListResponseMapper.MapDexLeadDetailsResponseData(latestDexJson["payload"]["data"])
                        : null;

                string latestStatus =
                    latestData?["order_status"]?.ToString()?.Trim()?.ToUpper() ?? "";

                /*
                 =====================================================
                 CASE: ALREADY CANCELED
                 =====================================================
                */
                if (latestStatus == "CANCELED" || latestStatus == "CANCELLED")
                {
                    await _dataManager.UpsertDEPOrder(
                        BuildUpsertModelFromLatestDexOrAppModel(model, latestData),
                        "DEX_ALREADY_CANCELED"
                    );

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Order already canceled at Dex end",
                        data = null
                    };
                }

                /*
                 =====================================================
                 CASE: LOCK API
                 =====================================================
                */
                if (latestStatus != "DISPATCHED")
                {
                    var lockLog = new BIAToDPELog();

                    string lockEndpoint = SettingsValues.GetDPEOrderCancellationLockEndPoint();
                    string lockResponseText = "";

                    var lockResponse = await _apiCall.HTTPPostRequestCancelationLockHomeWifi(
                        lockEndpoint,
                        model.retailer_code,
                        new { order_number = model.order_number, action = "lock" },
                        "CANCEL_LOCK",
                        null,
                        model.order_number,
                        req =>
                        {
                            lockLog.req_time = DateTime.Now;
                            lockLog.req_blob = _blJson.GetGenericJsonData(req);
                        },
                        res =>
                        {
                            lockResponseText = res ?? "";
                            lockLog.res_time = DateTime.Now;
                            lockLog.res_blob = Encoding.UTF8.GetBytes(lockResponseText);
                        }
                    );

                    JObject lockJson = JsonConvert.DeserializeObject<JObject>(
                        lockResponseText ?? JsonConvert.SerializeObject(lockResponse)
                    );

                    lockLog.is_success =
                        string.Equals(lockJson?["status"]?.ToString(), "SUCCESS", StringComparison.OrdinalIgnoreCase)
                            ? 1 : 0;

                    lockLog.message = lockJson?["message"]?.ToString() ?? "Lock API executed";

                    await InsertDexApiLogAsync(lockLog, "CANCEL_LOCK", lockLog.message);

                    latestDexJson = await GetLeadDetails(
                        model.order_number,
                        model.retailer_code,
                        req =>
                        {
                            log.req_time = DateTime.Now;
                            log.req_blob = _blJson.GetGenericJsonData(req);
                        },
                        res =>
                        {
                            log.res_time = DateTime.Now;
                            log.res_blob = Encoding.UTF8.GetBytes(res ?? "");
                        }
                    );

                    latestData =
                        latestDexJson?["payload"]?["data"] != null
                            ? HomeWifiLeadListResponseMapper.MapDexLeadDetailsResponseData(latestDexJson["payload"]["data"])
                            : null;
                }

                /*
                 =====================================================
                 CASE: CANCEL ORDER API
                 =====================================================
                */
                if (model.is_canceled == 1)
                {
                    var cancelLog = new BIAToDPELog();

                    string endpoint = SettingsValues.GetDPEUpdateOrderEndPoint();
                    string responseText = "";

                    var dexResponse = await _apiCall.HTTPPatchRequestHomeWifi(
                        $"{endpoint}/{model.order_number}",
                        model.retailer_code,
                        new
                        {
                            order_status = "CANCELED",
                            cancellation_reason = model.cancelation_reason,
                            remarks = model.remarks
                        },
                        "CANCEL_ORDER",
                        model.order_type,
                        model.order_number,
                        req =>
                        {
                            cancelLog.req_time = DateTime.Now;
                            cancelLog.req_blob = _blJson.GetGenericJsonData(req);
                        },
                        res =>
                        {
                            responseText = res ?? "";
                            cancelLog.res_time = DateTime.Now;
                            cancelLog.res_blob = Encoding.UTF8.GetBytes(responseText);
                        }
                    );

                    dexJson = JsonConvert.DeserializeObject<JObject>(responseText);

                    await InsertDexApiLogAsync(cancelLog, "CANCEL_ORDER", cancelLog.message);

                    /*
                     =====================================================
                     🔥 CRITICAL FIX: STOP DB UPDATE IF DEX FAILS
                     =====================================================
                    */

                    bool dexSuccess =
                        dexJson?["status"]?.ToString()?.ToUpper() == "SUCCESS"
                        || dexJson?["isError"]?.ToString()?.ToLower() == "false";

                    if (!dexSuccess)
                    {
                        return new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = dexJson?["message"]?.ToString()
                                      ?? "Cancellation failed at DEX",
                            data = null
                        };
                    }
                }

                /*
                 =====================================================
                 FINAL DB UPSERT (ONLY HAPPENS IF DEX SUCCESS)
                 =====================================================
                */
                var finalModel = BuildUpsertModelFromLatestDexOrAppModel(model, latestData);

                if (model.is_canceled == 1)
                {
                    finalModel.is_canceled = 1;
                    finalModel.order_status = "CANCELED";
                }

                await _dataManager.UpsertDEPOrder(finalModel, operationType);

                /*
                 =====================================================
                 RESPONSE
                 =====================================================
                */

                JToken responseData = latestData ?? GetSubmitOrderResponseData(model, dexJson);

                var pages = await GetPageMappingFromLeadDetails(responseData);
                var requiredPages = await GetRequiredPagesFromLeadDetails(responseData);

                return new HomeWifiCommonResponseModel
                {
                    isError = false,
                    message = "SUCCESS",
                    data = new HomeWifiLeadDetailsResponseData
                    {
                        lead_details = ConvertJTokenToObject(responseData),
                        pages = pages,
                        required_pages = requiredPages
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "BLLUpsertDEPOrder Failed");

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = ex.Message,
                    data = null
                };
            }
            finally
            {
                log.user_id = model?.retailer_code ?? "";
                log.order_number = model?.order_number ?? "";
                log.method_name = "UpsertDEPOrder";

                await _bllLog.BIAToDPELog(log);
            }
        }

        public async Task<HomeWifiCommonResponseModel> BLLHomeWifiRefer(HomeWifiReferOrderRequest model)
        {
            var log = new BIAToDPELog();
            string dexMessage = string.Empty;
            string order_token = string.Empty;

            HomeWifiCommonResponseModel finalResponse = new HomeWifiCommonResponseModel();

            try
            {
                JObject? dexJson = null;

                string endpoint = SettingsValues.GetReferLeadAPIendPoint();

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Dex Portal Cancel Order End Point is not configured.",
                        data = null
                    };
                }
                string dexResponseText = string.Empty;

                var dexRequest = new
                {
                    customer_name = model.customer_name,
                    email = model.email,
                    mobile = model.mobile,
                    alternate_mobile = model.alternate_mobile,
                    nid_number = model.nid_number,
                    nationality = model.nationality,
                    district_code = model.district_code,
                    area_code = model.area_code,
                    device_code = model.device_code,
                    plan_code = model.plan_code,
                    delivery_address = model.delivery_address,
                    appointment_date = model.appointment_date.ToString("yyyy-MM-dd"),
                    remarks = model.remarks
                };

                #region Final Code for UAT and Production

                var dexResponseObj = await _apiCall.HTTPPostRequestHomeWifiRefer(
                    endpoint,
                    model.retailer_id,
                    dexRequest,
                    "BLLHomeWifiRefer",
                    null,
                    null
                );

                dexResponseText = JsonConvert.SerializeObject(dexResponseObj);

                dexJson = JsonConvert.DeserializeObject<JObject>(dexResponseText);
                log.res_blob = _blJson.GetGenericJsonData(dexJson);

                #endregion

                if (dexJson == null)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Invalid response from Dex Portal API.",
                        data = null
                    };
                }

                string dexStatus = dexJson["status"]?.ToString() ?? string.Empty;

                if (string.Equals(dexStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                {
                    log.res_time = DateTime.Now;

                    log.is_success = 0;

                    order_token = await _dataManager.SaveReferOrderinBIODB(model);

                    dexMessage = dexJson["message"]?.ToString()
                                       ?? dexJson["error"]?["description"]?.ToString()
                                       ?? "Refer Order creation failed to Dex.";

                    finalResponse = new HomeWifiCommonResponseModel
                    {
                        isError = false,
                        message = dexMessage,
                        data = new
                        {
                            token = order_token,
                        }
                    };
                }
                else
                {
                    dexMessage = dexJson["message"]?.ToString() ?? dexJson["error"]?["description"]?.ToString()
                                        ?? "Refer Order creation failed to Dex.";

                    finalResponse = new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = dexMessage,
                        data = new
                        {
                            token = "",
                        }
                    };                    
                }

                return finalResponse;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "BLLUpsertDEPOrder Exception");

                log.res_time = DateTime.Now;
                log.is_success = 0;

                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_custom_msg ?? error.error_description;

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg)
                        ? error.error_description
                        : error.error_custom_msg,
                    data = null
                };
            }            
        }


        public async Task<HomeWifiCommonResponseModel> BLLGetDEPOrderStatus(string retailerCode)
        {
            try
            {
                Log.ForContext("LogTag", "ApiRequest")
                    .Information(
                        "BLLGetDEPOrderStatus Request: {@Request}",
                        new
                        {
                            retailer_code = retailerCode
                        });

                using (DataTable dt =
                    await _dataManager.GetDEPOrderStatus(retailerCode))
                {
                    var data = new List<object>();

                    foreach (DataRow row in dt.Rows)
                    {
                        string orderNumber = GetColumnString(row, "ORDER_NUMBER");
                        string initiatorChannel = GetColumnString(row, "INITIATOR_CHANNEL");
                        string orderType = GetColumnString(row, "ORDER_TYPE");
                        string subscriptionType = GetColumnString(row, "SUBSCRIPTION_TYPE");
                        string simkitType = GetColumnString(row, "SIMKIT_TYPE");
                        string paymentType = GetColumnString(row, "PAYMENT_TYPE");

                        object requiredPages = await GetRequiredPages(
                            orderNumber,
                            initiatorChannel,
                            orderType,
                            subscriptionType,
                            simkitType,
                            paymentType
                        );

                        data.Add(new
                        {
                            order_number = orderNumber,
                            mobile = GetColumnString(row, "MOBILE"),
                            alternate_mobile = GetColumnString(row, "ALTERNATE_MOBILE"),
                            customer_name = GetColumnString(row, "CUSTOMER_NAME"),
                            email = GetColumnString(row, "EMAIL"),

                            offer_name = GetColumnString(row, "OFFER_NAME"),
                            offer_code = GetColumnString(row, "OFFER_CODE"),

                            // v1.7: multiple devices are returned as array.
                            devices = GetDevicesFromStatusRow(row),

                            delivery_address = GetColumnString(row, "DELIVERY_ADDRESS"),
                            district = GetColumnString(row, "DISTRICT"),
                            area = GetColumnString(row, "AREA"),

                            payment_type = paymentType,
                            total_amount = GetColumnString(row, "TOTAL_AMOUNT"),
                            payment_status = GetColumnString(row, "PAYMENT_STATUS"),

                            order_date = GetColumnString(row, "ORDER_DATE"),
                            order_assigned_at = GetColumnString(row, "ORDER_ASSIGNED_AT"),
                            appointment_date = GetColumnString(row, "APPOINTMENT_DATE"),

                            nw_assess_id = GetColumnString(row, "NW_ASSESS_ID"),
                            nw_assess_status = GetColumnString(row, "NW_ASSESS_STATUS"),
                            is_nw_assessed = GetColumnString(row, "IS_NW_ASSESSED"),

                            order_type = orderType,
                            order_status = GetColumnString(row, "ORDER_STATUS"),
                            initiator_channel = initiatorChannel,
                            subscription_type = subscriptionType,
                            simkit_type = simkitType,

                            ordered_msisdn = GetColumnString(row, "ORDERED_MSISDN"),

                            retailer_code = GetColumnString(row, "RETAILER_CODE"),
                            remarks = GetColumnString(row, "REMARKS"),
                            cancelation_reason = GetColumnString(row, "CANCELATION_REASON"),

                            is_activation_done = GetColumnString(row, "IS_ACTIVATION_DONE"),
                            is_imei_updated = GetColumnString(row, "IS_IMEI_UPDATED"),
                            is_payslip_uploaded = GetColumnString(row, "IS_PAYSLIP_UPLOADED"),
                            is_payment_method_changed = GetColumnString(row, "IS_PAYMENT_METHOD_CHANGED"),
                            is_canceled = GetColumnString(row, "IS_CANCELED"),

                            bi_order_number = GetColumnString(row, "BI_ORDER_NUMBER"),
                            purpose_number = GetColumnString(row, "PURPOSE_NUMBER"),
                            bi_status = GetColumnString(row, "BI_STATUS"),

                            status = GetColumnString(row, "STATUS"),
                            next_action = GetColumnString(row, "NEXT_ACTION"),
                            next_action_page = GetColumnString(row, "NEXT_ACTION_PAGE"),
                            next_action_title = GetColumnString(row, "NEXT_ACTION_TITLE"),
                            next_action_description = GetColumnString(row, "NEXT_ACTION_DESCRIPTION"),
                            status_name = GetColumnString(row, "STATUS_NAME"),

                            is_pending = GetColumnString(row, "IS_PENDING"),
                            is_ongoing = GetColumnString(row, "IS_ONGOING"),
                            is_successfull = GetColumnString(row, "IS_SUCCESSFULL"),
                            is_successfully_done = GetColumnString(row, "IS_SUCCESSFULLY_DONE"),
                            is_failed = GetColumnString(row, "IS_FAILED"),
                            is_reffered = GetColumnString(row, "IS_REFFERED"),

                            required_pages = requiredPages
                        });
                    }

                    return new HomeWifiCommonResponseModel
                    {
                        isError = false,
                        message = "Order status fetched successfully.",
                        data = data
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "BLLGetDEPOrderStatus Exception");

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = ex.Message,
                    data = null
                };
            }
        }

        private async Task<object> GetRequiredPages(string orderNumber, string initiatorChannel, string orderType, string subscriptionType, string simkitType, string paymentType)
        {
            try
            {
                DataTable dtRequiredPages = await _dataManager.GetDEPRequiredPages(
                    orderNumber,
                    initiatorChannel,
                    orderType,
                    subscriptionType,
                    simkitType,
                    paymentType
                );

                bool isImeiRequired = false;
                bool isNetworkTestRequired = false;
                bool isPayslipUploadRequired = false;

                if (dtRequiredPages != null && dtRequiredPages.Rows.Count > 0)
                {
                    DataRow row = dtRequiredPages.Rows[0];

                    isImeiRequired =
                        row["IS_IMEI_REQUIRED"] != DBNull.Value &&
                        Convert.ToInt32(row["IS_IMEI_REQUIRED"]) == 1;

                    isNetworkTestRequired =
                        row["IS_NETWORK_TEST_REQUIRED"] != DBNull.Value &&
                        Convert.ToInt32(row["IS_NETWORK_TEST_REQUIRED"]) == 1;

                    isPayslipUploadRequired =
                        row["IS_PAYSLIP_UPLOAD_REQUIRED"] != DBNull.Value &&
                        Convert.ToInt32(row["IS_PAYSLIP_UPLOAD_REQUIRED"]) == 1;
                }

                return new
                {
                    is_imei_required = isImeiRequired,
                    is_network_test_required = isNetworkTestRequired,
                    is_payslip_upload_required = isPayslipUploadRequired
                };
            }
            catch
            {
                return new
                {
                    is_imei_required = false,
                    is_network_test_required = false,
                    is_payslip_upload_required = false
                };
            }
        }

        public async Task<HomeWifiCommonResponseModel> BLLHomeWifiNetworkAssessment(HomeWifiNetworkAssessmentRequestModel model)
        {
            var log = new BIAToDPELog();

            string dexResponseText = string.Empty;
            bool dexLogCaptured = false;

            try
            {
                string nwAssessStatusText =
                    model.nw_assess_status?.ToString()?.Trim() ?? string.Empty;

                string nwAssessIdText =
                    model.nw_assess_id?.ToString()?.Trim() ?? string.Empty;

                /*
                    =========================
                    STEP 1: CALL DEX FIRST
                    =========================
                */
                var dexRequest = new
                {
                    order_status = SettingsValues.GetNWAssessOrderStatus(),
                    nw_assess_id = nwAssessIdText,
                    nw_assess_status = nwAssessStatusText
                };

                string endpoint = SettingsValues.GetDPEUpdateOrderEndPoint();
                string finalEndpoint = $"{endpoint}/{model.order_number}";

                var dexApiResponse = await _apiCall.HTTPPatchRequestHomeWifi(
                    finalEndpoint,
                    model.retailer_code,
                    dexRequest,
                    "BLLHomeWifiNetworkAssessment",
                    model.order_type,
                    model.order_number,
                    actualDexRequest =>
                    {
                        log.req_time = DateTime.Now;
                        log.req_blob = _blJson.GetGenericJsonData(actualDexRequest);
                    },
                    rawDexResponse =>
                    {
                        dexLogCaptured = true;
                        dexResponseText = rawDexResponse ?? string.Empty;

                        log.res_time = DateTime.Now;
                        log.res_blob = Encoding.UTF8.GetBytes(dexResponseText);
                    }
                );

                JObject? dexJson = null;

                try
                {
                    dexJson = JsonConvert.DeserializeObject<JObject>(dexResponseText);
                }
                catch
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Invalid response from Dex Portal API.",
                        data = dexResponseText
                    };
                }

                string dexStatus = dexJson?["status"]?.ToString() ?? "";

                if (!string.Equals(dexStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = dexJson?["message"]?.ToString() ?? "Dex Portal failed.",
                        data = ConvertJTokenToObject(dexJson)
                    };
                }

                /*
                    =========================
                    STEP 2: DB UPDATE (ONLY IF DEX SUCCESS)
                    =========================
                */
                var upsertModel = new HomeWifiDEPOrderRequestModel
                {
                    order_number = model.order_number,
                    retailer_code = model.retailer_code,
                    nw_assess_id = model.nw_assess_id,
                    nw_assess_status = nwAssessStatusText
                };

                var dbResponse = await _dataManager.UpsertDEPOrder(upsertModel, "NETWORK_ASSESSMENT");

                log.is_success = dbResponse.isError ? 0 : 1;
                log.message = dbResponse.message;

                if (dbResponse.isError)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = dbResponse.message,
                        data = dbResponse.data
                    };
                }

                /*
                    =========================
                    STEP 3: SAME RESPONSE FORMAT AS IMEI / UPSERT BLL
                    =========================
                */
                JToken responseData = GetSubmitOrderResponseData(upsertModel, dexJson);

                var pages = await GetPageMappingFromLeadDetails(responseData);
                var requiredPages = await GetRequiredPagesFromLeadDetails(responseData);

                var finalResponse = new HomeWifiCommonResponseModel
                {
                    isError = false,
                    message = dbResponse.message,
                    data = new HomeWifiLeadDetailsResponseData
                    {
                        lead_details = ConvertJTokenToObject(responseData),
                        pages = pages,
                        required_pages = requiredPages
                    }
                };

                if (!dexLogCaptured)
                {
                    log.res_blob = _blJson.GetGenericJsonData(finalResponse);
                }

                return finalResponse;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "BLLHomeWifiNetworkAssessment Exception");

                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg)
                        ? error.error_description
                        : error.error_custom_msg,
                    data = null
                };
            }
            finally
            {
                log.user_id = model?.retailer_code ?? string.Empty;
                log.order_number = model?.order_number ?? string.Empty;
                log.method_name = "HomeWifiNetworkAssessment";

                await _bllLog.BIAToDPELog(log);
            }
        }

        public async Task<HomeWifiCommonResponseModel> BLLHomeWifiPayslipUpload(HomeWifiPayslipUploadRequestModel model)
        {
            var log = new BIAToDPELog();

            bool dexRawResponseCaptured = false;
            string rawDexResponseText = string.Empty;

            try
            {
                string endpoint = SettingsValues.GetDPEUpdateOrderEndPoint();

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Dex Portal Order Update End Point is not configured.",
                        data = null
                    };
                }

                string finalEndPoint = $"{endpoint}/{model.order_number}";

                Log.ForContext("LogTag", "ApiCall")
                    .Information("HomeWifi PayslipUpload Request Started: {@Request}", new
                    {
                        apiUrl = $"{SettingsValues.GetDPEBaseUrl()?.TrimEnd('/')}{finalEndPoint}",
                        retailerCode = model.retailer_code,
                        orderNumber = model.order_number,
                        fileName = model.payslip_image?.FileName,
                        fileSize = model.payslip_image?.Length
                    });

                string dexResponseText = string.Empty;
                JObject? dexJson = null;

                #region Final Code for UAT and Production

                object? actualDexRequestBlob = null;

                var dexResponseObj = await _apiCall.HTTPPostMultipartHomeWifi(
                    finalEndPoint,
                    model.retailer_code,
                    model.payslip_image,
                    "BLLHomeWifiPayslipUpload",
                    model.order_number,
                    null,
                    actualDexRequest =>
                    {
                        actualDexRequestBlob = actualDexRequest;

                        log.req_time = DateTime.Now;
                        log.req_blob = _blJson.GetGenericJsonData(actualDexRequest);
                    },
                    rawDexResponse =>
                    {
                        dexRawResponseCaptured = true;
                        rawDexResponseText = rawDexResponse ?? string.Empty;

                        log.res_time = DateTime.Now;
                        log.res_blob = Encoding.UTF8.GetBytes(rawDexResponseText);
                    }
                );

                if (actualDexRequestBlob == null)
                {
                    log.req_time = DateTime.Now;
                    log.req_blob = _blJson.GetGenericJsonData(new
                    {
                        method = "POST",
                        url = $"{SettingsValues.GetDPEBaseUrl()?.TrimEnd('/')}{finalEndPoint}",
                        message = "Actual request was not captured from ApiCall.",
                        body = new
                        {
                            order_number = model.order_number,
                            retailer_code = model.retailer_code,
                            file_name = model.payslip_image?.FileName,
                            file_size = model.payslip_image?.Length
                        }
                    });
                }

                /*
                    Keep existing app logic:
                    responseObj is still serialized and parsed exactly like before.
                    raw Dex response is only stored in log.res_blob through callback.
                */
                dexResponseText = JsonConvert.SerializeObject(dexResponseObj);
                dexJson = JsonConvert.DeserializeObject<JObject>(dexResponseText);

                #endregion

                #region Mock Response for Testing only

                //JObject mockDexResponse = GetMockDexPortalPayslipUploadResponse(model);

                //dexResponseText = JsonConvert.SerializeObject(mockDexResponse);
                //dexJson = JsonConvert.DeserializeObject<JObject>(dexResponseText);

                #endregion

                Log.ForContext("LogTag", "ApiCall")
                    .Information("HomeWifi PayslipUpload Response: {Response}", dexResponseText);

                if (dexJson == null)
                {
                    log.res_time = DateTime.Now;

                    if (!dexRawResponseCaptured)
                    {
                        log.res_blob = Encoding.UTF8.GetBytes(dexResponseText ?? string.Empty);
                    }

                    log.is_success = 0;
                    log.message = "Invalid response from Dex Portal API.";

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Invalid response from Dex Portal API.",
                        data = null
                    };
                }

                string dexStatus = dexJson["status"]?.ToString() ?? string.Empty;

                if (!string.Equals(dexStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                {
                    log.res_time = DateTime.Now;

                    if (!dexRawResponseCaptured)
                    {
                        log.res_blob = Encoding.UTF8.GetBytes(dexResponseText ?? string.Empty);
                    }

                    log.is_success = 0;

                    string dexMessage = dexJson["message"]?.ToString()
                                        ?? dexJson["error"]?["description"]?.ToString()
                                        ?? "Payslip upload failed from Dex Portal.";

                    log.message = dexMessage.StartsWith("Dex:", StringComparison.OrdinalIgnoreCase)
                        ? dexMessage
                        : $"Dex: {dexMessage}";

                    object? responseDataObject = ConvertJTokenToObject(dexJson);

                    JToken? mappedData = GetMappedDexLeadDetailsStyleData(dexJson);

                    if (mappedData != null && mappedData.Type == JTokenType.Object)
                    {
                        var pages = await GetPageMappingFromLeadDetails(mappedData);

                        var requiredPages = await GetRequiredPagesFromLeadDetails(mappedData);

                        responseDataObject = new HomeWifiLeadDetailsResponseData
                        {
                            lead_details = ConvertJTokenToObject(mappedData),
                            pages = pages,
                            required_pages = requiredPages
                        };
                    }

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = log.message,
                        data = responseDataObject
                    };
                }

                JToken? mappedDataSuccess = GetMappedDexLeadDetailsStyleData(dexJson);

                if (mappedDataSuccess == null || mappedDataSuccess.Type != JTokenType.Object)
                {
                    log.res_time = DateTime.Now;

                    if (!dexRawResponseCaptured)
                    {
                        log.res_blob = Encoding.UTF8.GetBytes(dexResponseText ?? string.Empty);
                    }

                    log.is_success = 0;
                    log.message = "Dex Portal response data missing.";

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Dex Portal response data missing.",
                        data = null
                    };
                }

                JObject responseData = (JObject)mappedDataSuccess;

                SetOrReplace(responseData, "retailer_code", model.retailer_code);
                SetOrReplace(responseData, "is_payslip_uploaded", 1);

                HomeWifiDEPOrderRequestModel? upsertModel =
                    BuildUpsertModelFromMappedData(responseData, model.retailer_code);

                if (upsertModel == null)
                {
                    log.res_time = DateTime.Now;

                    if (!dexRawResponseCaptured)
                    {
                        log.res_blob = Encoding.UTF8.GetBytes(dexResponseText ?? string.Empty);
                    }

                    log.is_success = 0;
                    log.message = "Unable to parse Dex Portal response.";

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Unable to parse Dex Portal response.",
                        data = null
                    };
                }

                upsertModel.is_payslip_uploaded = 1;

                var dbResponse = await _dataManager.UpsertDEPOrder(upsertModel, "PAYSLIP_UPLOAD");

                log.res_time = DateTime.Now;
                log.is_success = dbResponse.isError ? 0 : 1;
                log.message = dbResponse.message;

                if (dbResponse.isError)
                {
                    /*
                        Do not overwrite raw Dex response if Dex was already called.
                        Only log DB response when no Dex raw response was captured.
                    */
                    if (!dexRawResponseCaptured)
                    {
                        log.res_blob = _blJson.GetGenericJsonData(dbResponse);
                    }

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = dbResponse.message,
                        data = dbResponse.data
                    };
                }

                HomeWifiCommonResponseModel finalResponse =
                    await BuildLeadDetailsStyleFinalResponse(responseData, dbResponse.message, false);

                /*
                    Do not overwrite raw Dex response.
                    This fallback is only for cases where Dex was not actually called/captured.
                */
                if (!dexRawResponseCaptured)
                {
                    log.res_blob = _blJson.GetGenericJsonData(finalResponse);
                }

                Log.ForContext("LogTag", "ApiRequest")
                    .Information("BLLHomeWifiPayslipUpload Response: {@Response}", finalResponse);

                return finalResponse;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "BLLHomeWifiPayslipUpload Exception");

                log.res_time = DateTime.Now;
                log.is_success = 0;

                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                /*
                    If Dex response was already captured, keep raw Dex response.
                    Only log exception details if Dex was not called/captured.
                */
                if (!dexRawResponseCaptured)
                {
                    log.res_blob = _blJson.GetGenericJsonData(error);
                }

                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_custom_msg ?? error.error_description;

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg)
                        ? error.error_description
                        : error.error_custom_msg,
                    data = null
                };
            }
            finally
            {
                log.user_id = model?.retailer_code ?? string.Empty;
                log.order_number = model?.order_number ?? string.Empty;
                log.method_name = "HomeWifiPayslipUpload";

                await _bllLog.BIAToDPELog(log);
            }
        }

        public async Task<HomeWifiCommonResponseModel> BLLHomeWifiIMEIUpdate(HomeWifiIMEIUpdateRequestModel model)
        {
            var log = new BIAToDPELog();

            string dexResponseText = string.Empty;
            object? fallbackReqLogObject = null;

            try
            {
                string endpoint = SettingsValues.GetDPEImeiUpdateEndPoint();

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Dex Portal IMEI Update End Point is not configured.",
                        data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(model.order_number))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Order number is required.",
                        data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(model.device_name))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "SKU/device name is required.",
                        data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(model.new_identifier))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "New identifier is required.",
                        data = null
                    };
                }

                string finalEndPoint = endpoint.Replace(
                    "{orderNumber}",
                    Uri.EscapeDataString(model.order_number)
                );

                /*
                    APP old_identifier will be ignored.

                    Old identifier will always come from DB:
                    TBLDEPREQUEST_DEVICE by ORDER_NUMBER + SKU.
                */
                string? dbOldIdentifier =
                    await _dataManager.GetDEPDeviceOldIdentifier(
                        model.order_number,
                        model.device_name
                    );

                if (string.IsNullOrWhiteSpace(dbOldIdentifier))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Old identifier not found from device table for this order number and SKU.",
                        data = null
                    };
                }

                var dexRequest = new
                {
                    sku = model.device_name,
                    old_identifier = dbOldIdentifier,
                    new_identifier = model.new_identifier
                };

                string finalRetailerCode = model.retailer_code ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(finalRetailerCode) &&
                    !finalRetailerCode.StartsWith("R", StringComparison.OrdinalIgnoreCase))
                {
                    finalRetailerCode = "R" + finalRetailerCode;
                }

                /*
                    This is fallback only.
                    DB req_blob will use dexApiResponse.ActualRequest when ApiCall captures it.
                */
                fallbackReqLogObject = new
                {
                    request_time = DateTime.Now,
                    method = "PATCH",
                    url = $"{SettingsValues.GetDPEBaseUrl()?.TrimEnd('/')}{finalEndPoint}",
                    inner_method_name = "BLLHomeWifiIMEIUpdate",
                    message = "Fallback request log. Actual request should come from ApiCall ActualRequest.",
                    headers = new
                    {
                        Authorization = "Bearer captured in ApiCall",
                        Accept = "application/json",
                        Content_Type = "application/json",
                        x_client_uid = SettingsValues.GetDPEChannel(),
                        x_username = finalRetailerCode
                    },
                    body = dexRequest
                };

                Log.ForContext("LogTag", "ApiCall")
                    .Information("HomeWifi IMEIUpdate Request Started: {@Request}", fallbackReqLogObject);

                JObject? dexJson = null;

                var dexApiResponse = await _apiCall.HTTPPatchRequestIMEIUpdateHomeWifi(
                    finalEndPoint,
                    model.retailer_code,
                    dexRequest,
                    "BLLHomeWifiIMEIUpdate",
                    model.order_number,
                    false // replace-item API does not need X-Order-Type
                );

                /*
                    req_blob must be actual request sent to Dex with actual headers.
                    It must come from ApiCall, not manually built BLL object.
                */
                log.req_time = DateTime.Now;
                log.req_blob = _blJson.GetGenericJsonData(
                    dexApiResponse.ActualRequest ?? fallbackReqLogObject
                );

                /*
                    Raw Dex Portal response for DB log.
                    Do not replace this with custom app response later.
                */
                dexResponseText = dexApiResponse.RawResponse ?? string.Empty;

                log.res_time = DateTime.Now;
                log.res_blob = Encoding.UTF8.GetBytes(dexResponseText ?? string.Empty);

                if (string.IsNullOrWhiteSpace(dexResponseText))
                {
                    log.is_success = 0;
                    log.message = "Empty response from Dex Portal API.";

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = log.message,
                        data = null
                    };
                }

                try
                {
                    dexJson = JsonConvert.DeserializeObject<JObject>(dexResponseText);
                }
                catch
                {
                    log.is_success = 0;
                    log.message = "Invalid response from Dex Portal API.";

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = log.message,
                        data = dexResponseText
                    };
                }

                Log.ForContext("LogTag", "ApiCall")
                    .Information("HomeWifi IMEIUpdate Raw Dex Response: {Response}", dexResponseText);

                if (dexJson == null)
                {
                    log.is_success = 0;
                    log.message = "Invalid response from Dex Portal API.";

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = log.message,
                        data = null
                    };
                }

                string dexStatus = dexJson["status"]?.ToString() ?? string.Empty;

                if (!string.Equals(dexStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                {
                    log.is_success = 0;

                    string dexMessage = dexJson["message"]?.ToString()
                                        ?? dexJson["error"]?["description"]?.ToString()
                                        ?? "IMEI update failed from Dex Portal.";

                    log.message = dexMessage.StartsWith("Dex:", StringComparison.OrdinalIgnoreCase)
                        ? dexMessage
                        : $"Dex: {dexMessage}";

                    object? responseDataObject = ConvertJTokenToObject(dexJson);

                    JToken? mappedData = GetMappedDexLeadDetailsStyleData(dexJson);

                    if (mappedData != null && mappedData.Type == JTokenType.Object)
                    {
                        var pages = await GetPageMappingFromLeadDetails(mappedData);

                        var requiredPages = await GetRequiredPagesFromLeadDetails(mappedData);

                        responseDataObject = new HomeWifiLeadDetailsResponseData
                        {
                            lead_details = ConvertJTokenToObject(mappedData),
                            pages = pages,
                            required_pages = requiredPages
                        };
                    }

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = log.message,
                        data = responseDataObject
                    };
                }

                JToken? mappedDataSuccess = GetMappedDexLeadDetailsStyleData(dexJson);

                if (mappedDataSuccess == null || mappedDataSuccess.Type != JTokenType.Object)
                {
                    log.is_success = 0;
                    log.message = "Dex Portal response data missing.";

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = log.message,
                        data = null
                    };
                }

                JObject responseData = (JObject)mappedDataSuccess;

                SetOrReplace(responseData, "retailer_code", model.retailer_code);
                SetOrReplace(responseData, "ordered_msisdn", model.ordered_msisdn);
                SetOrReplace(responseData, "is_imei_updated", 1);

                /*
                    Use DB old identifier here also.
                    APP old_identifier is ignored completely.
                */
                UpdateDeviceIdentifierInResponse(
                    responseData,
                    dbOldIdentifier,
                    model.new_identifier,
                    model.device_name
                );

                HomeWifiDEPOrderRequestModel? upsertModel =
                    BuildUpsertModelFromMappedData(responseData, model.retailer_code);

                if (upsertModel == null)
                {
                    log.is_success = 0;
                    log.message = "Unable to parse Dex Portal response.";

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = log.message,
                        data = null
                    };
                }

                /*
                    Do not pass devices[] to upsert.
                    This prevents delete/reinsert of all devices.
                */
                upsertModel.devices = null;

                upsertModel.is_imei_updated = 1;
                upsertModel.ordered_msisdn = model.ordered_msisdn;

                /*
                    APP old_identifier ignored.
                    DB old identifier will be used.
                */
                upsertModel.old_identifier = dbOldIdentifier;
                upsertModel.new_identifier = model.new_identifier;
                upsertModel.imei_device_name = model.device_name;

                var dbResponse = await _dataManager.UpsertDEPOrder(upsertModel, "IMEI_UPDATE");

                log.is_success = dbResponse.isError ? 0 : 1;
                log.message = dbResponse.message;

                /*
                    Important:
                    Do not overwrite log.res_blob here.
                    log.res_blob must remain raw Dex Portal response.
                */
                if (dbResponse.isError)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = dbResponse.message,
                        data = dbResponse.data
                    };
                }

                HomeWifiCommonResponseModel finalResponse =
                    await BuildLeadDetailsStyleFinalResponse(responseData, dbResponse.message, false);

                Log.ForContext("LogTag", "ApiRequest")
                    .Information("BLLHomeWifiIMEIUpdate Response: {@Response}", finalResponse);

                return finalResponse;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "BLLHomeWifiIMEIUpdate Exception");

                log.res_time = DateTime.Now;
                log.is_success = 0;

                /*
                    If Dex was already called, keep raw Dex response in res_blob.
                    If exception happened before Dex call, dexResponseText will be empty.
                */
                if (!string.IsNullOrWhiteSpace(dexResponseText))
                {
                    log.res_blob = Encoding.UTF8.GetBytes(dexResponseText ?? string.Empty);
                }

                if ((log.req_blob == null || log.req_blob.Length == 0) && fallbackReqLogObject != null)
                {
                    log.req_time = DateTime.Now;
                    log.req_blob = _blJson.GetGenericJsonData(fallbackReqLogObject);
                }

                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_custom_msg ?? error.error_description;

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg)
                        ? error.error_description
                        : error.error_custom_msg,
                    data = null
                };
            }
            finally
            {
                log.user_id = model?.retailer_code ?? string.Empty;
                log.order_number = model?.order_number ?? string.Empty;
                log.method_name = "HomeWifiIMEIUpdate";

                await _bllLog.BIAToDPELog(log);
            }
        }

        public async Task<HomeWifiCommonResponseModel> BLLHomeWifiPaymentMethodChangeOld(HomeWifiPaymentMethodChangeRequestModel model)
        {
            var log = new BIAToDPELog();

            try
            {
                //------------------------------------------------
                // 1. GET LEAD DETAILS FIRST
                //------------------------------------------------
                string leadDetailsEndpoint =
                    SettingsValues.GetDPELeadDetailsEndPoint();

                if (string.IsNullOrWhiteSpace(leadDetailsEndpoint))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Dex Portal Lead Details End Point is not configured.",
                        data = null
                    };
                }

                string finalLeadDetailsEndpoint =
                    $"{leadDetailsEndpoint}/{model.order_number}";

                JObject? leadJson = null;

                #region Final Code for UAT and Production

                var leadDetailsResponse =
                    await _apiCall.HTTPGetRequestHomeWifi(
                        finalLeadDetailsEndpoint,
                        model.retailer_code,
                        "BLLHomeWifiPaymentMethodChange_LeadDetails"
                    );

                string leadDetailsText =
                    JsonConvert.SerializeObject(leadDetailsResponse);

                leadJson =
                    JsonConvert.DeserializeObject<JObject>(leadDetailsText);

                #endregion

                #region Mock Response for Testing only

                //JObject mockLeadDetailsResponse =
                //    GetMockDexPortalLeadDetailsResponse(model.order_number);
                //
                //string leadDetailsText =
                //    JsonConvert.SerializeObject(mockLeadDetailsResponse);
                //
                //leadJson =
                //    JsonConvert.DeserializeObject<JObject>(leadDetailsText);

                #endregion

                if (leadJson == null)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Invalid lead details response from Dex Portal.",
                        data = null
                    };
                }

                string leadStatus =
                    leadJson["status"]?.ToString() ?? string.Empty;

                if (!string.Equals(
                        leadStatus,
                        "SUCCESS",
                        StringComparison.OrdinalIgnoreCase))
                {
                    string dexMessage = leadJson["message"]?.ToString()
                                        ?? leadJson["error"]?["description"]?.ToString()
                                        ?? "Lead details fetch failed from Dex Portal.";

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = dexMessage.StartsWith("Dex:", StringComparison.OrdinalIgnoreCase)
                            ? dexMessage
                            : $"Dex: {dexMessage}",
                        data = ConvertJTokenToObject(leadJson)
                    };
                }

                JToken? leadResponseData =
                    leadJson["payload"]?.Type == JTokenType.Object
                        ? leadJson["payload"]?["data"]
                        : null;

                if (leadResponseData == null ||
                    leadResponseData.Type == JTokenType.Null ||
                    leadResponseData.Type == JTokenType.Undefined)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Lead details data not found from Dex Portal API.",
                        data = null
                    };
                }

                leadResponseData =
                    HomeWifiLeadListResponseMapper.MapDexLeadDetailsResponseData(leadResponseData);

                string paymentType =
                    leadResponseData["payment_type"]
                        ?.ToString()
                        ?.Trim()
                        ?.ToUpper()
                    ?? string.Empty;

                //------------------------------------------------
                // 2. VALIDATE PAYMENT TYPE FROM DEX DATA
                //------------------------------------------------
                if (paymentType != "COD")
                {
                    var pages = await GetPageMappingFromLeadDetails(leadResponseData);

                    var requiredPages = await GetRequiredPagesFromLeadDetails(leadResponseData);

                    var finalData = new HomeWifiLeadDetailsResponseData
                    {
                        lead_details = ConvertJTokenToObject(leadResponseData),
                        pages = pages,
                        required_pages = requiredPages
                    };

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Payment method change is allowed only for COD orders.",
                        data = finalData
                    };
                }

                //------------------------------------------------
                // 3. SEND PAYMENT LINK
                //------------------------------------------------
                string endpoint =
                    SettingsValues.GetDPESendPaymentLinkEndPoint();

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Dex Portal Send Payment Link End Point is not configured.",
                        data = null
                    };
                }

                var dexRequest = new
                {
                    order_number = model.order_number
                };

                Log.ForContext("LogTag", "ApiCall")
                    .Information("HomeWifi PaymentMethodChange Request: {@Request}", new
                    {
                        apiUrl = $"{SettingsValues.GetDPEBaseUrl()?.TrimEnd('/')}{endpoint}",
                        retailerCode = model.retailer_code,
                        requestBody = dexRequest
                    });

                string dexResponseText = string.Empty;
                JObject? dexJson = null;

                #region Final Code for UAT and Production

                var dexResponseObj =
                    await _apiCall.HTTPPostRequestHomeWifi(
                        endpoint,
                        model.retailer_code,
                        dexRequest,
                        "BLLHomeWifiPaymentMethodChange",
                        null,
                        actualDexRequest =>
                        {
                            log.req_time = DateTime.Now;
                            log.req_blob = _blJson.GetGenericJsonData(actualDexRequest);
                        },
                        rawDexResponse =>
                        {
                            log.res_time = DateTime.Now;
                            log.res_blob = Encoding.UTF8.GetBytes(rawDexResponse ?? string.Empty);
                        }
                    );

                dexResponseText =
                    JsonConvert.SerializeObject(dexResponseObj);

                dexJson =
                    JsonConvert.DeserializeObject<JObject>(dexResponseText);

                #endregion

                #region Mock Response for Testing only

                //JObject mockDexResponse =
                //    GetMockDexPortalPaymentMethodChangeResponse(model);
                //
                //dexResponseText =
                //    JsonConvert.SerializeObject(mockDexResponse);
                //
                //dexJson =
                //    JsonConvert.DeserializeObject<JObject>(dexResponseText);

                #endregion

                Log.ForContext("LogTag", "ApiCall")
                    .Information(
                        "HomeWifi PaymentMethodChange Response: {Response}",
                        dexResponseText);

                log.res_time = DateTime.Now;

                if (log.res_blob == null || log.res_blob.Length == 0)
                {
                    log.res_blob = Encoding.UTF8.GetBytes(dexResponseText ?? string.Empty);
                }

                if (dexJson == null)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Invalid response from Dex Portal API.",
                        data = null
                    };
                }

                string dexStatus =
                    dexJson["status"]?.ToString() ?? string.Empty;

                if (!string.Equals(
                        dexStatus,
                        "SUCCESS",
                        StringComparison.OrdinalIgnoreCase))
                {
                    log.is_success = 0;

                    string dexMessage =
                        dexJson["message"]?.ToString()
                        ?? dexJson["error"]?["description"]?.ToString()
                        ?? "Payment method change failed from Dex Portal.";

                    log.message = dexMessage.StartsWith("Dex:", StringComparison.OrdinalIgnoreCase)
                        ? dexMessage
                        : $"Dex: {dexMessage}";

                    var pages = await GetPageMappingFromLeadDetails(leadResponseData);

                    var requiredPages = await GetRequiredPagesFromLeadDetails(leadResponseData);

                    var finalData = new HomeWifiLeadDetailsResponseData
                    {
                        lead_details = ConvertJTokenToObject(leadResponseData),
                        pages = pages,
                        required_pages = requiredPages
                    };

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = log.message,
                        data = finalData
                    };
                }

                //------------------------------------------------
                // 4. UPDATE DB USING UPSERT
                //------------------------------------------------
                var upsertModel = new HomeWifiDEPOrderRequestModel
                {
                    order_number = model.order_number,
                    retailer_code = model.retailer_code,
                    payment_type = model.payment_type
                };

                var dbResponse =
                    await _dataManager.UpsertDEPOrder(upsertModel, "PAYMENT_METHOD_CHANGE");

                log.is_success = dbResponse.isError ? 0 : 1;
                log.message = dbResponse.message;

                SetOrReplace((JObject)leadResponseData, "payment_type", model.payment_type);
                SetOrReplace((JObject)leadResponseData, "is_payment_method_changed", 1);

                var finalPages = await GetPageMappingFromLeadDetails(leadResponseData);

                var finalRequiredPages = await GetRequiredPagesFromLeadDetails(leadResponseData);

                var successFinalData = new HomeWifiLeadDetailsResponseData
                {
                    lead_details = ConvertJTokenToObject(leadResponseData),
                    pages = finalPages,
                    required_pages = finalRequiredPages
                };

                var finalResponse = new HomeWifiCommonResponseModel
                {
                    isError = dbResponse.isError,
                    message = dbResponse.message,
                    data = successFinalData
                };

                Log.ForContext("LogTag", "ApiRequest")
                    .Information(
                        "BLLHomeWifiPaymentMethodChange Response: {@Response}",
                        finalResponse);

                return finalResponse;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "BLLHomeWifiPaymentMethodChange Exception");

                log.res_time = DateTime.Now;
                log.is_success = 0;

                var error =
                    await _bllLog.ManageException(
                        ex,
                        ex.HResult,
                        "BIA");

                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message =
                    error.error_custom_msg ?? error.error_description;

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message =
                        string.IsNullOrEmpty(error.error_custom_msg)
                            ? error.error_description
                            : error.error_custom_msg,
                    data = null
                };
            }
            finally
            {
                log.user_id = model?.retailer_code ?? string.Empty;
                log.order_number = model?.order_number ?? string.Empty;
                log.method_name = "HomeWifiPaymentMethodChange";

                await _bllLog.BIAToDPELog(log);
            }
        }
        public async Task<HomeWifiCommonResponseModel> BLLHomeWifiPaymentMethodChange(HomeWifiPaymentMethodChangeRequestModel model)
        {
            var log = new BIAToDPELog();

            try
            {
                //------------------------------------------------
                // STEP 1: GET LEAD DETAILS FIRST
                //------------------------------------------------
                string leadDetailsEndpoint = SettingsValues.GetDPELeadDetailsEndPoint();

                if (string.IsNullOrWhiteSpace(leadDetailsEndpoint))
                {
                    var endpointMissingResult = new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Dex Portal Lead Details End Point is not configured.",
                        data = null
                    };

                    Log.ForContext("LogTag", "ApiRequest")
                       .Information(
                           "BLLHomeWifiPaymentMethodChange Response: {@Response}",
                           endpointMissingResult
                       );

                    return endpointMissingResult;
                }

                string finalLeadDetailsEndpoint =
                    $"{leadDetailsEndpoint}/{model.order_number}";

                Log.ForContext("LogTag", "ApiCall")
                   .Information(
                       "HomeWifi PaymentMethodChange LeadDetails Request: {@Request}",
                       new
                       {
                           apiUrl = finalLeadDetailsEndpoint,
                           retailerCode = model.retailer_code,
                           orderNumber = model.order_number
                       }
                   );

                JObject leadJson = null;

                #region Final Code for UAT and Production

                var leadDetailsApiResult =
                    await _apiCall.HTTPGetRequestHomeWifi(
                        finalLeadDetailsEndpoint,
                        model.retailer_code,
                        "BLLHomeWifiPaymentMethodChange_LeadDetails"
                    );

                string leadDetailsText =
                    JsonConvert.SerializeObject(leadDetailsApiResult);

                Log.ForContext("LogTag", "ApiCall")
                   .Information(
                       "HomeWifi PaymentMethodChange LeadDetails Response: {Response}",
                       leadDetailsText
                   );

                leadJson =
                    JsonConvert.DeserializeObject<JObject>(leadDetailsText);

                #endregion

                #region Mock Response for Testing only

                //JObject mockLeadDetailsResponse =
                //    GetMockDexPortalLeadDetailsResponse(model.order_number);
                //
                //string leadDetailsText =
                //    JsonConvert.SerializeObject(mockLeadDetailsResponse);
                //
                //leadJson =
                //    JsonConvert.DeserializeObject<JObject>(leadDetailsText);

                #endregion

                if (leadJson == null)
                {
                    var invalidLeadDetailsResult = new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Invalid lead details response from Dex Portal.",
                        data = null
                    };

                    Log.ForContext("LogTag", "ApiRequest")
                       .Information(
                           "BLLHomeWifiPaymentMethodChange Response: {@Response}",
                           invalidLeadDetailsResult
                       );

                    return invalidLeadDetailsResult;
                }

                string leadStatus =
                    leadJson["status"]?.ToString() ?? string.Empty;

                if (!string.Equals(
                        leadStatus,
                        "SUCCESS",
                        StringComparison.OrdinalIgnoreCase))
                {
                    string dexMessage =
                        leadJson["message"]?.ToString()
                        ?? leadJson["error"]?["description"]?.ToString()
                        ?? "Lead details fetch failed from Dex Portal.";

                    var failedLeadDetailsResult = new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = dexMessage.StartsWith("Dex:", StringComparison.OrdinalIgnoreCase)
                            ? dexMessage
                            : $"Dex: {dexMessage}",
                        data = ConvertJTokenToObject(leadJson)
                    };

                    Log.ForContext("LogTag", "ApiRequest")
                       .Information(
                           "BLLHomeWifiPaymentMethodChange Response: {@Response}",
                           failedLeadDetailsResult
                       );

                    return failedLeadDetailsResult;
                }

                //------------------------------------------------
                // KEEP RAW LEAD DATA SEPARATE
                //------------------------------------------------
                JToken rawLeadResponseData =
                    leadJson["payload"]?.Type == JTokenType.Object
                        ? leadJson["payload"]?["data"]
                        : null;

                if (rawLeadResponseData == null ||
                    rawLeadResponseData.Type == JTokenType.Null ||
                    rawLeadResponseData.Type == JTokenType.Undefined)
                {
                    var leadDataNotFoundResult = new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Lead details data not found from Dex Portal API.",
                        data = null
                    };

                    Log.ForContext("LogTag", "ApiRequest")
                       .Information(
                           "BLLHomeWifiPaymentMethodChange Response: {@Response}",
                           leadDataNotFoundResult
                       );

                    return leadDataNotFoundResult;
                }

                //------------------------------------------------
                // UI MAPPED DATA ONLY FOR RESPONSE
                //------------------------------------------------
                JToken mappedLeadResponseData =
                    HomeWifiLeadListResponseMapper.MapDexLeadDetailsResponseData(rawLeadResponseData);

                string dexPaymentType =
                    mappedLeadResponseData["payment_type"]
                        ?.ToString()
                        ?.Trim()
                        ?.ToUpper()
                    ?? string.Empty;

                string requestedPaymentType =
                    model.payment_type
                        ?.ToString()
                        ?.Trim()
                        ?.ToUpper()
                    ?? string.Empty;

                //------------------------------------------------
                // STEP 2: COD FLOW
                //------------------------------------------------
                if (requestedPaymentType == "COD")
                {
                    if (dexPaymentType != "COD")
                    {
                        var pages = await GetPageMappingFromLeadDetails(rawLeadResponseData);
                        var requiredPages = await GetRequiredPagesFromLeadDetails(rawLeadResponseData);

                        var paidOrderCodBlockedResult = new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = "Paid order cannot be changed to COD.",
                            data = new HomeWifiLeadDetailsResponseData
                            {
                                lead_details = ConvertJTokenToObject(mappedLeadResponseData),
                                pages = pages,
                                required_pages = requiredPages
                            }
                        };

                        Log.ForContext("LogTag", "ApiRequest")
                           .Information(
                               "BLLHomeWifiPaymentMethodChange Response: {@Response}",
                               paidOrderCodBlockedResult
                           );

                        return paidOrderCodBlockedResult;
                    }

                    var codUpsertModel = new HomeWifiDEPOrderRequestModel
                    {
                        order_number = model.order_number,
                        retailer_code = model.retailer_code,
                        payment_type = "COD",
                        is_payment_method_changed = 0
                    };

                    Log.ForContext("LogTag", "DbCall")
                       .Information(
                           "HomeWifi PaymentMethodChange COD Upsert Request: {@Request}",
                           codUpsertModel
                       );

                    var codDbResult =
                        await _dataManager.UpsertDEPOrder(
                            codUpsertModel,
                            "PAYMENT_METHOD_CHANGE"
                        );

                    Log.ForContext("LogTag", "DbCall")
                       .Information(
                           "HomeWifi PaymentMethodChange COD Upsert Response: {@Response}",
                           codDbResult
                       );

                    log.is_success = codDbResult.isError ? 0 : 1;
                    log.message = codDbResult.message;

                    var pagesCod = await GetPageMappingFromLeadDetails(rawLeadResponseData);
                    var requiredPagesCod = await GetRequiredPagesFromLeadDetails(rawLeadResponseData);

                    var codFinalResult = new HomeWifiCommonResponseModel
                    {
                        isError = codDbResult.isError,
                        message = codDbResult.message,
                        data = new HomeWifiLeadDetailsResponseData
                        {
                            lead_details = ConvertJTokenToObject(mappedLeadResponseData),
                            pages = pagesCod,
                            required_pages = requiredPagesCod
                        }
                    };

                    Log.ForContext("LogTag", "ApiRequest")
                       .Information(
                           "BLLHomeWifiPaymentMethodChange Response: {@Response}",
                           codFinalResult
                       );

                    return codFinalResult;
                }

                //------------------------------------------------
                // STEP 3: ONLINE FLOW
                // IMPORTANT: API TRIGGER SAME AS OLD METHOD
                //------------------------------------------------

                //------------------------------------------------
                // 3.1 VALIDATE PAYMENT TYPE FROM DEX DATA
                //------------------------------------------------
                if (dexPaymentType != "COD")
                {
                    var pages = await GetPageMappingFromLeadDetails(rawLeadResponseData);
                    var requiredPages = await GetRequiredPagesFromLeadDetails(rawLeadResponseData);

                    var finalData = new HomeWifiLeadDetailsResponseData
                    {
                        lead_details = ConvertJTokenToObject(mappedLeadResponseData),
                        pages = pages,
                        required_pages = requiredPages
                    };

                    var onlineNotAllowedResult = new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Payment method change is allowed only for COD orders.",
                        data = finalData
                    };

                    Log.ForContext("LogTag", "ApiRequest")
                       .Information(
                           "BLLHomeWifiPaymentMethodChange Response: {@Response}",
                           onlineNotAllowedResult
                       );

                    return onlineNotAllowedResult;
                }

                //------------------------------------------------
                // 3.2 SEND PAYMENT LINK API
                //------------------------------------------------
                string endpoint =
                    SettingsValues.GetDPESendPaymentLinkEndPoint();

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    var paymentLinkEndpointMissingResult = new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Dex Portal Send Payment Link End Point is not configured.",
                        data = null
                    };

                    Log.ForContext("LogTag", "ApiRequest")
                       .Information(
                           "BLLHomeWifiPaymentMethodChange Response: {@Response}",
                           paymentLinkEndpointMissingResult
                       );

                    return paymentLinkEndpointMissingResult;
                }

                var dexRequest = new
                {
                    order_number = model.order_number
                };

                Log.ForContext("LogTag", "ApiCall")
                   .Information(
                       "HomeWifi PaymentMethodChange Request: {@Request}",
                       new
                       {
                           apiUrl = $"{SettingsValues.GetDPEBaseUrl()?.TrimEnd('/')}{endpoint}",
                           retailerCode = model.retailer_code,
                           requestBody = dexRequest
                       }
                   );

                string dexResponseText = string.Empty;
                JObject dexJson = null;

                #region Final Code for UAT and Production

                var dexResponseObj =
                    await _apiCall.HTTPPostRequestHomeWifi(
                        endpoint,
                        model.retailer_code,
                        dexRequest,
                        "BLLHomeWifiPaymentMethodChange",
                        null,
                        actualDexRequest =>
                        {
                            log.req_time = DateTime.Now;
                            log.req_blob = _blJson.GetGenericJsonData(actualDexRequest);
                        },
                        rawDexResponse =>
                        {
                            log.res_time = DateTime.Now;
                            log.res_blob = Encoding.UTF8.GetBytes(rawDexResponse ?? string.Empty);
                        }
                    );

                dexResponseText =
                    JsonConvert.SerializeObject(dexResponseObj);

                dexJson =
                    JsonConvert.DeserializeObject<JObject>(dexResponseText);

                #endregion

                #region Mock Response for Testing only

                //JObject mockDexResponse =
                //    GetMockDexPortalPaymentMethodChangeResponse(model);
                //
                //dexResponseText =
                //    JsonConvert.SerializeObject(mockDexResponse);
                //
                //dexJson =
                //    JsonConvert.DeserializeObject<JObject>(dexResponseText);

                #endregion

                Log.ForContext("LogTag", "ApiCall")
                   .Information(
                       "HomeWifi PaymentMethodChange Response: {Response}",
                       dexResponseText
                   );

                log.res_time = DateTime.Now;

                if (log.res_blob == null || log.res_blob.Length == 0)
                {
                    log.res_blob = Encoding.UTF8.GetBytes(dexResponseText ?? string.Empty);
                }

                if (dexJson == null)
                {
                    var invalidDexResult = new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Invalid response from Dex Portal API.",
                        data = null
                    };

                    Log.ForContext("LogTag", "ApiRequest")
                       .Information(
                           "BLLHomeWifiPaymentMethodChange Response: {@Response}",
                           invalidDexResult
                       );

                    return invalidDexResult;
                }

                string dexStatus =
                    dexJson["status"]?.ToString() ?? string.Empty;

                if (!string.Equals(
                        dexStatus,
                        "SUCCESS",
                        StringComparison.OrdinalIgnoreCase))
                {
                    log.is_success = 0;

                    string dexMessage =
                        dexJson["message"]?.ToString()
                        ?? dexJson["error"]?["description"]?.ToString()
                        ?? "Payment method change failed from Dex Portal.";

                    log.message = dexMessage.StartsWith("Dex:", StringComparison.OrdinalIgnoreCase)
                        ? dexMessage
                        : $"Dex: {dexMessage}";

                    var pages = await GetPageMappingFromLeadDetails(rawLeadResponseData);
                    var requiredPages = await GetRequiredPagesFromLeadDetails(rawLeadResponseData);

                    var finalData = new HomeWifiLeadDetailsResponseData
                    {
                        lead_details = ConvertJTokenToObject(mappedLeadResponseData),
                        pages = pages,
                        required_pages = requiredPages
                    };

                    var dexFailedResult = new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = log.message,
                        data = finalData
                    };

                    Log.ForContext("LogTag", "ApiRequest")
                       .Information(
                           "BLLHomeWifiPaymentMethodChange Response: {@Response}",
                           dexFailedResult
                       );

                    return dexFailedResult;
                }

                //------------------------------------------------
                // STEP 4: UPDATE DB USING UPSERT AFTER API SUCCESS
                //------------------------------------------------
                var onlineUpsertModel = new HomeWifiDEPOrderRequestModel
                {
                    order_number = model.order_number,
                    retailer_code = model.retailer_code,
                    is_payment_method_changed = 1
                };

                Log.ForContext("LogTag", "DbCall")
                   .Information(
                       "HomeWifi PaymentMethodChange ONLINE Upsert Request: {@Request}",
                       onlineUpsertModel
                   );

                var onlineDbResult =
                    await _dataManager.UpsertDEPOrder(
                        onlineUpsertModel,
                        "PAYMENT_METHOD_CHANGE"
                    );

                Log.ForContext("LogTag", "DbCall")
                   .Information(
                       "HomeWifi PaymentMethodChange ONLINE Upsert Response: {@Response}",
                       onlineDbResult
                   );

                log.is_success = onlineDbResult.isError ? 0 : 1;
                log.message = onlineDbResult.message;

                SetOrReplace((JObject)mappedLeadResponseData, "payment_type", requestedPaymentType);
                SetOrReplace((JObject)mappedLeadResponseData, "is_payment_method_changed", 1);

                var finalPages = await GetPageMappingFromLeadDetails(rawLeadResponseData);
                var finalRequiredPages = await GetRequiredPagesFromLeadDetails(rawLeadResponseData);

                var successFinalData = new HomeWifiLeadDetailsResponseData
                {
                    lead_details = ConvertJTokenToObject(mappedLeadResponseData),
                    pages = finalPages,
                    required_pages = finalRequiredPages
                };

                var onlineFinalResult = new HomeWifiCommonResponseModel
                {
                    isError = onlineDbResult.isError,
                    message = onlineDbResult.message,
                    data = successFinalData
                };

                Log.ForContext("LogTag", "ApiRequest")
                   .Information(
                       "BLLHomeWifiPaymentMethodChange Response: {@Response}",
                       onlineFinalResult
                   );

                return onlineFinalResult;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "BLLHomeWifiPaymentMethodChange Exception");

                log.res_time = DateTime.Now;
                log.is_success = 0;

                var error =
                    await _bllLog.ManageException(
                        ex,
                        ex.HResult,
                        "BIA"
                    );

                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message =
                    error.error_custom_msg ?? error.error_description;

                var exceptionResult = new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message =
                        string.IsNullOrEmpty(error.error_custom_msg)
                            ? error.error_description
                            : error.error_custom_msg,
                    data = null
                };

                Log.ForContext("LogTag", "ApiRequest")
                   .Information(
                       "BLLHomeWifiPaymentMethodChange Error Response: {@Response}",
                       exceptionResult
                   );

                return exceptionResult;
            }
            finally
            {
                log.user_id = model?.retailer_code ?? string.Empty;
                log.order_number = model?.order_number ?? string.Empty;
                log.method_name = "HomeWifiPaymentMethodChange";

                await _bllLog.BIAToDPELog(log);
            }
        }
        public async Task<HomeWifiCommonResponseModel> BLLHomeWifiLeadList(HomeWifiLeadListRequestModel model)
        {
            var log = new BIAToDPELog();
            string endpoint = string.Empty;
            string txtResp = string.Empty;

            try
            {
                string baseEndpoint = SettingsValues.GetDPELeadListEndPoint();

                int perPage = SettingsValues.GetDexPerPageValue();
                if (perPage <= 0)
                {
                    perPage = 100; // fallback default
                }

                int page = 1;

                string separator = baseEndpoint.Contains("?") ? "&" : "?";

                endpoint = $"{baseEndpoint}{separator}page={page}&per_page={perPage}";

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Dex Portal Lead List End Point is not configured.",
                        data = null
                    };
                }

                log.req_time = DateTime.Now;

                #region Final Code for UAT and Production

                var responseObj = await _apiCall.HTTPGetRequestHomeWifi(
                    endpoint,
                    model?.retailer_code ?? string.Empty,
                    "BLLHomeWifiLeadList",
                    null,
                    actualDexRequest =>
                    {
                        log.req_time = DateTime.Now;
                        log.req_blob = _blJson.GetGenericJsonData(actualDexRequest);
                    },
                    rawDexResponse =>
                    {
                        log.res_time = DateTime.Now;
                        log.res_blob = Encoding.UTF8.GetBytes(rawDexResponse ?? string.Empty);
                    }
                );

                if (responseObj is HomeWifiCommonResponseModel commonResponse)
                {
                    if (commonResponse.isError)
                    {
                        string dexMessage = commonResponse.message ?? "Dex Portal API request failed.";

                        try
                        {
                            JObject errorJson = JObject.Parse(dexMessage);

                            dexMessage = errorJson["message"]?.ToString()
                                         ?? errorJson["error"]?["description"]?.ToString()
                                         ?? "Dex Portal API request failed.";
                        }
                        catch
                        {
                            // keep existing message
                        }

                        commonResponse.message = dexMessage.StartsWith("Dex:", StringComparison.OrdinalIgnoreCase)
                            ? dexMessage
                            : $"Dex: {dexMessage}";
                    }

                    return commonResponse;
                }

                txtResp = JsonConvert.SerializeObject(responseObj);

                #endregion

                #region Mock Response for Testing only

                //try
                //{
                //    JObject mockDexResponse = GetMockDexPortalLeadListResponse();
                //
                //    txtResp = JsonConvert.SerializeObject(mockDexResponse);
                //}
                //catch
                //{
                //    JObject mockDexResponse = GetMockDexPortalLeadListResponse();
                //
                //    txtResp = JsonConvert.SerializeObject(mockDexResponse);
                //}

                #endregion

                log.res_time = DateTime.Now;

                if (log.res_blob == null || log.res_blob.Length == 0)
                {
                    log.res_blob = Encoding.UTF8.GetBytes(txtResp ?? string.Empty);
                }

                log.is_success = 1;

                if (string.IsNullOrWhiteSpace(txtResp))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Empty response from Dex Portal API.",
                        data = null
                    };
                }

                JObject? dexPortalJson = JsonConvert.DeserializeObject<JObject>(txtResp);

                if (dexPortalJson == null)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Invalid response from Dex Portal API.",
                        data = null
                    };
                }

                if (dexPortalJson["status"]?.ToString()
                        .Equals("FAILED", StringComparison.OrdinalIgnoreCase) == true)
                {
                    string dexMessage = dexPortalJson["message"]?.ToString()
                                        ?? dexPortalJson["error"]?["description"]?.ToString()
                                        ?? "Dex Portal API request failed.";

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = dexMessage.StartsWith("Dex:", StringComparison.OrdinalIgnoreCase)
                            ? dexMessage
                            : $"Dex: {dexMessage}",
                        data = null
                    };
                }

                JToken? payload = dexPortalJson["payload"];
                JToken? responseData = null;

                if (payload != null && payload.Type == JTokenType.Object)
                {
                    JToken? data = payload["data"];

                    if (data != null)
                    {
                        if (data.Type == JTokenType.Object)
                        {
                            responseData = data["list"] ?? data;
                        }
                        else if (data.Type == JTokenType.Array)
                        {
                            responseData = data;
                        }
                    }
                }

                /*
                    Mapping Dex response into BI/App response format.
                */
                responseData = HomeWifiLeadListResponseMapper.MapDexLeadListResponseData(responseData);

                /*
                    Hide these orders from lead list:
                    - CANCELED
                    - CANCELLED
                    - DELIVERED
                */
                bool ShouldHideOrder(string orderStatus)
                {
                    return string.Equals(orderStatus, "CANCELED", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(orderStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(orderStatus, "DELIVERED", StringComparison.OrdinalIgnoreCase);
                }

                if (responseData != null && responseData.Type == JTokenType.Array)
                {
                    JArray filteredList = new JArray();

                    foreach (JToken item in responseData)
                    {
                        string orderStatus =
                            item?["order_status"]?.ToString()?.Trim() ?? string.Empty;

                        if (!ShouldHideOrder(orderStatus))
                        {
                            filteredList.Add(item);
                        }
                    }

                    responseData = filteredList;
                }
                else if (responseData != null && responseData.Type == JTokenType.Object)
                {
                    string orderStatus =
                        responseData?["order_status"]?.ToString()?.Trim() ?? string.Empty;

                    if (ShouldHideOrder(orderStatus))
                    {
                        responseData = new JArray();
                    }
                }

                var finalResponse = ConvertJTokenToObject(responseData);

                return new HomeWifiCommonResponseModel
                {
                    isError = false,
                    message = dexPortalJson["message"]?.ToString()
                              ?? "Lead list fetched successfully.",
                    data = finalResponse
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "HomeWifi LeadList Exception");

                log.res_time = DateTime.Now;

                var error = await _bllLog.ManageException(
                    ex,
                    ex.HResult,
                    "BIA"
                );

                log.res_blob = _blJson.GetGenericJsonData(error);
                log.is_success = 0;
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_custom_msg ?? error.error_description;

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg)
                        ? error.error_description
                        : error.error_custom_msg,
                    data = null
                };
            }
            finally
            {
                log.user_id = model?.retailer_code ?? string.Empty;
                log.method_name = "HomeWifiLeadList";

                await _bllLog.BIAToDPELog(log);
            }
        }

        public async Task<HomeWifiCommonResponseModel> BLLHomeWifiLeadDetails(HomeWifiLeadDetailsRequestModel model)
        {
            var log = new BIAToDPELog();
            string endpoint = string.Empty;
            string txtResp = string.Empty;

            try
            {
                endpoint = SettingsValues.GetDPELeadDetailsEndPoint();

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Dex Portal Lead Details End Point is not configured.",
                        data = null
                    };
                }

                string finalEndPoint = $"{endpoint}/{model?.order_number}";

                log.req_time = DateTime.Now;
                log.req_blob = _blJson.GetGenericJsonData(new
                {
                    apiUrl = $"{SettingsValues.GetDPEBaseUrl()?.TrimEnd('/')}{finalEndPoint}"
                });

                #region Final Code for UAT and Production

                var responseObj = await _apiCall.HTTPGetRequestHomeWifi(
                    finalEndPoint,
                    model?.retailer_code ?? string.Empty,
                    "BLLHomeWifiLeadDetails"
                );

                if (responseObj is HomeWifiCommonResponseModel commonResponse)
                {
                    if (commonResponse.isError)
                    {
                        string dexMessage = commonResponse.message ?? "Dex Portal API request failed.";

                        try
                        {
                            JObject errorJson = JObject.Parse(dexMessage);

                            dexMessage = errorJson["message"]?.ToString()
                                         ?? errorJson["error"]?["description"]?.ToString()
                                         ?? "Dex Portal API request failed.";
                        }
                        catch
                        {
                            // keep existing message
                        }

                        commonResponse.message = dexMessage.StartsWith("Dex:", StringComparison.OrdinalIgnoreCase)
                            ? dexMessage
                            : $"Dex: {dexMessage}";
                    }

                    return commonResponse;
                }

                txtResp = JsonConvert.SerializeObject(responseObj);

                #endregion

                log.res_time = DateTime.Now;
                log.res_blob = Encoding.UTF8.GetBytes(txtResp);
                log.is_success = 1;

                Log.ForContext("LogTag", "ApiRequest")
                    .Information("HomeWifi LeadDetails Response: {Response}", txtResp);

                JObject? dexPortalJson = JsonConvert.DeserializeObject<JObject>(txtResp);

                if (dexPortalJson == null)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Invalid response from Dex Portal API.",
                        data = null
                    };
                }

                if (dexPortalJson["status"]?.ToString()
                        .Equals("FAILED", StringComparison.OrdinalIgnoreCase) == true)
                {
                    string dexMessage = dexPortalJson["message"]?.ToString()
                                        ?? dexPortalJson["error"]?["description"]?.ToString()
                                        ?? "Dex Portal API request failed.";

                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = dexMessage.StartsWith("Dex:", StringComparison.OrdinalIgnoreCase)
                            ? dexMessage
                            : $"Dex: {dexMessage}",
                        data = null
                    };
                }

                JToken? responseData =
                    dexPortalJson["payload"]?.Type == JTokenType.Object
                        ? dexPortalJson["payload"]?["data"]
                        : null;

                if (responseData == null ||
                    responseData.Type == JTokenType.Null ||
                    responseData.Type == JTokenType.Undefined)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Lead details data not found from Dex Portal API.",
                        data = null
                    };
                }

                responseData = HomeWifiLeadListResponseMapper.MapDexLeadDetailsResponseData(responseData);

                /*
                    DB fallback alignment.

                    Purpose:
                    If Dex Portal response has null/empty value,
                    but DB has value, then DB value will be used.

                    Dex value will NOT be overwritten if Dex already has value.
                */
                JObject? dbFallbackData = await GetLeadDetailsDbFallbackData(
                    responseData,
                    model?.order_number
                );

                if (dbFallbackData != null && dbFallbackData.HasValues)
                {
                    ApplyDbFallbackForMissingDexValues(responseData, dbFallbackData);
                }

                /*
                    Lead Details response should NOT include Payslip Upload page.
                    Order Status by Retailer will still handle Payslip Upload separately.
                */
                var pages = await GetPageMappingFromLeadDetails(responseData, false);

                var requiredPages = await GetRequiredPagesFromLeadDetails(responseData);

                var finalData = new HomeWifiLeadDetailsResponseData
                {
                    lead_details = ConvertJTokenToObject(responseData),
                    pages = pages,
                    required_pages = requiredPages
                };

                return new HomeWifiCommonResponseModel
                {
                    isError = false,
                    message = dexPortalJson["message"]?.ToString()
                              ?? "Lead details fetched successfully.",
                    data = finalData
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "HomeWifi LeadDetails Exception");

                log.res_time = DateTime.Now;

                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                log.res_blob = _blJson.GetGenericJsonData(error);
                log.is_success = 0;
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_custom_msg ?? error.error_description;

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg)
                        ? error.error_description
                        : error.error_custom_msg,
                    data = null
                };
            }
            finally
            {
                log.user_id = model?.retailer_code ?? string.Empty;
                log.order_number = model?.order_number ?? string.Empty;
                log.method_name = "HomeWifiLeadDetails";

                await _bllLog.BIAToDPELog(log);
            }
        }

        public async Task<HomeWifiCommonResponseModel> BLLHomeWifiCheckPaymentOld(HomeWifiLeadDetailsRequestModel model)
        {
            try
            {
                /*
                    This method reuses the existing Lead Details logic.

                    It will call BLLHomeWifiLeadDetails, receive the mapped lead details response,
                    extract payment_type and payment_status from lead_details,
                    then apply the required payment validation.
                */

                var leadDetailsResponse = await BLLHomeWifiLeadDetails(model);

                if (leadDetailsResponse == null)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Lead details response not found.",
                        data = null
                    };
                }

                if (leadDetailsResponse.isError)
                {
                    return leadDetailsResponse;
                }

                if (leadDetailsResponse.data == null)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Lead details data not found.",
                        data = null
                    };
                }

                string? paymentType = GetLeadDetailsStringValue(
                    leadDetailsResponse.data,
                    "payment_type",
                    "paymentType",
                    "PAYMENT_TYPE"
                );

                string? paymentStatus = GetLeadDetailsStringValue(
                    leadDetailsResponse.data,
                    "payment_status",
                    "paymentStatus",
                    "PAYMENT_STATUS"
                );

                if (string.IsNullOrWhiteSpace(paymentType))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "payment_type not found from lead details.",
                        data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(paymentStatus))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "payment_status not found from lead details.",
                        data = null
                    };
                }

                bool isOnlinePayment = IsSamePaymentValue(paymentType, "ONLINE");
                bool isUnpaid = IsSamePaymentValue(paymentStatus, "UNPAID");

                if (isOnlinePayment && isUnpaid)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Please pay First.",
                        data = new
                        {
                            payment_type = paymentType,
                            payment_status = paymentStatus
                        }
                    };
                }

                return new HomeWifiCommonResponseModel
                {
                    isError = false,
                    message = "Payment check successful.",
                    data = new
                    {
                        payment_type = paymentType,
                        payment_status = paymentStatus
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "HomeWifi CheckPayment Exception");

                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = string.IsNullOrEmpty(error.error_custom_msg)
                        ? error.error_description
                        : error.error_custom_msg,
                    data = null
                };
            }
        }

        public async Task<HomeWifiCommonResponseModel> BLLHomeWifiCheckPayment(HomeWifiLeadDetailsRequestModel model)
        {
            try
            {
                // =====================================================
                // STEP 1: FETCH DB STATE (DAL LAYER)
                // =====================================================
                int isPaymentMethodChanged =
                    await _dataManager.GetIsPaymentMethodChanged(model.order_number);

                // =====================================================
                // STEP 2: GET LEAD DETAILS
                // =====================================================
                var leadDetailsResponse = await BLLHomeWifiLeadDetails(model);

                if (leadDetailsResponse == null || leadDetailsResponse.data == null)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Lead details not found.",
                        data = null
                    };
                }

                if (leadDetailsResponse.isError)
                {
                    return leadDetailsResponse;
                }

                string? paymentType = GetLeadDetailsStringValue(
                    leadDetailsResponse.data,
                    "payment_type",
                    "paymentType",
                    "PAYMENT_TYPE"
                );

                string? paymentStatus = GetLeadDetailsStringValue(
                    leadDetailsResponse.data,
                    "payment_status",
                    "paymentStatus",
                    "PAYMENT_STATUS"
                );

                if (string.IsNullOrWhiteSpace(paymentType) || string.IsNullOrWhiteSpace(paymentStatus))
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Payment data missing from lead details.",
                        data = null
                    };
                }

                bool isOnline = IsSamePaymentValue(paymentType, "ONLINE");
                bool isPaid = IsSamePaymentValue(paymentStatus, "PAID");
                bool isUnpaid = IsSamePaymentValue(paymentStatus, "UNPAID");

                // =====================================================
                // CASE 1: PAYMENT METHOD CHANGED (STRICT MODE)
                // =====================================================
                if (isPaymentMethodChanged == 1)
                {
                    // ❌ INVALID STATE
                    if (!isOnline && !isPaid)
                    {
                        return new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = "Please complete the payment first before proceeding.",
                            data = new
                            {
                                payment_type = paymentType,
                                payment_status = paymentStatus
                            }
                        };
                    }

                    // ✅ VALID → UPSERT REQUIRED
                    if (isOnline && isPaid)
                    {
                        await BLLUpsertDEPOrder(new HomeWifiDEPOrderRequestModel
                        {
                            order_number = model.order_number,
                            payment_type = paymentType,
                            payment_status = paymentStatus
                        });

                        return new HomeWifiCommonResponseModel
                        {
                            isError = false,
                            message = "Payment verified and updated successfully.",
                            data = new
                            {
                                payment_type = paymentType,
                                payment_status = paymentStatus
                            }
                        };
                    }

                    return new HomeWifiCommonResponseModel
                    {
                        isError = false,
                        message = "Please complete the payment first before proceeding.",
                        data = new
                        {
                            payment_type = paymentType,
                            payment_status = paymentStatus
                        }
                    };
                }

                // =====================================================
                // CASE 2: NORMAL FLOW
                // =====================================================
                if (isOnline && isUnpaid)
                {
                    return new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Please complete payment first.",
                        data = new
                        {
                            payment_type = paymentType,
                            payment_status = paymentStatus
                        }
                    };
                }

                return new HomeWifiCommonResponseModel
                {
                    isError = false,
                    message = "Payment validation successful.",
                    data = new
                    {
                        payment_type = paymentType,
                        payment_status = paymentStatus
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "HomeWifi CheckPayment Exception");

                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = error.error_custom_msg ?? error.error_description,
                    data = null
                };
            }
        }



        #region HELPER METHODS

        private object? ConvertJTokenToObject(JToken? token)
        {
            if (token == null)
                return null;

            if (token is JArray array)
            {
                return array.Select(ConvertJTokenToObject).ToList();
            }

            if (token is JObject obj)
            {
                return obj.Properties()
                    .ToDictionary(
                        p => p.Name,
                        p => ConvertJTokenToObject(p.Value)
                    );
            }

            return ((JValue)token).Value;
        }
        private JToken GetSubmitOrderResponseData(HomeWifiDEPOrderRequestModel model, JObject? dexJson)
        {
            JToken? dexData =
                dexJson?["payload"]?.Type == JTokenType.Object
                    ? dexJson["payload"]?["data"]
                    : null;

            JToken? mappedData = null;

            if (dexData != null &&
                dexData.Type != JTokenType.Null &&
                dexData.Type != JTokenType.Undefined)
            {
                /*
                    DeX response format is same as LeadDetails API.
                    So map DEVICE/SIM items into flat APP response fields.
                    DEVICE.identifier will be mapped only as device_identifier.
                */
                mappedData = HomeWifiLeadListResponseMapper.MapDexLeadDetailsResponseData(dexData);
            }

            if (mappedData == null ||
                mappedData.Type == JTokenType.Null ||
                mappedData.Type == JTokenType.Undefined)
            {
                mappedData = BuildSubmitOrderResponseDataFromModel(model);
            }

            if (mappedData.Type == JTokenType.Object)
            {
                var obj = (JObject)mappedData;

                SetIfMissingOrNull(obj, "retailer_code", model.retailer_code);
                SetIfMissingOrNull(obj, "remarks", model.remarks);
                SetIfMissingOrNull(obj, "cancelation_reason", model.cancelation_reason);

                SetIfMissingOrNull(obj, "is_canceled", model.is_canceled ?? 0);
                SetIfMissingOrNull(obj, "is_payslip_uploaded", model.is_payslip_uploaded ?? 0);
                SetIfMissingOrNull(obj, "is_imei_updated", model.is_imei_updated ?? 0);

            }

            return mappedData;
        }

        private JObject BuildSubmitOrderResponseDataFromModel(HomeWifiDEPOrderRequestModel model)
        {
            var responseData = new JObject();

            responseData["order_number"] = ToActionResponseJToken(model.order_number);
            responseData["mobile"] = ToActionResponseJToken(model.mobile);
            responseData["alternate_mobile"] = ToActionResponseJToken(model.alternate_mobile);
            responseData["customer_name"] = ToActionResponseJToken(model.customer_name);
            responseData["email"] = ToActionResponseJToken(model.email);

            responseData["offer_name"] = ToActionResponseJToken(model.offer_name);
            responseData["offer_code"] = ToActionResponseJToken(model.offer_code);

            /*
                v1.7 supports multiple DEVICE items.
                So old flat fields are removed:
                device_name, device_identifier, device_color,
                device_brand, device_model, device_imei.
            */
            responseData["devices"] = BuildDeviceArrayFromModel(model.devices);

            responseData["delivery_address"] = ToActionResponseJToken(model.delivery_address);
            responseData["district"] = ToActionResponseJToken(model.district);
            responseData["area"] = ToActionResponseJToken(model.area);

            responseData["payment_type"] = ToActionResponseJToken(model.payment_type);
            responseData["total_amount"] = ToActionResponseJToken(model.total_amount);
            responseData["payment_status"] = ToActionResponseJToken(model.payment_status);

            responseData["order_date"] = ToActionResponseJToken(model.order_date);
            responseData["order_assigned_at"] = ToActionResponseJToken(model.order_assigned_at);
            responseData["appointment_date"] = ToActionResponseJToken(model.appointment_date);

            responseData["nw_assess_id"] = ToActionResponseJToken(model.nw_assess_id);
            responseData["nw_assess_status"] = ToActionResponseJToken(model.nw_assess_status);

            responseData["order_type"] = ToActionResponseJToken(model.order_type);
            responseData["order_status"] = ToActionResponseJToken(model.order_status);
            responseData["initiator_channel"] = ToActionResponseJToken(model.initiator_channel);

            responseData["subscription_type"] = ToActionResponseJToken(model.subscription_type);
            responseData["simkit_type"] = ToActionResponseJToken(model.simkit_type);

            responseData["ordered_msisdn"] = ToActionResponseJToken(model.ordered_msisdn);

            responseData["retailer_code"] = ToActionResponseJToken(model.retailer_code);
            responseData["remarks"] = ToActionResponseJToken(model.remarks);
            responseData["cancelation_reason"] = ToActionResponseJToken(model.cancelation_reason);

            responseData["is_canceled"] = ToActionResponseJToken(model.is_canceled ?? 0);
            responseData["is_payslip_uploaded"] = ToActionResponseJToken(model.is_payslip_uploaded ?? 0);
            responseData["is_imei_updated"] = ToActionResponseJToken(model.is_imei_updated ?? 0);

            return responseData;
        }
        private JArray BuildDeviceArrayFromModel(List<HomeWifiDeviceItemModel>? devices)
        {
            var deviceArray = new JArray();

            if (devices == null || devices.Count == 0)
            {
                return deviceArray;
            }

            foreach (var device in devices)
            {
                deviceArray.Add(new JObject
                {
                    ["sku"] = ToActionResponseJToken(device.sku),
                    ["identifier"] = ToActionResponseJToken(device.identifier),
                    ["name"] = ToActionResponseJToken(device.name),
                    ["brand"] = ToActionResponseJToken(device.brand),
                    ["model"] = ToActionResponseJToken(device.model),
                    ["color"] = ToActionResponseJToken(device.color),
                    ["offer_code"] = ToActionResponseJToken(device.offer_code),
                    ["offer_name"] = ToActionResponseJToken(device.offer_name)
                });
            }

            return deviceArray;
        }

        private static void SetIfMissingOrNull(JObject target, string propertyName, object? value)
        {
            if (target[propertyName] != null &&
                target[propertyName]?.Type != JTokenType.Null &&
                !string.IsNullOrWhiteSpace(target[propertyName]?.ToString()))
            {
                return;
            }

            target[propertyName] = ToActionResponseJToken(value);
        }

        private JToken? GetMappedDexLeadDetailsStyleData(JObject? dexJson)
        {
            JToken? dexData =
                dexJson?["payload"]?.Type == JTokenType.Object
                    ? dexJson["payload"]?["data"]
                    : null;

            if (dexData == null ||
                dexData.Type == JTokenType.Null ||
                dexData.Type == JTokenType.Undefined)
            {
                return null;
            }

            return HomeWifiLeadListResponseMapper.MapDexLeadDetailsResponseData(dexData);
        }

        private HomeWifiDEPOrderRequestModel? BuildUpsertModelFromMappedData(JObject mappedData, string? retailerCode)
        {
            HomeWifiDEPOrderRequestModel? upsertModel =
                mappedData.ToObject<HomeWifiDEPOrderRequestModel>();

            if (upsertModel == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(retailerCode))
            {
                upsertModel.retailer_code = retailerCode;
            }

            return upsertModel;
        }

        private async Task<HomeWifiCommonResponseModel> BuildLeadDetailsStyleFinalResponse(JToken responseData, string message, bool isError)
        {
            var pages = await GetPageMappingFromLeadDetails(responseData);

            var requiredPages = await GetRequiredPagesFromLeadDetails(responseData);

            var finalData = new HomeWifiLeadDetailsResponseData
            {
                lead_details = ConvertJTokenToObject(responseData),
                pages = pages,
                required_pages = requiredPages
            };

            return new HomeWifiCommonResponseModel
            {
                isError = isError,
                message = message,
                data = finalData
            };
        }

        private static void SetOrReplace(JObject target, string propertyName, object? value)
        {
            target[propertyName] = ToActionResponseJToken(value);
        }

        private static JToken ToActionResponseJToken(object? value)
        {
            return value == null
                ? JValue.CreateNull()
                : JToken.FromObject(value);
        }

        private string GetColumnString(DataRow row, string columnName)
        {
            if (row == null ||
                row.Table == null ||
                !row.Table.Columns.Contains(columnName) ||
                row[columnName] == DBNull.Value)
            {
                return string.Empty;
            }

            return row[columnName]?.ToString() ?? string.Empty;
        }

        private object GetDevicesFromStatusRow(DataRow row)
        {
            try
            {
                if (row == null ||
                    row.Table == null ||
                    !row.Table.Columns.Contains("DEVICES_JSON") ||
                    row["DEVICES_JSON"] == DBNull.Value)
                {
                    return new List<object>();
                }

                string devicesJson = row["DEVICES_JSON"]?.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(devicesJson))
                {
                    return new List<object>();
                }

                JArray devicesArray = JArray.Parse(devicesJson);

                return ConvertJTokenToObject(devicesArray) ?? new List<object>();
            }
            catch
            {
                return new List<object>();
            }
        }

        private static bool IsNetworkAssessSuccess(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalized = value.Trim();

            return
                normalized.Equals("pass", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("passed", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("success", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("successful", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("succeeded", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("complete", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("ok", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeNetworkAssessStatusForDex(object? value)
        {
            string rawValue = GetRawNetworkAssessStatus(value);

            return IsNetworkAssessSuccess(rawValue) ? "pass" : "fail";
        }

        private static string GetRawNetworkAssessStatus(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is JValue jValue)
            {
                return jValue.Value?.ToString() ?? string.Empty;
            }

            if (value is JToken jToken)
            {
                return jToken.Type == JTokenType.Null
                    ? string.Empty
                    : jToken.ToString();
            }

            return value.ToString() ?? string.Empty;
        }

        private void UpdateDeviceIdentifierInResponse(JObject responseData, string? oldIdentifier, string? newIdentifier, string? deviceName)
        {
            if (responseData == null ||
                string.IsNullOrWhiteSpace(oldIdentifier) ||
                string.IsNullOrWhiteSpace(newIdentifier))
            {
                return;
            }

            JToken? devicesToken = responseData["devices"];

            if (devicesToken == null || devicesToken.Type != JTokenType.Array)
            {
                return;
            }

            foreach (JObject device in devicesToken.Children<JObject>())
            {
                string currentIdentifier =
                    device["identifier"]?.ToString() ?? string.Empty;

                string currentDeviceName =
                    device["name"]?.ToString() ?? string.Empty;

                bool identifierMatched =
                    string.Equals(
                        currentIdentifier,
                        oldIdentifier,
                        StringComparison.OrdinalIgnoreCase
                    );

                bool deviceNameMatched =
                    string.IsNullOrWhiteSpace(deviceName) ||
                    string.Equals(
                        currentDeviceName,
                        deviceName,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (identifierMatched && deviceNameMatched)
                {
                    device["identifier"] = ToActionResponseJToken(newIdentifier);
                    return;
                }
            }
        }


        private async Task<JObject?> GetLeadDetailsDbFallbackData(JToken? responseData, string? requestOrderNumber = null)
        {
            try
            {
                string order_number =
                    responseData?["order_number"]?.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(order_number))
                {
                    order_number = requestOrderNumber ?? string.Empty;
                }

                Log.ForContext("LogTag", "ApiRequest")
                    .Information(
                        "GetLeadDetailsDbFallbackData Request: {@Request}",
                        new
                        {
                            order_number = order_number
                        });

                if (string.IsNullOrWhiteSpace(order_number))
                {
                    return null;
                }

                using (DataTable dtFallback =
                    await _dataManager.GetDPELeadDetailsDbFallbackData(order_number))
                {
                    JObject dbFallbackData = new JObject();

                    if (dtFallback != null && dtFallback.Rows.Count > 0)
                    {
                        DataRow row = dtFallback.Rows[0];

                        foreach (DataColumn column in dtFallback.Columns)
                        {
                            object value = row[column];

                            if (value == null || value == DBNull.Value)
                                continue;

                            string propertyName = column.ColumnName;

                            dbFallbackData[propertyName] = JToken.FromObject(value);
                        }
                    }

                    Log.ForContext("LogTag", "ApiRequest")
                        .Information(
                            "GetLeadDetailsDbFallbackData Response: {@Response}",
                            dbFallbackData);

                    return dbFallbackData;
                }
            }
            catch (Exception ex)
            {
                /*
                    Fallback failure should not break Lead Details response.
                    If DB fallback fails, Dex response will continue as usual.
                */
                Log.ForContext("LogTag", "ApiRequest")
                    .Error(
                        ex,
                        "GetLeadDetailsDbFallbackData Exception");

                return null;
            }
        }

        private static void ApplyDbFallbackForMissingDexValues(JToken responseData,JObject dbFallbackData)
        {
            if (responseData == null || dbFallbackData == null)
                return;

            if (responseData is not JObject responseObj)
                return;

            foreach (JProperty dbProperty in dbFallbackData.Properties())
            {
                if (!HasValidDbValue(dbProperty.Value))
                    continue;

                string propertyName = dbProperty.Name;

                JProperty? existingProperty = responseObj.Properties()
                    .FirstOrDefault(p =>
                        string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

                if (existingProperty != null)
                {
                    if (IsDexValueMissing(existingProperty.Value))
                    {
                        existingProperty.Value = dbProperty.Value.DeepClone();
                    }
                }
                else
                {
                    /*
                        If Dex response does not contain the property at all,
                        add it in root responseData.

                        This helps app response and page mapping use DB value.
                    */
                    responseObj[propertyName] = dbProperty.Value.DeepClone();
                }
            }
        }

        private static bool IsDexValueMissing(JToken? token)
        {
            if (token == null)
                return true;

            if (token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return true;

            if (token.Type == JTokenType.String &&
                string.IsNullOrWhiteSpace(token.ToString()))
                return true;

            return false;
        }

        private static bool HasValidDbValue(JToken? token)
        {
            if (token == null)
                return false;

            if (token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return false;

            if (token.Type == JTokenType.String &&
                string.IsNullOrWhiteSpace(token.ToString()))
                return false;

            return true;
        }
        private async Task<JObject?> GetLeadDetails(string? orderNumber,string? retailerCode, Action<object>? captureDexRequestLog = null,Action<string>? captureDexRawResponseLog = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderNumber))
                {
                    return null;
                }

                string endpoint = SettingsValues.GetDPELeadDetailsEndPoint();

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    return null;
                }

                string finalEndPoint = $"{endpoint}/{orderNumber}";

                var responseObj = await _apiCall.HTTPGetRequestHomeWifi(
                    finalEndPoint,
                    retailerCode ?? string.Empty,
                    "INTERNAL_GetLeadDetails",
                    null,
                    captureDexRequestLog,
                    captureDexRawResponseLog
                );

                if (responseObj == null)
                {
                    return null;
                }

                string json = JsonConvert.SerializeObject(responseObj);

                return JsonConvert.DeserializeObject<JObject>(json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetLeadDetails internal helper failed");

                return null;
            }
        }

        private HomeWifiDEPOrderRequestModel BuildUpsertModelFromLatestDexOrAppModel(HomeWifiDEPOrderRequestModel appModel,JToken? latestData)
        {
            HomeWifiDEPOrderRequestModel? upsertModel = null;

            if (latestData != null &&
                latestData.Type != JTokenType.Null &&
                latestData.Type != JTokenType.Undefined)
            {
                upsertModel = BuildUpsertModelFromMappedData(
                    (JObject)latestData,
                            appModel.retailer_code
                );
            }

            if (upsertModel == null)
            {
                upsertModel = appModel;
            }

            /*
                Preserve app-driven action values.
                This keeps your existing workflow intact.
            */
            upsertModel.retailer_code = appModel.retailer_code;
            upsertModel.order_number = appModel.order_number;

            if (appModel.is_canceled == 1)
            {
                upsertModel.is_canceled = appModel.is_canceled;
                upsertModel.cancelation_reason = appModel.cancelation_reason;
                upsertModel.remarks = appModel.remarks;
            }

            return upsertModel;
        }


        private static string? GetLeadDetailsStringValue(object responseData, params string[] fieldNames)
        {
            if (responseData == null)
                return null;

            JToken dataToken;

            if (responseData is JToken existingToken)
            {
                dataToken = existingToken;
            }
            else
            {
                dataToken = JToken.FromObject(responseData);
            }

            /*
                Expected structure:

                data: {
                    lead_details: {
                        payment_type: "ONLINE",
                        payment_status: "UNPAID"
                    },
                    pages: [],
                    required_pages: []
                }

                But this helper also supports direct data and case-insensitive search.
            */

            JToken? leadDetailsToken =
                GetChildTokenIgnoreCase(dataToken, "lead_details")
                ?? GetChildTokenIgnoreCase(dataToken, "leadDetails")
                ?? GetChildTokenIgnoreCase(dataToken, "LeadDetails")
                ?? dataToken;

            foreach (var fieldName in fieldNames)
            {
                var directToken = GetChildTokenIgnoreCase(leadDetailsToken, fieldName);

                if (directToken != null &&
                    directToken.Type != JTokenType.Null &&
                    directToken.Type != JTokenType.Undefined)
                {
                    return directToken.ToString()?.Trim();
                }
            }

            foreach (var fieldName in fieldNames)
            {
                var recursiveToken = FindTokenIgnoreCase(leadDetailsToken, fieldName);

                if (recursiveToken != null &&
                    recursiveToken.Type != JTokenType.Null &&
                    recursiveToken.Type != JTokenType.Undefined)
                {
                    return recursiveToken.ToString()?.Trim();
                }
            }

            return null;
        }

        private static JToken? GetChildTokenIgnoreCase(JToken token, string propertyName)
        {
            if (token == null || token.Type != JTokenType.Object)
                return null;

            var obj = (JObject)token;

            var property = obj.Properties()
                .FirstOrDefault(x => string.Equals(
                    x.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase
                ));

            return property?.Value;
        }

        private static JToken? FindTokenIgnoreCase(JToken token, string propertyName)
        {
            if (token == null)
                return null;

            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;

                foreach (var property in obj.Properties())
                {
                    if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        return property.Value;
                    }
                }

                foreach (var property in obj.Properties())
                {
                    var found = FindTokenIgnoreCase(property.Value, propertyName);

                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            if (token.Type == JTokenType.Array)
            {
                foreach (var item in token.Children())
                {
                    var found = FindTokenIgnoreCase(item, propertyName);

                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        private static bool IsSamePaymentValue(string? actualValue, string expectedValue)
        {
            return string.Equals(
                NormalizePaymentValue(actualValue),
                NormalizePaymentValue(expectedValue),
                StringComparison.OrdinalIgnoreCase
            );
        }

        private static string NormalizePaymentValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .ToUpperInvariant();
        }

        #endregion


        #region mock response for test
        private JObject GetMockDexPortalLeadListResponse()
        {
            return new JObject
            {
                ["status"] = "SUCCESS",
                ["status_code"] = 200,
                ["message"] = "Rider order summary fetched successfully.",
                ["payload"] = new JObject
                {
                    ["data"] = new JArray
            {
                new JObject
                {
                    ["order_number"] = "W2603091234",
                    ["mobile"] = "8801711111111",
                    ["alternate_mobile"] = "8801812345678",
                    ["customer_name"] = "John Doe",
                    ["email"] = "john@example.com",
                    ["delivery_address"] = "House 10, Road 2, Banani",
                    ["district"] = "Dhaka",
                    ["area"] = "Banani",
                    ["payment_type"] = "COD",
                    ["total_amount"] = 1200,
                    ["payment_status"] = "paid",
                    ["order_date"] = "2026-03-09 10:25",
                    ["order_assigned_at"] = "2026-03-10 11:23",
                    ["nw_assess_id"] = "334",
                    ["nw_assess_status"] = "pass",
                    ["appointment_date"] = "2026-03-11",
                    ["order_type"] = "FWA_ACTIVATION",
                    ["order_status"] = "ORDERED",
                    ["initiator_channel"] = "HOME_WIFI",
                    ["subscription_type"] = "prepaid",
                    ["simkit_type"] = "physical"
                },

                new JObject
                {
                    ["order_number"] = "D2603091235",
                    ["mobile"] = "8801722222222",
                    ["alternate_mobile"] = "8801822222222",
                    ["customer_name"] = "Alice Smith",
                    ["email"] = "alice@example.com",
                    ["delivery_address"] = "House 22, Road 5, Gulshan",
                    ["district"] = "Dhaka",
                    ["area"] = "Gulshan",
                    ["payment_type"] = "ONLINE",
                    ["total_amount"] = 1200,
                    ["payment_status"] = "paid",
                    ["order_date"] = "2026-03-10 09:15",
                    ["order_assigned_at"] = "2026-03-10 10:00",
                    ["nw_assess_id"] = null,
                    ["nw_assess_status"] = null,
                    ["appointment_date"] = "2026-03-12",
                    ["order_type"] = "SIM_REPLACEMENT",
                    ["order_status"] = "CONFIRMED",
                    ["initiator_channel"] = "BL-D2D",
                    ["subscription_type"] = "postpaid",
                    ["simkit_type"] = "esim"
                },

                new JObject
                {
                    ["order_number"] = "D2603091236",
                    ["mobile"] = "8801733333333",
                    ["alternate_mobile"] = "8801833333333",
                    ["customer_name"] = "Michael Johnson",
                    ["email"] = "michael@example.com",
                    ["delivery_address"] = "Flat 3B, Dhanmondi",
                    ["district"] = "Dhaka",
                    ["area"] = "Dhanmondi",
                    ["payment_type"] = "COD",
                    ["total_amount"] = 1000,
                    ["payment_status"] = "unpaid",
                    ["order_date"] = "2026-03-11 11:30",
                    ["order_assigned_at"] = "2026-03-11 12:00",
                    ["nw_assess_id"] = null,
                    ["nw_assess_status"] = null,
                    ["appointment_date"] = "2026-03-13",
                    ["order_type"] = "SIM_TOS",
                    ["order_status"] = "PROCESSING",
                    ["initiator_channel"] = "BL-D2D",
                    ["subscription_type"] = "prepaid",
                    ["simkit_type"] = "esim"
                },

                new JObject
                {
                    ["order_number"] = "D2603091237",
                    ["mobile"] = "8801744444444",
                    ["alternate_mobile"] = "8801844444444",
                    ["customer_name"] = "Sarah Ahmed",
                    ["email"] = "sarah@example.com",
                    ["delivery_address"] = "Road 12, Mirpur DOHS",
                    ["district"] = "Dhaka",
                    ["area"] = "Mirpur",
                    ["payment_type"] = "COD",
                    ["total_amount"] = 250,
                    ["payment_status"] = "unpaid",
                    ["order_date"] = "2026-03-12 14:05",
                    ["order_assigned_at"] = "2026-03-12 15:20",
                    ["nw_assess_id"] = null,
                    ["nw_assess_status"] = null,
                    ["appointment_date"] = "2026-03-14",
                    ["order_type"] = "SIM_MNP_PORT_IN",
                    ["order_status"] = "DISPATCHED",
                    ["initiator_channel"] = "BL-D2D",
                    ["subscription_type"] = "postpaid",
                    ["simkit_type"] = "physical"
                },

                new JObject
                {
                    ["order_number"] = "T2603091238",
                    ["mobile"] = "8801755555555",
                    ["alternate_mobile"] = "8801855555555",
                    ["customer_name"] = "Tanvir Rahman",
                    ["email"] = "tanvir@example.com",
                    ["delivery_address"] = "House 5, Road 8, Uttara",
                    ["district"] = "Dhaka",
                    ["area"] = "Uttara",
                    ["payment_type"] = "ONLINE",
                    ["total_amount"] = 3500,
                    ["payment_status"] = "paid",
                    ["order_date"] = "2026-03-13 16:45",
                    ["order_assigned_at"] = "2026-03-13 17:10",
                    ["nw_assess_id"] = null,
                    ["nw_assess_status"] = null,
                    ["appointment_date"] = "2026-03-15",
                    ["order_type"] = "TECHVAN_ACTIVATION",
                    ["order_status"] = "ORDERED",
                    ["initiator_channel"] = "TECHVAN",
                    ["subscription_type"] = "prepaid",
                    ["simkit_type"] = "physical"
                }
            },
                    ["_metadata"] = new JObject
                    {
                        ["pagination"] = new JObject
                        {
                            ["current_page"] = 1,
                            ["per_page"] = 10,
                            ["total_rows"] = 5,
                            ["total_pages"] = 1
                        }
                    }
                }
            };
        }

        private JObject GetMockDexPortalLeadDetailsResponse(string orderNumber)
        {
            JObject listResponse = GetMockDexPortalLeadListResponse();

            JArray? listData = listResponse["payload"]?["data"] as JArray;

            JObject? baseOrder =
                listData?
                    .Children<JObject>()
                    .FirstOrDefault(x =>
                        string.Equals(
                            x["order_number"]?.ToString(),
                            orderNumber,
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

            if (baseOrder == null)
            {
                return new JObject
                {
                    ["status"] = "FAILED",
                    ["status_code"] = 404,
                    ["message"] = "Order not found.",
                    ["error"] = new JObject
                    {
                        ["code"] = "E404",
                        ["description"] = "No order found for the provided order number."
                    }
                };
            }

            JObject detailsData = (JObject)baseOrder.DeepClone();

            detailsData["items"] = GetMockDexPortalLeadDetailsItems(detailsData);

            return new JObject
            {
                ["status"] = "SUCCESS",
                ["status_code"] = 200,
                ["message"] = "Order details fetched successfully.",
                ["payload"] = new JObject
                {
                    ["data"] = detailsData,
                    ["_metadata"] = JValue.CreateNull()
                }
            };
        }

        private JArray GetMockDexPortalLeadDetailsItems(JObject orderData)
        {
            string orderNumber =
                orderData["order_number"]?.ToString() ?? string.Empty;

            string orderType =
                orderData["order_type"]?.ToString() ?? string.Empty;

            string initiatorChannel =
                orderData["initiator_channel"]?.ToString() ?? string.Empty;

            if (string.Equals(orderType, "FWA_ACTIVATION", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(initiatorChannel, "HOME_WIFI", StringComparison.OrdinalIgnoreCase))
            {
                return new JArray
        {
            BuildMockDeviceItem(
                sku: "TOZED-T30-Plus",
                identifier: "356789123456789",
                name: "Tozed T30 Plus",
                brand: "TOZED",
                model: "Tozed T30 Plus",
                color: "White",
                offerCode: null,
                offerDetails: null
            ),

            BuildMockSimItem(
                sku: "FWA_SIM",
                identifier: "8801452000001",
                name: "FWA SIM",
                subscriptionType: "prepaid",
                simkitType: "physical",
                offerCode: "TK500USSD40D",
                offerDetails: "Starter 20Mbps"
            )
        };
            }

            if (string.Equals(orderType, "SIM_REPLACEMENT", StringComparison.OrdinalIgnoreCase))
            {
                return new JArray
        {
            BuildMockSimItem(
                sku: "SIM_REPLACEMENT_ESIM",
                identifier: "8801452000002",
                name: "Replacement eSIM",
                subscriptionType: "postpaid",
                simkitType: "esim",
                offerCode: "POSTPAID_REPL_ESIM",
                offerDetails: "Postpaid Replacement eSIM"
            )
        };
            }

            if (string.Equals(orderType, "SIM_TOS", StringComparison.OrdinalIgnoreCase))
            {
                return new JArray
        {
            BuildMockSimItem(
                sku: "SIM_TOS_ESIM",
                identifier: "8801452000003",
                name: "TOS eSIM",
                subscriptionType: "prepaid",
                simkitType: "esim",
                offerCode: "PREPAID_TOS_ESIM",
                offerDetails: "Prepaid Transfer of Ownership eSIM"
            )
        };
            }

            if (string.Equals(orderType, "SIM_MNP_PORT_IN", StringComparison.OrdinalIgnoreCase))
            {
                return new JArray
        {
            BuildMockSimItem(
                sku: "MNP_PHYSICAL_SIM",
                identifier: "8801452000004",
                name: "MNP Physical SIM",
                subscriptionType: "postpaid",
                simkitType: "physical",
                offerCode: "POSTPAID_MNP_PHYSICAL",
                offerDetails: "Postpaid MNP Physical SIM"
            )
        };
            }

            if (string.Equals(orderType, "TECHVAN_ACTIVATION", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(initiatorChannel, "TECHVAN", StringComparison.OrdinalIgnoreCase))
            {
                return new JArray
        {
            BuildMockDeviceItem(
                sku: "TECNO-SPARK-40",
                identifier: "356789123456790",
                name: "TECNO Spark 40",
                brand: "TECNO",
                model: "Spark 40",
                color: "Black",
                offerCode: null,
                offerDetails: null
            ),

            BuildMockSimItem(
                sku: "TECHVAN_SIM",
                identifier: "8801452000005",
                name: "TechVan SIM",
                subscriptionType: "prepaid",
                simkitType: "physical",
                offerCode: "TK500USSD30D15GB",
                offerDetails: "15GB+1000Min+200SMS-30Days"
            ),

            BuildMockDeviceItem(
                sku: "jbl-headset-001",
                identifier: "063519920150912AE",
                name: "JBL Boombox 3",
                brand: "JBL",
                model: "JBL Boombox 3",
                color: "Black",
                offerCode: null,
                offerDetails: null
            )
        };
            }

            return new JArray();
        }

        private JObject BuildMockDeviceItem(string sku, string identifier, string name, string brand, string model, string color, string? offerCode, string? offerDetails)
        {
            return new JObject
            {
                ["type"] = "DEVICE",
                ["sku"] = sku,
                ["identifier"] = identifier,
                ["name"] = name,
                ["attributes"] = new JObject
                {
                    ["brand"] = brand,
                    ["model"] = model,
                    ["color"] = color
                },
                ["offer"] = BuildMockOffer(offerCode, offerDetails)
            };
        }

        private JObject BuildMockSimItem(string sku, string identifier, string name, string subscriptionType, string simkitType, string? offerCode, string? offerDetails)
        {
            return new JObject
            {
                ["type"] = "SIM",
                ["sku"] = sku,
                ["identifier"] = identifier,
                ["name"] = name,
                ["attributes"] = new JObject
                {
                    ["subscription_type"] = subscriptionType,
                    ["simkit_type"] = simkitType
                },
                ["offer"] = BuildMockOffer(offerCode, offerDetails)
            };
        }

        private JObject GetMockDexPortalCancelOrderResponse(HomeWifiDEPOrderRequestModel model)
        {
            return new JObject
            {
                ["status"] = "SUCCESS",
                ["status_code"] = 200,
                ["message"] = "Order status updated successfully.",
                ["payload"] = new JObject
                {
                    ["data"] = new JObject
                    {
                        ["order_number"] = ToActionResponseJToken(model.order_number),
                        ["mobile"] = ToActionResponseJToken(model.mobile),
                        ["alternate_mobile"] = ToActionResponseJToken(model.alternate_mobile),
                        ["customer_name"] = ToActionResponseJToken(model.customer_name),
                        ["email"] = ToActionResponseJToken(model.email),

                        ["delivery_address"] = ToActionResponseJToken(model.delivery_address),
                        ["district"] = ToActionResponseJToken(model.district),
                        ["area"] = ToActionResponseJToken(model.area),

                        ["payment_type"] = ToActionResponseJToken(model.payment_type),
                        ["total_amount"] = ToActionResponseJToken(model.total_amount),
                        ["payment_status"] = ToActionResponseJToken(model.payment_status),

                        ["order_date"] = ToActionResponseJToken(model.order_date),
                        ["order_assigned_at"] = ToActionResponseJToken(model.order_assigned_at),
                        ["nw_assess_id"] = ToActionResponseJToken(model.nw_assess_id),
                        ["nw_assess_status"] = ToActionResponseJToken(model.nw_assess_status),
                        ["appointment_date"] = ToActionResponseJToken(model.appointment_date),

                        ["order_type"] = ToActionResponseJToken(model.order_type),
                        ["order_status"] = "CANCELED",
                        ["initiator_channel"] = ToActionResponseJToken(model.initiator_channel),

                        ["items"] = BuildMockDexPortalCancelOrderItems(model)
                    },
                    ["_metadata"] = JValue.CreateNull()
                }
            };
        }

        private JArray BuildMockDexPortalCancelOrderItems(HomeWifiDEPOrderRequestModel model)
        {
            var items = new JArray();

            if (model.devices != null && model.devices.Count > 0)
            {
                foreach (var device in model.devices)
                {
                    items.Add(new JObject
                    {
                        ["type"] = "DEVICE",
                        ["sku"] = ToActionResponseJToken(device.sku),
                        ["identifier"] = ToActionResponseJToken(device.identifier),
                        ["name"] = ToActionResponseJToken(device.name),
                        ["attributes"] = new JObject
                        {
                            ["brand"] = ToActionResponseJToken(device.brand),
                            ["model"] = ToActionResponseJToken(device.model),
                            ["color"] = ToActionResponseJToken(device.color)
                        },
                        ["offer"] = BuildMockOffer(
                            device.offer_code,
                            device.offer_name
                        )
                    });
                }
            }

            items.Add(new JObject
            {
                ["type"] = "SIM",
                ["sku"] = GetSimSku(model),
                ["identifier"] = ToActionResponseJToken(model.ordered_msisdn),
                ["name"] = GetSimName(model),
                ["attributes"] = new JObject
                {
                    ["subscription_type"] = ToActionResponseJToken(model.subscription_type),
                    ["simkit_type"] = ToActionResponseJToken(model.simkit_type)
                },
                ["offer"] = BuildMockOffer(
                    model.offer_code,
                    model.offer_name
                )
            });

            return items;
        }

        private JObject GetMockDexPortalNetworkAssessmentResponse(HomeWifiNetworkAssessmentRequestModel model)
        {
            JObject mockLeadDetails =
                GetMockDexPortalLeadDetailsResponse(model.order_number);

            string nwAssessStatusText =
                NormalizeNetworkAssessStatusForDex(model.nw_assess_status);

            /*
                For mock testing:
                If order does not exist in mock lead list, still return success
                so the network assessment flow can be tested independently.
            */
            if (!string.Equals(
                    mockLeadDetails["status"]?.ToString(),
                    "SUCCESS",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new JObject
                {
                    ["status"] = "SUCCESS",
                    ["status_code"] = 200,
                    ["message"] = "Network assessment updated successfully.",
                    ["payload"] = new JObject
                    {
                        ["data"] = new JObject
                        {
                            ["order_number"] = ToActionResponseJToken(model.order_number),

                            ["mobile"] = JValue.CreateNull(),
                            ["alternate_mobile"] = JValue.CreateNull(),
                            ["customer_name"] = JValue.CreateNull(),
                            ["email"] = JValue.CreateNull(),

                            ["delivery_address"] = JValue.CreateNull(),
                            ["district"] = JValue.CreateNull(),
                            ["area"] = JValue.CreateNull(),

                            ["payment_type"] = "COD",
                            ["total_amount"] = JValue.CreateNull(),
                            ["payment_status"] = JValue.CreateNull(),

                            ["order_date"] = JValue.CreateNull(),
                            ["order_assigned_at"] = JValue.CreateNull(),
                            ["appointment_date"] = JValue.CreateNull(),

                            ["nw_assess_id"] = ToActionResponseJToken(model.nw_assess_id),
                            ["nw_assess_status"] = nwAssessStatusText,

                            ["order_type"] = "FWA_ACTIVATION",
                            ["order_status"] = "ORDERED",
                            ["initiator_channel"] = "FWA",

                            ["subscription_type"] = "prepaid",
                            ["simkit_type"] = "physical",

                            ["items"] = new JArray()
                        },
                        ["_metadata"] = JValue.CreateNull()
                    }
                };
            }

            JObject? data = mockLeadDetails["payload"]?["data"] as JObject;

            if (data == null)
            {
                data = new JObject();

                mockLeadDetails["payload"] = new JObject
                {
                    ["data"] = data,
                    ["_metadata"] = JValue.CreateNull()
                };
            }

            data["nw_assess_id"] = ToActionResponseJToken(model.nw_assess_id);
            data["nw_assess_status"] = nwAssessStatusText;

            mockLeadDetails["message"] = "Network assessment updated successfully.";

            return mockLeadDetails;
        }

        private JObject GetMockDexPortalPayslipUploadResponse(HomeWifiPayslipUploadRequestModel model)
        {
            JObject mockLeadDetails = GetMockDexPortalLeadDetailsResponse(model.order_number);

            if (!string.Equals(
                    mockLeadDetails["status"]?.ToString(),
                    "SUCCESS",
                    StringComparison.OrdinalIgnoreCase))
            {
                return mockLeadDetails;
            }

            JObject? data = mockLeadDetails["payload"]?["data"] as JObject;

            if (data == null)
            {
                data = new JObject();

                mockLeadDetails["payload"] = new JObject
                {
                    ["data"] = data,
                    ["_metadata"] = null
                };
            }

            data["is_payslip_uploaded"] = 1;

            if (string.IsNullOrWhiteSpace(data["order_status"]?.ToString()))
            {
                data["order_status"] = "PENDING_FOR_REVIEW";
            }

            mockLeadDetails["message"] = "Payslip uploaded successfully.";

            return mockLeadDetails;
        }

        private JObject GetMockDexPortalPaymentMethodChangeResponse(HomeWifiPaymentMethodChangeRequestModel model)
        {
            return new JObject
            {
                ["status"] = "SUCCESS",
                ["status_code"] = 200,
                ["message"] = "Payment link sent successfully.",
                ["payload"] = new JObject
                {
                    ["data"] = new JObject
                    {
                        ["order_number"] = model.order_number
                    },
                    ["_metadata"] = null
                }
            };
        }

        private JObject GetMockDexPortalIMEIUpdateResponse(HomeWifiIMEIUpdateRequestModel model, string orderedMsisdn)
        {
            JObject mockLeadDetails =
                GetMockDexPortalLeadDetailsResponse(model.order_number);

            if (!string.Equals(
                    mockLeadDetails["status"]?.ToString(),
                    "SUCCESS",
                    StringComparison.OrdinalIgnoreCase))
            {
                return mockLeadDetails;
            }

            JObject? data = mockLeadDetails["payload"]?["data"] as JObject;

            if (data == null)
            {
                data = new JObject();

                mockLeadDetails["payload"] = new JObject
                {
                    ["data"] = data,
                    ["_metadata"] = JValue.CreateNull()
                };
            }

            data.Remove("device_imei");
            data["is_imei_updated"] = 1;

            JToken? itemsToken = data["items"];

            bool deviceUpdated = false;

            if (itemsToken != null && itemsToken.Type == JTokenType.Array)
            {
                foreach (JObject item in itemsToken.Children<JObject>())
                {
                    string itemType =
                        item["type"]?.ToString() ?? string.Empty;

                    if (!deviceUpdated &&
                        string.Equals(itemType, "DEVICE", StringComparison.OrdinalIgnoreCase))
                    {
                        string currentIdentifier =
                            item["identifier"]?.ToString() ?? string.Empty;

                        string currentDeviceName =
                            item["name"]?.ToString() ?? string.Empty;

                        bool identifierMatched =
                            string.Equals(
                                currentIdentifier,
                                model.old_identifier,
                                StringComparison.OrdinalIgnoreCase
                            );

                        bool deviceNameMatched =
                            string.IsNullOrWhiteSpace(model.device_name) ||
                            string.Equals(
                                currentDeviceName,
                                model.device_name,
                                StringComparison.OrdinalIgnoreCase
                            );

                        if (identifierMatched && deviceNameMatched)
                        {
                            item["identifier"] = ToActionResponseJToken(model.new_identifier);
                            deviceUpdated = true;
                        }
                    }

                    if (string.Equals(itemType, "SIM", StringComparison.OrdinalIgnoreCase))
                    {
                        item["identifier"] = ToActionResponseJToken(orderedMsisdn);
                    }
                }
            }

            data["ordered_msisdn"] = ToActionResponseJToken(orderedMsisdn);

            mockLeadDetails["message"] = "IMEI updated successfully.";

            return mockLeadDetails;
        }

        private JToken BuildMockOffer(string? offerCode, string? offerName)
        {
            if (string.IsNullOrWhiteSpace(offerCode) &&
                string.IsNullOrWhiteSpace(offerName))
            {
                return JValue.CreateNull();
            }

            return new JObject
            {
                ["offer_code"] = ToActionResponseJToken(offerCode),
                ["offer_details"] = ToActionResponseJToken(offerName)
            };
        }

        private string GetSimSku(HomeWifiDEPOrderRequestModel model)
        {
            string channel = model.initiator_channel ?? string.Empty;
            string orderType = model.order_type ?? string.Empty;

            if (channel.Equals("FWA", StringComparison.OrdinalIgnoreCase) ||
                channel.Equals("HOME_WIFI", StringComparison.OrdinalIgnoreCase))
            {
                return "FWA_SIM";
            }

            if (channel.Equals("TECHVAN", StringComparison.OrdinalIgnoreCase))
            {
                return "TECHVAN_SIM";
            }

            if (channel.Equals("BL-D2D", StringComparison.OrdinalIgnoreCase) ||
                channel.Equals("D2D", StringComparison.OrdinalIgnoreCase))
            {
                if (orderType.Equals("SIM_REPLACEMENT", StringComparison.OrdinalIgnoreCase))
                {
                    return "SIM_REPLACEMENT";
                }

                if (orderType.Equals("SIM_MNP_PORT_IN", StringComparison.OrdinalIgnoreCase))
                {
                    return "MNP_SIM";
                }

                if (orderType.Equals("SIM_TOS", StringComparison.OrdinalIgnoreCase))
                {
                    return "TOS_SIM";
                }

                return "BL_D2D_SIM";
            }

            return "SIM";
        }

        private string GetSimName(HomeWifiDEPOrderRequestModel model)
        {
            string channel = model.initiator_channel ?? string.Empty;

            if (channel.Equals("FWA", StringComparison.OrdinalIgnoreCase) ||
                channel.Equals("HOME_WIFI", StringComparison.OrdinalIgnoreCase))
            {
                return "FWA SIM";
            }

            if (channel.Equals("TECHVAN", StringComparison.OrdinalIgnoreCase))
            {
                return "TechVan SIM";
            }

            return "BL SIM";
        }


        #endregion
    }


}
