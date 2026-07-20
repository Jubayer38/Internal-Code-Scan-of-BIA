using BIA.BLL.Utility;
using BIA.DAL.Repositories;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BIA.BLL.BLLServices
{
    public class SingleSourceGACappingService
    {
        private readonly BL_Json _blJson;
        private readonly BLLLog _bllLog;
        private readonly BllHandleException _manageExecption;
        private readonly ApiCall _genericApiCall;
        private readonly DALBiometricRepo _dalBiometricRepo;
        private static string singleSourceLoginSessionToken = string.Empty;

        public SingleSourceGACappingService(
            BL_Json blJson,
            BLLLog bllLog,
            BllHandleException manageExecption,
            ApiCall genericApiCall,
            DALBiometricRepo dalBiometricRepo)
        {
            _blJson = blJson;
            _bllLog = bllLog;
            _manageExecption = manageExecption;
            _genericApiCall = genericApiCall;
            _dalBiometricRepo = dalBiometricRepo;
        }

        public async Task<string> GetSingleSourceSessionToken(string userName)
        {
            // Check if we have a valid token in database
            var savedToken = await GetSavedSessionToken();
            if (!string.IsNullOrEmpty(savedToken))
            {
                singleSourceLoginSessionToken = savedToken;
                return savedToken;
            }

            // If no valid token, login again
            var loginResponse = await SingleSourceLogin(userName);
            if (loginResponse.is_success && !string.IsNullOrEmpty(loginResponse.session_token))
            {
                singleSourceLoginSessionToken = loginResponse.session_token;
                await SaveSessionToken(loginResponse);
                return loginResponse.session_token;
            }

            throw new Exception("Unable to get Single Source session token");
        }

        public async Task<SingleSourceGACappingResponse> GetRegisteredMsisdnsByNid(string nid, string userName)
        {
            BIAToDBSSLog log = new BIAToDBSSLog();
            string apiUrl = string.Empty;
            SingleSourceGACappingResponse response = new SingleSourceGACappingResponse();

            DateTime reqTime = DateTime.Now;
            string loginResponseContent = string.Empty;

            try
            {
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    if (string.IsNullOrEmpty(singleSourceLoginSessionToken))
                    {
                        singleSourceLoginSessionToken = await GetSingleSourceSessionToken(userName);
                    }

                    apiUrl = string.Format(SingleSourceGACappingAPI.MSISDNCountCheck, nid, 0);

                    log.req_time = DateTime.Now;
                    log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                    // Use the new HttpGetRequestSingleSource method
                    var responseContent = await _genericApiCall.HttpGetRequestSingleSource(
                        apiUrl, singleSourceLoginSessionToken, "GetRegisteredMsisdnsByNid");

					// Convert the response to SingleSourceGACappingResponse
					//string json = JsonConvert.SerializeObject(responseContent);
					//response = JsonConvert.DeserializeObject<SingleSourceGACappingResponse>(json) ?? new SingleSourceGACappingResponse();
					// Your fixed code should look like this:
					string json = JsonConvert.SerializeObject(responseContent);

					// Add the pre-processing here, before deserialization
					string preprocessedJson = PreprocessJson(json);

					// Then deserialize the pre-processed JSON
					response = JsonConvert.DeserializeObject<SingleSourceGACappingResponse>(preprocessedJson)
							   ?? new SingleSourceGACappingResponse();

					log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(responseContent);

                    if (!response.is_success && response.message?.Contains("Invalid session token") == true)
                    {
                        singleSourceLoginSessionToken = string.Empty;
                        continue;
                    }

                    break;
                }

                return response;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in GetRegisteredMsisdnsByNid for NID: {Nid}", nid);

                ErrorDescription error = new ErrorDescription();
                try
                {
                    error = await _manageExecption.ManageException(ex, ex.HResult, "Single Source GA Capping");
                }
                catch { }

                log.req_time = reqTime;
                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(ex.Message);
                log.message = error?.error_description ?? string.Empty;
                log.error_code = error?.error_code ?? string.Empty;
                log.error_source = error?.error_source ?? "Single Source GA Capping";
                log.is_success = 0;

                response.is_success = false;
                response.message = error?.error_custom_msg ?? error?.error_description ?? "Unknown error occurred";

                throw;
            }
            finally
            {
                log.method_name = "GetRegisteredMsisdnsByNid";
                log.msisdn = _bllLog.FormatMSISDN("");
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = userName;

                await _bllLog.RAToDBSSLog(log);
            }
        }

		private string PreprocessJson(string json)
		{
			// Replace null date values with a default date string
			string pattern = @"\""reg_date\"":null";
			string replacement = @"""reg_date"":""0001-01-01T00:00:00""";

			return Regex.Replace(json, pattern, replacement);
		}

		private async Task<SingleSourceLoginRes> SingleSourceLogin(string userName)
        {
            using HttpClient client = new HttpClient();
            BIAToDBSSLog log = new BIAToDBSSLog();
            SingleSourceLoginRes loginapiResponse = new SingleSourceLoginRes();
            string loginapiUrl = SingleSourceAPI.LoginAPI;
            string loginResponseContent = string.Empty;

            DateTime reqTime = DateTime.Now;

            try
            {
                var loginReqModel = new SingleSourceLoginReq()
                {
                    user_name = SettingsValues.GetSingleSourceUserName(),
                    password = SettingsValues.GetSingleSourcePassword()
                };

                log.req_blob = _blJson.GetGenericJsonData(loginReqModel);
                string jsonData = JsonConvert.SerializeObject(loginReqModel);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(loginapiUrl, content);
                loginResponseContent = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrWhiteSpace(loginResponseContent))
                {
                    loginapiResponse = JsonConvert.DeserializeObject<SingleSourceLoginRes>(loginResponseContent) ?? new SingleSourceLoginRes();
                    loginapiResponse.is_success = true;
                }

                log.req_time = reqTime;
                log.res_blob = _blJson.GetGenericJsonData(loginapiResponse);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in SingleSourceLogin");

                ErrorDescription error = new ErrorDescription();
                try
                {
                    error = await _manageExecption.ManageException(ex, ex.HResult, "Single Source Login");
                }
                catch { }

                log.req_time = reqTime;
                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(!string.IsNullOrEmpty(loginResponseContent) ? loginResponseContent : ex.Message);
                log.message = error?.error_description ?? string.Empty;
                log.error_code = error?.error_code ?? string.Empty;
                log.error_source = error?.error_source ?? "Single Source Login";
                log.is_success = 0;

                loginapiResponse.is_success = false;
                loginapiResponse.message = error?.error_custom_msg ?? error?.error_description ?? "Login failed";

                throw;
            }
            finally
            {
                log.method_name = "SingleSourceLogin";
                log.msisdn = _bllLog.FormatMSISDN("");
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = userName;

                await _bllLog.RAToDBSSLog(log);
            }

            return loginapiResponse;
        }

        private async Task<string> GetSavedSessionToken()
        {
            try
            {
                DataTable sessionData = await _dalBiometricRepo.GetSingleSourceSessionValues();
                if (sessionData.Rows.Count > 0)
                {
                    var token = sessionData.Rows[0]["SESSION_TOKEN"]?.ToString();
                    var createdDate = Convert.ToDateTime(sessionData.Rows[0]["CREATE_DATE"]);

                    // Check if token is still valid (less than 1 hour old)
                    if (DateTime.Now.Subtract(createdDate).TotalHours < 1)
                    {
                        return token;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting saved session token");
            }

            return string.Empty;
        }

        private async Task SaveSessionToken(SingleSourceLoginRes loginResponse)
        {
            try
            {
                await _dalBiometricRepo.SaveSingleSourceSession(new SingleSourceSessionModel
                {
                    SessionToken = loginResponse.session_token,
                    CreatedDate = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving session token");
            }
        }
    }
}
