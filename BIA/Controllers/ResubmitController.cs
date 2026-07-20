///************************************************************************
///	|| Creation History ||
///-----------------------------------------------------------------------
///	Copyright     :	Copyright© NAAS Solutions Limited. All rights reserved.
///	Author	      :	Mohiuddin
///	Purpose	      :	Getting already submitted data for resubmit the request if first request is failed due to NID DOB not matched from EC error.
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
using BIA.Common;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.PopulateModel;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.JWT;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using static BIA.Common.ModelValidation;

namespace BIA.Controllers
{
    [Route("api/Resubmit")]
    [ApiController]
    public class ResubmitController : ControllerBase
    {
        private readonly BLLResubmit _resubmitManager;
        private readonly BLLLog _bllLog;        

        public ResubmitController(BLLResubmit resubmitManager, BLLLog bllLog)
        {
            _resubmitManager = resubmitManager;
            _bllLog = bllLog;
        }

        /// <summary>
        /// This api is used for getting already submitted data for resubmit the request if first request is failed due to NID DOB not matched from EC error from Biometric App. The request process steps is below-
        /// 1. Validate JWT session token
        /// 2. Validate the model if the token is exist or not
        /// 3. Fetch the data from Biometric database by BI token number
        /// 4. Catch block for any exception occured in the above checking
        /// 5. Send the data to Biometric App
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("ReSubmitOrder")]
        public async Task<IActionResult> ReSubmitOrder(FailedResubmitRequestModel request)
        {
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            ResubmitResponseModel response = new ResubmitResponseModel();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            ResubmitReqModel model = new ResubmitReqModel();
            try
            {
                model = populateModel.ResubmitRequestPopulateModel(request);
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
                        model.distributor_code = security.DistributorCode;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                var validateResponse = modelValidation.OrderReSubmitModelValidation(new ValidationPropertiesResubmitModel
                {
                    bi_token_number = model.bi_token_number,
                });

                if (!validateResponse.result)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = validateResponse.message,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = " "
                        }
                    });
                }

                response = await _resubmitManager.GetResubmitOrderInfo(model);

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

                response.data = new ResubmitResponseModelData()
                {

                };
                response.isError = true;
                response.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
            }

            return Ok(response);
        }
    }
}
