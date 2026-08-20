using BIA.DAL.DBManager;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Entity.Utility;
using BIA.Entity.ViewModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using Serilog;
using System;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BIA.DAL.Repositories;

public class DALBiometricRepo
    {
        //private readonly LogWriter _logWriter;
        private readonly OracleDataManagerV2 _oracleDataManagerV2;
        private readonly BL_Json _blJson;

        public DALBiometricRepo(OracleDataManagerV2 oracleDataManagerV2, BL_Json blJson)
        {
            //_logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
            _oracleDataManagerV2 = oracleDataManagerV2;
            _blJson = blJson;
        }
        #region ===================| Reservation Part |==================
        public async Task<bool> UpdateBioDbForReservation(string bi_token_no, string msisdn_reservation_id)
        {
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("P_BI_TOKEN_NO", OracleDbType.Varchar2, ParameterDirection.Input) { Value = bi_token_no },
            new OracleParameter("P_MSISDN_RESERVATION_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = msisdn_reservation_id }
                };

                bool result = await _oracleDataManagerV2.CallUpdateProcedure("BSS_UPDFORRESERVATION", parameters);
                return result;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "UpdateBioDbForReservation",
                    procedure_name = "BSS_UPDFORRESERVATION",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                ////_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        #endregion

        #region ===================| Error Message Part |================      

        public async Task<bool> UpdateStatusandErrorMessage(string bi_token, int status, long error_id, string error_description)
        {
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("P_BI_TOKEN_NO", OracleDbType.Varchar2, ParameterDirection.Input) { Value = bi_token },
            new OracleParameter("P_STATUS", OracleDbType.Int32, ParameterDirection.Input) { Value = status },
            new OracleParameter("P_ERROR_ID", OracleDbType.Int64, ParameterDirection.Input) { Value = error_id },
            new OracleParameter("P_ERROR_DESCRIPTION", OracleDbType.Varchar2, ParameterDirection.Input) { Value = error_description }
                };

                bool rowAffected = await _oracleDataManagerV2.CallUpdateProcedure("BSS_UPDSTATUSANDERROREMESSES", parameters);
                return rowAffected;
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "UpdateStatusandErrorMessage",
                    procedure_name = "BSS_UPDSTATUSANDERROREMESSES",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        #endregion

        #region ===================| Single Source Part |================       

        public async Task<SingleSourceCheckResponseModel> SingleSourceCheckFromBioDB(string msisdn, string sim_number, int purpose_No, string poc_number, int sim_rep_type, string dest_doc_id, string dest_dob, string dest_imsi)
        {
            DataTable dt = new DataTable();
            SingleSourceCheckResponseModel checkResponseModel = new SingleSourceCheckResponseModel();

            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = msisdn },
            new OracleParameter("P_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = sim_number },
            new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Int32, ParameterDirection.Input) { Value = purpose_No },
            new OracleParameter("P_POC_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = poc_number },
            new OracleParameter("P_SIM_REP_TYPE", OracleDbType.Int32, ParameterDirection.Input) { Value = sim_rep_type },
            new OracleParameter("P_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = dest_doc_id },
            new OracleParameter("P_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = dest_dob },
            new OracleParameter("P_IMSI", OracleDbType.Varchar2, ParameterDirection.Input) { Value = dest_imsi },
            new OracleParameter("P_SINGLESOURCE", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                dt = await _oracleDataManagerV2.SelectProcedure("BSS_CHECKSINGLESOURCE", parameters);

                if (dt.Rows.Count > 0)
                {
                    var item = dt.Rows[0];
                    checkResponseModel.Status = Convert.ToInt16(item["STATUS"]);
                    checkResponseModel.Message = item["MESSAGE"].ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "SingleSourceCheckFromBioDB",
                    procedure_name = "BSS_CHECKSINGLESOURCE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }

            return checkResponseModel;
        }

        #endregion

        #region ===================| Common Part |=======================
        public async Task<object> IsStockAvailable(int stock_id, int channel_id)
        {
            object data = new object();
            try
            {
                var parameters = new OracleParameter[]
            {
            new OracleParameter("P_STOCK_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = stock_id },
            new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = channel_id },
            };

                data = await _oracleDataManagerV2.CallSelectDataWithObjectReturn("BIA_CHECKSTOCKCHANNELMAPPING", "PO_IS_STOCK_AVAILABLE", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "IsStockAvailable",
                    procedure_name = "BIA_CHECKSTOCKCHANNELMAPPING",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                // Logging the error
                //_logWriter.WriteDailyLog2(logText);

                throw new Exception("OuterDetails: " + text, ex);
            }


            return data;
        }

        public async Task<DataTable> GetActivityLogData(int activity_type_id, string user_id)
        {
            DataTable result = new DataTable();
            try
            {
                // Adding parameters to be used in the stored procedure call
                var parameters = new OracleParameter[]
                {
            new OracleParameter("P_USER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = user_id },
            new OracleParameter("P_ORDER_ACTIVITY_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = activity_type_id },
            new OracleParameter("PO_RESULT", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("GETACTIVITYLOGDATA_1", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetActivityLogData",
                    procedure_name = "GETACTIVITYLOGDATA_1",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                // Logging the error
                //_logWriter.WriteDailyLog2(logText);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetActivityLogDataV2(int activity_type_id, string user_id)
        {
            DataTable result = new DataTable();
            try
            {
                // Adding parameters to be used in the stored procedure call
                var parameters = new OracleParameter[]
                {
            new OracleParameter("P_USER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = user_id },
            new OracleParameter("P_ORDER_ACTIVITY_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = activity_type_id },
            new OracleParameter("PO_RESULT", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("GETACTIVITYLOGDATAV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetActivityLogDataV2",
                    procedure_name = "GETACTIVITYLOGDATAV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetActivityLogDataV3(int activity_type_id, string user_id)
        {
            int isFtrFeatureOn = SettingsValues.GetisFtrFeatureOn();

            DataTable result = new DataTable();
            try
            {
                var parameters = new OracleParameter[]
                {
                    new OracleParameter("P_USER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = user_id },
                    new OracleParameter("P_ORDER_ACTIVITY_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = activity_type_id },
                    new OracleParameter("PO_RESULT", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                string storedProcedureName = isFtrFeatureOn == 1 ? "GETACTIVITYLOGDATAV7" : "GETACTIVITYLOGDATAV6";

                result = await _oracleDataManagerV2.SelectProcedure(storedProcedureName, parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetActivityLogDataV3",
                    procedure_name = isFtrFeatureOn == 1 ? "GETACTIVITYLOGDATAV7" : "GETACTIVITYLOGDATAV6",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }
        public async Task<DataTable> GetPurposeNumbers(RAGetPurposeRequest model)
        {
            DataTable result = new DataTable();
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("P_CASEID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.case_id },
            new OracleParameter("PO_PURS", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETB2BPURPOSES", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetPurposeNumbers",
                    procedure_name = "BIA_GETB2BPURPOSES",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<long> GetTokenNo(string msisdn)
        {
            long apiVersion = 0;
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = msisdn }
                };

                var result = await _oracleDataManagerV2.CallSelectDataWithObjectReturn("BI_GETTOKENNO", "PO_TOKENNO", parameters.ToArray());

                if (result != DBNull.Value && result != null)
                {
                    apiVersion = Convert.ToInt64(result);
                }
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetTokenNoAsync",
                    procedure_name = "BI_GETTOKENNO",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }

            return apiVersion;
        }

        #endregion

        #region ===================| Notification Part |==================       

        public async Task<DataTable> VarificationFinishNotification(BIAFinishNotiRequest model)
        {
            DataTable msisdnReservationIdList = new DataTable();
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("P_BSS_REQUEST_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bio_request_id },
            new OracleParameter("P_IS_SUCCESS", OracleDbType.Decimal, ParameterDirection.Input) { Value = Convert.ToDecimal(model.is_Success) },
            new OracleParameter("P_ERROR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_code },
            new OracleParameter("P_DESCRIPTION", OracleDbType.Varchar2, ParameterDirection.Input) { Value =string.IsNullOrEmpty(model.description) ? string.Empty : (model.description.Length > 1000 ? model.description.Substring(0, 1000) : model.description) },
            new OracleParameter("P_ERROR_SOURCE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_source },
            new OracleParameter("P_MSISDNRESERVATIONID", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                msisdnReservationIdList = await _oracleDataManagerV2.SelectProcedure("BIA_UPDVARIFICATIONBISTATUSV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "VarificationFinishNotificationAsync",
                    procedure_name = "BIA_UPDVARIFICATIONBISTATUSV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
            return msisdnReservationIdList;
        }

        public async Task<DataTable> GetCustomErrorMsg(decimal errorId)
        {
            DataTable errorMessageData = new DataTable();
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("P_ERRORID", OracleDbType.Decimal, ParameterDirection.Input) { Value = errorId },
            new OracleParameter("PO_ERROR_MSG", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                errorMessageData = await _oracleDataManagerV2.SelectProcedure("BIA_GTECUSTOMERRORMSG", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetCustomErrorMsg",
                    procedure_name = "BIA_GTECUSTOMERRORMSG",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
            return errorMessageData;
        }

        public async Task<DataTable> GetErrorId(string errMessage)
        {
            DataTable errorMessageData = new DataTable();
            try
            {
                var parameters = new OracleParameter[]
                {
                    new OracleParameter("P_ERROR_MSG", OracleDbType.Varchar2, ParameterDirection.Input) { Value = errMessage },
                    new OracleParameter("PO_ERROR_ID", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                errorMessageData = await _oracleDataManagerV2.SelectProcedure("BIA_GTECUSTOMERRORID", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetErrorId",
                    procedure_name = "BIA_GTECUSTOMERRORID",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
            return errorMessageData;
        }
        #endregion

        #region ===================| Division Thana Area |=================

        public async Task<DataTable> GetDivision()
        {
            DataTable result = new DataTable();
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("PO_DIVS", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("GETDIVISIONS", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetDivisionAsync",
                    procedure_name = "GETDIVISIONS",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetDistrict()
        {
            DataTable result = new DataTable();
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("PO_DISS", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("GETDISTRICTS", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetDistrict",
                    procedure_name = "GETDISTRICTS",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> GetThana()
        {
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("PO_THA", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                return await _oracleDataManagerV2.SelectProcedure("GETTHANA", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetThana",
                    procedure_name = "GETTHANA",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<DataTable> GetDivDisThana()
        {
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("PO_RESULT", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                return await _oracleDataManagerV2.SelectProcedure("GETDIVDISTHANA", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetDivDisThanaAsync",
                    procedure_name = "GETDIVDISTHANA",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }
        #endregion

        #region ===================| Log Area |=======================
        public async Task RAToDBSSLog(VMBIAToDBSSLog model)
        {
            if (model == null) return;
            try
            {
                var parameters = new OracleParameter[]
                {
                    new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bi_token_number },
                    new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                    new OracleParameter("P_BSS_REQUEST_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dbss_request_id },
                    new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number },
                    //new OracleParameter("P_USER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.username },
                    new OracleParameter("P_USER_ID", OracleDbType.Varchar2, ParameterDirection.Input)
                    {
                         Value = string.IsNullOrEmpty(model.username) ? "00000" : model.username
                    },
                    new OracleParameter("P_REQ_BLOB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.req_blob },
                    new OracleParameter("P_RES_BLOB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.res_blob },
                    new OracleParameter("P_REQ_TIME", OracleDbType.Date, ParameterDirection.Input) { Value = model.req_time },
                    new OracleParameter("P_RES_TIME", OracleDbType.Date, ParameterDirection.Input) { Value = model.res_time },
                    new OracleParameter("P_IS_SUCCESS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_success },
                    new OracleParameter("P_MESSAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.message },
                    new OracleParameter("P_ERROR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_code },
                    new OracleParameter("P_ERROR_SOURCE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_source },
                    new OracleParameter("P_METHOD_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.method_name },
                    new OracleParameter("P_INTEGRATION_POINT_FROM", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.integration_point_from },
                    new OracleParameter("P_INTEGRATION_POINT_TO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.integration_point_to },
                    new OracleParameter("P_REMARKS", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.remarks },
                    new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name }
                };

                long result = await _oracleDataManagerV2.CallInsertProcedure("BIA_LOGBIATODBSSV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "RAToDBSSLog",
                    procedure_name = "BIA_LOGBIATODBSSV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }


        public async Task RaiseCoplainLog(VMBIAToDBSSLog model)
        {
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("P_COMPLAIN_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.complain_id },
            new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.username },
            new OracleParameter("P_REQ_BLOB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.req_blob },
            new OracleParameter("P_RES_BLOB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.res_blob },
            new OracleParameter("P_REQ_TIME", OracleDbType.Date, ParameterDirection.Input) { Value = model.req_time },
            new OracleParameter("P_RES_TIME", OracleDbType.Date, ParameterDirection.Input) { Value = model.res_time },
            new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name }
                };

                var result = await _oracleDataManagerV2.CallInsertProcedure("BIA_COMPLAIN_LOG", parameters.ToArray());

            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "RaiseCoplainLog",
                    procedure_name = "BIA_COMPLAIN_LOG",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task RETtoBiometricLog(VMBIAToDBSSLog model, string requestTxt, string responseTxt)
        {
            try
            {
                var parameters = new List<OracleParameter>
        {
            new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
            new OracleParameter("P_USER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.username },
            new OracleParameter("P_REQ_BLOB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.req_blob },
            new OracleParameter("P_RES_BLOB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.res_blob },
            new OracleParameter("P_REQ_TIME", OracleDbType.Date, ParameterDirection.Input) { Value = model.req_time },
            new OracleParameter("P_RES_TIME", OracleDbType.Date, ParameterDirection.Input) { Value = model.res_time },
            new OracleParameter("P_IS_SUCCESS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_success },
            new OracleParameter("P_MESSAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.message },
            new OracleParameter("P_ERROR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_code },
            new OracleParameter("P_ERROR_SOURCE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_source },
            new OracleParameter("P_METHOD_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.method_name },
            new OracleParameter("P_REMARKS", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.remarks },
            new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name }
        };

                var result = await _oracleDataManagerV2.CallInsertProcedure("RET_STATUSUPDLOG", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "RETtoBiometricLog",
                    procedure_name = "RET_STATUSUPDLOG",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<DataTable> ManageException(string message, int code, string errorSource)
        {
            DataTable dt = new DataTable();
            try
            {
                var parameters = new List<OracleParameter>
            {
            new OracleParameter("P_MESSAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = message },
            new OracleParameter("P_CODE", OracleDbType.Int32, ParameterDirection.Input) { Value = code },
            new OracleParameter("P_ERROR_SOURCE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = errorSource },
            new OracleParameter("po_Cursor", OracleDbType.RefCursor, ParameterDirection.Output)
            };

                dt = await _oracleDataManagerV2.SelectProcedure("MANAGEEXCEPTION_BAMODULE", parameters.ToArray());
            }
            catch (Exception)
            {
                return dt;
            }
            return dt;
        }

        public async Task BALogInsert(LogModel log)
        {
            string logString = JsonConvert.SerializeObject(log);
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
        {
            new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = log.bi_token_number },
            new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = log.msisdn },
            new OracleParameter("P_BSS_REQUEST_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = log.bss_request_id },
            new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = log.purpose_number },
            new OracleParameter("P_USER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = log.user_id },
            new OracleParameter("P_REQ_BLOB", OracleDbType.Blob, ParameterDirection.Input) { Value = log.req_blob },
            new OracleParameter("P_RES_BLOB", OracleDbType.Blob, ParameterDirection.Input) { Value = log.res_blob },
            new OracleParameter("P_REQ_TIME", OracleDbType.Date, ParameterDirection.Input) { Value = log.req_time },
            new OracleParameter("P_RES_TIME", OracleDbType.Date, ParameterDirection.Input) { Value = log.res_time },
            new OracleParameter("P_IS_SUCCESS", OracleDbType.Decimal, ParameterDirection.Input) { Value = log.is_success },
            new OracleParameter("P_MESSAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = log.message },
            new OracleParameter("P_ERROR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = log.error_code },
            new OracleParameter("P_ERROR_SOURCE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = string.IsNullOrEmpty(log.error_source) ? string.Empty : (log.error_source.Length > 20 ? log.error_source.Substring(0, 20) : log.error_source) },
            new OracleParameter("P_METHOD_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = log.method_name },
            new OracleParameter("P_INTEGRATION_POINT_FROM", OracleDbType.Decimal, ParameterDirection.Input) { Value = log.integration_point_from },
            new OracleParameter("P_INTEGRATION_POINT_TO", OracleDbType.Decimal, ParameterDirection.Input) { Value = log.integration_point_to },
            new OracleParameter("P_REMARKS", OracleDbType.Varchar2, ParameterDirection.Input) { Value = string.IsNullOrEmpty(log.remarks) ? string.Empty : (log.remarks.Length > 250 ? log.remarks.Substring(0, 250) : log.remarks) },
            new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = Environment.MachineName }
        };

                long result = await _oracleDataManagerV2.CallInsertProcedure("BIA_LOGBIATODBSSV2", parameters.ToArray());

            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "BALogInsert",
                    procedure_name = "BIA_LOGBIATODBSSV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    RespData = logString,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }
        #endregion

        #region ===================| Order Req Log Area |=================
        /// <summary>
        /// FP as byte[]
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<decimal> SubmitOrder2(OrderRequest2 model)
        {
            decimal BIAReqsTokenId;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number.HasValue ? model.bi_token_number : null },
                new OracleParameter("P_BSS_ReqId", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bss_reqId },
                new OracleParameter("P_Status", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.status },
                new OracleParameter("P_error_id", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.error_id },
                new OracleParameter("p_error_description", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_description },
                new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number.HasValue ? model.purpose_number : null },
                new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                new OracleParameter("P_DEST_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_category.HasValue ? model.sim_category : null },
                new OracleParameter("P_DEST_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                new OracleParameter("P_SUBSCRIPTION_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.subscription_type_id.HasValue ? model.subscription_type_id : null },
                new OracleParameter("P_SUBSCRIPTION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_code },
                new OracleParameter("P_PACKAGE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.package_id.HasValue ? model.package_id : null },
                new OracleParameter("P_PACKAGE_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.package_code },
                new OracleParameter("P_DEST_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_doc_type_no.HasValue ? model.dest_doc_type_no : null },
                new OracleParameter("P_DEST_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_nid },
                new OracleParameter("P_DEST_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_dob },
                new OracleParameter("P_SRC_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_doc_type_no.HasValue ? model.src_doc_type_no : null },
                new OracleParameter("P_SRC_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_nid },
                new OracleParameter("P_SRC_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_dob },
                new OracleParameter("P_PLATFORM_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.platform_id },
                new OracleParameter("P_CUSTOMER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_name },
                new OracleParameter("P_GENDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.gender },
                new OracleParameter("P_FLAT_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.flat_number },
                new OracleParameter("P_HOUSE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.house_number },
                new OracleParameter("P_ROAD_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.road_number },
                new OracleParameter("P_VILLAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.village },
                new OracleParameter("P_DIVISION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.division_id.HasValue ? model.division_id : null },
                new OracleParameter("P_DISTRICT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.district_id.HasValue ? model.district_id : null },
                new OracleParameter("P_THANA_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.thana_id.HasValue ? model.thana_id : null },
                new OracleParameter("P_POSTAL_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.postal_code },
                new OracleParameter("P_EMAIL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.email },
                new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_code },
                new OracleParameter("P_DEST_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_thumb_score.HasValue ? model.dest_left_thumb_score : null },
                new OracleParameter("P_DEST_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_thumb },
                new OracleParameter("P_DEST_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_index_score.HasValue ? model.dest_left_index_score : null },
                new OracleParameter("P_DEST_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_index },
                new OracleParameter("P_DEST_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_thumb_score.HasValue ? model.dest_right_thumb_score : null },
                new OracleParameter("P_DEST_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_thumb },
                new OracleParameter("P_DEST_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_index_score.HasValue ? model.dest_right_index_score : null },
                new OracleParameter("P_DEST_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_index },
                new OracleParameter("P_SRC_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_thumb_score.HasValue ? model.src_left_thumb_score : null },
                new OracleParameter("P_SRC_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_thumb },
                new OracleParameter("P_SRC_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_index_score.HasValue ? model.src_left_index_score : null },
                new OracleParameter("P_SRC_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_index },
                new OracleParameter("P_SRC_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_thumb_score.HasValue ? model.src_right_thumb_score : null },
                new OracleParameter("P_SRC_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_thumb },
                new OracleParameter("P_SRC_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_index_score.HasValue ? model.src_right_index_score : null },
                new OracleParameter("P_SRC_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_index },
                new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                new OracleParameter("P_PORT_IN_DATE", OracleDbType.Date, ParameterDirection.Input) { Value = model.port_in_date },
                new OracleParameter("P_ALT_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.alt_msisdn },
                new OracleParameter("P_POC_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.poc_number },
                new OracleParameter("P_IS_URGENT", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_urgent.HasValue ? model.is_urgent : null },
                new OracleParameter("P_OPTIONAL1", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional1 },
                new OracleParameter("P_OPTIONAL2", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional2 },
                new OracleParameter("P_OPTIONAL3", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional3 },
                new OracleParameter("P_OPTIONAL4", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional4.HasValue ? model.optional4 : null },
                new OracleParameter("P_OPTIONAL5", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional5.HasValue ? model.optional5 : null },
                new OracleParameter("P_OPTIONAL6", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional6.HasValue ? model.optional6 : null },
                new OracleParameter("P_NOTE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.note },
                new OracleParameter("P_SIM_REP_REASON_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_rep_reason_id.HasValue ? model.sim_rep_reason_id : null },
                new OracleParameter("P_PAYMENT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.payment_type },
                new OracleParameter("P_ISPAIRED", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.is_paired : null },
                new OracleParameter("P_MSISDNRESERVATIONID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdnReservationId },
                new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.cahnnel_id : null },
                new OracleParameter("P_DIVISION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.division_name },
                new OracleParameter("P_DISTRICT_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.district_name },
                new OracleParameter("P_THANA_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.thana_name },
                new OracleParameter("P_CENTER_OR_DISTRIBUTOR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.distributor_code },
                new OracleParameter("P_SIM_REPLC_REASON", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_replc_reason },
                new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                new OracleParameter("P_RIGHT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.right_id },
                new OracleParameter("P_SIM_REPLACEMENT_TYPE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_replacement_type },
                new OracleParameter("P_OLD_SIM_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.old_sim_number },
                new OracleParameter("P_SRC_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_sim_category },
                new OracleParameter("P_PORT_IN_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.port_in_confirmation_code },
                new OracleParameter("P_DEST_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_ec_verifi_reqrd },
                new OracleParameter("P_SRC_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_ec_verifi_reqrd },
                new OracleParameter("P_DEST_FOREIGN_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_foreign_flag },
                new OracleParameter("P_DBSS_SUBSCRIPTION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dbss_subscription_id },
                new OracleParameter("P_SAF_STATUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.saf_status },
                new OracleParameter("P_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_id },
                new OracleParameter("P_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_confirmation_code },
                new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name },
                new OracleParameter("P_SRC_OWNER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_owner_customer_id },
                new OracleParameter("P_SRC_USER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_user_customer_id },
                new OracleParameter("P_SRC_PAYER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_payer_customer_id },
                new OracleParameter("P_DEST_IMSI", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_imsi },

                };
                var result = await _oracleDataManagerV2.CallInsertProcedure("SUBMITORDER5", parameters.ToArray());

                BIAReqsTokenId = Convert.ToDecimal(result);
            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "SubmitOrder2",
                    procedure_name = "SUBMITORDER5",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                ////_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);

            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "SubmitOrder2",
                    procedure_name = "SUBMITORDER5",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                ////_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return BIAReqsTokenId;
        }

        public async Task<decimal> SubmitOrderV3(OrderRequest3 model, string loginProviderId)
        {
            decimal BIAReqsTokenId;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number.HasValue ? model.bi_token_number : null },
                new OracleParameter("P_BSS_ReqId", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bss_reqId },
                new OracleParameter("P_Status", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.status },
                new OracleParameter("P_error_id", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.error_id },
                new OracleParameter("p_error_description", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_description },
                new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number.HasValue ? model.purpose_number : null },
                new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                new OracleParameter("P_DEST_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_category.HasValue ? model.sim_category : null },
                new OracleParameter("P_DEST_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                new OracleParameter("P_SUBSCRIPTION_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.subscription_type_id.HasValue ? model.subscription_type_id : null },
                new OracleParameter("P_SUBSCRIPTION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_code },
                new OracleParameter("P_PACKAGE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.package_id.HasValue ? model.package_id : null },
                new OracleParameter("P_PACKAGE_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.package_code },
                new OracleParameter("P_DEST_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_doc_type_no.HasValue ? model.dest_doc_type_no : null },
                new OracleParameter("P_DEST_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_nid },
                new OracleParameter("P_DEST_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_dob },
                new OracleParameter("P_SRC_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_doc_type_no.HasValue ? model.src_doc_type_no : null },
                new OracleParameter("P_SRC_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_nid },
                new OracleParameter("P_SRC_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_dob },
                new OracleParameter("P_PLATFORM_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.platform_id },
                new OracleParameter("P_CUSTOMER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_name },
                new OracleParameter("P_GENDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.gender },
                new OracleParameter("P_FLAT_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.flat_number },
                new OracleParameter("P_HOUSE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.house_number },
                new OracleParameter("P_ROAD_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.road_number },
                new OracleParameter("P_VILLAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.village },
                new OracleParameter("P_DIVISION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.division_id.HasValue ? model.division_id : null },
                new OracleParameter("P_DISTRICT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.district_id.HasValue ? model.district_id : null },
                new OracleParameter("P_THANA_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.thana_id.HasValue ? model.thana_id : null },
                new OracleParameter("P_POSTAL_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.postal_code },
                new OracleParameter("P_EMAIL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.email },
                new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_code },
                new OracleParameter("P_DEST_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_thumb_score.HasValue ? model.dest_left_thumb_score : null },
                new OracleParameter("P_DEST_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_thumb },
                new OracleParameter("P_DEST_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_index_score.HasValue ? model.dest_left_index_score : null },
                new OracleParameter("P_DEST_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_index },
                new OracleParameter("P_DEST_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_thumb_score.HasValue ? model.dest_right_thumb_score : null },
                new OracleParameter("P_DEST_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_thumb },
                new OracleParameter("P_DEST_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_index_score.HasValue ? model.dest_right_index_score : null },
                new OracleParameter("P_DEST_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_index },
                new OracleParameter("P_SRC_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_thumb_score.HasValue ? model.src_left_thumb_score : null },
                new OracleParameter("P_SRC_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_thumb },
                new OracleParameter("P_SRC_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_index_score.HasValue ? model.src_left_index_score : null },
                new OracleParameter("P_SRC_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_index },
                new OracleParameter("P_SRC_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_thumb_score.HasValue ? model.src_right_thumb_score : null },
                new OracleParameter("P_SRC_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_thumb },
                new OracleParameter("P_SRC_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_index_score.HasValue ? model.src_right_index_score : null },
                new OracleParameter("P_SRC_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_index },
                new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                new OracleParameter("P_PORT_IN_DATE", OracleDbType.Date, ParameterDirection.Input) { Value = model.port_in_date },
                new OracleParameter("P_ALT_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.alt_msisdn },
                new OracleParameter("P_POC_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.poc_number },
                new OracleParameter("P_IS_URGENT", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_urgent.HasValue ? model.is_urgent : null },
                new OracleParameter("P_OPTIONAL1", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional1 },
                new OracleParameter("P_OPTIONAL2", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional2 },
                new OracleParameter("P_OPTIONAL3", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional3 },
                new OracleParameter("P_OPTIONAL4", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional4.HasValue ? model.optional4 : null },
                new OracleParameter("P_OPTIONAL5", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional5.HasValue ? model.optional5 : null },
                new OracleParameter("P_OPTIONAL6", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional6.HasValue ? model.optional6 : null },
                new OracleParameter("P_NOTE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.note },
                new OracleParameter("P_SIM_REP_REASON_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_rep_reason_id.HasValue ? model.sim_rep_reason_id : null },
                new OracleParameter("P_PAYMENT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.payment_type },
                new OracleParameter("P_ISPAIRED", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.is_paired : null },
                new OracleParameter("P_MSISDNRESERVATIONID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdnReservationId },
                new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.cahnnel_id : null },
                new OracleParameter("P_DIVISION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.division_name },
                new OracleParameter("P_DISTRICT_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.district_name },
                new OracleParameter("P_THANA_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.thana_name },
                new OracleParameter("P_CENTER_OR_DISTRIBUTOR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.distributor_code },
                new OracleParameter("P_SIM_REPLC_REASON", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_replc_reason },
                new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                new OracleParameter("P_RIGHT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.right_id },
                new OracleParameter("P_SIM_REPLACEMENT_TYPE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_replacement_type },
                new OracleParameter("P_OLD_SIM_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.old_sim_number },
                new OracleParameter("P_SRC_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_sim_category },
                new OracleParameter("P_PORT_IN_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.port_in_confirmation_code },
                new OracleParameter("P_DEST_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_ec_verifi_reqrd },
                new OracleParameter("P_SRC_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_ec_verifi_reqrd },
                new OracleParameter("P_DEST_FOREIGN_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_foreign_flag },
                new OracleParameter("P_DBSS_SUBSCRIPTION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dbss_subscription_id },
                new OracleParameter("P_SAF_STATUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.saf_status },
                new OracleParameter("P_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_id },
                new OracleParameter("P_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_confirmation_code },
                new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name },
                new OracleParameter("P_SRC_OWNER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_owner_customer_id },
                new OracleParameter("P_SRC_USER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_user_customer_id },
                new OracleParameter("P_SRC_PAYER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_payer_customer_id },
                new OracleParameter("P_DEST_IMSI", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_imsi },
                new OracleParameter("P_SESSION_TOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = loginProviderId },
                new OracleParameter("P_LATITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.latitude },
                new OracleParameter("P_LONGITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.longitude },
                new OracleParameter("P_LAC_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.lac },
                new OracleParameter("P_CELL_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.cid },
                };

                var result = await _oracleDataManagerV2.CallInsertProcedure("SUBMITORDER6", parameters.ToArray());

                BIAReqsTokenId = Convert.ToDecimal(result);
            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "SubmitOrderV3",
                    procedure_name = "SUBMITORDER6",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "SubmitOrderV3",
                    procedure_name = "SUBMITORDER6",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return BIAReqsTokenId;
        }

        public async Task<decimal> SubmitOrderV4(OrderRequest3 model)
        {
            //_oracleDataManager = new OracleDataManager();
            decimal BIAReqsTokenId;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number.HasValue ? model.bi_token_number : null },
                new OracleParameter("P_BSS_ReqId", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bss_reqId },
                new OracleParameter("P_Status", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.status },
                new OracleParameter("P_error_id", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.error_id },
                new OracleParameter("p_error_description", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_description },
                new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number.HasValue ? model.purpose_number : null },
                new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                new OracleParameter("P_DEST_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_category.HasValue ? model.sim_category : null },
                new OracleParameter("P_DEST_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                new OracleParameter("P_SUBSCRIPTION_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.subscription_type_id.HasValue ? model.subscription_type_id : null },
                new OracleParameter("P_SUBSCRIPTION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_code },
                new OracleParameter("P_PACKAGE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.package_id.HasValue ? model.package_id : null },
                new OracleParameter("P_PACKAGE_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.package_code },
                new OracleParameter("P_DEST_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_doc_type_no.HasValue ? model.dest_doc_type_no : null },
                new OracleParameter("P_DEST_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_nid },
                new OracleParameter("P_DEST_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_dob },
                new OracleParameter("P_SRC_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_doc_type_no.HasValue ? model.src_doc_type_no : null },
                new OracleParameter("P_SRC_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_nid },
                new OracleParameter("P_SRC_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_dob },
                new OracleParameter("P_PLATFORM_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.platform_id },
                new OracleParameter("P_CUSTOMER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_name },
                new OracleParameter("P_GENDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.gender },
                new OracleParameter("P_FLAT_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.flat_number },
                new OracleParameter("P_HOUSE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.house_number },
                new OracleParameter("P_ROAD_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.road_number },
                new OracleParameter("P_VILLAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.village },
                new OracleParameter("P_DIVISION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.division_id.HasValue ? model.division_id : null },
                new OracleParameter("P_DISTRICT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.district_id.HasValue ? model.district_id : null },
                new OracleParameter("P_THANA_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.thana_id.HasValue ? model.thana_id : null },
                new OracleParameter("P_POSTAL_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.postal_code },
                new OracleParameter("P_EMAIL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.email },
                new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_code },
                new OracleParameter("P_DEST_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_thumb_score.HasValue ? model.dest_left_thumb_score : null },
                new OracleParameter("P_DEST_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_thumb },
                new OracleParameter("P_DEST_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_index_score.HasValue ? model.dest_left_index_score : null },
                new OracleParameter("P_DEST_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_index },
                new OracleParameter("P_DEST_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_thumb_score.HasValue ? model.dest_right_thumb_score : null },
                new OracleParameter("P_DEST_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_thumb },
                new OracleParameter("P_DEST_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_index_score.HasValue ? model.dest_right_index_score : null },
                new OracleParameter("P_DEST_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_index },
                new OracleParameter("P_SRC_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_thumb_score.HasValue ? model.src_left_thumb_score : null },
                new OracleParameter("P_SRC_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_thumb },
                new OracleParameter("P_SRC_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_index_score.HasValue ? model.src_left_index_score : null },
                new OracleParameter("P_SRC_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_index },
                new OracleParameter("P_SRC_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_thumb_score.HasValue ? model.src_right_thumb_score : null },
                new OracleParameter("P_SRC_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_thumb },
                new OracleParameter("P_SRC_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_index_score.HasValue ? model.src_right_index_score : null },
                new OracleParameter("P_SRC_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_index },
                new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                new OracleParameter("P_PORT_IN_DATE", OracleDbType.Date, ParameterDirection.Input) { Value = model.port_in_date },
                new OracleParameter("P_ALT_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.alt_msisdn },
                new OracleParameter("P_POC_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.poc_number },
                new OracleParameter("P_IS_URGENT", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_urgent.HasValue ? model.is_urgent : null },
                new OracleParameter("P_OPTIONAL1", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional1 },
                new OracleParameter("P_OPTIONAL2", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional2 },
                new OracleParameter("P_OPTIONAL3", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional3 },
                new OracleParameter("P_OPTIONAL4", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional4.HasValue ? model.optional4 : null },
                new OracleParameter("P_OPTIONAL5", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional5.HasValue ? model.optional5 : null },
                new OracleParameter("P_OPTIONAL6", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional6.HasValue ? model.optional6 : null },
                new OracleParameter("P_NOTE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.note },
                new OracleParameter("P_SIM_REP_REASON_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_rep_reason_id.HasValue ? model.sim_rep_reason_id : null },
                new OracleParameter("P_PAYMENT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.payment_type },
                new OracleParameter("P_ISPAIRED", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.is_paired : null },
                new OracleParameter("P_MSISDNRESERVATIONID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdnReservationId },
                new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.cahnnel_id : null },
                new OracleParameter("P_DIVISION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.division_name },
                new OracleParameter("P_DISTRICT_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.district_name },
                new OracleParameter("P_THANA_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.thana_name },
                new OracleParameter("P_CENTER_OR_DISTRIBUTOR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.distributor_code },
                new OracleParameter("P_SIM_REPLC_REASON", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_replc_reason },
                new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                new OracleParameter("P_RIGHT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.right_id },
                new OracleParameter("P_SIM_REPLACEMENT_TYPE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_replacement_type },
                new OracleParameter("P_OLD_SIM_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.old_sim_number },
                new OracleParameter("P_SRC_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_sim_category },
                new OracleParameter("P_PORT_IN_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.port_in_confirmation_code },
                new OracleParameter("P_DEST_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_ec_verifi_reqrd },
                new OracleParameter("P_SRC_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_ec_verifi_reqrd },
                new OracleParameter("P_DEST_FOREIGN_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_foreign_flag },
                new OracleParameter("P_DBSS_SUBSCRIPTION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dbss_subscription_id },
                new OracleParameter("P_SAF_STATUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.saf_status },
                new OracleParameter("P_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_id },
                new OracleParameter("P_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_confirmation_code },
                new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name },
                new OracleParameter("P_SRC_OWNER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_owner_customer_id },
                new OracleParameter("P_SRC_USER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_user_customer_id },
                new OracleParameter("P_SRC_PAYER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_payer_customer_id },
                new OracleParameter("P_DEST_IMSI", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_imsi },
                new OracleParameter("P_SESSION_TOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.prov_id },
                new OracleParameter("P_LATITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.latitude },
                new OracleParameter("P_LONGITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.longitude },
                new OracleParameter("P_LAC_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.lac },
                new OracleParameter("P_CELL_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.cid },
                new OracleParameter("P_SCANNER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.scanner_id },
                };
                var result = await _oracleDataManagerV2.CallInsertProcedure("SUBMITORDER7", parameters.ToArray());

                BIAReqsTokenId = Convert.ToDecimal(result);
            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    retailer_id = model.retailer_id,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV4",
                    procedure_name = "SUBMITORDER7",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    retailer_id = model.retailer_id,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV4",
                    procedure_name = "SUBMITORDER7",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return BIAReqsTokenId;
        }

        public async Task<decimal> SubmitOrderV5(OrderRequest3 model, string loginProviderId)
        {
            //_oracleDataManager = new OracleDataManager();
            decimal BIAReqsTokenId;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                   new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number.HasValue ? model.bi_token_number : null },
                   new OracleParameter("P_BSS_ReqId", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bss_reqId },
                   new OracleParameter("P_Status", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.status },
                   new OracleParameter("P_error_id", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.error_id },
                   new OracleParameter("p_error_description", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_description },
                   new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number.HasValue ? model.purpose_number : null },
                   new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                   new OracleParameter("P_DEST_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_category.HasValue ? model.sim_category : null },
                   new OracleParameter("P_DEST_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                   new OracleParameter("P_SUBSCRIPTION_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.subscription_type_id.HasValue ? model.subscription_type_id : null },
                   new OracleParameter("P_SUBSCRIPTION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_code },
                   new OracleParameter("P_PACKAGE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.package_id.HasValue ? model.package_id : null },
                   new OracleParameter("P_PACKAGE_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.package_code },
                   new OracleParameter("P_DEST_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_doc_type_no.HasValue ? model.dest_doc_type_no : null },
                   new OracleParameter("P_DEST_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_nid },
                   new OracleParameter("P_DEST_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_dob },
                   new OracleParameter("P_SRC_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_doc_type_no.HasValue ? model.src_doc_type_no : null },
                   new OracleParameter("P_SRC_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_nid },
                   new OracleParameter("P_SRC_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_dob },
                   new OracleParameter("P_PLATFORM_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.platform_id },
                   new OracleParameter("P_CUSTOMER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_name },
                   new OracleParameter("P_GENDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.gender },
                   new OracleParameter("P_FLAT_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.flat_number },
                   new OracleParameter("P_HOUSE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.house_number },
                   new OracleParameter("P_ROAD_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.road_number },
                   new OracleParameter("P_VILLAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.village },
                   new OracleParameter("P_DIVISION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.division_id.HasValue ? model.division_id : null },
                   new OracleParameter("P_DISTRICT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.district_id.HasValue ? model.district_id : null },
                   new OracleParameter("P_THANA_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.thana_id.HasValue ? model.thana_id : null },
                   new OracleParameter("P_POSTAL_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.postal_code },
                   new OracleParameter("P_EMAIL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.email },
                   new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_code },
                   new OracleParameter("P_DEST_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_thumb_score.HasValue ? model.dest_left_thumb_score : null },
                   new OracleParameter("P_DEST_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_thumb },
                   new OracleParameter("P_DEST_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_index_score.HasValue ? model.dest_left_index_score : null },
                   new OracleParameter("P_DEST_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_index },
                   new OracleParameter("P_DEST_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_thumb_score.HasValue ? model.dest_right_thumb_score : null },
                   new OracleParameter("P_DEST_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_thumb },
                   new OracleParameter("P_DEST_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_index_score.HasValue ? model.dest_right_index_score : null },
                   new OracleParameter("P_DEST_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_index },
                   new OracleParameter("P_SRC_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_thumb_score.HasValue ? model.src_left_thumb_score : null },
                   new OracleParameter("P_SRC_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_thumb },
                   new OracleParameter("P_SRC_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_index_score.HasValue ? model.src_left_index_score : null },
                   new OracleParameter("P_SRC_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_index },
                   new OracleParameter("P_SRC_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_thumb_score.HasValue ? model.src_right_thumb_score : null },
                   new OracleParameter("P_SRC_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_thumb },
                   new OracleParameter("P_SRC_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_index_score.HasValue ? model.src_right_index_score : null },
                   new OracleParameter("P_SRC_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_index },
                   new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                   new OracleParameter("P_PORT_IN_DATE", OracleDbType.Date, ParameterDirection.Input) { Value = model.port_in_date },
                   new OracleParameter("P_ALT_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.alt_msisdn },
                   new OracleParameter("P_POC_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.poc_number },
                   new OracleParameter("P_IS_URGENT", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_urgent.HasValue ? model.is_urgent : null },
                   new OracleParameter("P_OPTIONAL1", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional1 },
                   new OracleParameter("P_OPTIONAL2", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional2 },
                   new OracleParameter("P_OPTIONAL3", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional3 },
                   new OracleParameter("P_OPTIONAL4", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional4.HasValue ? model.optional4 : null },
                   new OracleParameter("P_OPTIONAL5", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional5.HasValue ? model.optional5 : null },
                   new OracleParameter("P_OPTIONAL6", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional6.HasValue ? model.optional6 : null },
                   new OracleParameter("P_NOTE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.note },
                   new OracleParameter("P_SIM_REP_REASON_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_rep_reason_id.HasValue ? model.sim_rep_reason_id : null },
                   new OracleParameter("P_PAYMENT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.payment_type },
                   new OracleParameter("P_ISPAIRED", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.is_paired : null },
                   new OracleParameter("P_MSISDNRESERVATIONID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdnReservationId },
                   new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.cahnnel_id : null },
                   new OracleParameter("P_DIVISION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.division_name },
                   new OracleParameter("P_DISTRICT_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.district_name },
                   new OracleParameter("P_THANA_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.thana_name },
                   new OracleParameter("P_CENTER_OR_DISTRIBUTOR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.distributor_code },
                   new OracleParameter("P_SIM_REPLC_REASON", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_replc_reason },
                   new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                   new OracleParameter("P_RIGHT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.right_id },
                   new OracleParameter("P_SIM_REPLACEMENT_TYPE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_replacement_type },
                   new OracleParameter("P_OLD_SIM_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.old_sim_number },
                   new OracleParameter("P_SRC_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_sim_category },
                   new OracleParameter("P_PORT_IN_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.port_in_confirmation_code },
                   new OracleParameter("P_DEST_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_ec_verifi_reqrd },
                   new OracleParameter("P_SRC_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_ec_verifi_reqrd },
                   new OracleParameter("P_DEST_FOREIGN_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_foreign_flag },
                   new OracleParameter("P_DBSS_SUBSCRIPTION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dbss_subscription_id },
                   new OracleParameter("P_SAF_STATUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.saf_status },
                   new OracleParameter("P_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_id },
                   new OracleParameter("P_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_confirmation_code },
                   new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name },
                   new OracleParameter("P_SRC_OWNER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_owner_customer_id },
                   new OracleParameter("P_SRC_USER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_user_customer_id },
                   new OracleParameter("P_SRC_PAYER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_payer_customer_id },
                   new OracleParameter("P_DEST_IMSI", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_imsi },
                   new OracleParameter("P_SESSION_TOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = loginProviderId },
                   new OracleParameter("P_LATITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.latitude },
                   new OracleParameter("P_LONGITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.longitude },
                   new OracleParameter("P_LAC_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.lac },
                   new OracleParameter("P_CELL_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.cid },
                   new OracleParameter("P_SCANNER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.scanner_id },
                };
                var result = await _oracleDataManagerV2.CallInsertProcedure("SUBMITORDER7", parameters.ToArray());

                BIAReqsTokenId = Convert.ToDecimal(result);
            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    retailer_id = model.retailer_id,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV5",
                    procedure_name = "SUBMITORDER7",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    retailer_id = model.retailer_id,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV5",
                    procedure_name = "SUBMITORDER7",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return BIAReqsTokenId;
        }

        public async Task<DataTable> SubmitOrderRegistrationReq(OrderRequest3 model, int isregrequest)
        {
            DataTable orderResponse = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number.HasValue ? model.bi_token_number : null },
                    new OracleParameter("P_BSS_ReqId", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bss_reqId },
                    new OracleParameter("P_Status", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.status },
                    new OracleParameter("P_error_id", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.error_id },
                    new OracleParameter("p_error_description", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_description },
                    new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number.HasValue ? model.purpose_number : null },
                    new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                    new OracleParameter("P_DEST_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_category.HasValue ? model.sim_category : null },
                    new OracleParameter("P_DEST_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                    new OracleParameter("P_SUBSCRIPTION_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.subscription_type_id.HasValue ? model.subscription_type_id : null },
                    new OracleParameter("P_SUBSCRIPTION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_code },
                    new OracleParameter("P_PACKAGE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.package_id.HasValue ? model.package_id : null },
                    new OracleParameter("P_PACKAGE_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.package_code },
                    new OracleParameter("P_DEST_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_doc_type_no.HasValue ? model.dest_doc_type_no : null },
                    new OracleParameter("P_DEST_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_nid },
                    new OracleParameter("P_DEST_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_dob },
                    new OracleParameter("P_SRC_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_doc_type_no.HasValue ? model.src_doc_type_no : null },
                    new OracleParameter("P_SRC_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_nid },
                    new OracleParameter("P_SRC_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_dob },
                    new OracleParameter("P_PLATFORM_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.platform_id },
                    new OracleParameter("P_CUSTOMER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_name },
                    new OracleParameter("P_GENDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.gender },
                    new OracleParameter("P_FLAT_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.flat_number },
                    new OracleParameter("P_HOUSE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.house_number },
                    new OracleParameter("P_ROAD_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.road_number },
                    new OracleParameter("P_VILLAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.village },
                    new OracleParameter("P_DIVISION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.division_id.HasValue ? model.division_id : null },
                    new OracleParameter("P_DISTRICT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.district_id.HasValue ? model.district_id : null },
                    new OracleParameter("P_THANA_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.thana_id.HasValue ? model.thana_id : null },
                    new OracleParameter("P_POSTAL_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.postal_code },
                    new OracleParameter("P_EMAIL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.email },
                    new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_code },
                    new OracleParameter("P_DEST_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_thumb_score.HasValue ? model.dest_left_thumb_score : null },
                    new OracleParameter("P_DEST_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_thumb },
                    new OracleParameter("P_DEST_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_index_score.HasValue ? model.dest_left_index_score : null },
                    new OracleParameter("P_DEST_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_index },
                    new OracleParameter("P_DEST_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_thumb_score.HasValue ? model.dest_right_thumb_score : null },
                    new OracleParameter("P_DEST_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_thumb },
                    new OracleParameter("P_DEST_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_index_score.HasValue ? model.dest_right_index_score : null },
                    new OracleParameter("P_DEST_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_index },
                    new OracleParameter("P_SRC_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_thumb_score.HasValue ? model.src_left_thumb_score : null },
                    new OracleParameter("P_SRC_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_thumb },
                    new OracleParameter("P_SRC_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_index_score.HasValue ? model.src_left_index_score : null },
                    new OracleParameter("P_SRC_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_index },
                    new OracleParameter("P_SRC_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_thumb_score.HasValue ? model.src_right_thumb_score : null },
                    new OracleParameter("P_SRC_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_thumb },
                    new OracleParameter("P_SRC_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_index_score.HasValue ? model.src_right_index_score : null },
                    new OracleParameter("P_SRC_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_index },
                    new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                    new OracleParameter("P_PORT_IN_DATE", OracleDbType.Date, ParameterDirection.Input) { Value = model.port_in_date },
                    new OracleParameter("P_ALT_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.alt_msisdn },
                    new OracleParameter("P_POC_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.poc_number },
                    new OracleParameter("P_IS_URGENT", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_urgent.HasValue ? model.is_urgent : null },
                    new OracleParameter("P_OPTIONAL1", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional1 },
                    new OracleParameter("P_OPTIONAL2", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional2 },
                    new OracleParameter("P_OPTIONAL3", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional3 },
                    new OracleParameter("P_OPTIONAL4", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional4.HasValue ? model.optional4 : null },
                    new OracleParameter("P_OPTIONAL5", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional5.HasValue ? model.optional5 : null },
                    new OracleParameter("P_OPTIONAL6", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional6.HasValue ? model.optional6 : null },
                    new OracleParameter("P_NOTE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.note },
                    new OracleParameter("P_SIM_REP_REASON_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_rep_reason_id.HasValue ? model.sim_rep_reason_id : null },
                    new OracleParameter("P_PAYMENT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.payment_type },
                    new OracleParameter("P_ISPAIRED", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.is_paired : null },
                    new OracleParameter("P_MSISDNRESERVATIONID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdnReservationId },
                    new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.cahnnel_id : null },
                    new OracleParameter("P_DIVISION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.division_name },
                    new OracleParameter("P_DISTRICT_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.district_name },
                    new OracleParameter("P_THANA_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.thana_name },
                    new OracleParameter("P_CENTER_OR_DISTRIBUTOR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.distributor_code },
                    new OracleParameter("P_SIM_REPLC_REASON", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_replc_reason },
                    new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("P_RIGHT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.right_id },
                    new OracleParameter("P_SIM_REPLACEMENT_TYPE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_replacement_type },
                    new OracleParameter("P_OLD_SIM_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.old_sim_number },
                    new OracleParameter("P_SRC_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_sim_category },
                    new OracleParameter("P_PORT_IN_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.port_in_confirmation_code },
                    new OracleParameter("P_DEST_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_ec_verifi_reqrd },
                    new OracleParameter("P_SRC_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_ec_verifi_reqrd },
                    new OracleParameter("P_DEST_FOREIGN_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_foreign_flag },
                    new OracleParameter("P_DBSS_SUBSCRIPTION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dbss_subscription_id },
                    new OracleParameter("P_SAF_STATUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.saf_status },
                    new OracleParameter("P_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_id },
                    new OracleParameter("P_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_confirmation_code },
                    new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name },
                    new OracleParameter("P_SRC_OWNER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_owner_customer_id },
                    new OracleParameter("P_SRC_USER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_user_customer_id },
                    new OracleParameter("P_SRC_PAYER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_payer_customer_id },
                    new OracleParameter("P_DEST_IMSI", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_imsi },
                    new OracleParameter("P_SESSION_TOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.prov_id },
                    new OracleParameter("P_LATITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.latitude },
                    new OracleParameter("P_LONGITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.longitude },
                    new OracleParameter("P_LAC_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.lac },
                    new OracleParameter("P_CELL_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.cid },
                    new OracleParameter("P_SCANNER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.scanner_id },
                    new OracleParameter("P_ORDER_BOOKING_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.order_booking_flag },
                    new OracleParameter("P_IS_ESIM", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_esim },
                    new OracleParameter("P_IS_REGREQUEST", OracleDbType.Decimal, ParameterDirection.Input) { Value = isregrequest },
                };
                orderResponse = await _oracleDataManagerV2.SelectProcedureV2("SUBMITORDER14", parameters.ToArray());

            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV6",
                    procedure_name = "SUBMITORDER12",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);

            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderRegistrationReq",
                    procedure_name = "SUBMITORDER15",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return orderResponse;
        }


        public async Task<DataTable> SubmitOrderV6(OrderRequest3 model)
        {
            DataTable orderResponse = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number.HasValue ? model.bi_token_number : null },
                new OracleParameter("P_BSS_ReqId", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bss_reqId },
                new OracleParameter("P_Status", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.status },
                new OracleParameter("P_error_id", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.error_id },
                new OracleParameter("p_error_description", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_description },
                new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number.HasValue ? model.purpose_number : null },
                new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                new OracleParameter("P_DEST_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_category.HasValue ? model.sim_category : null },
                new OracleParameter("P_DEST_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                new OracleParameter("P_SUBSCRIPTION_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.subscription_type_id.HasValue ? model.subscription_type_id : null },
                new OracleParameter("P_SUBSCRIPTION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_code },
                new OracleParameter("P_PACKAGE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.package_id.HasValue ? model.package_id : null },
                new OracleParameter("P_PACKAGE_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.package_code },
                new OracleParameter("P_DEST_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_doc_type_no.HasValue ? model.dest_doc_type_no : null },
                new OracleParameter("P_DEST_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_nid },
                new OracleParameter("P_DEST_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_dob },
                new OracleParameter("P_SRC_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_doc_type_no.HasValue ? model.src_doc_type_no : null },
                new OracleParameter("P_SRC_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_nid },
                new OracleParameter("P_SRC_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_dob },
                new OracleParameter("P_PLATFORM_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.platform_id },
                new OracleParameter("P_CUSTOMER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_name },
                new OracleParameter("P_GENDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.gender },
                new OracleParameter("P_FLAT_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.flat_number },
                new OracleParameter("P_HOUSE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.house_number },
                new OracleParameter("P_ROAD_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.road_number },
                new OracleParameter("P_VILLAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.village },
                new OracleParameter("P_DIVISION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.division_id.HasValue ? model.division_id : null },
                new OracleParameter("P_DISTRICT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.district_id.HasValue ? model.district_id : null },
                new OracleParameter("P_THANA_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.thana_id.HasValue ? model.thana_id : null },
                new OracleParameter("P_POSTAL_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.postal_code },
                new OracleParameter("P_EMAIL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.email },
                new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_code },
                new OracleParameter("P_DEST_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_thumb_score.HasValue ? model.dest_left_thumb_score : null },
                new OracleParameter("P_DEST_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_thumb },
                new OracleParameter("P_DEST_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_index_score.HasValue ? model.dest_left_index_score : null },
                new OracleParameter("P_DEST_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_index },
                new OracleParameter("P_DEST_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_thumb_score.HasValue ? model.dest_right_thumb_score : null },
                new OracleParameter("P_DEST_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_thumb },
                new OracleParameter("P_DEST_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_index_score.HasValue ? model.dest_right_index_score : null },
                new OracleParameter("P_DEST_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_index },
                new OracleParameter("P_SRC_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_thumb_score.HasValue ? model.src_left_thumb_score : null },
                new OracleParameter("P_SRC_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_thumb },
                new OracleParameter("P_SRC_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_index_score.HasValue ? model.src_left_index_score : null },
                new OracleParameter("P_SRC_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_index },
                new OracleParameter("P_SRC_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_thumb_score.HasValue ? model.src_right_thumb_score : null },
                new OracleParameter("P_SRC_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_thumb },
                new OracleParameter("P_SRC_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_index_score.HasValue ? model.src_right_index_score : null },
                new OracleParameter("P_SRC_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_index },
                new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                new OracleParameter("P_PORT_IN_DATE", OracleDbType.Date, ParameterDirection.Input) { Value = model.port_in_date },
                new OracleParameter("P_ALT_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.alt_msisdn },
                new OracleParameter("P_POC_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.poc_number },
                new OracleParameter("P_IS_URGENT", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_urgent.HasValue ? model.is_urgent : null },
                new OracleParameter("P_OPTIONAL1", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional1 },
                new OracleParameter("P_OPTIONAL2", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional2 },
                new OracleParameter("P_OPTIONAL3", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional3 },
                new OracleParameter("P_OPTIONAL4", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional4.HasValue ? model.optional4 : null },
                new OracleParameter("P_OPTIONAL5", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional5.HasValue ? model.optional5 : null },
                new OracleParameter("P_OPTIONAL6", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional6.HasValue ? model.optional6 : null },
                new OracleParameter("P_NOTE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.note },
                new OracleParameter("P_SIM_REP_REASON_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_rep_reason_id.HasValue ? model.sim_rep_reason_id : null },
                new OracleParameter("P_PAYMENT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.payment_type },
                new OracleParameter("P_ISPAIRED", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.is_paired : null },
                new OracleParameter("P_MSISDNRESERVATIONID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdnReservationId },
                new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.cahnnel_id : null },
                new OracleParameter("P_DIVISION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.division_name },
                new OracleParameter("P_DISTRICT_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.district_name },
                new OracleParameter("P_THANA_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.thana_name },
                new OracleParameter("P_CENTER_OR_DISTRIBUTOR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.distributor_code },
                new OracleParameter("P_SIM_REPLC_REASON", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_replc_reason },
                new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                new OracleParameter("P_RIGHT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.right_id },
                new OracleParameter("P_SIM_REPLACEMENT_TYPE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_replacement_type },
                new OracleParameter("P_OLD_SIM_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.old_sim_number },
                new OracleParameter("P_SRC_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_sim_category },
                new OracleParameter("P_PORT_IN_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.port_in_confirmation_code },
                new OracleParameter("P_DEST_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_ec_verifi_reqrd },
                new OracleParameter("P_SRC_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_ec_verifi_reqrd },
                new OracleParameter("P_DEST_FOREIGN_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_foreign_flag },
                new OracleParameter("P_DBSS_SUBSCRIPTION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dbss_subscription_id },
                new OracleParameter("P_SAF_STATUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.saf_status },
                new OracleParameter("P_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_id },
                new OracleParameter("P_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_confirmation_code },
                new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name },
                new OracleParameter("P_SRC_OWNER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_owner_customer_id },
                new OracleParameter("P_SRC_USER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_user_customer_id },
                new OracleParameter("P_SRC_PAYER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_payer_customer_id },
                new OracleParameter("P_DEST_IMSI", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_imsi },
                new OracleParameter("P_SESSION_TOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.prov_id },
                new OracleParameter("P_LATITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.latitude },
                new OracleParameter("P_LONGITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.longitude },
                new OracleParameter("P_LAC_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.lac },
                new OracleParameter("P_CELL_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.cid },
                new OracleParameter("P_SCANNER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.scanner_id },
                new OracleParameter("P_ORDER_BOOKING_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.order_booking_flag },
                new OracleParameter("P_IS_ESIM", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_esim },
                };

                orderResponse = await _oracleDataManagerV2.SelectProcedureV2("SUBMITORDER12", parameters.ToArray());
            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV6",
                    procedure_name = "SUBMITORDER12",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);

            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV6",
                    procedure_name = "SUBMITORDER12",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return orderResponse;
        }

        public async Task<DataTable> HomeWifiSubmitOrderV2(HomeWifiOrderRequest2 model)
        {
            DataTable orderResponse = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number.HasValue ? model.bi_token_number : null },
                new OracleParameter("P_BSS_ReqId", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bss_reqId },
                new OracleParameter("P_Status", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.status },
                new OracleParameter("P_error_id", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.error_id },
                new OracleParameter("p_error_description", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_description },
                new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number.HasValue ? model.purpose_number : null },
                new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                new OracleParameter("P_DEST_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_category.HasValue ? model.sim_category : null },
                new OracleParameter("P_DEST_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                new OracleParameter("P_SUBSCRIPTION_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.subscription_type_id.HasValue ? model.subscription_type_id : null },
                new OracleParameter("P_SUBSCRIPTION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_code },
                new OracleParameter("P_PACKAGE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.package_id.HasValue ? model.package_id : null },
                new OracleParameter("P_PACKAGE_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.package_code },
                new OracleParameter("P_DEST_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_doc_type_no.HasValue ? model.dest_doc_type_no : null },
                new OracleParameter("P_DEST_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_nid },
                new OracleParameter("P_DEST_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_dob },
                new OracleParameter("P_SRC_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_doc_type_no.HasValue ? model.src_doc_type_no : null },
                new OracleParameter("P_SRC_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_nid },
                new OracleParameter("P_SRC_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_dob },
                new OracleParameter("P_PLATFORM_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.platform_id },
                new OracleParameter("P_CUSTOMER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_name },
                new OracleParameter("P_GENDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.gender },
                new OracleParameter("P_FLAT_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.flat_number },
                new OracleParameter("P_HOUSE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.house_number },
                new OracleParameter("P_ROAD_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.road_number },
                new OracleParameter("P_VILLAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.village },
                new OracleParameter("P_DIVISION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.division_id.HasValue ? model.division_id : null },
                new OracleParameter("P_DISTRICT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.district_id.HasValue ? model.district_id : null },
                new OracleParameter("P_THANA_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.thana_id.HasValue ? model.thana_id : null },
                new OracleParameter("P_POSTAL_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.postal_code },
                new OracleParameter("P_EMAIL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.email },
                new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_code },
                new OracleParameter("P_DEST_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_thumb_score.HasValue ? model.dest_left_thumb_score : null },
                new OracleParameter("P_DEST_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_thumb },
                new OracleParameter("P_DEST_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_index_score.HasValue ? model.dest_left_index_score : null },
                new OracleParameter("P_DEST_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_index },
                new OracleParameter("P_DEST_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_thumb_score.HasValue ? model.dest_right_thumb_score : null },
                new OracleParameter("P_DEST_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_thumb },
                new OracleParameter("P_DEST_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_index_score.HasValue ? model.dest_right_index_score : null },
                new OracleParameter("P_DEST_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_index },
                new OracleParameter("P_SRC_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_thumb_score.HasValue ? model.src_left_thumb_score : null },
                new OracleParameter("P_SRC_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_thumb },
                new OracleParameter("P_SRC_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_index_score.HasValue ? model.src_left_index_score : null },
                new OracleParameter("P_SRC_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_index },
                new OracleParameter("P_SRC_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_thumb_score.HasValue ? model.src_right_thumb_score : null },
                new OracleParameter("P_SRC_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_thumb },
                new OracleParameter("P_SRC_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_index_score.HasValue ? model.src_right_index_score : null },
                new OracleParameter("P_SRC_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_index },
                new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                new OracleParameter("P_PORT_IN_DATE", OracleDbType.Date, ParameterDirection.Input) { Value = model.port_in_date },
                new OracleParameter("P_ALT_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.alt_msisdn },
                new OracleParameter("P_POC_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.poc_number },
                new OracleParameter("P_IS_URGENT", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_urgent.HasValue ? model.is_urgent : null },
                new OracleParameter("P_OPTIONAL1", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional1 },
                new OracleParameter("P_OPTIONAL2", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional2 },
                new OracleParameter("P_OPTIONAL3", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional3 },
                new OracleParameter("P_OPTIONAL4", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional4.HasValue ? model.optional4 : null },
                new OracleParameter("P_OPTIONAL5", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional5.HasValue ? model.optional5 : null },
                new OracleParameter("P_OPTIONAL6", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional6.HasValue ? model.optional6 : null },
                new OracleParameter("P_NOTE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.note },
                new OracleParameter("P_SIM_REP_REASON_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_rep_reason_id.HasValue ? model.sim_rep_reason_id : null },
                new OracleParameter("P_PAYMENT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.payment_type },
                new OracleParameter("P_ISPAIRED", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.is_paired : null },
                new OracleParameter("P_MSISDNRESERVATIONID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdnReservationId },
                new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.cahnnel_id : null },
                new OracleParameter("P_DIVISION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.division_name },
                new OracleParameter("P_DISTRICT_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.district_name },
                new OracleParameter("P_THANA_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.thana_name },
                new OracleParameter("P_CENTER_OR_DISTRIBUTOR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.distributor_code },
                new OracleParameter("P_SIM_REPLC_REASON", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_replc_reason },
                new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                new OracleParameter("P_RIGHT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.right_id },
                new OracleParameter("P_SIM_REPLACEMENT_TYPE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_replacement_type },
                new OracleParameter("P_OLD_SIM_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.old_sim_number },
                new OracleParameter("P_SRC_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_sim_category },
                new OracleParameter("P_PORT_IN_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.port_in_confirmation_code },
                new OracleParameter("P_DEST_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_ec_verifi_reqrd },
                new OracleParameter("P_SRC_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_ec_verifi_reqrd },
                new OracleParameter("P_DEST_FOREIGN_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_foreign_flag },
                new OracleParameter("P_DBSS_SUBSCRIPTION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dbss_subscription_id },
                new OracleParameter("P_SAF_STATUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.saf_status },
                new OracleParameter("P_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_id },
                new OracleParameter("P_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_confirmation_code },
                new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name },
                new OracleParameter("P_SRC_OWNER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_owner_customer_id },
                new OracleParameter("P_SRC_USER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_user_customer_id },
                new OracleParameter("P_SRC_PAYER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_payer_customer_id },
                new OracleParameter("P_DEST_IMSI", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_imsi },
                new OracleParameter("P_SESSION_TOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.prov_id },
                new OracleParameter("P_LATITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.latitude },
                new OracleParameter("P_LONGITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.longitude },
                new OracleParameter("P_LAC_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.lac },
                new OracleParameter("P_CELL_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.cid },
                new OracleParameter("P_SCANNER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.scanner_id },
                new OracleParameter("P_ORDER_BOOKING_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.order_booking_flag },
                new OracleParameter("P_IS_ESIM", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_esim },
                new OracleParameter("P_ORDER_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_number },
                new OracleParameter("P_INITIATOR_CHANNEL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.initiator_channel },
                new OracleParameter("P_ORDER_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_type },
                new OracleParameter("P_SUBSCRIPTION_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_type },
                new OracleParameter("P_SIMKIT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.simkit_type }   
                };

                orderResponse = await _oracleDataManagerV2.SelectProcedureV2("SUBMITORDER20", parameters.ToArray());
            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV6",
                    procedure_name = "SUBMITORDER12",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);

            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV6",
                    procedure_name = "SUBMITORDER12",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return orderResponse;
        }

        public async Task<DataTable> SubmitOrderV8(OrderRequest3 model)
        {
            DataTable orderResponse = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number.HasValue ? model.bi_token_number : null },
                new OracleParameter("P_BSS_ReqId", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bss_reqId },
                new OracleParameter("P_Status", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.status },
                new OracleParameter("P_error_id", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.error_id },
                new OracleParameter("p_error_description", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_description },
                new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number.HasValue ? model.purpose_number : null },
                new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                new OracleParameter("P_DEST_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_category.HasValue ? model.sim_category : null },
                new OracleParameter("P_DEST_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                new OracleParameter("P_SUBSCRIPTION_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.subscription_type_id.HasValue ? model.subscription_type_id : null },
                new OracleParameter("P_SUBSCRIPTION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_code },
                new OracleParameter("P_PACKAGE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.package_id.HasValue ? model.package_id : null },
                new OracleParameter("P_PACKAGE_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.package_code },
                new OracleParameter("P_DEST_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_doc_type_no.HasValue ? model.dest_doc_type_no : null },
                new OracleParameter("P_DEST_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_nid },
                new OracleParameter("P_DEST_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_dob },
                new OracleParameter("P_SRC_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_doc_type_no.HasValue ? model.src_doc_type_no : null },
                new OracleParameter("P_SRC_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_nid },
                new OracleParameter("P_SRC_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_dob },
                new OracleParameter("P_PLATFORM_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.platform_id },
                new OracleParameter("P_CUSTOMER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_name },
                new OracleParameter("P_GENDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.gender },
                new OracleParameter("P_FLAT_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.flat_number },
                new OracleParameter("P_HOUSE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.house_number },
                new OracleParameter("P_ROAD_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.road_number },
                new OracleParameter("P_VILLAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.village },
                new OracleParameter("P_DIVISION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.division_id.HasValue ? model.division_id : null },
                new OracleParameter("P_DISTRICT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.district_id.HasValue ? model.district_id : null },
                new OracleParameter("P_THANA_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.thana_id.HasValue ? model.thana_id : null },
                new OracleParameter("P_POSTAL_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.postal_code },
                new OracleParameter("P_EMAIL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.email },
                new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_code },
                new OracleParameter("P_DEST_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_thumb_score.HasValue ? model.dest_left_thumb_score : null },
                new OracleParameter("P_DEST_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_thumb },
                new OracleParameter("P_DEST_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_index_score.HasValue ? model.dest_left_index_score : null },
                new OracleParameter("P_DEST_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_index },
                new OracleParameter("P_DEST_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_thumb_score.HasValue ? model.dest_right_thumb_score : null },
                new OracleParameter("P_DEST_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_thumb },
                new OracleParameter("P_DEST_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_index_score.HasValue ? model.dest_right_index_score : null },
                new OracleParameter("P_DEST_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_index },
                new OracleParameter("P_SRC_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_thumb_score.HasValue ? model.src_left_thumb_score : null },
                new OracleParameter("P_SRC_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_thumb },
                new OracleParameter("P_SRC_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_index_score.HasValue ? model.src_left_index_score : null },
                new OracleParameter("P_SRC_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_index },
                new OracleParameter("P_SRC_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_thumb_score.HasValue ? model.src_right_thumb_score : null },
                new OracleParameter("P_SRC_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_thumb },
                new OracleParameter("P_SRC_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_index_score.HasValue ? model.src_right_index_score : null },
                new OracleParameter("P_SRC_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_index },
                new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                new OracleParameter("P_PORT_IN_DATE", OracleDbType.Date, ParameterDirection.Input) { Value = model.port_in_date },
                new OracleParameter("P_ALT_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.alt_msisdn },
                new OracleParameter("P_POC_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.poc_number },
                new OracleParameter("P_IS_URGENT", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_urgent.HasValue ? model.is_urgent : null },
                new OracleParameter("P_OPTIONAL1", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional1 },
                new OracleParameter("P_OPTIONAL2", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional2 },
                new OracleParameter("P_OPTIONAL3", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional3 },
                new OracleParameter("P_OPTIONAL4", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional4.HasValue ? model.optional4 : null },
                new OracleParameter("P_OPTIONAL5", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional5.HasValue ? model.optional5 : null },
                new OracleParameter("P_OPTIONAL6", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional6.HasValue ? model.optional6 : null },
                new OracleParameter("P_NOTE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.note },
                new OracleParameter("P_SIM_REP_REASON_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_rep_reason_id.HasValue ? model.sim_rep_reason_id : null },
                new OracleParameter("P_PAYMENT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.payment_type },
                new OracleParameter("P_ISPAIRED", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.is_paired : null },
                new OracleParameter("P_MSISDNRESERVATIONID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdnReservationId },
                new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.cahnnel_id : null },
                new OracleParameter("P_DIVISION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.division_name },
                new OracleParameter("P_DISTRICT_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.district_name },
                new OracleParameter("P_THANA_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.thana_name },
                new OracleParameter("P_CENTER_OR_DISTRIBUTOR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.distributor_code },
                new OracleParameter("P_SIM_REPLC_REASON", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_replc_reason },
                new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                new OracleParameter("P_RIGHT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.right_id },
                new OracleParameter("P_SIM_REPLACEMENT_TYPE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_replacement_type },
                new OracleParameter("P_OLD_SIM_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.old_sim_number },
                new OracleParameter("P_SRC_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_sim_category },
                new OracleParameter("P_PORT_IN_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.port_in_confirmation_code },
                new OracleParameter("P_DEST_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_ec_verifi_reqrd },
                new OracleParameter("P_SRC_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_ec_verifi_reqrd },
                new OracleParameter("P_DEST_FOREIGN_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_foreign_flag },
                new OracleParameter("P_DBSS_SUBSCRIPTION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dbss_subscription_id },
                new OracleParameter("P_SAF_STATUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.saf_status },
                new OracleParameter("P_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_id },
                new OracleParameter("P_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_confirmation_code },
                new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name },
                new OracleParameter("P_SRC_OWNER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_owner_customer_id },
                new OracleParameter("P_SRC_USER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_user_customer_id },
                new OracleParameter("P_SRC_PAYER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_payer_customer_id },
                new OracleParameter("P_DEST_IMSI", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_imsi },
                new OracleParameter("P_SESSION_TOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.prov_id },
                new OracleParameter("P_LATITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.latitude },
                new OracleParameter("P_LONGITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.longitude },
                new OracleParameter("P_LAC_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.lac },
                new OracleParameter("P_CELL_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.cid },
                new OracleParameter("P_SCANNER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.scanner_id },
                new OracleParameter("P_ORDER_BOOKING_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.order_booking_flag },
                new OracleParameter("P_IS_ESIM", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_esim },
                new OracleParameter("P_IS_LUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_lus },
                new OracleParameter("P_BTS_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.btsCode },
                new OracleParameter("P_SELECTED_CATEGORY", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.selected_category }
                };

                orderResponse = await _oracleDataManagerV2.SelectProcedureV2("SUBMITORDER18", parameters.ToArray());
            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV6",
                    procedure_name = "SUBMITORDER17",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);

            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV6",
                    procedure_name = "SUBMITORDER12",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return orderResponse;
        }
        public async Task<DataTable> HomeWifiSubmitOrder(HomeWifiOrderRequest2 model)
        {
            DataTable orderResponse = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number.HasValue ? model.bi_token_number : null },
                new OracleParameter("P_BSS_ReqId", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bss_reqId },
                new OracleParameter("P_Status", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.status },
                new OracleParameter("P_error_id", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.error_id },
                new OracleParameter("p_error_description", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_description },
                new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number.HasValue ? model.purpose_number : null },
                new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                new OracleParameter("P_DEST_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_category.HasValue ? model.sim_category : null },
                new OracleParameter("P_DEST_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                new OracleParameter("P_SUBSCRIPTION_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.subscription_type_id.HasValue ? model.subscription_type_id : null },
                new OracleParameter("P_SUBSCRIPTION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_code },
                new OracleParameter("P_PACKAGE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.package_id.HasValue ? model.package_id : null },
                new OracleParameter("P_PACKAGE_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.package_code },
                new OracleParameter("P_DEST_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_doc_type_no.HasValue ? model.dest_doc_type_no : null },
                new OracleParameter("P_DEST_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_nid },
                new OracleParameter("P_DEST_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_dob },
                new OracleParameter("P_SRC_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_doc_type_no.HasValue ? model.src_doc_type_no : null },
                new OracleParameter("P_SRC_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_nid },
                new OracleParameter("P_SRC_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_dob },
                new OracleParameter("P_PLATFORM_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.platform_id },
                new OracleParameter("P_CUSTOMER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_name },
                new OracleParameter("P_GENDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.gender },
                new OracleParameter("P_FLAT_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.flat_number },
                new OracleParameter("P_HOUSE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.house_number },
                new OracleParameter("P_ROAD_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.road_number },
                new OracleParameter("P_VILLAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.village },
                new OracleParameter("P_DIVISION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.division_id.HasValue ? model.division_id : null },
                new OracleParameter("P_DISTRICT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.district_id.HasValue ? model.district_id : null },
                new OracleParameter("P_THANA_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.thana_id.HasValue ? model.thana_id : null },
                new OracleParameter("P_POSTAL_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.postal_code },
                new OracleParameter("P_EMAIL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.email },
                new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_code },
                new OracleParameter("P_DEST_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_thumb_score.HasValue ? model.dest_left_thumb_score : null },
                new OracleParameter("P_DEST_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_thumb },
                new OracleParameter("P_DEST_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_index_score.HasValue ? model.dest_left_index_score : null },
                new OracleParameter("P_DEST_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_index },
                new OracleParameter("P_DEST_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_thumb_score.HasValue ? model.dest_right_thumb_score : null },
                new OracleParameter("P_DEST_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_thumb },
                new OracleParameter("P_DEST_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_index_score.HasValue ? model.dest_right_index_score : null },
                new OracleParameter("P_DEST_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_index },
                new OracleParameter("P_SRC_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_thumb_score.HasValue ? model.src_left_thumb_score : null },
                new OracleParameter("P_SRC_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_thumb },
                new OracleParameter("P_SRC_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_index_score.HasValue ? model.src_left_index_score : null },
                new OracleParameter("P_SRC_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_index },
                new OracleParameter("P_SRC_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_thumb_score.HasValue ? model.src_right_thumb_score : null },
                new OracleParameter("P_SRC_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_thumb },
                new OracleParameter("P_SRC_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_index_score.HasValue ? model.src_right_index_score : null },
                new OracleParameter("P_SRC_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_index },
                new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                new OracleParameter("P_PORT_IN_DATE", OracleDbType.Date, ParameterDirection.Input) { Value = model.port_in_date },
                new OracleParameter("P_ALT_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.alt_msisdn },
                new OracleParameter("P_POC_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.poc_number },
                new OracleParameter("P_IS_URGENT", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_urgent.HasValue ? model.is_urgent : null },
                new OracleParameter("P_OPTIONAL1", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional1 },
                new OracleParameter("P_OPTIONAL2", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional2 },
                new OracleParameter("P_OPTIONAL3", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional3 },
                new OracleParameter("P_OPTIONAL4", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional4.HasValue ? model.optional4 : null },
                new OracleParameter("P_OPTIONAL5", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional5.HasValue ? model.optional5 : null },
                new OracleParameter("P_OPTIONAL6", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional6.HasValue ? model.optional6 : null },
                new OracleParameter("P_NOTE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.note },
                new OracleParameter("P_SIM_REP_REASON_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_rep_reason_id.HasValue ? model.sim_rep_reason_id : null },
                new OracleParameter("P_PAYMENT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.payment_type },
                new OracleParameter("P_ISPAIRED", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.is_paired : null },
                new OracleParameter("P_MSISDNRESERVATIONID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdnReservationId },
                new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.cahnnel_id : null },
                new OracleParameter("P_DIVISION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.division_name },
                new OracleParameter("P_DISTRICT_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.district_name },
                new OracleParameter("P_THANA_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.thana_name },
                new OracleParameter("P_CENTER_OR_DISTRIBUTOR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.distributor_code },
                new OracleParameter("P_SIM_REPLC_REASON", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_replc_reason },
                new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                new OracleParameter("P_RIGHT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.right_id },
                new OracleParameter("P_SIM_REPLACEMENT_TYPE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_replacement_type },
                new OracleParameter("P_OLD_SIM_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.old_sim_number },
                new OracleParameter("P_SRC_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_sim_category },
                new OracleParameter("P_PORT_IN_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.port_in_confirmation_code },
                new OracleParameter("P_DEST_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_ec_verifi_reqrd },
                new OracleParameter("P_SRC_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_ec_verifi_reqrd },
                new OracleParameter("P_DEST_FOREIGN_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_foreign_flag },
                new OracleParameter("P_DBSS_SUBSCRIPTION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dbss_subscription_id },
                new OracleParameter("P_SAF_STATUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.saf_status },
                new OracleParameter("P_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_id },
                new OracleParameter("P_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_confirmation_code },
                new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name },
                new OracleParameter("P_SRC_OWNER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_owner_customer_id },
                new OracleParameter("P_SRC_USER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_user_customer_id },
                new OracleParameter("P_SRC_PAYER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_payer_customer_id },
                new OracleParameter("P_DEST_IMSI", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_imsi },
                new OracleParameter("P_SESSION_TOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.prov_id },
                new OracleParameter("P_LATITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.latitude },
                new OracleParameter("P_LONGITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.longitude },
                new OracleParameter("P_LAC_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.lac },
                new OracleParameter("P_CELL_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.cid },
                new OracleParameter("P_SCANNER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.scanner_id },
                new OracleParameter("P_ORDER_BOOKING_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.order_booking_flag },
                new OracleParameter("P_IS_ESIM", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_esim },
                new OracleParameter("P_IS_LUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_lus },
                new OracleParameter("P_BTS_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.btsCode },
                new OracleParameter("P_SELECTED_CATEGORY", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.selected_category },
                new OracleParameter("P_ORDER_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_number },
                new OracleParameter("P_INITIATOR_CHANNEL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.initiator_channel },
                new OracleParameter("P_ORDER_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_type },
                new OracleParameter("P_SUBSCRIPTION_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_type },
                new OracleParameter("P_SIMKIT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.simkit_type }
                };

                orderResponse = await _oracleDataManagerV2.SelectProcedureV2("SUBMITORDER19", parameters.ToArray());
            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV6",
                    procedure_name = "SUBMITORDER19",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);

            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV6",
                    procedure_name = "SUBMITORDER19",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return orderResponse;
        }

        public async Task<DataTable> SubmitOrderV7(OrderRequest3 model)
        {
            DataTable orderResponse = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number.HasValue ? model.bi_token_number : null },
                new OracleParameter("P_BSS_ReqId", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bss_reqId },
                new OracleParameter("P_Status", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.status },
                new OracleParameter("P_error_id", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.error_id },
                new OracleParameter("p_error_description", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_description },
                new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number.HasValue ? model.purpose_number : null },
                new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                new OracleParameter("P_DEST_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_category.HasValue ? model.sim_category : null },
                new OracleParameter("P_DEST_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                new OracleParameter("P_SUBSCRIPTION_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.subscription_type_id.HasValue ? model.subscription_type_id : null },
                new OracleParameter("P_SUBSCRIPTION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_code },
                new OracleParameter("P_PACKAGE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.package_id.HasValue ? model.package_id : null },
                new OracleParameter("P_PACKAGE_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.package_code },
                new OracleParameter("P_DEST_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_doc_type_no.HasValue ? model.dest_doc_type_no : null },
                new OracleParameter("P_DEST_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_nid },
                new OracleParameter("P_DEST_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_dob },
                new OracleParameter("P_SRC_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_doc_type_no.HasValue ? model.src_doc_type_no : null },
                new OracleParameter("P_SRC_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_nid },
                new OracleParameter("P_SRC_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_dob },
                new OracleParameter("P_PLATFORM_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.platform_id },
                new OracleParameter("P_CUSTOMER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_name },
                new OracleParameter("P_GENDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.gender },
                new OracleParameter("P_FLAT_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.flat_number },
                new OracleParameter("P_HOUSE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.house_number },
                new OracleParameter("P_ROAD_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.road_number },
                new OracleParameter("P_VILLAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.village },
                new OracleParameter("P_DIVISION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.division_id.HasValue ? model.division_id : null },
                new OracleParameter("P_DISTRICT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.district_id.HasValue ? model.district_id : null },
                new OracleParameter("P_THANA_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.thana_id.HasValue ? model.thana_id : null },
                new OracleParameter("P_POSTAL_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.postal_code },
                new OracleParameter("P_EMAIL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.email },
                new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_code },
                new OracleParameter("P_DEST_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_thumb_score.HasValue ? model.dest_left_thumb_score : null },
                new OracleParameter("P_DEST_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_thumb },
                new OracleParameter("P_DEST_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_index_score.HasValue ? model.dest_left_index_score : null },
                new OracleParameter("P_DEST_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_index },
                new OracleParameter("P_DEST_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_thumb_score.HasValue ? model.dest_right_thumb_score : null },
                new OracleParameter("P_DEST_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_thumb },
                new OracleParameter("P_DEST_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_index_score.HasValue ? model.dest_right_index_score : null },
                new OracleParameter("P_DEST_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_index },
                new OracleParameter("P_SRC_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_thumb_score.HasValue ? model.src_left_thumb_score : null },
                new OracleParameter("P_SRC_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_thumb },
                new OracleParameter("P_SRC_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_index_score.HasValue ? model.src_left_index_score : null },
                new OracleParameter("P_SRC_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_index },
                new OracleParameter("P_SRC_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_thumb_score.HasValue ? model.src_right_thumb_score : null },
                new OracleParameter("P_SRC_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_thumb },
                new OracleParameter("P_SRC_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_index_score.HasValue ? model.src_right_index_score : null },
                new OracleParameter("P_SRC_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_index },
                new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                new OracleParameter("P_PORT_IN_DATE", OracleDbType.Date, ParameterDirection.Input) { Value = model.port_in_date },
                new OracleParameter("P_ALT_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.alt_msisdn },
                new OracleParameter("P_POC_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.poc_number },
                new OracleParameter("P_IS_URGENT", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_urgent.HasValue ? model.is_urgent : null },
                new OracleParameter("P_OPTIONAL1", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional1 },
                new OracleParameter("P_OPTIONAL2", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional2 },
                new OracleParameter("P_OPTIONAL3", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional3 },
                new OracleParameter("P_OPTIONAL4", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional4.HasValue ? model.optional4 : null },
                new OracleParameter("P_OPTIONAL5", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional5.HasValue ? model.optional5 : null },
                new OracleParameter("P_OPTIONAL6", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional6.HasValue ? model.optional6 : null },
                new OracleParameter("P_NOTE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.note },
                new OracleParameter("P_SIM_REP_REASON_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_rep_reason_id.HasValue ? model.sim_rep_reason_id : null },
                new OracleParameter("P_PAYMENT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.payment_type },
                new OracleParameter("P_ISPAIRED", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.is_paired : null },
                new OracleParameter("P_MSISDNRESERVATIONID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdnReservationId },
                new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.cahnnel_id : null },
                new OracleParameter("P_DIVISION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.division_name },
                new OracleParameter("P_DISTRICT_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.district_name },
                new OracleParameter("P_THANA_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.thana_name },
                new OracleParameter("P_CENTER_OR_DISTRIBUTOR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.distributor_code },
                new OracleParameter("P_SIM_REPLC_REASON", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_replc_reason },
                new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                new OracleParameter("P_RIGHT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.right_id },
                new OracleParameter("P_SIM_REPLACEMENT_TYPE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_replacement_type },
                new OracleParameter("P_OLD_SIM_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.old_sim_number },
                new OracleParameter("P_SRC_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_sim_category },
                new OracleParameter("P_PORT_IN_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.port_in_confirmation_code },
                new OracleParameter("P_DEST_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_ec_verifi_reqrd },
                new OracleParameter("P_SRC_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_ec_verifi_reqrd },
                new OracleParameter("P_DEST_FOREIGN_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_foreign_flag },
                new OracleParameter("P_DBSS_SUBSCRIPTION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dbss_subscription_id },
                new OracleParameter("P_SAF_STATUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.saf_status },
                new OracleParameter("P_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_id },
                new OracleParameter("P_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_confirmation_code },
                new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name },
                new OracleParameter("P_SRC_OWNER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_owner_customer_id },
                new OracleParameter("P_SRC_USER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_user_customer_id },
                new OracleParameter("P_SRC_PAYER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_payer_customer_id },
                new OracleParameter("P_DEST_IMSI", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_imsi },
                new OracleParameter("P_SESSION_TOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.prov_id },
                new OracleParameter("P_LATITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.latitude },
                new OracleParameter("P_LONGITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.longitude },
                new OracleParameter("P_LAC_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.lac },
                new OracleParameter("P_CELL_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.cid },
                new OracleParameter("P_SCANNER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.scanner_id },
                new OracleParameter("P_ORDER_BOOKING_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.order_booking_flag },
                new OracleParameter("P_IS_ESIM", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_esim },
                new OracleParameter("P_IS_STARTREK", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_starTrek },
                new OracleParameter("P_ORDER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_id },
                new OracleParameter("P_IS_ONLINE_SALE", OracleDbType.Int32, ParameterDirection.Input) { Value = model.is_online_sale },
                };

                orderResponse = await _oracleDataManagerV2.SelectProcedureV2("SUBMITORDER13", parameters.ToArray());

            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV7",
                    procedure_name = "SUBMITORDER13",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);

            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV7",
                    procedure_name = "SUBMITORDER13",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return orderResponse;
        }

        public async Task<decimal> SubmitOrder(OrderRequest model)
        {
            decimal BIAReqsTokenId;
            try
            {
                var dateTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");

                List<OracleParameter> parameters = new List<OracleParameter>
                {
                new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number.HasValue ? model.bi_token_number : null },
                new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number.HasValue ? model.purpose_number : null },
                new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                new OracleParameter("P_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_category.HasValue ? model.sim_category : null },
                new OracleParameter("P_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                new OracleParameter("P_SUBSCRIPTION_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.subscription_type_id.HasValue ? model.subscription_type_id : null },
                new OracleParameter("P_SUBSCRIPTION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_code },
                new OracleParameter("P_PACKAGE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.package_id.HasValue ? model.package_id : null },
                new OracleParameter("P_PACKAGE_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.package_code },
                new OracleParameter("P_DEST_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_doc_type_no.HasValue ? model.dest_doc_type_no : null },
                new OracleParameter("P_DEST_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_nid },
                new OracleParameter("P_DEST_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_dob },
                new OracleParameter("P_SRC_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_doc_type_no.HasValue ? model.src_doc_type_no : null },
                new OracleParameter("P_SRC_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_nid },
                new OracleParameter("P_SRC_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_dob },
                new OracleParameter("P_PLATFORM_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.platform_id },
                new OracleParameter("P_CUSTOMER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_name },
                new OracleParameter("P_GENDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.gender },
                new OracleParameter("P_FLAT_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.flat_number },
                new OracleParameter("P_HOUSE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.house_number },
                new OracleParameter("P_ROAD_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.road_number },
                new OracleParameter("P_VILLAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.village },
                new OracleParameter("P_DIVISION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.division_id.HasValue ? model.division_id : null },
                new OracleParameter("P_DISTRICT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.district_id.HasValue ? model.district_id : null },
                new OracleParameter("P_THANA_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.thana_id.HasValue ? model.thana_id : null },
                new OracleParameter("P_POSTAL_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.postal_code },
                new OracleParameter("P_EMAIL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.email },
                new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.salesman_code },
                new OracleParameter("P_DEST_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_thumb_score.HasValue ? model.dest_left_thumb_score : null },
                new OracleParameter("P_DEST_LEFT_THUMB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_left_thumb },
                new OracleParameter("P_DEST_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_index_score.HasValue ? model.dest_left_index_score : null },
                new OracleParameter("P_DEST_LEFT_INDEX", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_left_index },
                new OracleParameter("P_DEST_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_thumb_score.HasValue ? model.dest_right_thumb_score : null },
                new OracleParameter("P_DEST_RIGHT_THUMB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_right_thumb },
                new OracleParameter("P_DEST_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_index_score.HasValue ? model.dest_right_index_score : null },
                new OracleParameter("P_DEST_RIGHT_INDEX", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_right_index },
                new OracleParameter("P_SRC_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_thumb_score.HasValue ? model.src_left_thumb_score : null },
                new OracleParameter("P_SRC_LEFT_THUMB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_left_thumb },
                new OracleParameter("P_SRC_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_index_score.HasValue ? model.src_left_index_score : null },
                new OracleParameter("P_SRC_LEFT_INDEX", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_left_index },
                new OracleParameter("P_SRC_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_thumb_score.HasValue ? model.src_right_thumb_score : null },
                new OracleParameter("P_SRC_RIGHT_THUMB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_right_thumb },
                new OracleParameter("P_SRC_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_index_score.HasValue ? model.src_right_index_score : null },
                new OracleParameter("P_SRC_RIGHT_INDEX", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_right_index },
                new OracleParameter("P_USER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                new OracleParameter("P_PORT_IN_DATE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.port_in_date },
                new OracleParameter("P_ALT_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.alt_msisdn },
                new OracleParameter("P_POC_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.poc_number },
                new OracleParameter("P_IS_URGENT", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_urgent.HasValue ? model.is_urgent : null },
                new OracleParameter("P_OPTIONAL1", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional1 },
                new OracleParameter("P_OPTIONAL2", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional2 },
                new OracleParameter("P_OPTIONAL3", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional3 },
                new OracleParameter("P_OPTIONAL4", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional4.HasValue ? model.optional4 : null },
                new OracleParameter("P_OPTIONAL5", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional5.HasValue ? model.optional5 : null },
                new OracleParameter("P_OPTIONAL6", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional6.HasValue ? model.optional6 : null },
                new OracleParameter("P_CREATE_DATE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = dateTime },
                new OracleParameter("P_UPDATE_DATE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = dateTime },
                new OracleParameter("P_NOTE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.note },
                new OracleParameter("P_SIM_REP_REASON_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_rep_reason_id.HasValue ? model.sim_rep_reason_id : null },
                new OracleParameter("P_PAYMENT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.payment_type },
                new OracleParameter("P_ISPAIRED", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.is_paired : null },
                new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.cahnnel_id : null },
                new OracleParameter("DIVISION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.division_name },
                new OracleParameter("DISTRICT_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.district_name },
                new OracleParameter("THANA_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.thana_name },
                new OracleParameter("CENTER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.center_code },
                new OracleParameter("DISTRIBUTOR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.distributor_code },
                new OracleParameter("SIM_REPLC_REASON", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_replc_reason },
                new OracleParameter("CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                new OracleParameter("RIGHT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.right_id }
            };

                var result = await _oracleDataManagerV2.CallInsertProcedure("SUBMITORDER", parameters.ToArray());
                BIAReqsTokenId = Convert.ToDecimal(result);
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    retailer_id = model.retailer_id,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrder",
                    procedure_name = "SUBMITORDER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return BIAReqsTokenId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<DataTable> GetStatus(StatusRequest model)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = Convert.ToDecimal(model.request_id) },
                    new OracleParameter("PO_STASUS", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETSTATUS", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "GetStatus",
                    procedure_name = "BIA_GETSTATUS",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        /// <summary>
        /// This method is used for Activity Log Long press.
        /// </summary>
        /// <param name="token_id"></param>
        /// <returns></returns>
        public async Task<long> CheckBIAToken(string token_id)
        {
            long tokenNo = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_TOKEN_NO", OracleDbType.Varchar2, ParameterDirection.Input) { Value = token_id },
                    new OracleParameter("PO_RESULT", OracleDbType.Decimal, ParameterDirection.Output)
                };
                var result = await _oracleDataManagerV2.CallSelectDataWithObjectReturn("BIA_CHECKBIATOKENID", "PO_RESULT", parameters.ToArray());
                tokenNo = Convert.ToInt64(result.ToString());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "CheckBIAToken",
                    procedure_name = "BIA_CHECKBIATOKENID",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return tokenNo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token_id"></param>
        /// <returns></returns>
        public async Task<DataTable> GetOrderInfoByTokenNo(decimal token_id)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_TOKEN_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = token_id },
                    new OracleParameter("PO_RESULT", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETORDERINFO", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetOrderInfoByTokenNo",
                    procedure_name = "BIA_GETORDERINFO",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<DataTable> GetPortInOrderConfirmCode(int purposeId, string msisdn)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                   new OracleParameter("P_PURPOSE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = Convert.ToDecimal(purposeId) },
                   new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = msisdn },
                   new OracleParameter("PO_RESULT", OracleDbType.RefCursor, ParameterDirection.Output),
                };
                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETORDERCONFIRMCODE", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetTOSPortInOrderConfirmCode",
                    procedure_name = "BIA_GETORDERCONFIRMCODE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> ValidateOrder(VMValidateOrder model)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                    new OracleParameter("P_DEST_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                    new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number == null ? DBNull.Value : model.purpose_number },
                    new OracleParameter("P_IS_CORPORATE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_corporate == null ? DBNull.Value : model.is_corporate },
                    new OracleParameter("P_RETAILER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                    new OracleParameter("P_DEST_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_dob },
                    new OracleParameter("PO_CURSOR", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_VALIDATEORDERV3", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ValidateOrder",
                    procedure_name = "BIA_VALIDATEORDERV3",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        /// <summary>
        /// Checks if submitted order is in process or not.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<DataTable> ValidateOrder_(VMValidateOrder model)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                   new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                   new OracleParameter("P_DEST_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                   new OracleParameter("PO_CURSOR", OracleDbType.RefCursor, ParameterDirection.Output),
                };
                result = await _oracleDataManagerV2.SelectProcedure("BIA_VALIDATEORDER", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ValidateOrder_",
                    procedure_name = "BIA_VALIDATEORDER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<DataTable> GetInventoryIdByChannelName(string channelName)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                   new OracleParameter("P_CHENNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = channelName },
                   new OracleParameter("PO_RESULT", OracleDbType.RefCursor, ParameterDirection.Output),
                };
                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETINVENTORYIDBYCHANNEL", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetInventoryIdByChannelName",
                    procedure_name = "BIA_GETINVENTORYIDBYCHANNEL",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<DataTable> GetCenterCodeByUserName(string userName)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                   new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userName },
                   new OracleParameter("PO_RESULT", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETCENTERCODEBYUSERNAME", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetCenterCodeByUserName",
                    procedure_name = "BIA_GETCENTERCODEBYUSERNAME",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }
        #endregion

        #region ===================| Order BSS Service |======================
        public async Task<DataTable> GetBssDataList(OrderListReqModel reqModel)
        {
            DataTable dt;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_ORDER_STATUS", OracleDbType.Int32, ParameterDirection.Input) { Value = reqModel.order_staus },
                    new OracleParameter("P_BSS_FLAG", OracleDbType.Int32, ParameterDirection.Input) { Value = reqModel.order_flag },// this is static for booking data
                    new OracleParameter("P_MAX_ROW", OracleDbType.Int32, ParameterDirection.Input) { Value = reqModel.max_row },
                    new OracleParameter("PO_BSSDATALIST", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                dt = await _oracleDataManagerV2.SelectProcedure("BSS_GETORDERDATALIST", parameters.ToArray());
                return dt;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetBssDataList",
                    procedure_name = "BSS_GETORDERDATALIST",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }
        public async Task<bool> UpdateBioDbForOrderReq(string bi_token_no, string order_conframtion_code)
        {
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_BI_TOKEN_NO", bi_token_no),
                    new OracleParameter("P_ORDER_CONFRAMTION_CODE", order_conframtion_code)
                };

                bool rowAffect = await _oracleDataManagerV2.CallUpdateProcedure("BSS_UPDBIREQUESTFORORDER", parameters.ToArray());
                return rowAffect;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "UpdateBioDbForOrderReq",
                    procedure_name = "BSS_UPDBIREQUESTFORORDER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<bool> UpdateBioDbForCreateCustomerReq(string bi_token_no, string owner_customer_id)
        {
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_BI_TOKEN_NO", bi_token_no),
                    new OracleParameter("P_DEST_CUSTOMER_ID", owner_customer_id)
                };
                bool rowAffect = await _oracleDataManagerV2.CallUpdateProcedure("BSS_UPDBIREQFORCREATECUSTOMER", parameters.ToArray());
                return rowAffect;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "UpdateBioDbForCreateCustomerReq",
                    procedure_name = "BSS_UPDBIREQFORCREATECUSTOMER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<bool> ClearBookingFlagForOrderReq(int order_booking_flag)
        {
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_ORDER_BOOKING_FLAG", order_booking_flag)
                };

                bool rowAffect = await _oracleDataManagerV2.CallUpdateProcedure("BSS_CLEARORDERBOOKING", parameters.ToArray());
                return rowAffect;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ClearBookingFlagForOrderReq",
                    procedure_name = "BSS_CLEARORDERBOOKING",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<DataTable> GetBTSInformationByLacCid(int lac, int cid)
        {
            DataTable dt;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_LAC", OracleDbType.Int32, ParameterDirection.Input) { Value = lac },
                    new OracleParameter("P_CELL_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = cid },
                    new OracleParameter("PO_CURSOR", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                dt = await _oracleDataManagerV2.SelectProcedure("BSS_GETBTSINFOBYLACCID", parameters.ToArray());
                return dt;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetBTSInformationByLacCid",
                    procedure_name = "BSS_GETBTSINFOBYLACCID",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }
        #endregion

        #region ====================| SIM Replacement Area |================
        public async Task<DataTable> GetSIMReplacementReasons()
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PO_REASON", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETSIMREPLACEMENTREASONS", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetSIMReplacementReasons",
                    procedure_name = "BIA_GETSIMREPLACEMENTREASONS",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }
        #endregion

        #region ====================| Authentication and Authorization |=================
        public async Task<DataTable> ValidateUser(vmUserInfo model)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.user_name },
                    new OracleParameter("PI_PASSWORD", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.password },
                    new OracleParameter("PO_QC_USER", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                result = await _oracleDataManagerV2.SelectProcedure("VALIDATE_USER", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ValidateUser",
                    procedure_name = "VALIDATE_USER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }
        public async Task<DataTable> ValidateUserV2(LoginRequestsV2 userModel, vmUserInfo model)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.user_name },
                    new OracleParameter("PI_PASSWORD", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.password },
                    new OracleParameter("PI_BP_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userModel.BPMSISDN },
                    new OracleParameter("PO_QC_USER", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                result = await _oracleDataManagerV2.SelectProcedure("VALIDATE_USERV3", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ValidateUser",
                    procedure_name = "VALIDATE_USER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                ////_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> ValidateUserV3(FPValidationReqModel userModel)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userModel.user_name },
                    new OracleParameter("PI_BP_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userModel.BPMSISDN },
                    new OracleParameter("PO_QC_USER", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                result = await _oracleDataManagerV2.SelectProcedure("VALIDATE_USERV4", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ValidateUserV3",
                    procedure_name = "VALIDATE_USERV4",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }


        /// <summary>
        /// ValidateUser without password for reseller. 
        /// </summary>
        /// <param name="user_name"></param>
        /// <returns></returns>
        public async Task<DataTable> ValidateUser(string user_name)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                   new OracleParameter("PI_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = user_name },
                   new OracleParameter("PO_QC_USER", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("RESELLER_VALIDATE_USER", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ValidateUser",
                    procedure_name = "VALIDATE_USER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> ValidateUserReseller(string user_name)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                   new OracleParameter("PI_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = user_name },
                   new OracleParameter("PO_QC_USER", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("RESELLER_VALIDATE_USER", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ValidateUser",
                    procedure_name = "VALIDATE_USER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> ValidateExternalUser(ExternalLoginReqModel model)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                   new OracleParameter("PI_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.username },
                   new OracleParameter("PI_PASSWORD", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.password },
                   new OracleParameter("PO_EX_USER", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("VALIDATE_EXTERNAL_USER", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ValidateExternalUser",
                    procedure_name = "VALIDATE_EXTERNAL_USER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        //public async Task<int> GetUserAPIVersion(APIVersionRequest model)
        //{
        //    int apiVersion = 0;
        //    try
        //    {
        //        List<OracleParameter> parameters = new List<OracleParameter>
        //        {
        //            new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.username },
        //            new OracleParameter("P_PASSWORD", OracleDbType.Varchar2, ParameterDirection.Input) { Value = "" }
        //            //new OracleParameter("PO_APIVERSION", OracleDbType.Int32, ParameterDirection.Output)
        //        };

        //        var result = await _oracleDataManagerV2.CallSelectDataWithObjectReturn("USERAPIVERSION", "PO_APIVERSION", parameters.ToArray());
        //        apiVersion = Convert.ToInt32(result.ToString());
        //    }
        //    catch (Exception ex)
        //    {
        //        string? text = Convert.ToString(new
        //        {
        //            request_time = DateTime.Now,
        //            method_name = "GetUserAPIVersion",
        //            procedure_name = "USERAPIVERSION",
        //            error_source = ex.Source,
        //            error_code = ex.HResult,
        //            error_description = ex.Message
        //        });
        //        //_logWriter.WriteDailyLog2(text == null ? "" : text);

        //        throw new Exception("OuterDetails: " + text, ex);
        //    }

        //    return Convert.ToInt32(apiVersion);
        //}

        public async Task<int> GetUserAPIVersion(APIVersionRequest model)
        {
            if (model == null) return 0;
            int apiVersion = 0;

            Log.Information("DAL GetUserAPIVersion Started. Username: {Username}", model.username);

            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
        {
            new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.username ?? string.Empty },
            new OracleParameter("P_PASSWORD", OracleDbType.Varchar2, ParameterDirection.Input) { Value = "" }
        };

                var result = await _oracleDataManagerV2.CallSelectDataWithObjectReturn(
                    "USERAPIVERSION",
                    "PO_APIVERSION",
                    parameters.ToArray());

                if (result != null && result != DBNull.Value)
                {
                    apiVersion = Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("504") ||
                    ex.InnerException?.Message.Contains("504") == true ||
                    ex is TaskCanceledException ||
                    ex is TimeoutException)
                {
                    Log.Error(ex, "504 or Timeout in DAL USERAPIVERSION. Username: {Username}", model.username);
                }
                else
                {
                    Log.Error(ex, "Exception in DAL USERAPIVERSION. Username: {Username}", model.username);
                }

                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetUserAPIVersion",
                    procedure_name = "USERAPIVERSION",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }

            return Convert.ToInt32(apiVersion);
        }

        //public async Task<DataTable> GetUserAPIVersionWithAppUpdateCheck(VMAPIVersionRequestWithAppUpdateCheck model)
        //{
        //    try
        //    {
        //        List<OracleParameter> parameters = new List<OracleParameter>
        //        {
        //            new OracleParameter("PO_RESULT", OracleDbType.RefCursor, ParameterDirection.Output)
        //        };

        //        var result = await _oracleDataManagerV2.SelectProcedure("GETAPPUPDATEINFO", parameters.ToArray());
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        string? text = Convert.ToString(new
        //        {
        //            request_time = DateTime.Now,
        //            method_name = "GetUserAPIVersionWithAppUpdateCheck",
        //            model = Convert.ToString(model),
        //            procedure_name = "GETAPPUPDATEINFO",
        //            error_source = ex.Source,
        //            error_code = ex.HResult,
        //            error_description = ex.Message
        //        });
        //        //_logWriter.WriteDailyLog2(text == null ? "" : text);

        //        throw new Exception("OuterDetails: " + text, ex);
        //    }
        //}

        public async Task<DataTable> GetUserAPIVersionWithAppUpdateCheck(VMAPIVersionRequestWithAppUpdateCheck model)
        {
            Log.Information("DAL GetUserAPIVersionWithAppUpdateCheck Started. Username: {Username}", model?.username);

            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
        {
            new OracleParameter("PO_RESULT", OracleDbType.RefCursor, ParameterDirection.Output)
        };

                var result = await _oracleDataManagerV2.SelectProcedure("GETAPPUPDATEINFO", parameters.ToArray());
                return result;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("504") ||
                    ex.InnerException?.Message.Contains("504") == true ||
                    ex is TaskCanceledException ||
                    ex is TimeoutException)
                {
                    Log.Error(ex, "504 or Timeout in DAL GETAPPUPDATEINFO. Username: {Username}", model?.username);
                }
                else
                {
                    Log.Error(ex, "Exception in DAL GETAPPUPDATEINFO. Username: {Username}", model?.username);
                }

                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetUserAPIVersionWithAppUpdateCheck",
                    model = Convert.ToString(model),
                    procedure_name = "GETAPPUPDATEINFO",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<long> SaveLoginAtmInfo(UserLogInAttempt model)
        {
            long result = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                     new OracleParameter("P_USERID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.userid },
                     new OracleParameter("P_IS_SUCCESS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_success },
                     new OracleParameter("P_IP_ADDRESS", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.ip_address },
                     new OracleParameter("P_MACHINE_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.machine_name },
                     new OracleParameter("P_LOGINPROVIDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.loginprovider },
                     new OracleParameter("P_DEVICEID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.deviceid },
                     new OracleParameter("P_LAN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.lan },
                     new OracleParameter("P_VERSIONCODE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.versioncode },
                     new OracleParameter("P_VERSIONNAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.versionname },
                     new OracleParameter("P_OSVERSION", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.osversion },
                     new OracleParameter("P_KERNELVERSION", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.kernelversion },
                     new OracleParameter("P_FERMWAREVIRSION", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.fermwarevirsion }
                };

                result = await _oracleDataManagerV2.CallInsertProcedure("USERLOGINATTEMPTINSERT", parameters.ToArray());
                return result;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "SaveLoginAtmInfo",
                    model = Convert.ToString(model),
                    procedure_name = "USERLOGINATTEMPTINSERT",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }
        public async Task<long> SaveLoginAtmInfoV2(UserLogInAttemptV2 model)
        {
            long result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_USERID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.userid },
                    new OracleParameter("P_IS_SUCCESS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_success },
                    new OracleParameter("P_IP_ADDRESS", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.ip_address },
                    new OracleParameter("P_MACHINE_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.machine_name },
                    new OracleParameter("P_LOGINPROVIDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.loginprovider },
                    new OracleParameter("P_DEVICEID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.deviceid },
                    new OracleParameter("P_LAN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.lan },
                    new OracleParameter("P_VERSIONCODE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.versioncode },
                    new OracleParameter("P_VERSIONNAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.versionname },
                    new OracleParameter("P_OSVERSION", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.osversion },
                    new OracleParameter("P_KERNELVERSION", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.kernelversion },
                    new OracleParameter("P_FERMWAREVIRSION", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.fermwarevirsion },
                    new OracleParameter("P_LATITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.latitude },
                    new OracleParameter("P_LONGITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.longitude },
                    new OracleParameter("P_LAC", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.lac },
                    new OracleParameter("P_CID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.cid },
                    new OracleParameter("P_IS_BP", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_bp },
                    new OracleParameter("P_BP_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bp_msisdn },
                    new OracleParameter("P_DEVICE_MODEL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.device_model }
                };
                result = await _oracleDataManagerV2.CallInsertProcedure("USERLOGINATTEMPTINSERTV2", parameters.ToArray());
                return result;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "SaveLoginAtmInfoV2",
                    model = Convert.ToString(model),
                    procedure_name = "USERLOGINATTEMPTINSERTV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                ////_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }


        public async Task<long> SaveFailedLoginAtmInfo(UserLogInAttemptV2 model, object? requestData, object? responseData, string? remarks)
        {
            long result;

            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
        {
            new OracleParameter("P_USERID", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = DbValue(model.userid)
            },
            new OracleParameter("P_IS_SUCCESS", OracleDbType.Decimal, ParameterDirection.Input)
            {
                Value = DbValue(model.is_success)
            },
            new OracleParameter("P_IP_ADDRESS", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = DbValue(model.ip_address)
            },
            new OracleParameter("P_MACHINE_NAME", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = DbValue(model.machine_name)
            },
            new OracleParameter("P_LOGINPROVIDER", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = DbValue(model.loginprovider)
            },
            new OracleParameter("P_DEVICEID", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = DbValue(model.deviceid)
            },
            new OracleParameter("P_LAN", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = DbValue(model.lan)
            },
            new OracleParameter("P_VERSIONCODE", OracleDbType.Decimal, ParameterDirection.Input)
            {
                Value = DbValue(model.versioncode)
            },
            new OracleParameter("P_VERSIONNAME", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = DbValue(model.versionname)
            },
            new OracleParameter("P_OSVERSION", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = DbValue(model.osversion)
            },
            new OracleParameter("P_KERNELVERSION", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = DbValue(model.kernelversion)
            },
            new OracleParameter("P_FERMWAREVIRSION", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = DbValue(model.fermwarevirsion)
            },
            new OracleParameter("P_LATITUDE", OracleDbType.Decimal, ParameterDirection.Input)
            {
                Value = DbValue(model.latitude)
            },
            new OracleParameter("P_LONGITUDE", OracleDbType.Decimal, ParameterDirection.Input)
            {
                Value = DbValue(model.longitude)
            },
            new OracleParameter("P_LAC", OracleDbType.Decimal, ParameterDirection.Input)
            {
                Value = DbValue(model.lac)
            },
            new OracleParameter("P_CID", OracleDbType.Decimal, ParameterDirection.Input)
            {
                Value = DbValue(model.cid)
            },
            new OracleParameter("P_IS_BP", OracleDbType.Decimal, ParameterDirection.Input)
            {
                Value = DbValue(model.is_bp)
            },
            new OracleParameter("P_BP_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = DbValue(model.bp_msisdn)
            },
            new OracleParameter("P_DEVICE_MODEL", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = DbValue(model.device_model)
            },
            new OracleParameter("P_REQ_BLOB", OracleDbType.Blob, ParameterDirection.Input)
            {
                Value = ToBlobBytes(requestData)
            },
            new OracleParameter("P_RES_BLOB", OracleDbType.Blob, ParameterDirection.Input)
            {
                Value = ToBlobBytes(responseData)
            },
            new OracleParameter("P_REMARKS", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = DbValue(remarks)
            }
        };

                result = await _oracleDataManagerV2.CallInsertProcedure(
                    "USERFAILEDLOGINATTEMPTINSERTV2",
                    parameters.ToArray());

                return result;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "SaveFailedLoginAtmInfo",
                    procedure_name = "USERFAILEDLOGINATTEMPTINSERTV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }
        public async Task<int> IsSecurityTokenValid(string loginProvider)
        {
            int status = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter() { ParameterName = "p_Login_Provider", Value = loginProvider }
                };

                status = await _oracleDataManagerV2.CallInsertProcedureV3("LOGINPROVIDERVALIDORNOT", parameters.ToArray());

                return status;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "IsSecurityTokenValid",
                    procedure_name = "LOGINPROVIDERVALIDORNOT",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }
        public async Task<int> IsAESEligibleUser(string retailer)
        {
            int status = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter() { ParameterName = "P_RETAILER_ID", Value = retailer }
                };

                status = await _oracleDataManagerV2.CallInsertProcedureV3("BSSCHECKELIGIBLEUSER", parameters.ToArray());

                return status;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "IsAESEligibleUser",
                    procedure_name = "BSSCHECKELIGIBLEUSER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                ////_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }
        public async Task<int> ChangePassword(VMChangePassword model)
        {
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_OLD_PASS", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.old_password },
                    new OracleParameter("P_NEW_PASS", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.new_password },
                    new OracleParameter("P_USERNAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.username }
                };

                var data = await _oracleDataManagerV2.CallSelectDataWithObjectReturn("CHANGEPASSWORD", "po_PKValue", parameters.ToArray());

                return Convert.ToInt32(data.ToString());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ChangePassword",
                    procedure_name = "CHANGEPASSWORD",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<int> ChangePasswordV2(VMChangePassword model)
        {
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_OLD_PASS", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.old_password },
                    new OracleParameter("P_NEW_PASS", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.new_password },
                    new OracleParameter("P_USERNAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.username }
                };

                var data = await _oracleDataManagerV2.CallSelectDataWithObjectReturn("CHANGEPASSWORDV2", "po_PKValue", parameters.ToArray());

                return Convert.ToInt32(data.ToString());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ChangePasswordV2",
                    procedure_name = "CHANGEPASSWORDV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<int> ChangePasswordV3(VMChangePassword model)
        {
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_OLD_PASS", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.old_password },
                    new OracleParameter("P_NEW_PASS", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.new_password },
                    new OracleParameter("P_USERNAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.username }
                };

                var data = await _oracleDataManagerV2.CallSelectDataWithObjectReturn("CHANGEPASSWORDV3", "po_PKValue", parameters.ToArray());

                return Convert.ToInt32(data.ToString());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ChangePasswordV3",
                    procedure_name = "CHANGEPASSWORDV3",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<DataTable> GetPasswordLength()
        {
            DataTable dataRows;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("po_PKValue", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                return dataRows = await _oracleDataManagerV2.SelectProcedure("GETPASSWORDLENGTH", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetPasswordLength",
                    procedure_name = "GETPASSWORDLENGTH",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<DataTable> GetPasswordLengthV2()
        {
            DataTable dataRows;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("po_PKValue", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                return dataRows = await _oracleDataManagerV2.SelectProcedure("GETPASSWORDLENGTHV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetPasswordLengthV2",
                    procedure_name = "GETPASSWORDLENGTHV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }


        //================New Forget PWD =================
        public async Task<DataTable> GetUserMobileNoAndOTP(string userName)
        {
            DataTable dataTable;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_USERNAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userName },
                    new OracleParameter("PO_USERINFO", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                dataTable = await _oracleDataManagerV2.SelectProcedure("GETUSERMOBILENO", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetUserMobileNoAndOTP",
                    procedure_name = "GETUSERMOBILEANDOTP",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return dataTable;
        }

        //================New Forget PWD =================
        public async Task<DataTable> GetUserMobileNoAndOTPV2(string userName)
        {
            //_oracleDataManager = new OracleDataManager();
            DataTable dataTable;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_USERNAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userName },
                    new OracleParameter("PO_USERINFO", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                dataTable = await _oracleDataManagerV2.SelectProcedure("GETUSERMOBILENOV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetUserMobileNoAndOTPV2",
                    procedure_name = "GETUSERMOBILENOV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return dataTable;
        }

        public async Task<int> FORGETPWD(VMForgetPWD model)
        {
            int result = 0;
            long? values = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_USERID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.user_id },
                    new OracleParameter("P_MOBILENO", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.mobile_no },
                    new OracleParameter("P_NEW_PWD", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.new_pwd },
                    new OracleParameter("P_NEW_HASHPWD", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.new_hashed_pwd },
                };

                values = await _oracleDataManagerV2.CallInsertProcedure("FORGETPWD", parameters.ToArray());
                result = Convert.ToInt32(values);
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "FORGETPWD",
                    procedure_name = "FORGETPWD",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);

            }
            return result;
        }

        public async Task<int> FORGETPWDV2(VMForgetPWD model)
        {
            //_oracleDataManager = new OracleDataManager();
            int result;
            long? values = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_USERID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.user_id },
                    new OracleParameter("P_MOBILENO", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.mobile_no },
                    new OracleParameter("P_NEW_PWD", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.new_pwd },
                    new OracleParameter("P_NEW_HASHPWD", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.new_hashed_pwd }
                };
                values = await _oracleDataManagerV2.CallInsertProcedure("FORGETPWDV2", parameters.ToArray());
                result = Convert.ToInt32(values);
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "FORGETPWDV2",
                    procedure_name = "FORGETPWDV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<int> FORGETPWDV3(VMForgetPWD model)
        {
            //_oracleDataManager = new OracleDataManager();
            int result;
            long? values = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_USERID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.user_id },
                    new OracleParameter("P_MOBILENO", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.mobile_no },
                    new OracleParameter("P_NEW_PWD", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.new_pwd },
                    new OracleParameter("P_NEW_HASHPWD", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.new_hashed_pwd }
                };

                values = await _oracleDataManagerV2.CallInsertProcedure("FORGETPWDV3", parameters.ToArray());
                result = Convert.ToInt32(values);
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "FORGETPWDV2",
                    procedure_name = "FORGETPWDV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }
        //============x==================

        public async Task<DataTable> IsUserCurrentlyLoggedIn(decimal userId)
        {
            //_oracleDataManager = new OracleDataManager();
            DataTable dataTable;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_USER_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = userId },
                    new OracleParameter("PO_LOGIN_PROVIDER", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                dataTable = await _oracleDataManagerV2.SelectProcedure("ISUSERCURRENTLYLOGGEDIN", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "IsUserCurrentlyLoggedIn",
                    procedure_name = "ISUSERCURRENTLYLOGGEDIN",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);

            }
            return dataTable;
        }


        public async Task<int> IsSecurityTokenValid2(string loginProvider, string deviceId)
        {
            int status = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter() { ParameterName = "P_DEVICEID", Value = deviceId },
                    new OracleParameter() { ParameterName = "P_LOGIN_PROVIDER", Value = loginProvider }
                };
                status = await _oracleDataManagerV2.CallInsertProcedureV3("BIA_LOGINPROVIDERVALIDORNOT", parameters.ToArray());

                return status;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "IsSecurityTokenValid2",
                    procedure_name = "BIA_LOGINPROVIDERVALIDORNOT",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<long> IsSecurityTokenValidV3(string loginProvider, string deviceId)
        {
            long status = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter() { ParameterName = "P_DEVICEID", Value = deviceId },
                    new OracleParameter() { ParameterName = "P_LOGIN_PROVIDER", Value = loginProvider }
                };

                status = await _oracleDataManagerV2.CallInsertProcedureV3("BIA_LOGINPROVIDERVALIDORNOTV3", parameters.ToArray());

                return status;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "IsSecurityTokenValidV3",
                    procedure_name = "BIA_LOGINPROVIDERVALIDORNOTV3",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<long> IsSecurityTokenValidForBPLogin(string loginProvider, string deviceId)
        {

            long status = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter() { ParameterName = "P_DEVICEID", Value = deviceId },
                    new OracleParameter() { ParameterName = "P_LOGIN_PROVIDER", Value = loginProvider }
                };

                status = await _oracleDataManagerV2.CallInsertProcedureV3("BIA_CHKLOGINPROVIDFORBPLOGIN", parameters.ToArray());

                return status;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "IsSecurityTokenValidV3",
                    procedure_name = "BIA_LOGINPROVIDERVALIDORNOTV3",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<DataTable> GetChangePasswordGlobalSettingsData()
        {
            DataTable dataRows;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("po_PKValue", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                return dataRows = await _oracleDataManagerV2.SelectProcedure("GETDATAFORPWDCHANGE", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetChangePasswordGlobalSettingsData",
                    procedure_name = "GETDATAFORPWDCHANGE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }
        public async Task<DataTable> GetChangePasswordGlobalSettingsDataV2()
        {
            DataTable dataRows;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("po_PKValue", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                return dataRows = await _oracleDataManagerV2.SelectProcedure("GETDATAFORPWDCHANGEV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetChangePasswordGlobalSettingsDataV2",
                    procedure_name = "GETDATAFORPWDCHANGE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<DataTable> ValidateDbssUser(vmUserInfo model)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.user_name },
                    new OracleParameter("PI_PASSWORD", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.password },
                    new OracleParameter("PO_QC_USER", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("VALIDATE_DBSS_USER", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ValidateDbssUser",
                    procedure_name = "VALIDATE_DBSS_USER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<DataTable> ValidateBPUser(string bp_msisdn, string user_name)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = user_name },
                    new OracleParameter("P_BP_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = bp_msisdn },
                    new OracleParameter("PO_CURR", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("VALIDATE_BP_USER", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ValidateBPUser",
                    procedure_name = "VALIDATE_BP_USER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);

            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<DataTable> ValidateBPUserV1(string bp_msisdn, string user_name)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = user_name },
                    new OracleParameter("P_BP_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = bp_msisdn },
                    new OracleParameter("PO_CURR", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("VALIDATE_BP_USERV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ValidateBPUserV1",
                    procedure_name = "VALIDATE_BP_USERV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                ////_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);

            }
            return result;
        }

        public async Task<int> GenerateBPLoginOTP(string loginProvider)
        {
            int status = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter() { ParameterName = "P_LOGIN_PROVIDER", Value = loginProvider }
                };

                status = await _oracleDataManagerV2.CallInsertProcedureV3("BIA_GENERATEBPLOGINOTP", parameters.ToArray());

                return status;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GenerateBPLoginOTP",
                    procedure_name = "BIA_GENERATEBPLOGINOTP",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<int> GenerateBPLoginOTPV2(string loginProvider)
        {
            int status = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter() { ParameterName = "P_LOGIN_PROVIDER", Value = loginProvider }
                };
                status = await _oracleDataManagerV2.CallInsertProcedureV3("BIA_GENERATEBPLOGINOTPV2", parameters.ToArray());

                return status;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GenerateBPLoginOTPV2",
                    procedure_name = "BIA_GENERATEBPLOGINOTPV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                ////_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<DataTable> ValidateBPOtp(decimal bp_otp, decimal retailer_otp, string sessionToken)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_SESSION_TOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = sessionToken },
                    new OracleParameter("P_BP_OTP", OracleDbType.Decimal, ParameterDirection.Input) { Value = bp_otp },
                    new OracleParameter("P_RETAILER_OTP", OracleDbType.Decimal, ParameterDirection.Input) { Value = retailer_otp },
                    new OracleParameter("PO_CURR", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("VALIDATE_BP_OTP", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ValidateBPOtp",
                    procedure_name = "VALIDATE_BP_OTP",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<DataTable> ValidateBPOtpV2(decimal bp_otp, decimal retailer_otp, string sessionToken)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_SESSION_TOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = sessionToken },
                    new OracleParameter("P_BP_OTP", OracleDbType.Decimal, ParameterDirection.Input) { Value = bp_otp },
                    new OracleParameter("P_RETAILER_OTP", OracleDbType.Decimal, ParameterDirection.Input) { Value = retailer_otp },
                    new OracleParameter("PO_CURR", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("VALIDATE_BP_OTPV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ValidateBPOtpV2",
                    procedure_name = "VALIDATE_BP_OTPV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<int> ResendBPOTP(string loginProviderId)
        {
            int status = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter() { ParameterName = "P_SESSION_TOKEN", Value = loginProviderId }
                };

                status = await _oracleDataManagerV2.CallInsertProcedureV3("BIA_RESENDBPOTP", parameters.ToArray());

                return status;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ResendBPOTP",
                    procedure_name = "BIA_RESENDBPOTP",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<int> ResendBPOTPV2(string loginProviderId)
        {
            int status = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter() { ParameterName = "P_SESSION_TOKEN", Value = loginProviderId }
                };
                status = await _oracleDataManagerV2.CallInsertProcedureV3("BIA_RESENDBPOTPV2", parameters.ToArray());

                return status;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "ResendBPOTPV2",
                    procedure_name = "BIA_RESENDBPOTPV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<long> Logout(string loginProvider)
        {
            long result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_LOGIN_PROVIDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = loginProvider }
                };

                result = await _oracleDataManagerV2.CallInsertProcedure("BIA_USER_LOGOUT", parameters.ToArray());
                return result;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "Logout",
                    procedure_name = "BSS_USER_LOGOUT",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }
        #endregion

        public async Task<DataTable> GetUnpairedMSISDNSearchDefaultValue(UnpairedMSISDNListReqModel model)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("PO_PURS", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETMSISDNDEFAULTVALUE", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetUnpairedMSISDNSearchDefaultValue",
                    procedure_name = "BIA_GETMSISDNDEFAULTVALUE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetUnpairedMSISDNSearchDefaultValueV2(UnpairedMSISDNListReqModel model)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("PO_PURS", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETMSISDNDEFAULTVALUEV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetUnpairedMSISDNSearchDefaultValueV2",
                    procedure_name = "BIA_GETMSISDNDEFAULTVALUEV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetStockAvailable(string channel_Name)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CHANNEL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = channel_Name },
                    new OracleParameter("PO_CHANNEL_ID", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETSTOCKAVAILABLE", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetStockAvailable",
                    procedure_name = "BIA_GETSTOCKAVAILABLE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }
        public async Task<decimal> UpdateOrder(OrderRequest3 model)
        {
            decimal BIAReqsTokenId = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number.HasValue ? model.bi_token_number : null },
                    new OracleParameter("P_BSS_REQ_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bss_reqId },
                    new OracleParameter("P_STATUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.status },
                    new OracleParameter("P_ERROR_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.error_id },
                    new OracleParameter("P_ERROR_DESCRIPTION", OracleDbType.Varchar2, ParameterDirection.Input) { Value =string.IsNullOrEmpty( model.error_description) ? string.Empty : ( model.error_description.Length > 1000 ?  model.error_description.Substring(0, 1000) :  model.error_description) },
                    new OracleParameter("P_MSISDN_RESERVATION_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdnReservationId },
                    new OracleParameter("P_DEST_IMSI", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_imsi }
                };

                var result = await _oracleDataManagerV2.CallInsertProcedure("UPDATEORDER", parameters.ToArray());

                BIAReqsTokenId = Convert.ToDecimal(result);
            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "UpdateOrder",
                    procedure_name = "UPDATEORDER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "UpdateOrder",
                    procedure_name = "UPDATEORDER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
            }
            return BIAReqsTokenId;
        }

        public async Task<DataTable> GetPaymentMethod(RAGetPaymentMehtodRequest model)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.channel_id },
                    new OracleParameter("PO_PAYMENT_METHOD", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETPAYMENTMETHOD", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "GetPaymentMethod",
                    procedure_name = "BIA_GETPAYMENTMETHOD",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetPaymentMethodV2(RAGetPaymentMehtodRequest model, string userName)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.channel_id },
                    ///new OracleParameter("P_USERNAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userName });
                    new OracleParameter("PO_PAYMENT_METHOD", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETPAYMENTMETHOD", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "GetPaymentMethod",
                    procedure_name = "BIA_GETPAYMENTMETHODV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        #region Geofencing
        public async Task<DataTable> GetLoggedinRetLatLon(string retailerCode)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = retailerCode },
                    new OracleParameter("PO_CURSOR", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETRETLATLON", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(retailerCode),
                    method_name = "GetLoggedinRetLatLon",
                    procedure_name = "BIA_GETRETLATLON",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }
        #endregion
        #region Retailer user synchronization
        public async Task<decimal?> UpdateRetailerUserByDMS(DMSRetailerReqModel model)
        {
            decimal? successNumber = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailerCode },
                    new OracleParameter("P_IS_ACTIVE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.isActive },
                    new OracleParameter("P_ITOPUPNUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.iTopUpNumber }
                };

                var result = await _oracleDataManagerV2.CallInsertProcedure("BIA_UPDATE_DMS_USER", parameters.ToArray());

                successNumber = Convert.ToDecimal(result);
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "UpdateRetailerUserByDMS",
                    procedure_name = "BIA_UPDATE_DMS_USER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }

            return successNumber;
        }
        #endregion

        #region First Recharge
        public async Task<DataTable> GetRechargeAmount(RechargeAmountReqModel model, string userName)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userName },
                    new OracleParameter("PO_AMOUNT", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETRCHRGAMOUNTV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetRechargeAmount",
                    procedure_name = "BIA_GETRCHRGAMOUNTV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<long> UpdateOrderFirstRechargeStatus(long RequestId)
        {
            long response = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = RequestId }
                };

                var result = await _oracleDataManagerV2.CallInsertProcedure("UPDATEORDERFIRSTRECHARGESTATUS", parameters.ToArray());

                response = Convert.ToInt64(result);
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "SUBMITCOMPLAINT",
                    procedure_name = "UPDATEORDERCOMPLAINTSTATUS",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,

                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return response;
        }

        #endregion
        #region Help button Area
        public async Task<DataTable> GetUserTypeDropdownValu()
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PO_USERTYPE", OracleDbType.RefCursor, ParameterDirection.Output)
                };
                result = await _oracleDataManagerV2.SelectProcedure("GETUSERTYPEVALUE", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetUserTypeDropdownValu",
                    procedure_name = "GETUSERTYPEVALUE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetContentTypeDropdownValue()
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PO_CONTENT", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("GETCONTENTTYPEVALUE", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetContentTypeDropdownValue",
                    procedure_name = "GETCONTENTTYPEVALUE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetContentURL()
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PO_CONTENTURL", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("GETCONTENTURL", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetContentURL",
                    procedure_name = "GETCONTENTURL",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }


        #endregion

        #region Raise Complaint
        public async Task<long> SubmitComplaint(SubmitComplaintModel model)
        {
            long ComplaintId = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_COMPLAINT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.complaintType },
                    new OracleParameter("P_COMPLAINT_TITLE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.complaintTitle },
                    new OracleParameter("P_DESCRIPTION", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.description },
                    new OracleParameter("P_PREFERRED_LEVEL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.preferredLevel },
                    new OracleParameter("P_PREFERRED_LEVEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.preferredLevelName },
                    new OracleParameter("P_PREFERRED_LEVEL_CONTACT", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.preferredLevelContact },
                    new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailerCode }
                };

                var result = await _oracleDataManagerV2.CallInsertProcedure("SUBMITCOMPLAINT", parameters.ToArray());

                ComplaintId = result;
            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailerCode,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SUBMITCOMPLAINT",
                    procedure_name = "SUBMITCOMPLAINT",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,

                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return ComplaintId;
        }

        public async Task<decimal> UpdateOrderComplaintStatus(decimal RequestId)
        {
            decimal response = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = RequestId }
                };
                var result = await _oracleDataManagerV2.CallInsertProcedure("UPDATEORDERCOMPLAINTSTATUS", parameters.ToArray());

                response = Convert.ToDecimal(result);
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {

                    request_time = DateTime.Now,
                    method_name = "SUBMITCOMPLAINT",
                    procedure_name = "UPDATEORDERCOMPLAINTSTATUS",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,

                });
                throw new Exception("OuterDetails: " + text, ex);
            }
            return response;
        }


        #endregion

        #region Resubmit
        public async Task<DataTable> GetResubmitData(ResubmitReqModel model)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_TOKEN_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number },
                    new OracleParameter("PO_RESULT", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETRESUBMITINFO", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "GetResubmitData",
                    procedure_name = "BIA_GETRESUBMITINFO",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }
        #endregion
        #region App info Update from Retailer
        public async Task<long> AppInfoUpdate(AppInfoUpdateReqModel model, string loginProvider)
        {
            long result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_LOGIN_PROVIDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = loginProvider },
                    new OracleParameter("P_VERSION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.app_version_code },
                    new OracleParameter("P_VERSION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.app_version_name }
                };

                result = await _oracleDataManagerV2.CallInsertProcedure("BIA_APP_INFO_UPD", parameters.ToArray());
                return result;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "AppInfoUpdate",
                    procedure_name = "BIA_APP_INFO_UPD",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
        }
        #endregion

        public async Task<DataTable> GetBTSCode(SiteIdRequestModel model)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_LAC", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.lac },
                    new OracleParameter("P_CID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.cid },
                    new OracleParameter("PO_BTSCODE", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETGETBTS_CODE", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetBTSCode",
                    procedure_name = "BIA_GETGETBTS_CODE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetOfferId(string channelName, string userName, string bi_token_number)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = channelName },
                    new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userName },
                    new OracleParameter("P_BI_TOKEN", OracleDbType.Double, ParameterDirection.Input) { Value = Convert.ToDouble(bi_token_number) },
                    new OracleParameter("PO_OFFERID", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("GET_OFFERIDBYCHANNEL", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetOfferId",
                    procedure_name = "GET_OFFERIDBYCHANNEL",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetOfferIdV2(string channelName, string userName, string bi_token_number, int is_lus)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = channelName },
                    new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userName },
                    new OracleParameter("P_BI_TOKEN", OracleDbType.Double, ParameterDirection.Input) { Value = Convert.ToDouble(bi_token_number) },
                    new OracleParameter("P_IS_LUS", OracleDbType.Int32, ParameterDirection.Input) { Value = Convert.ToInt32(is_lus) },
                    new OracleParameter("PO_OFFERID", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("GET_OFFERIDBYCHANNELV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetOfferId",
                    procedure_name = "GET_OFFERIDBYCHANNEL",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }


        #region ====================| FTR Restriction | ==================
        public async Task<string> GetRetailerItopUpNumber(string userName)
        {
            string msisdn = string.Empty;
            DataTable dataTable = new DataTable();

            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_USERNAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userName },
                    new OracleParameter("PO_MSISDN", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                dataTable = await _oracleDataManagerV2.SelectProcedure("BIA_GETRETAILERMSISDN", parameters.ToArray());

                foreach (DataRow dtRow in dataTable.Rows)
                {
                    msisdn = dtRow["MOBILE_NUMBER"].ToString() ?? "";
                }

                return msisdn;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetRetailerItopUpNumber",
                    procedure_name = "BIA_GETRETAILERMSISDN",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task FTR_UpdateData(FTRDBUpdateModel model)
        {
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Long, ParameterDirection.Input) { Value = model.bi_token_no },
                    new OracleParameter("P_ISFTR_RESTRICTED", OracleDbType.Int32, ParameterDirection.Input) { Value = model.is_ftr_restricted },
                    new OracleParameter("P_FTR_MESSAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.ftr_message }
                };

                bool? log_id = await _oracleDataManagerV2.CallUpdateProcedure("BIA_FTR_RESTRICTIONUPD", parameters.ToArray());
                if (log_id == false)
                {
                    throw new Exception();
                }
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "FTR_UpdateData",
                    procedure_name = "BIA_FTR_RESTRICTIONUPD",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task LUS_FTR_UpdateData(FTRDBUpdateModel model)
        {
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Long, ParameterDirection.Input) { Value = model.bi_token_no },
                    new OracleParameter("P_ISLUS_RESTRICTED", OracleDbType.Int32, ParameterDirection.Input) { Value = model.is_ftr_restricted }
                };

                bool? log_id = await _oracleDataManagerV2.CallUpdateProcedure("BIA_LUS_RESTRICTIONUPD", parameters.ToArray());
                if (log_id == false)
                {
                    throw new Exception();
                }
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "FTR_UpdateData",
                    procedure_name = "BIA_FTR_RESTRICTIONUPD",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }
        }
        #endregion

        public async Task<DataTable> GetBlackListedWordForAddress()
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PO_BLACKLISTEDWORD", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETBLACKLISTED_ADDR", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    //request_model = Convert.ToString(model),
                    method_name = "GetBlackListedWordForAddress",
                    procedure_name = "BIA_GETBLACKLISTED_ADDR",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetBlackListedWordForName()
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PO_BLACKLISTEDWORD", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETBLACKLISTED_NAME", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetBlackListedWordForName",
                    procedure_name = "BIA_GETBLACKLISTED_NAME",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> CheckUser(UserCheckModel userModel)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userModel.user_name },
                    new OracleParameter("PI_BP_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userModel.bpmsisdn },
                    new OracleParameter("PO_USER", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("CHECK_USER_STATUS", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "CheckUser",
                    procedure_name = "CHECK_USER_STATUS",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> FetchFingerPrint(FPValidationReqModel userModel)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userModel.user_name },
                    new OracleParameter("PO_FINGERPRINT", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("GET_FINGERPRINT", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "FetchFingerPrint",
                    procedure_name = "GET_FINGERPRINT",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<long?> SaveFingerPrint(FPRegistrationModel userModel)
        {
            long? result = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userModel.user_name },
                    new OracleParameter("PI_RIGHT_THUMB", OracleDbType.Clob, ParameterDirection.Input) { Value = userModel.right_thumb },
                    new OracleParameter("PI_RIGHT_THUMB_SCORE", OracleDbType.Int32, ParameterDirection.Input) { Value = userModel.right_thumb_score },
                    new OracleParameter("PI_RIGHT_INDEX", OracleDbType.Clob, ParameterDirection.Input) { Value = userModel.right_index },
                    new OracleParameter("PI_RIGHT_INDEX_SCORE", OracleDbType.Int32, ParameterDirection.Input) { Value = userModel.right_index_score },
                    new OracleParameter("PI_LEFT_THUMB", OracleDbType.Clob, ParameterDirection.Input) { Value = userModel.left_thumb },
                    new OracleParameter("PI_LEFT_THUMB_SCORE", OracleDbType.Int32, ParameterDirection.Input) { Value = userModel.left_thumb_score },
                    new OracleParameter("PI_LEFT_INDEX", OracleDbType.Clob, ParameterDirection.Input) { Value = userModel.left_index },
                    new OracleParameter("PI_LEFT_INDEX_SCORE", OracleDbType.Int32, ParameterDirection.Input) { Value = userModel.left_index_score },
                    new OracleParameter("PI_MOBILE_NO", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userModel.mobile_no }
                };

                result = await _oracleDataManagerV2.CallInsertProcedure("SAVE_FINGERPRINT", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "SaveFingerPrint",
                    procedure_name = "SAVE_FINGERPRINT",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> GetFingerPrintResult(double? bi_token)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_BI_TOKEN", OracleDbType.Decimal, ParameterDirection.Input) { Value = bi_token },
                    new OracleParameter("PO_FINGERPRINT", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("GET_FINGERPRINT_RESULT", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetFingerPrintResult",
                    procedure_name = "GET_FINGERPRINT_RESULT",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> GetScannerInfo(ScannerInfoReqModel model)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PO_SCANNER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.scanner_id },
                    new OracleParameter("PO_CURSOR", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETSCANNERINFO", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetScannerInfo",
                    procedure_name = "BIA_GETSCANNERINFO",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetRetailerNIDDOB(string username)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_USERNAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = username },
                    new OracleParameter("PO_NID_DOB", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("GET_RETAILER_NIDDOB", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetRetailerNIDDOB",
                    procedure_name = "GET_RETAILER_NIDDOB",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> GetIsRegistered(string userName)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userName },
                    new OracleParameter("PO_ISREGISTERED", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("GET_ISREGISTERED", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetIsRegistered",
                    procedure_name = "GET_ISREGISTERED",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> GetUpdateAPKVersion()
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PO_APPVERSION", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETAPPVERSION", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetUpdateAPKVersion",
                    procedure_name = "BIA_GETGETBTS_CODE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetTOSFeeAsync(string channel, string productType)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
            {
                new OracleParameter("P_CHANNEL", OracleDbType.Varchar2, channel, ParameterDirection.Input),
                new OracleParameter("P_PRODUCTTYPE", OracleDbType.Varchar2, productType, ParameterDirection.Input),
                new OracleParameter("PO_AMOUNT", OracleDbType.RefCursor, ParameterDirection.Output)
            };

                result = await _oracleDataManagerV2.SelectProcedure("GET_TOS_FEE", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetTOSFeeAsync",
                    procedure_name = "GET_TOS_FEE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> GetProductValueForSearChingSIM(UnpairedSIMsearchReqModelV2 model)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("P_CHANNEL_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_id },
                    new OracleParameter("P_RIGHT_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.right_id },
                    new OracleParameter("P_RETAILER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_code },
                    new OracleParameter("PO_PRODUCT", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("GETPRODUCTVALUESIMSEARCHING", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetProductValueForSearChingSIM",
                    procedure_name = "GETPRODUCTVALUESIMSEARCHING",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        #region Cherish Number Sell
        public async Task<DataTable> GetCherishCategoryData(string channelName)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = channelName },
                    new OracleParameter("PO_RESULT", OracleDbType.RefCursor, ParameterDirection.Output)
                };


                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETCATEGORIES", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetCherishCategoryData",
                    procedure_name = "BIA_GETCATEGORIES",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }
        public async Task<DataTable> GetDesiredCatMessage(string CategoryName, string channel_name)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                new OracleParameter("P_CATEGORY_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = CategoryName },
                new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = channel_name },
                new OracleParameter("PO_RESULT", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETCATEGORYMESSAGE", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetDesiredCatMessage",
                    procedure_name = "BIA_GETCATEGORYMESSAGE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetSubscriptionsTypes(RASubscriptionTypeReq model)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("PO_PURS", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETSUBSCRIPTIONTYPES", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetSubscriptionsTypes",
                    procedure_name = "BIA_GETSUBSCRIPTIONTYPES",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetCategoryMinAmount(string category)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CATEGORY_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = category },
                    new OracleParameter("PO_PURS", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETCATEGORYAMOUNT", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetCategoryMinAmount",
                    procedure_name = "BIA_GETCATEGORYAMOUNT",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetRechargeAmountV2(RechargeAmountReqModelRev model, string userName)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userName },
                    new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number },
                    new OracleParameter("PO_AMOUNT", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETRCHRGAMOUNTV3", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetRechargeAmount",
                    procedure_name = "BIA_GETRCHRGAMOUNTV3",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }
        public async Task<DataTable> GetRechargeAmountV3(RechargeAmountReqModelRevV3 model, string userName)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userName },
                    new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number },
                    new OracleParameter("PO_AMOUNT", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETRCHRGAMOUNTV4", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetRechargeAmount",
                    procedure_name = "BIA_GETRCHRGAMOUNTV4",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }
        public async Task<DataTable> GetIsLusEligibleAsync(string btsCode)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_BTS_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = btsCode },
                    new OracleParameter("P_IS_ELIGIBLE", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETISLUSELIGIBLE", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetIsLusEligibleAsync",
                    procedure_name = "BIA_GETISLUSELIGIBLE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }
        public async Task<DataTable> GetLUSEligiblefromBIA(string bi_token_number)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
            {
                new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = bi_token_number },
                new OracleParameter("PO_CURSOR", OracleDbType.RefCursor, ParameterDirection.Output)
            };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_LUSSTATUS", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetLUSEligiblefromBIA",
                    procedure_name = "BIA_LUSSTATUS",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }
        public async Task<DataTable> SubmitOrderV8(OrderRequest4 model, string loginProviderId)
        {
            DataTable orderResponse = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_BI_TOKEN_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.bi_token_number.HasValue ? model.bi_token_number : null },
                    new OracleParameter("P_BSS_ReqId", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.bss_reqId },
                    new OracleParameter("P_Status", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.status },
                    new OracleParameter("P_error_id", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.error_id },
                    new OracleParameter("p_error_description", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.error_description },
                    new OracleParameter("P_PURPOSE_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.purpose_number.HasValue ? model.purpose_number : null },
                    new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdn },
                    new OracleParameter("P_DEST_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_category.HasValue ? model.sim_category : null },
                    new OracleParameter("P_DEST_SIM_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_number },
                    new OracleParameter("P_SUBSCRIPTION_TYPE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.subscription_type_id.HasValue ? model.subscription_type_id : null },
                    new OracleParameter("P_SUBSCRIPTION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_code },
                    new OracleParameter("P_PACKAGE_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.package_id.HasValue ? model.package_id : null },
                    new OracleParameter("P_PACKAGE_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.package_code },
                    new OracleParameter("P_DEST_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_doc_type_no.HasValue ? model.dest_doc_type_no : null },
                    new OracleParameter("P_DEST_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_nid },
                    new OracleParameter("P_DEST_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_dob },
                    new OracleParameter("P_SRC_DOC_TYPE_NO", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_doc_type_no.HasValue ? model.src_doc_type_no : null },
                    new OracleParameter("P_SRC_NID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_nid },
                    new OracleParameter("P_SRC_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_dob },
                    new OracleParameter("P_PLATFORM_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.platform_id },
                    new OracleParameter("P_CUSTOMER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_name },
                    new OracleParameter("P_GENDER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.gender },
                    new OracleParameter("P_FLAT_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.flat_number },
                    new OracleParameter("P_HOUSE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.house_number },
                    new OracleParameter("P_ROAD_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.road_number },
                    new OracleParameter("P_VILLAGE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.village },
                    new OracleParameter("P_DIVISION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.division_id.HasValue ? model.division_id : null },
                    new OracleParameter("P_DISTRICT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.district_id.HasValue ? model.district_id : null },
                    new OracleParameter("P_THANA_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.thana_id.HasValue ? model.thana_id : null },
                    new OracleParameter("P_POSTAL_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.postal_code },
                    new OracleParameter("P_EMAIL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.email },
                    new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_code },
                    new OracleParameter("P_DEST_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_thumb_score.HasValue ? model.dest_left_thumb_score : null },
                    new OracleParameter("P_DEST_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_thumb },
                    new OracleParameter("P_DEST_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_left_index_score.HasValue ? model.dest_left_index_score : null },
                    new OracleParameter("P_DEST_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_left_index },
                    new OracleParameter("P_DEST_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_thumb_score.HasValue ? model.dest_right_thumb_score : null },
                    new OracleParameter("P_DEST_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_thumb },
                    new OracleParameter("P_DEST_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_right_index_score.HasValue ? model.dest_right_index_score : null },
                    new OracleParameter("P_DEST_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.dest_right_index },
                    new OracleParameter("P_SRC_LEFT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_thumb_score.HasValue ? model.src_left_thumb_score : null },
                    new OracleParameter("P_SRC_LEFT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_thumb },
                    new OracleParameter("P_SRC_LEFT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_left_index_score.HasValue ? model.src_left_index_score : null },
                    new OracleParameter("P_SRC_LEFT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_left_index },
                    new OracleParameter("P_SRC_RIGHT_THUMB_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_thumb_score.HasValue ? model.src_right_thumb_score : null },
                    new OracleParameter("P_SRC_RIGHT_THUMB", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_thumb },
                    new OracleParameter("P_SRC_RIGHT_INDEX_SCORE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_right_index_score.HasValue ? model.src_right_index_score : null },
                    new OracleParameter("P_SRC_RIGHT_INDEX", OracleDbType.Blob, ParameterDirection.Input) { Value = model.src_right_index },
                    new OracleParameter("P_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                    new OracleParameter("P_PORT_IN_DATE", OracleDbType.Date, ParameterDirection.Input) { Value = model.port_in_date },
                    new OracleParameter("P_ALT_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.alt_msisdn },
                    new OracleParameter("P_POC_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.poc_number },
                    new OracleParameter("P_IS_URGENT", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_urgent.HasValue ? model.is_urgent : null },
                    new OracleParameter("P_OPTIONAL1", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional1 },
                    new OracleParameter("P_OPTIONAL2", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional2 },
                    new OracleParameter("P_OPTIONAL3", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.optional3 },
                    new OracleParameter("P_OPTIONAL4", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional4.HasValue ? model.optional4 : null },
                    new OracleParameter("P_OPTIONAL5", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional5.HasValue ? model.optional5 : null },
                    new OracleParameter("P_OPTIONAL6", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.optional6.HasValue ? model.optional6 : null },
                    new OracleParameter("P_NOTE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.note },
                    new OracleParameter("P_SIM_REP_REASON_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_rep_reason_id.HasValue ? model.sim_rep_reason_id : null },
                    new OracleParameter("P_PAYMENT_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.payment_type },
                    new OracleParameter("P_ISPAIRED", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.is_paired : null },
                    new OracleParameter("P_MSISDNRESERVATIONID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.msisdnReservationId },
                    new OracleParameter("P_CHANNEL_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_paired.HasValue ? model.cahnnel_id : null },
                    new OracleParameter("P_DIVISION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.division_name },
                    new OracleParameter("P_DISTRICT_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.district_name },
                    new OracleParameter("P_THANA_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.thana_name },
                    new OracleParameter("P_CENTER_OR_DISTRIBUTOR_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.distributor_code },
                    new OracleParameter("P_SIM_REPLC_REASON", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.sim_replc_reason },
                    new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("P_RIGHT_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.right_id },
                    new OracleParameter("P_SIM_REPLACEMENT_TYPE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.sim_replacement_type },
                    new OracleParameter("P_OLD_SIM_NUMBER", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.old_sim_number },
                    new OracleParameter("P_SRC_SIM_CATEGORY", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_sim_category },
                    new OracleParameter("P_PORT_IN_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.port_in_confirmation_code },
                    new OracleParameter("P_DEST_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_ec_verifi_reqrd },
                    new OracleParameter("P_SRC_EC_VERIFI_REQRD", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.src_ec_verifi_reqrd },
                    new OracleParameter("P_DEST_FOREIGN_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dest_foreign_flag },
                    new OracleParameter("P_DBSS_SUBSCRIPTION_ID", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.dbss_subscription_id },
                    new OracleParameter("P_SAF_STATUS", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.saf_status },
                    new OracleParameter("P_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_id },
                    new OracleParameter("P_CONFIRMATION_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_confirmation_code },
                    new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.server_name },
                    new OracleParameter("P_SRC_OWNER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_owner_customer_id },
                    new OracleParameter("P_SRC_USER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_user_customer_id },
                    new OracleParameter("P_SRC_PAYER_CUSTOMER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.src_payer_customer_id },
                    new OracleParameter("P_DEST_IMSI", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dest_imsi },
                    new OracleParameter("P_SESSION_TOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = loginProviderId },
                    new OracleParameter("P_LATITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.latitude },
                    new OracleParameter("P_LONGITUDE", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.longitude },
                    new OracleParameter("P_LAC_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.lac },
                    new OracleParameter("P_CELL_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.cid },
                    new OracleParameter("P_SCANNER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.scanner_id },
                    new OracleParameter("P_ORDER_BOOKING_FLAG", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.order_booking_flag },
                    new OracleParameter("P_IS_ESIM", OracleDbType.Decimal, ParameterDirection.Input) { Value = model.is_esim },
                    new OracleParameter("P_SELECTED_CATEGORY", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.selected_category }
                };

                orderResponse = await _oracleDataManagerV2.SelectProcedureV2("SUBMITORDER16", parameters.ToArray());

            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV8",
                    procedure_name = "SUBMITORDER16",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                throw new Exception("OuterDetails: " + text, ex);

            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "SubmitOrderV8",
                    procedure_name = "SUBMITORDER16",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                    server_name = model.server_name
                });
                throw new Exception("OuterDetails: " + text, ex);
            }
            return orderResponse;
        }
        public async Task<DataTable> GetUnpairedMSISDNSearchDefaultValueCherished(UnpairedMSISDNListReqModelV2 model)
        {
            DataTable result;
            try
            {

                List<OracleParameter> parameters = new List<OracleParameter>
                {
                new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                new OracleParameter("P_CATEGORY", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.Selected_category },
                new OracleParameter("PO_PURS", OracleDbType.RefCursor, ParameterDirection.Output)

                };
                result = await _oracleDataManagerV2.SelectProcedure("BIA_GETMSISDNDEFAULTVALCHER", parameters.ToArray());
            }
            catch (OracleException ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "GetUnpairedMSISDNSearchDefaultValueCherished",
                    procedure_name = "BIA_GETMSISDNDEFAULTVALCHER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                throw new Exception("OuterDetails: " + text, ex);

            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    retailer_id = model.retailer_id,
                    request_time = DateTime.Now,
                    request_model = Convert.ToString(model),
                    method_name = "GetUnpairedMSISDNSearchDefaultValueCherished",
                    procedure_name = "BIA_GETMSISDNDEFAULTVALCHER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });
                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }
        #endregion

        public async Task<DataTable> GetDMSSessionValues()
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PO_SESSION", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIAGETDMSSESSION", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetDMSSessionValues",
                    procedure_name = "BIAGETDMSSESSION",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task SaveDMSSession(DMSLoginResponse model)
        {
            long? result = 0;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_SESSIONTOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.Data.AccessToken },
                    new OracleParameter("P_REFRESHTOKEN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.Data.RefreshToken },
                    new OracleParameter("P_SESSIONTIME", OracleDbType.Int32, ParameterDirection.Input) { Value = model.Data.AccessTokenExpireInMinutes }
                };

                result = await _oracleDataManagerV2.CallInsertProcedure("BIA_SAVEDMSSESSION", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "SaveDMSSession",
                    procedure_name = "BIAGETDMSSESSION",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
            //return result;
        }

        public async Task<DataTable> GetRetailerTransactionNumber(string userName)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userName},
                    new OracleParameter("PO_DETAILS", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIAGETEVTRANSACTIONNUMBER", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetRetailerTransactionNumber",
                    procedure_name = "BIAGETEVTRANSACTIONNUMBER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        public async Task<DataTable> GetMSISDNStatusForTOS(string msisdn)
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_MSISDN", OracleDbType.Varchar2, ParameterDirection.Input) { Value = msisdn},
                    new OracleParameter("PO_NUMBER", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIAGETTOSBYPASSMSISDN", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetMSISDNStatusForTOS",
                    procedure_name = "BIAGETTOSBYPASSMSISDN",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }

            return result;
        }

        #region GA Capping

        public async Task SaveSingleSourceSession(SingleSourceSessionModel model)
        {
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_SESSIONTOKEN", OracleDbType.Varchar2, ParameterDirection.Input)
                    {
                        Value = string.IsNullOrWhiteSpace(model.SessionToken) ? DBNull.Value : model.SessionToken,
                        Size = 500
                    },
                    new OracleParameter("P_CREATEDDATE", OracleDbType.Date, ParameterDirection.Input)
                    {
                        Value = model.CreatedDate
                    }
                };

                await _oracleDataManagerV2.CallInsertProcedure("SAVESINGLESOURCESESSION", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "SaveSingleSourceSession",
                    procedure_name = "SAVESINGLESOURCESESSION",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<DataTable> GetGACappingConfig()
        {
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PO_CONFIG", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                DataTable result = await _oracleDataManagerV2.SelectProcedure("GETGACAPPINGCONFIG", parameters.ToArray());

                return result;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetGACappingConfig",
                    procedure_name = "GETGACAPPINGCONFIG",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message,
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        #endregion GA Capping

        public async Task<(int currentAttempt, int minutesLeft, string message)> UserLoginAttemptCount(string username, int isCredentialOk)
        {
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("p_username", OracleDbType.Varchar2)
            {
                Value = username
            },

            new OracleParameter("p_is_credential_ok", OracleDbType.Int32)
            {
                Value = isCredentialOk
            },

            new OracleParameter("p_fail_count", OracleDbType.Int32)
            {
                Direction = ParameterDirection.Output
            },

            new OracleParameter("p_minutes_left", OracleDbType.Int32)
            {
                Direction = ParameterDirection.Output
            },

            new OracleParameter("p_message", OracleDbType.Varchar2, 300)
            {
                Direction = ParameterDirection.Output
            }
                };

                await _oracleDataManagerV2.ExecuteProcedure(
                    "CheckFailedLoginAttempts",
                    parameters
                );

                int failCount = Convert.ToInt32(((OracleDecimal)parameters[2].Value).ToInt32());
                int minutesLeft = Convert.ToInt32(((OracleDecimal)parameters[3].Value).ToInt32());
                string message = parameters[4].Value?.ToString();

                return (failCount, minutesLeft, message);
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "UserLoginAttemptCount",
                    procedure_name = "CheckFailedLoginAttempts",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<DataTable> GetSingleSourceSessionValues()
        {
            DataTable result;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
            {
                new OracleParameter("PO_SESSION", OracleDbType.RefCursor, ParameterDirection.Output)
            };

                result = await _oracleDataManagerV2.SelectProcedure("GETSINGLESOURCESESSION", parameters.ToArray());

            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,

                    method_name = "GetSingleSourceSessionValues",
                    procedure_name = "GETSINGLESOURCESESSION",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<bool> InsertLoginAttempt(string username, int is_success)
        {
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
        {
            new OracleParameter("p_username", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = username
            },
            new OracleParameter("p_is_success", OracleDbType.Int32, ParameterDirection.Input)
            {
                Value = is_success
            }
        };

                bool isUpdated = await _oracleDataManagerV2.CallUpdateProcedure(
                    "InsertLoginAttempt",
                    parameters.ToArray()
                );

                return isUpdated;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "InsertLoginAttempt",
                    procedure_name = "InsertLoginAttempt",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<DataTable> RemoveFingerprint(string userModel)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_USER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = userModel },
                    new OracleParameter("PO_USER", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_DELETE_FINGERPRINT", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "RemoveFingerprint",
                    procedure_name = "BIA_DELETE_FINGERPRINT",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> CeckSIMProductMapping(SIMProductMappingReqModel model)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("PI_RIGHT_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.right_id },
                    new OracleParameter("PI_IS_BP", OracleDbType.Int32, ParameterDirection.Input) { Value = model.is_bp },
                    new OracleParameter("PI_MOBILE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.mobile_number },
                    new OracleParameter("PI_RETAILER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                    new OracleParameter("PI_CHANNEL_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.channel_id },
                    new OracleParameter("PI_PRODUCT_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.product_code },
                    new OracleParameter("PO_MAPPING", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GET_SIMMAPPING", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "CeckSIMProductMapping",
                    procedure_name = "BIA_GET_SIMMAPPING",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> CeckSIMProductMappingV2(SIMProductMappingReqModelV2 model)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("PI_RIGHT_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.right_id },
                    new OracleParameter("PI_IS_BP", OracleDbType.Int32, ParameterDirection.Input) { Value = model.is_bp },
                    new OracleParameter("PI_MOBILE_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.mobile_number },
                    new OracleParameter("PI_RETAILER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                    new OracleParameter("PI_CHANNEL_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.channel_id },
                    new OracleParameter("PI_PRODUCT_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.product_code },
                    new OracleParameter("PI_EXT_CHANNEL_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.ext_channel_type },
                    new OracleParameter("PI_EXT_ACTION_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.ext_action_type },
                    new OracleParameter("PI_EXT_SIM_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.ext_sim_type },
                    new OracleParameter("PI_EXT_STORAGE_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.ext_storage_type },
                    new OracleParameter("PO_MAPPING", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GET_SIMMAPPINGV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "CeckSIMProductMappingV2",
                    procedure_name = "BIA_GET_SIMMAPPINGV2",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> GetSubscriptionMapping(RASubscriptionTypeReqWithMapping model)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("PI_RIGHT_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.right_id },
                    new OracleParameter("PI_IS_BP", OracleDbType.Int32, ParameterDirection.Input) { Value = model.is_bp },
                    new OracleParameter("PI_RETAILER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                    new OracleParameter("PO_MAPPING", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GET_SUBSCRMAPPING", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetSubscriptionMapping",
                    procedure_name = "BIA_GET_SUBSCRMAPPING",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> GetSubscriptionMappingV2(RASubscriptionTypeReqWithMappingV2 model)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("PI_RIGHT_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.right_id },
                    new OracleParameter("PI_IS_BP", OracleDbType.Int32, ParameterDirection.Input) { Value = model.is_bp },
                    new OracleParameter("PI_RETAILER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                    new OracleParameter("PI_DPE_CHANNEL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.initiator_channel },
                    new OracleParameter("PI_OPERATION_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_type },
                    new OracleParameter("PI_SIM_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.ext_subscription_type },
                    new OracleParameter("PI_STATE_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.simkit_type },
                    new OracleParameter("PO_MAPPING", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GET_SUBSCRMAPPINGV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetSubscriptionMapping",
                    procedure_name = "BIA_GET_SUBSCRMAPPING",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> GetPackageMapping(RAGetPackageResquestV4 model)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("PI_RIGHT_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.right_id },
                    new OracleParameter("PI_IS_BP", OracleDbType.Int32, ParameterDirection.Input) { Value = model.is_bp },
                    new OracleParameter("PI_RETAILER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                    new OracleParameter("PI_SUBSCRIPTION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_name },
                    new OracleParameter("PI_DMS_OFFER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.offer_name },
                    new OracleParameter("PO_MAPPING", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GET_PACKAGEMAPPING", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetPackageMapping",
                    procedure_name = "BIA_GET_PACKAGEMAPPING",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> GetPackageMappingV2(PackagesFetchedRequestModel model)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("PI_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name },
                    new OracleParameter("PI_RIGHT_ID", OracleDbType.Int32, ParameterDirection.Input) { Value = model.right_id },
                    new OracleParameter("PI_IS_BP", OracleDbType.Int32, ParameterDirection.Input) { Value = model.is_bp },
                    new OracleParameter("PI_RETAILER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                    new OracleParameter("PI_SUBSCRIPTION_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_name },
                    new OracleParameter("PI_DMS_OFFER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.offer_name },
                    new OracleParameter("PI_DPE_CHANNEL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.initiator_channel },
                    new OracleParameter("PI_OPERATION_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.order_type },
                    new OracleParameter("PI_SIM_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.subscription_type },
                    new OracleParameter("PI_STATE_TYPE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.simkit_type },
                    new OracleParameter("PO_MAPPING", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_GET_PACKAGEMAPPINGV2", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetPackageMapping",
                    procedure_name = "BIA_GET_PACKAGEMAPPING",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }

        public async Task<DataTable> GetRecycleBaseChecking(RecycleBaseCheckingReqModel model)
        {
            DataTable result = new DataTable();
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_DOCUMENT_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.nid },
                    new OracleParameter("P_DOB", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.dob },
                    new OracleParameter("P_MSISDN", OracleDbType.Int32, ParameterDirection.Input) { Value = model.msisdn },
                    new OracleParameter("PO_CURSOR", OracleDbType.RefCursor, ParameterDirection.Output)
                };

                result = await _oracleDataManagerV2.SelectProcedure("BIA_CHECKRECYCLEBASE", parameters.ToArray());
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "GetRecycleBaseChecking",
                    procedure_name = "BIA_CHECKRECYCLEBASE",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                //_logWriter.WriteDailyLog2(text == null ? "" : text);

                throw new Exception("OuterDetails: " + text, ex);
            }
            return result;
        }


        #region Home Wifi
        public async Task<string?> GetDEPDeviceOldIdentifier(string orderNumber, string sku)
        {
            try
            {
                Log.ForContext("LogTag", "DBRequest")
                    .Information(
                        "GET_DEP_DEVICE_OLD_IDENTIFIER Request: {@Request}",
                        new
                        {
                            order_number = orderNumber,
                            sku
                        });

                OracleParameter[] parameters =
                {
            new OracleParameter("P_ORDER_NUMBER", OracleDbType.Varchar2)
            {
                Value = string.IsNullOrWhiteSpace(orderNumber)
                    ? DBNull.Value
                    : orderNumber.Trim()
            },

            new OracleParameter("P_SKU", OracleDbType.Varchar2)
            {
                Value = string.IsNullOrWhiteSpace(sku)
                    ? DBNull.Value
                    : sku.Trim()
            }
        };

                using (DataTable dt =
                    await _oracleDataManagerV2.SelectProcedureV2(
                        "BIODB.GET_DEP_DEVICE_OLD_IDENTIFIER",
                        parameters
                    ))
                {
                    Log.ForContext("LogTag", "DBRequest")
                        .Information(
                            "GET_DEP_DEVICE_OLD_IDENTIFIER Response Count: {Count}",
                            dt?.Rows.Count ?? 0);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        string? oldIdentifier = dt.Rows[0]["OLD_IDENTIFIER"]?.ToString();

                        return string.IsNullOrWhiteSpace(oldIdentifier)
                            ? null
                            : oldIdentifier.Trim();
                    }
                }

                return null;
            }
            catch
            {
                throw;
            }
        }
        public async Task BIAtoDPELog(VMBIAToDPELog model)
        {
            try
            {
                var parameters = new List<OracleParameter>
        {
            new OracleParameter("P_ORDER_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = string.IsNullOrWhiteSpace(model.order_number)
                    ? DBNull.Value
                    : model.order_number
            },

            new OracleParameter("P_USER_ID", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = string.IsNullOrWhiteSpace(model.username)
                    ? DBNull.Value
                    : model.username
            },

            new OracleParameter("P_REQ_BLOB", OracleDbType.Blob, ParameterDirection.Input)
            {
                Value = model.req_blob == null || model.req_blob.Length == 0
                    ? DBNull.Value
                    : model.req_blob
            },

            new OracleParameter("P_RES_BLOB", OracleDbType.Blob, ParameterDirection.Input)
            {
                Value = model.res_blob == null || model.res_blob.Length == 0
                    ? DBNull.Value
                    : model.res_blob
            },

            new OracleParameter("P_REQ_TIME", OracleDbType.Date, ParameterDirection.Input)
            {
                Value = model.req_time == default
                    ? DBNull.Value
                    : model.req_time
            },

            new OracleParameter("P_RES_TIME", OracleDbType.Date, ParameterDirection.Input)
            {
                Value = model.res_time == default
                    ? DBNull.Value
                    : model.res_time
            },

            new OracleParameter("P_IS_SUCCESS", OracleDbType.Int32, ParameterDirection.Input)
            {
                Value = model.is_success
            },

            new OracleParameter("P_MESSAGE", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = string.IsNullOrWhiteSpace(model.message)
                    ? DBNull.Value
                    : model.message
            },

            new OracleParameter("P_ERROR_CODE", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = string.IsNullOrWhiteSpace(model.error_code)
                    ? DBNull.Value
                    : model.error_code
            },

            new OracleParameter("P_ERROR_SOURCE", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = string.IsNullOrWhiteSpace(model.error_source)
                    ? DBNull.Value
                    : model.error_source
            },

            new OracleParameter("P_METHOD_NAME", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = string.IsNullOrWhiteSpace(model.method_name)
                    ? DBNull.Value
                    : model.method_name
            },

            new OracleParameter("P_REMARKS", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = string.IsNullOrWhiteSpace(model.remarks)
                    ? DBNull.Value
                    : model.remarks
            },

            new OracleParameter("P_SERVER_NAME", OracleDbType.Varchar2, ParameterDirection.Input)
            {
                Value = string.IsNullOrWhiteSpace(model.server_name)
                    ? DBNull.Value
                    : model.server_name
            }
        };

                var result = await _oracleDataManagerV2.CallInsertProcedure(
                    "BIODB.BIA_TO_DPE_LOG_INS",
                    parameters.ToArray()
                );
            }
            catch (Exception ex)
            {
                string text = JsonConvert.SerializeObject(new
                {
                    request_time = DateTime.Now,
                    method_name = "BIAtoDPELog",
                    procedure_name = "BIODB.BIA_TO_DPE_LOG_INS",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
        }

        public async Task<List<DPECancelReasonModel>> GetDPECancelReasons()
        {
            try
            {
                Log.ForContext("LogTag", "ApiRequest")
                    .Information("GetDPECancelReasons DB Request: Procedure={Procedure}",
                        "BIODB.GET_DPE_CANCEL_REASON");

                using (DataTable dt = await _oracleDataManagerV2.SelectProcedureV2(
                    "BIODB.GET_DPE_CANCEL_REASON"
                ))
                {
                    var list = new List<DPECancelReasonModel>();

                    if (dt != null)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            list.Add(new DPECancelReasonModel
                            {
                                id = Convert.ToDecimal(row["ID"]),
                                reason = row["REASON"]?.ToString() ?? string.Empty
                            });
                        }
                    }

                    Log.ForContext("LogTag", "ApiRequest")
                        .Information("GetDPECancelReasons DB Response: Count={Count}", list.Count);

                    return list;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetDPECancelReasons DB Exception");
                throw;
            }
        }

        public async Task<DataTable> GetDEPPageMapping(string orderNumber, string initiatorChannel, string orderType, string subscriptionType, string simkitType, string nwAssessStatus, string paymentMethod)
        {
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("P_ORDER_NUMBER", OracleDbType.Varchar2)
            {
                Value = orderNumber
            },

            new OracleParameter("P_INITIATOR_CHANNEL", OracleDbType.Varchar2)
            {
                Value = initiatorChannel
            },

            new OracleParameter("P_ORDER_TYPE", OracleDbType.Varchar2)
            {
                Value = orderType
            },

            new OracleParameter("P_SUBSCRIPTION_TYPE", OracleDbType.Varchar2)
            {
                Value = subscriptionType
            },

            new OracleParameter("P_SIMKIT_TYPE", OracleDbType.Varchar2)
            {
                Value = simkitType
            },

            new OracleParameter("P_NW_ASSESS_STATUS", OracleDbType.Varchar2)
            {
                Value = nwAssessStatus
            },

            new OracleParameter("P_PAYMENT_METHOD", OracleDbType.Varchar2)
            {
                Value = paymentMethod
            }
                };

                return await _oracleDataManagerV2.SelectProcedureV2(
                    "BIODB.GET_DEP_PAGE_MAPPING",
                    parameters
                );
            }
            catch
            {
                throw;
            }
        }

        public async Task<DataTable> GetDPELeadDetailsDbFallbackData(string orderNumber)
        {
            try
            {
                var parameters = new OracleParameter[]
                {
            new OracleParameter("P_ORDER_NUMBER", OracleDbType.Varchar2)
            {
                Value = orderNumber
            }
                };

                return await _oracleDataManagerV2.SelectProcedureV2(
                    "BIODB.GET_DPE_LEAD_DETAILS_DB_FALLBACK",
                    parameters
                );
            }
            catch
            {
                throw;
            }
        }

        public async Task<HomeWifiCommonResponseModel> UpsertDEPOrder(HomeWifiDEPOrderRequestModel model, string operationType = "GENERAL")
        {
            if (model == null)
            {
                return new HomeWifiCommonResponseModel { isError = true, message = "Request model is null", data = null };
            }

            try
            {
                string? devicesJson =
                    model.devices != null
                        ? JsonConvert.SerializeObject(model.devices)
                        : null;

                Log.ForContext("LogTag", "DBRequest")
                    .Information("TBLDEPREQUEST_UPSERT_ORDER Request: {@Request}", new
                    {
                        operationType,

                        model.order_number,
                        model.retailer_code,
                        model.is_canceled,
                        model.cancelation_reason,
                        model.order_status,
                        model.is_imei_updated,

                        devices = model.devices,

                        model.old_identifier,
                        model.new_identifier,
                        model.imei_device_name,

                        model.ordered_msisdn,
                        model.offer_name,
                        model.offer_code,

                        model.mobile,
                        model.alternate_mobile,
                        model.customer_name,
                        model.email,

                        model.payment_type,
                        model.total_amount,
                        model.payment_status,
                        model.is_payment_method_changed,

                        model.nw_assess_id,
                        model.nw_assess_status,

                        model.order_type,
                        model.initiator_channel,
                        model.subscription_type,
                        model.simkit_type
                    });

                OracleParameter[] parameters =
                {
            new OracleParameter("P_ORDER_NUMBER", ToDbValue(model.order_number)),

            new OracleParameter("P_MOBILE", ToDbValue(model.mobile)),
            new OracleParameter("P_ALTERNATE_MOBILE", ToDbValue(model.alternate_mobile)),
            new OracleParameter("P_CUSTOMER_NAME", ToDbValue(model.customer_name)),
            new OracleParameter("P_EMAIL", ToDbValue(model.email)),

            new OracleParameter("P_OFFER_NAME", ToDbValue(model.offer_name)),
            new OracleParameter("P_OFFER_CODE", ToDbValue(model.offer_code)),

            new OracleParameter("P_DELIVERY_ADDRESS", ToDbValue(model.delivery_address)),
            new OracleParameter("P_DISTRICT", ToDbValue(model.district)),
            new OracleParameter("P_AREA", ToDbValue(model.area)),

            new OracleParameter("P_PAYMENT_TYPE", ToDbValue(model.payment_type)),

            new OracleParameter("P_TOTAL_AMOUNT", OracleDbType.Decimal)
            {
                Value = model.total_amount.HasValue
                    ? model.total_amount.Value
                    : DBNull.Value
            },

            new OracleParameter("P_PAYMENT_STATUS", ToDbValue(model.payment_status)),

            new OracleParameter("P_IS_PAYMENT_METHOD_CHANGED", OracleDbType.Int32)
            {
                Value = model.is_payment_method_changed.HasValue
                    ? model.is_payment_method_changed.Value
                    : DBNull.Value
            },

            new OracleParameter("P_ORDER_DATE", OracleDbType.Date)
            {
                Value = ToOracleDate(model.order_date)
            },

            new OracleParameter("P_ORDER_ASSIGNED_AT", OracleDbType.Date)
            {
                Value = ToOracleDate(model.order_assigned_at)
            },

            new OracleParameter("P_NW_ASSESS_ID", ToDbValue(model.nw_assess_id)),

            new OracleParameter("P_NW_ASSESS_STATUS", OracleDbType.Varchar2)
            {
                Value = ToDbValue(model.nw_assess_status)
            },

            new OracleParameter("P_APPOINTMENT_DATE", OracleDbType.Date)
            {
                Value = ToOracleDate(model.appointment_date)
            },

            new OracleParameter("P_ORDER_TYPE", ToDbValue(model.order_type)),
            new OracleParameter("P_ORDER_STATUS", ToDbValue(model.order_status)),

            new OracleParameter("P_INITIATOR_CHANNEL", ToDbValue(model.initiator_channel)),
            new OracleParameter("P_SIMKIT_TYPE", ToDbValue(model.simkit_type)),
            new OracleParameter("P_SUBSCRIPTION_TYPE", ToDbValue(model.subscription_type)),

            new OracleParameter("P_IS_ACTIVATION_DONE", OracleDbType.Int32)
            {
                Value = model.is_activation_done.HasValue
                    ? model.is_activation_done.Value
                    : DBNull.Value
            },

            new OracleParameter("P_IS_IMEI_UPDATED", OracleDbType.Int32)
            {
                Value = model.is_imei_updated.HasValue
                    ? model.is_imei_updated.Value
                    : DBNull.Value
            },

            new OracleParameter("P_IS_PAYSLIP_UPLOADED", OracleDbType.Int32)
            {
                Value = model.is_payslip_uploaded.HasValue
                    ? model.is_payslip_uploaded.Value
                    : DBNull.Value
            },

            new OracleParameter("P_IS_CANCELED", OracleDbType.Int32)
            {
                Value = model.is_canceled.HasValue
                    ? model.is_canceled.Value
                    : DBNull.Value
            },

            new OracleParameter("P_REMARKS", ToDbValue(model.remarks)),
            new OracleParameter("P_CANCELATION_REASON", ToDbValue(model.cancelation_reason)),
            new OracleParameter("P_RETAILER_CODE", ToDbValue(model.retailer_code)),

            new OracleParameter("P_ORDERED_MSISDN", ToDbValue(model.ordered_msisdn)),

            new OracleParameter("P_DEVICES_JSON", OracleDbType.Clob)
            {
                Value = string.IsNullOrWhiteSpace(devicesJson)
                    ? DBNull.Value
                    : devicesJson
            },

            new OracleParameter("P_OLD_IDENTIFIER", ToDbValue(model.old_identifier)),
            new OracleParameter("P_NEW_IDENTIFIER", ToDbValue(model.new_identifier)),
            new OracleParameter("P_IMEI_DEVICE_NAME", ToDbValue(model.imei_device_name)),

            new OracleParameter("P_OPERATION_TYPE", ToDbValue(operationType))
        };

                var dbResponse =
                    await _oracleDataManagerV2.ExecuteProcedureWithOutput(
                        "TBLDEPREQUEST_UPSERT_ORDER",
                        parameters
                    );

                var response = new HomeWifiCommonResponseModel
                {
                    isError = dbResponse.result == 0,
                    message = dbResponse.message,
                    data = null
                };

                Log.ForContext("LogTag", "DBRequest")
                    .Information("TBLDEPREQUEST_UPSERT_ORDER Response: {@Response}", response);

                return response;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "UpsertDEPOrder Exception");

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = ex.Message,
                    data = null
                };
            }
        }

        private static object ToDbValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? DBNull.Value
                : value;
        }

        private static object ToOracleDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DBNull.Value;
            }

            if (DateTime.TryParse(value, out DateTime parsedDate))
            {
                return parsedDate;
            }

            return DBNull.Value;
        }

        public async Task<DataTable> GetDEPOrderStatus(string retailerCode)
        {
            try
            {
                Log.ForContext("LogTag", "DBRequest")
                    .Information(
                        "GET_DEP_ORDER_STATUS_BY_RETAILER Request: {@Request}",
                        new
                        {
                            retailer_code = retailerCode
                        });

                OracleParameter[] parameters =
                {
            new OracleParameter("P_RETAILER_CODE", OracleDbType.Varchar2)
            {
                Value = string.IsNullOrWhiteSpace(retailerCode)
                    ? DBNull.Value
                    : retailerCode.Trim()
            }
        };

                DataTable? dt = null;
                try
                {
                    dt =
                        await _oracleDataManagerV2.SelectProcedureV2(
                            "GET_DEP_ORDER_STATUS_BY_RETAILER",
                            parameters
                        );

                    Log.ForContext("LogTag", "DBRequest")
                        .Information(
                            "GET_DEP_ORDER_STATUS_BY_RETAILER Response Count: {Count}",
                            dt?.Rows.Count ?? 0);

                    return dt;
                }
                catch
                {
                    dt?.Dispose();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetDEPOrderStatus Exception");
                throw;
            }
        }

        public async Task<DataTable> GetBIRequestByOrder(string orderNumber, string orderedMsisdn)
        {
            try
            {
                Log.ForContext("LogTag", "DBRequest")
                    .Information("GET_BI_REQUEST_BY_ORDER Request: {@Request}", new
                    {
                        order_number = orderNumber,
                        ordered_msisdn = orderedMsisdn
                    });

                var parameters = new OracleParameter[]
                {
            new OracleParameter("P_ORDER_NUMBER", OracleDbType.Varchar2)
            {
                Value = string.IsNullOrWhiteSpace(orderNumber)
                    ? (object)DBNull.Value
                    : orderNumber
            },

            new OracleParameter("P_ORDERED_MSISDN", OracleDbType.Varchar2)
            {
                Value = string.IsNullOrWhiteSpace(orderedMsisdn)
                    ? (object)DBNull.Value
                    : orderedMsisdn
            }
                };

                DataTable? dt = null;
                try
                {
                    dt = await _oracleDataManagerV2.SelectProcedureV2(
                        "GET_BI_REQUEST_BY_ORDER",
                        parameters
                    );

                    Log.ForContext("LogTag", "DBRequest")
                        .Information("GET_BI_REQUEST_BY_ORDER Response Count: {Count}",
                            dt?.Rows.Count ?? 0);

                    return dt;
                }
                catch
                {
                    dt?.Dispose();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetBIRequestByOrder Exception");
                throw;
            }
        }

        public async Task<DataTable> GetDEPRequiredPages(string orderNumber, string initiatorChannel, string orderType, string subscriptionType, string simkitType, string paymentType)
        {
            OracleParameter[] parameters =
            {
        new OracleParameter("P_ORDER_NUMBER", OracleDbType.Varchar2) { Value = orderNumber },
        new OracleParameter("P_INITIATOR_CHANNEL", OracleDbType.Varchar2) { Value = initiatorChannel },
        new OracleParameter("P_ORDER_TYPE", OracleDbType.Varchar2) { Value = orderType },
        new OracleParameter("P_SUBSCRIPTION_TYPE", OracleDbType.Varchar2) { Value = subscriptionType },
        new OracleParameter("P_SIMKIT_TYPE", OracleDbType.Varchar2) { Value = simkitType },
        new OracleParameter("P_PAYMENT_TYPE", OracleDbType.Varchar2) { Value = paymentType }
    };

            return await _oracleDataManagerV2.SelectProcedureV2(
                "GET_REQUIRED_PAGES",
                parameters
            );
        }

        private DateTime? ToDateTime(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var formats = new[]
            {
        "yyyy-MM-dd",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "dd-MM-yyyy",
        "MM/dd/yyyy"
    };

            if (DateTime.TryParseExact(value, formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
            {
                return dt;
            }

            if (DateTime.TryParse(value, out dt))
                return dt;

            return null;
        }

        private object ToNetworkAssessStatusDbValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DBNull.Value;
            }

            string normalizedValue = value.Trim();

            if (
                normalizedValue.Equals("pass", StringComparison.OrdinalIgnoreCase) ||
                normalizedValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                normalizedValue.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                normalizedValue.Equals("success", StringComparison.OrdinalIgnoreCase)
            )
            {
                return 1;
            }

            return 0;
        }

        public async Task<DpeSessionTokenModel?> GetDPEValidSessionToken()
        {
            try
            {
                if (_oracleDataManagerV2 == null)
                {
                    throw new InvalidOperationException("_oracleDataManagerV2 is not initialized.");
                }

                DataTable dt =
                    await _oracleDataManagerV2.SelectProcedureV2(
                        "GET_DPE_VALID_SESSION_TOKEN",
                        Array.Empty<OracleParameter>()
                    );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return null;
                }

                DataRow row = dt.Rows[0];

                return new DpeSessionTokenModel
                {
                    access_token = row["ACCESS_TOKEN"]?.ToString() ?? string.Empty,
                    token_type = row["TOKEN_TYPE"]?.ToString() ?? "Bearer",
                    expires_at = Convert.ToDateTime(row["EXPIRES_AT"])
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetDPEValidSessionToken Exception");
                throw;
            }
        }

        public async Task<HomeWifiCommonResponseModel> UpsertDPELoginToken(string accessToken, string tokenType, DateTime expiresAt)
        {
            try
            {
                OracleParameter[] parameters =
                {
            new OracleParameter("P_ACCESS_TOKEN", OracleDbType.Clob)
            {
                Value = string.IsNullOrWhiteSpace(accessToken)
                    ? DBNull.Value
                    : accessToken
            },

            new OracleParameter("P_TOKEN_TYPE", ToDbValue(tokenType)),

            new OracleParameter("P_EXPIRES_AT", OracleDbType.Date)
            {
                Value = expiresAt
            }
        };

                var dbResponse =
                    await _oracleDataManagerV2.ExecuteProcedureWithOutput(
                        "UPSERT_DPE_SESSION_TOKEN",
                        parameters
                    );

                return new HomeWifiCommonResponseModel
                {
                    isError = dbResponse.result == 0,
                    message = dbResponse.message,
                    data = null
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "UpsertDPELoginToken Exception");

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = ex.Message,
                    data = null
                };
            }
        }

        public async Task<string?> GetDPEOrderType(string orderNumber)
        {
            try
            {
                Log.ForContext("LogTag", "DBRequest")
                    .Information(
                        "GET_DPE_ORDER_TYPE_BY_ORDER_NUMBER Request: {@Request}",
                        new
                        {
                            order_number = orderNumber
                        });

                OracleParameter[] parameters =
                {
            new OracleParameter("P_ORDER_NUMBER", OracleDbType.Varchar2)
            {
                Value = string.IsNullOrWhiteSpace(orderNumber)
                    ? DBNull.Value
                    : orderNumber.Trim()
            }
        };

                using (DataTable dt =
                    await _oracleDataManagerV2.SelectProcedureV2(
                        "SP_GET_DPE_ORDER_TYPE",
                        parameters
                    ))
                {
                    Log.ForContext("LogTag", "DBRequest")
                        .Information(
                            "GET_DPE_ORDER_TYPE_BY_ORDER_NUMBER Response Count: {Count}",
                            dt?.Rows.Count ?? 0);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        string? orderType = dt.Rows[0]["ORDER_TYPE"]?.ToString();

                        return string.IsNullOrWhiteSpace(orderType)
                            ? null
                            : orderType.Trim();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetDPEOrderType Exception");
                throw;
            }
        }

        public async Task<DataTable> GetDPENwInfoByOrderNumber(string orderNumber)
        {
            try
            {
                Log.ForContext("LogTag", "DBRequest")
                    .Information(
                        "GET_DPE_NW_INFO_BY_ORDER_NUMBER Request: {@Request}",
                        new
                        {
                            order_number = orderNumber
                        });

                OracleParameter[] parameters =
                {
            new OracleParameter("P_ORDER_NUMBER", OracleDbType.Varchar2)
            {
                Value = string.IsNullOrWhiteSpace(orderNumber)
                    ? DBNull.Value
                    : orderNumber.Trim()
            }
        };

                DataTable? dt = null;
                try
                {
                    dt =
                        await _oracleDataManagerV2.SelectProcedureV2(
                            "GET_DPE_NW_INFO_BY_ORDER_NUMBER",
                            parameters
                        );

                    Log.ForContext("LogTag", "DBRequest")
                        .Information(
                            "GET_DPE_NW_INFO_BY_ORDER_NUMBER Response Count: {Count}",
                            dt?.Rows.Count ?? 0);

                    return dt ?? new DataTable();
                }
                catch
                {
                    dt?.Dispose();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetDPENwInfoByOrderNumber Exception");
                throw;
            }
        }

        public async Task<int> GetIsPaymentMethodChanged(string orderNumber)
        {
            try
            {
                Log.ForContext("LogTag", "DBRequest")
                    .Information(
                        "GET_IS_PAYMENT_METHOD_CHANGED_BY_ORDER_NUMBER Request: {@Request}",
                        new
                        {
                            order_number = orderNumber
                        });

                OracleParameter[] parameters =
                {
            new OracleParameter("P_ORDER_NUMBER", OracleDbType.Varchar2)
            {
                Value = string.IsNullOrWhiteSpace(orderNumber)
                    ? DBNull.Value
                    : orderNumber.Trim()
            }
        };

                using (DataTable dt =
                    await _oracleDataManagerV2.SelectProcedureV2(
                        "SP_GET_IS_PAYMENT_METHOD_CHANGED",
                        parameters
                    ))
                {
                    Log.ForContext("LogTag", "DBRequest")
                        .Information(
                            "GET_IS_PAYMENT_METHOD_CHANGED_BY_ORDER_NUMBER Response Count: {Count}",
                            dt?.Rows.Count ?? 0);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        string? flag = dt.Rows[0]["IS_PAYMENT_METHOD_CHANGED"]?.ToString();

                        return string.IsNullOrWhiteSpace(flag)
                            ? 0
                            : Convert.ToInt32(flag);
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetIsPaymentMethodChanged Exception");
                throw;
            }
        }
        #endregion

        public async Task<string> SaveReferOrderinBIODB(HomeWifiReferOrderRequest model)
        {
            string result = string.Empty;
            try
            {
                List<OracleParameter> parameters = new List<OracleParameter>
                {
                    new OracleParameter("P_CUSTOMER_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.customer_name },
                    new OracleParameter("P_EMAIL", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.email },
                    new OracleParameter("P_MOBILE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.mobile },
                    new OracleParameter("P_ALTERNATE_MOBILE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.alternate_mobile },
                    new OracleParameter("P_NID_NUMBER", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.nid_number },
                    new OracleParameter("P_NATIONALITY", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.nationality },
                    new OracleParameter("P_DISTRICT_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.district_code },
                    new OracleParameter("P_AREA_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.area_code },
                    new OracleParameter("P_DELIVERY_ADDRESS", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.delivery_address },
                    new OracleParameter("P_APPOINTMENT_DATE", OracleDbType.Date, ParameterDirection.Input) { Value = model.appointment_date },
                    new OracleParameter("P_PLAN_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.plan_code },
                    new OracleParameter("P_PLAN_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.plan_name },
                    new OracleParameter("P_DEVICE_CODE", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.device_code },
                    new OracleParameter("P_DEVICE_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.device_name },
                    new OracleParameter("P_REMARKS", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.remarks },
                    new OracleParameter("P_RETAILER_ID", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.retailer_id },
                    new OracleParameter("P_CHANNEL_NAME", OracleDbType.Varchar2, ParameterDirection.Input) { Value = model.channel_name }
                };

                result = await _oracleDataManagerV2.CallInsertProcedureForRefer("BIA_SAVEREFERORDER", parameters.ToArray());

                return result;
            }
            catch (Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "SaveReferOrderinBIODB",
                    procedure_name = "BIA_SAVEREFERORDER",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });

                throw new Exception("OuterDetails: " + text, ex);
            }
            //return result;
        }

        #region Helper method
        private static readonly System.Text.Json.JsonSerializerOptions BlobJsonOptions =new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

        private static object DbValue(object? value)
        {
            return value ?? DBNull.Value;
        }

        private static byte[] ToBlobBytes(object? value)
        {
            if (value == null)
                return Encoding.UTF8.GetBytes("{}");

            string json = System.Text.Json.JsonSerializer.Serialize(
                value,
                value.GetType(),
                BlobJsonOptions);

            JsonNode? node = JsonNode.Parse(json);

            MaskPasswordOnly(node);

            string finalJson = node?.ToJsonString(BlobJsonOptions) ?? "{}";

            return Encoding.UTF8.GetBytes(finalJson);
        }

        private static void MaskPasswordOnly(JsonNode? node)
        {
            if (node == null)
                return;

            if (node is JsonObject obj)
            {
                foreach (var property in obj.ToList())
                {
                    if (IsPasswordField(property.Key))
                    {
                        obj[property.Key] = "***MASKED***";
                    }
                    else
                    {
                        MaskPasswordOnly(property.Value);
                    }
                }
            }
            else if (node is JsonArray array)
            {
                foreach (var item in array)
                {
                    MaskPasswordOnly(item);
                }
            }
        }

        private static bool IsPasswordField(string key)
        {
            return key.Equals("password", StringComparison.OrdinalIgnoreCase);
        }



        #endregion
    }

