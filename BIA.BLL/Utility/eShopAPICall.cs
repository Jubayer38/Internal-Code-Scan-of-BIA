using BIA.BLL.BLLServices;
using BIA.Entity.Collections;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.Interfaces;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BIA.BLL.Utility
{
    public class eShopAPICall
    {
        private readonly BLLLog _bLLLog;
        private readonly ApiRequest _apirequest;
        public eShopAPICall(BLLLog bLLLog, ApiRequest apirequest)
        {
            _bLLLog = bLLLog;
            _apirequest = apirequest;
        }
        public async Task<eShopOrderResponseModel> OrderValidation(eShopOrderValidationReqModel model)
        {
            eShopOrderResponseModel responseModel = new eShopOrderResponseModel();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BL_Json _blJson = new BL_Json();
            try
            {
                var Baseurl = SettingsValues.GeteShopBaseUrl();
                var methodUrl = "api/v1/bio/order-activation";
                var authorizationHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(SettingsValues.GeteShopCredential()));

                if (model.msisdn.Substring(0, 2) == "01")
                {
                    model.msisdn = "88" + model.msisdn;
                }

                var requestData = new
                {
                    order_id = model.orderId,
                    msisdn = model.msisdn,
                };

                var url = Baseurl + methodUrl;

                responseModel = await _apirequest.HttpPostRequesteSHOP(requestData, url, authorizationHeader, "OrderValidation");

                log.res_blob = _blJson.GetGenericJsonData(responseModel);
            }
            catch (HttpRequestException ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "HttpGetRequest",
                    procedure_name = "",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                throw new Exception("OuterDetails: " + text, ex);
            }
            catch(Exception ex)
            {
                string? text = Convert.ToString(new
                {
                    request_time = DateTime.Now,
                    method_name = "HttpGetRequest",
                    procedure_name = "",
                    error_source = ex.Source,
                    error_code = ex.HResult,
                    error_description = ex.Message
                });
                throw new Exception("OuterDetails: " + text, ex);
            }
            finally
            {
                log.msisdn = _bLLLog.FormatMSISDN(model.msisdn);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = "2";
                log.user_id = model.retailer_id;
                log.method_name = "eShopOrderValidation";

                await _bLLLog.RAToDBSSLog(log);
            }
            return responseModel;
        }
    }
}
