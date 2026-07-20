using BIA.DAL.Repositories;
using BIA.Entity.DB_Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BIA.BLL.BLLServices
{
    public class BllHandleException
    {
        private readonly DALBiometricRepo _dataManager;

        public BllHandleException(DALBiometricRepo dataManager)
        {
            _dataManager = dataManager;
        }
        public async Task<ErrorDescription> ManageException(Exception exError, int code, string errorSource)
        {
            string errorMessage = string.Empty;
            ErrorDescription errorDescription = new ErrorDescription();
            if (exError != null)
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
            if (dt.Rows.Count > 0)
                return ExceptionMapping(dt.Rows[0]);
            else return errorDescription;
        }
        internal ErrorDescription ExceptionMapping(DataRow row)
        {
            ErrorDescription error = new ErrorDescription();
            error.error_id = Convert.ToInt64(row["ERROR_ID"] == DBNull.Value ? 0 : row["ERROR_ID"]);
            error.error_code = row["ERROR_CODE"] == DBNull.Value ? "" : row["ERROR_CODE"].ToString()??"";
            error.error_description = row["ERROR_DESCRIPTION"] == DBNull.Value ? "" : row["ERROR_DESCRIPTION"].ToString()??"";
            error.error_custom_msg = row["ERROR_CUSTOM_MSG"] == DBNull.Value ? "" : row["ERROR_CUSTOM_MSG"].ToString()??"";
            error.error_source = row["ERROR_SOURCE"] == DBNull.Value ? "" : row["ERROR_SOURCE"].ToString()??"";
            return error;
        }
    }
}
