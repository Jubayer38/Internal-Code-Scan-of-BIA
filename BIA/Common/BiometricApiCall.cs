using BIA.BLL.BLLServices;
using BIA.BLL.Utility;
using BIA.Controllers;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.PopulateModel;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Entity.Utility;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Text;

namespace BIA.Common
{
    public class BiometricApiCall
    {
        private readonly BL_Json _blJson;
        private readonly BLLLog _bllLog;
        private readonly BLLCommon _bllCommon;
        private readonly BllBiometricBssService _bllObj;
        private readonly BllHandleException _manageExecption;
        private readonly BLLDBSSToRAParse _dbssToRaParse;
        private readonly ApiRequest _apiReq;
        private readonly ApiCall genericApiCall;

        public BiometricApiCall(BL_Json blJson, BLLLog bllLog, BLLCommon bllCommon, BllBiometricBssService bllObj, BllHandleException manageExecption, BLLDBSSToRAParse dbssToRaParse, ApiRequest apiReq, ApiCall genericApiCall2)
        {
            _blJson = blJson;
            _bllLog = bllLog;
            _bllCommon = bllCommon;
            _bllObj = bllObj;
            _manageExecption = manageExecption;
            _dbssToRaParse = dbssToRaParse;
            _apiReq = apiReq;
            genericApiCall = genericApiCall2;
        }

        public static string singleSourceLoginSessionToken = string.Empty;
        public async Task<BioVerifyResp> BioVerificationReqToBss(BiomerticDataModel item, object reqModel, string meathodUrl)
        {
            BIAToDBSSLog log = new BIAToDBSSLog();
            object bioResponse = new object();
            bool bioLogNeeded = false;
            bool isCdtError = false;
            BL_Json byteArrayConverter = new BL_Json();
            BioVerifyResp verifyResp = new BioVerifyResp();
            SingleSourceCheckResponseModel checkResponseModel = new SingleSourceCheckResponseModel();
            MSISDNReservationResponse reservationResponse = new MSISDNReservationResponse();
            DateTime reqTime = DateTime.Now;
            DateTime resTime = DateTime.Now;

            try
            {

                checkResponseModel = await SingleSourceCheckFromBioDB(item.msisdn, item.sim_number, item.purpose_number, item.poc_number, item.sim_replacement_type, item.dest_doc_id, item.dest_dob, item.dest_imsi);

                if (checkResponseModel.Status == 0)
                {
                    verifyResp.is_success = false;
                    verifyResp.err_msg = checkResponseModel.Message;
                    log.message = checkResponseModel.Message;
                    return verifyResp;
                }

                if (string.IsNullOrEmpty(item.poc_number) ||
                    (!string.IsNullOrEmpty(item.poc_number)
                        && item.purpose_number == (int)EnumPurposeNumber.SIMReplacement
                        && (item.sim_replacement_type == (int)EnumSIMReplacementType.ByPOC
                            || item.sim_replacement_type == (int)EnumSIMReplacementType.ByAuthPerson)
                        )
                     )
                {

                    if (item.purpose_number == (int)EnumPurposeNumber.NewRegistration || item.purpose_number == (int)EnumPurposeNumber.MNPRegistration || item.purpose_number == (int)EnumPurposeNumber.MNPEmergencyReturn)
                    {
                        string CDTMessage = await CDT(item);
                        if (!string.IsNullOrEmpty(CDTMessage))
                        {
                            isCdtError = true;
                            verifyResp.err_msg = "DBSS: CDT Operation Fail.";
                            log.message = "DBSS: CDT Operation Fail.";
                            throw new Exception(CDTMessage);
                        }
                    }//######################## CDT Others ###########################
                    else if (item.purpose_number == (int)EnumPurposeNumber.SIMReplacement
                            || item.purpose_number == (int)EnumPurposeNumber.DeRegistration)
                    {
                        var res = GetPropertyValue(reqModel, "data.attributes.msisdn");
                        string? msisdn = res?.ToString();
                        bool isOtherCDTSuccess = await OtherCDT(item);
                        if (!isOtherCDTSuccess)
                        {
                            verifyResp.err_msg = "DBSS: Other CDT Operation Fail.";
                            log.message = "DBSS: Other CDT Operation Fail.";
                            throw new Exception("DBSS: Other CDT Operation Fail.");
                        }
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.SIMTransfer)
                    {
                        RACommonResponse raResp = new RACommonResponse();
                        raResp = await OtherCDTForTOS(item);
                        if (raResp.result == false)
                        {
                            verifyResp.err_msg = raResp.message;
                            log.message = "DBSS: Other CDT Operation Fail.";
                            return verifyResp;
                            //throw new Exception("DBSS: Other CDT Operation for TOS Fail.");
                        }
                    }
                    if (item.is_paired != null)
                    {
                        if (item.is_paired == 0 && item.purpose_number == (int)EnumPurposeNumber.NewRegistration)
                        {
                            try
                            {
                                reservationResponse = await MSISDNReservation(item);
                                if (reservationResponse.IsReserve != true)
                                {
                                    log.message = reservationResponse.Error_message;
                                    verifyResp.err_msg = "DBSS: MSISDN Reservation Fail.";
                                    throw new Exception("DBSS: MSISDN Reservation Fail.");
                                }
                                else
                                {
                                    verifyResp.Reservation_Id = reservationResponse.Reservation_Id;
                                }
                            }
                            catch (Exception ex)
                            {
                                verifyResp.err_msg = ex.Message.ToString();
                                //verifyResp.is_success = false;
                                //return verifyResp;
                            }
                        }
                    }
                }
                bioLogNeeded = true;

                object reqModel_temp = reqModel;

                try
                {
                    string req_string = JsonConvert.SerializeObject(reqModel_temp);
                    JObject parsedObj = JObject.Parse(req_string);
                    var attributes = parsedObj["data"]?["attributes"] as JObject;
                    if (attributes != null)
                    {
                        if (item.dest_left_thumb != null)
                            attributes["dest_left_thumb"] = null;

                        if (item.dest_left_index != null)
                            attributes["dest_left_index"] = null;

                        if (item.dest_right_thumb != null)
                            attributes["dest_right_thumb"] = null;

                        if (item.dest_right_index != null)
                            attributes["dest_right_index"] = null;

                        if (item.src_left_index != null)
                            attributes["src_left_index"] = null; // Fixed the logical mismatch

                        if (item.src_left_thumb != null)
                            attributes["src_left_thumb"] = null;

                        if (item.src_right_index != null)
                            attributes["src_right_index"] = null;

                        if (item.src_right_thumb != null)
                            attributes["src_right_thumb"] = null;
                    }

                    log.req_blob = byteArrayConverter.GetGenericJsonData(parsedObj.ToString());
                }
                catch (Exception ex)
                {
                    throw new Exception("Error Occurred in FP Set to Null in Log time.", ex);
                }

                try
                {
                    log.req_time = DateTime.Now;
                    bioResponse = await genericApiCall.HttpPostRequest(reqModel, meathodUrl, "BioVerificationReqToBss");
                    log.res_time = DateTime.Now;
                }
                catch (Exception ex)
                {
                    log.message = "DBSS: Bio Req-" + ex.Message.ToString();
                    verifyResp.err_msg = "DBSS: Bio Req-" + ex.Message.ToString();
                    throw new Exception("DBSS: Bio Req-" + ex.Message);
                }

                //log.res_string = JsonConvert.SerializeObject(bioResponse.ToString()).ToString();
                log.res_blob = byteArrayConverter.GetGenericJsonData(bioResponse);

                BioResModel? response = new BioResModel();
                try
                {
                    string bioResponsestr = bioResponse?.ToString() ?? string.Empty;
                    response = JsonConvert.DeserializeObject<BioResModel>(bioResponsestr);
                }
                catch (Exception)
                {
                    verifyResp.err_msg = "DBSS: Biometric Api Response Parsing Error.";
                    throw new Exception("DBSS: Biometric Api Response Parsing Error.");
                }

                if (response == null)
                {
                    verifyResp.err_msg = "DBSS: Invalid Biometric Api Response.";
                    throw new Exception("DBSS: Invalid Biometric Api Response.");
                }
                else if (response.data == null)
                {
                    verifyResp.err_msg = "DBSS: Invalid Biometric Api Response.";
                    throw new Exception("DBSS: Invalid Biometric Api Response.");
                }
                else if (response.data.request_id == null)
                {
                    verifyResp.err_msg = "DBSS: Invalid Biometric Api Response.";
                    throw new Exception("DBSS: Invalid Biometric Api Response.");
                }
                // this  is successfull case and bss give us failled response
                item.bss_request_id = response.data.request_id;
                log.is_success = 1;
                verifyResp.is_success = true;
                verifyResp.bss_req_id = response.data.request_id;
                item.bss_request_id = verifyResp.bss_req_id;

                return verifyResp;
            }
            catch (Exception ex)
            {
                if (bioLogNeeded || isCdtError)
                {
                    ErrorDescription error = new ErrorDescription();
                    try { error = await _manageExecption.ManageException(ex, ex.HResult, "DBSS Service"); }
                    catch { }
                    log.req_time = reqTime;
                    log.res_time = resTime;
                    //log.res_string = JsonConvert.SerializeObject(bioResponse != null ? bioResponse.ToString() : ex.Message).ToString();
                    log.res_blob = byteArrayConverter.GetGenericJsonData(bioResponse != null ? bioResponse.ToString() : ex.Message);
                    log.message = error.error_custom_msg ?? error.error_description;
                    log.error_code = error.error_code ?? "";
                    log.error_source = error.error_source ?? "DBSS Service";
                    log.is_success = 0;
                    // BIRequset Table Update Status 150 and Error id and description for biometric Failuer
                    item.status = 150;
                    item.error_id = error.error_id;
                    item.error_description = error.error_custom_msg ?? error.error_description;
                    verifyResp.error_Id = item.error_id;
                    verifyResp.is_success = false;
                    verifyResp.err_code = error.error_code ?? "";
                    verifyResp.err_msg = ex.Message.ToString();
                }
                return verifyResp;
            }
            finally
            {
                if (bioLogNeeded)
                {
                    //log.bss_request_id = item.bss_request_id;
                    log.bi_token_number = item.bi_token_number;
                    log.msisdn = item.msisdn;
                    log.user_id = item.user_id;
                    log.integration_point_from = (int)IntegrationPoints.BI;
                    log.integration_point_to = (int)IntegrationPoints.BSS;
                    log.method_name = "BioVerificationReqToBss";
                    log.purpose_number = item.purpose_number.ToString();
                    await _bllLog.RAToDBSSLog(log);
                }
            }
        }

        public async Task<BioVerifyResp> BioVerificationReqToBssV2(BiomerticDataModel item, object reqModel, string meathodUrl, object blob_data)
        {
            BIAToDBSSLog log = new BIAToDBSSLog();
            object bioResponse = new object();
            bool bioLogNeeded = false;
            bool isCdtError = false;
            //log.status = item.status;
            BL_Json byteArrayConverter = new BL_Json();
            BioVerifyResp verifyResp = new BioVerifyResp();
            // SingleSourceCheckResponseModel checkResponseModel = new SingleSourceCheckResponseModel();
            SingleSourceCheckResponseModelRevamp checkResponseModel = new SingleSourceCheckResponseModelRevamp();
            MSISDNReservationResponse reservationResponse = new MSISDNReservationResponse();
            DateTime reqTime = DateTime.Now;
            DateTime resTime = DateTime.Now;

            try
            {
                if (item.purpose_number == (int)EnumPurposeNumber.NewRegistration && String.IsNullOrEmpty(item.poc_number))
                {
                    checkResponseModel = await SingleSourceCheckThroughAPI(item.msisdn, item.user_id);

                    if (checkResponseModel.Status == true)
                    {
                        verifyResp.is_success = false;
                        verifyResp.err_msg = checkResponseModel.Message;
                        log.message = checkResponseModel.Message;
                        return verifyResp;
                    }
                }

                if (string.IsNullOrEmpty(item.poc_number) ||
                    (!string.IsNullOrEmpty(item.poc_number)
                        && item.purpose_number == (int)EnumPurposeNumber.SIMReplacement
                        && (item.sim_replacement_type == (int)EnumSIMReplacementType.ByPOC
                            || item.sim_replacement_type == (int)EnumSIMReplacementType.ByAuthPerson)
                        )
                     )
                {
                    if (item.purpose_number == (int)EnumPurposeNumber.NewRegistration || item.purpose_number == (int)EnumPurposeNumber.MNPRegistration || item.purpose_number == (int)EnumPurposeNumber.MNPEmergencyReturn)
                    {
                        string CDTMessage = await CDT(item);
                        if (!string.IsNullOrEmpty(CDTMessage))
                        {
                            isCdtError = true;
                            verifyResp.err_msg = "DBSS: CDT Operation Fail.";
                            log.message = "DBSS: CDT Operation Fail.";
                            throw new Exception(CDTMessage);
                        }
                    }//######################## CDT Others ###########################
                    else if (item.purpose_number == (int)EnumPurposeNumber.SIMReplacement
                            || item.purpose_number == (int)EnumPurposeNumber.DeRegistration)
                    {
                        var res = GetPropertyValue(reqModel, "data.attributes.msisdn");
                        string? msisdn = res?.ToString();
                        bool isOtherCDTSuccess = await OtherCDT(item);
                        if (!isOtherCDTSuccess)
                        {
                            verifyResp.err_msg = "DBSS: Other CDT Operation Fail.";
                            log.message = "DBSS: Other CDT Operation Fail.";
                            throw new Exception("DBSS: Other CDT Operation Fail.");
                        }
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.SIMTransfer)
                    {
                        RACommonResponse raResp = new RACommonResponse();
                        raResp = await OtherCDTForTOS(item);
                        if (raResp.result == false)
                        {
                            verifyResp.err_msg = raResp.message;
                            log.message = "DBSS: Other CDT Operation Fail.";
                            return verifyResp;
                            //throw new Exception("DBSS: Other CDT Operation for TOS Fail.");
                        }                       
                    }
                    if (item.is_paired != null)
                    {
                        if (item.is_paired == 0 && item.purpose_number == (int)EnumPurposeNumber.NewRegistration)
                        {
                            try
                            {
                                reservationResponse = await MSISDNReservation(item);
                                if (reservationResponse.IsReserve != true)
                                {
                                    log.message = reservationResponse.Error_message;
                                    verifyResp.err_msg = "DBSS: MSISDN Reservation Fail.";
                                    throw new Exception("DBSS: MSISDN Reservation Fail.");
                                }
                                else
                                {
                                    verifyResp.Reservation_Id = reservationResponse.Reservation_Id;
                                }
                            }
                            catch (Exception ex)
                            {
                                verifyResp.err_msg = ex.Message.ToString();
                                //verifyResp.is_success = false;
                                //return verifyResp;
                            }
                        }
                    }
                }
                bioLogNeeded = true;
                try
                {
                    log.req_blob = byteArrayConverter.GetGenericJsonData(blob_data);
                }
                catch { }
                try
                {
                    log.req_time = DateTime.Now;
                    bioResponse = await genericApiCall.HttpPostRequest(reqModel, meathodUrl, "BioVerificationReqToBssV2");
                    log.res_time = DateTime.Now;
                }
                catch (Exception ex)
                {
                    log.message = "DBSS: Bio Req-" + ex.Message.ToString();
                    verifyResp.err_msg = "DBSS: Bio Req-" + ex.Message.ToString();
                    throw;
                }

                //log.res_string = JsonConvert.SerializeObject(bioResponse.ToString()).ToString();
                log.res_blob = byteArrayConverter.GetGenericJsonData(bioResponse);

                BioResModel? response = null;
                try
                {
                    string bioResponsestr = bioResponse?.ToString() ?? string.Empty;
                    response = JsonConvert.DeserializeObject<BioResModel>(bioResponsestr);
                }
                catch (Exception)
                {
                    verifyResp.err_msg = "DBSS: Biometric Api Response Parsing Error.";
                    throw;
                }

                if (response == null)
                {
                    verifyResp.err_msg = "DBSS: Invalid Biometric Api Response.";
                    throw new Exception("DBSS: Invalid Biometric Api Response.");
                }
                else if (response.data == null)
                {
                    verifyResp.err_msg = "DBSS: Invalid Biometric Api Response.";
                    throw new Exception("DBSS: Invalid Biometric Api Response.");
                }
                else if (response.data.request_id == null)
                {
                    verifyResp.err_msg = "DBSS: Invalid Biometric Api Response.";
                    throw new Exception("DBSS: Invalid Biometric Api Response.");
                }
                // this  is successfull case and bss give us failled response
                item.bss_request_id = response.data.request_id;
                log.is_success = 1;
                verifyResp.is_success = true;
                verifyResp.bss_req_id = response.data.request_id;
                item.bss_request_id = verifyResp.bss_req_id;

                return verifyResp;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                if (bioLogNeeded || isCdtError)
                {
                    ErrorDescription error = new ErrorDescription();
                    try { error = await _manageExecption.ManageException(ex, ex.HResult, "DBSS Service"); }
                    catch { }
                    log.req_time = reqTime;
                    log.res_time = resTime;
                    //log.res_string = JsonConvert.SerializeObject(bioResponse != null ? bioResponse.ToString() : ex.Message).ToString();
                    log.res_blob = byteArrayConverter.GetGenericJsonData(bioResponse != null ? bioResponse.ToString() : ex.Message);
                    log.message = error.error_custom_msg ?? error.error_description;
                    log.error_code = error.error_code ?? "";
                    log.error_source = error.error_source ?? "DBSS Service";
                    log.is_success = 0;
                    // BIRequset Table Update Status 150 and Error id and description for biometric Failuer
                    item.status = 150;
                    item.error_id = error.error_id;
                    item.error_description = error.error_custom_msg ?? error.error_description;
                    verifyResp.error_Id = item.error_id;
                    verifyResp.is_success = false;
                    verifyResp.err_code = error.error_code ?? "";
                    verifyResp.err_msg = ex.Message.ToString();
                }
                //throw ex;
                return verifyResp;
            }
            finally
            {
                if (bioLogNeeded)
                {
                    //log.bss_request_id = item.bss_request_id;
                    log.bi_token_number = item.bi_token_number;
                    log.msisdn = item.msisdn;
                    log.user_id = item.user_id;
                    log.integration_point_from = (int)IntegrationPoints.BI;
                    log.integration_point_to = (int)IntegrationPoints.BSS;
                    log.method_name = "BioVerificationReqToBssV2";
                    log.purpose_number = item.purpose_number.ToString();
                    await _bllLog.RAToDBSSLog(log);
                }
            }
        }

        public async Task<BioVerifyResp> BioVerificationReqToBssV3(BiomerticDataModel item, object reqModel, string meathodUrl, object blob_data)
        {
            BIAToDBSSLog log = new BIAToDBSSLog();
            object bioResponse = new object();
            bool bioLogNeeded = false;
            bool isCdtError = false;
            BL_Json byteArrayConverter = new BL_Json();
            BioVerifyResp verifyResp = new BioVerifyResp();
            SingleSourceCheckResponseModelRevamp checkResponseModel = new SingleSourceCheckResponseModelRevamp();
            MSISDNReservationResponse reservationResponse = new MSISDNReservationResponse();
            DateTime reqTime = DateTime.Now;
            DateTime resTime = DateTime.Now;

            try
            {
                if (item.purpose_number == (int)EnumPurposeNumber.NewRegistration && String.IsNullOrEmpty(item.poc_number))
                {
                    checkResponseModel = await SingleSourceCheckThroughAPI(item.msisdn, item.user_id);

                    if (checkResponseModel.Status == true)
                    {
                        verifyResp.is_success = false;
                        verifyResp.err_msg = checkResponseModel.Message;
                        log.message = checkResponseModel.Message;
                        return verifyResp;
                    }
                }

                if (string.IsNullOrEmpty(item.poc_number) ||
                    (!string.IsNullOrEmpty(item.poc_number)
                        && item.purpose_number == (int)EnumPurposeNumber.SIMReplacement
                        && (item.sim_replacement_type == (int)EnumSIMReplacementType.ByPOC
                            || item.sim_replacement_type == (int)EnumSIMReplacementType.ByAuthPerson)
                        )
                     )
                {

                    if (item.purpose_number == (int)EnumPurposeNumber.NewRegistration || item.purpose_number == (int)EnumPurposeNumber.MNPRegistration || item.purpose_number == (int)EnumPurposeNumber.MNPEmergencyReturn)
                    {
                        string CDTMessage = await CDT(item);
                        if (!string.IsNullOrEmpty(CDTMessage))
                        {
                            isCdtError = true;
                            verifyResp.err_msg = "DBSS: CDT Operation Fail.";
                            log.message = "DBSS: CDT Operation Fail.";
                            throw new Exception(CDTMessage);
                        }
                    }//######################## CDT Others ###########################
                    else if (item.purpose_number == (int)EnumPurposeNumber.SIMReplacement
                            || item.purpose_number == (int)EnumPurposeNumber.DeRegistration)
                    {
                        var res = GetPropertyValue(reqModel, "data.attributes.msisdn");
                        string? msisdn = res?.ToString();
                        bool isOtherCDTSuccess = await OtherCDT(item);
                        if (!isOtherCDTSuccess)
                        {
                            verifyResp.err_msg = "DBSS: Other CDT Operation Fail.";
                            log.message = "DBSS: Other CDT Operation Fail.";
                            throw new Exception("DBSS: Other CDT Operation Fail.");
                        }
                    }
                    else if (item.purpose_number == (int)EnumPurposeNumber.SIMTransfer)
                    {
                        RACommonResponse raResp = new RACommonResponse();
                        raResp = await OtherCDTForTOS(item);
                        if (raResp.result == false)
                        {
                            verifyResp.err_msg = raResp.message;
                            log.message = "DBSS: Other CDT Operation Fail.";
                            return verifyResp;
                            //throw new Exception("DBSS: Other CDT Operation for TOS Fail.");
                        }
                    }
                }
                bioLogNeeded = true;
                object reqModel_temp = reqModel;
                try
                {
                    log.req_blob = byteArrayConverter.GetGenericJsonData(blob_data);
                }
                catch { }
                try
                {
                    log.req_time = DateTime.Now;
                    bioResponse = await genericApiCall.HttpPostRequest(reqModel, meathodUrl, "BioVerificationReqToBssV3");
                    log.res_time = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "ExMessage");
                    ErrorDescription error = new ErrorDescription();
                    error = await _manageExecption.ManageException(ex, ex.HResult, "CDT");
                    string erMessage = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description;
                    log.message = "DBSS: Bio Req-" + erMessage;
                    verifyResp.err_msg = "DBSS: Bio Req-" + erMessage;
                    throw;
                }

                //log.res_string = JsonConvert.SerializeObject(bioResponse.ToString()).ToString();
                log.res_blob = byteArrayConverter.GetGenericJsonData(bioResponse);

                BioResModel? response = null;
                try
                {
                    string bioResponsestr = bioResponse?.ToString() ?? string.Empty;
                    response = JsonConvert.DeserializeObject<BioResModel>(bioResponsestr);
                }
                catch (Exception)
                {
                    verifyResp.err_msg = "DBSS: Biometric Api Response Parsing Error.";
                    throw;
                }

                if (response == null)
                {
                    verifyResp.err_msg = "DBSS: Invalid Biometric Api Response.";
                    throw new Exception("DBSS: Invalid Biometric Api Response.");
                }
                else if (response.data == null)
                {
                    verifyResp.err_msg = "DBSS: Invalid Biometric Api Response.";
                    throw new Exception("DBSS: Invalid Biometric Api Response.");
                }
                else if (response.data.request_id == null)
                {
                    verifyResp.err_msg = "DBSS: Invalid Biometric Api Response.";
                    throw new Exception("DBSS: Invalid Biometric Api Response.");
                }
                // this  is successfull case and bss give us failled response
                item.bss_request_id = response.data.request_id;
                log.is_success = 1;
                verifyResp.is_success = true;
                verifyResp.bss_req_id = response.data.request_id;
                item.bss_request_id = verifyResp.bss_req_id;

                return verifyResp;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                if (bioLogNeeded || isCdtError)
                {
                    ErrorDescription error = new ErrorDescription();
                    try { error = await _manageExecption.ManageException(ex, ex.HResult, "DBSS Service"); }
                    catch { }
                    log.req_time = reqTime;
                    log.res_time = resTime;
                    //log.res_string = JsonConvert.SerializeObject(bioResponse != null ? bioResponse.ToString() : ex.Message).ToString();
                    log.res_blob = byteArrayConverter.GetGenericJsonData(bioResponse != null ? bioResponse.ToString() : ex.Message);
                    log.message = error.error_custom_msg ?? error.error_description;
                    log.error_code = error.error_code ?? "";
                    log.error_source = error.error_source ?? "DBSS Service";
                    log.is_success = 0;
                    // BIRequset Table Update Status 150 and Error id and description for biometric Failuer
                    item.status = 150;
                    item.error_id = error.error_id;
                    item.error_description = error.error_custom_msg ?? error.error_description;
                    verifyResp.error_Id = item.error_id;
                    verifyResp.is_success = false;
                    verifyResp.err_code = error.error_code ?? "";
                    verifyResp.err_msg = ex.Message.ToString();
                }
                //throw ex;
                return verifyResp;
            }
            finally
            {
                if (bioLogNeeded)
                {
                    //log.bss_request_id = item.bss_request_id;
                    log.bi_token_number = item.bi_token_number;
                    log.msisdn = item.msisdn;
                    log.user_id = item.user_id;
                    log.integration_point_from = (int)IntegrationPoints.BI;
                    log.integration_point_to = (int)IntegrationPoints.BSS;
                    log.method_name = "BioVerificationReqToBssV3";
                    log.purpose_number = item.purpose_number.ToString();
                    await _bllLog.RAToDBSSLog(log);
                }
            }
        }
        #region CDT
        public async Task<string> CDT(BiomerticDataModel item)
        {
            BIAToDBSSLog log = new BIAToDBSSLog();
            string? res = string.Empty;
            CDTRequestModel cdtReqModel = new CDTRequestModel();
            object cdtResponse = new object();
            BiometricPopulateModel pltObj = new BiometricPopulateModel();
            BL_Json byteArrayConverter = new BL_Json();
            DateTime reqTime = DateTime.Now;
            DateTime resTime = DateTime.Now;

            try
            {
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToLower();

                string meathodUrl = "/api/v1/residential-credit-decisions";
                cdtReqModel = pltObj.PopulateCDTRequestModel(item);

                log.req_blob = byteArrayConverter.GetGenericJsonData(cdtReqModel);
                try
                {
                    log.req_time = DateTime.Now;
                    if(env == "production" || env == "prod")
                    {
                        cdtResponse = await genericApiCall.HttpPostRequest(cdtReqModel, meathodUrl, "CDT");
                    }
                    else
                    {
                        cdtResponse = await genericApiCall.HttpPostRequestCDT(cdtReqModel, meathodUrl, "CDT");
                    }
                       
                    log.res_time = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "ExMessage");
                    ErrorDescription error = new ErrorDescription();
                    error = await _manageExecption.ManageException(ex, ex.HResult, "CDT");
                    string erMessage = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description;
                    throw new Exception("DBSS: CDT " + erMessage);
                }

                //log.res_string = JsonConvert.SerializeObject(cdtResponse.ToString()).ToString();
                log.res_blob = byteArrayConverter.GetGenericJsonData(cdtResponse);

                // if "credit_decision" is "ACCEPTED" then CDT return true else false.
                // till now can not get any response so that job is pending, for this reason if get success then pass by default true.

                string? desision = string.Empty;
                JObject dbssRespObj;
                try
                {
                    string cdtResponseStr = cdtResponse?.ToString() ?? "";
                    dbssRespObj = JObject.Parse(cdtResponseStr);

                    desision = dbssRespObj["data"]?["attributes"]?["credit-decision"]?.ToString();

                    if (!string.IsNullOrEmpty(desision))
                    {
                        if (desision == "ACCEPTED")
                        {
                            res = "";
                        }
                        else if (desision == "REJECTED")
                        {
                            res = dbssRespObj["data"]?["attributes"]?["business-instruction"]?.ToString() ?? "DBSS: CDT Api Response not Valid.";
                        }
                    }
                    else
                    {
                        throw new Exception("DBSS: CDT Api Response not Valid.");
                    }

                    log.is_success = 1;
                    return res;
                }
                catch (Exception)
                { throw new Exception("DBSS: CDT Api Response not Valid."); }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");

                ErrorDescription error = new ErrorDescription();

                try
                {
                    error = await _manageExecption.ManageException(ex, ex.HResult, "BIA");
                }
                catch
                {
                    error = new ErrorDescription(); // Fallback to non-null object
                }

                log.req_time = reqTime;
                log.res_time = resTime;
                log.res_blob = byteArrayConverter.GetGenericJsonData(cdtResponse != null ? cdtResponse : ex.Message);
                log.message = error?.error_custom_msg ?? error?.error_description ?? "Unknown error";
                log.error_code = error?.error_code ?? "";
                log.error_source = error?.error_source ?? "BIA";
                log.is_success = 0;
                item.status = 150;
                item.error_id = error?.error_id ?? 0;
                res = !string.IsNullOrEmpty(error?.error_custom_msg)
                    ? error.error_custom_msg
                    : error?.error_description ?? "Unknown error";
                item.error_description = res;

                return res;
            }
            finally
            {
                //log.bss_request_id = item.bss_request_id;
                log.bi_token_number = item.bi_token_number;
                log.msisdn = item.msisdn;
                log.user_id = item.user_id;
                log.integration_point_from = (int)IntegrationPoints.BI;
                log.integration_point_to = (int)IntegrationPoints.BSS;
                log.method_name = "CDT";
                log.purpose_number = item.purpose_number.ToString();
                await _bllLog.RAToDBSSLog(log);
                //await _bllLog.BALogInsert(log);
            }
        }

        public async Task<bool> OtherCDT(BiomerticDataModel item)
        {
            BL_Json byteArrayConverter = new BL_Json();
            BIAToDBSSLog log = new BIAToDBSSLog();
            bool res = true;
            string meathodUrl = $"/api/v1/subscriptions?filter%5Bmsisdn%5D={item.msisdn}&include=barrings";
            object otherCdtResponse = new object();
            //log.status = item.status;
            DateTime reqTime = DateTime.Now;
            DateTime resTime = DateTime.Now;

            try
            {
                log.req_time = DateTime.Now;
                //log.req_string = meathodUrl;
                log.req_blob = byteArrayConverter.GetGenericJsonData(meathodUrl);
                try
                { otherCdtResponse = await genericApiCall.HttpGetRequestAsync(meathodUrl, "OtherCDT"); }
                catch (Exception)
                { throw; }
                resTime = DateTime.Now;
                log.req_time = reqTime;
                log.res_time = resTime;
                //log.res_string = JsonConvert.SerializeObject(otherCdtResponse.ToString()).ToString();
                log.res_blob = byteArrayConverter.GetGenericJsonData(otherCdtResponse);

                OtherCDTResModel? response = new OtherCDTResModel();
                try
                {
                    response = JsonConvert.DeserializeObject<OtherCDTResModel>(otherCdtResponse.ToString() ?? "");
                }
                catch (Exception)
                { throw; }

                if (response == null)
                    throw new Exception("DBSS: Other CDT Api Response not Valid.");

                if (response.included != null && response.included.Count > 0)
                    foreach (var item1 in response.included)
                    {
                        if (item1.id == "BAR_EXCEPTION" || item1.id == "BAR_RAFM")
                        {
                            throw new Exception("User is Blocked by " + item1.id + " role.");
                        }
                    }

                log.is_success = 1;
                return res;
            }

            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                res = false;
                ErrorDescription error = new ErrorDescription();
                try { error = await _manageExecption.ManageException(ex, ex.HResult, "BIA"); }
                catch { }
                log.req_time = reqTime;
                log.res_time = resTime;
                //log.res_string = JsonConvert.SerializeObject(otherCdtResponse != null ? otherCdtResponse.ToString() : ex.Message).ToString();
                log.res_blob = byteArrayConverter.GetGenericJsonData(otherCdtResponse != null ? otherCdtResponse : ex.Message);
                log.message = error != null ? error.error_description : "";
                log.error_code = error != null ? error.error_code : "";
                log.error_source = error != null ? error.error_source : "BIA";
                log.is_success = 0;
                item.status = 150;
                item.error_id = error != null ? error.error_id : 0;
                if (error != null)
                {
                    item.error_description = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description;
                }
                return res;
            }
            finally
            {
                //log.bss_request_id = item.bss_request_id;
                log.bi_token_number = item.bi_token_number;
                log.msisdn = item.msisdn;
                log.user_id = item.user_id;
                log.integration_point_from = (int)IntegrationPoints.BI;
                log.integration_point_to = (int)IntegrationPoints.BSS;
                log.method_name = "OtherCDT";
                log.purpose_number = item.purpose_number.ToString();

                await _bllLog.RAToDBSSLog(log);
            }
        }

        public async Task<RACommonResponse> OtherCDTForTOS(BiomerticDataModel item)
        {
            BL_Json byteArrayConverter = new BL_Json();
            BIAToDBSSLog log = new BIAToDBSSLog();
            RACommonResponse raResponse = new RACommonResponse();

            string meathodUrl = $"/api/v1/subscriptions?filter%5Bmsisdn%5D={item.msisdn}&include=barrings";
            object otherCdtResponse = new object();
            //log.status = item.status;
            DateTime reqTime = DateTime.Now;
            DateTime resTime = DateTime.Now;
            try
            {
                log.req_time = DateTime.Now;
                //log.req_string = meathodUrl;
                log.req_blob = await byteArrayConverter.GetGenericJsonDataAsync(meathodUrl);
                try
                { otherCdtResponse = await genericApiCall.HttpGetRequestAsync(meathodUrl, "OtherCDTForTOS"); }
                catch (Exception)
                { throw; }
                resTime = DateTime.Now;
                log.req_time = reqTime;
                log.res_time = resTime;
                //log.res_string = JsonConvert.SerializeObject(otherCdtResponse.ToString()).ToString();
                log.res_blob = await byteArrayConverter.GetGenericJsonDataAsync(otherCdtResponse);

                OtherCDTResModel? response = new OtherCDTResModel();
                try
                {
                    response = JsonConvert.DeserializeObject<OtherCDTResModel>(otherCdtResponse.ToString() ?? "");

                }
                catch (Exception)
                { throw; }

                if (response == null)
                    throw new Exception("DBSS: Other CDT For TOS Api Response not Valid.");

                if (response.included != null && response.included.Count > 0)
                {
                    foreach (var item1 in response.included)
                    {
                        if (item1.id == "BAR_EXCEPTION" || item1.id == "BAR_RAFM")
                        {
                            raResponse.result = false;
                            raResponse.message = "User is Blocked by " + item1.id + " role.";
                            throw new Exception("User is Blocked by " + item1.id + " role.");
                        }
                    }

                    #region Cherished number validation for TOS
                    RACommonResponse raCommon = await CheckCherishMSISDNParseForTos(item, "OtherCDTForTOS");

                    if (raCommon.result == true)
                    {
                        if (response.included != null && response.included.Count > 0)
                        {
                            foreach (var item1 in response.included)
                            {
                                if (item1.id.Equals("BAR_PREMIUM"))
                                {
                                    raResponse.result = false;
                                    raResponse.message = "User is Blocked by " + item1.id + " role.";
                                    throw new Exception("User is Blocked by " + item1.id + " role.");
                                }
                            }
                        }
                    }

                    #endregion
                }

                log.is_success = 1;
                raResponse.result = true;
                return raResponse;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                raResponse.result = false;
                ErrorDescription error = new ErrorDescription();
                error = await _manageExecption.ManageException(ex, ex.HResult, "DBSS Service");
                log.req_time = reqTime;
                log.res_time = resTime;
                //log.res_string = JsonConvert.SerializeObject(otherCdtResponse != null ? otherCdtResponse.ToString() : ex.Message).ToString();
                log.res_blob = byteArrayConverter.GetGenericJsonData(otherCdtResponse != null ? otherCdtResponse : ex.Message);
                log.message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description;
                log.error_code =  error.error_code ?? "";
                log.error_source = error.error_source ?? "DBSS Service";
                log.is_success = 0;
                // BIRequset Table Update Status 150 and Error id and description for biometric Failuer
                item.status = 150;
                item.error_id = error.error_id;
                item.error_description = !string.IsNullOrEmpty(error.error_custom_msg)  ? error.error_custom_msg : error.error_description;
                raResponse.message = !string.IsNullOrEmpty(error.error_custom_msg)  ? error.error_custom_msg : error.error_description;
                return raResponse;
            }
            finally
            {
                //log.bss_request_id = item.bss_request_id;
                log.bi_token_number = item.bi_token_number;
                log.msisdn = item.msisdn;
                log.user_id = item.user_id;
                log.integration_point_from = (int)IntegrationPoints.BI;
                log.integration_point_to = (int)IntegrationPoints.BSS;
                log.method_name = "OtherCDTForTOS";
                log.purpose_number = item.purpose_number.ToString();

                await _bllLog.RAToDBSSLog(log);
            }
        }
        #endregion
        #region Single Source Check
        public async Task<SingleSourceCheckResponseModel> SingleSourceCheckFromBioDB(string msisdn, string sim_number, int purpose_No, string poc_number, int sim_rep_type, string dest_doc_id, string dest_dob, string dest_imsi)
        {
            SingleSourceCheckResponseModel checkResponseModel = new SingleSourceCheckResponseModel();
            try
            {
                checkResponseModel = await _bllObj.SingleSourceCheckFromBioDB(msisdn, sim_number, purpose_No, poc_number, sim_rep_type, dest_doc_id, dest_dob, dest_imsi);
            }
            catch (Exception)
            {
                throw;
            }
            return checkResponseModel;
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
                if (msisdnCheckReqest.msisdn != null && msisdnCheckReqest.msisdn.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.msisdn = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.msisdn;
                }

                apiUrl = String.Format(GetAPICollection.CherishMSISDNValidation, msisdnCheckReqest.msisdn);

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
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;
                raRespModel.result = false;

                throw;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.msisdn ?? "");
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number.ToString();
                log.user_id = msisdnCheckReqest.user_id;
                log.method_name = "CheckCherishMSISDNParseForTos";

                await _bllLog.RAToDBSSLog(log);

            }
        }
        public async Task<SingleSourceLoginRes> SingleSourceLogin(string msisdn, string userName)
        {
            using HttpClient client = new HttpClient();
            BIAToDBSSLog log = new BIAToDBSSLog();
            SingleSourceLoginRes loginapiResponse = new SingleSourceLoginRes();
            BL_Json byteArrayConverter = new BL_Json();
            string loginapiUrl = SingleSourceAPI.LoginAPI;
            string loginResponseContent = string.Empty;
            string singleSourceLoginSessionToken = string.Empty;

            DateTime reqTime = DateTime.Now;
            string res = string.Empty;

            try
            {
                var loginReqModel = new SingleSourceLoginReq()
                {
                    user_name = SettingsValues.GetSingleSourceUserName(),
                    password = SettingsValues.GetSingleSourcePassword()
                };

                log.req_blob = byteArrayConverter.GetGenericJsonData(loginReqModel);
                string jsonData = JsonConvert.SerializeObject(loginReqModel);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Async call – avoid .Result
                HttpResponseMessage response = await client.PostAsync(loginapiUrl, content);
                loginResponseContent = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrWhiteSpace(loginResponseContent))
                {
                    loginapiResponse = JsonConvert.DeserializeObject<SingleSourceLoginRes>(loginResponseContent) ?? new SingleSourceLoginRes();
                    singleSourceLoginSessionToken = loginapiResponse.session_token ?? string.Empty;
                    loginapiResponse.session_token = singleSourceLoginSessionToken;
                }

                log.req_time = reqTime;
                log.res_blob = byteArrayConverter.GetGenericJsonData(loginapiResponse);
                loginapiResponse.is_success = true;
            }
            catch (Exception ex)
            {
                res = ex.Message;
                ErrorDescription error = new ErrorDescription();

                try
                {
                    error = await _manageExecption.ManageException(ex, ex.HResult, "Single Source");
                }
                catch { }

                log.req_time = reqTime;
                log.res_time = DateTime.Now;
                log.res_blob = byteArrayConverter.GetGenericJsonData(!string.IsNullOrEmpty(loginResponseContent) ? loginResponseContent : res);
                log.message = error?.error_description ?? string.Empty;
                log.error_code = error?.error_code ?? string.Empty;
                log.error_source = error?.error_source ?? "Single Source";
                log.is_success = 0;

                loginapiResponse.is_success = false;
                loginapiResponse.message = !string.IsNullOrEmpty(error?.error_custom_msg) ? error.error_custom_msg : error?.error_description ?? "Unknown error";

                throw; // better than throw ex;
            }
            finally
            {
                log.method_name = "SingleSourceLogin";
                log.msisdn = _bllLog.FormatMSISDN(msisdn);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = userName;

                await _bllLog.RAToDBSSLog(log);
            }

            return loginapiResponse;
        }

        public async Task<SingleSourceCheckResponseModelRevamp> SingleSourceCheckThroughAPI(string msisdn, string userName)
        {
            using HttpClient client = new HttpClient();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BL_Json byteArrayConverter = new BL_Json();
            string loginapiUrl = SingleSourceAPI.LoginAPI;
            string apiUrl = SingleSourceAPI.BiometricInfoAPI;
            string loginResponseContent = string.Empty;
            string messages = SettingsValues.GetSingleSourceMessage();
            DateTime reqTime = DateTime.Now;
            DateTime resTime = DateTime.Now;

            SingleSourceLoginRes loginapiResponse = new SingleSourceLoginRes();
            SingleSourceRes infoResponse = new SingleSourceRes();

            try
            {
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    if (string.IsNullOrEmpty(singleSourceLoginSessionToken))
                    {
                        loginapiResponse = await SingleSourceLogin(msisdn, userName);
                        singleSourceLoginSessionToken = loginapiResponse.session_token;
                    }

                    log.req_time = DateTime.Now;

                    var reqModel = new SingleSourceReqModel { msisdn = msisdn };
                    string jsonData = JsonConvert.SerializeObject(reqModel);
                    log.req_blob = byteArrayConverter.GetGenericJsonData(jsonData);

                    var responseContent = await genericApiCall.HttpPostRequestSingleSourceCheck(
                        reqModel, apiUrl, singleSourceLoginSessionToken, "SingleSourceCheckThroughAPI");

                    string json = JsonConvert.SerializeObject(responseContent);
                    infoResponse = JsonConvert.DeserializeObject<SingleSourceRes>(json) ?? new SingleSourceRes();

                    log.res_time = DateTime.Now;
                    log.res_blob = byteArrayConverter.GetGenericJsonData(responseContent);

                    if (infoResponse.is_success == false &&
                        infoResponse.message?.Contains("Invalid session token") == true)
                    {
                        // Invalidate token and retry
                        singleSourceLoginSessionToken = string.Empty;
                        continue;
                    }

                    bool isActive = infoResponse.Data?.is_active ?? false;
                    string message = isActive ? messages : infoResponse.message ?? "";

                    return new SingleSourceCheckResponseModelRevamp
                    {
                        Status = isActive,
                        Message = message
                    };
                }

                // Final fallback after retries
                bool isActiveFinal = infoResponse.Data?.is_active ?? false;
                string messageFinal = isActiveFinal ? messages : infoResponse.message ?? "";

                return new SingleSourceCheckResponseModelRevamp
                {
                    Status = isActiveFinal,
                    Message = messageFinal
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");

                ErrorDescription error = new ErrorDescription();
                try
                {
                    error = await _manageExecption.ManageException(ex, ex.HResult, "Single Source");
                }
                catch { }

                log.req_time = reqTime;
                log.res_time = DateTime.Now;
                log.res_blob = byteArrayConverter.GetGenericJsonData(ex.Message);
                log.message = error?.error_description ?? string.Empty;
                log.error_code = error?.error_code ?? string.Empty;
                log.error_source = error?.error_source ?? "Single Source";
                log.is_success = 0;

                throw; // Preserve stack trace
            }
            finally
            {
                log.method_name = "SingleSourceCheckThroughAPI";
                log.msisdn = _bllLog.FormatMSISDN(msisdn);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = userName;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        #endregion

        public async Task<RACommonResponse> CherishedNumberValidationForTOS(BiomerticDataModel msisdnCheckReqest, string apiName)
        {
            BL_Json byteArrayConverter = new BL_Json();
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

                apiUrl = String.Format(GetAPICollection.CherishMSISDNValidation, msisdnCheckReqest.msisdn);

                log.req_blob = await byteArrayConverter.GetGenericJsonDataAsync(apiUrl);

                log.req_time = DateTime.Now;
                dbssResp = await genericApiCall.HttpGetRequest(apiUrl, "CherishedNumberValidationForTOS");
                log.res_time = DateTime.Now;

                txtResp = Convert.ToString(dbssResp);

                log.res_blob = await byteArrayConverter.GetGenericJsonDataAsync(dbssResp);

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.CherishMSISDNReqParsing(dbssResp, msisdnCheckReqest.user_id);

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
                ErrorDescription error = new ErrorDescription();
                error = await _manageExecption.ManageException(ex, ex.HResult, "DBSS Service");
                raRespModel.result = false;
                raRespModel.message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.is_success = 0;
                log.message = raRespModel.message;

                return raRespModel;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.msisdn);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number.ToString();
                log.user_id = msisdnCheckReqest.user_id;
                log.method_name = "CherishedNumberValidationForTOS";

                await _bllLog.RAToDBSSLog(log);
            }
        }


        #region Privat Method
        public static object? GetPropertyValue(object src, string propName)
        {
            if (src == null) throw new ArgumentException("CDT Other-Value cannot be null.", nameof(src));
            if (propName == null) throw new ArgumentException("CDT Other-Value cannot be null.", nameof(propName));

            if (propName.Contains(".")) // For nested properties
            {
                var temp = propName.Split(new[] { '.' }, 2);
                var nestedObject = GetPropertyValue(src, temp[0]);
                return nestedObject != null ? GetPropertyValue(nestedObject, temp[1]) : null;
            }
            else
            {
                var prop = src.GetType().GetProperty(propName);
                return prop?.GetValue(src);
            }
        }

        //public static object GetPropertyValue(object src, string propName)
        //{
        //    if (src == null) throw new ArgumentException("CDT Other-Value cannot be null.", "src");
        //    if (propName == null) throw new ArgumentException("CDT Other-Value cannot be null.", "propName");

        //    if (propName.Contains("."))//complex type nested
        //    {
        //        var temp = propName.Split(new char[] { '.' }, 2);
        //        return GetPropertyValue(GetPropertyValue(src, temp[0]), temp[1]);
        //    }
        //    else
        //    {
        //        var prop = src.GetType().GetProperty(propName);
        //        return prop != null ? prop.GetValue(src, null) : "";
        //    }
        //}
        #endregion
        #region Unreserve MSISDN

        /// <summary>
        /// Unreserve-MSISDN
        /// </summary>
        /// <param name="msisdnReservationId"></param>
        //public async Task<RACommonResponse> UnreserveMSISDN(string msisdnReservationId, string sessionToken, string bio_request_id, string bi_token_number, string msisdn)
        //{
        //    BIAToDBSSLog logObj = new BIAToDBSSLog();
        //    string apiUrl = "", txtResp = "";


        //    RACommonResponse resp = new RACommonResponse();
        //    BLLRAToDBSSParse rAParse = new BLLRAToDBSSParse();
        //    UnreserveMSISDNRequestRootobject reqRootObj = new UnreserveMSISDNRequestRootobject();
        //    try
        //    {
        //        reqRootObj = rAParse.UnreserveMSISDNReqParsing(msisdnReservationId);

        //        apiUrl = String.Format(DeleteAPICollection.UnreserveMSISDN);

        //        logObj.req_blob = _blJson.GetGenericJsonData(reqRootObj);
        //        logObj.req_time = DateTime.Now;

        //        //object dbssResp = new object();
        //        object dbssResp = await _apiReq.HttpDeleteRequest(reqRootObj, apiUrl, "UnreserveMSISDN");

        //        logObj.res_blob = _blJson.GetGenericJsonData(dbssResp);
        //        logObj.res_time = DateTime.Now;
        //        txtResp = apiUrl + "//" + Convert.ToString(dbssResp);

        //        if (dbssResp != null)
        //        {
        //            logObj.is_success = 1;

        //            var dbssRespModel = JsonConvert.DeserializeObject<ReserverMSISDNResponseRootobject>(dbssResp.ToString());
        //            if (dbssRespModel.data != null)
        //            {
        //                if (dbssRespModel.data.status == 200)
        //                {
        //                    resp.result = true;
        //                    resp.message = "MSISDN unreserved successfully.";
        //                    //ToDo: Update BIReq Tbl remarks column with 200 status.(need to confirm.)
        //                    //return resp;
        //                }
        //                else
        //                {
        //                    resp.result = false;
        //                    resp.message = "MSISDN unreservation failed!";
        //                    //return resp;
        //                }
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        logObj.res_blob = _blJson.GetGenericJsonData(ex.Message);
        //        logObj.res_time = DateTime.Now;
        //        logObj.is_success = 0;
        //        logObj.error_code = error.error_code ?? String.Empty;
        //        logObj.error_source = error.error_source ?? String.Empty;
        //        logObj.message = error.error_custom_msg ?? error.error_description;

        //        resp.result = false;
        //        resp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //    }
        //    finally
        //    {
        //        logObj.msisdn = msisdn;
        //        logObj.bi_token_number = bi_token_number;
        //        logObj.dbss_request_id = bio_request_id;

        //        logObj.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        logObj.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
        //        logObj.user_id = _bllCommon.GetUserNameFromSessionToken(sessionToken);
        //        logObj.method_name = "UnreserveMSISDN";

        //        await _bllLog.RAToDBSSLog(logObj, apiUrl + Convert.ToString(reqRootObj), txtResp);
        //    }
        //    return resp;
        //}

        //public async Task<RACommonResponse> UnreserveMSISDNV2(string msisdnReservationId, string sessionToken, string? bio_request_id, string bi_token_number, string msisdn, string retailer_id)
        //{
        //    BIAToDBSSLog logObj = new BIAToDBSSLog();
        //    string apiUrl = "", txtResp = "";

        //    RACommonResponse resp = new RACommonResponse();
        //    BLLRAToDBSSParse rAParse = new BLLRAToDBSSParse();
        //    UnreserveMSISDNRequestRootobject reqRootObj = new UnreserveMSISDNRequestRootobject();
        //    try
        //    {
        //        reqRootObj = rAParse.UnreserveMSISDNReqParsing(msisdnReservationId);

        //        apiUrl = String.Format(DeleteAPICollection.UnreserveMSISDN);

        //        logObj.req_blob = _blJson.GetGenericJsonData(reqRootObj);
        //        logObj.req_time = DateTime.Now;

        //        //object dbssResp = new object();
        //        object dbssResp = await _apiReq.HttpDeleteRequest(reqRootObj, apiUrl, "UnreserveMSISDNV2");

        //        logObj.res_blob = _blJson.GetGenericJsonData(dbssResp);
        //        logObj.res_time = DateTime.Now;
        //        txtResp = apiUrl + "//" + Convert.ToString(dbssResp);

        //        if (dbssResp != null)
        //        {
        //            logObj.is_success = 1;

        //            var dbssRespModel = JsonConvert.DeserializeObject<ReserverMSISDNResponseRootobject>(dbssResp.ToString());
        //            if (dbssRespModel.data != null)
        //            {
        //                if (dbssRespModel.data.status == 200)
        //                {
        //                    resp.result = true;
        //                    resp.message = "MSISDN unreserved successfully.";
        //                    //ToDo: Update BIReq Tbl remarks column with 200 status.(need to confirm.)
        //                    //return resp;
        //                }
        //                else
        //                {
        //                    resp.result = false;
        //                    resp.message = "MSISDN unreservation failed!";
        //                    //return resp;
        //                }
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        logObj.res_time = DateTime.Now;
        //        ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        logObj.res_blob = _blJson.GetGenericJsonData(error);
        //        logObj.is_success = 0;
        //        logObj.error_code = error.error_code ?? String.Empty;
        //        logObj.error_source = error.error_source ?? String.Empty;
        //        logObj.message = error.error_description ?? String.Empty;

        //        resp.result = false;
        //        resp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //    }
        //    finally
        //    {
        //        logObj.msisdn = msisdn;
        //        logObj.bi_token_number = bi_token_number;
        //        logObj.dbss_request_id = bio_request_id;

        //        logObj.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        logObj.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
        //        logObj.user_id = retailer_id;
        //        logObj.method_name = "UnreserveMSISDNV2";

        //        await _bllLog.RAToDBSSLog(logObj, apiUrl + Convert.ToString(reqRootObj), txtResp);
        //    }
        //    return resp;
        //}

        public async Task<RACommonResponse> UnreserveMSISDNV2(string msisdnReservationId, string sessionToken, string? bio_request_id, string bi_token_number, string msisdn, string retailer_id)
        {
            BIAToDBSSLog logObj = new BIAToDBSSLog();
            RACommonResponse resp = new RACommonResponse();
            string apiUrl = string.Empty, txtResp = string.Empty;

            try
            {
                var rAParse = new BLLRAToDBSSParse();
                var reqRootObj = rAParse.UnreserveMSISDNReqParsing(msisdnReservationId);

                apiUrl = DeleteAPICollection.UnreserveMSISDN;

                logObj.req_blob = _blJson.GetGenericJsonData(reqRootObj);
                logObj.req_time = DateTime.Now;

                var dbssResp = await _apiReq.HttpDeleteRequest(reqRootObj, apiUrl, "UnreserveMSISDNV2");

                logObj.res_blob = _blJson.GetGenericJsonData(dbssResp);
                logObj.res_time = DateTime.Now;
                txtResp = $"{apiUrl}//{dbssResp}";

                if (dbssResp != null)
                {
                    logObj.is_success = 1;

                    var dbssRespModel = JsonConvert.DeserializeObject<ReserverMSISDNResponseRootobject>(dbssResp.ToString() ?? string.Empty);
                    if (dbssRespModel?.data?.status == 200)
                    {
                        resp.result = true;
                        resp.message = "MSISDN unreserved successfully.";
                    }
                    else
                    {
                        resp.result = false;
                        resp.message = "MSISDN unreservation failed!";
                    }
                }
                else
                {
                    resp.result = false;
                    resp.message = "No response received from DBSS.";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "UnreserveMSISDNV2 Exception");
                logObj.res_time = DateTime.Now;

                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                logObj.res_blob = _blJson.GetGenericJsonData(error);
                logObj.is_success = 0;
                logObj.error_code = error.error_code ?? string.Empty;
                logObj.error_source = error.error_source ?? "BIA";
                logObj.message = error.error_description ?? "Unexpected error occurred.";

                resp.result = false;
                resp.message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description ?? "Operation failed.";
            }
            finally
            {
                logObj.msisdn = msisdn;
                logObj.bi_token_number = bi_token_number;
                logObj.dbss_request_id = bio_request_id ?? "";
                logObj.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                logObj.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                logObj.user_id = retailer_id;
                logObj.method_name = nameof(UnreserveMSISDNV2);

                await _bllLog.RAToDBSSLog(logObj);
            }

            return resp;
        }

        public async Task<RACommonResponse> UnreserveMSISDNV3(string msisdnReservationId, string sessionToken, string bio_request_id, string bi_token_number, string msisdn)
        {
            BIAToDBSSLog logObj = new BIAToDBSSLog();
            RACommonResponse resp = new RACommonResponse();
            string apiUrl = string.Empty;
            string txtResp = string.Empty;

            var rAParse = new BLLRAToDBSSParse();
            UnreserveMSISDNRequestRootobject? reqRootObj = null;
            object? dbssResp = null;

            try
            {
                reqRootObj = rAParse.UnreserveMSISDNReqParsing(msisdnReservationId);
                apiUrl = DeleteAPICollection.UnreserveMSISDN;

                logObj.req_blob = _blJson.GetGenericJsonData(reqRootObj);
                logObj.req_time = DateTime.Now;

                dbssResp = await _apiReq.HttpDeleteRequest(reqRootObj, apiUrl, "UnreserveMSISDNV3");

                logObj.res_blob = _blJson.GetGenericJsonData(dbssResp);
                logObj.res_time = DateTime.Now;
                txtResp = $"{apiUrl}//{Convert.ToString(dbssResp)}";

                if (dbssResp != null)
                {
                    logObj.is_success = 1;

                    var dbssRespModel = JsonConvert.DeserializeObject<ReserverMSISDNResponseRootobject>(dbssResp.ToString() ?? string.Empty);
                    if (dbssRespModel?.data?.status == 200)
                    {
                        resp.result = true;
                        resp.message = "MSISDN unreserved successfully.";
                    }
                    else
                    {
                        resp.result = false;
                        resp.message = "MSISDN unreservation failed!";
                    }
                }
                else
                {
                    resp.result = false;
                    resp.message = "No response received from DBSS.";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "UnreserveMSISDNV3 Exception");

                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                logObj.res_time = DateTime.Now;
                logObj.res_blob = _blJson.GetGenericJsonData(error);
                logObj.is_success = 0;
                logObj.error_code = error?.error_code ?? string.Empty;
                logObj.error_source = error?.error_source ?? "BIA";
                logObj.message = !string.IsNullOrEmpty(error?.error_description) ? error.error_custom_msg : "Unhandled exception occurred.";

                resp.result = false;
                resp.message = !string.IsNullOrEmpty(error?.error_custom_msg) ? error.error_custom_msg : error?.error_description ?? "Operation failed.";
            }
            finally
            {
                logObj.msisdn = msisdn;
                logObj.bi_token_number = bi_token_number;
                logObj.dbss_request_id = bio_request_id;
                logObj.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                logObj.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                logObj.user_id = _bllCommon.GetUserNameFromSessionTokenV2(sessionToken);
                logObj.method_name = nameof(UnreserveMSISDNV3);

                await _bllLog.RAToDBSSLog(logObj);
            }

            return resp;
        }


        //public async Task<RACommonResponse> UnreserveMSISDNV3(string msisdnReservationId, string sessionToken, string bio_request_id, string bi_token_number, string msisdn)
        //{
        //    BIAToDBSSLog logObj = new BIAToDBSSLog();
        //    string apiUrl = "", txtResp = "";

        //    RACommonResponse resp = new RACommonResponse();
        //    BLLRAToDBSSParse rAParse = new BLLRAToDBSSParse();
        //    UnreserveMSISDNRequestRootobject reqRootObj = new UnreserveMSISDNRequestRootobject();
        //    try
        //    {
        //        reqRootObj = rAParse.UnreserveMSISDNReqParsing(msisdnReservationId);

        //        apiUrl = String.Format(DeleteAPICollection.UnreserveMSISDN);

        //        logObj.req_blob = _blJson.GetGenericJsonData(reqRootObj);
        //        logObj.req_time = DateTime.Now;

        //        //object dbssResp = new object();
        //        object dbssResp = await _apiReq.HttpDeleteRequest(reqRootObj, apiUrl, "UnreserveMSISDNV3");

        //        logObj.res_blob = _blJson.GetGenericJsonData(dbssResp);
        //        logObj.res_time = DateTime.Now;
        //        txtResp = apiUrl + "//" + Convert.ToString(dbssResp);

        //        if (dbssResp != null)
        //        {
        //            logObj.is_success = 1;

        //            var dbssRespModel = JsonConvert.DeserializeObject<ReserverMSISDNResponseRootobject>(dbssResp.ToString());
        //            if (dbssRespModel.data != null)
        //            {
        //                if (dbssRespModel.data.status == 200)
        //                {
        //                    resp.result = true;
        //                    resp.message = "MSISDN unreserved successfully.";
        //                    //ToDo: Update BIReq Tbl remarks column with 200 status.(need to confirm.)
        //                    //return resp;
        //                }
        //                else
        //                {
        //                    resp.result = false;
        //                    resp.message = "MSISDN unreservation failed!";
        //                    //return resp;
        //                }
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        logObj.res_blob = _blJson.GetGenericJsonData(error);
        //        logObj.res_time = DateTime.Now;
        //        logObj.is_success = 0;
        //        logObj.error_code = error.error_code ?? String.Empty;
        //        logObj.error_source = error.error_source ?? String.Empty;
        //        logObj.message = error.error_description ?? String.Empty;

        //        resp.result = false;
        //        resp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //    }
        //    finally
        //    {
        //        logObj.msisdn = msisdn;
        //        logObj.bi_token_number = bi_token_number;
        //        logObj.dbss_request_id = bio_request_id;

        //        logObj.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        logObj.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
        //        logObj.user_id = _bllCommon.GetUserNameFromSessionTokenV2(sessionToken);
        //        logObj.method_name = "UnreserveMSISDNV3";

        //        await _bllLog.RAToDBSSLog(logObj, apiUrl + Convert.ToString(reqRootObj), txtResp);
        //    }
        //    return resp;
        //}

        public async Task<RACommonResponse> UnreserveMSISDNStarTrek(string msisdnReservationId, string userId, string bio_request_id, string bi_token_number, string msisdn)
        {
            BIAToDBSSLog logObj = new BIAToDBSSLog();
            string apiUrl = "", txtResp = "";

            RACommonResponse resp = new RACommonResponse();
            BLLRAToDBSSParse rAParse = new BLLRAToDBSSParse();
            UnreserveMSISDNRequestRootobject reqRootObj = new UnreserveMSISDNRequestRootobject();
            try
            {
                reqRootObj = rAParse.UnreserveMSISDNPopulate(msisdnReservationId);

                apiUrl = String.Format(DeleteAPICollection.UnreserveMSISDN);

                logObj.req_blob = _blJson.GetGenericJsonData(reqRootObj);
                logObj.req_time = DateTime.Now;

                object dbssResp = await _apiReq.HttpDeleteRequest(reqRootObj, apiUrl, "UnreserveMSISDNStarTrek");

                logObj.res_blob = _blJson.GetGenericJsonData(dbssResp);
                logObj.res_time = DateTime.Now;
                txtResp = apiUrl + "//" + Convert.ToString(dbssResp);

                if (dbssResp != null)
                {
                    logObj.is_success = 1;

                    var dbssRespModel = JsonConvert.DeserializeObject<ReserverMSISDNResponseRootobject>(dbssResp.ToString() ?? "");

                    if (dbssRespModel != null && dbssRespModel.data != null)
                    {
                        if (dbssRespModel.data.status == 200)
                        {
                            resp.result = true;
                            resp.message = "MSISDN unreserved successfully.";
                        }
                        else
                        {
                            resp.result = false;
                            resp.message = "MSISDN unreservation failed!";
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                logObj.res_time = DateTime.Now;

                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                logObj.res_blob = _blJson.GetGenericJsonData(error);
                logObj.is_success = 0;
                logObj.error_code = error.error_code ?? String.Empty;
                logObj.error_source = error.error_source ?? String.Empty;
                logObj.message = error.error_custom_msg ?? error.error_description;

                resp.result = false;
                resp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
            }
            finally
            {
                logObj.msisdn = msisdn;
                logObj.bi_token_number = bi_token_number;
                logObj.dbss_request_id = bio_request_id;

                logObj.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                logObj.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                logObj.user_id = userId;
                logObj.method_name = "UnreserveMSISDNStarTrek";

                await _bllLog.RAToDBSSLog(logObj);
            }
            return resp;
        }


        #endregion
        #region MSISDN Reservation
        public async Task<MSISDNReservationResponse> MSISDNReservation(BiomerticDataModel item)
        {
            BiometricPopulateModel pltObj = new BiometricPopulateModel();
            MSISDNReservationResponse response = new MSISDNReservationResponse();
            BL_Json byteArrayConverter = new BL_Json();

            BIAToDBSSLog log = new BIAToDBSSLog();
            //bool res = false;
            MSISDNReservation msisdnRes = new MSISDNReservation();
            string meathodUrl = "/api/v1/msisdn-reservations";
            object reservationResponse = new object();
            //log.status = item.status;
            DateTime reqTime = DateTime.Now;
            DateTime resTime = DateTime.Now;
            try
            {
                msisdnRes = pltObj.PopulateMSISDNReservationReqModel(item.msisdn);

                log.req_time = DateTime.Now;
                //log.req_string = JsonConvert.SerializeObject(msisdnRes).ToString();
                log.req_blob = byteArrayConverter.GetGenericJsonData(msisdnRes);
                try
                {
                    log.req_time = DateTime.Now; ;

                    reservationResponse = await genericApiCall.HttpPostRequest(msisdnRes, meathodUrl, "MSISDNReservation");
                    //reservationResponse = @"{data:{reservation-id:22801bd1-619c-4ba0-86ba-158c561118fb,reserve-valid-for:2021-09-02T06:20:20Z}}";
                    log.res_time = DateTime.Now; ;
                }
                catch (Exception ex)
                {
                    response.Error_message = "DBSS: Reservation " + ex.Message;
                    throw;
                }
                //log.res_time = DateTime.Now;

                //log.res_string = JsonConvert.SerializeObject(reservationResponse.ToString()).ToString();
                log.res_blob = byteArrayConverter.GetGenericJsonData(reservationResponse);

                try
                {
                    JObject dbssRespObj = JObject.Parse(reservationResponse.ToString() ?? "");
                    if (dbssRespObj.ContainsKey("data"))
                        response.Reservation_Id = dbssRespObj["data"]?["reservation-id"]?.ToString() ?? "";
                    if (response.Reservation_Id == null) throw new Exception("DBSS: MSISDN Reservation Id not found.");
                }
                catch (Exception)
                { throw new Exception("DBSS: MSISDN Reservation Api Response Parsing Error."); }

                await _bllObj.UpdateBioDbForReservation(item.bi_token_number, response.Reservation_Id);

                log.is_success = 1;
                response.IsReserve = true;
                return response;

            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                ErrorDescription error = await _manageExecption.ManageException(ex, ex.HResult, "BIA");
                //log.res_time = DateTime.Now;
                log.req_time = reqTime;
                log.res_time = resTime;
                //log.res_string = JsonConvert.SerializeObject(reservationResponse != null ? reservationResponse : ex.Message).ToString();
                log.res_blob = byteArrayConverter.GetGenericJsonData(reservationResponse != null ? reservationResponse : ex.Message);
                log.message = error.error_custom_msg ?? error.error_description;
                log.error_code = error.error_code ?? "";
                log.error_source = error.error_source ?? "BIA";
                log.is_success = 0;
                // BIRequset Table Update Status 150 and Error id and description for biometric Failuer
                item.status = 150;
                item.error_id = error.error_id;
                item.error_description = error.error_description ?? ex.InnerException?.Message ?? ex.Message;
                response.Error_message = item.error_description;
                //bllObj.UpdateStatusandErrorMessage(item.bi_token_number, item.status, item.error_id, item.error_description);
                return response;
            }
            finally
            {
                //log.bss_request_id = item.bss_request_id;
                log.bi_token_number = item.bi_token_number;
                log.msisdn = item.msisdn;
                log.user_id = item.user_id;
                log.integration_point_from = (int)IntegrationPoints.BI;
                log.integration_point_to = (int)IntegrationPoints.BSS;
                log.method_name = "MSISDNReservation";
                log.purpose_number = item.purpose_number.ToString();

                await _bllLog.RAToDBSSLog(log);
            }

        }
        #endregion

    }
}
