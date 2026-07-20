///************************************************************************
///	|| Creation History ||
///-----------------------------------------------------------------------
///	Copyright     :	Copyright© NAAS Solutions Limited. All rights reserved.
///	Author	      :	Mohiuddin
///	Purpose	      :	For updating retailer as real time called from Retailer App API
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
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.PopulateModel;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Entity.Utility;
using BIA.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BIA.Controllers
{
    [Route("api/RetailerSync")]
    [ApiController]
    public class RetailerSyncController : ControllerBase
    {
        private readonly BLLRetailerUserSync _userSync;
        private readonly BLLLog _bllLog;

        public RetailerSyncController(BLLRetailerUserSync userSync, BLLLog bllLog)
        {
            _userSync = userSync;
            _bllLog = bllLog;
        }

        /// <summary>
        /// This API is used for updating any retailer status (Deactive only) from DMS system through Retailer App API to Biometric database. The request process steps is below-
        /// 1. Checking credential sended from Retailer App API if the caller source is correct or not.
        /// 2. Update the retailer status in Biometric database.
        /// 3. Catch block for any exception occured in the above checking
        /// 4. Insert a final log in Retailer update log table with request and response blob data.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [DMSRetailerSyncRequestModelValidator]
        [Route("PostRetailerStatus")]
        public async Task<IActionResult> PostRetailerStatus([FromBody][Bind("iTopUpNumber,isActive,password,retailerCode,typeName,userName")] DMSRetailerSyncRequestModel request)
        {
            AESCryptography aESCryptography = new AESCryptography();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BL_Json bL_Json = new BL_Json();
            DMSRetailerResponseModel respModel = new DMSRetailerResponseModel();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            DMSRetailerReqModel model = new DMSRetailerReqModel();
            try
            {
                model = populateModel.DMSRetailerSyncRequestPopulateModel(request);

                string biometricUserName = string.Empty;
                string biometricPassword = string.Empty;
                string userName = string.Empty;
                string password = string.Empty;
                decimal? result = 0;
                log.req_blob = bL_Json.GetGenericJsonData(model);
                log.req_time = DateTime.Now;
                try
                {
                    biometricUserName = SettingsValues.GetUserStatusUpdateUserName();
                    biometricPassword = SettingsValues.GetUserStatusUpdatePassword();
                }
                catch
                { }

                userName = AESCryptography.Encrypt(model.userName);
                password = AESCryptography.Encrypt(model.password);


                if (userName == biometricUserName && password == biometricPassword)
                {
                    result = await _userSync.UpdateRetailerUserByDMS(model);

                    log.res_blob = bL_Json.GetGenericJsonData(result);
                    log.res_time = DateTime.Now;

                    if (result > 0)
                    {
                        log.is_success = 1;
                        respModel.is_success = true;
                        respModel.message = "Successfully Updated " + model.retailerCode + " !";
                        return Ok(respModel);
                    }
                    else
                    {
                        log.is_success = 0;
                        respModel.is_success = false;
                        respModel.message = "Error occured!";
                        return Ok(respModel);
                    }
                }
                else
                {
                    log.is_success = 0;
                    respModel.is_success = false;
                    respModel.message = "Invalid User credentials!";
                    return Ok(respModel);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = bL_Json.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                return Ok(new DMSRetailerResponseModel()
                {
                    is_success = false,
                    message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                });
            }
            finally
            {                
                log.msisdn = _bllLog.FormatMSISDN(model.iTopUpNumber);

                string retailerId = string.Empty;
                if (model.retailerCode.Substring(0, 1) == "R")
                {
                    retailerId = model.retailerCode.Substring(1);
                }
                log.user_id = retailerId;
                log.method_name = "PostRetailerStatus";

                await _bllLog.RAToDBSSLogV2(log, "", "");

            }
        }
    }
}
