using BIA.BLL.BLLServices;
using BIA.Entity.Collections;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.Interfaces;
using BIA.Entity.ResponseEntity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.BLL.Utility
{
    public class OrderApiCall
    {
        private readonly BllBiometricBssService errorUpdate;
        private readonly BllOrderBssService _bllOrderBssService;
        private readonly BllHandleException _manageExecption;
        private readonly BLLLog _bllLog;
        private readonly ApiCall genericApiCall;

        public OrderApiCall(BllBiometricBssService _errorUpdate, BllOrderBssService bllOrderBssService, BllHandleException manageExecption, BLLLog bllLog, ApiCall genericApiCall2)
        {
            errorUpdate = _errorUpdate;
            _bllOrderBssService = bllOrderBssService;
            _manageExecption = manageExecption;
            _bllLog = bllLog;
            genericApiCall = genericApiCall2;
        }
        public async Task PatchOrderRequestToBss(OrderDataModel item, object reqModel, string meathodUrl)
        {
            BSS_Json byteArrayConverter = new BSS_Json();
            LogModel log = new LogModel();
            object orderResponse = new object();
            log.status = item.status;
            DateTime reqTime = DateTime.Now;
            DateTime resTime = DateTime.Now;
            try
            {
                log.req_time = DateTime.Now;
                log.req_string = JsonConvert.SerializeObject(reqModel);
                log.req_blob = byteArrayConverter.GetGenericJsonData(reqModel);

                try
                { orderResponse = await genericApiCall.HttpPatchRequest(reqModel, meathodUrl, "PatchOrderRequestToBss"); }
                catch (Exception)
                { throw; }
                //log.res_time = DateTime.Now;
                log.req_time = reqTime;
                log.res_time = resTime;
                log.res_string = JsonConvert.SerializeObject(orderResponse);
                log.res_blob = byteArrayConverter.GetGenericJsonData(orderResponse);

                OrderResModelPatch? response = new OrderResModelPatch();
                try
                {
                    if (orderResponse.ToString() != null)
                    {
                        response = JsonConvert.DeserializeObject<OrderResModelPatch>(orderResponse.ToString() ?? "");
                    }
                }
                catch (Exception)
                { throw; }

                if (response == null)
                    throw new Exception("DBSS: Order Api Response is not valid.");
                else if (response.data == null)
                    throw new Exception("DBSS: Order Api Response is not valid.");
                else if (response.data.FirstOrDefault()?.id == null)
                    throw new Exception("DBSS: Order Api Response is not valid.");

                await _bllOrderBssService.UpdateBioDbForOrderReq(item.bi_token_number, response.data.FirstOrDefault()?.id ?? "");
                log.is_success = 1;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                ErrorDescription error = await _manageExecption.ManageException(ex, ex.HResult, "BIA"); ;
                //log.res_time = DateTime.Now;
                log.req_time = reqTime;
                log.res_time = resTime;
                log.res_string = JsonConvert.SerializeObject(orderResponse != null ? orderResponse.ToString() : ex.InnerException?.Message).ToString();
                log.res_blob = byteArrayConverter.GetGenericJsonData(orderResponse != null ? orderResponse : ex.InnerException?.Message);
                log.message = error.error_custom_msg ?? error.error_description;
                log.error_code = error.error_code ?? "";
                log.error_source = error.error_source ?? "BIA OM Request";
                log.is_success = 0;
                //BIRequset Table Update with Status 35 and Error id and description for Order failure
                item.status = (int)StatusNo.order_request_fail;
                item.error_id = error.error_id;
                item.error_description = "DBSS: Order-" + ex.InnerException?.Message;
                await errorUpdate.UpdateStatusandErrorMessage(item.bi_token_number, item.status, item.error_id, item.error_description);
            }
            finally
            {
                log.bss_request_id = item.bss_request_id;
                log.bi_token_number = item.bi_token_number;
                log.msisdn = item.msisdn;
                log.user_id = item.user_id;
                log.integration_point_from = (int)IntegrationPoint.bss_service;
                log.integration_point_to = (int)IntegrationPoint.bss;
                log.method_name = "PatchOrderRequestToBss_New";
                log.purpose_number = item.purpose_number.ToString();
                await _bllLog.BALogInsert(log);
            }
        }

        public async Task PostOrderRequestToBss(OrderDataModel item, object reqModel, string meathodUrl)
        {
            BSS_Json byteArrayConverter = new BSS_Json();
            LogModel log = new LogModel();
            object orderResponse = new object();
            log.status = item.status;

            try
            {
                log.req_time = DateTime.Now;
                log.req_string = JsonConvert.SerializeObject(reqModel);
                log.req_blob = byteArrayConverter.GetGenericJsonData(reqModel);
                try
                {
                    DateTime reqTime = DateTime.Now;
                    log.req_time = reqTime;

                    orderResponse = await genericApiCall.HttpPostRequestOrderDBSS(reqModel, meathodUrl, "PostOrderRequestToBss");

                    DateTime resTime = DateTime.Now;
                    log.res_time = resTime;
                }
                catch (Exception)
                { throw; }

                log.res_string = JsonConvert.SerializeObject(orderResponse);
                log.res_blob = byteArrayConverter.GetGenericJsonData(orderResponse);

                string confirmationCode = string.Empty;
                try
                {
                    JObject dbssRespObj = JObject.Parse(log.res_string);

                    if (dbssRespObj != null && dbssRespObj.ContainsKey("data"))
                    {
                        if (dbssRespObj["data"] != null)
                        {
                            confirmationCode = dbssRespObj["data"]?["attributes"]?["confirmation-code"]?.ToString() ?? "";
                        }
                    }
                }
                catch (Exception)
                { throw; }


                if (confirmationCode == null)
                    throw new Exception("DBSS: Order Api Response is not valid.");

                await _bllOrderBssService.UpdateBioDbForOrderReq(item.bi_token_number, confirmationCode);
                log.is_success = 1;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");

                ErrorDescription error = await _manageExecption.ManageException(ex, ex.HResult, "BIA");

                //log.res_time = DateTime.Now;
                log.res_time = DateTime.Now; ;
                log.res_string = JsonConvert.SerializeObject(orderResponse != null ? orderResponse.ToString() : ex.Message).ToString();
                log.res_blob = byteArrayConverter.GetGenericJsonData(error);
                log.message = error.error_custom_msg ?? error.error_description;
                log.error_code = error.error_code ?? "";
                log.error_source = error.error_source ?? "BIA";
                log.is_success = 0;
                //BIRequset Table Update with Status 35 and Error id and description for Order failure
                item.status = (int)StatusNo.order_request_fail;
                item.error_id = error.error_id;
                item.error_description = string.IsNullOrEmpty(error.error_custom_msg) ? "DBSS: Order-" + error.error_description : "DBSS: Order-" + error.error_custom_msg;
                await errorUpdate.UpdateStatusandErrorMessage(item.bi_token_number, item.status, item.error_id, item.error_description);
            }
            finally
            {
                log.bss_request_id = item.bss_request_id;
                log.bi_token_number = item.bi_token_number;
                log.msisdn = item.msisdn;
                log.user_id = item.user_id;
                log.integration_point_from = (int)IntegrationPoint.bss_service;
                log.integration_point_to = (int)IntegrationPoint.bss;
                log.method_name = "PostOrderRequestToBss_New";
                log.purpose_number = item.purpose_number.ToString();
                await _bllLog.BALogInsert(log);
            }
        }

        public async Task<string> PostCreatCustomerRequestToBss(OrderDataModel item, object reqModel, int createCustomerMaxRetry)
        {
            string ownerCustomerId = "";
            BSS_Json byteArrayConverter = new BSS_Json();

            for (int i = 0; i < createCustomerMaxRetry; i++)
            {
                LogModel log = new LogModel();
                object orderResponse = null;

                DateTime reqTime = DateTime.Now;
                DateTime resTime = DateTime.Now;

                try
                {
                    log.status = item.status;
                    log.req_time = DateTime.Now;
                    log.req_string = JsonConvert.SerializeObject(reqModel);
                    log.req_blob = byteArrayConverter.GetGenericJsonData(reqModel);

                    // ---------------- CALL EXTERNAL API ----------------
                    orderResponse = await genericApiCall.HttpPostRequest(
                        reqModel, "/api/v1/customers", "PostCreatCustomerRequestToBss");

                    log.res_time = DateTime.Now;

                    // ---------------- HANDLE SUCCESS RESPONSE ----------------
                    if (orderResponse != null)
                    {
                        string jsonStr = orderResponse.ToString();

                        if (!string.IsNullOrWhiteSpace(jsonStr) && IsValidJson(jsonStr))
                        {
                            JObject dbssRespObj = JObject.Parse(jsonStr);

                            if (dbssRespObj.ContainsKey("data"))
                                ownerCustomerId = dbssRespObj["data"]?["id"]?.ToString() ?? "";
                        }

                        log.res_string = jsonStr;

                        // Safe blob handling
                        if (IsValidJson(jsonStr))
                            log.res_blob = byteArrayConverter.GetGenericJsonData(orderResponse);
                        else
                            log.res_blob = byteArrayConverter.GetGenericJsonData(new { rawText = jsonStr });
                    }
                    else
                    {
                        throw new Exception("Empty response received from BSS API.");
                    }

                    // ---------------- VALIDATE CUSTOMER ID ----------------
                    if (string.IsNullOrWhiteSpace(ownerCustomerId))
                    {
                        throw new Exception("DBSS: Create Customer API response did not return customer ID.");
                    }

                    // ---------------- UPDATE DB ON SUCCESS ----------------
                    await _bllOrderBssService.UpdateBioDbForCreateCustomerReq(item.bi_token_number, ownerCustomerId);

                    log.is_success = 1;
                    break;  // EXIT RETRY LOOP ON SUCCESS
                }
                catch (Exception ex)
                {
                    // ---------------- HANDLE FAILURE ----------------
                    Log.Error(ex, "ExMessage");

                    log.req_time = reqTime;
                    log.res_time = resTime;

                    ErrorDescription error = await _manageExecption.ManageException(ex, ex.HResult, "BIA");

                    string errText = orderResponse?.ToString() ?? ex.InnerException?.Message ?? ex.Message;

                    log.res_string = errText;

                    // Safe blob handling
                    if (IsValidJson(errText))
                        log.res_blob = byteArrayConverter.GetGenericJsonData(errText);
                    else
                        log.res_blob = byteArrayConverter.GetGenericJsonData(new { rawText = errText });

                    log.message = error.error_custom_msg ?? error.error_description;
                    log.error_code = error.error_code ?? "";
                    log.error_source = error.error_source ?? "BIA";
                    log.is_success = 0;

                    item.status = (int)StatusNo.order_request_fail;
                    item.error_id = error.error_id;

                    try
                    {
                        item.error_description =
                            "DBSS: Order - " +
                            (ex.InnerException?.Message ?? ex.Message)
                            .Substring(0, Math.Min((ex.InnerException?.Message ?? ex.Message).Length, 900));
                    }
                    catch { }

                    // If last attempt, update DB
                    if (i == createCustomerMaxRetry - 1)
                    {
                        await errorUpdate.UpdateStatusandErrorMessage(
                            item.bi_token_number, item.status, item.error_id, item.error_description);
                    }
                }
                finally
                {
                    // ---------------- ALWAYS SAVE LOG ----------------
                    log.bss_request_id = item.bss_request_id;
                    log.bi_token_number = item.bi_token_number;
                    log.msisdn = item.msisdn;
                    log.user_id = item.user_id;
                    log.integration_point_from = (int)IntegrationPoint.bss_service;
                    log.integration_point_to = (int)IntegrationPoint.bss;
                    log.method_name = "PostCreatCustomerRequestToBss_New";
                    log.purpose_number = item.purpose_number.ToString();

                    await _bllLog.BALogInsert(log);
                }
            }

            return ownerCustomerId;
        }


        private bool IsValidJson(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return false;

            str = str.Trim();

            if ((str.StartsWith("{") && str.EndsWith("}")) ||   // JSON object
                (str.StartsWith("[") && str.EndsWith("]")))     // JSON array
            {
                try
                {
                    JToken.Parse(str);
                    return true;
                }
                catch { return false; }
            }

            return false;
        }


        //public async Task<string> PostCreatCustomerRequestToBss(OrderDataModel item, object reqModel, int createCustomerMaxRetry)
        //{
        //    string ownerCustomerId = "";

        //    for (int i = 0; i < createCustomerMaxRetry; i++)
        //    {
        //        BSS_Json byteArrayConverter = new BSS_Json();
        //        LogModel log = new LogModel();
        //        object orderResponse = new object();
        //        log.status = item.status;
        //        DateTime reqTime = DateTime.Now;
        //        DateTime resTime = DateTime.Now;
        //        try
        //        {
        //            log.req_string = JsonConvert.SerializeObject(reqModel);
        //            log.req_blob = byteArrayConverter.GetGenericJsonData(reqModel);

        //            try
        //            {
        //                log.req_time = DateTime.Now;

        //                orderResponse = await genericApiCall.HttpPostRequest(reqModel, "/api/v1/customers", "PostCreatCustomerRequestToBss");

        //                log.res_time = DateTime.Now;

        //                if (orderResponse != null)
        //                {
        //                    string jsonStr = orderResponse.ToString() ?? "";

        //                    if (!string.IsNullOrWhiteSpace(jsonStr))
        //                    {
        //                        JObject dbssRespObj = JObject.Parse(jsonStr);

        //                        if (dbssRespObj.ContainsKey("data"))
        //                            ownerCustomerId = dbssRespObj["data"]?["id"]?.ToString() ?? "";
        //                    }
        //                }

        //                log.res_string = JsonConvert.SerializeObject(orderResponse);
        //                log.res_blob = byteArrayConverter.GetGenericJsonData(orderResponse);
        //            }
        //            catch (JsonReaderException jsonEx)
        //            {
        //                // This handles bad JSON parsing
        //                throw new Exception("Invalid JSON returned from BSS API.", jsonEx);
        //            }
        //            catch (Exception ex)
        //            {
        //                // This handles all other exceptions
        //                throw new Exception("Unexpected error during customer creation.", ex);
        //            }

        //            if (ownerCustomerId == null)
        //            {
        //                throw new Exception("DBSS: Create Customer Api Response is not valid.");
        //            }

        //            await _bllOrderBssService.UpdateBioDbForCreateCustomerReq(item.bi_token_number, ownerCustomerId);
        //            log.is_success = 1;

        //            i = createCustomerMaxRetry;
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error(ex, "ExMessage");
        //            log.req_time = reqTime;
        //            log.res_time = resTime;

        //            ErrorDescription error = await _manageExecption.ManageException(ex, ex.HResult, "BIA");

        //            log.res_string = JsonConvert.SerializeObject(orderResponse != null ? orderResponse.ToString() : ex.InnerException?.Message).ToString();
        //            log.res_blob = byteArrayConverter.GetGenericJsonData(orderResponse != null ? orderResponse : ex.InnerException?.Message);
        //            log.message = error.error_custom_msg ?? error.error_description;
        //            log.error_code = error.error_code ?? "";
        //            log.error_source = error.error_source ?? "BIA";
        //            log.is_success = 0;
        //            item.status = (int)StatusNo.order_request_fail;
        //            item.error_id = error.error_id;

        //            try
        //            {
        //                item.error_description = "DBSS: Order-" + ex.InnerException?.Message.Substring(0, Math.Min(ex.InnerException.Message.Length, 900));
        //            }
        //            catch
        //            {
        //            }

        //            if (i == createCustomerMaxRetry - 1)
        //            {
        //                await errorUpdate.UpdateStatusandErrorMessage(item.bi_token_number, item.status, item.error_id, item.error_description);
        //            }
        //        }
        //        finally
        //        {
        //            log.bss_request_id = item.bss_request_id;
        //            log.bi_token_number = item.bi_token_number;
        //            log.msisdn = item.msisdn;
        //            log.user_id = item.user_id;
        //            log.integration_point_from = (int)IntegrationPoint.bss_service;
        //            log.integration_point_to = (int)IntegrationPoint.bss;
        //            log.method_name = "PostCreatCustomerRequestToBss_New";
        //            log.purpose_number = item.purpose_number.ToString();
        //            await _bllLog.BALogInsert(log);
        //        }
        //    }

        //    return ownerCustomerId;
        //}

    }
}
