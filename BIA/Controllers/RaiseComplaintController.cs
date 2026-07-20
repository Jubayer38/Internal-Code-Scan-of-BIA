///************************************************************************
///	|| Creation History ||
///-----------------------------------------------------------------------
///	Copyright     :	Copyright© NAAS Solutions Limited. All rights reserved.
///	Author	      :	Mohiuddin
///	Purpose	      :	Sending complain to RSO system from all channel from Biometric App
///	Creation Date :	10-Jun-2023
/// =======================================================================
///  || Modification History ||
///  ----------------------------------------------------------------------
///  Sl No.	Date:		    Author:			    Ver:	    Area of Change:
///  1.     
///	 ----------------------------------------------------------------------
///	***********************************************************************
using BIA.BLL.BLLServices;
using BIA.BLL.Utility;
using BIA.Entity.Collections;
using BIA.Entity.DB_Model;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.JWT;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace BIA.Controllers
{
    [Route("api/RaiseComplaint")]
    [ApiController]
    public class RaiseComplaintController : ControllerBase
    {
        private readonly BLLRaiseComplaint _complaintManager;
        private readonly BaseController _bio;
        private readonly BLLLog _bllLog;
        private readonly ApiRequest _apiRequest;

        public RaiseComplaintController(BLLRaiseComplaint complaintManager, BaseController bio, BLLLog bllLog, ApiRequest apiRequest)
        {
            _complaintManager = complaintManager;
            _bio = bio;
            _bllLog = bllLog;
            _apiRequest = apiRequest;
        }

        /// <summary>
        /// This api is used from sending complain to RSO system from all channel from Biometric App. The request process steps is below
        /// 1. Insert the complain into Biometric database first.
        /// 2. Send the complain to RSO system
        /// 3. Update the complain into Biometric database as it has sent
        /// 4. Catch block for any exception occured in the above checking
        /// 5. Finaly insert a log in Complain Log table with BLOB Data
        /// </summary>
        /// <param name="reqModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("ComplaintSubmit")]
        public async Task<IActionResult> RaiseComplaintSubmit([FromBody][Bind("bi_token_number,description,retailerCode,session_token")] ComplaintReqModel reqModel)
        {
            ValidTokenResponse security = new ValidTokenResponse();
            BLLRAToDBSSParse dBSSParse = new BLLRAToDBSSParse();
            ComplaintResponseModel complaintResponse = new ComplaintResponseModel();
            BL_Json _blJson = new BL_Json();
            BIAToRaiseComplainLog log = new BIAToRaiseComplainLog();
            ErrorDescription error = new ErrorDescription();

            DateTime reqTime = DateTime.Now;
            bool isSuccess = false;
            string? message = String.Empty;

            try
            {
                SubmitComplaintModel model = new SubmitComplaintModel
                {
                    session_token = reqModel.session_token,
                    retailerCode = reqModel.retailerCode,
                    description = reqModel.description,
                    userName = SettingsValues.GetRSOAppUserName(),
                    password = SettingsValues.GetRSOComplainCred(),
                    complaintType = SettingsValues.GetRSOComplainType(),
                    complaintTitle = SettingsValues.GetRSOComplainTitle(),
                    preferredLevel = SettingsValues.GetRSOComplainPreferedLabel(),
                    preferredLevelName = SettingsValues.GetRSOComplainPreferedLabelName(),
                    preferredLevelContact = SettingsValues.GetRSOComplainPreferedLabelContact()
                };

                #region Insert_Complaint_In_DB
                var res = await _complaintManager.SubmitComplaint(model);
                if (!res.is_success)
                {
                    return Ok(new ComplaintResponseModel
                    {
                        isError = true,
                        message = res.message
                    });
                }
                #endregion

                #region Submit_Complaint_To_RSO
                model.raiseComplaintID = res.complaint_id;
                log.complaint_id = res.complaint_id;
                log.user_id = reqModel.retailerCode;

                RSOComplaintRequestModel rsoReqModel = dBSSParse.ComplaintReqPargeModel(model);
                log.req_blob = _blJson.GetGenericJsonData(rsoReqModel);
                log.req_time = DateTime.Now;

                JObject jsonResponse = await _apiRequest.HttpPostRequestRSO(rsoReqModel, RSOAPI.ComplaintAPI, "RaiseComplaintSubmit");

                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(jsonResponse);

                if (jsonResponse != null)
                {
                    isSuccess = Convert.ToBoolean(jsonResponse["success"]);
                    message = Convert.ToString(jsonResponse["message"]);
                }
                else
                {
                    isSuccess = false;
                    message = "Invalid Response from RSO App";
                }

                if (!isSuccess)
                {
                    return Ok(new ComplaintResponseModel
                    {
                        isError = true,
                        message = message
                    });
                }

                #region Update_Bi_Request_Raise_Complaint_Flag
                if (!string.IsNullOrEmpty(reqModel.bi_token_number))
                {
                    await _complaintManager.UpdateOrderComplaintStatus(reqModel.bi_token_number);
                }
                #endregion

                return Ok(new ComplaintResponseModel
                {
                    isError = false,
                    message = message
                });
                #endregion
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
                Log.Error(ex, "RaiseComplaintSubmit Exception");
                log.res_time = DateTime.Now;
                log.is_success = 0;
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                complaintResponse.isError = true;
                complaintResponse.message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_description ?? string.Empty;

                return Ok(complaintResponse);
            }
            finally
            {
                await _bllLog.RaiseCoplainLog(log);
            }
        }
    }
}
