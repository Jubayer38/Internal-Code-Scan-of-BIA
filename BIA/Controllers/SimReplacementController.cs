using BIA.BLL.BLLServices;
using BIA.BLL.Utility;
using BIA.Common;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.PopulateModel;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Entity.Utility;
using BIA.Entity.ViewModel;
using BIA.Helper;
using BIA.JWT;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Net;

namespace BIA.Controllers
{
    [Route("api/SimReplacement")]
    [ApiController]
    public class SimReplacementController : ControllerBase
    {
        private readonly BLLRAToDBSSParse _raToDBssParse;
        private readonly BLLDBSSToRAParse _dbssToRaParse;
        private readonly ApiRequest _apiReq;
        private readonly BL_Json _blJson;
        private readonly BLLCommon _bllCommon;
        private readonly BaseController _bio;
        private readonly ApiManager _apiManager;
        private readonly BLLOrder _orderManager;
        private readonly GeoFencingValidation _geo;
        private readonly BLLSIMReplacement _simReplacementManager;
        private readonly BLLLog _bllLog;


        public SimReplacementController(BLLRAToDBSSParse raToDBssParse, BLLDBSSToRAParse dbssToRaParse, ApiRequest apiReq, BL_Json blJson, BLLCommon bllCommon, BaseController bio, ApiManager apiManager, BLLOrder orderManager, GeoFencingValidation geo, BLLSIMReplacement simReplacementManager, BLLLog bllLog)
        {
            _raToDBssParse = raToDBssParse;
            _dbssToRaParse = dbssToRaParse;
            _apiReq = apiReq;
            _blJson = blJson;
            _bllCommon = bllCommon;
            _bio = bio;
            _apiManager = apiManager;
            _orderManager = orderManager;
            _geo = geo;
            _simReplacementManager = simReplacementManager;
            _bllLog = bllLog;
        }

        #region Get SIM Replacement Reasons

        /// Send Order
        /// <summary>
        /// Get SIM replacement reasons.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>message</returns>
        //[ResponseType(typeof(SIMReplacementReasonsResponse))]
        [HttpPost]
        [ValidateModel]
        [Route("GetSIMReplacementReasonsV3")]
        public async Task<IActionResult> GetSIMReplacementReasonsV3([FromBody][Bind("right_id,session_token")] RACommonRequest model)
        {
            List<SIMReplacementReasonModel> reasons = new List<SIMReplacementReasonModel>();
            SIMReplacementReasonsResponseRevamp reasonsResp = new SIMReplacementReasonsResponseRevamp();
            BIAToDBSSLog log = new BIAToDBSSLog();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();

            try
            {
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

                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                reasons = await _simReplacementManager.GetSIMReplacementReasons();
                if (reasons.Count > 0)
                {
                    reasonsResp.data = reasons;
                    reasonsResp.isError = false;
                    reasonsResp.message = MessageCollection.Success;
                }
                else
                {
                    reasonsResp.data = new List<SIMReplacementReasonModel>();
                    reasonsResp.isError = true;
                    reasonsResp.message = MessageCollection.NoDataFound;
                }
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
                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? String.Empty;

                reasonsResp.data = new List<SIMReplacementReasonModel>();
                reasonsResp.isError = true;
                reasonsResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
            }
            return Ok(reasonsResp);
        }
        #endregion

        public async Task<NidDobInfoResponse> GetNidDob(IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest)
        {
            NidDobInfoResponse nidDobInfo = new NidDobInfoResponse();
            BIAToDBSSLog log = new BIAToDBSSLog();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingOwnerCustomerUserCustomerSimCardInfo, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                JObject dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetNidDob");
                //JObject dbssResp = JObject.Parse("{\"data\":{\"attributes\":{\"activation-time\":\"2025-06-03T09:59:32+06:00\",\"allow-reactivation\":false,\"contract-id\":\"403716\",\"contract-status\":\"rollover\",\"directory-listing\":\"none\",\"first-call-date\":null,\"language\":\"bn\",\"latest-contract-termination-time\":\"2027-06-03T09:59:32+06:00\",\"loan-category-id\":\"1\",\"loan-category-name\":\"Slab-1\",\"monthly-costs\":0,\"msisdn\":\"8801953020377\",\"original-contract-confirmation-code\":\"O3M4P936N2Q5\",\"payment-type\":\"postpaid\",\"status\":\"active\",\"termination-time\":\"3000-01-01T00:00:00+06:00\"},\"id\":\"402204\",\"links\":{\"self\":\"/api/v1/subscriptions/402204\"},\"relationships\":{\"available-child-products\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/available-child-products\"}},\"available-loan-products\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/available-loan-products\"}},\"available-products\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/available-products\"}},\"available-subscription-types\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/available-subscription-types\"}},\"balances\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/balances\"}},\"barrings\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/barrings\"}},\"billing-accounts\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/billing-accounts\"},\"data\":[{\"id\":\"408417\",\"type\":\"billing-accounts\"},{\"id\":\"408420\",\"type\":\"billing-accounts\"}]},\"billing-rate-plan\":{\"data\":{\"id\":\"60\",\"type\":\"billing-rate-plans\"},\"links\":{\"related\":\"/api/v1/subscriptions/402204/billing-rate-plan\"}},\"billing-usages\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/billing-usages\"}},\"catalog-sim-cards\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/catalog-sim-cards\"}},\"combined-usage-reports\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/combined-usage-reports\"}},\"connected-products\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/connected-products\"}},\"connection-type\":{\"data\":{\"id\":\"2\",\"type\":\"connection-types\"},\"links\":{\"related\":\"/api/v1/subscriptions/402204/connection-type\"}},\"coordinator-customer\":{\"data\":null,\"links\":{\"related\":\"/api/v1/subscriptions/402204/coordinator-customer\"}},\"document-validations\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/document-validations\"}},\"gsm-service-usages\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/gsm-service-usages\"}},\"network-services\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/network-services\"}},\"owner-customer\":{\"data\":{\"id\":\"1085760171\",\"type\":\"customers\"},\"links\":{\"related\":\"/api/v1/subscriptions/402204/owner-customer\"}},\"payer-customer\":{\"data\":{\"id\":\"1085760174\",\"type\":\"customers\"},\"links\":{\"related\":\"/api/v1/subscriptions/402204/payer-customer\"}},\"porting-requests\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/porting-requests\"}},\"product-usages\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/product-usages\"}},\"products\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/products\"}},\"services\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/services\"}},\"sim-card-orders\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/sim-card-orders\"}},\"sim-cards\":{\"data\":[{\"id\":\"primary-410658\",\"type\":\"sim-cards\"}],\"links\":{\"related\":\"/api/v1/subscriptions/402204/sim-cards\"}},\"subscription-discounts\":{\"links\":{\"related\":\"/api/v1/subscriptions/402204/subscription-discounts\"}},\"subscription-type\":{\"data\":{\"id\":\"129\",\"type\":\"subscription-types\"},\"links\":{\"related\":\"/api/v1/subscriptions/402204/subscription-type\"}},\"user-customer\":{\"data\":{\"id\":\"1085760174\",\"type\":\"customers\"},\"links\":{\"related\":\"/api/v1/subscriptions/402204/user-customer\"}}},\"type\":\"subscriptions\"},\"included\":[{\"attributes\":{\"account-type\":null,\"agreement-start-date\":null,\"alt-contact-phone\":\"unknown\",\"ban\":null,\"bank-account-number\":null,\"business-uid\":null,\"category\":\"consumer\",\"contact-phone\":\"unknown\",\"coordinator-id\":null,\"date-of-birth\":\"1994-10-11\",\"email\":\"unknown@gmail.com\",\"first-name\":\"unknown\",\"frame-agreement-ended-at\":null,\"frame-agreement-started-at\":null,\"gender\":\"notitle\",\"id-document-number\":\"7306264289\",\"id-document-type\":\"smart_national_id\",\"id-expiry\":null,\"invoice-delivery-type\":\"delivery_not_needed\",\"is-company\":false,\"is-coordinator\":false,\"is-fleet-manager\":false,\"is-loyalty-manager\":false,\"language\":\"bn\",\"last-name\":\"unknown\",\"marketing-own\":true,\"marketing-third-party\":true,\"middle-name\":null,\"nationality\":\"BD\",\"occupation\":\"unknown\",\"online-id\":null,\"payment-method\":\"bank_payment\",\"segmentation-category\":\"001\",\"trade-register-id\":null,\"vat-usage-code\":\"domestic\"},\"id\":\"1085760171\",\"links\":{\"self\":\"/api/v1/customers/1085760171\"},\"relationships\":{\"addresses\":{\"links\":{\"related\":\"/api/v1/customers/1085760171/addresses\"}},\"company-people\":{\"links\":{\"related\":\"/api/v1/customers/1085760171/company-people\"}},\"contact-companies\":{\"links\":{\"related\":\"/api/v1/customers/1085760171/contact-companies\"}},\"coordinator-customer\":{\"data\":null,\"links\":{\"related\":\"/api/v1/customers/1085760171/coordinator-customer\"}},\"customer-edit-permission\":{\"data\":{\"id\":\"1085760171\",\"type\":\"customer-edit-permissions\"},\"links\":{\"related\":\"/api/v1/customers/1085760171/customer-edit-permission\"}},\"inventory\":{\"data\":{\"id\":\"1085760171\",\"type\":\"inventories\"},\"links\":{\"related\":\"/api/v1/customers/1085760171/inventory\"}},\"orders\":{\"links\":{\"related\":\"/api/v1/customers/1085760171/orders\"}}},\"type\":\"customers\"},{\"attributes\":{\"icc\":\"898803991653949847\",\"is-multi-surf\":false,\"pin-1\":\"1234\",\"pin-2\":\"7152\",\"puk-1\":\"59293335\",\"puk-2\":\"24332788\",\"sim-type\":\"USIM\",\"status\":\"primary\"},\"id\":\"primary-410658\",\"links\":{\"self\":\"/api/v1/sim-cards/primary-410658\"},\"relationships\":{\"subscription\":{\"data\":{\"id\":\"402204\",\"type\":\"subscriptions\"},\"links\":{\"related\":\"/api/v1/sim-cards/primary-410658/subscription\"}}},\"type\":\"sim-cards\"},{\"attributes\":{\"account-type\":null,\"agreement-start-date\":null,\"alt-contact-phone\":\"\",\"ban\":null,\"bank-account-number\":null,\"business-uid\":null,\"category\":\"consumer\",\"contact-phone\":\"8801953020377\",\"coordinator-id\":null,\"date-of-birth\":null,\"email\":\"\",\"first-name\":\"rizaul\",\"frame-agreement-ended-at\":null,\"frame-agreement-started-at\":null,\"gender\":\"male\",\"id-document-number\":\"\",\"id-document-type\":\"\",\"id-expiry\":null,\"invoice-delivery-type\":\"post\",\"is-company\":false,\"is-coordinator\":false,\"is-fleet-manager\":false,\"is-loyalty-manager\":false,\"language\":\"en\",\"last-name\":\"\",\"marketing-own\":true,\"marketing-third-party\":true,\"middle-name\":null,\"nationality\":\"BD\",\"occupation\":\"\",\"online-id\":null,\"payment-method\":\"bank_payment\",\"segmentation-category\":\"001\",\"trade-register-id\":null,\"vat-usage-code\":\"domestic\"},\"id\":\"1085760174\",\"links\":{\"self\":\"/api/v1/customers/1085760174\"},\"relationships\":{\"addresses\":{\"links\":{\"related\":\"/api/v1/customers/1085760174/addresses\"}},\"company-people\":{\"links\":{\"related\":\"/api/v1/customers/1085760174/company-people\"}},\"contact-companies\":{\"links\":{\"related\":\"/api/v1/customers/1085760174/contact-companies\"}},\"coordinator-customer\":{\"data\":null,\"links\":{\"related\":\"/api/v1/customers/1085760174/coordinator-customer\"}},\"customer-edit-permission\":{\"data\":{\"id\":\"1085760174\",\"type\":\"customer-edit-permissions\"},\"links\":{\"related\":\"/api/v1/customers/1085760174/customer-edit-permission\"}},\"inventory\":{\"data\":{\"id\":\"1085760174\",\"type\":\"inventories\"},\"links\":{\"related\":\"/api/v1/customers/1085760174/inventory\"}},\"orders\":{\"links\":{\"related\":\"/api/v1/customers/1085760174/orders\"}}},\"type\":\"customers\"}]}");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null || dbssResp["included"] == null)
                {
                    nidDobInfo.result = false;
                    nidDobInfo.message = MessageCollection.SIMReplNoDataFound;
                    return nidDobInfo;
                }

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.IndividualSIMReplacementMSISDNReqParsingV3(dbssResp);

                if (msisdnResp.result == false)
                {
                    nidDobInfo.result = false;
                    nidDobInfo.message = MessageCollection.SIMReplNoDataFound;
                    return nidDobInfo;
                }
                
                nidDobInfo.dest_nid = msisdnResp.doc_id_number??"";
                nidDobInfo.dest_dob = msisdnResp.dob??"";
                nidDobInfo.old_sim_type = msisdnResp.old_sim_type;
                nidDobInfo.old_sim_number = msisdnResp.old_sim_number;
                nidDobInfo.result = true;
                nidDobInfo.message = "";

                return nidDobInfo;
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

                nidDobInfo.result = false;
                nidDobInfo.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.res_blob = _blJson.GetGenericJsonData(nidDobInfo);
                return nidDobInfo;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number??"";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "GetNidDob";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        public async Task<NidDobInfoResponse> GetNidDobForCorporate(CorporateMSISDNCheckRequest msisdnCheckReqest)
        {
            NidDobInfoResponse nidDobInfo = new NidDobInfoResponse();
            BIAToDBSSLog log = new BIAToDBSSLog();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            try
            {
                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingCustomerInfo, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                JObject dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetNidDobForCorporate");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp["data"] == null || dbssResp["included"] == null)
                {
                    nidDobInfo.result = false;
                    nidDobInfo.message = MessageCollection.SIMReplNoDataFound;
                    return nidDobInfo;
                }

                log.is_success = 1;

                CorporateSIMReplacementCheckResponseWithCustomerId msisdnResp = _dbssToRaParse.CorporateSIMReplacementMSISDNReqParsing2(dbssResp);

                if (msisdnResp.result == false)
                {
                    nidDobInfo.result = false;
                    nidDobInfo.message = msisdnResp.message;
                    return nidDobInfo;
                }

                SIMReplacementMSISDNCheckResponse customerResp = await _bio.GetCoordicatorCustomerInfo(msisdnResp.customer_id, msisdnCheckReqest.poc_msisdn_number, msisdnCheckReqest.purpose_number ?? "", msisdnCheckReqest.retailer_id);

                if (customerResp.result == false)
                {
                    nidDobInfo.result = false;
                    nidDobInfo.message = customerResp.message;
                    return nidDobInfo;
                }
                nidDobInfo.dest_nid = customerResp.doc_id_number ?? "";
                nidDobInfo.dest_dob = customerResp.dob ?? "";
                nidDobInfo.old_sim_type = msisdnResp.old_sim_type;
                nidDobInfo.old_sim_number = msisdnResp.old_sim_number;
                nidDobInfo.result = true;
                nidDobInfo.message = "";
                log.res_blob = _blJson.GetGenericJsonData(nidDobInfo);
                return nidDobInfo;
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

                nidDobInfo.result = false;
                nidDobInfo.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                return nidDobInfo;
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);

                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "GetNidDob";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        #region Individual SIM Replacement MSISDN validation  
        /// <summary>
        /// This API is used for MSISDN validation for paired
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        //[Authorize]
        //[ResponseType(typeof(IndividualSIMReplacementMSISDNCheckResponse))]
        [HttpPost]
        [SIMReplacementModelValidator]
        [Route("ValidateMSISDNForIndividualSIMReplacementV3")]
        public async Task<IActionResult> ValidateMSISDNForIndividualSIMReplacementV3([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest)
        {
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            IndividualSIMReplacementMSISDNCheckResponseRevamp response = new IndividualSIMReplacementMSISDNCheckResponseRevamp();

            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingOwnerCustomerUserCustomerSimCardInfo, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                JObject dbssResp = new JObject();
                try
                {
                    dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateMSISDNForIndividualSIMReplacementV3");
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("Not Found"))
                    {
                        return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                        {
                            isError = true,
                            message = FixedValueCollection.DBSSError + "Invalid MSISDN input for SIM Replacement."
                        });
                    }
                    else
                    {
                        throw;
                    }
                }

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null || dbssResp["included"] == null)
                {
                    return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = MessageCollection.SIMReplNoDataFound,
                    });
                }

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.IndividualSIMReplacementMSISDNReqParsingV3(dbssResp);

                if (msisdnResp.result == false)
                {
                    return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = FixedValueCollection.MSISDNError + msisdnResp.message
                    });
                }

                var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.SIMReplacement, null, null, msisdnResp.old_sim_type);

                if (simResp.result == false)
                {
                    return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = simResp.message
                    });
                }

                var resp = new IndividualSIMReplacementMSISDNCheckResponse()
                {
                    dbss_subscription_id = msisdnResp.dbss_subscription_id,
                    old_sim_number = msisdnResp.old_sim_number,
                    doc_id_number = "**********",
                    dob = "**/**/****",
                    result = true,
                    message = MessageCollection.MSISDNandSIMBothValid,
                    saf_status = msisdnResp.saf_status,
                    customer_id = msisdnResp.customer_id
                };
                return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                {
                    isError = false,
                    message = resp.message,
                    data = resp
                });
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
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? String.Empty;

                return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                {
                    isError = true,
                    message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                });
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);

                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidateMSISDNForIndividualSIMReplacementV3";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        /// <summary>
        /// This API is used for MSISDN validation for paired
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        //[Authorize]
        //[ResponseType(typeof(IndividualSIMReplacementMSISDNCheckResponse))]
        [HttpPost]
        [SIMReplacementModelValidator]
        [Route("ValidateMSISDNForIndividualReplacement_ESIMV2")]
        public async Task<IActionResult> ValidateMSISDNForIndividualESIMReplacementV2([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest)
        {
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }

                apiUrl = String.Format(GetAPICollection.GetSubscriptionByMSISDNIncludingOwnerCustomerUserCustomerSimCardInfo, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                JObject dbssResp = new JObject();
                try
                {
                    dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidateMSISDNForIndividualReplacement_ESIMV2");
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("Not Found"))
                    {
                        throw new Exception("Invalid MSISDN input for E-SIM Replacement.");
                    }
                    else
                    {
                        throw;
                    }
                }

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp["data"] == null || dbssResp["included"] == null)
                {
                    return Ok(new IndividualSIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = MessageCollection.SIMReplNoDataFound,
                    });
                }

                log.is_success = 1;

                var msisdnResp = _dbssToRaParse.IndividualSIMReplacementMSISDNReqParsingV3(dbssResp);

                if (msisdnResp.result == false)
                {
                    return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = FixedValueCollection.MSISDNError + msisdnResp.message
                    });
                }

                var simResp = await _bio.CheckSIMNumber4(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.SIMReplacement, null, null, msisdnResp.old_sim_type);

                if (simResp.result == false)
                {
                    return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = simResp.message
                    });
                }

                var resp = new IndividualSIMReplacementMSISDNCheckResponse()
                {
                    dbss_subscription_id = msisdnResp.dbss_subscription_id,
                    old_sim_number = msisdnResp.old_sim_number,
                    doc_id_number = "**********",
                    dob = "**/**/****",
                    result = true,
                    message = MessageCollection.MSISDNandSIMBothValid,
                    saf_status = msisdnResp.saf_status,
                    customer_id = msisdnResp.customer_id
                };
                return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                {
                    isError = false,
                    message = resp.message,
                    data = resp
                });
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
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? String.Empty;

                return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                {
                    isError = true,
                    message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                });
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidateMSISDNForIndividualReplacement_ESIM";

                await _bllLog.RAToDBSSLog(log);
            }
        }

        #endregion

        #region Corporate SIM Replacement MSISDN validation by POC
        /// <summary>
        /// This API is used for MSISDN validation for B2B SIM replacement by POC.  
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        //[Authorize]
        //[ResponseType(typeof(SIMReplacementMSISDNCheckResponse))]
        [HttpPost]
        [SIMReplacementPOCModelValidator]
        [Route("ValidateMSISDNForCorporateSIMReplacementByPOCV3")]
        public async Task<IActionResult> ValidateMSISDNForCorporateSIMReplacementByPOCV3([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,poc_msisdn_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] CorporateMSISDNCheckRequest msisdnCheckReqest)
        {
            SIMReplacementMSISDNCheckResponse checkResponse = new SIMReplacementMSISDNCheckResponse();
            ValidTokenResponse security = new ValidTokenResponse();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;

                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }
                msisdnCheckReqest.inventory_id = 2;
                checkResponse = await _bio.ValidateCorporateMSISDNV1(msisdnCheckReqest, "ValidateMSISDNForCorporateSIMReplacementByPOCV1");

                if (checkResponse.result == true)
                {
                    return Ok(new SIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = checkResponse.result == true ? false : true,
                        message = checkResponse.message,
                        data = checkResponse
                    });
                }
                else
                {
                    return Ok(new SIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = checkResponse.message,
                        data = checkResponse
                    });
                }
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
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                return Ok(new SIMReplacementMSISDNCheckResponseRevamp()
                {
                    isError = true,
                    message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                });
            }
        }

        /// <summary>
        /// This API is used for MSISDN validation for B2B SIM replacement by POC.  
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        //[Authorize]
        //eSim(New Logic)
        //[ResponseType(typeof(SIMReplacementMSISDNCheckResponse))]
        [HttpPost]
        [SIMReplacementPOCModelValidator]
        [Route("ValidateMSISDNForCorporateReplacementByPOC_ESIMV2")]
        public async Task<IActionResult> ValidateMSISDNForCorporateE_SIMReplacementByPOCV2([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,poc_msisdn_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] CorporateMSISDNCheckRequest msisdnCheckReqest)
        {
            SIMReplacementMSISDNCheckResponse checkResponse = new SIMReplacementMSISDNCheckResponse();
            ValidTokenResponse security = new ValidTokenResponse();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }
                msisdnCheckReqest.inventory_id = 2;

                checkResponse = await _bio.ValidateCorporateMSISDNV2(msisdnCheckReqest, "ValidateMSISDNForCorporateReplacementByPOC_ESIM");

                return Ok(new SIMReplacementMSISDNCheckResponseRevamp()
                {
                    isError = checkResponse.result == true ? false : true,
                    message = checkResponse.message,
                    data = checkResponse
                });
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
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                return Ok(new SIMReplacementMSISDNCheckResponseRevamp()
                {
                    isError = true,
                    message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                });
            }
        }

        #endregion

        #region Corporate SIM Replacement MSISDN validation BY Auth Person 

        /// <summary>
        /// This API is used for MSISDN validation B2B by auth person. 
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        //[Authorize]
        //[ResponseType(typeof(SIMReplacementMSISDNCheckResponse))]
        [HttpPost]
        [SIMReplacementAuthModelValidator]
        [Route("ValidateMSISDNForCorporateSIMReplacementByAuthPersonV3")]
        public async Task<IActionResult> ValidateMSISDNForCorporateSIMReplacementByAuthPersonV3([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,otp,poc_msisdn_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] CorporateMSISDNCheckWithOTPRequest msisdnCheckReqest)
        {
            ValidTokenResponse security = new ValidTokenResponse();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                #region OTP validation
                OTPResponseRev otpResp = await _bio.ValidateOTPV2(new DBSSOTPValidationRequest()
                {
                    otp = msisdnCheckReqest.otp,
                    poc_msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.poc_msisdn_number),
                    auth_msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number),
                    purpose = Convert.ToInt16(EnumPurposeForDBSSOTP.SIMReplByAuth)
                }, msisdnCheckReqest.retailer_id);

                if (otpResp.data != null && otpResp.data.is_otp_valid == false)
                {
                    return Ok(new SIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = otpResp.message
                    });
                }
                #endregion
                msisdnCheckReqest.inventory_id = 2;
                var response = await _bio.ValidateCorporateMSISDNV1(msisdnCheckReqest, "ValidateMSISDNForCorporateSIMReplacementByAuthPersonV3");

                if (response.result == true)
                {
                    return Ok(new SIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = false,
                        message = response.message,
                        data = response
                    });
                }
                else
                {
                    return Ok(new SIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = response.message,
                        data = response
                    });
                }
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
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                return Ok(new SIMReplacementMSISDNCheckResponseRevamp()
                {
                    isError = true,
                    message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                });
            }
        }

        /// <summary>
        /// This API is used for MSISDN validation B2B by auth person. 
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        //[Authorize]
        //eSim(Existing Logic)
        //[ResponseType(typeof(SIMReplacementMSISDNCheckResponse))]
        [HttpPost]
        [SIMReplacementAuthModelValidator]
        [Route("ValidateMSISDNForCorporateReplacementByAuthPerson_ESIMV2")]
        public async Task<IActionResult> ValidateMSISDNForCorporateE_SIMReplacementByAuthPersonV2([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,otp,poc_msisdn_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] CorporateMSISDNCheckWithOTPRequest msisdnCheckReqest)
        {
            ValidTokenResponse security = new ValidTokenResponse();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                try
                {
                    secreteKey = SettingsValues.GetJWTSequrityKey();
                }
                catch
                { }

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                #region OTP validation
                OTPResponseRev otpResp = await _bio.ValidateOTPV2(new DBSSOTPValidationRequest()
                {
                    otp = msisdnCheckReqest.otp,
                    poc_msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.poc_msisdn_number),
                    auth_msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number),
                    purpose = Convert.ToInt16(EnumPurposeForDBSSOTP.SIMReplByAuth)
                }, msisdnCheckReqest.retailer_id);

                if (otpResp.data != null && otpResp.data.is_otp_valid == false)
                {
                    return Ok(new SIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = otpResp.message
                    });
                }
                msisdnCheckReqest.inventory_id = 2;

                #endregion
                var response = await _bio.ValidateCorporateMSISDNV2(msisdnCheckReqest, "ValidateMSISDNForCorporateReplacementByAuthPerson_ESIM");

                if (response.result == true)
                {
                    return Ok(new SIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = false,
                        message = response.message,
                        data = response
                    });
                }
                else
                {
                    return Ok(new SIMReplacementMSISDNCheckResponseRevamp()
                    {
                        isError = true,
                        message = response.message,
                        data = response
                    });
                }
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
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                return Ok(new SIMReplacementMSISDNCheckResponseRevamp()
                {
                    isError = true,
                    message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                });
            }
        }
        #endregion 

        #region Individual SimReplacement submit Order API
        /// Send Order
        /// <summary>
        /// This API is used for SimReplacement submit order.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Order request token id</returns>
        //[ResponseType(typeof(SendOrderResponse))]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [SIMReplacementOrderRequestValidator]
        [Route("IndividualSIMReplacementSubmitOrderV4")]
        public async Task<IActionResult> IndividualSIMReplacementSubmitOrderV4([FromBody][Bind("alt_msisdn,center_code,channel_name,cid,customer_id,customer_name,dbss_subscription_id,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,distributor_code,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,lac,latitude,longitude,msisdn,old_sim_number,payment_type,postal_code,purpose_number,retailer_id,right_id,road_number,saf_status,scanner_id,session_token,sim_number,sim_rep_reason_id,sim_replc_reason,thana_id,thana_name,village")] SimReplacementRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            NidDobInfoResponse dobInfoResponse = new NidDobInfoResponse();
            IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest = new IndividualSIMReplsMSISDNCheckRequest();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            string is_error_from_ongoing = string.Empty;
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();
            try
            {
                model = populateModel.SIMReplacementRequestPopulateModel(request);

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                TokenValidationService token = new TokenValidationService(secreteKey);

                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.CastRequestModel(model);

                log.req_blob = _blJson.GetGenericJsonData(requestModelBLOB);

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

                #region Geo fencing BP user
                if (geoFencEnable == 1)
                {
                    if (model.isBPUser == 1)
                    {
                        RACommonResponseRevamp responseRevamp = await _geo.GeoFencingBPUser(model);

                        if (responseRevamp != null && responseRevamp.isError == true)
                        {
                            return Ok(responseRevamp);
                        }
                    }
                }
                #endregion
                #region Get_Data_from_Validation

                msisdnCheckReqest.mobile_number = model.msisdn;
                msisdnCheckReqest.purpose_number = model.purpose_number;
                msisdnCheckReqest.retailer_id = model.retailer_id;
                msisdnCheckReqest.center_code = model.center_code;
                msisdnCheckReqest.channel_name = model.channel_name;
                msisdnCheckReqest.session_token = model.session_token;
                msisdnCheckReqest.sim_number = model.sim_number;

                dobInfoResponse = await GetNidDob(msisdnCheckReqest);

                if (dobInfoResponse.result == false)
                {
                    //orderRes.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = dobInfoResponse.message;
                    log.remarks = dobInfoResponse.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                else
                {
                    var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest()
                    {
                        center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                        distributor_code = "",
                        channel_name = msisdnCheckReqest.channel_name,
                        session_token = msisdnCheckReqest.session_token,
                        sim_number = msisdnCheckReqest.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        product_code = "",
                        inventory_id = 2,
                        msisdn = msisdnCheckReqest.mobile_number,
                        purpose_number = msisdnCheckReqest.purpose_number ?? ""
                    }, (int)EnumPurposeOfSIMCheck.SIMReplacement, null, null, dobInfoResponse.old_sim_type);

                    if (simResp.result == false)
                    {
                        return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                        {
                            isError = true,
                            message = simResp.message
                        });
                    }
                    else
                    {
                        model.dest_nid = dobInfoResponse.dest_nid;
                        model.dest_dob = dobInfoResponse.dest_dob;
                        model.old_sim_number = dobInfoResponse.old_sim_number;
                    }
                }

                #endregion 
                #region Check if submitted order is already in process or not.

                VMValidateOrder vMValidateOrder = new VMValidateOrder()
                {
                    msisdn = model.msisdn,
                    sim_number = model.sim_number,
                    purpose_number = Convert.ToInt32(model.purpose_number),
                    is_corporate = 0,
                    retailer_id = model.retailer_id,
                    dest_dob = DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat)
                };

                var orderValidationResult = await _orderManager.ValidateOrder(vMValidateOrder);
                if (orderValidationResult.result == false)
                {
                    //orderRes.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = orderValidationResult.message;
                    log.remarks = orderValidationResult.message;
                    is_error_from_ongoing = "ongoing";
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                #endregion
              
                #region Insert_Order
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;
                model.prov_id = loginProviderId;
                orderRes = await _orderManager.SubmitOrderV7(model);

                if (orderRes.isError)
                {
                    return Ok(new SendOrderResponse()
                    {
                        is_success = false,
                        message = orderRes.message
                    });
                }
                else
                {
                    try
                    {
                        model.bi_token_number = Convert.ToDouble(orderRes.data.request_id);
                        #endregion
                        #region Get IMSI
                        var imsiResp = await _bio.GetImsiBySimAsync(new GetImsiReq
                        {
                            purpose_number = model.purpose_number,
                            retailer_id = model.retailer_id,
                            sim = model.sim_number,
                            msisdn = model.msisdn
                        });

                        if (imsiResp.result == false)
                        {
                            model.status = (int)EnumRAOrderStatus.Failed;
                            // orderRes.request_id = "0";
                            orderRes.isError = true;
                            orderRes.message = imsiResp.message;
                            model.err_msg = imsiResp.message;
                            log.remarks = imsiResp.message;
                            return Ok(orderRes);
                        }
                        else
                        {
                            model.dest_imsi = imsiResp.imsi;//[Note: here IMSI is being sent as SIM number as per business requirement]
                        }
                        #endregion
                        #region bio verification

                        var pursedData = await _orderManager.SubmitOrderDataPurseV2(model);
                        BiomerticDataModel dataModel = bioverifyDataMapp(pursedData);

                        verifyResp = await _bio.BssServiceProcessV2(dataModel);

                        if (verifyResp.is_success == true)
                        {
                            model.bss_reqId = verifyResp.bss_req_id;
                            model.status = (int)EnumRAOrderStatus.BioVerificationSubmitted;
                        }
                        else
                        {
                            model.status = (int)EnumRAOrderStatus.Failed;
                            model.err_code = verifyResp.err_code;
                            model.err_msg = verifyResp.err_msg;
                            model.error_id = verifyResp.error_Id;
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        ErrorDescription errorDescription = new ErrorDescription();
                        model.status = (int)EnumRAOrderStatus.Failed;
                        errorDescription = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                        model.err_code = errorDescription.error_code;
                        model.error_id = errorDescription.error_id;
                        orderRes.message = String.IsNullOrEmpty(errorDescription.error_custom_msg) ? errorDescription.error_description : errorDescription.error_custom_msg;
                        model.err_msg = String.IsNullOrEmpty(errorDescription.error_custom_msg) ? errorDescription.error_description : errorDescription.error_custom_msg;
                        orderRes.isError = true;
                        return Ok(orderRes);
                    }
                }
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
                model.status = (int)EnumRAOrderStatus.Failed;
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;

                if (verifyResp != null)
                {
                    orderRes.data = new DataRes()
                    {
                        request_id = verifyResp.bss_req_id
                    };
                }
                else
                {
                    orderRes.data = new DataRes()
                    {
                        request_id = "0"
                    };
                }
                orderRes.isError = true;
                orderRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                model.err_msg = orderRes.message;
                log.res_blob = _blJson.GetGenericJsonData(error);
            }
            finally
            {
                log.purpose_number = model.purpose_number;
                log.msisdn = _bllLog.FormatMSISDN(model.msisdn);
                log.req_time = DateTime.Now;
                if (model.bi_token_number != null && model.bi_token_number > 1 && is_error_from_ongoing != "ongoing")
                {
                    response2 = await _orderManager.UpdateOrder(new RAOrderRequestUpdate
                    {
                        bi_token_number = model.bi_token_number,
                        msidn = model.msisdn,
                        user_name = model.retailer_id,
                        dest_imsi = model.dest_imsi,
                        status = model.status,
                        bss_reqId = model.bss_reqId,
                        error_id = model.error_id,
                        err_msg = model.err_msg,
                    });
                }
                log.res_time = DateTime.Now;
                if (orderRes.data != null)
                {
                    log.is_success = orderRes.data.request_id.Length > 1 ? 1 : 0;
                    log.bi_token_number = orderRes.data.request_id;
                }
                log.res_blob = _blJson.GetGenericJsonData(orderRes);
                log.method_name = "IndividualSIMReplacementSubmitOrderV4";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null
                                && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(orderRes);
        }

        /// Send Order
        /// <summary>
        /// This API is used for SimReplacement submit order.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Order request token id</returns>
        //[ResponseType(typeof(SendOrderResponse))]
        [HttpPost]
        [SIMReplacementOrderRequestValidator]
        [Route("IndividualSIMReplacementSubmitOrder_ESIMV2")]
        public async Task<IActionResult> IndividualSIMReplacementSubmitOrder_ESIMV2([FromBody][Bind("alt_msisdn,center_code,channel_name,cid,customer_id,customer_name,dbss_subscription_id,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,distributor_code,district_id,district_name,division_id,division_name,email,flat_number,gender,house_number,isBPUser,lac,latitude,longitude,msisdn,old_sim_number,payment_type,postal_code,purpose_number,retailer_id,right_id,road_number,saf_status,scanner_id,session_token,sim_number,sim_rep_reason_id,sim_replc_reason,thana_id,thana_name,village")] SimReplacementRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp(); 
            NidDobInfoResponse dobInfoResponse = new NidDobInfoResponse();
            IndividualSIMReplsMSISDNCheckRequest msisdnCheckReqest = new IndividualSIMReplsMSISDNCheckRequest();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            string is_error_from_ongoing = string.Empty;
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();

            try
            {
                model = populateModel.SIMReplacementRequestPopulateModel(request);
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                TokenValidationService token = new TokenValidationService(secreteKey);

                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.CastRequestModel(model);

                log.req_blob = _blJson.GetGenericJsonData(requestModelBLOB);
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

                #region Geo fencing BP user
                if (geoFencEnable == 1)
                {
                    if (model.isBPUser == 1)
                    {
                        RACommonResponseRevamp responseRevamp = await _geo.GeoFencingBPUser(model);

                        if (responseRevamp != null && responseRevamp.isError == true)
                        {
                            return Ok(responseRevamp);
                        }
                    }
                }
                #endregion

                #region Get_Data_from_Validation

                msisdnCheckReqest.mobile_number = model.msisdn;
                msisdnCheckReqest.purpose_number = model.purpose_number;
                msisdnCheckReqest.retailer_id = model.retailer_id;
                msisdnCheckReqest.center_code = model.center_code;
                msisdnCheckReqest.channel_name = model.channel_name;
                msisdnCheckReqest.session_token = model.session_token;
                msisdnCheckReqest.sim_number = model.sim_number;

                dobInfoResponse = await GetNidDob(msisdnCheckReqest);

                if (dobInfoResponse.result == false)
                {
                    // orderRes.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = dobInfoResponse.message;
                    log.remarks = dobInfoResponse.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                else
                {
                    var simResp = await _bio.CheckSIMNumber4(new SIMNumberCheckRequest()
                    {
                        center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                        distributor_code = "",
                        channel_name = msisdnCheckReqest.channel_name,
                        session_token = msisdnCheckReqest.session_token,
                        sim_number = msisdnCheckReqest.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        product_code = "",
                        inventory_id = 2,
                        msisdn = msisdnCheckReqest.mobile_number,
                        purpose_number = msisdnCheckReqest.purpose_number ?? ""
                    }, (int)EnumPurposeOfSIMCheck.SIMReplacement, null, null, dobInfoResponse.old_sim_type);

                    if (simResp.result == false)
                    {
                        return Ok(new IndividualSIMReplacementMSISDNCheckResponseRevamp()
                        {
                            isError = true,
                            message = simResp.message
                        });
                    }
                    else
                    {
                        model.dest_nid = dobInfoResponse.dest_nid;
                        model.dest_dob = dobInfoResponse.dest_dob;
                        model.old_sim_number = dobInfoResponse.old_sim_number;
                    }                    
                }

                #endregion

                #region Check if submitted order is already in process or not.
                var orderValidationResult = await _orderManager.ValidateOrder(new VMValidateOrder
                {
                    msisdn = model.msisdn,
                    sim_number = model.sim_number,
                    purpose_number = Convert.ToInt32(model.purpose_number),
                    is_corporate = 0,
                    retailer_id = model.retailer_id,
                    dest_dob = DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat)
                });
                if (orderValidationResult.result == false)
                {
                    // orderRes.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = orderValidationResult.message;
                    log.remarks = orderValidationResult.message;
                    is_error_from_ongoing = "ongoing";
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                #endregion
                #region Insert_Order
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;
                model.is_esim = 1;
                model.prov_id = loginProviderId;
                orderRes = await _orderManager.SubmitOrderV7(model);

                if (orderRes.isError)
                {
                    return Ok(new SendOrderResponseRev()
                    {
                        isError = true,
                        message = orderRes.message
                    });
                }
                model.bi_token_number = Convert.ToDouble(orderRes.data.request_id);
                #endregion
                #region Get IMSI
                var imsiResp = await _bio.GetImsiBySimAsync(new GetImsiReq
                {
                    purpose_number = model.purpose_number,
                    retailer_id = model.retailer_id,
                    sim = model.sim_number,
                    msisdn = model.msisdn
                });

                if (imsiResp.result == false)
                {
                    model.status = (int)EnumRAOrderStatus.Failed;
                    orderRes.isError = true;
                    orderRes.message = imsiResp.message;
                    model.err_msg = imsiResp.message;
                    log.remarks = imsiResp.message;
                    return Ok(orderRes);
                }
                else
                {
                    model.dest_imsi = imsiResp.imsi;//[Note: here IMSI is being sent as SIM number as per business requirement]
                }
                #endregion
                #region bio verification

                var pursedData = await _orderManager.SubmitOrderDataPurseV2(model);
                BiomerticDataModel dataModel = bioverifyDataMapp(pursedData);

                verifyResp = await _bio.BssServiceProcessV2(dataModel);

                if (verifyResp.is_success == true)
                {
                    model.bss_reqId = verifyResp.bss_req_id;
                    model.status = (int)EnumRAOrderStatus.BioVerificationSubmitted;
                }
                else
                {
                    model.status = (int)EnumRAOrderStatus.Failed;
                    model.err_code = verifyResp.err_code;
                    model.err_msg = verifyResp.err_msg;
                    model.error_id = verifyResp.error_Id;
                }
                #endregion
                //=====Order submission=====

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
                model.status = (int)EnumRAOrderStatus.Failed;
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;

                if (verifyResp != null)
                {
                    orderRes.data.request_id = verifyResp.bss_req_id;
                }
                else
                {
                    orderRes.data.request_id = "";
                }

                orderRes.isError = true;
                orderRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                model.err_msg = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.res_blob = _blJson.GetGenericJsonData(error);
            }
            finally
            {
                log.purpose_number = model.purpose_number;
                log.msisdn = _bllLog.FormatMSISDN(model.msisdn);
                log.req_time = DateTime.Now;
                if (model.bi_token_number != null && model.bi_token_number > 1 && is_error_from_ongoing != "ongoing")
                {
                    response2 = await _orderManager.UpdateOrder(new RAOrderRequestUpdate
                    {
                        bi_token_number = model.bi_token_number,
                        msidn = model.msisdn,
                        dest_imsi = model.dest_imsi,
                        status = model.status,
                        bss_reqId = model.bss_reqId,
                        error_id = model.error_id,
                        err_msg = model.err_msg,
                        user_name = model.retailer_id
                    });
                }

                log.res_time = DateTime.Now;
                if (orderRes != null)
                {
                    log.is_success = orderRes.data != null && orderRes.data.request_id.Length > 1 ? 1 : 0;
                    log.bi_token_number = orderRes.data != null ? orderRes.data.request_id : "";
                }
                log.res_blob = _blJson.GetGenericJsonData(orderRes);
                log.method_name = "IndividualSIMReplacementSubmitOrder_ESIM";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;
                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(orderRes);
        }
        #endregion

        /// Send Order
        /// <summary>
        /// This API is used for SimReplacement submit order.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Order request token id</returns>
        //[ResponseType(typeof(SendOrderResponse))]
        [HttpPost]
        [CorpSIMReplacementOrderRequestValidator]
        [Route("CorporateSIMReplacementSubmitOrderV4")]
        public async Task<IActionResult> CorporateSIMReplacementSubmitOrderV4([FromBody][Bind("bi_token_number,channel_name,cid,dbss_subscription_id,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,isBPUser,lac,latitude,longitude,msisdn,old_sim_number,payment_type,poc_msisdn_number,purpose_number,retailer_id,right_id,scanner_id,session_token,sim_number,sim_rep_reason_id,sim_replacement_type,src_dob,src_nid")] CorpSimReplacementRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog(); 
            BioVerifyResp verifyResp = new BioVerifyResp();
            NidDobInfoResponse nidDobInfo = new NidDobInfoResponse();
            CorporateMSISDNCheckRequest checkRequest = new CorporateMSISDNCheckRequest();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            string is_error_from_ongoing = string.Empty;
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();
            try
            {
                model = populateModel.CorpSIMReplacementRequestPopulateModel(request);

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                TokenValidationService token = new TokenValidationService(secreteKey);

                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.CastRequestModel(model);

                log.req_blob = _blJson.GetGenericJsonData(requestModelBLOB);

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

                #region Geo fencing BP user
                if (geoFencEnable == 1)
                {
                    if (model.isBPUser == 1)
                    {
                        RACommonResponseRevamp responseRevamp = await _geo.GeoFencingBPUser(model);

                        if (responseRevamp != null && responseRevamp.isError == true)
                        {
                            return Ok(responseRevamp);
                        }
                    }
                }
                #endregion

                #region Get_NID_DOB
                checkRequest.mobile_number = model.msisdn;
                checkRequest.poc_msisdn_number = model.poc_msisdn_number ?? "";
                checkRequest.purpose_number = model.purpose_number;
                checkRequest.retailer_id = model.retailer_id;

                nidDobInfo = await GetNidDobForCorporate(checkRequest);

                if (nidDobInfo.result == false)
                {
                    //orderRes.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = nidDobInfo.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                else
                {
                    if (model.sim_replacement_type != null && model.sim_replacement_type == (int)EnumSIMReplacementType.ByAuthPerson)
                    {
                        model.src_nid = nidDobInfo.dest_nid;
                        model.src_dob = nidDobInfo.dest_dob;
                        model.old_sim_number = nidDobInfo.old_sim_number;
                    }
                    else
                    {
                        model.dest_nid = nidDobInfo.dest_nid;
                        model.dest_dob = nidDobInfo.dest_dob;
                        model.old_sim_number = nidDobInfo.old_sim_number;
                    }
                }
                #endregion                

                #region Check if submitted order is already in process or not.
                var orderValidationResult = await _orderManager.ValidateOrder(new VMValidateOrder
                {
                    msisdn = model.msisdn,
                    sim_number = model.sim_number,
                    purpose_number = Convert.ToInt32(model.purpose_number),
                    is_corporate = 1,
                    retailer_id = model.retailer_id,
                    dest_dob = DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat)
                });
                if (orderValidationResult.result == false)
                {
                    //orderRes.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = orderValidationResult.message;
                    is_error_from_ongoing = "ongoing";
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                #endregion
                #region Insert_Order
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;
                model.prov_id = loginProviderId;
                orderRes = await _orderManager.SubmitOrderV7(model);

                if (orderRes.isError)
                {
                    return Ok(new SendOrderResponseRev()
                    {
                        isError = true,
                        message = orderRes.message
                    });
                }
                else
                {
                    try
                    {
                        model.bi_token_number = Convert.ToDouble(orderRes.data.request_id);
                        #endregion
                        #region Get IMSI
                        var imsiResp = await _bio.GetImsiBySimAsync(new GetImsiReq
                        {
                            purpose_number = model.purpose_number,
                            retailer_id = model.retailer_id,
                            sim = model.sim_number,
                            msisdn = model.msisdn
                        });

                        if (imsiResp.result == false)
                        {
                            model.status = (int)EnumRAOrderStatus.Failed;
                            // orderRes.request_id = "0";
                            orderRes.isError = false;
                            orderRes.message = imsiResp.message;
                            model.err_msg = imsiResp.message;
                            return Ok(orderRes);
                        }
                        else
                        {
                            model.dest_imsi = imsiResp.imsi;//[Note: here IMSI is being sent as SIM number as per business requirement]
                        }
                        #endregion

                        #region bio verification

                        var pursedData = await _orderManager.SubmitOrderDataPurseV2(model);
                        BiomerticDataModel dataModel = bioverifyDataMapp(pursedData);
                        verifyResp = await _bio.BssServiceProcessV2(dataModel);

                        if (verifyResp.is_success == true)
                        {
                            model.bss_reqId = verifyResp.bss_req_id;
                            model.status = (int)EnumRAOrderStatus.BioVerificationSubmitted;
                        }
                        else
                        {
                            model.status = (int)EnumRAOrderStatus.Failed;
                            model.err_code = verifyResp.err_code;
                            model.err_msg = verifyResp.err_msg;
                            model.error_id = verifyResp.error_Id;
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        ErrorDescription errorDescription = new ErrorDescription();
                        model.status = (int)EnumRAOrderStatus.Failed;
                        errorDescription = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                        model.err_code = errorDescription.error_code;
                        model.error_id = errorDescription.error_id;
                        orderRes.message = String.IsNullOrEmpty(errorDescription.error_custom_msg) ? errorDescription.error_description : errorDescription.error_custom_msg;
                        model.err_msg = String.IsNullOrEmpty(errorDescription.error_custom_msg) ? errorDescription.error_description : errorDescription.error_custom_msg;
                        orderRes.isError = false;
                        return Ok(orderRes);
                    }
                }
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
                model.status = (int)EnumRAOrderStatus.Failed;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;

                if (verifyResp != null)
                {
                    orderRes.data.request_id = verifyResp.bss_req_id;
                }
                else
                {
                    orderRes.data.request_id = "";
                }

                orderRes.isError = true;
                orderRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                model.err_msg = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(error);
            }
            finally
            {
                log.purpose_number = model.purpose_number;
                log.msisdn = _bllLog.FormatMSISDN(model.msisdn);
                log.req_time = DateTime.Now;
                if (model.bi_token_number != null && model.bi_token_number > 1 && is_error_from_ongoing != "ongoing")
                {
                    response2 = await _orderManager.UpdateOrder(new RAOrderRequestUpdate
                    {
                        bi_token_number = model.bi_token_number,
                        msidn = model.msisdn,
                        user_name = model.retailer_id,
                        dest_imsi = model.dest_imsi,
                        status = model.status,
                        bss_reqId = model.bss_reqId,
                        error_id = model.error_id,
                        err_msg = model.err_msg,
                    });
                }
                log.res_time = DateTime.Now;
                if (orderRes.data != null)
                {
                    log.is_success = orderRes.data.request_id.Length > 1 ? 1 : 0;
                    log.bi_token_number = orderRes.data.request_id;
                }
                log.res_blob = _blJson.GetGenericJsonData(orderRes);
                log.method_name = "CorporateSIMReplacementSubmitOrderV3";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

                await _bllLog.RAToDBSSLog(log);

            }
            return Ok(orderRes);
        }

        /// Send Order
        /// <summary>
        /// This API is used for SimReplacement submit order.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Order request token id</returns>
        //[ResponseType(typeof(SendOrderResponse))]
        [HttpPost]
        [CorpSIMReplacementOrderRequestValidator]
        [Route("CorporateReplacementSubmitOrder_ESIMV2")]
        public async Task<IActionResult> CorporateReplacementSubmitOrder_ESIMV2([FromBody][Bind("bi_token_number,channel_name,cid,dbss_subscription_id,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,isBPUser,lac,latitude,longitude,msisdn,old_sim_number,payment_type,poc_msisdn_number,purpose_number,retailer_id,right_id,scanner_id,session_token,sim_number,sim_rep_reason_id,sim_replacement_type,src_dob,src_nid")] CorpSimReplacementRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            NidDobInfoResponse nidDobInfo = new NidDobInfoResponse();
            CorporateMSISDNCheckRequest checkRequest = new CorporateMSISDNCheckRequest();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            string is_error_from_ongoing = string.Empty;
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();
            try
            {
                model = populateModel.CorpSIMReplacementRequestPopulateModel(request);

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;
                double allowedDistance = 0;
                int geoFencEnable = 0;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                geoFencEnable = SettingsValues.GetgeoFencEnableEnability();

                TokenValidationService token = new TokenValidationService(secreteKey);

                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.CastRequestModel(model);

                log.req_blob = _blJson.GetGenericJsonData(requestModelBLOB);
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

                #region Geo fencing BP user
                if (geoFencEnable == 1)
                {
                    if (model.isBPUser == 1)
                    {
                        RACommonResponseRevamp responseRevamp = await _geo.GeoFencingBPUser(model);

                        if (responseRevamp != null && responseRevamp.isError == true)
                        {
                            return Ok(responseRevamp);
                        }
                    }
                }
                #endregion


                #region Get_NID_DOB
                checkRequest.mobile_number = model.msisdn;
                checkRequest.poc_msisdn_number = model.poc_msisdn_number ?? "";
                checkRequest.purpose_number = model.purpose_number;
                checkRequest.retailer_id = model.retailer_id;

                nidDobInfo = await GetNidDobForCorporate(checkRequest);

                if (nidDobInfo.result == false)
                {
                    //orderRes.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = nidDobInfo.message;
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                else
                {
                    if (model.sim_replacement_type != null && model.sim_replacement_type == (int)EnumSIMReplacementType.ByAuthPerson)
                    {
                        model.src_nid = nidDobInfo.dest_nid;
                        model.src_dob = nidDobInfo.dest_dob;
                        model.old_sim_number = nidDobInfo.old_sim_number;
                    }
                    else
                    {
                        model.dest_nid = nidDobInfo.dest_nid;
                        model.dest_dob = nidDobInfo.dest_dob;
                        model.old_sim_number = nidDobInfo.old_sim_number;
                    }
                }

                #region Check if submitted order is already in process or not.
                var orderValidationResult = await _orderManager.ValidateOrder(new VMValidateOrder
                {
                    msisdn = model.msisdn,
                    sim_number = model.sim_number,
                    purpose_number = Convert.ToInt32(model.purpose_number),
                    is_corporate = 1,
                    retailer_id = model.retailer_id,
                    dest_dob = DateTime.Parse(model.dest_dob).ToString(StringFormatCollection.DBSSDOBFormat)
                });
                if (orderValidationResult.result == false)
                {
                    // orderRes.request_id = "0";
                    orderRes.isError = true;
                    orderRes.message = orderValidationResult.message;
                    is_error_from_ongoing = "ongoing";
                    log.is_success = 0;
                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                    return Ok(orderRes);
                }
                #endregion
                #region Insert_Order
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;
                model.is_esim = 1;
                model.prov_id = loginProviderId;
                orderRes = await _orderManager.SubmitOrderV7(model);

                if (orderRes.isError)
                {
                    return Ok(new SendOrderResponseRev()
                    {
                        isError = false,
                        message = orderRes.message
                    });
                }
                model.bi_token_number = Convert.ToDouble(orderRes.data.request_id);
                #endregion
                #region Get IMSI
                var imsiResp = await _bio.GetImsiBySimAsync(new GetImsiReq
                {
                    purpose_number = model.purpose_number,
                    retailer_id = model.retailer_id,
                    sim = model.sim_number,
                    msisdn = model.msisdn
                });

                if (imsiResp.result == false)
                {
                    model.status = (int)EnumRAOrderStatus.Failed;
                    orderRes.isError = true;
                    orderRes.message = imsiResp.message;
                    model.err_msg = imsiResp.message;
                    return Ok(orderRes);
                }
                else
                {
                    model.dest_imsi = imsiResp.imsi;//[Note: here IMSI is being sent as SIM number as per business requirement]
                }
                #endregion

                #region bio verification

                var pursedData = await _orderManager.SubmitOrderDataPurseV2(model);
                BiomerticDataModel dataModel = bioverifyDataMapp(pursedData);
                verifyResp = await _bio.BssServiceProcessV2(dataModel);

                if (verifyResp.is_success == true)
                {
                    model.bss_reqId = verifyResp.bss_req_id;
                    model.status = (int)EnumRAOrderStatus.BioVerificationSubmitted;
                }
                else
                {
                    model.status = (int)EnumRAOrderStatus.Failed;
                    model.err_code = verifyResp.err_code;
                    model.err_msg = verifyResp.err_msg;
                    model.error_id = verifyResp.error_Id;
                }
                #endregion
                //=====Order submission=====
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
                model.status = (int)EnumRAOrderStatus.Failed;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.is_success = 0;

                if (verifyResp != null)
                {
                    orderRes.data.request_id = verifyResp.bss_req_id;
                }
                else
                {
                    orderRes.data.request_id = "";
                }

                orderRes.isError = true;
                orderRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                model.err_msg = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(error);
            }
            finally
            {
                log.purpose_number = model.purpose_number;
                log.msisdn = _bllLog.FormatMSISDN(model.msisdn);
                log.req_time = DateTime.Now;
                if (model.bi_token_number != null && model.bi_token_number > 1 && is_error_from_ongoing != "ongoing")
                {
                    response2 = await _orderManager.UpdateOrder(new RAOrderRequestUpdate
                    {
                        bi_token_number = model.bi_token_number,
                        dest_imsi = model.dest_imsi,
                        msidn = model.msisdn,
                        status = model.status,
                        bss_reqId = model.bss_reqId,
                        error_id = model.error_id,
                        err_msg = model.err_msg,
                        user_name = model.retailer_id
                    });
                }
                log.res_time = DateTime.Now;
                try
                {
                    log.is_success = orderRes.data.request_id.Length > 1 ? 1 : 0;
                }
                catch
                {
                }
                log.res_blob = _blJson.GetGenericJsonData(orderRes);
                log.bi_token_number = orderRes.data.request_id;
                log.method_name = "CorporateReplacementSubmitOrder_ESIM";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(orderRes);
        }
        #endregion

        public BiomerticDataModel bioverifyDataMapp(OrderRequest2 order)
        {
            BiomerticDataModel resp = new BiomerticDataModel();

            if(order != null)
            {
                resp.status = order.status;
                resp.create_date = DateTime.Now.ToString();

                if (order.purpose_number != null)
                    resp.purpose_number = (int)order.purpose_number;
                if (order.dest_doc_type_no != null)
                    resp.dest_doc_type_no = order.dest_doc_type_no.ToString() ?? "";
                if (!String.IsNullOrEmpty(order.dest_nid))
                    resp.dest_doc_id = order.dest_nid;
                if (!String.IsNullOrEmpty(order.retailer_id))
                    resp.user_id = order.retailer_id;
                if (!String.IsNullOrEmpty(order.msisdn))
                    resp.msisdn = order.msisdn;
                if (order.dest_ec_verifi_reqrd != null)
                    resp.dest_ec_verification_required = (int)order.dest_ec_verifi_reqrd;
                if (!String.IsNullOrEmpty(order.dest_imsi))
                    resp.dest_imsi = order.dest_imsi;
                if (order.dest_foreign_flag != null)
                    resp.dest_foreign_flag = (int)order.dest_foreign_flag;
                if (order.dbss_subscription_id != 0)
                    resp.dbss_subscription_id = (int?)order.dbss_subscription_id;
                if (order.sim_category != null)
                {
                    resp.sim_category = (int)order.sim_category;
                }
                else
                {
                    resp.sim_category = 0;
                }
                if (!String.IsNullOrEmpty(order.poc_number))
                    resp.poc_number = order.poc_number;

                resp.dest_dob = order.dest_dob;
                resp.dest_left_thumb = order.dest_left_thumb;
                resp.dest_left_index = order.dest_left_index;
                resp.dest_right_thumb = order.dest_right_thumb;
                resp.dest_right_index = order.dest_right_index;

                if (order.src_doc_type_no != null)
                    resp.src_doc_type_no = order.src_doc_type_no.ToString() ?? "";
                if (order.src_ec_verifi_reqrd != null)
                    resp.src_ec_verification_required = (int)order.src_ec_verifi_reqrd;
                if (order.sim_replacement_type != null)
                    resp.sim_replacement_type = (int)order.sim_replacement_type;
                if (!String.IsNullOrEmpty(order.src_dob))
                    resp.src_dob = order.src_dob;
                if (!String.IsNullOrEmpty(order.src_nid))
                    resp.src_doc_id = order.src_nid;
            } 
            
            return resp;

        }
    }
}
