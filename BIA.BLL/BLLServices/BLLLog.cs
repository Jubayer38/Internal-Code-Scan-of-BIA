using BIA.DAL.Repositories;
using BIA.Entity.Collections;
using BIA.Entity.DB_Model;
using BIA.Entity.RequestEntity;
using BIA.Entity.ViewModel;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.BLL.BLLServices
{
    public class BLLLog
    {
        private readonly DALBiometricRepo _dataManager;

        public BLLLog(DALBiometricRepo dataManager)
        {
            _dataManager = dataManager;
        }
        public async Task BALogInsert(LogModel log)
        {
            await _dataManager.BALogInsert(log);
        }
        public async Task RAToDBSSLog(BIAToDBSSLog model)
        {
            VMBIAToDBSSLog logObj = new VMBIAToDBSSLog();
            logObj.dbss_request_id = model.dbss_request_id;
            logObj.bi_token_number = model.bi_token_number;
            logObj.error_code = model.error_code;
            logObj.error_source = string.IsNullOrEmpty(model.error_source) ? string.Empty : (model.error_source.Length > 20 ? model.error_source.Substring(0, 20) : model.error_source);
            logObj.integration_point_from = model.integration_point_from;
            logObj.integration_point_to = model.integration_point_to;
            logObj.is_success = model.is_success;
            logObj.message = string.IsNullOrEmpty(model.message) ? string.Empty : (model.message.Length > 1000 ? model.message.Substring(0, 1000) : model.message);
            logObj.method_name = model.method_name;
            logObj.msisdn = model.msisdn;
            logObj.purpose_number = String.IsNullOrEmpty(model.purpose_number) ? 0 : Convert.ToInt16(model.purpose_number);
            logObj.remarks = string.IsNullOrEmpty(model.remarks) ? string.Empty : (model.remarks.Length > 250 ? model.remarks.Substring(0, 250) : model.remarks);
            logObj.req_blob = model.req_blob;
            logObj.req_time = model.req_time;
            logObj.res_blob = model.res_blob;
            logObj.res_time = model.res_time;
            if (string.IsNullOrEmpty(model.user_id))
            {
                logObj.username = "00000";
            }
            else
            {
                logObj.username = model.user_id;
            }
            logObj.username = model.user_id;
            logObj.server_name = Environment.MachineName;

            await _dataManager.RAToDBSSLog(logObj);
        }        

        public async Task RaiseCoplainLog(BIAToRaiseComplainLog model)
        {
            VMBIAToDBSSLog logObj = new VMBIAToDBSSLog()
            {
                req_blob = model.req_blob,
                req_time = model.req_time,
                res_blob = model.res_blob,
                res_time = model.res_time,
                username = model.user_id,
                complain_id = model.complaint_id,
                server_name = Environment.MachineName
            };

            await _dataManager.RaiseCoplainLog(logObj);

        }

        ///RaiseCoplainLog

        public async Task RAToDBSSLogV2(BIAToDBSSLog model, string requestTxt, string responseTxt)
        {

            VMBIAToDBSSLog logObj = new VMBIAToDBSSLog()
            {
                error_code = model.error_code,
                error_source = model.error_source,
                integration_point_from = model.integration_point_from,
                integration_point_to = model.integration_point_to,
                is_success = model.is_success,
                message = model.message,
                method_name = model.method_name,
                msisdn = model.msisdn,
                remarks = model.remarks,
                req_blob = model.req_blob,
                req_time = model.req_time,
                res_blob = model.res_blob,
                res_time = model.res_time,
                username = model.user_id,
                server_name = Environment.MachineName
            };

            await _dataManager.RETtoBiometricLog(logObj, requestTxt, responseTxt);

        }

        public async Task BIAToDPELog(BIAToDPELog model)
        {
            VMBIAToDPELog logObj = new VMBIAToDPELog();

            logObj.order_number = model.order_number;

            logObj.error_code = model.error_code;

            logObj.error_source = string.IsNullOrEmpty(model.error_source)
                ? string.Empty
                : (model.error_source.Length > 20
                    ? model.error_source.Substring(0, 20)
                    : model.error_source);

            logObj.is_success = model.is_success;

            logObj.message = string.IsNullOrEmpty(model.message)
                ? string.Empty
                : (model.message.Length > 1000
                    ? model.message.Substring(0, 1000)
                    : model.message);

            logObj.method_name = model.method_name;

            logObj.remarks = string.IsNullOrEmpty(model.remarks)
                ? string.Empty
                : (model.remarks.Length > 250
                    ? model.remarks.Substring(0, 250)
                    : model.remarks);

            logObj.req_blob = model.req_blob;

            logObj.req_time = model.req_time;

            logObj.res_blob = model.res_blob;

            logObj.res_time = model.res_time;

            if (string.IsNullOrEmpty(model.user_id))
            {
                logObj.username = "00000";
            }
            else
            {
                logObj.username = model.user_id;
            }

            logObj.server_name = Environment.MachineName;

            await _dataManager.BIAtoDPELog(logObj);
        }

        //public async Task<ErrorDescription> ManageExceptionForDBSSNotification(string errorMessage, int code, string errorSource)
        //{
        //    ErrorDescription error = new ErrorDescription();
        //    try
        //    {                
        //        DataTable dt = await _dataManager.ManageException(errorMessage, code, errorSource);

        //        error = ExceptionMapping(dt);

        //        if (error.error_description == null)
        //        {
        //            error.error_description = errorMessage;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        error.error_description = ex.InnerException?.Message ?? ex.Message;
        //        return error;
        //    }

        //    return error;
        //}
        public async Task<ErrorDescription> ManageExceptionForDBSSNotification(string errorMessage, int code, string errorSource)
        {
            ErrorDescription error = new ErrorDescription();
            try
            {
                DataTable dt = await _dataManager.ManageException(errorMessage, code, errorSource);

                error = ExceptionMapping(dt);

                if (error.error_description == null)
                {
                    error.error_description = errorMessage;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                error.error_description = ex.InnerException?.Message ?? ex.Message;
                return error;
            }

            return error;
        }
        public async Task<ErrorDescription> ManageException(Exception exError, int code, string errorSource)
        {
            ErrorDescription error = new ErrorDescription();
            string errorMessage = string.Empty;
            try
            {
                if(exError != null)
                {
                    if (exError.Message.Contains("OuterDetails") && exError.InnerException != null)
                    {
                        errorMessage = exError.InnerException.Message;
                    }
                    else
                    {
                        errorMessage = exError.Message;
                    }
                }
                DataTable dt = await _dataManager.ManageException(errorMessage, code, errorSource);
                
                error = ExceptionMapping(dt);
                
                if(error.error_description == null)
                {
                    error.error_description = errorMessage;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                error.error_description = ex.InnerException?.Message ?? ex.Message;
                return error;
            }

            return error;
        }

        private ErrorDescription ExceptionMapping(DataTable dt)
        {
            ErrorDescription error = new ErrorDescription();
            try
            {
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    error.error_id = Convert.ToInt64(row["ERROR_ID"] == DBNull.Value ? 0 : row["ERROR_ID"]);
                    error.error_code = row["ERROR_CODE"] == DBNull.Value ? "" : row["ERROR_CODE"].ToString() ?? "";
                    error.error_description = row["ERROR_DESCRIPTION"] == DBNull.Value ? "" : row["ERROR_DESCRIPTION"].ToString()??"";
                    error.error_custom_msg = row["ERROR_CUSTOM_MSG"] == DBNull.Value ? "" : row["ERROR_CUSTOM_MSG"].ToString()??"";
                    error.error_source = row["ERROR_SOURCE"] == DBNull.Value ? "" : row["ERROR_SOURCE"].ToString()??"";
                }
            }
            catch (Exception)
            {
                throw;
            }
            return error;
        }

        public string FormatMSISDN(string msisdn)
        {
            string formattedMsisdn = "";
            try
            {
                if (string.IsNullOrEmpty(msisdn)) return "";
                formattedMsisdn = msisdn.Substring(0, 2) == FixedValueCollection.MSISDNCountryCode ? msisdn
                                                            : FixedValueCollection.MSISDNCountryCode + msisdn;
            }
            catch (Exception)
            {
                throw;
            }
            return formattedMsisdn;
        }
    }


}
