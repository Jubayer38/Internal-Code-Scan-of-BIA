using BIA.BLL.Utility;
using BIA.DAL.Repositories;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Entity.Utility;
using BIA.Entity.ViewModel;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BIA.BLL.BLLServices
{
    public class BLLCommon
    {
        private readonly DALBiometricRepo dataManager;
        private readonly BLLUserAuthenticaion _bLLUserAuthenticaion;

        public BLLCommon(DALBiometricRepo _dataManager, BLLUserAuthenticaion bLLUserAuthenticaion)
        {
            dataManager = _dataManager;
            _bLLUserAuthenticaion = bLLUserAuthenticaion;
        }
        public async Task<bool> IsStockAvailable(int stock_id, int channel_id)
        {
            var data = await dataManager.IsStockAvailable(stock_id, channel_id);

            return Convert.ToInt32(data.ToString()) == 1 ? true : false;
        }

        public async Task<ActivityLogResponse> GetActivityLogData(int activity_type_id, string user_id)
        {
            ActivityLogResponse response = new ActivityLogResponse();
            try
            {
                var dataRow = await dataManager.GetActivityLogData(activity_type_id, user_id);

                if (dataRow.Rows.Count > 0)
                {
                    List<VMActivityLog> alrs = new List<VMActivityLog>();
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {
                        VMActivityLog alr = new VMActivityLog();
                        alr.token_id = Convert.ToString(dataRow.Rows[i]["BI_TOKEN_NUMBER"] == DBNull.Value ? "" : dataRow.Rows[i]["BI_TOKEN_NUMBER"]) ?? "";
                        alr.time = Convert.ToString(dataRow.Rows[i]["CREATE_DATE"] == DBNull.Value ? "" : dataRow.Rows[i]["CREATE_DATE"]) ?? "";
                        string msisdn = Convert.ToString(dataRow.Rows[i]["MSISDN"] == DBNull.Value ? "" : dataRow.Rows[i]["MSISDN"]) ?? "";
                        if (!String.IsNullOrEmpty(msisdn))
                            alr.mobile_number = msisdn.Substring(0, 2) == FixedValueCollection.MSISDNCountryCode ? msisdn.Remove(0, 2) : msisdn;
                        alr.nid = Convert.ToString(dataRow.Rows[i]["DEST_DOC_ID"] == DBNull.Value ? "" : dataRow.Rows[i]["DEST_DOC_ID"]) ?? "";
                        alr.dob = Convert.ToString(dataRow.Rows[i]["DEST_DOB"] == DBNull.Value ? "" : dataRow.Rows[i]["DEST_DOB"]) ?? "";
                        alr.type = Convert.ToString(dataRow.Rows[i]["ACCU_TYPE"] == DBNull.Value ? "" : dataRow.Rows[i]["ACCU_TYPE"]) ?? "";

                        string statusName = Convert.ToString(dataRow.Rows[i]["STATUS_NAME"] == DBNull.Value ? "" : dataRow.Rows[i]["STATUS_NAME"]) ?? "";
                        string errDescription = Convert.ToString(dataRow.Rows[i]["ERROR_DESCRIPTION"] == DBNull.Value ? "" : dataRow.Rows[i]["ERROR_DESCRIPTION"]) ?? "";
                        int isStatusNameNotAdd = Convert.ToInt32(dataRow.Rows[i]["IS_NOT_ADDED_STATUS"] == DBNull.Value ? null : dataRow.Rows[i]["IS_NOT_ADDED_STATUS"]);

                        if (!String.IsNullOrEmpty(statusName)
                           && statusName.Contains("Failed")
                           && !String.IsNullOrEmpty(errDescription))
                        {
                            if (isStatusNameNotAdd == 1)
                            {
                                alr.status = errDescription;
                            }
                            else
                            {
                                alr.status = statusName + ", " + errDescription;
                            }
                        }
                        else
                        {
                            alr.status = statusName;
                        }

                        alr.is_re_submittable = Convert.ToInt32(dataRow.Rows[i]["IS_RE_SUBMITTABLE"] == DBNull.Value ? null : dataRow.Rows[i]["IS_RE_SUBMITTABLE"]);
                        alr.re_submit_error_message = Convert.ToString(dataRow.Rows[i]["RE_SUBMIT_ERROR_MESSAGE"] == DBNull.Value ? "" : dataRow.Rows[i]["RE_SUBMIT_ERROR_MESSAGE"]) ?? "";
                        alr.re_submit_expire_time = Convert.ToInt32(dataRow.Rows[i]["ACTIVITYLOGEXPIRTIME"] == DBNull.Value ? null : dataRow.Rows[i]["ACTIVITYLOGEXPIRTIME"]);
                        alr.right_id = Convert.ToInt32(dataRow.Rows[i]["RIGHT_ID"] == DBNull.Value ? null : dataRow.Rows[i]["RIGHT_ID"]);

                        alrs.Add(alr);
                    }

                    response.data = alrs;
                    response.result = true;
                    response.message = MessageCollection.Success;
                    return response;
                }
                else
                {
                    response.data = new List<VMActivityLog>();
                    response.result = false;
                    response.message = MessageCollection.NoDataFound;
                    return response;
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ActivityLogResponse> GetActivityLogDataV2(int activity_type_id, string user_id)
        {
            ActivityLogResponse response = new ActivityLogResponse();
            try
            {
                var dataRow = await dataManager.GetActivityLogDataV2(activity_type_id, user_id);

                if (dataRow.Rows.Count > 0)
                {
                    List<VMActivityLog> alrs = new List<VMActivityLog>();
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {
                        VMActivityLog alr = new VMActivityLog();
                        alr.token_id = Convert.ToString(dataRow.Rows[i]["BI_TOKEN_NUMBER"] == DBNull.Value ? "" : dataRow.Rows[i]["BI_TOKEN_NUMBER"]) ?? "";
                        alr.time = Convert.ToString(dataRow.Rows[i]["CREATE_DATE"] == DBNull.Value ? "" : dataRow.Rows[i]["CREATE_DATE"]) ?? "";
                        string msisdn = Convert.ToString(dataRow.Rows[i]["MSISDN"] == DBNull.Value ? "" : dataRow.Rows[i]["MSISDN"]) ?? "";
                        if (!String.IsNullOrEmpty(msisdn))
                            alr.mobile_number = msisdn.Substring(0, 2) == FixedValueCollection.MSISDNCountryCode ? msisdn.Remove(0, 2) : msisdn;
                        alr.nid = Convert.ToString(dataRow.Rows[i]["DEST_DOC_ID"] == DBNull.Value ? "" : dataRow.Rows[i]["DEST_DOC_ID"]) ?? "";
                        alr.dob = Convert.ToString(dataRow.Rows[i]["DEST_DOB"] == DBNull.Value ? "" : dataRow.Rows[i]["DEST_DOB"]) ?? "";
                        alr.type = Convert.ToString(dataRow.Rows[i]["ACCU_TYPE"] == DBNull.Value ? "" : dataRow.Rows[i]["ACCU_TYPE"]) ?? "";

                        string statusName = Convert.ToString(dataRow.Rows[i]["STATUS_NAME"] == DBNull.Value ? "" : dataRow.Rows[i]["STATUS_NAME"]) ?? "";
                        string errDescription = Convert.ToString(dataRow.Rows[i]["ERROR_DESCRIPTION"] == DBNull.Value ? "" : dataRow.Rows[i]["ERROR_DESCRIPTION"]) ?? "";

                        int isStatusNameNotAdd = Convert.ToInt32(dataRow.Rows[i]["IS_NOT_ADDED_STATUS"] == DBNull.Value ? null : dataRow.Rows[i]["IS_NOT_ADDED_STATUS"]);

                        if (!String.IsNullOrEmpty(statusName)
                            && statusName.Contains("Failed")
                            && !String.IsNullOrEmpty(errDescription))
                        {
                            if (isStatusNameNotAdd == 1)
                            {
                                alr.status = errDescription;
                            }
                            else
                            {
                                alr.status = statusName + ", " + errDescription;
                            }
                        }
                        else
                        {
                            alr.status = statusName;
                        }

                        alr.is_re_submittable = Convert.ToInt32(dataRow.Rows[i]["IS_RE_SUBMITTABLE"] == DBNull.Value ? null : dataRow.Rows[i]["IS_RE_SUBMITTABLE"]);
                        alr.re_submit_error_message = Convert.ToString(dataRow.Rows[i]["RE_SUBMIT_ERROR_MESSAGE"] == DBNull.Value ? "" : dataRow.Rows[i]["RE_SUBMIT_ERROR_MESSAGE"]) ?? "";
                        alr.re_submit_expire_time = Convert.ToInt32(dataRow.Rows[i]["ACTIVITYLOGEXPIRTIME"] == DBNull.Value ? null : dataRow.Rows[i]["ACTIVITYLOGEXPIRTIME"]);
                        alr.right_id = Convert.ToInt32(dataRow.Rows[i]["RIGHT_ID"] == DBNull.Value ? null : dataRow.Rows[i]["RIGHT_ID"]);

                        alr.is_bp_user = Convert.ToString(dataRow.Rows[i]["IS_BP"] == DBNull.Value ? "" : dataRow.Rows[i]["IS_BP"]) ?? "";
                        alr.bp_msisdn = Convert.ToString(dataRow.Rows[i]["BP_MSISDN"] == DBNull.Value ? "" : dataRow.Rows[i]["BP_MSISDN"]) ?? "";

                        alrs.Add(alr);
                    }

                    response.data = alrs;
                    response.result = true;
                    response.message = MessageCollection.Success;
                    return response;
                }
                else
                {
                    response.data = new List<VMActivityLog>();
                    response.result = false;
                    response.message = MessageCollection.NoDataFound;
                    return response;
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ActivityLogResponseRevamp> GetActivityLogDataV3(int activity_type_id, string user_id)
        {
            ActivityLogResponseRevamp response = new ActivityLogResponseRevamp();

            int isFtrFeatureOn = SettingsValues.GetisFtrFeatureOn();

            try
            {
                var dataRow = await dataManager.GetActivityLogDataV3(activity_type_id, user_id);

                if (dataRow.Rows.Count > 0)
                {
                    List<VMActivityLogRevamp> alrs = new List<VMActivityLogRevamp>();
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {
                        VMActivityLogRevamp alr = new VMActivityLogRevamp();
                        alr.token_id = Convert.ToString(dataRow.Rows[i]["BI_TOKEN_NUMBER"] == DBNull.Value ? "" : dataRow.Rows[i]["BI_TOKEN_NUMBER"]) ?? "";
                        alr.time = Convert.ToString(dataRow.Rows[i]["CREATE_DATE"] == DBNull.Value ? "" : dataRow.Rows[i]["CREATE_DATE"]) ?? "";
                        string msisdn = Convert.ToString(dataRow.Rows[i]["MSISDN"] == DBNull.Value ? "" : dataRow.Rows[i]["MSISDN"]) ?? "";
                        if (!String.IsNullOrEmpty(msisdn))
                            alr.mobile_number = msisdn.Substring(0, 2) == FixedValueCollection.MSISDNCountryCode ? msisdn.Remove(0, 2) : msisdn;
                        alr.nid = Convert.ToString(dataRow.Rows[i]["DEST_DOC_ID"] == DBNull.Value ? "" : dataRow.Rows[i]["DEST_DOC_ID"]) ?? "";
                        alr.dob = Convert.ToString(dataRow.Rows[i]["DEST_DOB"] == DBNull.Value ? "" : dataRow.Rows[i]["DEST_DOB"]) ?? "";
                        alr.type = Convert.ToString(dataRow.Rows[i]["ACCU_TYPE"] == DBNull.Value ? "" : dataRow.Rows[i]["ACCU_TYPE"]) ?? "";

                        string statusName = Convert.ToString(dataRow.Rows[i]["STATUS_NAME"] == DBNull.Value ? "" : dataRow.Rows[i]["STATUS_NAME"]) ?? "";
                        string errDescription = Convert.ToString(dataRow.Rows[i]["ERROR_DESCRIPTION"] == DBNull.Value ? "" : dataRow.Rows[i]["ERROR_DESCRIPTION"]) ?? "";

                        int isStatusNameNotAdd = Convert.ToInt32(dataRow.Rows[i]["IS_NOT_ADDED_STATUS"] == DBNull.Value ? null : dataRow.Rows[i]["IS_NOT_ADDED_STATUS"]);

                        if (!String.IsNullOrEmpty(statusName)
                            && statusName.Contains("Failed")
                            && !String.IsNullOrEmpty(errDescription))
                        {
                            if (isStatusNameNotAdd == 1)
                            {
                                alr.status = errDescription;
                            }
                            else
                            {
                                alr.status = statusName + ", " + errDescription;
                            }
                        }
                        else
                        {
                            alr.status = statusName;
                        }

                        alr.is_re_submittable = Convert.ToInt32(dataRow.Rows[i]["IS_RE_SUBMITTABLE"] == DBNull.Value ? null : dataRow.Rows[i]["IS_RE_SUBMITTABLE"]);
                        alr.re_submit_error_message = Convert.ToString(dataRow.Rows[i]["RE_SUBMIT_ERROR_MESSAGE"] == DBNull.Value ? "" : dataRow.Rows[i]["RE_SUBMIT_ERROR_MESSAGE"]) ?? "";
                        alr.re_submit_expire_time = Convert.ToInt32(dataRow.Rows[i]["ACTIVITYLOGEXPIRTIME"] == DBNull.Value ? null : dataRow.Rows[i]["ACTIVITYLOGEXPIRTIME"]);
                        alr.right_id = Convert.ToInt32(dataRow.Rows[i]["RIGHT_ID"] == DBNull.Value ? null : dataRow.Rows[i]["RIGHT_ID"]);

                        alr.is_bp_user = Convert.ToString(dataRow.Rows[i]["IS_ARRANGED"] == DBNull.Value ? "" : dataRow.Rows[i]["IS_ARRANGED"]) ?? "";
                        alr.bp_msisdn = Convert.ToString(dataRow.Rows[i]["BP_MSISDN"] == DBNull.Value ? "" : dataRow.Rows[i]["BP_MSISDN"]) ?? "";
                        alr.designation = Convert.ToString(dataRow.Rows[i]["DESIGNATION"] == DBNull.Value ? "" : dataRow.Rows[i]["DESIGNATION"]) ?? "";
                        alr.action_point = Convert.ToString(dataRow.Rows[i]["ACTION_POINT"] == DBNull.Value ? "" : dataRow.Rows[i]["ACTION_POINT"]) ?? "";
                        if (!String.IsNullOrEmpty(alr.action_point))
                        {
                            if (Convert.ToChar(alr.action_point.Substring(0, 1)) == ',')
                            {
                                alr.action_point = alr.action_point.Substring(1);
                            }
                        }
                        if (isFtrFeatureOn == 1)
                        {
                            alr.recharge_status = Convert.ToString(dataRow.Rows[i]["RECHARGE_MESSAGE"] == DBNull.Value ? "" : dataRow.Rows[i]["RECHARGE_MESSAGE"]) ?? "";
                            alr.is_recharge_done = Convert.ToInt32(dataRow.Rows[i]["ISRECHARGE_DONE"] == DBNull.Value ? null : dataRow.Rows[i]["ISRECHARGE_DONE"]);
                        }

                        alrs.Add(alr);
                    }

                    response.data = alrs;
                    response.isError = false;
                    response.message = MessageCollection.Success;
                    return response;
                }
                else
                {
                    response.data = new List<VMActivityLogRevamp>();
                    response.isError = true;
                    response.message = MessageCollection.NoDataFound;
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<PurposeNumberReponse> GetPurposeNumbers(RAGetPurposeRequest model)
        {
            List<PurposeNumberReponseData> pns = new List<PurposeNumberReponseData>();
            PurposeNumberReponse pnRes = new PurposeNumberReponse();
            try
            {

                var dataRow = await dataManager.GetPurposeNumbers(model);

                if (dataRow.Rows.Count > 0)
                {
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {

                        PurposeNumberReponseData pn = new PurposeNumberReponseData();
                        pn.purpose_id = Convert.ToInt32(dataRow.Rows[i]["PURPOSE_ID"] == DBNull.Value ? null : dataRow.Rows[i]["PURPOSE_ID"]);
                        pn.purpose_name = Convert.ToString(dataRow.Rows[i]["PURPOSE_NAME"] == DBNull.Value ? "" : dataRow.Rows[i]["PURPOSE_NAME"]) ?? "";
                        pns.Add(pn);
                    }

                    pnRes.data = pns;
                    pnRes.result = true;
                    pnRes.message = MessageCollection.Success;
                }
                else
                {
                    pnRes.data = pns;
                    pnRes.result = false;
                    pnRes.message = MessageCollection.NoDataFound;
                }

            }
            catch (Exception)
            {
                throw;
            }
            return pnRes;
        }

        public async Task<PurposeNumberReponseRev> GetPurposeNumbersV2(RAGetPurposeRequest model)
        {
            List<PurposeNumberReponseDataRev> pns = new List<PurposeNumberReponseDataRev>();
            PurposeNumberReponseRev pnRes = new PurposeNumberReponseRev();
            try
            {
                var dataRow = await dataManager.GetPurposeNumbers(model);

                if (dataRow.Rows.Count > 0)
                {
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {

                        PurposeNumberReponseDataRev pn = new PurposeNumberReponseDataRev();
                        pn.purpose_id = Convert.ToInt32(dataRow.Rows[i]["PURPOSE_ID"] == DBNull.Value ? null : dataRow.Rows[i]["PURPOSE_ID"]);
                        pn.purpose_name = Convert.ToString(dataRow.Rows[i]["PURPOSE_NAME"] == DBNull.Value ? "" : dataRow.Rows[i]["PURPOSE_NAME"]) ?? "";
                        pns.Add(pn);
                    }

                    pnRes.data = pns;
                    pnRes.isError = false;
                    pnRes.message = MessageCollection.Success;
                }
                else
                {
                    pnRes.data = pns;
                    pnRes.isError = true;
                    pnRes.message = MessageCollection.NoDataFound;
                }

            }
            catch (Exception)
            {
                throw;
            }
            return pnRes;
        }

        public async Task<long> GetTokenNo(string mssisdn)
        {
            long result = 0;
            try
            {
                result = Convert.ToInt32(mssisdn);
                result = await dataManager.GetTokenNo(mssisdn);

            }
            catch (Exception)
            {
                throw;
            }
            return result;
        }

        #region Get Distributor Code From Session Token

        public string GetDistributorCodeFromSessionToken(string sessiontoken)
        {
            string distCode = string.Empty;
            try
            {
                string decryptedToken = Cryptography.Decrypt(sessiontoken, true);
                string[] tokenProperties = new string[] { ",uid:", ",uname:", ",dc:" };
                var splitedData = decryptedToken.Split(tokenProperties, StringSplitOptions.None);
                if (splitedData.Count() > 1)
                {
                    for (int i = 0; i < splitedData.Count(); i++)
                    {
                        if (i == 2)
                        {
                            distCode = splitedData[i];
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return distCode;

            //string decryptedToken = Cryptography.Decrypt(sessiontoken, true);
            //int indexOfKey = decryptedToken.IndexOf(",dc:");
            //string distCode = decryptedToken.Substring(indexOfKey + ",dc:".Length, decryptedToken.Length - (indexOfKey + ",dc:".Length));
        }

        public async Task<string> GetDistributorCodeFromSessionTokenV2(string sessiontoken, string userName)
        {
            string distCode = string.Empty;
            try
            {
                int isEligible = Convert.ToInt32(SettingsValues.GetIsEligibleAES());

                if (isEligible == 1)
                {
                    bool isEligibleUser = await _bLLUserAuthenticaion.IsAESEligibleUser(userName);
                    if (isEligibleUser == true)
                    {
                        distCode = GetDistCodeFromSesTokenForAES(sessiontoken);
                        return distCode;
                    }
                    else
                    {
                        distCode = GetDistCodeFromSesTokenForMD5(sessiontoken);
                        return distCode;
                    }
                }
                else
                {
                    distCode = GetDistCodeFromSesTokenForAES(sessiontoken);
                    return distCode;
                }
            }
            catch (Exception)
            {
                throw;
            }

            //string decryptedToken = Cryptography.Decrypt(sessiontoken, true);
            //int indexOfKey = decryptedToken.IndexOf(",dc:");
            //string distCode = decryptedToken.Substring(indexOfKey + ",dc:".Length, decryptedToken.Length - (indexOfKey + ",dc:".Length));
        }

        private string GetDistCodeFromSesTokenForAES(string sessiontoken)
        {
            string distCode = string.Empty;
            try
            {
                string decryptedToken = AESCryptography.Decrypt(sessiontoken);
                string[] tokenProperties = new string[] { ",uid:", ",uname:", ",dc:", ",deviceId:", ",random:" };
                var splitedData = decryptedToken.Split(tokenProperties, StringSplitOptions.None);
                if (splitedData.Count() > 1)
                {
                    for (int i = 0; i < splitedData.Count(); i++)
                    {
                        if (i == 3)
                        {
                            distCode = splitedData[i];
                            break;
                        }
                    }
                }
                return distCode;
            }
            catch (Exception)
            {
                throw;
            }

        }
        private string GetDistCodeFromSesTokenForMD5(string sessiontoken)
        {
            string distCode = string.Empty;
            try
            {
                string decryptedToken = Cryptography.Decrypt(sessiontoken, true);
                string[] tokenProperties = new string[] { ",uid:", ",uname:", ",dc:", ",deviceId:" };
                var splitedData = decryptedToken.Split(tokenProperties, StringSplitOptions.None);
                if (splitedData.Count() > 1)
                {
                    for (int i = 0; i < splitedData.Count(); i++)
                    {
                        if (i == 3)
                        {
                            distCode = splitedData[i];
                            break;
                        }
                    }
                }
                return distCode;
            }
            catch (Exception)
            {
                throw;
            }

        }
        #endregion

        #region Get User Id From Session Token

        public string GetUserIdFromSessionToken(string sessiontoken)
        {
            string userId = string.Empty;
            try
            {
                string decryptedToken = Cryptography.Decrypt(sessiontoken, true);
                string[] tokenProperties = new string[] { ",uid:", ",uname:", ",dc:" };
                var splitedData = decryptedToken.Split(tokenProperties, StringSplitOptions.None);
                if (splitedData.Count() > 1)
                {
                    for (int i = 0; i < splitedData.Count(); i++)
                    {
                        if (i == 1)
                        {
                            userId = splitedData[i];
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return userId;
        }
        #endregion

        #region Get User Name From Session Token
        public string GetUserNameFromSessionToken(string sessiontoken)
        {
            string userName = String.Empty;
            try
            {
                string decryptedToken = Cryptography.Decrypt(sessiontoken, true);
                string[] tokenProperties = new string[] { ",uid:", ",uname:", ",dc:" };
                var splitedData = decryptedToken.Split(tokenProperties, StringSplitOptions.None);
                if (splitedData.Count() > 1)
                {
                    for (int i = 0; i < splitedData.Count(); i++)
                    {
                        if (i == 2)
                        {
                            userName = splitedData[i];
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return userName;
        }
        public string GetUserNameFromSessionTokenV2(string sessiontoken)
        {
            string userName = String.Empty;

            try
            {
                string decriptedSecurityToken = string.Empty;
                string decriptedSecurityTokenMD5 = string.Empty;
                try
                {
                    decriptedSecurityToken = AESCryptography.Decrypt(sessiontoken);
                    if (decriptedSecurityToken.Equals("InvalidSessionToken"))
                    {
                        decriptedSecurityToken = string.Empty;
                        decriptedSecurityTokenMD5 = Cryptography.Decrypt(sessiontoken, true);
                    }
                }
                catch (Exception)
                {
                    try
                    {
                        decriptedSecurityTokenMD5 = Cryptography.Decrypt(sessiontoken, true);
                    }
                    catch (Exception)
                    {
                        return string.Empty;
                    }
                }
                if (!String.IsNullOrEmpty(decriptedSecurityTokenMD5))
                {
                    userName = GetUserNameFromMD5Token(decriptedSecurityTokenMD5);
                }
                else
                {
                    userName = GetUserNameFromAESToken(decriptedSecurityToken);
                }

                return userName;

            }
            catch (Exception)
            {
                throw;
            }
        }
        private string GetUserNameFromAESToken(string sessiontoken)
        {
            string userName = String.Empty;
            try
            {
                string[] tokenProperties = new string[] { ",uid:", ",uname:", ",dc:" };
                var splitedData = sessiontoken.Split(tokenProperties, StringSplitOptions.None);
                if (splitedData.Count() > 1)
                {
                    for (int i = 0; i < splitedData.Count(); i++)
                    {
                        if (i == 2)
                        {
                            userName = splitedData[i];
                            break;
                        }
                    }
                }

                return userName;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private string GetUserNameFromMD5Token(string sessiontoken)
        {
            string userName = String.Empty;
            try
            {
                string[] tokenProperties = new string[] { ",uid:", ",uname:", ",dc:" };
                var splitedData = sessiontoken.Split(tokenProperties, StringSplitOptions.None);
                if (splitedData.Count() > 1)
                {
                    for (int i = 0; i < splitedData.Count(); i++)
                    {
                        if (i == 2)
                        {
                            userName = splitedData[i];
                            break;
                        }
                    }
                }

                return userName;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region Get Device Id From Session Token

        public string GetDeviceIdFromSessionToken(string sessiontoken)
        {
            string deviceId = string.Empty;
            try
            {
                string decryptedToken = Cryptography.Decrypt(sessiontoken, true);
                string[] tokenProperties = new string[] { ",uid:", ",uname:", ",dc:", ",deviceId:" };
                var splitedData = decryptedToken.Split(tokenProperties, StringSplitOptions.None);
                if (splitedData.Count() > 1)
                {
                    for (int i = 0; i < splitedData.Count(); i++)
                    {
                        if (i == 4)
                        {
                            deviceId = splitedData[i];
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return deviceId;
        }
        #endregion

        #region Get Data From Security Token 

        public string GetDataFromSecurityToken(string decryptedSessiontoken, int tonkenPropertyIndex)
        {
            string data = string.Empty;
            try
            {
                //string decryptedToken = Cryptography.Decrypt(decryptedSessiontoken, true);

                string[] tokenProperties = StringFormatCollection.AccessTokenPropertyArray;
                var splitedDataList = decryptedSessiontoken.Split(tokenProperties, StringSplitOptions.None);

                if (tokenProperties.Length <= splitedDataList.Count()
                    && tonkenPropertyIndex <= splitedDataList.Count())
                {
                    for (int i = 0; i < splitedDataList.Count(); i++)
                    {
                        if (i == tonkenPropertyIndex)
                        {
                            data = splitedDataList[i];
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return data;
        }
        public string GetDataFromSecurityTokenV2(string decryptedSessiontoken, int tonkenPropertyIndex)
        {
            string data = string.Empty;
            try
            {
                //string decryptedToken = Cryptography.Decrypt(decryptedSessiontoken, true);

                string[] tokenProperties = StringFormatCollection.AccessTokenPropertyArrayV2;
                var splitedDataList = decryptedSessiontoken.Split(tokenProperties, StringSplitOptions.None);

                if (tokenProperties.Length <= splitedDataList.Count()
                    && tonkenPropertyIndex <= splitedDataList.Count())
                {
                    for (int i = 0; i < splitedDataList.Count(); i++)
                    {
                        if (i == tonkenPropertyIndex)
                        {
                            data = splitedDataList[i];
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return data;
        }

        public string GetDataFromSecurityTokenV3(string decryptedSessiontoken, int tonkenPropertyIndex)
        {
            string data = string.Empty;
            try
            {
                //string decryptedToken = Cryptography.Decrypt(decryptedSessiontoken, true);

                string[] tokenProperties = StringFormatCollection.AccessTokenPropertyArray;
                var splitedDataList = decryptedSessiontoken.Split(tokenProperties, StringSplitOptions.None);

                if (tokenProperties.Length <= splitedDataList.Count()
                    && tonkenPropertyIndex <= splitedDataList.Count())
                {
                    for (int i = 0; i < splitedDataList.Count(); i++)
                    {
                        if (i == tonkenPropertyIndex)
                        {
                            data = splitedDataList[i];
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return data;
        }
        #endregion

        #region Get Login Provider From Session Token

        public string GetLoginProviderFromSessionToken(string sessiontoken, int tokenPropertyIndex)
        {
            string loginProvider = string.Empty;
            try
            {
                string decryptedToken = Cryptography.Decrypt(sessiontoken, true);
                string[] tokenProperties = new string[] { ",uid:", ",uname:", ",dc:", ",deviceId:" };
                var splitedData = decryptedToken.Split(tokenProperties, StringSplitOptions.None);
                if (splitedData.Count() > 0)
                {
                    for (int i = 0; i < splitedData.Count(); i++)
                    {
                        if (i == tokenPropertyIndex)
                        {
                            loginProvider = splitedData[i];
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return loginProvider;
        }
        #endregion

        #region Check Security Token Format

        public bool CheckSecurityTokenFormat(string sessiontoken)
        {
            bool reasult = false;
            try
            {
                string decryptedToken = Cryptography.Decrypt(sessiontoken, true);
                string[] tokenProperties = StringFormatCollection.AccessTokenPropertyArray;

                for (int i = 0; i < tokenProperties.Length; i++)
                {
                    if (decryptedToken.Contains(tokenProperties[i]))
                    {
                        int strIndex = decryptedToken.IndexOf(tokenProperties[i]);
                        int tempStrIndex = 0;

                        if (strIndex > tempStrIndex)
                        {
                            tempStrIndex = strIndex;
                            reasult = true;
                            continue;
                        }
                        else
                        {
                            reasult = false;
                            break;
                        }
                    }
                    else
                    {
                        reasult = false;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return reasult;
        }

        public bool CheckSecurityTokenFormatV2(string sessiontoken)
        {
            bool reasult = false;
            try
            {
                string[] tokenProperties = StringFormatCollection.AccessTokenPropertyArrayV2;

                for (int i = 0; i < tokenProperties.Length; i++)
                {
                    if (sessiontoken.Contains(tokenProperties[i]))
                    {
                        int strIndex = sessiontoken.IndexOf(tokenProperties[i]);
                        int tempStrIndex = 0;

                        if (strIndex > tempStrIndex)
                        {
                            tempStrIndex = strIndex;
                            reasult = true;
                            continue;
                        }
                        else
                        {
                            reasult = false;
                            break;
                        }
                    }
                    else
                    {
                        reasult = false;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return reasult;
        }
        public bool CheckSecurityTokenFormatV3(string sessiontoken)
        {
            bool reasult = false;
            try
            {
                string[] tokenProperties = StringFormatCollection.AccessTokenPropertyArray;

                for (int i = 0; i < tokenProperties.Length; i++)
                {
                    if (sessiontoken.Contains(tokenProperties[i]))
                    {
                        int strIndex = sessiontoken.IndexOf(tokenProperties[i]);
                        int tempStrIndex = 0;

                        if (strIndex > tempStrIndex)
                        {
                            tempStrIndex = strIndex;
                            reasult = true;
                            continue;
                        }
                        else
                        {
                            reasult = false;
                            break;
                        }
                    }
                    else
                    {
                        reasult = false;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return reasult;
        }
        #endregion 
        public async Task<string> GetUnpairedMSISDNSearchDefaultValue(UnpairedMSISDNListReqModel model)
        {
            string msisdn = string.Empty;
            try
            {
                var dataRow = await dataManager.GetUnpairedMSISDNSearchDefaultValue(model);

                if (dataRow.Rows.Count > 0)
                {
                    msisdn = dataRow.Rows[0]["MSISDNPFX"] == DBNull.Value ? "" : dataRow.Rows[0]["MSISDNPFX"].ToString() ?? "";
                }
            }
            catch (Exception)
            {
                throw;
            }
            return msisdn;
        }

        public async Task<string> GetUnpairedMSISDNSearchDefaultValueV2(UnpairedMSISDNListReqModel model)
        {
            string msisdn = string.Empty;
            try
            {
                var dataRow = await dataManager.GetUnpairedMSISDNSearchDefaultValueV2(model);

                if (dataRow.Rows.Count > 0)
                {
                    msisdn = dataRow.Rows[0]["MSISDNPFX"] == DBNull.Value ? "" : dataRow.Rows[0]["MSISDNPFX"].ToString() ?? "";
                }
            }
            catch (Exception)
            {
                throw;
            }
            return msisdn;
        }

        public async Task<ChannelWiseResponse> GetPaymentMethod(RAGetPaymentMehtodRequest model)
        {
            List<ChannelWiseResponseData> cws = new List<ChannelWiseResponseData>();
            ChannelWiseResponse cwRes = new ChannelWiseResponse();
            try
            {
                var dataRow = await dataManager.GetPaymentMethod(model);

                if (dataRow.Rows.Count > 0)
                {
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {

                        ChannelWiseResponseData cw = new ChannelWiseResponseData();
                        cw.payment_amount = Convert.ToString(dataRow.Rows[i]["PAYMENT_AMOUNT"] == DBNull.Value ? "" : dataRow.Rows[i]["PAYMENT_AMOUNT"]) ?? "";
                        cw.payment_method = Convert.ToString(dataRow.Rows[i]["PAYMENT_METHOD"] == DBNull.Value ? "" : dataRow.Rows[i]["PAYMENT_METHOD"]) ?? "";
                        cws.Add(cw);
                    }

                    cwRes.data = cws;
                    cwRes.result = true;
                    cwRes.message = MessageCollection.Success;
                }
                else
                {
                    cwRes.data = cws;
                    cwRes.result = false;
                    cwRes.message = MessageCollection.NoDataFound;
                }

            }
            catch (Exception)
            {
                throw;
            }
            return cwRes;
        }

        public async Task<ChannelWiseResponseRev> GetPaymentMethodV2(RAGetPaymentMehtodRequest model, string userName)
        {
            List<ChannelWiseResponseDataRev> cws = new List<ChannelWiseResponseDataRev>();
            ChannelWiseResponseRev cwRes = new ChannelWiseResponseRev();
            try
            {
                var dataRow = await dataManager.GetPaymentMethodV2(model, userName);

                if (dataRow.Rows.Count > 0)
                {
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {

                        ChannelWiseResponseDataRev cw = new ChannelWiseResponseDataRev();
                        cw.payment_amount = Convert.ToString(dataRow.Rows[i]["PAYMENT_AMOUNT"] == DBNull.Value ? "" : dataRow.Rows[i]["PAYMENT_AMOUNT"]) ?? "";
                        cw.payment_method = Convert.ToString(dataRow.Rows[i]["PAYMENT_METHOD"] == DBNull.Value ? "" : dataRow.Rows[i]["PAYMENT_METHOD"]) ?? "";
                        cws.Add(cw);
                    }

                    cwRes.data = cws;
                    cwRes.isError = false;
                    cwRes.message = MessageCollection.Success;
                }
                else
                {
                    cwRes.data = cws;
                    cwRes.isError = true;
                    cwRes.message = MessageCollection.NoDataFound;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return cwRes;
        }

        public async Task<RechargeAmountData> GetRechargeAmount(RechargeAmountReqModel model, string userName)
        {
            List<RechargeAmountResponse> rechList = new List<RechargeAmountResponse>();
            RechargeAmountData rechargeAmnt = new RechargeAmountData();
            try
            {
                DataTable dataRow = await dataManager.GetRechargeAmount(model, userName);

                if (dataRow.Rows.Count > 0)
                {
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {
                        RechargeAmountResponse rcrg = new RechargeAmountResponse();
                        rcrg.rechargeAmount = Convert.ToDouble(dataRow.Rows[i]["AMOUNT"] == DBNull.Value ? null : dataRow.Rows[i]["AMOUNT"]);
                        rcrg.amountId = Convert.ToDouble(dataRow.Rows[i]["AMOUNTVALUE"] == DBNull.Value ? null : dataRow.Rows[i]["AMOUNTVALUE"]);
                        rechList.Add(rcrg);
                    }

                    rechargeAmnt.data = rechList;
                    rechargeAmnt.isError = false;
                    rechargeAmnt.message = MessageCollection.Success;
                }
                else
                {
                    rechargeAmnt.data = rechList;
                    rechargeAmnt.isError = true;
                    rechargeAmnt.message = MessageCollection.NoDataFound;
                }

            }
            catch (Exception)
            {
                throw;
            }
            return rechargeAmnt;
        }
        public async Task<long> AppInfoUpdate(AppInfoUpdateReqModel model, string loginProvider)
        {
            long response = 0;
            try
            {
                response = await dataManager.AppInfoUpdate(model, loginProvider);

                return response;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<BTSCode> GetBTSCode(SiteIdRequestModel model)
        {
            List<RechargeAmountResponse> rechList = new List<RechargeAmountResponse>();
            RechargeAmountData rechargeAmnt = new RechargeAmountData();
            BTSCode bTSCode = new BTSCode();
            try
            {
                DataTable dataRow = await dataManager.GetBTSCode(model);

                if (dataRow.Rows.Count > 0)
                {
                    for (int i = 0; i < dataRow.Rows.Count;)
                    {
                        bTSCode.bts_code = Convert.ToString(dataRow.Rows[i]["BTS_CODE"]) ?? "";
                        break;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return bTSCode;
        }

        public async Task<BlackListedWordModel> GetBlackListedWordForAddress()
        {
            BlackListedWordModel blackListed = new BlackListedWordModel();

            try
            {
                DataTable dataTable = await dataManager.GetBlackListedWordForAddress();

                blackListed.data = dataTable.AsEnumerable()
                    .Select(row => row.Field<string>("ADDRESS_JUNK") ?? string.Empty)
                    .ToArray();
            }
            catch (Exception)
            {
                throw;
            }

            return blackListed;
        }

        public async Task<BlackListedWordModel> GetBlackListedWordForName()
        {
            BlackListedWordModel blackListed = new BlackListedWordModel();

            try
            {
                DataTable dataTable = await dataManager.GetBlackListedWordForName();

                blackListed.data = dataTable.AsEnumerable()
                    .Select(row => row.Field<string>("NAME_JUNK") ?? string.Empty)
                    .ToArray();
            }
            catch (Exception)
            {
                throw;
            }

            return blackListed;
        }

        public async Task<ScannerInfoRespModel> GetScannerInfo(ScannerInfoReqModel model)
        {
            ScannerInfoRespModel scanner = new ScannerInfoRespModel();
            try
            {
                DataTable dataTable = await dataManager.GetScannerInfo(model);

                if (dataTable.Rows.Count > 0)
                {
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        scanner.data = new ScannerData
                        {
                            is_bl_scanner = Convert.ToString(dataTable.Rows[i]["IS_BL_SCANNER"]) ?? ""
                        };
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return scanner;
        }
        public async Task<FTROfferIdRespModel> GetOfferIdforFTR(string channelName, string userId, string bi_token_number)
        {
            List<RechargeAmountResponse> rechList = new List<RechargeAmountResponse>();
            RechargeAmountData rechargeAmnt = new RechargeAmountData();
            FTROfferIdRespModel fTROfferId = new FTROfferIdRespModel();
            try
            {
                DataTable dataRow = await dataManager.GetOfferId(channelName, userId, bi_token_number);

                if (dataRow.Rows.Count > 0)
                {
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {
                        fTROfferId.offer_id = Convert.ToString(dataRow.Rows[i]["OFFERID"]) ?? "";
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return fTROfferId;
        }

        public async Task<FTROfferIdRespModel> GetOfferIdforFTRV2(string channelName, string userId, string bi_token_number, int is_lus)
        {
            List<RechargeAmountResponse> rechList = new List<RechargeAmountResponse>();
            RechargeAmountData rechargeAmnt = new RechargeAmountData();
            FTROfferIdRespModel fTROfferId = new FTROfferIdRespModel();
            try
            {
                DataTable dataRow = await dataManager.GetOfferIdV2(channelName, userId, bi_token_number, is_lus);

                if (dataRow.Rows.Count > 0)
                {
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {
                        fTROfferId.offer_id = Convert.ToString(dataRow.Rows[i]["OFFERID"]) ?? "";
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return fTROfferId;
        }

        public async Task<ProductInfoRespModel> GetProductValueForSIMSearching(UnpairedSIMsearchReqModelV2 model)
        {
            ProductInfoRespModel productInfo = new ProductInfoRespModel();
            try
            {
                var dataRow = await dataManager.GetProductValueForSearChingSIM(model);

                if (dataRow.Rows.Count > 0)
                {
                    productInfo.product_code = Convert.ToString(dataRow.Rows[0]["PRODUCT_CODE"] == DBNull.Value ? "" : dataRow.Rows[0]["PRODUCT_CODE"].ToString() ?? "");
                    productInfo.product_category = Convert.ToString(dataRow.Rows[0]["PRODUCT_CATEGORY"] == DBNull.Value ? "" : dataRow.Rows[0]["PRODUCT_CATEGORY"].ToString() ?? "");
                }
            }
            catch (Exception)
            {
                throw;
            }
            return productInfo;
        }

        #region Cherish Number Sell
        public async Task<CherishCategoryListResModel> GetCherishCategoyListData(string channelName)
        {
            CherishCategoryListResModel response = new CherishCategoryListResModel();

            string defaultCategory = SettingsValues.GetCherishDefaultCategory();
            response.default_category = defaultCategory;
            try
            {
                var dataRow = await dataManager.GetCherishCategoryData(channelName);

                if (dataRow.Rows.Count > 0)
                {
                    List<CategoryList> alrs = new List<CategoryList>();
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {
                        CategoryList data = new CategoryList();
                        data.category_id = Convert.ToString(dataRow.Rows[i]["NAME"] == DBNull.Value ? "" : dataRow.Rows[i]["NAME"].ToString() ?? "");
                        var amount = Convert.ToString(dataRow.Rows[i]["AMOUNT"] == DBNull.Value ? null : dataRow.Rows[i]["AMOUNT"]);
                        var message = Convert.ToString(dataRow.Rows[i]["MESSAGE"] == DBNull.Value ? null : dataRow.Rows[i]["MESSAGE"]);

                        data.category_Name = data.category_id + " @" + message + " " + amount + "";

                        alrs.Add(data);
                    }

                    response.data = alrs;
                    response.isError = false;
                    response.message = MessageCollection.Success;
                    return response;
                }
                else
                {
                    response.data = new List<CategoryList>();
                    response.isError = true;
                    response.message = MessageCollection.NoDataFound;
                    return response;
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<CherishCategory> GetDesiredCategoryMessage(string categoryName, string channel_name)
        {
            CherishCategory res = new CherishCategory();
            try
            {
                var dataRow = await dataManager.GetDesiredCatMessage(categoryName, channel_name);

                if (dataRow.Rows.Count > 0)
                {
                    res.name = Convert.ToString(dataRow.Rows[0]["NAME"] == DBNull.Value ? "" : dataRow.Rows[0]["NAME"]) ?? "";
                    res.channel_name = Convert.ToString(dataRow.Rows[0]["CHANNEL_NAME"] == DBNull.Value ? "" : dataRow.Rows[0]["CHANNEL_NAME"]) ?? "";
                    var amount = Convert.ToString(dataRow.Rows[0]["AMOUNT"] == DBNull.Value ? null : dataRow.Rows[0]["AMOUNT"]);
                    if (res.channel_name != null && res.channel_name.ToLower() == "B2C_postpaid".ToLower())
                    {
                        res.message = "This is <b>" + res.name + "</b> number; Applicable recharge bundle <b>" + res.name + "</b>= <b>" + amount + "</b> Tk.";
                    }
                    else
                    {
                        res.message = "This is <b>" + res.name + "</b> number; Applicable recharge amount <b>" + res.name + "</b>= <b>" + amount + "</b> Tk.";
                    }
                }

                return res;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<SubscriptionTypeResModel> GetSubscriptionTypes(RASubscriptionTypeReq model)
        {
            List<SubscriptionTypeResData> subscriptions = new List<SubscriptionTypeResData>();
            SubscriptionTypeResModel subscriptionsRes = new SubscriptionTypeResModel();
            try
            {
                var dataRow = await dataManager.GetSubscriptionsTypes(model);

                if (dataRow.Rows.Count > 0)
                {
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {

                        SubscriptionTypeResData pn = new SubscriptionTypeResData();
                        pn.subscription_id = Convert.ToInt32(dataRow.Rows[i]["SUBSCRIPTION_ID"] == DBNull.Value ? null : dataRow.Rows[i]["SUBSCRIPTION_ID"]);
                        pn.subscription_name = Convert.ToString(dataRow.Rows[i]["NAME"] == DBNull.Value ? "" : dataRow.Rows[i]["NAME"].ToString() ?? "");
                        subscriptionsRes.data.Add(pn);


                        subscriptions.Add(pn);
                    }

                    subscriptionsRes.data = subscriptions;
                    subscriptionsRes.isError = false;
                    subscriptionsRes.message = MessageCollection.Success;
                }
                else
                {
                    subscriptionsRes.data = subscriptions;
                    subscriptionsRes.isError = true;
                    subscriptionsRes.message = MessageCollection.NoDataFound;
                }

            }
            catch (Exception)
            {
                throw;
            }
            return subscriptionsRes;
        }

        public async Task<string> GetCategoryMinAmount(string category)
        {
            string amount = string.Empty;
            try
            {
                var dataRow = await dataManager.GetCategoryMinAmount(category);

                if (dataRow.Rows.Count > 0)
                {
                    amount = Convert.ToString(dataRow.Rows[0]["AMOUNT"] == DBNull.Value ? "" : dataRow.Rows[0]["AMOUNT"].ToString() ?? "");
                }
            }
            catch (Exception)
            {
                throw;
            }
            return amount;
        }

        public async Task<RechargeAmountData> GetRechargeAmountV2(RechargeAmountReqModelRev model, string userName)
        {
            List<RechargeAmountResponse> rechList = new List<RechargeAmountResponse>();
            RechargeAmountData rechargeAmnt = new RechargeAmountData();
            try
            {
                DataTable dataRow = await dataManager.GetRechargeAmountV2(model, userName);

                if (dataRow.Rows.Count > 0)
                {
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {
                        RechargeAmountResponse rcrg = new RechargeAmountResponse();
                        rcrg.rechargeAmount = Convert.ToDouble(dataRow.Rows[i]["AMOUNT"] == DBNull.Value ? null : dataRow.Rows[i]["AMOUNT"]);
                        rcrg.amountId = Convert.ToDouble(dataRow.Rows[i]["AMOUNTVALUE"] == DBNull.Value ? null : dataRow.Rows[i]["AMOUNTVALUE"]);
                        rechList.Add(rcrg);
                    }

                    rechargeAmnt.data = rechList;
                    rechargeAmnt.isError = false;
                    rechargeAmnt.message = MessageCollection.Success;
                }
                else
                {
                    rechargeAmnt.data = rechList;
                    rechargeAmnt.isError = true;
                    rechargeAmnt.message = MessageCollection.NoDataFound;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return rechargeAmnt;
        }
        public async Task<RechargeAmountData> GetRechargeAmountV3(RechargeAmountReqModelRevV3 model, string userName)
        {
            List<RechargeAmountResponse> rechList = new List<RechargeAmountResponse>();
            RechargeAmountData rechargeAmnt = new RechargeAmountData();
            try
            {
                DataTable dataRow;

                dataRow = await dataManager.GetRechargeAmountV3(model, userName);

                if (dataRow.Rows.Count > 0)
                {
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {
                        RechargeAmountResponse rcrg = new RechargeAmountResponse
                        {
                            rechargeAmount = Convert.ToDouble(dataRow.Rows[i]["AMOUNT"] == DBNull.Value ? 0 : dataRow.Rows[i]["AMOUNT"]),
                            amountId = Convert.ToDouble(dataRow.Rows[i]["AMOUNTVALUE"] == DBNull.Value ? 0 : dataRow.Rows[i]["AMOUNTVALUE"])
                        };
                        rechList.Add(rcrg);
                    }

                    rechargeAmnt.data = rechList;
                    rechargeAmnt.isError = false;
                    rechargeAmnt.message = MessageCollection.Success;
                }
                else
                {
                    rechargeAmnt.data = rechList;
                    rechargeAmnt.isError = true;
                    rechargeAmnt.message = MessageCollection.NoDataFound;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return rechargeAmnt;
        }
        public async Task<bool> GetIsLusEligibleAsync(string btsCode)
        {
            bool isEligible = false;
            string isLus = string.Empty;
            try
            {
                DataTable dataRow = new DataTable();
                dataRow = await dataManager.GetIsLusEligibleAsync(btsCode);
                if (dataRow.Rows.Count > 0)
                {
                    isLus = Convert.ToString(
                            dataRow.Rows[0]["BTS_CODE"] == DBNull.Value ? "" : dataRow.Rows[0]["BTS_CODE"]
                        );
                }
                isEligible = !String.IsNullOrEmpty(isLus) ? true : false;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return isEligible;
        }
        public async Task<int> GetLUSEligibleStatusfromBIA(string is_lus)
        {
            int lusValue = 0;

            try
            {
                DataTable dataRow = await dataManager.GetLUSEligiblefromBIA(is_lus);

                if (dataRow.Rows.Count > 0)
                {
                    lusValue = Convert.ToInt32(dataRow.Rows[0]["IS_LUS"] == DBNull.Value ? 0 : dataRow.Rows[0]["IS_LUS"]);
                }

                //isEligible = lusValue > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in GetLUSEligiblefromBIA: " + ex.Message, ex);
            }

            return lusValue;
        }
        public async Task<string> GetUnpairedMSISDNSearchDefaultValueCherished(UnpairedMSISDNListReqModelV2 model)
        {
            string? msisdn = string.Empty;
            try
            {
                var dataRow = await dataManager.GetUnpairedMSISDNSearchDefaultValueCherished(model);

                if (dataRow.Rows.Count > 0)
                {
                    msisdn = dataRow.Rows[0]["MSISDNPFX"] == DBNull.Value ? null : dataRow.Rows[0]["MSISDNPFX"].ToString();
                }
            }
            catch (Exception)
            {
                throw;
            }
            return msisdn;
        }
        #endregion

        
        public async Task<TOSFeeResponseModel> GetTOSFeeFromDB(string channel, int simCategory)
        {
            TOSFeeResponseModel response = new TOSFeeResponseModel();
            try
            {
                string productType = string.Empty;

                productType = simCategory == 1 ? "PREPAID" : "POSTPAID";

                DataTable dt = await dataManager.GetTOSFeeAsync(channel, productType);

                if (dt.Rows.Count > 0)
                {
                    response.FeeAmount = Convert.ToDecimal(dt.Rows[0]["FEEAMOUNT"]);
                }
            }
            catch (Exception)
            {
                throw;
            }

            return response;
        }
        public async Task<DMSDBSessionResponse> GetDMSSessionValues()
        {
            DMSDBSessionResponse session = new DMSDBSessionResponse();
            try
            {
                DataTable dataTable = await dataManager.GetDMSSessionValues();

                if (dataTable.Rows.Count > 0)
                {
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        session.SESSIONTOKEN = Convert.ToString(dataTable.Rows[i]["SESSIONTOKEN"]) ?? "";
                        session.CREATE_DATE = Convert.ToDateTime(dataTable.Rows[i]["CREATE_DATE"] ?? DateTime.Now);
                        session.SESSIONTIME = Convert.ToInt32(dataTable.Rows[i]["SESSIONTIME"] ?? 0);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return session;
        }


        public async Task<string> GetEVTransactionNumber(string userName)
        {
            string ev_transactionNumber = string.Empty;
            try
            {
                DataTable dataRow = await dataManager.GetRetailerTransactionNumber(userName);

                if (dataRow.Rows.Count > 0)
                {
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {
                        ev_transactionNumber = Convert.ToString(dataRow.Rows[i]["ITOPUPSRNUMBER"]) ?? "";
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return ev_transactionNumber;
        }

        public async Task<int> GetMSISDNStatusForTOS(string msisdn)
        {
            int ev_transactionNumber = 0;
            try
            {
                if (!String.IsNullOrEmpty(msisdn))
                {
                    msisdn = msisdn.Substring(0, 2) == FixedValueCollection.MSISDNCountryCode ? msisdn
                                                            : FixedValueCollection.MSISDNCountryCode + msisdn;
                }
                DataTable dataRow = await dataManager.GetMSISDNStatusForTOS(msisdn);

                if (dataRow.Rows.Count > 0)
                {
                    for (int i = 0; i < dataRow.Rows.Count; i++)
                    {
                        ev_transactionNumber = Convert.ToInt32(dataRow.Rows[i]["TOTAL_COUNT"]);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return ev_transactionNumber;
        }

        public async Task SaveDMSSession(DMSLoginResponse model)
        {
            await dataManager.SaveDMSSession(model);
        }
        public async Task<List<GACappingConfig>> GetGACappingConfig()
        {
            try
            {
                List<GACappingConfig> cappingConfigs = new List<GACappingConfig>();

                DataTable dt = await dataManager.GetGACappingConfig();

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var gACappingConfig = new GACappingConfig();   // NEW object every iteration

                        gACappingConfig.cappType = Convert.ToString(dt.Rows[i]["CAPPTYPE"]);
                        gACappingConfig.cappDayCount = Convert.ToInt32(dt.Rows[i]["CAPPDAYCOUNT"]);
                        gACappingConfig.capQuantityCount = Convert.ToInt32(dt.Rows[i]["CAPPQUANTITYCOUNT"]);

                        cappingConfigs.Add(gACappingConfig);
                    }
                }

                return cappingConfigs;
            }
            catch (Exception)
            {
                throw;
            }
        } 

        public async Task<SIMProductMapResponse> CeckSIMProductMapping(SIMProductMappingReqModel model)
        {
            SIMProductMapResponse response = new SIMProductMapResponse();
            try
            {
                DataTable dataTable = await dataManager.CeckSIMProductMapping(model);

                if (dataTable.Rows.Count > 0)
                {
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        response.is_success = Convert.ToInt32(dataTable.Rows[i]["IS_SUCCESS"]) == 1 ? true : false;
                        response.message = Convert.ToString(dataTable.Rows[i]["MESSAGE"]) ?? "";
                        response.product_code = Convert.ToString(dataTable.Rows[i]["PRODUCT_CODE"]) ?? "";
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return response;
        }

        public async Task<SIMProductMapResponse> CeckSIMProductMappingV2(SIMProductMappingReqModelV2 model)
        {
            SIMProductMapResponse response = new SIMProductMapResponse();
            try
            {
                DataTable dataTable = await dataManager.CeckSIMProductMappingV2(model);

                if (dataTable.Rows.Count > 0)
                {
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        response.is_success = Convert.ToInt32(dataTable.Rows[i]["IS_SUCCESS"]) == 1 ? true : false;
                        response.message = Convert.ToString(dataTable.Rows[i]["MESSAGE"]) ?? "";
                        response.product_code = Convert.ToString(dataTable.Rows[i]["PRODUCT_CODE"]) ?? "";
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return response;
        }
        public async Task<List<SubscriptionMappingResponse>> GetSubscriptionMapping(RASubscriptionTypeReqWithMapping model)
        {
            List<SubscriptionMappingResponse> responseList = new List<SubscriptionMappingResponse>();
            try
            {
                DataTable dataTable = await dataManager.GetSubscriptionMapping(model);

                if (dataTable.Rows.Count > 0)
                {
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        SubscriptionMappingResponse response = new SubscriptionMappingResponse();
                        response.is_success = Convert.ToInt32(dataTable.Rows[i]["IS_SUCCESS"]) == 1 ? true : false;
                        response.message = Convert.ToString(dataTable.Rows[i]["MESSAGE"]) ?? "";
                        response.subscription_code = Convert.ToString(dataTable.Rows[i]["SUBSCRIPTIONCODE"]) ?? "";
                        responseList.Add(response);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return responseList;
        }
        
        public async Task<List<SubscriptionMappingResponse>> GetSubscriptionMappingV2(RASubscriptionTypeReqWithMappingV2 model)
        {
            List<SubscriptionMappingResponse> responseList = new List<SubscriptionMappingResponse>();
            try
            {
                DataTable dataTable = await dataManager.GetSubscriptionMappingV2(model);

                if (dataTable.Rows.Count > 0)
                {
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        SubscriptionMappingResponse response = new SubscriptionMappingResponse();
                        response.is_success = Convert.ToInt32(dataTable.Rows[i]["IS_SUCCESS"]) == 1 ? true : false;
                        response.message = Convert.ToString(dataTable.Rows[i]["MESSAGE"]) ?? "";
                        response.subscription_code = Convert.ToString(dataTable.Rows[i]["SUBSCRIPTIONCODE"]) ?? "";
                        responseList.Add(response);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return responseList;
        }

        public async Task<List<PackageCodeMappingRespModel>> GetPackageMapping(RAGetPackageResquestV4 model)
        {
            List<PackageCodeMappingRespModel> responseList = new List<PackageCodeMappingRespModel>();
            try
            {
                DataTable dataTable = await dataManager.GetPackageMapping(model);

                if (dataTable.Rows.Count > 0)
                {
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        PackageCodeMappingRespModel response = new PackageCodeMappingRespModel();
                        response.is_success = Convert.ToInt32(dataTable.Rows[i]["IS_SUCCESS"]) == 1 ? true : false;
                        response.message = Convert.ToString(dataTable.Rows[i]["MESSAGE"]) ?? "";
                        response.package_code = Convert.ToString(dataTable.Rows[i]["PACKAGECODE"]) ?? "";
                        responseList.Add(response);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return responseList;
        }

        public async Task<List<PackageCodeMappingRespModel>> GetPackageMappingV2(PackagesFetchedRequestModel model)
        {
            List<PackageCodeMappingRespModel> responseList = new List<PackageCodeMappingRespModel>();
            try
            {
                DataTable dataTable = await dataManager.GetPackageMappingV2(model);

                if (dataTable.Rows.Count > 0)
                {
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        PackageCodeMappingRespModel response = new PackageCodeMappingRespModel();
                        response.is_success = Convert.ToInt32(dataTable.Rows[i]["IS_SUCCESS"]) == 1 ? true : false;
                        response.message = Convert.ToString(dataTable.Rows[i]["MESSAGE"]) ?? "";
                        response.package_code = Convert.ToString(dataTable.Rows[i]["PACKAGECODE"]) ?? "";
                        responseList.Add(response);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return responseList;
        }

        public async Task<RecycleBaseCheckingRespModel> GetCheckingRecycleBase(RecycleBaseCheckingReqModel model)
        {
            RecycleBaseCheckingRespModel response = new RecycleBaseCheckingRespModel();
            try
            {
                DataTable dataTable = await dataManager.GetRecycleBaseChecking(model);

                if (dataTable.Rows.Count > 0)
                {
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        response.is_success = Convert.ToInt32(dataTable.Rows[i]["IS_SUCCESS"]) == 1 ? true : false;
                        response.error_message = Convert.ToString(dataTable.Rows[i]["DESCRIPTION"]) ?? "";
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return response;
        }        
    }
}
