using BIA.BLL.BLLServices;
using BIA.BLL.Utility;
using BIA.Entity.Collections;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.Interfaces;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Entity.Utility;
using Serilog;
using System.Text;
using System.Xml.Linq;

namespace BIA.Common
{
    public class ApiManager
    {
        private readonly BLLCommon _bLLCommon;
        private readonly BLLUserAuthenticaion _bua;
        private readonly BllOrderBssService _bssService;
        private readonly ApiRequest _apiRequest;
        private readonly BL_Json _blJson;
        private readonly BLLLog _bllLog;

        public ApiManager(BLLCommon bLLCommon, BLLUserAuthenticaion bua, BllOrderBssService bssService, ApiRequest apiRequest, BL_Json blJson, BLLLog bllLog)
        {
            _bLLCommon = bLLCommon;
            _bua = bua;
            _bssService = bssService;
            _apiRequest = apiRequest;
            _blJson = blJson;
            _bllLog = bllLog;
        }
        internal async Task<bool> ValidUserBySecurityToken(string securityToken)
        {
            bool result = false;
            try
            {
                if (!_bLLCommon.CheckSecurityTokenFormat(securityToken))
                {
                    return false;
                }
                string decriptedSecurityToken = Cryptography.Decrypt(securityToken, true);
                string prov_id = _bLLCommon.GetDataFromSecurityToken(decriptedSecurityToken, (int)EnumSecurityTokenPropertyIndex.prov_id);
                result = await _bua.IsSecurityTokenValid(prov_id, _bLLCommon.GetDataFromSecurityToken(decriptedSecurityToken, (int)EnumSecurityTokenPropertyIndex.deviceId));

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal async Task<bool> ValidUserBySecurityTokenV2(string securityToken)
        {
            bool result = false;
            try
            {
                string decriptedSecurityToken = string.Empty;
                string decriptedSecurityTokenMD5 = string.Empty;
                try
                {
                    decriptedSecurityToken = AESCryptography.Decrypt(securityToken);
                    if (decriptedSecurityToken.Equals("InvalidSessionToken"))
                    {
                        decriptedSecurityToken = string.Empty;
                        decriptedSecurityTokenMD5 = Cryptography.Decrypt(securityToken, true);
                    }
                }
                catch (Exception)
                {
                    try
                    {
                        decriptedSecurityTokenMD5 = Cryptography.Decrypt(securityToken, true);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }

                if (!String.IsNullOrEmpty(decriptedSecurityTokenMD5))
                {
                    if (!_bLLCommon.CheckSecurityTokenFormatV3(decriptedSecurityTokenMD5))
                    {
                        return false;
                    }
                    result = await validateSequrityTokenForMD5(decriptedSecurityTokenMD5);
                }
                else
                {
                    if (!_bLLCommon.CheckSecurityTokenFormatV2(decriptedSecurityToken))
                    {
                        return false;
                    }
                    result = await validateSequrityTokenForAES(decriptedSecurityToken);
                }

                return result;

            }
            catch (Exception)
            {
                throw;
            }
        }
        internal async Task<bool> validateSequrityTokenForAES(string decriptedSecurityToken)
        {
            bool result = false;
            try
            {
                string prov_id = _bLLCommon.GetDataFromSecurityTokenV2(decriptedSecurityToken, (int)EnumSecurityTokenPropertyIndex.prov_id);
                result = await _bua.IsSecurityTokenValid2(prov_id, _bLLCommon.GetDataFromSecurityTokenV2(decriptedSecurityToken, (int)EnumSecurityTokenPropertyIndex.deviceId));
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal async Task<bool> validateSequrityTokenForMD5(string decriptedSecurityTokenMD5)
        {
            bool result = false;
            try
            {
                string prov_id = _bLLCommon.GetDataFromSecurityTokenV3(decriptedSecurityTokenMD5, (int)EnumSecurityTokenPropertyIndex.prov_id);
                result = await _bua.IsSecurityTokenValid2(prov_id, _bLLCommon.GetDataFromSecurityTokenV3(decriptedSecurityTokenMD5, (int)EnumSecurityTokenPropertyIndex.deviceId));
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
        internal async Task<bool> validateSequrityTokenForAESForBP(string decriptedSecurityToken)
        {
            bool result = false;
            try
            {
                string prov_id = _bLLCommon.GetDataFromSecurityTokenV2(decriptedSecurityToken, (int)EnumSecurityTokenPropertyIndex.prov_id);
                result = await _bua.IsSecurityTokenValidForBPLogin(prov_id, _bLLCommon.GetDataFromSecurityTokenV2(decriptedSecurityToken, (int)EnumSecurityTokenPropertyIndex.deviceId));

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
        internal async Task<bool> validateSequrityTokenForMD5ForBP(string decriptedSecurityTokenMD5)
        {
            bool result = false;
            try
            {
                string prov_id = _bLLCommon.GetDataFromSecurityTokenV3(decriptedSecurityTokenMD5, (int)EnumSecurityTokenPropertyIndex.prov_id);
                result = await _bua.IsSecurityTokenValidForBPLogin(prov_id, _bLLCommon.GetDataFromSecurityTokenV3(decriptedSecurityTokenMD5, (int)EnumSecurityTokenPropertyIndex.deviceId));
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal async Task<bool> ValidUserBySecurityTokenForBPLogin(string securityToken)
        {
            bool result = false;
            try
            {
                string tokenAes = string.Empty;
                string tokenMd5 = string.Empty;
                try
                {
                    tokenAes = AESCryptography.Decrypt(securityToken);
                    if (tokenAes.Equals("InvalidSessionToken"))
                    {
                        tokenAes = string.Empty;
                        tokenMd5 = Cryptography.Decrypt(securityToken, true);
                    }

                }
                catch (Exception)
                {
                    try
                    {
                        tokenMd5 = Cryptography.Decrypt(securityToken, true);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }

                if (!String.IsNullOrEmpty(tokenMd5))
                {
                    if (!_bLLCommon.CheckSecurityTokenFormatV3(tokenMd5))
                    {
                        return false;
                    }
                    result = await validateSequrityTokenForMD5ForBP(tokenMd5); //encrypted
                }
                else
                {
                    if (!_bLLCommon.CheckSecurityTokenFormatV2(tokenAes))
                    {
                        return false;
                    }
                    result = await validateSequrityTokenForAESForBP(tokenAes); //encrypted
                }

                return result;

            }
            catch (Exception)
            {
                throw;
            }
        }

        //===============DBSSLogin================
        internal async Task<bool> ValidUserBySecurityTokenForDBSS(string securityToken)
        {
            bool result = false;
            try
            {
                string prov_id = _bLLCommon.GetDataFromSecurityToken(Cryptography.Decrypt(securityToken, true), (int)EnumSecurityTokenPropertyIndex.prov_id);
                result = await _bua.IsSecurityTokenValidForDBSS(prov_id);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal async Task<bool> ValidUserBySecurityTokenForDBSSV2(string securityToken)
        {
            bool result = false;
            string prov_id = string.Empty;
            try
            {
                string decriptedSecurityToken = string.Empty;
                string decriptedSecurityTokenMD5 = string.Empty;
                try
                {
                    decriptedSecurityToken = AESCryptography.Decrypt(securityToken);
                    if (decriptedSecurityToken.Equals("InvalidSessionToken"))
                    {
                        decriptedSecurityToken = string.Empty;
                        decriptedSecurityTokenMD5 = Cryptography.Decrypt(securityToken, true);
                    }

                }
                catch (Exception)
                {
                    try
                    {
                        decriptedSecurityTokenMD5 = Cryptography.Decrypt(securityToken, true);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                if (!String.IsNullOrEmpty(decriptedSecurityTokenMD5))
                {
                    prov_id = _bLLCommon.GetDataFromSecurityToken(decriptedSecurityTokenMD5, (int)EnumSecurityTokenPropertyIndex.prov_id);
                    result = await _bua.IsSecurityTokenValidForDBSS(prov_id);
                    return result;
                }
                else
                {
                    prov_id = _bLLCommon.GetDataFromSecurityToken(decriptedSecurityToken, (int)EnumSecurityTokenPropertyIndex.prov_id);
                    result = await _bua.IsSecurityTokenValidForDBSS(prov_id);
                    return result;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        //=========x=================

        private static string getLoginProvider(string token)
        {
            return token.Substring(0, token.IndexOf(",uid:"));
        }




        internal async Task<bool> ValidUserBySecurityToken_Test(string securityToken)
        {
            bool result = false;
            try
            {
                if (!_bLLCommon.CheckSecurityTokenFormat(securityToken))
                {
                    return false;
                }

                string prov_id = _bLLCommon.GetDataFromSecurityToken(securityToken, (int)EnumSecurityTokenPropertyIndex.prov_id);
                result = await _bua.IsSecurityTokenValid2(prov_id, _bLLCommon.GetDataFromSecurityToken(securityToken, (int)EnumSecurityTokenPropertyIndex.deviceId));

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
        internal async Task<string> GetBtsInfoByLacCid(int lac, int cid)
        {
            string BtsCode = string.Empty;
            try
            {
                BtsCode = await _bssService.GetBTSInfoByLacCid(lac, cid);
            }
            catch (Exception)
            {
                throw;
            }
            return BtsCode;
        }

        internal async Task<ICCDetailsResponse> CheckICCfromDMS(ICCDetailsRequestModel model)
        {
            var iCCDetails = new ICCDetailsResponse();
            DMSICCCheckResponse response = new DMSICCCheckResponse();
            BIAToDBSSLog log = new BIAToDBSSLog();
            DMSICCCheckRequest request = new DMSICCCheckRequest();
            string dmsSessionToken = string.Empty;
            try
            {
                string retCode = string.Empty;
                string sim_number = string.Empty;

                if (!model.retailer_id.StartsWith("R", StringComparison.OrdinalIgnoreCase))
                {
                    retCode = "R" + model.retailer_id;
                }
                else
                {
                    retCode = model.retailer_id;
                }

                if (model.icc.Substring(0, FixedValueCollection.SIMCode.Length) != FixedValueCollection.SIMCode)
                {
                    model.icc = FixedValueCollection.SIMCode + model.icc;
                }
                request.retailerCode = retCode;
                request.serialNo = model.icc;

                log.req_blob = _blJson.GetGenericJsonData(request);
                log.req_time = DateTime.Now;

                dmsSessionToken = await GetOrRefreshDMSSession();

                response = await _apiRequest.HttpPostRequestDMSICCCheck(
                    request,
                    ICCCheckDMS.ICCCheckAPI,
                    "ValidatePairedMSISDNV4",
                    dmsSessionToken
                );

                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(response);

                var iccData = response?.Data?.FirstOrDefault();
                
                if (iccData != null && iccData.Description.StartsWith("Error"))
                {
                    string msg = iccData.Description;

                    int dashIndex = msg.IndexOf('|');

                    if (dashIndex > -1 && dashIndex + 1 < msg.Length)
                        msg = msg.Substring(dashIndex + 1).Trim(); // Only text after "Error-"

                    iCCDetails.result = false;
                    iCCDetails.message = msg;
                }
                else if (response != null && response.Status != 200)
                {
                    iCCDetails.result = false;
                    iCCDetails.message = response.Message;
                }
                else if (response != null && response.Status == 200 && iccData != null)
                {
                    iCCDetails.result = true;
                    iCCDetails.message = iccData.Description;
                    iCCDetails.offer_description = iccData.Description;
                    iCCDetails.product_name = iccData.ProductCode;
                    iCCDetails.offer_name = iccData.OfferName;
                }
                else
                {
                    iCCDetails.result = false;
                    iCCDetails.message = "The distributor is not eligible for sale this offer!";
                }
                return iCCDetails;
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

                iCCDetails.result = false;
                iCCDetails.message = response.Message;
                iCCDetails.offer_description = "";
                iCCDetails.product_name = "";
                iCCDetails.offer_name = "";
                return iCCDetails;
            }
            finally
            {
                if (model.retailer_id.StartsWith("R", StringComparison.OrdinalIgnoreCase))
                {
                    model.retailer_id = model.retailer_id.Substring(1);
                }
               
                log.msisdn = _bllLog.FormatMSISDN(model.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = model.purpose_number;
                log.user_id = model.retailer_id;
                log.message = Encoding.UTF8.GetString(log.res_blob);
                log.method_name = "CheckICCfromDMS";
                await _bllLog.RAToDBSSLog(log);
            }            
        }

        private async Task<string> GetOrRefreshDMSSession()
        {
            var session = await _bLLCommon.GetDMSSessionValues();
            if (session != null)
            {
                var diff = DateTime.Now - session.CREATE_DATE;
                if (diff.TotalMinutes < session.SESSIONTIME)
                    return session.SESSIONTOKEN;
            }

            var loginUrl = ICCCheckDMS.LoginAPI;
            var request = new DMSLoginRequest
            {
                userName = SettingsValues.GetIccCheckUserName(),
                password = SettingsValues.GetIccCheckPassword()
            };

            var response = await _apiRequest.HttpPostRequestDMSLogin(request, loginUrl, "ValidatePairedMSISDNV4");
            if (response?.Status == 200 && !string.IsNullOrEmpty(response.Data?.AccessToken))
            {
                await _bLLCommon.SaveDMSSession(response);
                return response.Data.AccessToken;
            }

            throw new Exception("DMS Login Failed");
        }
    }
}
