using BIA.BLL.BLLServices;
using BIA.BLL.Utility;
using BIA.Common;
using BIA.DAL.Repositories;
using BIA.Entity;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Entity.ViewModel;
using BIA.Helper;
using BIA.JWT;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Collections;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace BIA.Controllers
{
    [Route("api/Common")]
    [ApiController]
    public class CommonController : ControllerBase
    {
        private BLLRAToDBSSParse _raToDBssParse;
        private BLLDBSSToRAParse _dbssToRaParse;
        private ApiRequest _apiReq;
        private BLLCommon _bllCommon;
        private BL_Json _blJson;
        private readonly DALBiometricRepo dataManager;
        private readonly BLLOrder _bllOrder;
        private readonly ApiManager _apiManager;
        private readonly BLLLog _bllLog;
        private readonly BaseController _bio;
        private readonly BLLDivDisThana _divDisThana;
        private readonly BLLUserAuthenticaion _bLLUserAuthenticaion;
        private readonly SingleSourceGACappingService _singleSourceGACappingService;

        public CommonController(DALBiometricRepo dataManager, BLLDBSSToRAParse dbssToRaParse, BLLRAToDBSSParse raToDBssParse, ApiRequest apiReq, BL_Json blJson, BLLCommon bllCommon, BLLOrder bllOrder, ApiManager apiManager, BLLLog bllLog, BaseController bio, BLLDivDisThana divDisThana, BLLUserAuthenticaion bLLUserAuthenticaion, SingleSourceGACappingService singleSourceGACappingService)
        {
            this._bllCommon = bllCommon;
            this._raToDBssParse = raToDBssParse;
            this._dbssToRaParse = dbssToRaParse;
            this._apiReq = apiReq;
            this._blJson = blJson;
            this.dataManager = dataManager;
            this._bllOrder = bllOrder;
            this._apiManager = apiManager;
            this._bllLog = bllLog;
            this._bio = bio;
            this._divDisThana = divDisThana;
            this._bLLUserAuthenticaion = bLLUserAuthenticaion;
            this._singleSourceGACappingService = singleSourceGACappingService;
        }

        #region Get Subscription Type

        /// <summary>
        /// This API is used to Get Subscription Type.
        /// </summary>
        /// <param name=""></param>
        /// <returns>Subscription Type List / Failure</returns>
        //[Authorize(Roles = "Retailer")]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetSubscriptionTypesV3")]
        public async Task<IActionResult> GetSubscriptionTypesV3([FromBody][Bind("channel_name,lan,retailer_id,session_token,subscription_type")] RASubscriptionTypeReq model)
        {
            List<SubscriptionTypeReponseDataRev> raRespData = new List<SubscriptionTypeReponseDataRev>();
            SubscriptionTypeReponseRev raResp = new SubscriptionTypeReponseRev();
            string? apiUrl = string.Empty, txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                try
                {
                    secreteKey = SettingsValues.GetJWTSequrityKey();
                }
                catch
                { }
                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                apiUrl = String.Format(GetAPICollection.GetSubscriptionTypes, model.subscription_type, model.channel_name);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetSubscriptionTypesV3");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                if (dbssResp != null)
                {
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    log.is_success = 1;

                    //var dataBss = JsonConvert.DeserializeObject(dbssResp.ToString());

                    SubscriptionTypeRootData? dbssRespModel = JsonConvert.DeserializeObject<SubscriptionTypeRootData>(dbssResp.ToString());

                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel.data != null)
                        {
                            var result = ((IEnumerable)dbssRespModel.data).Cast<object>().ToList();

                            raRespData = _dbssToRaParse.SubscripTypesReqParsingV2(result);

                            if (raRespData.Count > 0)
                            {
                                raResp.data = raRespData;
                                raResp.isError = false;
                                raResp.message = MessageCollection.Success;
                            }
                            else
                            {
                                raResp.data = raRespData;
                                raResp.isError = true;
                                raResp.message = MessageCollection.NoDataFound;
                            }
                        }
                        else
                        {
                            raResp.data = raRespData;
                            raResp.isError = true;
                            raResp.message = "DBSS API doesn't contains any subscription types data.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "DBSS API doesn't contains any subscription types data.";
                    }
                }
                else
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = "Unable to load data from DBSS API.";
                }
                return Ok(raResp);
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
                log.message = error.error_custom_msg ?? error.error_description;
                raResp.data = raRespData;
                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                return Ok(raResp);
            }
            finally
            {
                log.method_name = "GetSubscriptionTypesV3";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;
                string rspStr = string.Empty;
                if (txtResp != null)
                {
                    rspStr = txtResp;
                }
                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetSubscriptionTypesV4")]
        public async Task<IActionResult> GetSubscriptionTypesV4([FromBody][Bind("channel_name,is_bp,lan,retailer_id,right_id,session_token,subscription_type")] RASubscriptionTypeReqWithMapping model)
        {
            List<SubscriptionTypeReponseDataRev> raRespData = new List<SubscriptionTypeReponseDataRev>();
            SubscriptionTypeReponseRev raResp = new SubscriptionTypeReponseRev();
            string? apiUrl = string.Empty, txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            List<SubscriptionMappingResponse> subscriptionMapping = new List<SubscriptionMappingResponse>();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                try
                {
                    secreteKey = SettingsValues.GetJWTSequrityKey();
                }
                catch
                { }
                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                apiUrl = String.Format(GetAPICollection.GetSubscriptionTypes, model.subscription_type, model.channel_name);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetSubscriptionTypesV3");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                if (dbssResp != null)
                {
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    log.is_success = 1;

                    //var dataBss = JsonConvert.DeserializeObject(dbssResp.ToString());

                    SubscriptionTypeRootData? dbssRespModel = JsonConvert.DeserializeObject<SubscriptionTypeRootData>(dbssResp.ToString());

                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel.data != null)
                        {
                            var result = ((IEnumerable)dbssRespModel.data).Cast<object>().ToList();

                            raRespData = _dbssToRaParse.SubscripTypesReqParsingV2(result);

                            if (raRespData.Count > 0)
                            {
                                subscriptionMapping = await _bllCommon.GetSubscriptionMapping(model);

                                if(subscriptionMapping.Count > 0)
                                {
                                    var filteredRaRespData = raRespData.Where(r => subscriptionMapping.Any(m => m.subscription_code == r.subscription_name)).ToList();

                                    if (filteredRaRespData.Any())
                                    {
                                        raResp.data = filteredRaRespData;
                                        raResp.isError = false;
                                        raResp.message = MessageCollection.Success;
                                    }
                                    else
                                    {
                                        raResp.data = null;
                                        raResp.isError = true;
                                        raResp.message = MessageCollection.NoDataFound;
                                    }
                                }
                                else
                                {
                                    raResp.data = null;
                                    raResp.isError = true;
                                    raResp.message = "Data not found in Mapping!";
                                }                                
                            }
                            else
                            {
                                raResp.data = raRespData;
                                raResp.isError = true;
                                raResp.message = MessageCollection.NoDataFound;
                            }
                        }
                        else
                        {
                            raResp.data = raRespData;
                            raResp.isError = true;
                            raResp.message = "DBSS API doesn't contains any subscription types data.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "DBSS API doesn't contains any subscription types data.";
                    }
                }
                else
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = "Unable to load data from DBSS API.";
                }
                return Ok(raResp);
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
                log.message = error.error_custom_msg ?? error.error_description;
                raResp.data = raRespData;
                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                return Ok(raResp);
            }
            finally
            {
                log.method_name = "GetSubscriptionTypesV4";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;
                string rspStr = string.Empty;
                if (txtResp != null)
                {
                    rspStr = txtResp;
                }
                await _bllLog.RAToDBSSLog(log);
            }
        }



        /// <summary>
        /// This API is used to Get Subscription Type.
        /// </summary>
        /// <param name=""></param>
        /// <returns>Subscription Type List / Failure</returns>
        //[Authorize(Roles = "Retailer")] 
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetSubscriptionTypesByIdV2")]
        public async Task<IActionResult> GetSubscriptionTypesByIdV2([FromBody][Bind("channel_name,dbss_subscription_id,lan,retailer_id,session_token,subscription_type")] RASubscriptionTypeReqV2 model)
        {
            List<SubscriptionTypeByIdReponseDataRev> raRespData = new List<SubscriptionTypeByIdReponseDataRev>();
            SubscriptionTypeReponseByIdRev raResp = new SubscriptionTypeReponseByIdRev();
            string? apiUrl = string.Empty, txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                apiUrl = String.Format(GetAPICollection.GetSubscriptionTypesById, model.dbss_subscription_id);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetSubscriptionTypesByIdV2");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                if (dbssResp != null)
                {
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    log.is_success = 1;

                    //var dataBss = JsonConvert.DeserializeObject(dbssResp.ToString());

                    SubscriptionTypeRootData? dbssRespModel = JsonConvert.DeserializeObject<SubscriptionTypeRootData>(dbssResp.ToString());

                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel.data != null)
                        {
                            var result = ((IEnumerable)dbssRespModel.data).Cast<object>().ToList();

                            raRespData = _dbssToRaParse.SubscripTypesByIdReqParsingRev(result);

                            if (raRespData.Count > 0)
                            {
                                raResp.data = raRespData;
                                raResp.isError = false;
                                raResp.message = MessageCollection.Success;
                            }
                            else
                            {
                                raResp.data = raRespData;
                                raResp.isError = true;
                                raResp.message = MessageCollection.NoDataFound;
                            }
                        }
                        else
                        {
                            raResp.data = raRespData;
                            raResp.isError = true;
                            raResp.message = "DBSS API doesn't contains any subscription types data.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "DBSS API doesn't contains any subscription types data.";
                    }
                }
                else
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = "Unable to load data from DBSS API.";
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
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? error.error_description;

                raResp.data = raRespData;
                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.method_name = "GetSubscriptionTypesById";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;
                string rspStr = string.Empty;
                if (txtResp != null)
                {
                    rspStr = txtResp;
                }
                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(raResp);
        }
        #endregion

        [HttpPost]
        [Route("GetSubscriptionTypesPostPaid")]
        public async Task<IActionResult> GetSubscriptionTypesPostPaid(RASubscriptionTypeReq model)
        {
            List<SubscriptionTypeReponseDataRev> raRespData = new List<SubscriptionTypeReponseDataRev>();
            SubscriptionTypeReponseRev raResp = new SubscriptionTypeReponseRev();
            string? apiUrl = string.Empty, txtResp = string.Empty;
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                try
                {
                    secreteKey = SettingsValues.GetJWTSequrityKey();
                }
                catch
                { }
                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }
                var dbssResp = await _bllCommon.GetSubscriptionTypes(model);

                if (dbssResp != null)
                {
                    if (dbssResp.data != null)
                    {
                        for (int i = 0; i < dbssResp.data.Count(); i++)
                        {
                            SubscriptionTypeReponseDataRev data = new SubscriptionTypeReponseDataRev();
                            data.subscription_id = dbssResp.data[i].subscription_id != null ? dbssResp.data[i].subscription_id.ToString() : null;
                            data.subscription_name = dbssResp.data[i].subscription_name != null ? dbssResp.data[i].subscription_name : null;
                            raRespData.Add(data);
                        }
                        if (raRespData.Count > 0)
                        {
                            raResp.data = raRespData;
                            raResp.isError = false;
                            raResp.message = MessageCollection.Success;
                        }
                        else
                        {
                            raResp.data = raRespData;
                            raResp.isError = true;
                            raResp.message = MessageCollection.NoDataFound;
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = MessageCollection.NoDataFound;
                    }
                }
                else
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = MessageCollection.NoDataFound;
                }
                return Ok(raResp);
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

                ErrorDescription error;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    raResp.data = new List<SubscriptionTypeReponseDataRev>();
                    raResp.isError = true;
                    raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                }
                catch (Exception)
                {
                    raResp.isError = true;
                    raResp.message = ex.Message;
                }
                return Ok(raResp);
            }

        }

        #region Get Package
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetPackagesV3")]
        public async Task<IActionResult> GetPackagesV3([FromBody][Bind("channel_name,lan,offer_name,retailer_id,session_token,subscription_id")] RAGetPackageResquest model)
        {
            //Step-0 :
            List<PackagesReponseDataRev> raRespData = new List<PackagesReponseDataRev>();
            PackagesResponseRev raResp = new PackagesResponseRev();
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            PackageRootData? dbssRespModel = new PackageRootData();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                apiUrl = String.Format(GetAPICollection.GetPackagesBySubscriptionTypeId, model.subscription_id);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.user_id = model.retailer_id;
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetPackagesV3");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp != null)
                {
                    log.is_success = 1;
                    try
                    {
                        dbssRespModel = JsonConvert.DeserializeObject<PackageRootData>(dbssResp.ToString());
                    }
                    catch
                    {
                        raResp.isError = true;
                        raResp.message = MessageCollection.NoDataFound;
                        return Ok(raResp);
                    }

                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel?.included != null && dbssRespModel.included is IEnumerable enumerable)
                        {
                            var result = enumerable.Cast<object>().ToList();

                            if (result.Count > 0)
                            {
                                raRespData = _dbssToRaParse.PackagesParsingV2(result);

                                if (raRespData.Count > 0)
                                {
                                    raResp.data = raRespData;
                                    raResp.isError = false;
                                    raResp.message = MessageCollection.Success;
                                }
                                else
                                {
                                    raResp.data = raRespData;
                                    raResp.isError = true;
                                    raResp.message = MessageCollection.NoDataFound;
                                }
                            }
                            else
                            {
                                raResp.data = new List<PackagesReponseDataRev>();
                                raResp.isError = true;
                                raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                            }
                        }
                        else
                        {
                            raResp.data = new List<PackagesReponseDataRev>();
                            raResp.isError = true;
                            raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                    }
                }

                return Ok(raResp);
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
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return Ok(raResp);
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.user_id = model.retailer_id;
                log.method_name = "GetPackagesV3";
                string rspStr = string.Empty;
                if (txtResp != null)
                {
                    rspStr = txtResp;
                }
                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetPackagesV4")]
        public async Task<IActionResult> GetPackagesV4([FromBody][Bind("category_name,lan,retailer_id,session_token,subscription_id")] RAGetPackageResquestV3 model)
        {
            List<PackagesReponseDataRev> raRespData = new List<PackagesReponseDataRev>();
            PackagesResponseRev raResp = new PackagesResponseRev();
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                apiUrl = String.Format(GetAPICollection.GetPackagesBySubscriptionTypeId, model.subscription_id);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.user_id = model.retailer_id;
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetPackagesV4");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp != null)
                {
                    log.is_success = 1;
                    PackageRootData? dbssRespModel = JsonConvert.DeserializeObject<PackageRootData>(dbssResp.ToString());

                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel.included != null)
                        {
                            var result = ((IEnumerable)dbssRespModel.included).Cast<object>().ToList();
                            raRespData = await _dbssToRaParse.PackagesParsingV3(result, model.category_name);

                            if (raRespData.Count > 0)
                            {
                                raResp.data = raRespData;
                                raResp.isError = false;
                                raResp.message = MessageCollection.Success;
                            }
                            else
                            {
                                raResp.data = raRespData;
                                raResp.isError = true;
                                raResp.message = MessageCollection.NoDataFound;
                            }
                        }
                        else
                        {
                            raResp.data = raRespData;
                            raResp.isError = true;
                            raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                    }
                }

                return Ok(raResp);
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
                log.res_time = DateTime.Now;
                ErrorDescription error = new ErrorDescription();
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.InnerException?.Message);
                log.res_time = DateTime.Now;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    raResp.isError = true;
                    raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    return Ok(raResp);
                }
                catch (Exception)
                {
                    raResp.isError = true;
                    raResp.message = ex.Message;

                    return Ok(raResp);
                }
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.user_id = model.retailer_id;
                log.method_name = "GetPackagesV3";
                string rspStr = string.Empty;
                if (txtResp != null)
                {
                    rspStr = txtResp;
                }

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetPackagesV5")]
        public async Task<IActionResult> GetPackagesV5([FromBody][Bind("channel_name,lan,offer_name,retailer_id,session_token,subscription_id")] RAGetPackageResquest model)
        {
            //Step-0 :
            List<PackagesReponseDataRev> raRespData = new List<PackagesReponseDataRev>();
            PackagesResponseRev raResp = new PackagesResponseRev();
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            PackageRootData? dbssRespModel = new PackageRootData();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                apiUrl = String.Format(GetAPICollection.GetPackagesBySubscriptionTypeId, model.subscription_id);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.user_id = model.retailer_id;
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetPackagesV3");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp != null)
                {
                    log.is_success = 1;
                    try
                    {
                        dbssRespModel = JsonConvert.DeserializeObject<PackageRootData>(dbssResp.ToString());
                    }
                    catch
                    {
                        raResp.isError = true;
                        raResp.message = MessageCollection.NoDataFound;
                        return Ok(raResp);
                    }

                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel?.included != null && dbssRespModel.included is IEnumerable enumerable)
                        {
                            var result = enumerable.Cast<object>().ToList();

                            if (result.Count > 0)
                            {
                                if (model.channel_name.Equals("RESELLER") && model.subscription_id == "1")
                                {
                                    raRespData = _dbssToRaParse.PackagesParsingV4(result, model.offer_name);
                                }
                                else
                                {
                                    raRespData = _dbssToRaParse.PackagesParsingV2(result);
                                }

                                if (raRespData.Count > 0)
                                {
                                    raResp.data = raRespData;
                                    raResp.isError = false;
                                    raResp.message = MessageCollection.Success;
                                }
                                else
                                {
                                    raResp.data = raRespData;
                                    raResp.isError = true;
                                    raResp.message = MessageCollection.NoDataFound;
                                }
                            }
                            else
                            {
                                raResp.data = new List<PackagesReponseDataRev>();
                                raResp.isError = true;
                                raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                            }
                        }
                        else
                        {
                            raResp.data = new List<PackagesReponseDataRev>();
                            raResp.isError = true;
                            raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                    }
                }

                return Ok(raResp);
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
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return Ok(raResp);
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.user_id = model.retailer_id;
                log.method_name = "GetPackagesV3";
                string rspStr = string.Empty;
                if (txtResp != null)
                {
                    rspStr = txtResp;
                }
                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetPackagesV6")]
        public async Task<IActionResult> GetPackagesV6([FromBody][Bind("category_name,channel_name,is_bp,lan,offer_name,retailer_id,right_id,session_token,subscription_id,subscription_name")] RAGetPackageResquestV4 model)
        {
            //Step-0 :
            List<PackagesReponseDataRev> raRespData = new List<PackagesReponseDataRev>();
            List<PackageCodeMappingRespModel> packageCodes = new List<PackageCodeMappingRespModel>();
            PackagesResponseRev raResp = new PackagesResponseRev();
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            PackageRootData? dbssRespModel = new PackageRootData();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                apiUrl = String.Format(GetAPICollection.GetPackagesBySubscriptionTypeId, model.subscription_id);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.user_id = model.retailer_id;
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetPackagesV6");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);


                if (dbssResp != null)
                {
                    log.is_success = 1;
                    try
                    {
                        dbssRespModel = JsonConvert.DeserializeObject<PackageRootData>(dbssResp.ToString());
                    }
                    catch
                    {
                        raResp.isError = true;
                        raResp.message = MessageCollection.NoDataFound;
                        return Ok(raResp);
                    }

                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel?.included != null && dbssRespModel.included is IEnumerable enumerable)
                        {
                            var result = enumerable.Cast<object>().ToList();

                            if (result.Count > 0)
                            {
                                //if (model.channel_name.Equals("RESELLER") && model.subscription_id == "1")
                                //{
                                //    raRespData = _dbssToRaParse.PackagesParsingV4(result, model.offer_name);
                                //}
                                //else
                                //{
                                //    raRespData = _dbssToRaParse.PackagesParsingV2(result);
                                //}

                                raRespData = _dbssToRaParse.PackagesParsingV2(result);

                                if (raRespData.Count > 0)
                                {
                                    packageCodes = await _bllCommon.GetPackageMapping(model);

                                    if (packageCodes.Count > 0)
                                    {
                                        var filteredRaRespData = raRespData.Where(r => packageCodes.Any(m => m.package_code == r.package_name)).ToList();

                                        if (filteredRaRespData.Any())
                                        {
                                            raResp.data = filteredRaRespData;
                                            raResp.isError = false;
                                            raResp.message = MessageCollection.Success;
                                        }
                                        else
                                        {
                                            raResp.data = null;
                                            raResp.isError = true;
                                            raResp.message = MessageCollection.NoDataFound;
                                        }
                                    }
                                }
                                else
                                {
                                    raResp.data = raRespData;
                                    raResp.isError = true;
                                    raResp.message = MessageCollection.NoDataFound;
                                }
                            }
                            else
                            {
                                raResp.data = new List<PackagesReponseDataRev>();
                                raResp.isError = true;
                                raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                            }
                        }
                        else
                        {
                            raResp.data = new List<PackagesReponseDataRev>();
                            raResp.isError = true;
                            raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "Subscription type id " + model.subscription_id + " doesn't contain any packages.";
                    }
                }

                return Ok(raResp);
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
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;

                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return Ok(raResp);
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.user_id = model.retailer_id;
                log.method_name = "GetPackagesV6";
                string rspStr = string.Empty;
                if (txtResp != null)
                {
                    rspStr = txtResp;
                }
                await _bllLog.RAToDBSSLog(log);
            }
        }



        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetPackagesPreToPostMigrationV2")]
        public async Task<IActionResult> GetPackagesPreToPostMigrationV2([FromBody][Bind("lan,retailer_id,session_token,subscription_type_id")] RAGetPackageResquestV2 model)
        {
            //Step-0 :
            List<PackagesReponseDataRev> raRespData = new List<PackagesReponseDataRev>();
            PackagesResponseRev raResp = new PackagesResponseRev();
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                apiUrl = String.Format(GetAPICollection.GetPackagesBySubscriptionTypeId, model.subscription_type_id);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.user_id = model.retailer_id;
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetPackagesPreToPostMigrationV2");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp != null)
                {
                    log.is_success = 1;
                    PackageRootData? dbssRespModel = JsonConvert.DeserializeObject<PackageRootData>(dbssResp.ToString());

                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel.included != null)
                        {
                            var result = ((IEnumerable)dbssRespModel.included).Cast<object>().ToList();
                            raRespData = _dbssToRaParse.PackagesParsingV2(result);

                            if (raRespData.Count > 0)
                            {
                                raResp.data = raRespData;
                                raResp.isError = false;
                                raResp.message = MessageCollection.Success;
                            }
                            else
                            {
                                raResp.data = raRespData;
                                raResp.isError = true;
                                raResp.message = "packagesNotFound";  //MessageCollection.NoDataFound;
                            }
                        }
                        else
                        {
                            raResp.data = raRespData;
                            raResp.isError = true;
                            raResp.message = "packagesNotFound";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "packagesNotFound";
                    }
                }

                return Ok(raResp);
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

                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.res_time = DateTime.Now;
                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return Ok(raResp);
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;
                log.method_name = "GetPackagesPreToPostMigrationV2";
                string rspStr = string.Empty;
                if (txtResp != null)
                {
                    rspStr = txtResp;
                }

                await _bllLog.RAToDBSSLog(log);
            }
        }

        #endregion


        #region  MSISDN validation Unpaired v2
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ValidateUnpairedMSISDNV3")]
        public async Task<IActionResult> ValidateUnpairedMSISDNV4([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequest msisdnCheckReqest)
        {
            try
            {
                var response = new RACommonResponseRevamp();
                var secreteKey = SettingsValues.GetJWTSequrityKey();
                var tokenService = new TokenValidationService(secreteKey);

                var security = tokenService.ValidateToken(msisdnCheckReqest.session_token);

                if (security == null || !security.IsVallid)
                    throw new Exception(security?.Message ?? "Invalid session token.");

                if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                    throw new Exception(SettingsValues.GetSessionMessage());

                var prov_id = security.LoginProviderId;

                response = await _bio.ValidateUnpairedMSISDNV4(msisdnCheckReqest, "ValidateUnpairedMSISDNV3");

                return Ok(response);
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
                return Ok(new RACommonResponseRevamp
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas
                    {
                        isEsim = 0,
                        request_id = "0"
                    }
                });
            }
        }

        [HttpPost]
        //[ValidateModel]
        [Route("ValidateUnpairedMSISDNV4")]
        public async Task<IActionResult> ValidateUnpairedMSISDNV5([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequest msisdnCheckReqest)
        {
            try
            {
                RACommonResponseRevampV3 response = new RACommonResponseRevampV3();

                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                response = await _bio.ValidateUnpairedMSISDNV6(msisdnCheckReqest, "ValidateUnpairedMSISDNV5");

                return Ok(response);
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
                return Ok(new RACommonResponseRevampV3()
                {
                    isError = true,
                    message = ex.Message,
                    data = null

                });
            }
        }


        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ValidateUnpairedMSISDNV5")]
        public async Task<IActionResult> ValidateUnpairedMSISDNV6([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequest msisdnCheckReqest)
        {
            try
            {
                var response = new RACommonResponseRevamp();
                var secreteKey = SettingsValues.GetJWTSequrityKey();
                var tokenService = new TokenValidationService(secreteKey);

                var security = tokenService.ValidateToken(msisdnCheckReqest.session_token);

                if (security == null || !security.IsVallid)
                    throw new Exception(security?.Message ?? "Invalid session token.");

                if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                    throw new Exception(SettingsValues.GetSessionMessage());

                var prov_id = security.LoginProviderId;

                response = await _bio.ValidateUnpairedMSISDNV4(msisdnCheckReqest, "ValidateUnpairedMSISDNV3");

                if (response.isError == false)
                {
                    #region ICC checking from DMS 
                    ICCDetailsRequestModel model = new ICCDetailsRequestModel()
                    {
                        center_code = msisdnCheckReqest.center_code,
                        icc = msisdnCheckReqest.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        mobile_number = msisdnCheckReqest.mobile_number
                    };

                    ICCDetailsResponse? iccData = await _apiManager.CheckICCfromDMS(model);

                    if (iccData != null && iccData.result)
                    {
                        response.isError = false;
                        response.data.offer_name = iccData.offer_name ?? string.Empty;
                        response.data.product_name = iccData.product_name ?? string.Empty;
                        response.data.details_message = iccData.offer_description ?? string.Empty;
                    }
                    else
                    {
                        response.isError = true;
                        response.message = iccData?.message ?? "Unknown error";
                        return Ok(response);
                    }
                    #endregion
                }
                return Ok(response);
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
                return Ok(new RACommonResponseRevamp
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas
                    {
                        isEsim = 0,
                        request_id = "0"
                    }
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ValidateUnpairedMSISDNV6")]
        public async Task<IActionResult> ValidateUnpairedMSISDNV7([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequest msisdnCheckReqest)
        {
            try
            {
                var response = new RACommonResponseRevampV3();
                var secreteKey = SettingsValues.GetJWTSequrityKey();
                var tokenService = new TokenValidationService(secreteKey);

                var security = tokenService.ValidateToken(msisdnCheckReqest.session_token);

                if (security == null || !security.IsVallid)
                    throw new Exception(security?.Message ?? "Invalid session token.");

                if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                    throw new Exception(SettingsValues.GetSessionMessage());

                var prov_id = security.LoginProviderId;

                response = await _bio.ValidateUnpairedMSISDNV6(msisdnCheckReqest, "ValidateUnpairedMSISDNV6");

                if (response.isError == false)
                {
                    #region ICC checking from DMS 
                    ICCDetailsRequestModel model = new ICCDetailsRequestModel()
                    {
                        center_code = msisdnCheckReqest.center_code,
                        icc = msisdnCheckReqest.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        mobile_number = msisdnCheckReqest.mobile_number
                    };

                    ICCDetailsResponse? iccData = await _apiManager.CheckICCfromDMS(model);

                    if (iccData != null && iccData.result)
                    {
                        response.isError = false;
                        response.data.offer_name = iccData.offer_name ?? string.Empty;
                        response.data.product_name = iccData.product_name ?? string.Empty;
                        response.data.details_message = iccData.offer_description ?? string.Empty;
                    }
                    else
                    {
                        response.isError = true;
                        response.message = iccData?.message ?? "Unknown error";
                        return Ok(response);
                    }
                    #endregion
                }
                return Ok(response);
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
                return Ok(new RACommonResponseRevamp
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas
                    {
                        isEsim = 0,
                        request_id = "0"
                    }
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ValidateUnpairedMSISDNV7")]
        public async Task<IActionResult> ValidateUnpairedMSISDNV8([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,is_bp,lan,mobile_number,product_code,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] unpairedMSISDNCheckReq msisdnCheckReqest)
        {
            try
            {
                SIMProductMapResponse productMapResponse = new SIMProductMapResponse();
                var response = new RACommonResponseRevampV3();
                var secreteKey = SettingsValues.GetJWTSequrityKey();
                var tokenService = new TokenValidationService(secreteKey);

                var security = tokenService.ValidateToken(msisdnCheckReqest.session_token);

                if (security == null || !security.IsVallid)
                    throw new Exception(security?.Message ?? "Invalid session token.");

                if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                    throw new Exception(SettingsValues.GetSessionMessage());

                var prov_id = security.LoginProviderId;

                #region ICC checking from DMS 
                ICCDetailsRequestModel model = new ICCDetailsRequestModel()
                {
                    center_code = msisdnCheckReqest.center_code,
                    icc = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    mobile_number = msisdnCheckReqest.mobile_number
                };

                ICCDetailsResponse? iccData = await _apiManager.CheckICCfromDMS(model);

                if (iccData != null && iccData.result)
                {
                    var reqmodel = new SIMProductMappingReqModel()
                    {
                        channel_id = msisdnCheckReqest.channel_id,
                        channel_name = msisdnCheckReqest.channel_name,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        is_bp = msisdnCheckReqest.is_bp,
                        mobile_number = msisdnCheckReqest.mobile_number,
                        product_code = iccData.product_name,
                        right_id = msisdnCheckReqest.right_id
                    };

                    productMapResponse = await _bllCommon.CeckSIMProductMapping(reqmodel);

                    if (productMapResponse != null && productMapResponse.is_success && productMapResponse.message.ToLower() == "valid")
                    {
                        response.isError = false;
                    }
                    else
                    {
                        if (productMapResponse != null && !String.IsNullOrEmpty(productMapResponse.message))
                        {
                            response.isError = true;
                            response.message = productMapResponse.message;
                            return Ok(response);
                        }
                        else
                        {
                            response.isError = true;
                            response.message = "Error while checking the SIM Mapping!";
                            return Ok(response);
                        }
                    }
                }
                else
                {
                    response.isError = true;
                    response.message = iccData?.message ?? "Unknown error";
                    return Ok(response);
                }
                #endregion
                response = await _bio.ValidateUnpairedMSISDNWithMapping(msisdnCheckReqest, "ValidateUnpairedMSISDNV8");

                if (!response.isError)
                {
                    response.isError = false;
                    response.data.offer_name = iccData.offer_name ?? string.Empty;
                    response.data.product_name = iccData.product_name ?? string.Empty;
                    response.data.details_message = iccData.offer_description ?? string.Empty;
                }
                else
                {
                    response.isError = true;
                }

                if (response.isError == false && response.data.isDesiredCategory == true)
                {
                    var product_category_config = SettingsValues.GetMMSTDProduct();

                    if (!string.IsNullOrEmpty(product_category_config))
                    {
                        var configValues = product_category_config.Contains(',') ? product_category_config.Split(',') : new string[] { product_category_config };

                        if (configValues.Any(x => x == response.data.product_name))
                        {
                            response.isError = true;
                            response.message = "You are not authorised for this connection!";
                            return Ok(response);
                        }
                    }
                }

                return Ok(response);
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
                return Ok(new RACommonResponseRevamp
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas
                    {
                        isEsim = 0,
                        request_id = "0"
                    }
                });
            }
        }


        /// <summary>
        /// This API is used for MSISDN validation for unpaired MSISDN
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        //[Authorize]
        //[GzipCompression]
        //[ResponseType(typeof(RACommonResponse))]


        /// <summary>
        /// This API is used for MSISDN validation for unpaired MSISDN
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        //[Authorize]
        //[GzipCompression]
        ////eSim Start(eSim Logic)
        //[ResponseType(typeof(RACommonResponse))]
        [HttpPost]
        [ValidateModel]
        [IgnoreAntiforgeryToken]
        [Route("ValidateUnpairedMSISDN_ESIMV2")]
        public async Task<IActionResult> ValidateUnpairedMSISDN_ESIMV2([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequest msisdnCheckReqest)
        {
            RACommonResponseRevamp rACommonResponse = new RACommonResponseRevamp();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                rACommonResponse = await _bio.ValidateUnpairedMSISDNV5(msisdnCheckReqest, "ValidateUnpairedMSISDN_ESIMV2");

                return Ok(rACommonResponse);
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas()
                    {
                        request_id = "0",
                        isEsim = 1
                    }
                });
            }
        }

        [HttpPost]
        [ValidateModel]
        [IgnoreAntiforgeryToken]
        [Route("ValidateUnpairedMSISDN_ESIMV3")]
        public async Task<IActionResult> ValidateUnpairedMSISDN_ESIMV3([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] UnpairedMSISDNCheckRequest msisdnCheckReqest)
        {
            RACommonResponseRevamp rACommonResponse = new RACommonResponseRevamp();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                rACommonResponse = await _bio.ValidateUnpairedMSISDNV5(msisdnCheckReqest, "ValidateUnpairedMSISDN_ESIMV2");

                if (rACommonResponse.isError == false)
                {
                    #region ICC checking from DMS 
                    ICCDetailsRequestModel model = new ICCDetailsRequestModel()
                    {
                        center_code = msisdnCheckReqest.center_code,
                        icc = msisdnCheckReqest.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        mobile_number = msisdnCheckReqest.mobile_number
                    };

                    ICCDetailsResponse? iccData = await _apiManager.CheckICCfromDMS(model);

                    if (iccData != null && iccData.result)
                    {
                        rACommonResponse.isError = false;
                        rACommonResponse.data.offer_name = iccData.offer_name ?? string.Empty;
                        rACommonResponse.data.product_name = iccData.product_name ?? string.Empty;
                        rACommonResponse.data.details_message = iccData.offer_description ?? string.Empty;
                    }
                    else
                    {
                        rACommonResponse.isError = true;
                        rACommonResponse.message = iccData?.message ?? "Unknown error";
                        return Ok(rACommonResponse);
                    }
                    #endregion
                }

                return Ok(rACommonResponse);
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas()
                    {
                        request_id = "0",
                        isEsim = 1
                    }
                });
            }
        }

        [HttpPost]
        [ValidateModel]
        [IgnoreAntiforgeryToken]
        [Route("ValidateUnpairedMSISDN_ESIMV4")]
        public async Task<IActionResult> ValidateUnpairedMSISDN_ESIMV4([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,is_bp,lan,mobile_number,product_code,purpose_number,retailer_id,right_id,session_token,sim_category,sim_number")] unpairedMSISDNCheckReq msisdnCheckReqest)
        {
            RACommonResponseRevamp rACommonResponse = new RACommonResponseRevamp();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();
                SIMProductMapResponse productMapResponse = new SIMProductMapResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                #region ICC checking from DMS 
                ICCDetailsRequestModel model = new ICCDetailsRequestModel()
                {
                    center_code = msisdnCheckReqest.center_code,
                    icc = msisdnCheckReqest.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    mobile_number = msisdnCheckReqest.mobile_number
                };

                ICCDetailsResponse? iccData = await _apiManager.CheckICCfromDMS(model);

                if (iccData != null && iccData.result)
                {
                    var reqmodel = new SIMProductMappingReqModel()
                    {
                        channel_id = msisdnCheckReqest.channel_id,
                        channel_name = msisdnCheckReqest.channel_name,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        is_bp = msisdnCheckReqest.is_bp,
                        mobile_number = msisdnCheckReqest.mobile_number,
                        product_code = iccData.product_name,
                        right_id = msisdnCheckReqest.right_id
                    };

                    productMapResponse = await _bllCommon.CeckSIMProductMapping(reqmodel);

                    if (productMapResponse != null && productMapResponse.is_success && productMapResponse.message.ToLower() == "valid")
                    {
                        rACommonResponse.isError = false;
                    }
                    else
                    {
                        if (productMapResponse != null && !String.IsNullOrEmpty(productMapResponse.message))
                        {
                            rACommonResponse.isError = true;
                            rACommonResponse.message = productMapResponse.message;
                            return Ok(rACommonResponse);
                        }
                        else
                        {
                            rACommonResponse.isError = true;
                            rACommonResponse.message = "Error while checking the SIM Mapping!";
                            return Ok(rACommonResponse);
                        }
                    }
                }
                else
                {
                    rACommonResponse.isError = true;
                    rACommonResponse.message = iccData?.message ?? "Unknown error";
                    return Ok(rACommonResponse);
                }
                #endregion

                rACommonResponse = await _bio.ValidateUnpairedMSISDNDuplicateDialESIM(msisdnCheckReqest, "ValidateUnpairedMSISDN_ESIMV4");

                if (!rACommonResponse.isError)
                {
                    rACommonResponse.isError = false;
                    rACommonResponse.data.offer_name = iccData.offer_name ?? string.Empty;
                    rACommonResponse.data.product_name = iccData.product_name ?? string.Empty;
                    rACommonResponse.data.details_message = iccData.offer_description ?? string.Empty;
                }
                else
                {
                    rACommonResponse.isError = true;
                }

                return Ok(rACommonResponse);
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas()
                    {
                        request_id = "0",
                        isEsim = 1
                    }
                });
            }
        }
        #endregion


        #region Get DivDisThana
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetDivDisThanaV3")]
        public async Task<IActionResult> GetDivDisThanaV3([FromBody][Bind("right_id,session_token")] RACommonRequest model)
        {
            DivDisThanaResponseRevamp divDisThanaRes = new DivDisThanaResponseRevamp();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                List<DivisionModelV2> divList = await _divDisThana.GetDivisionV2();
                List<DistrictModelV2> disList = await _divDisThana.GetDistrictV2();
                List<ThanaModelV2> thanaList = await _divDisThana.GetThanaV2();

                foreach (DivisionModelV2 item in divList)
                {
                    item.DistrictModel = disList.Where(a => a.DIVISIONID == item.DIVISIONID);

                    foreach (DistrictModelV2 item2 in item.DistrictModel)
                    {
                        item2.ThanaModel = thanaList.Where(a => a.DISTRICTID == item2.DISTRICTID);
                    }
                }
                divDisThanaRes.data = divList;
                divDisThanaRes.isError = false;
                divDisThanaRes.message = MessageCollection.Success;
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

                ErrorDescription error;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    divDisThanaRes.data = new List<DivisionModelV2>();
                    divDisThanaRes.isError = true;
                    divDisThanaRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                }
                catch (Exception ex2)
                {
                    divDisThanaRes.isError = true;
                    divDisThanaRes.message = ex2.Message;
                }
            }
            return Ok(divDisThanaRes);
        }
        #endregion


        #region Get Status
        /// Send Order
        /// <summary>
        /// This API is used for Status.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>message</returns>
        //[ResponseType(typeof(GetStatusResponse))]
        //[HttpPost]
        //[Route("GetStatusV1")]
        //public async Task<IActionResult> GetStatusV1([FromBody][Bind("request_id,right_id,session_token")] StatusRequest model)
        //{
        //    GetStatusResponse statusRes = new GetStatusResponse();
        //    try
        //    {
        //        if (!await _apiManager.ValidUserBySecurityToken(model.session_token))
        //            throw new Exception(MessageCollection.InvalidSecurityToken);

        //        statusRes = await _bllOrder.GetStatus(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        try
        //        {
        //            ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //            statusRes.status = null;
        //            statusRes.result = false;
        //            statusRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

        //        }
        //        catch (Exception ex2)
        //        {
        //            statusRes.status = null;
        //            statusRes.result = false;
        //            statusRes.message = ex2.Message;

        //        }
        //    }
        //    return Ok(statusRes);
        //}

        ///// Send Order
        ///// <summary>
        ///// This API is used for Status.
        ///// </summary>
        ///// <param name="model"></param>
        ///// <returns>message</returns>
        ////[ResponseType(typeof(GetStatusResponse))]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetStatusV2")]
        public async Task<IActionResult> GetStatusV2([FromBody][Bind("request_id,right_id,session_token")] StatusRequest model)
        {
            GetStatusResponse statusRes = new GetStatusResponse();
            try
            {
                if (!await _apiManager.ValidUserBySecurityTokenV2(model.session_token))
                    throw new Exception(MessageCollection.InvalidSecurityToken);

                statusRes = await _bllOrder.GetStatus(model);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    result = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    statusRes.status = null;
                    statusRes.result = false;
                    statusRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                }
                catch (Exception ex2)
                {
                    statusRes.status = null;
                    statusRes.result = false;
                    statusRes.message = ex2.Message;

                }
            }
            return Ok(statusRes);
        }

        /// Send Order
        /// <summary>
        /// This API is used for Status.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>message</returns>
        //[ResponseType(typeof(GetStatusResponse))]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetStatusV3")]
        public async Task<IActionResult> GetStatusV3([FromBody][Bind("request_id,right_id,session_token")] StatusRequest model)
        {
            GetStatusResponseRevamp statusRes = new GetStatusResponseRevamp();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                statusRes = await _bllOrder.GetStatusV2(model);
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
                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    statusRes.data = new GetStatusResponseDataRevamp()
                    {
                        status = null
                    };
                    statusRes.isError = true;
                    statusRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                }
                catch (Exception ex2)
                {
                    statusRes.data = new GetStatusResponseDataRevamp()
                    {
                        status = null
                    };
                    statusRes.isError = true;
                    statusRes.message = ex2.Message;

                }
            }
            return Ok(statusRes);
        }


        #endregion


        #region Get Purpose Numbers

        /// <summary>
        /// This API is used for Getting DivDisThana
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        //[GzipCompression]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetPurposeNumbersV3")]
        public async Task<IActionResult> GetPurposeNumbersV3([FromBody][Bind("case_id,right_id,session_token")] RAGetPurposeRequest model)
        {
            PurposeNumberReponseRev pnRes = new PurposeNumberReponseRev();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                pnRes = await _bllCommon.GetPurposeNumbersV2(model);

                return Ok(pnRes);
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
                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }
                catch (Exception ex2)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = ex2.Message,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }
            }
        }

        #endregion


        #region Get  Rejected QC Orders 


        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetRejectedQCOrdersV3")]
        public async Task<IActionResult> GetRejectedQCOrdersV3([FromBody][Bind("lan,retailer_id,right_id,session_token")] RejectedOrdersRequest model)
        {
            List<VMRejectedOrder> raRespDataList = new List<VMRejectedOrder>();
            RejectedOrdersResponseRev raResp = new RejectedOrdersResponseRev();
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            RejectedOrdersRootobject? dbssRespModel = new RejectedOrdersRootobject();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                string qcStatus = FixedValueCollection.QCStatusRejected;
                apiUrl = String.Format(GetAPICollection.GetRejectedQCOrders, qcStatus, model.retailer_id);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);

                object dbssResp = new object();
                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetRejectedQCOrdersV3");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                if (dbssResp != null)
                {
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);
                    log.is_success = 1;

                    string? dbssRespStr = dbssResp.ToString();

                    if (!string.IsNullOrWhiteSpace(dbssRespStr))
                    {
                        dbssRespModel = JsonConvert.DeserializeObject<RejectedOrdersRootobject>(dbssRespStr);
                    }

                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel.data.Count > 0)
                        {
                            for (int i = 0; i < dbssRespModel.data.Count; i++)
                            {
                                CustomerInfoResponse customerInfo = await GetCustomerInfo(dbssRespModel.data[i].relationships.usercustomer.links.related
                                                                                , model.retailer_id);
                                var rejectedOrder = await _dbssToRaParse.RejectionOrdersParsing(dbssRespModel.data[i].id
                                                                                        , dbssRespModel.data[i].relationships.usercustomer.data.id /*customer Id*/
                                                                                        , dbssRespModel.data[i].attributes
                                                                                        , customerInfo.CustomerInfo
                                                                                        , customerInfo.CustomerAddressInfo);
                                raRespDataList.Add(rejectedOrder);
                            }


                            if (raRespDataList.Count > 0)
                            {
                                raResp.data = raRespDataList;
                                raResp.isError = false;
                                raResp.message = MessageCollection.Success;
                            }
                        }
                        else
                        {
                            raResp.data = raRespDataList;
                            raResp.isError = true;
                            raResp.message = MessageCollection.NoDataFound;
                        }
                    }
                    else
                    {
                        raResp.data = raRespDataList;
                        raResp.isError = true;
                        raResp.message = MessageCollection.NoDataFound;
                    }
                }
                else
                {
                    raResp.data = raRespDataList;
                    raResp.isError = true;
                    raResp.message = "Unable to get data from DBSS API.";
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

                log.res_time = DateTime.Now;
                log.is_success = 0;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? error.error_description;
                raResp.data = raRespDataList;
                raResp.isError = true;

                raResp.message = !string.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;
                log.method_name = "GetRejectedQCOrdersV3";

                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }

                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(raResp);
        }
        #endregion


        #region Get Customers Info
        private async Task<CustomerInfoResponse> GetCustomerInfo(string apiUrl, string retailerId)
        {
            CustomerInfoResponseRootobject? dbssRespModel = new CustomerInfoResponseRootobject();
            CustomerInfoResponse customerInfoResp = new CustomerInfoResponse();
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                log.req_blob = _blJson.GetGenericJsonData(JsonConvert.SerializeObject(apiUrl));


                JObject dbssResp = new JObject();
                log.req_time = DateTime.Now;

                dbssResp = await _apiReq.HttpGetRequest(SettingsValues.GetDbssBaseUrl() + apiUrl, "GetCustomerInfo");

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                if (dbssResp != null)
                {
                    log.is_success = 1;

                    dbssRespModel = dbssResp.ToObject<CustomerInfoResponseRootobject>();

                    if (dbssRespModel?.data?.attributes != null &&
                        dbssRespModel.data.relationships?.addresses?.links?.related != null)
                    {
                        customerInfoResp.CustomerInfo = dbssRespModel.data.attributes;
                        customerInfoResp.CustomerAddressInfo = await GetCustomerAddress(
                            dbssRespModel.data.relationships.addresses.links.related, retailerId);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;
                ErrorDescription error = new ErrorDescription();
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = retailerId;
                log.method_name = "GetCustomerInfo";
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }

                await _bllLog.RAToDBSSLog(log);
            }
            return customerInfoResp;
        }
        #endregion


        #region Get-Customer-Address
        private async Task<CustomerAddressResponseAttributes> GetCustomerAddress(string apiUrl, string retailerId)
        {
            CustomerAddressResponseRootobject? dbssRespModel = new CustomerAddressResponseRootobject();
            CustomerAddressResponseAttributes customerAddress = new CustomerAddressResponseAttributes();
            string? txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                log.req_blob = _blJson.GetGenericJsonData(JsonConvert.SerializeObject(apiUrl)); ;

                object dbssResp = new object();
                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(SettingsValues.GetDbssBaseUrl() + apiUrl, "GetCustomerAddress");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);
                if (dbssResp != null)
                {
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    log.is_success = 1;

                    string? dbssRespStr = dbssResp.ToString();

                    if (!string.IsNullOrWhiteSpace(dbssRespStr))
                    {
                        dbssRespModel = JsonConvert.DeserializeObject<CustomerAddressResponseRootobject>(dbssRespStr);
                    }

                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel.data.Count < 1)
                        {
                            throw new Exception("No data found. API url: " + SettingsValues.GetDbssBaseUrl() + apiUrl);
                        }
                        else
                        {
                            if (dbssRespModel != null && dbssRespModel.data != null && dbssRespModel.data.Any())
                            {
                                var firstDataItem = dbssRespModel.data.FirstOrDefault();
                                if (firstDataItem != null)
                                {
                                    customerAddress = firstDataItem.attributes;
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("No data found. API url: " + SettingsValues.GetDbssBaseUrl() + apiUrl);
                    }
                }
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
                log.message = error.error_description ?? String.Empty;
            }
            finally
            {
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = retailerId;
                log.method_name = "GetCustomerAddress";
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }
                await _bllLog.RAToDBSSLog(log);
            }
            return customerAddress;
        }
        #endregion


        #region Customer-Update

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("CustomerInfoUpdateV3")]
        public async Task<IActionResult> CustomerInfoUpdateV3([FromBody][Bind("alt_msisdn,customer_id,customer_name,district_name,division_name,email,flat_number,gender,house_number,mobile_number,postal_code,quality_control_id,reject_reason,rejection_date,retailer_id,right_id,road_number,session_token,thana_name,village")] RACustomerInfoUpdateRequest model)
        {
            var raResp = new RACommonResponseRevamp();
            string apiUrl = string.Empty;
            string txtResp = string.Empty;

            var log = new BIAToDBSSLog();

            try
            {
                string secreteKey = SettingsValues.GetJWTSequrityKey();
                var tokenService = new TokenValidationService(secreteKey);
                var security = tokenService.ValidateToken(model.session_token);

                if (security == null || !security.IsVallid)
                {
                    throw new Exception(security?.Message ?? "Invalid token.");
                }

                if (!string.Equals(model.retailer_id, security.UserName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception(SettingsValues.GetSessionMessage());
                }

                string prov_id = security.LoginProviderId;

                var rAParse = new BLLRAToDBSSParse();
                var resRootObj = rAParse.CustomerInfoReqParsing(model);

                apiUrl = string.Format(PatchAPICollection.CustomerInfoUpdate, model.customer_id);

                log.req_blob = _blJson.GetGenericJsonData(resRootObj);
                log.req_time = DateTime.Now;

                var dbssResp = await _apiReq.HttpPatchRequest(resRootObj, apiUrl, "CustomerInfoUpdateV3");

                log.res_time = DateTime.Now;
                txtResp = dbssResp?.ToString() ?? string.Empty;
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssResp != null)
                {
                    log.is_success = 1;

                    var dbssRespModel = JsonConvert.DeserializeObject<CustomerUpdateRespRootobject>(txtResp);

                    if (dbssRespModel?.data != null)
                    {
                        var customerUpdateRAResp = _dbssToRaParse.CustomerUpdateRespParsing(dbssRespModel) ?? new RACommonResponse();

                        if (!customerUpdateRAResp.result)
                        {
                            return Ok(new RACommonResponseRevamp
                            {
                                isError = true,
                                message = "Customer info updated failed!",
                                data = new Datas
                                {
                                    isEsim = 0,
                                    request_id = "0"
                                }
                            });
                        }

                        var qcStatusUpdateRAResp = await QCStatusUpdate(model.quality_control_id ?? "", model.retailer_id ?? "", model.mobile_number ?? "") ?? new RACommonResponse();

                        if (qcStatusUpdateRAResp.result)
                        {
                            return Ok(new RACommonResponseRevamp
                            {
                                isError = false,
                                message = "Customer updated successfully!",
                                data = new Datas
                                {
                                    isEsim = 0,
                                    request_id = "0"
                                }
                            });
                        }
                        else
                        {
                            return Ok(new RACommonResponseRevamp
                            {
                                isError = true,
                                message = MessageCollection.QCStatusUpdateFailed,
                                data = new Datas
                                {
                                    isEsim = 0,
                                    request_id = "0"
                                }
                            });
                        }
                    }
                    else
                    {
                        raResp = new RACommonResponseRevamp
                        {
                            isError = true,
                            message = MessageCollection.NoDataFound,
                            data = new Datas
                            {
                                isEsim = 0,
                                request_id = "0"
                            }
                        };
                    }
                }
                else
                {
                    raResp = new RACommonResponseRevamp
                    {
                        isError = true,
                        message = "No response got from DBSS API!",
                        data = new Datas
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    };
                }

                return Ok(raResp);
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
                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                log.is_success = 0;
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_custom_msg ?? error.error_description;

                raResp.isError = true;
                raResp.message = string.IsNullOrEmpty(error.error_custom_msg)
                    ? error.error_description
                    : error.error_custom_msg;

                return Ok(raResp);
            }
            finally
            {
                log.msisdn = model.mobile_number != null ? _bllLog.FormatMSISDN(model.mobile_number) : "";
                log.integration_point_from = (decimal)IntegrationPoints.BI;
                log.integration_point_to = (decimal)IntegrationPoints.BSS;
                log.user_id = model.retailer_id ?? "";
                log.method_name = nameof(CustomerInfoUpdateV3);

                await _bllLog.RAToDBSSLog(log);
            }
        }

        #endregion


        #region QC Status Update 
        //public async Task<RACommonResponse> QCStatusUpdate(string quality_control_id, string retailer_id, string msisdn)
        //{
        //    RACommonResponse raResp = new RACommonResponse();
        //    string? apiUrl = "", txtResp = "";
        //    object dbssResp = new object();
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    try
        //    {
        //        BLLRAToDBSSParse rAParse = new BLLRAToDBSSParse();

        //        var reqRootObj = rAParse.QCStatusUpdateReqParsing(quality_control_id, retailer_id);

        //        apiUrl = String.Format(PatchAPICollection.QCStatusUpdate);

        //        log.req_blob = _blJson.GetGenericJsonData(reqRootObj);
        //        log.req_time = DateTime.Now;

        //        dbssResp = await _apiReq.HttpPatchRequest(reqRootObj, apiUrl, "QCStatusUpdate");

        //        log.res_time = DateTime.Now;
        //        log.res_blob = _blJson.GetGenericJsonData(dbssResp);


        //        txtResp = Convert.ToString(dbssResp);

        //        if (dbssResp != null)
        //        {
        //            log.is_success = 1;

        //            var dbssRespModel = JsonConvert.DeserializeObject<QCStatusResponseRootobject>(dbssResp.ToString());
        //            if (dbssRespModel != null)
        //            {
        //                if (dbssRespModel.data != null)
        //                {
        //                    if (dbssRespModel.data.Count > 0)
        //                    {
        //                        raResp = _dbssToRaParse.QCUpdateRespParsing(dbssRespModel) ?? new RACommonResponse();
        //                    }
        //                    else
        //                    {
        //                        raResp = new RACommonResponse()
        //                        {
        //                            result = false,
        //                            message = MessageCollection.QCStatusUpdateFailed
        //                        };
        //                    }
        //                }
        //                else
        //                {
        //                    raResp = new RACommonResponse()
        //                    {
        //                        result = false,
        //                        message = MessageCollection.QCStatusUpdateFailed
        //                    };
        //                }
        //            }
        //            else
        //            {
        //                raResp = new RACommonResponse()
        //                {
        //                    result = false,
        //                    message = MessageCollection.QCStatusUpdateFailed
        //                };
        //            }
        //        }
        //        else
        //        {
        //            raResp = new RACommonResponse()
        //            {
        //                result = false,
        //                message = MessageCollection.QCStatusUpdateFailed
        //            };
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        log.res_time = DateTime.Now; 
        //        ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        log.res_blob = _blJson.GetGenericJsonData(error);

        //        log.is_success = 0;
        //        log.error_code = error.error_code ?? String.Empty;
        //        log.error_source = error.error_source ?? String.Empty;
        //        log.message = error.error_description ?? String.Empty;

        //        raResp = new RACommonResponse()
        //        {
        //            result = false,
        //            message = ex.InnerException.Message
        //        };
        //    }
        //    finally
        //    {
        //        log.msisdn = msisdn;
        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.BI);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
        //        log.user_id = retailer_id;
        //        log.method_name = "QCStatusUpdate";
        //        string resStr = string.Empty;
        //        if (txtResp != null)
        //        {
        //            resStr = txtResp.ToString();
        //        }

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //    return raResp;
        //}

        public async Task<RACommonResponse> QCStatusUpdate(string quality_control_id, string retailer_id, string msisdn)
        {
            var raResp = new RACommonResponse();
            string apiUrl = string.Empty;
            string txtResp = string.Empty;
            object? dbssResp = null;
            var log = new BIAToDBSSLog();

            try
            {
                var rAParse = new BLLRAToDBSSParse();

                var reqRootObj = rAParse.QCStatusUpdateReqParsing(quality_control_id, retailer_id);
                apiUrl = PatchAPICollection.QCStatusUpdate;

                log.req_blob = _blJson.GetGenericJsonData(reqRootObj);
                log.req_time = DateTime.Now;

                dbssResp = await _apiReq.HttpPatchRequest(reqRootObj, apiUrl, "QCStatusUpdate");

                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                txtResp = dbssResp?.ToString() ?? string.Empty;

                if (dbssResp != null)
                {
                    log.is_success = 1;

                    var dbssRespModel = JsonConvert.DeserializeObject<QCStatusResponseRootobject>(txtResp);

                    if (dbssRespModel?.data?.Count > 0)
                    {
                        raResp = _dbssToRaParse.QCUpdateRespParsing(dbssRespModel) ?? new RACommonResponse
                        {
                            result = false,
                            message = MessageCollection.QCStatusUpdateFailed
                        };
                    }
                    else
                    {
                        raResp = new RACommonResponse
                        {
                            result = false,
                            message = MessageCollection.QCStatusUpdateFailed
                        };
                    }
                }
                else
                {
                    raResp = new RACommonResponse
                    {
                        result = false,
                        message = MessageCollection.QCStatusUpdateFailed
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                log.res_time = DateTime.Now;

                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                log.is_success = 0;
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_description ?? string.Empty;

                raResp = new RACommonResponse
                {
                    result = false,
                    message = ex.InnerException?.Message ?? ex.Message
                };
            }
            finally
            {
                log.msisdn = msisdn;
                log.integration_point_from = (decimal)IntegrationPoints.BI;
                log.integration_point_to = (decimal)IntegrationPoints.BSS;
                log.user_id = retailer_id;
                log.method_name = nameof(QCStatusUpdate);

                await _bllLog.RAToDBSSLog(log);
            }

            return raResp;
        }

        #endregion


        #region Get-Activity-Log-Data
        /// <summary>
        /// Get ACTIVITY LOG/ PENDING LIST/ ACTIVATION LIST by and type.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //[ResponseType(typeof(ActivityLogResponse))]
        //[HttpPost]
        //[Route("ActivityLogData")]
        //public async Task<IActionResult> GetActivityLogData([FromBody][Bind("activity_type_id,right_id,session_token")] RAOrderActivityRequest model)
        //{
        //    ActivityLogResponse activityLogData = new ActivityLogResponse();
        //    try
        //    {
        //        if (!await _apiManager.ValidUserBySecurityTokenV2(model.session_token))
        //            throw new Exception(MessageCollection.InvalidSecurityToken);

        //        string user_id = _bllCommon.GetUserNameFromSessionToken(model.session_token);

        //        activityLogData = await _bllCommon.GetActivityLogData(model.activity_type_id, user_id);

        //        return Ok(activityLogData);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        try
        //        {
        //            ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
        //            });
        //        }
        //        catch (Exception ex2)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = ex2.Message
        //            });
        //        }
        //    }
        //}

        /// <summary>
        /// Get ACTIVITY LOG/ PENDING LIST/ ACTIVATION LIST by and type.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //[ResponseType(typeof(ActivityLogResponse))]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ActivityLogDataV2")]
        public async Task<IActionResult> GetActivityLogDataV2([FromBody][Bind("activity_type_id,right_id,session_token")] RAOrderActivityRequest model)
        {
            ActivityLogResponse activityLogData = new ActivityLogResponse();
            try
            {
                if (!await _apiManager.ValidUserBySecurityTokenV2(model.session_token))
                    throw new Exception(MessageCollection.InvalidSecurityToken);

                string id = _bllCommon.GetUserNameFromSessionTokenV2(model.session_token);

                activityLogData = await _bllCommon.GetActivityLogDataV2(model.activity_type_id, id);

                return Ok(activityLogData);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    result = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    return Ok(new RACommonResponse()
                    {
                        result = false,
                        message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                    });
                }
                catch (Exception ex2)
                {
                    return Ok(new RACommonResponse()
                    {
                        result = false,
                        message = ex2.Message
                    });
                }
            }
        }

        /// <summary>
        /// Get ACTIVITY LOG/ PENDING LIST/ ACTIVATION LIST by and type.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //[ResponseType(typeof(ActivityLogResponse))]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ActivityLogDataV3")]
        public async Task<IActionResult> GetActivityLogDataV3([FromBody][Bind("activity_type_id,right_id,session_token")] RAOrderActivityRequest model)
        {
            ActivityLogResponseRevamp activityLogData = new ActivityLogResponseRevamp();
            string id = string.Empty;
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        prov_id = security.LoginProviderId;
                        id = security.UserName;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                activityLogData = await _bllCommon.GetActivityLogDataV3(model.activity_type_id, id);

                return Ok(activityLogData);
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

                return Ok(new ActivityLogResponseRevamp()
                {
                    isError = true,
                    message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new List<VMActivityLogRevamp>()
                    {

                    }
                });
            }
        }


        #endregion


        #region Get-Order-Info-By-TokenId

        //[HttpPost]
        //[ValidateModel]
        //[Route("OrderInfoByTokenId")]
        //public async Task<IActionResult> GetOrderInfoByTokenId(RAGetCustomerInfoByTokenNoRequest model)
        //{
        //    try
        //    {
        //        if (!await _apiManager.ValidUserBySecurityTokenV2(model.session_token))
        //            throw new Exception(MessageCollection.InvalidSecurityToken);

        //        var result = await _bllOrder.GetOrderInfoByTokenNo(model.token_id);


        //        if (result.result == false)
        //        {
        //            return Ok(result);
        //        }
        //        else
        //        {
        //            return Ok(result);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

        //        return Ok(new RACommonResponse()
        //        {
        //            result = false,
        //            message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
        //        });
        //    }
        //}


        //[HttpPost]
        ////[ValidateModel]
        //[Route("OrderInfoByTokenIdV2")]
        //public async Task<IActionResult> GetOrderInfoByTokenIdV2(RAGetCustomerInfoByTokenNoRequest model)
        //{
        //    try
        //    {
        //        if (!await _apiManager.ValidUserBySecurityTokenV2(model.session_token))
        //            throw new Exception(MessageCollection.InvalidSecurityToken);


        //        var result = await _bllOrder.GetOrderInfoByTokenNo(model.token_id);


        //        if (result.result == false)
        //        {
        //            return Ok(result);
        //        }
        //        else
        //        {
        //            return Ok(result);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        return Ok(new RACommonResponse()
        //        {
        //            result = false,
        //            message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
        //        });
        //    }
        //}

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("OrderInfoByTokenIdV3")]
        public async Task<IActionResult> GetOrderInfoByTokenIdV3([FromBody][Bind("right_id,session_token,token_id")] RAGetCustomerInfoByTokenNoRequest model)
        {
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                var result = await _bllOrder.GetOrderInfoByTokenNoV2(model.token_id);

                return Ok(result);
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    data = new Datas()
                    {
                        isEsim = 0,
                        request_id = "0"
                    }
                });
            }
        }
        #endregion


        #region MSISDN validation Paired 
        /// <summary>
        /// This API is used for MSISDN validation for paired MSISDN.
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        //[Authorize]
        //[ResponseType(typeof(PaiedMSISDNCheckResponse))]
        //[HttpPost]
        //[Route("ValidatePairedMSISDNV1")]
        //public async Task<IActionResult> ValidatePairedMSISDNV2([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category")] PairedMSISDNCheckRequest msisdnCheckReqest)
        //{
        //    PaiedMSISDNCheckResponse raRespModel = new PaiedMSISDNCheckResponse();
        //    string? apiUrl = "", txtResp = "";
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    try
        //    {
        //        if (!await _apiManager.ValidUserBySecurityTokenV2(msisdnCheckReqest.session_token))
        //            throw new Exception(MessageCollection.InvalidSecurityToken);

        //        if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
        //        {
        //            msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
        //        }

        //        string a = GetAPICollection.PairedMSISDNValidation;


        //        apiUrl = String.Format(GetAPICollection.PairedMSISDNValidation, msisdnCheckReqest.mobile_number);


        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl);
        //        log.req_time = DateTime.Now;
        //        var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidatePairedMSISDNV2");
        //        log.res_time = DateTime.Now;
        //        var dbssRespObj = JsonConvert.DeserializeObject<PairedMSISDNValidationResponseRootobject>(dbssResp.ToString());
        //        txtResp = Convert.ToString(dbssResp);

        //        log.res_blob = _blJson.GetGenericJsonData(dbssResp);


        //        if (dbssRespObj.data == null)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = "DBSS Error: " + MessageCollection.NoDataFound
        //            });
        //        }

        //        log.is_success = 1;

        //        raRespModel = _dbssToRaParse.PairedMSISDNReqParsing2(dbssRespObj);

        //        if (raRespModel.result == false)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = raRespModel.message
        //            });
        //        }

        //        #region SIM category check
        //        string paymentType = await GetPaymentTypeFromGetSubscriptionType(raRespModel.subscription_type_code, msisdnCheckReqest.retailer_id);
        //        if (GetSIMCategoryByPaymentType(paymentType) != msisdnCheckReqest.sim_category)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = String.Format(MessageCollection.SIMCategoryMismatch, msisdnCheckReqest.sim_category == (int)EnumSimCategory.Prepaid ?
        //                                                                               FixedValueCollection.PaymentTypePrepaid : FixedValueCollection.PaymentTypePostpaid)
        //            });
        //        }
        //        #endregion

        //        #region SIM Validation

        //        var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest()
        //        {
        //            center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //            distributor_code = "",
        //            channel_name = msisdnCheckReqest.channel_name,
        //            session_token = msisdnCheckReqest.session_token,
        //            sim_number = raRespModel.sim_number,
        //            retailer_id = msisdnCheckReqest.retailer_id,
        //            product_code = "",
        //            inventory_id = msisdnCheckReqest.inventory_id,
        //            msisdn = msisdnCheckReqest.mobile_number,
        //            purpose_number = msisdnCheckReqest.purpose_number
        //        }, (int)EnumPurposeOfSIMCheck.NewConnection, true, msisdnCheckReqest.sim_category, "");

        //        if (simResp.result == false)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = simResp.message
        //            });
        //        }
        //        #endregion

        //        if (raRespModel.result == true)
        //        {
        //            RACommonResponse raCommon = await _bio.CheckCherishedNumber(msisdnCheckReqest, "ValidatePairedMSISDNV3");

        //            if (raCommon.result == true)
        //            {
        //                raRespModel.result = true;
        //                raRespModel.message = raCommon.message;
        //            }
        //            else
        //            {
        //                raRespModel.result = false;
        //                raRespModel.message = raCommon.message;
        //            }

        //            return Ok(raRespModel);

        //        }
        //        //raRespModel.result = true;
        //        //raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
        //        return Ok(raRespModel);
        //    }            
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        log.res_time = DateTime.Now;
        //        ErrorDescription error = null;
        //        error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        log.is_success = 0;
        //        log.res_blob = _blJson.GetGenericJsonData(error);

        //        raRespModel.result = false;
        //        raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

        //        log.error_code = error.error_code ?? String.Empty;
        //        log.error_source = error.error_source ?? String.Empty;
        //        log.message = error.error_description ?? String.Empty;

        //        return Ok(raRespModel);
        //    }
        //    finally
        //    {
        //        log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);

        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

        //        log.purpose_number = msisdnCheckReqest.purpose_number;
        //        log.user_id = msisdnCheckReqest.retailer_id;
        //        log.method_name = "ValidatePairedMSISDNV2";
        //        string resStr = string.Empty;
        //        if (txtResp != null)
        //        {
        //            resStr = txtResp.ToString();
        //        }
        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //}


        ////[ResponseType(typeof(PaiedMSISDNCheckResponse))]
        //[HttpPost]
        //[Route("ValidatePairedMSISDNV2")]
        //[ValidatePairedMSISDNValidatorModel]
        //public async Task<IActionResult> ValidatePairedMSISDNV3([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category")] PairedMSISDNCheckRequest msisdnCheckReqest)
        //{
        //    PaiedMSISDNCheckResponse raRespModel = new PaiedMSISDNCheckResponse();
        //    string apiUrl = "", txtResp = "";
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    try
        //    {
        //        if (!await _apiManager.ValidUserBySecurityTokenV2(msisdnCheckReqest.session_token))
        //            throw new Exception(MessageCollection.InvalidSecurityToken);

        //        if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
        //        {
        //            msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
        //        }

        //        string a = GetAPICollection.PairedMSISDNValidation;


        //        apiUrl = String.Format(GetAPICollection.PairedMSISDNValidation, msisdnCheckReqest.mobile_number);


        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl);
        //        log.req_time = DateTime.Now;
        //        var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidatePairedMSISDNV3");
        //        log.res_time = DateTime.Now;
        //        var dbssRespObj = JsonConvert.DeserializeObject<PairedMSISDNValidationResponseRootobject>(dbssResp.ToString());
        //        txtResp = Convert.ToString(dbssResp);

        //        log.res_blob = _blJson.GetGenericJsonData(dbssResp);

        //        if (dbssRespObj == null)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = "DBSS Error: " + MessageCollection.NoDataFound
        //            });
        //        }
        //        if (dbssRespObj.data == null)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = "DBSS Error: " + MessageCollection.NoDataFound
        //            });
        //        }

        //        log.is_success = 1;

        //        raRespModel = _dbssToRaParse.PairedMSISDNReqParsing2(dbssRespObj);

        //        if (raRespModel.result == false)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = raRespModel.message
        //            });
        //        }

        //        #region SIM category check
        //        string paymentType = await GetPaymentTypeFromGetSubscriptionType(raRespModel.subscription_type_code, msisdnCheckReqest.retailer_id);
        //        if (GetSIMCategoryByPaymentType(paymentType) != msisdnCheckReqest.sim_category)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = String.Format(MessageCollection.SIMCategoryMismatch, msisdnCheckReqest.sim_category == (int)EnumSimCategory.Prepaid ?
        //                                                                               FixedValueCollection.PaymentTypePrepaid : FixedValueCollection.PaymentTypePostpaid)
        //            });
        //        }
        //        #endregion

        //        #region SIM Validation

        //        var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest()
        //        {
        //            center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //            distributor_code = "",
        //            channel_name = msisdnCheckReqest.channel_name,
        //            session_token = msisdnCheckReqest.session_token,
        //            sim_number = raRespModel.sim_number,
        //            retailer_id = msisdnCheckReqest.retailer_id,
        //            product_code = "",
        //            inventory_id = msisdnCheckReqest.inventory_id,
        //            msisdn = msisdnCheckReqest.mobile_number,
        //            purpose_number = msisdnCheckReqest.purpose_number
        //        }, (int)EnumPurposeOfSIMCheck.NewConnection, true, msisdnCheckReqest.sim_category, "");

        //        if (simResp.result == false)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = simResp.message
        //            });
        //        }
        //        #endregion

        //        if (raRespModel.result == true)
        //        {
        //            RACommonResponse raCommon = await _bio.CheckCherishedNumber(msisdnCheckReqest, "ValidatePairedMSISDNV3");

        //            if (raCommon.result == true)
        //            {
        //                raRespModel.result = true;
        //                raRespModel.message = raCommon.message;
        //            }
        //            else
        //            {
        //                raRespModel.result = false;
        //                raRespModel.message = raCommon.message;
        //            }

        //            return Ok(raRespModel);
        //        }

        //        //raRespModel.result = true;
        //        //raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
        //        return Ok(raRespModel);
        //    }            
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        log.res_time = DateTime.Now;
        //        ErrorDescription error = null;
        //        error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        log.is_success = 0;
        //        log.res_blob = _blJson.GetGenericJsonData(error);

        //        raRespModel.result = false;

        //        raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

        //        log.error_code = error.error_code ?? String.Empty;
        //        log.error_source = error.error_source ?? String.Empty;
        //        log.message = error.error_description ?? String.Empty;

        //        return Ok(raRespModel);
        //    }
        //    finally
        //    {
        //        log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);

        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

        //        log.purpose_number = msisdnCheckReqest.purpose_number;
        //        log.user_id = msisdnCheckReqest.retailer_id;
        //        log.method_name = "ValidatePairedMSISDNV2";
        //        string resStr = string.Empty;
        //        if (txtResp != null)
        //        {
        //            resStr = txtResp.ToString();
        //        }

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //}
        #endregion


        #region Cherish MSISDN validation Paired V3
        /// <summary>
        /// This API is used for MSISDN validation for paired MSISDN.
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        //[Authorize]
        //[ResponseType(typeof(PaiedMSISDNCheckResponse))]
        //[HttpPost]
        //[Route("ValidatePairedMSISDNV3")]
        //public async Task<IActionResult> ValidatePairedMSISDNV4([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category")] PairedMSISDNCheckRequest msisdnCheckReqest)
        //{
        //    PaiedMSISDNCheckResponse raRespModel = new PaiedMSISDNCheckResponse();
        //    string? apiUrl = "", txtResp = "";
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    try
        //    {
        //        if (!await _apiManager.ValidUserBySecurityTokenV2(msisdnCheckReqest.session_token))
        //            throw new Exception(MessageCollection.InvalidSecurityToken);

        //        if (msisdnCheckReqest.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
        //        {
        //            msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
        //        }

        //        string a = GetAPICollection.PairedMSISDNValidation;


        //        apiUrl = String.Format(GetAPICollection.PairedMSISDNValidation, msisdnCheckReqest.mobile_number);


        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl);
        //        log.req_time = DateTime.Now;
        //        var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidatePairedMSISDNV4");
        //        log.res_time = DateTime.Now;
        //        var dbssRespObj = JsonConvert.DeserializeObject<PairedMSISDNValidationResponseRootobject>(dbssResp.ToString());
        //        txtResp = Convert.ToString(dbssResp);

        //        log.res_blob = _blJson.GetGenericJsonData(dbssResp);

        //        if (dbssRespObj == null)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = "DBSS Error: " + MessageCollection.NoDataFound
        //            });

        //        }
        //        if (dbssRespObj.data == null)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = "DBSS Error: " + MessageCollection.NoDataFound
        //            });
        //        }

        //        log.is_success = 1;

        //        raRespModel = _dbssToRaParse.PairedMSISDNReqParsing2(dbssRespObj);

        //        if (raRespModel.result == false)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = raRespModel.message
        //            });
        //        }

        //        #region SIM category check
        //        string paymentType = await GetPaymentTypeFromGetSubscriptionType(raRespModel.subscription_type_code, msisdnCheckReqest.retailer_id);
        //        if (GetSIMCategoryByPaymentType(paymentType) != msisdnCheckReqest.sim_category)
        //        {
        //            return Ok(new RACommonResponse()
        //            {
        //                result = false,
        //                message = String.Format(MessageCollection.SIMCategoryMismatch, msisdnCheckReqest.sim_category == (int)EnumSimCategory.Prepaid ?
        //                                                                               FixedValueCollection.PaymentTypePrepaid : FixedValueCollection.PaymentTypePostpaid)
        //            });
        //        }
        //        #endregion

        //        #region SIM Validation

        //        var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest()
        //        {
        //            center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
        //            distributor_code = "",
        //            channel_name = msisdnCheckReqest.channel_name,
        //            session_token = msisdnCheckReqest.session_token,
        //            sim_number = raRespModel.sim_number,
        //            retailer_id = msisdnCheckReqest.retailer_id,
        //            product_code = "",
        //            inventory_id = msisdnCheckReqest.inventory_id,
        //            msisdn = msisdnCheckReqest.mobile_number,
        //            purpose_number = msisdnCheckReqest.purpose_number
        //        }, (int)EnumPurposeOfSIMCheck.NewConnection, true, msisdnCheckReqest.sim_category, "");

        //        if (simResp.result == false)
        //        {
        //            return Ok(new RACommonResponse
        //            {
        //                result = false,
        //                message = simResp.message
        //            });
        //        }
        //        #endregion

        //        #region Cherish number check 
        //        if (raRespModel.result == true)
        //        {
        //            RACommonResponse raCommon = await _bio.CheckCherishedNumber(msisdnCheckReqest, "ValidatePairedMSISDNV3");

        //            if (raCommon.result == true)
        //            {
        //                raRespModel.result = true;
        //                raRespModel.message = raCommon.message;
        //            }
        //            else
        //            {
        //                raRespModel.result = false;
        //                raRespModel.message = raCommon.message;
        //            }

        //            return Ok(raRespModel);

        //        }
        //        #endregion

        //        //raRespModel.result = true;
        //        //raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
        //        return Ok(raRespModel);
        //    }            
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        ErrorDescription? error = null;
        //        error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        log.res_blob = _blJson.GetGenericJsonData(error);                

        //        log.res_time = DateTime.Now;
        //        log.is_success = 0;
        //        raRespModel.result = false;

        //        raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

        //        log.error_code = error.error_code ?? String.Empty;
        //        log.error_source = error.error_source ?? String.Empty;
        //        log.message = error.error_description ?? String.Empty;

        //        return Ok(raRespModel);
        //    }
        //    finally
        //    {
        //        log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);

        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

        //        log.purpose_number = msisdnCheckReqest.purpose_number;
        //        log.user_id = msisdnCheckReqest.retailer_id;
        //        log.method_name = "ValidatePairedMSISDNV3";
        //        string resStr = string.Empty;
        //        if (txtResp != null)
        //        {
        //            resStr = txtResp.ToString();
        //        }

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //}

        /// <summary>
        /// This API is used for MSISDN validation for paired MSISDN.
        /// </summary>
        /// <param name="msisdnCheckReqest">Mobile number</param>
        /// <returns>Success/ Failure</returns>
        //[Authorize]
        //[ResponseType(typeof(PaiedMSISDNCheckResponse))]

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ValidatePairedMSISDNV2")]
        [ValidatePairedMSISDNValidatorModel]
        public async Task<IActionResult> ValidatePairedMSISDNV2([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category")] PairedMSISDNCheckRequest msisdnCheckReqest)
        {
            PaiedMSISDNCheckResponseDataRev raRespModel = new PaiedMSISDNCheckResponseDataRev();
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
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

                string a = GetAPICollection.PairedMSISDNValidation;

                apiUrl = String.Format(GetAPICollection.PairedMSISDNValidation, msisdnCheckReqest.mobile_number);


                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidatePairedMSISDNV2");
                log.res_time = DateTime.Now;
                var dbssRespObj = JsonConvert.DeserializeObject<PairedMSISDNValidationResponseRootobject>(dbssResp.ToString());
                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssRespObj == null)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "DBSS Error: " + MessageCollection.NoDataFound,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }
                if (dbssRespObj.data == null)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "DBSS Error: " + MessageCollection.NoDataFound,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }

                log.is_success = 1;

                raRespModel = _dbssToRaParse.PairedMSISDNReqParsingV3(dbssRespObj);

                if (raRespModel.isError == true)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = raRespModel.message,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }

                #region SIM category check
                string paymentType = await GetPaymentTypeFromGetSubscriptionType(raRespModel.data.subscription_type_code, msisdnCheckReqest.retailer_id);
                if (GetSIMCategoryByPaymentType(paymentType) != msisdnCheckReqest.sim_category)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = String.Format(MessageCollection.SIMCategoryMismatch, msisdnCheckReqest.sim_category == (int)EnumSimCategory.Prepaid ?
                                                                                       FixedValueCollection.PaymentTypePrepaid : FixedValueCollection.PaymentTypePostpaid),
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }
                #endregion

                #region SIM Validation

                var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = raRespModel.data.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, true, msisdnCheckReqest.sim_category, "");

                if (simResp.result == false)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = simResp.message,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }
                #endregion

                #region Cherish number check 
                if (raRespModel.isError == false)
                {
                    RACommonResponse raCommon = await _bio.CheckCherishedNumber(msisdnCheckReqest, "ValidatePairedMSISDNV3");

                    if (raCommon.result == true)
                    {
                        raRespModel.isError = false;
                        raRespModel.message = raCommon.message;
                        raRespModel.data = new PaiedMSISDNCheckResponseRev()
                        {
                            sim_number = raRespModel.data.sim_number,
                            subscription_type_code = raRespModel.data.subscription_type_code,
                            imsi = raRespModel.data.imsi
                        };
                    }
                    else
                    {
                        raRespModel.isError = true;
                        raRespModel.message = raCommon.message;
                        raRespModel.data = new PaiedMSISDNCheckResponseRev();
                    }

                    return Ok(raRespModel);
                }
                #endregion

                return Ok(raRespModel);
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
                ErrorDescription? error = null;
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_time = DateTime.Now;
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);

                raRespModel.isError = true;

                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return Ok(raRespModel);
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);

                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidatePairedMSISDNV2";
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }

                await _bllLog.RAToDBSSLog(log);
            }
        }


        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ValidatePairedMSISDNV4")]
        [ValidatePairedMSISDNValidatorModel]
        public async Task<IActionResult> ValidatePairedMSISDNV5([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category")] PairedMSISDNCheckRequest msisdnCheckReqest)
        {
            PaiedMSISDNCheckResponseDataRev raRespModel = new PaiedMSISDNCheckResponseDataRev();
            string? apiUrl = "", txtResp = "";
            string dmsSessionToken = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
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

                string a = GetAPICollection.PairedMSISDNValidation;


                apiUrl = String.Format(GetAPICollection.PairedMSISDNValidation, msisdnCheckReqest.mobile_number);


                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidatePairedMSISDNV5");
                log.res_time = DateTime.Now;
                var dbssRespObj = JsonConvert.DeserializeObject<PairedMSISDNValidationResponseRootobject>(dbssResp.ToString());
                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssRespObj == null)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "DBSS Error: " + MessageCollection.NoDataFound,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }
                if (dbssRespObj.data == null)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "DBSS Error: " + MessageCollection.NoDataFound,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }

                log.is_success = 1;

                raRespModel = _dbssToRaParse.PairedMSISDNReqParsingV3(dbssRespObj);

                if (raRespModel.isError == true)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = raRespModel.message,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }

                #region SIM category check
                string paymentType = await GetPaymentTypeFromGetSubscriptionType(raRespModel.data.subscription_type_code, msisdnCheckReqest.retailer_id);
                if (GetSIMCategoryByPaymentType(paymentType) != msisdnCheckReqest.sim_category)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = String.Format(MessageCollection.SIMCategoryMismatch, msisdnCheckReqest.sim_category == (int)EnumSimCategory.Prepaid ?
                                                                                       FixedValueCollection.PaymentTypePrepaid : FixedValueCollection.PaymentTypePostpaid),
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }
                #endregion

                #region SIM Validation

                var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = raRespModel.data.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, true, msisdnCheckReqest.sim_category, "");

                if (simResp.result == false)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = simResp.message,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }
                #endregion

                #region Cherish number check 
                if (raRespModel.isError == false)
                {
                    RACommonResponse raCommon = await _bio.CheckCherishedNumber(msisdnCheckReqest, "ValidatePairedMSISDNV3");

                    if (raCommon.result == true)
                    {
                        raRespModel.isError = false;
                        raRespModel.message = raCommon.message;
                        raRespModel.data = new PaiedMSISDNCheckResponseRev()
                        {
                            sim_number = raRespModel.data.sim_number,
                            subscription_type_code = raRespModel.data.subscription_type_code,
                            imsi = raRespModel.data.imsi
                        };
                    }
                    else
                    {
                        raRespModel.isError = true;
                        raRespModel.message = raCommon.message;
                        raRespModel.data = new PaiedMSISDNCheckResponseRev();
                    }

                    return Ok(raRespModel);
                }
                #endregion

                return Ok(raRespModel);
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
                ErrorDescription? error = null;
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_time = DateTime.Now;
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);

                raRespModel.isError = true;

                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return Ok(raRespModel);
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);

                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidatePairedMSISDNV4";
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [Route("ValidatePairedMSISDNV5")]
        public async Task<IActionResult> ValidatePairedMSISDNV6([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category")] PairedMSISDNCheckRequest msisdnCheckReqest)
        {
            PaiedMSISDNCheckResponseDataRevV1 raRespModel = new PaiedMSISDNCheckResponseDataRevV1();
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            string cherish_category_config = string.Empty;
            string category_config = String.Empty;
            string[] cofigValue = null;
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
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

                string a = GetAPICollection.PairedMSISDNValidation;

                apiUrl = String.Format(GetAPICollection.PairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidatePairedMSISDNV6");

                log.res_time = DateTime.Now;
                var dbssRespObj = JsonConvert.DeserializeObject<PairedMSISDNValidationResponseRootobject>(dbssResp.ToString());
                txtResp = Convert.ToString(dbssResp);

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                if (dbssRespObj == null)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "MSISDN: " + MessageCollection.NoDataFound,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });

                }
                if (dbssRespObj.data == null)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "MSISDN: " + MessageCollection.NoDataFound,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }

                log.is_success = 1;

                raRespModel = _dbssToRaParse.PairedMSISDNReqParsingV4(dbssRespObj);

                if (raRespModel.isError == true)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = raRespModel.message,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }
                else
                {
                    cherish_category_config = SettingsValues.GetCherishCategory();

                    if (cherish_category_config.Contains(","))
                    {
                        cofigValue = cherish_category_config.Split(',');
                    }
                    else
                    {
                        cofigValue = cherish_category_config.Split(' ');
                    }

                    if (cofigValue.Any(x => x == raRespModel.data.number_category))
                    {
                        var category = cofigValue.Where(x => x.Equals(raRespModel.data.number_category)).FirstOrDefault();
                        if (category != null)
                        {
                            var catInfo = await _bllCommon.GetDesiredCategoryMessage(category, msisdnCheckReqest.channel_name);
                            raRespModel.data = new PaiedMSISDNCheckResponseRevV1()
                            {
                                sim_number = raRespModel.data.sim_number,
                                subscription_type_code = raRespModel.data.subscription_type_code,
                                imsi = raRespModel.data.imsi,
                                message = catInfo != null ? catInfo.message : "No amount is configured for " + category + " category",
                                isDesiredCategory = catInfo != null ? true : false,
                                category = raRespModel.data.number_category
                            };
                        }
                    }
                    else
                    {
                        RACommonResponse raCommon = await _bio.CheckCherishedNumber(msisdnCheckReqest, "ValidatePairedMSISDNV3");

                        if (raCommon.result == true)
                        {
                            raRespModel.isError = false;
                            raRespModel.message = raCommon.message;
                            raRespModel.data = new PaiedMSISDNCheckResponseRevV1()
                            {
                                sim_number = raRespModel.data.sim_number,
                                subscription_type_code = raRespModel.data.subscription_type_code,
                                imsi = raRespModel.data.imsi,
                                message = "",
                                isDesiredCategory = false,
                                category = raRespModel.data.number_category
                            };
                        }
                        else
                        {
                            raRespModel.isError = true;
                            raRespModel.message = raCommon.message;
                            raRespModel.data = new PaiedMSISDNCheckResponseRevV1();
                            return Ok(raRespModel);
                        }
                    }
                }

                #region SIM category check
                string paymentType = await GetPaymentTypeFromGetSubscriptionType(raRespModel.data.subscription_type_code, msisdnCheckReqest.retailer_id);
                if (GetSIMCategoryByPaymentType(paymentType) != msisdnCheckReqest.sim_category)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = String.Format(MessageCollection.SIMCategoryMismatch, msisdnCheckReqest.sim_category == (int)EnumSimCategory.Prepaid ?
                                                                                       FixedValueCollection.PaymentTypePrepaid : FixedValueCollection.PaymentTypePostpaid),
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }
                #endregion

                #region SIM Validation

                var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = raRespModel.data.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, true, msisdnCheckReqest.sim_category, "");

                if (simResp.result == false)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = simResp.message,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }
                #endregion                
                return Ok(raRespModel);
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
                ErrorDescription? error = null;

                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                log.res_time = DateTime.Now;
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(ex.InnerException?.Message);

                try
                {
                    raRespModel.isError = true;

                    raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;


                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    return Ok(raRespModel);
                }
                catch (Exception)
                {
                    raRespModel.isError = true;
                    raRespModel.message = ex.Message;

                    return Ok(raRespModel);
                }
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);

                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidatePairedMSISDNV6";
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }

                await _bllLog.RAToDBSSLog(log);
            }
        }


        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ValidatePairedMSISDNV6")]
        [ValidatePairedMSISDNValidatorModel]
        public async Task<IActionResult> ValidatePairedMSISDNV7([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category")] PairedMSISDNCheckRequest msisdnCheckReqest)
        {
            PaiedMSISDNCheckResponseDataRev raRespModel = new();
            string? apiUrl = "", txtResp = "";
            string dmsSessionToken = string.Empty;
            BIAToDBSSLog log = new();
            ICCDetailsResponse? iccData = new ICCDetailsResponse();

            try
            {
                #region Token Validation
                var secreteKey = SettingsValues.GetJWTSequrityKey();
                var tokenValidator = new TokenValidationService(secreteKey);
                var security = tokenValidator.ValidateToken(msisdnCheckReqest.session_token);

                if (security == null || !security.IsVallid)
                    throw new Exception(security?.Message ?? "Invalid token");

                if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                    throw new Exception(SettingsValues.GetSessionMessage());

                var prov_id = security.LoginProviderId;
                #endregion

                #region Normalize MSISDN
                if (!msisdnCheckReqest.mobile_number.StartsWith(FixedValueCollection.MSISDNCountryCode))
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }
                #endregion

                #region DBSS Call
                apiUrl = string.Format(GetAPICollection.PairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidatePairedMSISDNV5");
                log.res_time = DateTime.Now;

                txtResp = dbssResp?.ToString() ?? string.Empty;
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                var dbssRespObj = JsonConvert.DeserializeObject<PairedMSISDNValidationResponseRootobject>(txtResp);

                if (dbssRespObj?.data == null)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = "DBSS Error: " + MessageCollection.NoDataFound,
                        data = new Datas { isEsim = 0, request_id = "0" }
                    });
                }

                log.is_success = 1;
                raRespModel = _dbssToRaParse.PairedMSISDNReqParsingV3(dbssRespObj);

                if (raRespModel.isError)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = raRespModel.message,
                        data = new Datas { isEsim = 0, request_id = "0" }
                    });
                }
                #endregion

                #region SIM Category Check
                var paymentType = await GetPaymentTypeFromGetSubscriptionType(raRespModel.data.subscription_type_code, msisdnCheckReqest.retailer_id);
                if (GetSIMCategoryByPaymentType(paymentType) != msisdnCheckReqest.sim_category)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = string.Format(MessageCollection.SIMCategoryMismatch,
                                msisdnCheckReqest.sim_category == (int)EnumSimCategory.Prepaid ?
                                FixedValueCollection.PaymentTypePrepaid : FixedValueCollection.PaymentTypePostpaid),
                        data = new Datas { isEsim = 0, request_id = "0" }
                    });
                }
                #endregion

                #region SIM Validation
                var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest
                {
                    center_code = msisdnCheckReqest.center_code ?? "",
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = raRespModel.data.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, true, msisdnCheckReqest.sim_category, "");

                if (!simResp.result)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = simResp.message,
                        data = new Datas { isEsim = 0, request_id = "0" }
                    });
                }
                else
                {
                    #region ICC checking from DMS 
                    ICCDetailsRequestModel model = new ICCDetailsRequestModel()
                    {
                        center_code = msisdnCheckReqest.center_code,
                        icc = raRespModel.data.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        mobile_number = msisdnCheckReqest.mobile_number
                    };

                    iccData = await _apiManager.CheckICCfromDMS(model);

                    if (iccData != null && iccData.result)
                    {
                        raRespModel.isError = false;
                        raRespModel.data.offer_name = iccData.offer_name ?? string.Empty;
                        raRespModel.data.product_name = iccData.product_name ?? string.Empty;
                        raRespModel.data.details_message = iccData.offer_description ?? string.Empty;
                    }
                    else
                    {
                        raRespModel.isError = true;
                        raRespModel.message = iccData?.message ?? "Unknown error";
                        return Ok(raRespModel);
                    }
                    #endregion
                }
                #endregion

                if (raRespModel.isError == false)
                {
                    var cherishResp = await _bio.CheckCherishedNumber(msisdnCheckReqest, "ValidatePairedMSISDNV3");
                    if (!cherishResp.result)
                    {
                        raRespModel.isError = true;
                        raRespModel.message = cherishResp.message;
                        raRespModel.data = new PaiedMSISDNCheckResponseRev();
                    }
                    else
                    {
                        raRespModel.isError = false;
                        raRespModel.message = cherishResp.message;
                        raRespModel.data = new PaiedMSISDNCheckResponseRev
                        {
                            sim_number = raRespModel.data.sim_number,
                            subscription_type_code = raRespModel.data.subscription_type_code,
                            imsi = raRespModel.data.imsi,
                            offer_name = iccData.offer_name ?? string.Empty,
                            product_name = iccData.product_name ?? string.Empty,
                            details_message = iccData.offer_description ?? string.Empty,
                        };
                    }
                }
                return Ok(raRespModel);
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
                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_time = DateTime.Now;
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);

                raRespModel.isError = true;
                raRespModel.message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_description ?? string.Empty;

                return Ok(raRespModel);
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidatePairedMSISDNV4";
                log.res_blob ??= _blJson.GetGenericJsonData(txtResp);

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ValidatePairedMSISDNV7")]
        [ValidatePairedMSISDNValidatorModel]
        public async Task<IActionResult> ValidatePairedMSISDNV8([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category")] PairedMSISDNCheckRequest msisdnCheckReqest)
        {
            PaiedMSISDNCheckResponseDataRevV1 raRespModel = new();
            string? apiUrl = "", txtResp = "";
            string dmsSessionToken = string.Empty;
            BIAToDBSSLog log = new();
            ICCDetailsResponse? iccData = new ICCDetailsResponse();
            string cherish_category_config = string.Empty;
            string category_config = String.Empty;
            string[] cofigValue = null;

            try
            {
                #region Token Validation
                var secreteKey = SettingsValues.GetJWTSequrityKey();
                var tokenValidator = new TokenValidationService(secreteKey);
                var security = tokenValidator.ValidateToken(msisdnCheckReqest.session_token);

                if (security == null || !security.IsVallid)
                    throw new Exception(security?.Message ?? "Invalid token");

                if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                    throw new Exception(SettingsValues.GetSessionMessage());

                var prov_id = security.LoginProviderId;
                #endregion

                #region Normalize MSISDN
                if (!msisdnCheckReqest.mobile_number.StartsWith(FixedValueCollection.MSISDNCountryCode))
                {
                    msisdnCheckReqest.mobile_number = FixedValueCollection.MSISDNCountryCode + msisdnCheckReqest.mobile_number;
                }
                #endregion

                #region DBSS Call
                apiUrl = string.Format(GetAPICollection.PairedMSISDNValidation, msisdnCheckReqest.mobile_number);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;

                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidatePairedMSISDNV5");

                log.res_time = DateTime.Now;

                txtResp = dbssResp?.ToString() ?? string.Empty;
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                var dbssRespObj = JsonConvert.DeserializeObject<PairedMSISDNValidationResponseRootobject>(txtResp);

                if (dbssRespObj?.data == null)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = "DBSS Error: " + MessageCollection.NoDataFound,
                        data = new Datas { isEsim = 0, request_id = "0" }
                    });
                }

                log.is_success = 1;
                raRespModel = _dbssToRaParse.PairedMSISDNReqParsingV4(dbssRespObj);

                if (raRespModel.isError)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = raRespModel.message,
                        data = new Datas { isEsim = 0, request_id = "0" }
                    });
                }
                else
                {
                    cherish_category_config = SettingsValues.GetCherishCategory();

                    if (cherish_category_config.Contains(","))
                    {
                        cofigValue = cherish_category_config.Split(',');
                    }
                    else
                    {
                        cofigValue = cherish_category_config.Split(' ');
                    }

                    if (cofigValue.Any(x => x == raRespModel.data.number_category))
                    {
                        var category = cofigValue.Where(x => x.Equals(raRespModel.data.number_category)).FirstOrDefault();

                        if (category != null)
                        {
                            var catInfo = await _bllCommon.GetDesiredCategoryMessage(category, msisdnCheckReqest.channel_name);

                            raRespModel.data.message = catInfo != null ? catInfo.message : "No amount is configured for " + category + " category";
                            raRespModel.data.isDesiredCategory = catInfo != null ? true : false;
                        }
                    }
                    else
                    {
                        RACommonResponse raCommon = await _bio.CheckCherishedNumber(msisdnCheckReqest, "ValidatePairedMSISDNV3");

                        if (raCommon.result == true)
                        {
                            raRespModel.isError = false;
                            raRespModel.message = raCommon.message;
                            raRespModel.data.message = "";
                            raRespModel.data.isDesiredCategory = false;
                        }
                        else
                        {
                            raRespModel.isError = true;
                            raRespModel.message = raCommon.message;
                            //raRespModel.data = new PaiedMSISDNCheckResponseRevV1();
                            return Ok(raRespModel);
                        }
                    }
                }
                #endregion

                #region SIM Category Check
                var paymentType = await GetPaymentTypeFromGetSubscriptionType(raRespModel.data.subscription_type_code, msisdnCheckReqest.retailer_id);
                if (GetSIMCategoryByPaymentType(paymentType) != msisdnCheckReqest.sim_category)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = string.Format(MessageCollection.SIMCategoryMismatch,
                                msisdnCheckReqest.sim_category == (int)EnumSimCategory.Prepaid ?
                                FixedValueCollection.PaymentTypePrepaid : FixedValueCollection.PaymentTypePostpaid),
                        data = new Datas { isEsim = 0, request_id = "0" }
                    });
                }
                #endregion

                #region SIM Validation
                var simResp = await _bio.CheckSIMNumber3(new SIMNumberCheckRequest
                {
                    center_code = msisdnCheckReqest.center_code ?? "",
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = raRespModel.data.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, true, msisdnCheckReqest.sim_category, "");

                if (!simResp.result)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = simResp.message,
                        data = new Datas { isEsim = 0, request_id = "0" }
                    });
                }
                else
                {
                    #region ICC checking from DMS 
                    ICCDetailsRequestModel model = new ICCDetailsRequestModel()
                    {
                        center_code = msisdnCheckReqest.center_code,
                        icc = raRespModel.data.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        mobile_number = msisdnCheckReqest.mobile_number
                    };

                    iccData = await _apiManager.CheckICCfromDMS(model);

                    if (iccData != null && iccData.result)
                    {
                        raRespModel.isError = false;
                        raRespModel.data.offer_name = iccData.offer_name ?? string.Empty;
                        raRespModel.data.product_name = iccData.product_name ?? string.Empty;
                        raRespModel.data.details_message = iccData.offer_description ?? string.Empty;
                    }
                    else
                    {
                        raRespModel.isError = true;
                        raRespModel.message = iccData?.message ?? "Unknown error";
                        return Ok(raRespModel);
                    }
                    #endregion
                }
                #endregion

                return Ok(raRespModel);
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
                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_time = DateTime.Now;
                log.is_success = 0;
                log.res_blob = _blJson.GetGenericJsonData(error);

                raRespModel.isError = true;
                raRespModel.message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                log.error_code = error.error_code ?? string.Empty;
                log.error_source = error.error_source ?? string.Empty;
                log.message = error.error_description ?? string.Empty;

                return Ok(raRespModel);
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidatePairedMSISDNV7";
                log.res_blob ??= _blJson.GetGenericJsonData(txtResp);

                await _bllLog.RAToDBSSLog(log);
            }
        }


        #endregion
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ValidatePairedMSISDN_ESIMV2")]
        public async Task<IActionResult> ValidatePairedMSISDN_ESIMV2([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category")] PairedMSISDNCheckRequest msisdnCheckReqest)
        {
            PaiedMSISDNCheckResponseDataRev raRespModel = new PaiedMSISDNCheckResponseDataRev();
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
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

                string a = GetAPICollection.PairedMSISDNValidation;


                apiUrl = String.Format(GetAPICollection.PairedMSISDNValidation, msisdnCheckReqest.mobile_number);


                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidatePairedMSISDN_ESIMV2");
                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);
                var dbssRespObj = JsonConvert.DeserializeObject<PairedMSISDNValidationResponseRootobject>(dbssResp.ToString());
                txtResp = Convert.ToString(dbssResp);

                if (dbssRespObj == null)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "DBSS Error: " + MessageCollection.NoDataFound,
                        data = new Datas()
                        {
                            isEsim = 1,
                            request_id = "0"
                        }
                    });
                }
                if (dbssRespObj.data == null)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "DBSS Error: " + MessageCollection.NoDataFound,
                        data = new Datas()
                        {
                            isEsim = 1,
                            request_id = "0"
                        }
                    });
                }

                log.is_success = 1;

                raRespModel = _dbssToRaParse.PairedMSISDNReqParsingV3(dbssRespObj);

                if (raRespModel.isError == true)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = raRespModel.message,
                        data = new Datas()
                        {
                            isEsim = 1,
                            request_id = "0"
                        }
                    });
                }

                #region SIM category check
                string paymentType = await GetPaymentTypeFromGetSubscriptionType(raRespModel.data.subscription_type_code, msisdnCheckReqest.retailer_id);
                if (GetSIMCategoryByPaymentType(paymentType) != msisdnCheckReqest.sim_category)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = String.Format(MessageCollection.SIMCategoryMismatch, msisdnCheckReqest.sim_category == (int)EnumSimCategory.Prepaid ?
                                                                                       FixedValueCollection.PaymentTypePrepaid : FixedValueCollection.PaymentTypePostpaid),
                        data = new Datas()
                        {
                            isEsim = 1,
                            request_id = "0"
                        }
                    });
                }
                #endregion

                #region SIM Validation

                var simResp = await _bio.CheckSIMNumber4(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = raRespModel.data.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, true, msisdnCheckReqest.sim_category, "");

                if (simResp.result == false)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = simResp.message,
                        data = new Datas()
                        {
                            isEsim = 1,
                            request_id = "0"
                        }
                    });
                }
                #endregion

                if (raRespModel.isError == false)
                {
                    RACommonResponse raCommon = await _bio.CheckCherishedNumber(msisdnCheckReqest, "ValidatePairedMSISDN_ESIM");

                    if (raCommon.result == true)
                    {
                        raRespModel.isError = false;
                        raRespModel.message = raCommon.message;
                        raRespModel.data = new PaiedMSISDNCheckResponseRev()
                        {
                            sim_number = raRespModel.data.sim_number,
                            subscription_type_code = raRespModel.data.subscription_type_code,
                            imsi = raRespModel.data.imsi
                        };
                    }
                    else
                    {
                        raRespModel.isError = true;
                        raRespModel.message = raCommon.message;
                        raRespModel.data = new PaiedMSISDNCheckResponseRev()
                        {
                            sim_number = raRespModel.data.sim_number,
                            subscription_type_code = raRespModel.data.subscription_type_code,
                            imsi = raRespModel.data.imsi
                        };
                    }

                    return Ok(raRespModel);

                }
                //raRespModel.result = true;
                //raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                return Ok(raRespModel);
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
                ErrorDescription? error = null;
                log.is_success = 0;
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                raRespModel.isError = true;

                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return Ok(raRespModel);
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);

                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidatePairedMSISDN_ESIMV2";
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }

                await _bllLog.RAToDBSSLog(log);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ValidatePairedMSISDN_ESIMV3")]
        public async Task<IActionResult> ValidatePairedMSISDN_ESIMV3([FromBody][Bind("center_code,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,session_token,sim_category")] PairedMSISDNCheckRequest msisdnCheckReqest)
        {
            PaiedMSISDNCheckResponseDataRev raRespModel = new PaiedMSISDNCheckResponseDataRev();
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            ICCDetailsResponse? iccData = new ICCDetailsResponse();
            string cherish_category_config = string.Empty;
            string category_config = String.Empty;
            string[] cofigValue = null;
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
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

                string a = GetAPICollection.PairedMSISDNValidation;


                apiUrl = String.Format(GetAPICollection.PairedMSISDNValidation, msisdnCheckReqest.mobile_number);


                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "ValidatePairedMSISDN_ESIMV2");
                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(dbssResp);
                var dbssRespObj = JsonConvert.DeserializeObject<PairedMSISDNValidationResponseRootobject>(dbssResp.ToString());
                txtResp = Convert.ToString(dbssResp);

                if (dbssRespObj == null)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "DBSS Error: " + MessageCollection.NoDataFound,
                        data = new Datas()
                        {
                            isEsim = 1,
                            request_id = "0"
                        }
                    });
                }
                if (dbssRespObj.data == null)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "DBSS Error: " + MessageCollection.NoDataFound,
                        data = new Datas()
                        {
                            isEsim = 1,
                            request_id = "0"
                        }
                    });
                }

                log.is_success = 1;

                raRespModel = _dbssToRaParse.PairedMSISDNReqParsingV3(dbssRespObj);

                if (raRespModel.isError == true)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = raRespModel.message,
                        data = new Datas()
                        {
                            isEsim = 1,
                            request_id = "0"
                        }
                    });
                }
                #region SIM category check
                string paymentType = await GetPaymentTypeFromGetSubscriptionType(raRespModel.data.subscription_type_code, msisdnCheckReqest.retailer_id);
                if (GetSIMCategoryByPaymentType(paymentType) != msisdnCheckReqest.sim_category)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = String.Format(MessageCollection.SIMCategoryMismatch, msisdnCheckReqest.sim_category == (int)EnumSimCategory.Prepaid ?
                                                                                       FixedValueCollection.PaymentTypePrepaid : FixedValueCollection.PaymentTypePostpaid),
                        data = new Datas()
                        {
                            isEsim = 1,
                            request_id = "0"
                        }
                    });
                }
                #endregion

                #region SIM Validation

                var simResp = await _bio.CheckSIMNumber4(new SIMNumberCheckRequest()
                {
                    center_code = String.IsNullOrEmpty(msisdnCheckReqest.center_code) ? "" : msisdnCheckReqest.center_code,
                    distributor_code = "",
                    channel_name = msisdnCheckReqest.channel_name,
                    session_token = msisdnCheckReqest.session_token,
                    sim_number = raRespModel.data.sim_number,
                    retailer_id = msisdnCheckReqest.retailer_id,
                    product_code = "",
                    inventory_id = msisdnCheckReqest.inventory_id,
                    msisdn = msisdnCheckReqest.mobile_number,
                    purpose_number = msisdnCheckReqest.purpose_number ?? ""
                }, (int)EnumPurposeOfSIMCheck.NewConnection, true, msisdnCheckReqest.sim_category, "");

                if (simResp.result == false)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = simResp.message,
                        data = new Datas()
                        {
                            isEsim = 1,
                            request_id = "0"
                        }
                    });
                }
                else
                {
                    #region ICC checking from DMS 
                    ICCDetailsRequestModel model = new ICCDetailsRequestModel()
                    {
                        center_code = msisdnCheckReqest.center_code,
                        icc = raRespModel.data.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        mobile_number = msisdnCheckReqest.mobile_number
                    };

                    iccData = await _apiManager.CheckICCfromDMS(model);

                    if (iccData != null && iccData.result)
                    {
                        raRespModel.isError = false;
                        raRespModel.data.offer_name = iccData.offer_name ?? string.Empty;
                        raRespModel.data.product_name = iccData.product_name ?? string.Empty;
                        raRespModel.data.details_message = iccData.offer_description ?? string.Empty;
                    }
                    else
                    {
                        raRespModel.isError = true;
                        raRespModel.message = iccData?.message ?? "Unknown error";
                        return Ok(raRespModel);
                    }
                    #endregion
                }
                #endregion

                if (raRespModel.isError == false)
                {
                    RACommonResponse raCommon = await _bio.CheckCherishedNumber(msisdnCheckReqest, "ValidatePairedMSISDN_ESIM");

                    if (raCommon.result == true)
                    {
                        raRespModel.isError = false;
                        raRespModel.message = raCommon.message;
                        raRespModel.data.sim_number = raRespModel.data.sim_number;
                        raRespModel.data.subscription_type_code = raRespModel.data.subscription_type_code;
                        raRespModel.data.imsi = raRespModel.data.imsi;
                    }
                    else
                    {
                        raRespModel.isError = true;
                        raRespModel.message = raCommon.message;
                        raRespModel.data.sim_number = raRespModel.data.sim_number;
                        raRespModel.data.subscription_type_code = raRespModel.data.subscription_type_code;
                        raRespModel.data.imsi = raRespModel.data.imsi;
                    }

                    return Ok(raRespModel);

                }

                //raRespModel.result = true;
                //raRespModel.message = MessageCollection.MSISDNandSIMBothValid;
                return Ok(raRespModel);
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
                ErrorDescription? error = null;
                log.is_success = 0;
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);

                raRespModel.isError = true;

                raRespModel.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_description ?? String.Empty;

                return Ok(raRespModel);
            }
            finally
            {
                log.msisdn = _bllLog.FormatMSISDN(msisdnCheckReqest.mobile_number);

                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);

                log.purpose_number = msisdnCheckReqest.purpose_number ?? "";
                log.user_id = msisdnCheckReqest.retailer_id;
                log.method_name = "ValidatePairedMSISDN_ESIMV2";
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }

                await _bllLog.RAToDBSSLog(log);
            }
        }


        #region Get Payment Type (Prepaid/Postpaid) from get subscription type 

        public async Task<string> GetPaymentTypeFromGetSubscriptionType(string subscription_type_code, string retailerName)
        {
            string paymentType = "";
            string? apiUrl = "", txtResp = "";
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                apiUrl = String.Format(GetAPICollection.GetPaymentTypeFromGetSubscriptionType, subscription_type_code);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetPaymentTypeFromGetSubscriptionType");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                if (!dbssResp.HasValues)
                {
                    return paymentType;
                }

                log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                log.is_success = 1;

                paymentType = _dbssToRaParse.PaymentTypeFromSubscripTypeReqParsing(dbssResp);

                if (String.IsNullOrEmpty(paymentType)) throw new Exception(MessageCollection.DataNotFound);

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
                log.message = error.error_description ?? String.Empty;

            }
            finally
            {
                log.method_name = "GetPaymentTypeFromGetSubscriptionType";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = retailerName;
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }

                //Thread logThread = new Thread(() => _bllLog.RAToDBSSLog(log, apiUrl, resStr));
                //logThread.Start();
                await _bllLog.RAToDBSSLog(log);
            }
            return paymentType;
        }
        #endregion


        #region Get-SIM-Category-By-Payment-Type
        private int GetSIMCategoryByPaymentType(string paymentType)
        {
            return paymentType == "prepaid" ? (int)EnumSimCategory.Prepaid : (int)EnumSimCategory.Postpaid;
        }
        #endregion


        #region Check Security Token Valid or Not
        /// <summary>
        /// This API is used for Check Security Token
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("CheckSecurityToken")]
        public async Task<IActionResult> CheckSecurityTokenV1([FromBody][Bind("right_id,session_token")] RACommonRequest model)
        {
            try
            {
                bool result = await _apiManager.ValidUserBySecurityToken(model.session_token);
                return Ok(new RACommonResponse()
                {
                    result = result,
                    message = result == false ? MessageCollection.InvalidSecurityToken : MessageCollection.ValidAccessToken
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                ErrorDescription error;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    return Ok(new RACommonResponse
                    {
                        result = false,
                        message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                    });
                }
                catch (Exception ex2)
                {
                    return Ok(new RACommonResponse
                    {
                        result = false,
                        message = ex2.InnerException?.Message ?? ex2.Message
                    });
                }
            }
        }

        /// <summary>
        /// This API is used for Check Security Token
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("CheckSecurityTokenV2")]
        public async Task<IActionResult> CheckSecurityTokenV2([FromBody][Bind("right_id,session_token")] RACommonRequest model)
        {
            try
            {
                bool result = await _apiManager.ValidUserBySecurityTokenV2(model.session_token);
                return Ok(new RACommonResponse()
                {
                    result = result,
                    message = result == false ? MessageCollection.InvalidSecurityToken : MessageCollection.ValidAccessToken
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                ErrorDescription error;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    return Ok(new RACommonResponse
                    {
                        result = false,
                        message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                    });
                }
                catch (Exception ex2)
                {
                    return Ok(new RACommonResponse
                    {
                        result = false,
                        message = ex2.InnerException?.Message ?? ex2.Message
                    });
                }
            }
        }

        /// <summary>
        /// This API is used for Check Security Token
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("CheckSecurityTokenV3")]
        public async Task<IActionResult> CheckSecurityTokenV3([FromBody][Bind("right_id,session_token")] RACommonRequest model)
        {
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }


                return Ok(new RACommonResponseRevamp
                {
                    isError = false,
                    message = security == null ? "" : security.Message //result == false ? MessageCollection.InvalidSecurityToken : MessageCollection.ValidAccessToken
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
                ErrorDescription error;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                    });
                }
                catch (Exception ex2)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = ex2.InnerException?.Message ?? ex2.Message
                    });
                }
            }
        }

        #endregion

        #region Unpaired MSISDN and SIM serial
        /// <summary>
        /// This API is used to Get Unpaired MSISDN List Type.
        /// </summary>
        /// <param name=""></param>
        /// <returns>Subscription Type List / Failure</returns>
        //[Authorize(Roles = "Retailer")]
        //[HttpPost]
        //[Route("GetUnpairedMSISDNList")]
        //public async Task<IActionResult> GetUnpairedMSISDNList(UnpairedMSISDNListReqModel model)
        //{
        //    List<ReponseData> raRespData = new List<ReponseData>();
        //    UnpairedMSISDNData raResp = new UnpairedMSISDNData();
        //    string apiUrl = string.Empty;
        //    string? txtResp = string.Empty;
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    try
        //    {
        //        if (!await _apiManager.ValidUserBySecurityTokenV2(model.session_token))
        //            throw new Exception(MessageCollection.InvalidSecurityToken);

        //        if (String.IsNullOrEmpty(model.msisdn))
        //        {
        //            //GetUnpairedMSISDNSearchDefaultValue
        //            model.msisdn = await _bllCommon.GetUnpairedMSISDNSearchDefaultValue(model);

        //            if (String.IsNullOrEmpty(model.msisdn))
        //            {
        //                return Ok(raResp);
        //            }
        //            if (model.msisdn.Substring(0, 4) != FixedValueCollection.MSISDNFixedValue)
        //            {
        //                model.msisdn = FixedValueCollection.MSISDNFixedValue + model.msisdn;
        //            }
        //            if (model.msisdn.Substring(0, 1) == "0")
        //            {
        //                model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
        //            }
        //        }
        //        else
        //        {
        //            if (model.msisdn.Substring(0, 4) != FixedValueCollection.MSISDNFixedValue)
        //            {
        //                model.msisdn = FixedValueCollection.MSISDNFixedValue + model.msisdn;
        //            }
        //            if (model.msisdn.Substring(0, 1) == "0")
        //            {
        //                model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
        //            }
        //        }

        //        string channelIdFromConfig = string.Empty;
        //        string[] arrChannelId = null;
        //        string stockIdFromConfig = string.Empty;
        //        string[] arrStockId = null;
        //        string channelId = string.Empty;
        //        int arrIndexChannel = 0;
        //        string stockIdValue = string.Empty;
        //        string stockIdByDefault = string.Empty;
        //        try
        //        {
        //            IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

        //            channelIdFromConfig = configuration.GetSection("AppSettings:ChannelId").Value;
        //            stockIdFromConfig = configuration.GetSection("AppSettings:ChannelStockId").Value;
        //            stockIdByDefault = configuration.GetSection("AppSettings:ChannelStockIdDefault").Value;
        //        }
        //        catch { }

        //        if (channelIdFromConfig.Contains(","))
        //        {
        //            arrChannelId = channelIdFromConfig.Split(',');
        //        }
        //        else
        //        {
        //            arrChannelId = channelIdFromConfig.Split(' ');
        //        }

        //        if (stockIdFromConfig.Contains(","))
        //        {
        //            arrStockId = stockIdFromConfig.Split(',');
        //        }
        //        else
        //        {
        //            arrStockId = stockIdFromConfig.Split(' ');
        //        }

        //        channelId = await _dbssToRaParse.GetStockResponses(model.channel_name);

        //        if (arrChannelId.Contains(channelId))
        //        {
        //            arrIndexChannel = Array.IndexOf(arrChannelId, channelId);
        //            stockIdValue = arrStockId[arrIndexChannel];
        //        }
        //        else
        //        {
        //            stockIdValue = stockIdByDefault;
        //        }

        //        apiUrl = String.Format(UnpairedMSISDNList.GetUnpairedMSISDNList, 1, 10, model.msisdn, stockIdValue);

        //        log.req_blob = _blJson.GetGenericJsonData(apiUrl);
        //        log.req_time = DateTime.Now;
        //        var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetUnpairedMSISDNList");
        //        log.res_time = DateTime.Now;
        //        txtResp = Convert.ToString(dbssResp);

        //        if (dbssResp != null)
        //        {
        //            log.res_blob = _blJson.GetGenericJsonData(dbssResp);

        //            log.is_success = 1;
        //            //var dataBss = JsonConvert.DeserializeObject(dbssResp.ToString());
        //            UnpairedMSISDNRootData? dbssRespModel = JsonConvert.DeserializeObject<UnpairedMSISDNRootData>(dbssResp.ToString());
        //            if (dbssRespModel != null)
        //            {
        //                if (dbssRespModel.data != null)
        //                {
        //                    var result = ((IEnumerable)dbssRespModel.data).Cast<object>().ToList();

        //                    raRespData = _dbssToRaParse.UnpairedMSISDNListDataParsing(result);

        //                    if (raRespData.Count > 0)
        //                    {
        //                        raResp.data = raRespData;
        //                        raResp.result = true;
        //                        raResp.message = MessageCollection.Success;
        //                    }
        //                    else
        //                    {
        //                        raResp.data = raRespData;
        //                        raResp.result = false;
        //                        raResp.message = MessageCollection.NoDataFound;
        //                    }
        //                }
        //                else
        //                {
        //                    raResp.data = raRespData;
        //                    raResp.result = false;
        //                    raResp.message = "DBSS API doesn't contains any Unpaired MSISDN list.";
        //                }
        //            }
        //            else
        //            {
        //                raResp.data = raRespData;
        //                raResp.result = false;
        //                raResp.message = "DBSS API doesn't contains any Unpaired MSISDN list.";
        //            }
        //        }
        //        else
        //        {
        //            raResp.data = raRespData;
        //            raResp.result = false;
        //            raResp.message = "Unable to load data from DBSS API.";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        log.res_time = DateTime.Now;
        //        ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        log.res_blob = _blJson.GetGenericJsonData(error);

        //        log.is_success = 0;
        //        log.error_code = error.error_code ?? String.Empty;
        //        log.error_source = error.error_source ?? String.Empty;
        //        log.message = error.error_description ?? String.Empty;

        //        raResp.data = raRespData;
        //        raResp.result = false;
        //        raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //    }
        //    finally
        //    {
        //        log.method_name = "GetUnpairedMSISDNList";
        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
        //        log.user_id = model.retailer_id;
        //        string resStr = string.Empty;
        //        if (txtResp != null)
        //        {
        //            resStr = txtResp.ToString();
        //        }

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //    return Ok(raResp);
        //}

        /// <summary>
        /// This API is used to Get Unpaired MSISDN List Type.
        /// </summary>
        /// <param name=""></param>
        /// <returns>Subscription Type List / Failure</returns>
        //[Authorize(Roles = "Retailer")] 
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetUnpairedMSISDNListV2")]
        public async Task<IActionResult> GetUnpairedMSISDNListV2([FromBody][Bind("FWA_channel_name,channel_name,is_fwa,msisdn,retailer_id,right_id,session_token")] UnpairedMSISDNListReqModel model)
        {
            List<ReponseDataRev> raRespData = new List<ReponseDataRev>();
            UnpairedMSISDNDataRev raResp = new UnpairedMSISDNDataRev();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (model.is_fwa == 1)
                {
                    model.channel_name = model.FWA_channel_name ?? model.channel_name;
                }

                if (String.IsNullOrEmpty(model.msisdn))
                {
                    //GetUnpairedMSISDNSearchDefaultValue
                    model.msisdn = await _bllCommon.GetUnpairedMSISDNSearchDefaultValue(model);

                    if (String.IsNullOrEmpty(model.msisdn))
                    {
                        return Ok(raResp);
                    }
                    if (model.msisdn.Substring(0, 4) != FixedValueCollection.MSISDNFixedValue)
                    {
                        model.msisdn = FixedValueCollection.MSISDNFixedValue + model.msisdn;
                    }
                    if (model.msisdn.Substring(0, 1) == "0")
                    {
                        model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                    }
                }
                else
                {
                    if (model.msisdn.Substring(0, 4) != FixedValueCollection.MSISDNFixedValue)
                    {
                        model.msisdn = FixedValueCollection.MSISDNFixedValue + model.msisdn;
                    }
                    if (model.msisdn.Substring(0, 1) == "0")
                    {
                        model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                    }
                }

                string channelIdFromConfig = SettingsValues.GetChannelId();
                string stockIdFromConfig = SettingsValues.GetChannelStockId();
                string stockIdByDefault = SettingsValues.GetChannelStockDefault();
                string[] arrChannelId = Array.Empty<string>();
                string[] arrStockId = Array.Empty<string>();
                string channelId = string.Empty;
                int arrIndexChannel = 0;
                string stockIdValue = string.Empty;

                if (channelIdFromConfig.Contains(","))
                {
                    arrChannelId = channelIdFromConfig.Split(',');
                }
                else
                {
                    arrChannelId = channelIdFromConfig.Split(' ');
                }

                if (stockIdFromConfig.Contains(","))
                {
                    arrStockId = stockIdFromConfig.Split(',');
                }
                else
                {
                    arrStockId = stockIdFromConfig.Split(' ');
                }

                channelId = await _dbssToRaParse.GetStockResponses(model.channel_name);

                if (arrChannelId.Contains(channelId))
                {
                    arrIndexChannel = Array.IndexOf(arrChannelId, channelId);
                    stockIdValue = arrStockId[arrIndexChannel];
                }
                else
                {
                    stockIdValue = stockIdByDefault;
                }

                // Safe construction
                var encodedMsisdn = Uri.EscapeDataString(model.msisdn);
                apiUrl = String.Format(UnpairedMSISDNList.GetUnpairedMSISDNList, 1, 10, encodedMsisdn, stockIdValue);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetUnpairedMSISDNListV2");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                if (dbssResp != null)
                {
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    log.is_success = 1;
                    UnpairedMSISDNRootData? dbssRespModel = JsonConvert.DeserializeObject<UnpairedMSISDNRootData>(dbssResp.ToString());
                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel.data != null)
                        {
                            var result = ((IEnumerable)dbssRespModel.data).Cast<object>().ToList();

                            raRespData = _dbssToRaParse.UnpairedMSISDNListDataParsingV2(result);

                            if (raRespData.Count > 0)
                            {
                                raResp.data = raRespData;
                                raResp.isError = false;
                                raResp.message = MessageCollection.Success;
                            }
                            else
                            {
                                raResp.data = raRespData;
                                raResp.isError = true;
                                raResp.message = MessageCollection.NoDataFound;
                            }
                        }
                        else
                        {
                            raResp.data = raRespData;
                            raResp.isError = true;
                            raResp.message = "DBSS API doesn't contains any Unpaired MSISDN list.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "DBSS API doesn't contains any Unpaired MSISDN list.";
                    }
                }
                else
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = "Unable to load data from DBSS API.";
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
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? error.error_description;
                raResp.data = raRespData;
                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
            }
            finally
            {
                log.method_name = "GetUnpairedMSISDNListV2";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }
                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(raResp);
        }

        /// <summary>
        /// This API is used to Get Unpaired SIM from DMS Type.
        /// </summary>
        /// <param name=""></param>
        /// <returns>Subscription Type List / Failure</returns>
        //[Authorize(Roles = "Retailer")]
        //[HttpPost]
        //[Route("GetUnpairedSIMlist")]
        //public async Task<IActionResult> GetUnpairedSIMlist(UnpairedSIMsearchReqModel model)
        //{
        //    List<SIMReponseData> raRespData = new List<SIMReponseData>();
        //    UnpairedSIMData raResp = new UnpairedSIMData();
        //    string apiUrl = string.Empty;
        //    string? txtResp = string.Empty;
        //    BIAToDBSSLog log = new BIAToDBSSLog();
        //    BLLRAToDBSSParse dMSParse = new BLLRAToDBSSParse();

        //    string userName = string.Empty;
        //    string password = string.Empty;
        //    string product_code_prepaid = string.Empty;
        //    string product_code_postpaid = string.Empty;
        //    string product_category_prepaid = string.Empty;
        //    string product_category_postpaid = string.Empty;
        //    string sim_s = string.Empty;
        //    string product_category_simReplacment = string.Empty;

        //    try
        //    {
        //        if (!await _apiManager.ValidUserBySecurityTokenV2(model.session_token))
        //            throw new Exception(MessageCollection.InvalidSecurityToken);



        //        try
        //        {
        //            string secreteKey = string.Empty;
        //            string prov_id = string.Empty;

        //            secreteKey = SettingsValues.GetJWTSequrityKey();

        //            IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

        //            product_code_prepaid = configuration.GetSection("AppSettings:product_code_prepaid").Value;
        //            product_code_postpaid = configuration.GetSection("AppSettings:product_code_Postpaid").Value;
        //            product_category_prepaid = configuration.GetSection("AppSettings:product_category_prepaid").Value;
        //            product_category_postpaid = configuration.GetSection("AppSettings:product_category_postpaid").Value;
        //            product_category_simReplacment = configuration.GetSection("AppSettings:product_category_simReplacment").Value;

        //            try
        //            {
        //                if (model.sim_serial.Length < 4)
        //                {
        //                    string msg = "sim_serial must be last 4 digits!";
        //                    raResp.result = false;
        //                    raResp.message = msg;
        //                    return Ok(raResp);
        //                }
        //                else if (model.sim_serial.Length > 4)
        //                {
        //                    sim_s = model.sim_serial.Substring(model.sim_serial.Length - Math.Min(4, model.sim_serial.Length));
        //                    model.sim_serial = sim_s;
        //                }
        //            }
        //            catch (Exception)
        //            {
        //                string keyNotFound = "sim_serial is Mandatory!";
        //                raResp.result = false;
        //                raResp.message = keyNotFound;
        //                return Ok(raResp);
        //            }

        //            model.user_name = SettingsValues.GetDMSUserName();
        //            model.password = SettingsValues.GetDMSPassword();
        //            model.product_code_prepaid = product_code_prepaid;
        //            model.product_code_postpaid = product_code_postpaid;
        //            model.product_category_prepaid = product_category_prepaid;
        //            model.product_category_postpaid = product_category_postpaid;
        //            model.product_category_simReplacement = product_category_simReplacment;
        //        }
        //        catch (Exception)
        //        {
        //            string keyNotFound = "Key not found in Web.config!";
        //            raResp.result = false;
        //            raResp.message = keyNotFound;
        //            return Ok(raResp);
        //        }

        //        apiUrl = String.Format(UnpairedMSISDNList.CheckUnpairedSIM);
        //        UnpairedSIMreqRootModel reqValue = dMSParse.UnpairedSIMReqModelParse(model);
        //        log.req_blob = _blJson.GetGenericJsonData(reqValue);
        //        log.req_time = DateTime.Now;

        //        JObject dmsResp = (JObject)await _apiReq.HttpPostRequestSIMSerial(reqValue, apiUrl, "GetUnpairedSIMlist");

        //        log.res_time = DateTime.Now;
        //        txtResp = Convert.ToString(dmsResp);
        //        log.res_blob = _blJson.GetGenericJsonData(dmsResp);

        //        if (dmsResp != null)
        //        {
        //            log.res_blob = _blJson.GetGenericJsonData(dmsResp);

        //            log.is_success = 1;

        //            UnpairedSIMRespRootData? dbssRespModel = JsonConvert.DeserializeObject<UnpairedSIMRespRootData>(dmsResp.ToString());

        //            if (dbssRespModel != null)
        //            {
        //                if (dbssRespModel.data != null)
        //                {
        //                    var result = ((IEnumerable)dbssRespModel.data).Cast<object>().ToList();

        //                    raRespData = _dbssToRaParse.UnpairedSIMListDataParsing(result);

        //                    if (raRespData.Count > 0)
        //                    {
        //                        raResp.data = raRespData;
        //                        raResp.result = true;
        //                        raResp.message = MessageCollection.Success;
        //                    }
        //                    else
        //                    {
        //                        raResp.data = raRespData;
        //                        raResp.result = false;
        //                        raResp.message = MessageCollection.NoDataFound;
        //                    }
        //                }
        //                else
        //                {
        //                    raResp.data = raRespData;
        //                    raResp.result = false;
        //                    raResp.message = "DMS API doesn't return any SIM.";
        //                }
        //            }
        //            else
        //            {
        //                raResp.data = raRespData;
        //                raResp.result = false;
        //                raResp.message = "DMS API doesn't return any SIM.";
        //            }
        //        }
        //        else
        //        {
        //            raResp.data = raRespData;
        //            raResp.result = false;
        //            raResp.message = "Unable to load data from DMS API.";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        log.res_time = DateTime.Now;
        //        ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //        log.res_blob = _blJson.GetGenericJsonData(error);

        //        log.is_success = 0;
        //        log.error_code = error.error_code ?? String.Empty;
        //        log.error_source = error.error_source ?? String.Empty;
        //        log.message = error.error_description ?? String.Empty;

        //        raResp.data = raRespData;
        //        raResp.result = false;
        //        raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
        //    }
        //    finally
        //    {
        //        log.method_name = "GetUnpairedSIMlist";
        //        log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
        //        log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
        //        log.user_id = model.retailer_code;
        //        string resStr = string.Empty;
        //        if (txtResp != null)
        //        {
        //            resStr = txtResp.ToString();
        //        }

        //        await _bllLog.RAToDBSSLog(log, apiUrl, txtResp);
        //    }
        //    return Ok(raResp);
        //}

        /// <summary>
        /// This API is used to Get Unpaired SIM from DMS Type.
        /// </summary>
        /// <param name=""></param>
        /// <returns>Subscription Type List / Failure</returns>
        //[Authorize(Roles = "Retailer")]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetUnpairedSIMlistV2")]
        public async Task<IActionResult> GetUnpairedSIMlistV2([FromBody][Bind("password,product_category_StarTrekEsim,product_category_StarTrekPrepaid,product_category_postpaid,product_category_prepaid,product_category_simReplacement,product_code_StarTrekEsim,product_code_StarTrekPrepaid,product_code_postpaid,product_code_prepaid,product_code_simReplacement,retailer_code,right_id,session_token,sim_serial,user_name")] UnpairedSIMsearchReqModel model)
        {
            List<SIMReponseDataRev> raRespData = new List<SIMReponseDataRev>();
            UnpairedSIMDataRev raResp = new UnpairedSIMDataRev();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse dMSParse = new BLLRAToDBSSParse();
            string userName = string.Empty;
            string password = string.Empty;
            string sim_s = string.Empty;
            string secreteKey = SettingsValues.GetJWTSequrityKey();
            string product_code_prepaid = SettingsValues.Getproduct_code_prepaid();
            string product_code_postpaid = SettingsValues.Getproduct_code_Postpaid();
            string product_category_prepaid = SettingsValues.Getproduct_category_prepaid();
            string product_category_postpaid = SettingsValues.Getproduct_category_postpaid();
            string product_category_simReplacment = SettingsValues.Getproduct_category_simReplacment();
            string product_code_simReplacment = SettingsValues.Getproduct_code_simReplacment();
            string product_code_StarTrekPrepaid = SettingsValues.Getp_code_starTrek_prepaid();
            string product_code_StarTrekPrepaid_esim = SettingsValues.Getp_code_starTrek_prepaid_esim();
            string product_category_StarTrekPrepaid = SettingsValues.Getproduct_category_StarTrekPrepaid();
            string product_category_StarTrekPrepaid_esim = SettingsValues.Getproduct_category_StarTrekPrepaid_esim();

            try
            {

                string prov_id = string.Empty;

                ValidTokenResponse security = new ValidTokenResponse();
                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!String.IsNullOrEmpty(model.retailer_code))
                        {
                            string username = model.retailer_code.Substring(1);
                            //if (!username.Equals(security.UserName))
                            //{
                            //    throw new Exception(SettingsValues.GetSessionMessage());
                            //}
                            prov_id = security.LoginProviderId;
                        }
                    }
                    else
                    {
                        return Ok(new RACommonResponseRevamp() { isError = true, message = security.Message });
                    }
                }

                if (string.IsNullOrEmpty(model.sim_serial) || model.sim_serial.Length < 4)
                {
                    string msg = "sim_serial must be last 4 digits!";
                    raResp.isError = true;
                    raResp.message = msg;
                    return Ok(raResp);
                }
                else if (model.sim_serial.Length > 4)
                {
                    sim_s = model.sim_serial.Substring(model.sim_serial.Length - Math.Min(4, model.sim_serial.Length));
                    model.sim_serial = sim_s;
                }

                model.user_name = SettingsValues.GetDMSUserName();
                model.password = SettingsValues.GetDMSPassword();
                model.product_code_prepaid = product_code_prepaid;
                model.product_code_postpaid = product_code_postpaid;
                model.product_category_prepaid = product_category_prepaid;
                model.product_category_postpaid = product_category_postpaid;
                model.product_category_simReplacement = product_category_simReplacment;
                model.product_code_simReplacement = product_code_simReplacment;
                model.product_code_StarTrekPrepaid = product_code_StarTrekPrepaid;
                model.product_code_StarTrekEsim = product_code_StarTrekPrepaid_esim;
                model.product_category_StarTrekPrepaid = product_category_StarTrekPrepaid;
                model.product_category_StarTrekEsim = product_category_StarTrekPrepaid_esim;

                apiUrl = String.Format(UnpairedMSISDNList.CheckUnpairedSIM);
                UnpairedSIMreqRootModel reqValue = dMSParse.UnpairedSIMReqModelParse(model);
                log.req_blob = _blJson.GetGenericJsonData(reqValue);
                log.req_time = DateTime.Now;

                JObject dmsResp = (JObject)await _apiReq.HttpPostRequestSIMSerial(reqValue, apiUrl, "GetUnpairedSIMlistV2");

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dmsResp);
                log.res_blob = _blJson.GetGenericJsonData(dmsResp);

                if (dmsResp != null)
                {
                    log.res_blob = _blJson.GetGenericJsonData(dmsResp);

                    log.is_success = 1;

                    UnpairedSIMRespRootData? dbssRespModel = JsonConvert.DeserializeObject<UnpairedSIMRespRootData>(dmsResp.ToString());

                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel.data != null)
                        {
                            var result = ((IEnumerable)dbssRespModel.data).Cast<object>().ToList();

                            raRespData = _dbssToRaParse.UnpairedSIMListDataParsingV2(result);

                            if (raRespData.Count > 0)
                            {
                                raResp.data = raRespData;
                                raResp.isError = false;
                                raResp.message = MessageCollection.Success;
                            }
                            else
                            {
                                raResp.data = raRespData;
                                raResp.isError = true;
                                raResp.message = MessageCollection.NoDataFound;
                            }
                        }
                        else
                        {
                            raResp.data = raRespData;
                            raResp.isError = true;
                            raResp.message = "DMS API doesn't return any SIM.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "DMS API doesn't return any SIM.";
                    }
                }
                else
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = "Unable to load data from DMS API.";
                }
                return Ok(raResp);
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
                log.message = error.error_custom_msg ?? error.error_description;
                raResp.data = raRespData;
                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                return Ok(raResp);
            }
            finally
            {
                log.method_name = "GetUnpairedSIMlistV2";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_code ?? "";
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }

                await _bllLog.RAToDBSSLog(log);
            }

        }

        [HttpPost]
        [Route("GetUnpairedSIMlistV3")]
        public async Task<IActionResult> GetUnpairedSIMlistV3(UnpairedSIMsearchReqModelV2 model)
        {
            List<SIMReponseDataRev> raRespData = new List<SIMReponseDataRev>();
            UnpairedSIMDataRev raResp = new UnpairedSIMDataRev();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            BLLRAToDBSSParse dMSParse = new BLLRAToDBSSParse();
            ProductInfoRespModel productInfo = new ProductInfoRespModel();

            string userName = string.Empty;
            string password = string.Empty;
            string sim_s = string.Empty;
            try
            {
                try
                {
                    string secreteKey = string.Empty;
                    string loginProviderId = string.Empty;
                    secreteKey = SettingsValues.GetJWTSequrityKey();
                    ValidTokenResponse security = new ValidTokenResponse();
                    TokenValidationService token = new TokenValidationService(secreteKey);

                    security = token.ValidateToken(model.session_token);

                    if (security != null)
                    {
                        if (security.IsVallid == true)
                        {
                            if (!String.IsNullOrEmpty(model.retailer_code))
                            {
                                string username = model.retailer_code.Substring(1);
                                //if (!username.Equals(security.UserName))
                                //{
                                //    throw new Exception(SettingsValues.GetSessionMessage());
                                //}
                                loginProviderId = security.LoginProviderId;
                            }
                        }
                        else
                        {
                            throw new Exception(security.Message);
                        }
                    }

                    try
                    {
                        productInfo = await _bllCommon.GetProductValueForSIMSearching(model);

                        model.product_code = productInfo.product_code;
                        model.product_category = productInfo.product_category;

                        if (model.sim_serial.Length < 4)
                        {
                            string msg = "sim_serial must be last 4 digits!";
                            raResp.isError = true;
                            raResp.message = msg;
                            return Ok(raResp);
                        }
                        else if (model.sim_serial.Length > 4)
                        {
                            sim_s = model.sim_serial.Substring(model.sim_serial.Length - Math.Min(4, model.sim_serial.Length));
                            model.sim_serial = sim_s;
                        }
                    }
                    catch (Exception)
                    {
                        string keyNotFound = "sim_serial is Mandatory!";
                        raResp.isError = true;
                        raResp.message = keyNotFound;
                        return Ok(raResp);
                    }
                    try { model.user_name = SettingsValues.GetDMSUserName(); }
                    catch (Exception) { throw new Exception("userName is not found in appsettings.json"); }

                    try { model.password = SettingsValues.GetDMSPassword(); }
                    catch (Exception) { throw new Exception("dms_pas is not found in appsettings.json"); }

                }
                catch (Exception ex)
                {
                    string keyNotFound = ex.Message;
                    raResp.isError = true;
                    raResp.message = keyNotFound;
                    return Ok(raResp);
                }

                apiUrl = String.Format(UnpairedMSISDNList.CheckUnpairedSIM); 
                UnpairedSIMreqRootModel reqValue = dMSParse.UnpairedSIMReqModelParseV2(model);
                log.req_blob = _blJson.GetGenericJsonData(reqValue);
                log.req_time = DateTime.Now;

                JObject dmsResp = (JObject)await _apiReq.HttpPostRequestSIMSerial(reqValue, apiUrl, "GetUnpairedSIMlistV3");

                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dmsResp);
                log.res_blob = _blJson.GetGenericJsonData(dmsResp);

                if (dmsResp != null)
                {
                    log.res_blob = _blJson.GetGenericJsonData(dmsResp);

                    log.is_success = 1;

                    UnpairedSIMRespRootData? dbssRespModel = JsonConvert.DeserializeObject<UnpairedSIMRespRootData>(dmsResp.ToString());

                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel.data != null)
                        {
                            var result = ((IEnumerable)dbssRespModel.data).Cast<object>().ToList();

                            raRespData = _dbssToRaParse.UnpairedSIMListDataParsingV2(result);

                            if (raRespData.Count > 0)
                            {
                                raResp.data = raRespData;
                                raResp.isError = false;
                                raResp.message = MessageCollection.Success;
                            }
                            else
                            {
                                raResp.data = raRespData;
                                raResp.isError = true;
                                raResp.message = MessageCollection.NoDataFound;
                            }
                        }
                        else
                        {
                            raResp.data = raRespData;
                            raResp.isError = true;
                            raResp.message = "DMS API doesn't return any SIM.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "DMS API doesn't return any SIM.";
                    }
                }
                else
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = "Unable to load data from DMS API.";
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
                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(ex.Message);

                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    log.is_success = 0;
                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                }
                catch (Exception)
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = ex.Message;
                }
            }
            finally
            {
                log.method_name = "GetUnpairedSIMlistV3";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_code;
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }

                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(raResp);
        }

        #endregion
        #region Get Channel Wise Payment Method
        /// <summary>
        /// This API is used for Getting DivDisThana
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        //[GzipCompression]
        //[HttpPost]
        //[Route("GetPaymentMethod")]
        //public async Task<IActionResult> GetPaymentMethod(RAGetPaymentMehtodRequest model)
        //{
        //    ChannelWiseResponse cwRes = new ChannelWiseResponse();
        //    try
        //    {
        //        if (!await _apiManager.ValidUserBySecurityTokenV2(model.session_token))
        //            throw new Exception(MessageCollection.InvalidSecurityToken);

        //        cwRes = await _bllCommon.GetPaymentMethod(model);

        //        return Ok(cwRes);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        try
        //        {
        //            ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
        //            return Ok(new RACommonResponse
        //            {
        //                result = false,
        //                message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
        //            });
        //        }
        //        catch (Exception ex2)
        //        {
        //            return Ok(new RACommonResponse
        //            {
        //                result = false,
        //                message = ex2.InnerException.Message
        //            });
        //        }
        //    }
        //}

        /// <summary>
        /// This API is used for Getting DivDisThana
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        //[GzipCompression]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetPaymentMethodV2")]
        public async Task<IActionResult> GetPaymentMethodV2([FromBody][Bind("channel_id,right_id,session_token")] RAGetPaymentMehtodRequest model)
        {
            ChannelWiseResponseRev cwRes = new ChannelWiseResponseRev();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;
                string user_name = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        prov_id = security.LoginProviderId;
                        user_name = security.UserName;
                    }
                    else
                    {
                        return Ok(new RACommonResponseRevamp()
                        {
                            isError = true,
                            message = security.Message
                        });
                    }
                }

                cwRes = await _bllCommon.GetPaymentMethodV2(model, user_name);

                return Ok(cwRes);
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
                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                    });
                }
                catch (Exception ex2)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = ex2.InnerException?.Message ?? ex.Message
                    });
                }
            }
        }
        #endregion
        #region get paired MSISDN
        //[Authorize(Roles = "Retailer")]  
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetPairedMSISDN")]
        public async Task<IActionResult> GetPairedMSISDN([FromBody][Bind("retailer_id,right_id,session_token,sim_serial")] PairedMSISDNReqModel model)
        {
            List<ReponseDataRev> raRespData = new List<ReponseDataRev>();
            PairedMSISDNDataRev raResp = new PairedMSISDNDataRev();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            JObject dbssResp = new JObject();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        return Ok(new RACommonResponseRevamp()
                        {
                            isError = true,
                            message = security.Message
                        });
                    }
                }

                if (model.sim_serial.Substring(0, FixedValueCollection.SIMCode.Length) != FixedValueCollection.SIMCode)
                {
                    model.sim_serial = FixedValueCollection.SIMCode + model.sim_serial;
                }

                apiUrl = String.Format(PairedMSISDN.PairedMSISDNURL, model.sim_serial);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetPairedMSISDN");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                if (dbssResp != null)
                {
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    log.is_success = 1;
                    PairedMSISDNRootData? dbssRespModel = JsonConvert.DeserializeObject<PairedMSISDNRootData>(dbssResp.ToString());

                    if (dbssRespModel?.data == null)
                    {
                        log.is_success = 0;
                        raResp.isError = true;
                        raResp.message = "DBSS Error: " + MessageCollection.NoDataFound;
                        return Ok(raResp);
                    }
                    raResp = _dbssToRaParse.PairedMSISDNSearchParsing(dbssResp);

                    if (raResp.data != null)
                    {
                        return Ok(new PairedMSISDNDataRev()
                        {
                            isError = false,
                            message = "MSISDN found",
                            data = new ReponseDataRev()
                            {
                                msisdn = raResp.data.msisdn
                            }
                        });
                    }
                    else
                    {
                        return Ok(new PairedMSISDNDataRev()
                        {
                            isError = true,
                            message = "MSISDN not found",
                            data = new ReponseDataRev()
                            {
                                msisdn = ""
                            }
                        });
                    }
                }
                else
                {
                    raResp.isError = true;
                    raResp.message = "Unable to load data from DBSS API.";
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
                log.res_time = DateTime.Now;
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                log.res_blob = _blJson.GetGenericJsonData(error);
                log.is_success = 0;
                log.error_code = error.error_code ?? String.Empty;
                log.error_source = error.error_source ?? String.Empty;
                log.message = error.error_custom_msg ?? error.error_description;
                raResp.isError = true;
                raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                return Ok(raResp);
            }
            finally
            {
                log.method_name = "GetPairedMSISDN";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }

                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(raResp);
        }
        #endregion
        #region App information Update from Retailer
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("UpdateAppVersionFromRetailerApp")]
        public async Task<IActionResult> UpdateAppVersionFromRetailerApp([FromBody][Bind("app_version_code,app_version_name,center_code,channel_name,distributor_code,right_id,session_token,user_name")] AppInfoUpdateReqModel model)
        {
            RACommonResponseRevamp cwRes = new RACommonResponseRevamp();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                try
                {
                    secreteKey = SettingsValues.GetJWTSequrityKey();
                }
                catch
                { }
                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        return Ok(new RACommonResponseRevamp()
                        {
                            isError = true,
                            message = security.Message
                        });
                    }
                }

                await _bllCommon.AppInfoUpdate(model, prov_id);

                return Ok(new RACommonResponseRevamp()
                {
                    isError = false,
                    message = "Successfully Updated!"
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
                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                    });
                }
                catch (Exception ex2)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = ex2.InnerException?.Message ?? ex2.Message
                    });
                }
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("UpdateAppVersionFromRetailerAppV2")]
        public async Task<IActionResult> UpdateAppVersionFromRetailerAppV2([FromBody][Bind("app_version_code,app_version_name,center_code,channel_name,distributor_code,right_id,session_token,user_name")] AppInfoUpdateReqModel model)
        {
            RACommonResponseRevamp cwRes = new RACommonResponseRevamp();
            string secreteKey = string.Empty;
            APPVersionRespModel respModel = new APPVersionRespModel();

            secreteKey = SettingsValues.GetJWTSequrityKey();

            TokenService token = new TokenService(secreteKey);
            try
            {
                if (!await _apiManager.ValidUserBySecurityTokenV2(model.session_token))
                    throw new WebException(MessageCollection.InvalidSecurityToken);

                string prov_id = _bio.GetDecryptedSecurityToken(model.session_token);

                if (prov_id.Equals("Fail"))
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "Invalid Security Token"
                    });
                }
                try
                {
                    await _bllCommon.AppInfoUpdate(model, prov_id);
                }
                catch (Exception ex)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = ex.Message
                    });
                }

                ResellerLoginUserInfoResponse resellerLogin = new ResellerLoginUserInfoResponse()
                {
                    user_name = model.user_name,
                    center_code = model.center_code,
                    channel_name = model.channel_name,
                    distributor_code = model.distributor_code
                };

                return Ok(new RACommonResponseRetailLoginUpdateToken()
                {
                    isError = false,
                    message = "Successfully Updated!",
                    data = new SessionForRetailToBiometric()
                    {
                        session_token = token.GenerateTokenV2(resellerLogin, prov_id)
                    }
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
                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                    });
                }
                catch (Exception ex2)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = ex2.InnerException?.Message ?? ex2.Message
                    });
                }
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("UpdateAppVersionFromRetailerAppV3")]
        public async Task<IActionResult> UpdateAppVersionFromRetailerAppV3([FromBody][Bind("app_version_code,app_version_name,center_code,channel_name,distributor_code,right_id,session_token,user_name")] AppInfoUpdateReqModel model)
        {
            RACommonResponseRevamp cwRes = new RACommonResponseRevamp();
            string secreteKey = string.Empty;
            APPVersionRespModel respModel = new APPVersionRespModel();

            secreteKey = SettingsValues.GetJWTSequrityKey();

            TokenService token = new TokenService(secreteKey);
            try
            {
                if (!await _apiManager.ValidUserBySecurityTokenV2(model.session_token))
                    throw new WebException(MessageCollection.InvalidSecurityToken);

                string prov_id = _bio.GetDecryptedSecurityToken(model.session_token);

                if (prov_id.Equals("Fail"))
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "Invalid Security Token"
                    });
                }
                try
                {
                    respModel = await _bLLUserAuthenticaion.GetAppVersion();

                    if (Convert.ToInt32(model.app_version_code) < respModel.app_version)
                    {
                        return Ok(new RACommonResponseRevamp
                        {
                            isError = true,
                            message = $"Update version is available. Please update {respModel.app_url} Version!"
                        });
                    }
                    else
                    {
                        await _bllCommon.AppInfoUpdate(model, prov_id);
                    }
                }
                catch (Exception ex)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = ex.Message
                    });
                }

                ResellerLoginUserInfoResponse resellerLogin = new ResellerLoginUserInfoResponse()
                {
                    user_name = model.user_name,
                    center_code = model.center_code,
                    channel_name = model.channel_name,
                    distributor_code = model.distributor_code
                };

                return Ok(new RACommonResponseRetailLoginUpdateToken()
                {
                    isError = false,
                    message = "Successfully Updated!",
                    data = new SessionForRetailToBiometric()
                    {
                        session_token = token.GenerateTokenV2(resellerLogin, prov_id)
                    }
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
                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg
                    });
                }
                catch (Exception ex2)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = ex2.InnerException?.Message ?? ex2.Message
                    });
                }
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetBTSSiteId")]
        public async Task<IActionResult> GetBTSSiteId([FromBody][Bind("cid,lac,right_id,session_token")] SiteIdRequestModel model)
        {
            RACommonResponseRevamp cwRes = new RACommonResponseRevamp();
            BTSCode bTSCode = new BTSCode();
            SiteIdResponseModel bts_response = new SiteIdResponseModel();
            string secreteKey = string.Empty;

            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string prov_id = string.Empty;

                try
                {
                    secreteKey = SettingsValues.GetJWTSequrityKey();
                }
                catch
                { }
                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        bts_response = new SiteIdResponseModel()
                        {
                            isError = true,
                            message = security.Message,
                            data = new BTSCode()
                            {
                                bts_code = "--"
                            }
                        };
                        return Ok(bts_response);
                    }
                }

                int isBTSshow = 0;

                isBTSshow = SettingsValues.GetBTSCodeShowingOrNot();

                if (isBTSshow != 0)
                {
                    bTSCode = await _bllCommon.GetBTSCode(model);

                    if (!String.IsNullOrEmpty(bTSCode.bts_code))
                    {
                        bts_response = new SiteIdResponseModel()
                        {
                            isError = false,
                            message = "BTS_ID Found!",
                            data = new BTSCode()
                            {
                                bts_code = bTSCode.bts_code
                            }
                        };
                    }
                    else
                    {
                        bts_response = new SiteIdResponseModel()
                        {
                            isError = false,
                            message = "BTS_ID Not Found!",
                            data = new BTSCode()
                            {
                                bts_code = "---"
                            }
                        };
                    }
                }
                else
                {
                    bts_response = new SiteIdResponseModel()
                    {
                        isError = false,
                        message = "BTS_ID Not Found!",
                        data = new BTSCode()
                        {
                            bts_code = "---"
                        }
                    };
                }

                return Ok(bts_response);
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
                bts_response = new SiteIdResponseModel()
                {
                    isError = true,
                    message = error.error_custom_msg != null ? error.error_custom_msg : error.error_description,
                    data = new BTSCode()
                    {
                        bts_code = "--"
                    }
                };

                return Ok(bts_response);

            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetBTSSiteIdV2")]
        public async Task<IActionResult> GetBTSSiteIdV2([FromBody][Bind("cid,lac,number_category,right_id,session_token")] SiteIdRequestModelV2 model)
        {
            SiteIdResponseModelV2 resp = new SiteIdResponseModelV2();

            try
            {
                // 1) Token validation
                var secretKey = SettingsValues.GetJWTSequrityKey();
                var token = new TokenValidationService(secretKey);
                var security = token.ValidateToken(model.session_token);

                if (security == null || !security.IsVallid)
                {
                    resp.isError = true;
                    resp.message = security?.Message ?? "Invalid session token.";
                    resp.data = new BTSCodeV2 { bts_code = "--", is_lus = false };
                    return Ok(resp);
                }

                var showBts = SettingsValues.GetBTSCodeShowingOrNot();
                if (showBts == 0)
                {
                    resp.isError = false;
                    resp.message = "BTS_ID Not Found!";
                    resp.data = new BTSCodeV2 { bts_code = "---", is_lus = false };
                    return Ok(resp);
                }

                // 3) Cherish category bypass
                var cherishCfg = SettingsValues.GetCherishCategory();
                var cherishSet = cherishCfg.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim().ToUpperInvariant())
                                           .ToHashSet();

                var isCherish = cherishSet.Contains((model.number_category ?? string.Empty).Trim().ToUpperInvariant());

                // 4) Get BTS code from existing BLL
                var btsCodeRow = await _bllCommon.GetBTSCode(new SiteIdRequestModel
                {
                    session_token = model.session_token,
                    lac = model.lac,
                    cid = model.cid
                });

                var btsCode = btsCodeRow?.bts_code ?? string.Empty;
                if (string.IsNullOrWhiteSpace(btsCode))
                {
                    resp.isError = false;
                    resp.message = "BTS_ID Not Found!";
                    resp.data = new BTSCodeV2 { bts_code = "---", is_lus = false };
                    return Ok(resp);
                }

                // 5) LUS eligibility: skip if cherish
                bool isLus = false;
                bool is_cherish = false;
                if (!isCherish)
                {
                    string cleanBTSCode = btsCode.Trim('"', '\\');
                    isLus = await _bllCommon.GetIsLusEligibleAsync(cleanBTSCode); // new BLL method below
                    is_cherish = false;
                }
                else
                {
                    is_cherish = true;
                }

                resp.isError = false;
                resp.message = "BTS_ID Found!";
                resp.data = new BTSCodeV2 { bts_code = btsCode, is_lus = isLus, is_cherish = is_cherish };
                return Ok(resp);
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
                var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                resp.isError = true;
                resp.message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                resp.data = new BTSCodeV2 { bts_code = "--", is_lus = false };
                return Ok(resp);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetRestrictedAddress")]
        public async Task<IActionResult> GetRestrictedAddress([FromBody][Bind("right_id,session_token")] RACommonRequest model)
        {
            string secreteKey = string.Empty;
            BlackListedWordModel blackListed = new BlackListedWordModel();

            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string prov_id = string.Empty;

                try
                {
                    secreteKey = SettingsValues.GetJWTSequrityKey();
                }
                catch
                { }
                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        blackListed.message = security.Message;
                        blackListed.isError = true;
                        return Ok(blackListed);
                    }
                }

                blackListed = await _bllCommon.GetBlackListedWordForAddress();
                blackListed.message = "Success!";
                blackListed.isError = false;

                return Ok(blackListed);
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
                blackListed.message = error.error_custom_msg != null ? error.error_custom_msg : error.error_description;
                blackListed.isError = true;
                return Ok(blackListed);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetRestrictedName")]
        public async Task<IActionResult> GetRestrictedName([FromBody][Bind("right_id,session_token")] RACommonRequest model)
        {
            string secreteKey = string.Empty;
            BlackListedWordModel blackListed = new BlackListedWordModel();

            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string prov_id = string.Empty;

                try
                {
                    secreteKey = SettingsValues.GetJWTSequrityKey();
                }
                catch
                { }
                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        prov_id = security.LoginProviderId;
                    }
                    else
                    {
                        blackListed.message = security.Message;
                        blackListed.isError = true;
                        return Ok(blackListed);
                    }
                }

                blackListed = await _bllCommon.GetBlackListedWordForName();
                blackListed.message = "Success!";
                blackListed.isError = false;

                return Ok(blackListed);
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
                blackListed.message = error.error_custom_msg != null ? error.error_custom_msg : error.error_description;
                blackListed.isError = true;
                return Ok(blackListed);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetScannerInfo")]
        public async Task<IActionResult> GetScannerInfo([FromBody][Bind("scanner_id")] ScannerInfoReqModel model)
        {
            string secreteKey = string.Empty;
            ScannerInfoRespModel scannerInfo = new ScannerInfoRespModel();

            try
            {
                scannerInfo = await _bllCommon.GetScannerInfo(model);

                return Ok(scannerInfo);
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

                scannerInfo.isError = false;
                scannerInfo.message = error.error_custom_msg != null ? error.error_custom_msg : error.error_description;
                scannerInfo.data = new ScannerData()
                {
                    is_bl_scanner = "No"
                };
                return Ok(scannerInfo);
            }
        }
        #endregion


        #region Cherish Number Sell
        [HttpPost]
        [Route("categoryDropdown")]
        public async Task<IActionResult> CherishCategoryDropdown(CherishCategoryReqModel model)
        {
            CherishCategoryListResModel categoryData = new CherishCategoryListResModel();
            string user_id = string.Empty;
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

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
                        user_id = security.UserName;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                categoryData = await _bllCommon.GetCherishCategoyListData(model.channel_name);

                return Ok(categoryData);
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
                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    return Ok(new ActivityLogResponseRevamp()
                    {
                        isError = true,
                        message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                        data = new List<VMActivityLogRevamp>()
                    });
                }
                catch (Exception)
                {
                    return Ok(new ActivityLogResponseRevamp()
                    {
                        isError = true,
                        message = ex.Message,
                        data = new List<VMActivityLogRevamp>()
                    });
                }
            }
        }

        [HttpPost]
        //[ValidateModel]
        [Route("ValidateMSISDNANDSIM")]
        public async Task<IActionResult> ValidateMSISDNANDSIM([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,selected_category,session_token,sim_category,sim_number")] CherishMSISDNCheckRequest msisdnCheckReqest)
        {
            try
            {
                RACommonResponseRevamp response = new RACommonResponseRevamp();

                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                response = await _bio.ValidateMSISDNVAndSIM(msisdnCheckReqest, "ValidateMSISDNANDSIM");
                if (response.isError == false)
                {
                    #region ICC checking from DMS 
                    ICCDetailsRequestModel model = new ICCDetailsRequestModel()
                    {
                        center_code = msisdnCheckReqest.center_code,
                        icc = msisdnCheckReqest.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        mobile_number = msisdnCheckReqest.mobile_number
                    };

                    ICCDetailsResponse? iccData = await _apiManager.CheckICCfromDMS(model);

                    if (iccData != null && iccData.result)
                    {
                        response.isError = false;
                        response.data.offer_name = iccData.offer_name ?? string.Empty;
                        response.data.product_name = iccData.product_name ?? string.Empty;
                        response.data.details_message = iccData.offer_description ?? string.Empty;
                    }
                    else
                    {
                        response.isError = true;
                        response.message = iccData?.message ?? "Unknown error";
                        return Ok(response);
                    }
                    #endregion
                }
                return Ok(response);
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = ex.Message,
                    data = new Datas()
                    {
                        isEsim = 0,
                        request_id = "0"
                    }

                });
            }
        }

        [HttpPost]
        [ValidateModel]
        [Route("ValidateMSISDNANDSIM_ESIM")]
        public async Task<IActionResult> ValidateMSISDNANDSIM_ESIM([FromBody][Bind("center_code,channel_id,channel_name,inventory_id,lan,mobile_number,purpose_number,retailer_id,right_id,selected_category,session_token,sim_category,sim_number")] CherishMSISDNCheckRequest msisdnCheckReqest)
        {
            RACommonResponseRevamp rACommonResponse = new RACommonResponseRevamp();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(msisdnCheckReqest.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!msisdnCheckReqest.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                rACommonResponse = await _bio.ValidateMSISDNVAndSIMV2(msisdnCheckReqest, "ValidateUnpairedMSISDN_ESIMV2");

                if (rACommonResponse.isError == false)
                {
                    #region ICC checking from DMS 
                    ICCDetailsRequestModel model = new ICCDetailsRequestModel()
                    {
                        center_code = msisdnCheckReqest.center_code,
                        icc = msisdnCheckReqest.sim_number,
                        retailer_id = msisdnCheckReqest.retailer_id,
                        mobile_number = msisdnCheckReqest.mobile_number
                    };

                    ICCDetailsResponse? iccData = await _apiManager.CheckICCfromDMS(model);

                    if (iccData != null && iccData.result)
                    {
                        rACommonResponse.isError = false;
                        rACommonResponse.data.offer_name = iccData.offer_name ?? string.Empty;
                        rACommonResponse.data.product_name = iccData.product_name ?? string.Empty;
                        rACommonResponse.data.details_message = iccData.offer_description ?? string.Empty;
                    }
                    else
                    {
                        rACommonResponse.isError = true;
                        rACommonResponse.message = iccData?.message ?? "Unknown error";
                        return Ok(rACommonResponse);
                    }
                    #endregion
                }

                return Ok(rACommonResponse);
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = ex.Message,
                    data = new Datas()
                    {
                        request_id = "0",
                        isEsim = 1
                    }
                });
            }
        }

        [HttpPost]
        [Route("GetCherishedMSISDNList")]
        public async Task<IActionResult> GetCherishedMSISDNList(UnpairedMSISDNListReqModelV2 model)
        {
            List<ReponseDataRev> raRespData = new List<ReponseDataRev>();
            UnpairedMSISDNDataRev raResp = new UnpairedMSISDNDataRev();
            string apiUrl = string.Empty;
            string? txtResp = string.Empty;
            BIAToDBSSLog log = new BIAToDBSSLog();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                if (String.IsNullOrEmpty(model.msisdn))
                {
                    //GetUnpairedMSISDNSearchDefaultValue
                    model.msisdn = await _bllCommon.GetUnpairedMSISDNSearchDefaultValueCherished(model);

                    if (String.IsNullOrEmpty(model.msisdn))
                    {
                        return Ok(raResp);
                    }
                    if (model.msisdn.Substring(0, 4) != FixedValueCollection.MSISDNFixedValue)
                    {
                        model.msisdn = FixedValueCollection.MSISDNFixedValue + model.msisdn;
                    }
                    if (model.msisdn.Substring(0, 1) == "0")
                    {
                        model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                    }
                }
                else
                {
                    if (model.msisdn.Substring(0, 4) != FixedValueCollection.MSISDNFixedValue)
                    {
                        model.msisdn = FixedValueCollection.MSISDNFixedValue + model.msisdn;
                    }
                    if (model.msisdn.Substring(0, 1) == "0")
                    {
                        model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                    }
                }

                string channelIdFromConfig = string.Empty;
                string[] arrChannelId = null;
                string stockIdFromConfig = string.Empty;
                string[] arrStockId = null;
                string channelId = string.Empty;
                int arrIndexChannel = 0;
                string stockIdValue = string.Empty;
                string stockIdByDefault = string.Empty;

                channelIdFromConfig = SettingsValues.GetChannelId();
                stockIdFromConfig = SettingsValues.GetChannelStockId();
                stockIdByDefault = SettingsValues.GetChannelStockDefault();


                if (channelIdFromConfig.Contains(","))
                {
                    arrChannelId = channelIdFromConfig.Split(',');
                }
                else
                {
                    arrChannelId = channelIdFromConfig.Split(' ');
                }

                if (stockIdFromConfig.Contains(","))
                {
                    arrStockId = stockIdFromConfig.Split(',');
                }
                else
                {
                    arrStockId = stockIdFromConfig.Split(' ');
                }

                channelId = await _dbssToRaParse.GetStockResponses(model.channel_name);

                if (arrChannelId.Contains(channelId))
                {
                    arrIndexChannel = Array.IndexOf(arrChannelId, channelId);
                    stockIdValue = arrStockId[arrIndexChannel];
                }
                else
                {
                    stockIdValue = stockIdByDefault;
                }

                apiUrl = String.Format(UnpairedMSISDNList.GetUnpairedMSISDNListCherished, 1, 10, model.msisdn, stockIdValue, model.Selected_category);

                log.req_blob = _blJson.GetGenericJsonData(apiUrl);
                log.req_time = DateTime.Now;
                var dbssResp = await _apiReq.HttpGetRequest(apiUrl, "GetCherishedMSISDNList");
                log.res_time = DateTime.Now;
                txtResp = Convert.ToString(dbssResp);

                if (dbssResp != null)
                {
                    log.res_blob = _blJson.GetGenericJsonData(dbssResp);

                    log.is_success = 1;
                    //var dataBss = JsonConvert.DeserializeObject(dbssResp.ToString());
                    UnpairedMSISDNRootData? dbssRespModel = JsonConvert.DeserializeObject<UnpairedMSISDNRootData>(dbssResp.ToString());
                    if (dbssRespModel != null)
                    {
                        if (dbssRespModel.data != null)
                        {
                            var result = ((IEnumerable)dbssRespModel.data).Cast<object>().ToList();

                            raRespData = _dbssToRaParse.UnpairedMSISDNListDataParsingV2(result);

                            if (raRespData.Count > 0)
                            {
                                raResp.data = raRespData;
                                raResp.isError = false;
                                raResp.message = MessageCollection.Success;
                            }
                            else
                            {
                                raResp.data = raRespData;
                                raResp.isError = true;
                                raResp.message = MessageCollection.NoDataFound;
                            }
                        }
                        else
                        {
                            raResp.data = raRespData;
                            raResp.isError = true;
                            raResp.message = "DBSS API doesn't contains any Unpaired MSISDN list.";
                        }
                    }
                    else
                    {
                        raResp.data = raRespData;
                        raResp.isError = true;
                        raResp.message = "DBSS API doesn't contains any Unpaired MSISDN list.";
                    }
                }
                else
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = "Unable to load data from DBSS API.";
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
                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(ex.Message);

                try
                {
                    ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    log.is_success = 0;
                    log.error_code = error.error_code ?? String.Empty;
                    log.error_source = error.error_source ?? String.Empty;
                    log.message = error.error_description ?? String.Empty;

                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                }
                catch (Exception)
                {
                    raResp.data = raRespData;
                    raResp.isError = true;
                    raResp.message = ex.Message;
                }
            }
            finally
            {
                log.method_name = "GetUnpairedMSISDNListV2";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;
                string resStr = string.Empty;
                if (txtResp != null)
                {
                    resStr = txtResp.ToString();
                }

                //Thread logThread = new Thread(() => bllLog.RAToDBSSLog(log, apiUrl, txtResp));
                //logThread.Start();

                await _bllLog.RAToDBSSLog(log);
            }
            return Ok(raResp);
        }

        #endregion

        #region For GA Capping

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("CheckMSISDNCountByNID")]
        public async Task<IActionResult> CheckMSISDNCountByNID([FromBody][Bind("channel_name,dob,mobile_number,nid,retailer_id,session_token")] SingleSourceGACappingReqModel model)
        {
            GACappingResponse response = new GACappingResponse();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                string[] arrChannelName = Array.Empty<string>();

                string allowedChannel = SettingsValues.GetActiveNumberCountEligibility();

                if (!string.IsNullOrEmpty(allowedChannel))
                {
                    if (allowedChannel.Contains(","))
                    {
                        arrChannelName = allowedChannel.Split(',');
                    }
                    else
                    {
                        arrChannelName = allowedChannel.Split(' ');
                    }
                }
                if (arrChannelName.Length > 0 && arrChannelName.Contains(model.channel_name))
                {
                    response = await CheckMSISDNCount(model);
                }
                else
                {
                    response.isError = false;
                    response.message = "No need to check with this channel!";
                }
                return Ok(response);
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
                return Ok(new
                {
                    isError = true,
                    message = ex.Message
                });
            }
        }

        
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("CheckMSISDNCountByNIDV2")]
        public async Task<IActionResult> CheckMSISDNCountByNIDV2([FromBody][Bind("channel_name,dob,mobile_number,nid,retailer_id,session_token")] SingleSourceGACappingReqModel model)
        {
            GACappingResponse response = new GACappingResponse();
            try
            {
                ValidTokenResponse security = new ValidTokenResponse();

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.retailer_id.Equals(security.UserName))
                        {
                            throw new Exception(SettingsValues.GetSessionMessage());
                        }
                        loginProviderId = security.LoginProviderId;
                    }
                    else
                    {
                        throw new Exception(security.Message);
                    }
                }

                #region RecycleBaseChecking

                RecycleBaseCheckingRespModel checkingRespModel = new RecycleBaseCheckingRespModel();
                
                int isRecycleBaseCheckingNeeded = SettingsValues.GetIsRecycleCheckingNeeded();

                if(isRecycleBaseCheckingNeeded == 1)
                {
                    if (model.mobile_number.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                    {
                        model.mobile_number = FixedValueCollection.MSISDNCountryCode + model.mobile_number;
                    }
                    RecycleBaseCheckingReqModel checkingReqModel = new RecycleBaseCheckingReqModel()
                    {
                        nid = model.nid,
                        dob = model.dob,
                        msisdn = model.mobile_number
                    };

                    checkingRespModel = await _bllCommon.GetCheckingRecycleBase(checkingReqModel);

                    if(checkingRespModel != null && !checkingRespModel.is_success)
                    {
                        response.isError = true;
                        response.message = checkingRespModel.error_message;
                        return Ok(response);
                    }
                }
                #endregion

                string[] arrChannelName = Array.Empty<string>();

                string allowedChannel = SettingsValues.GetActiveNumberCountEligibility();

                if (!string.IsNullOrEmpty(allowedChannel))
                {
                    if (allowedChannel.Contains(","))
                    {
                        arrChannelName = allowedChannel.Split(',');
                    }
                    else
                    {
                        arrChannelName = allowedChannel.Split(' ');
                    }
                }
                if (arrChannelName.Length > 0 && arrChannelName.Contains(model.channel_name))
                {
                    response = await CheckMSISDNCount(model);
                }
                else
                {
                    response.isError = false;
                    response.message = "No need to check with this channel!";
                }
                return Ok(response);
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
                return Ok(new
                {
                    isError = true,
                    message = ex.Message
                });
            }
        }

        private async Task<GACappingResponse> CheckMSISDNCount(SingleSourceGACappingReqModel model)
        {
            GACappingResponse response = new GACappingResponse();
            BIAToDBSSLog log = new BIAToDBSSLog();
            ValidTokenResponse? security = null;
            string retailerIdFromRequest = model.retailer_id?.Trim() ?? string.Empty;
            string userIdForLog = string.Empty;
            string userIdForResponse = string.Empty;
            string loginProviderId = string.Empty;

            try
            {
                // Token validation
                string secreteKey = SettingsValues.GetJWTSequrityKey();
                TokenValidationService token = new TokenValidationService(secreteKey);
                security = token.ValidateToken(model.session_token);

                if (security == null || !security.IsVallid)
                {
                    response.isError = true;
                    response.message = security?.Message ?? "Invalid security token";
                    response.data.user_id = string.IsNullOrWhiteSpace(retailerIdFromRequest)
                        ? security?.UserName ?? string.Empty
                        : retailerIdFromRequest;
                    return response;
                }

                var gaConfig = await _bllCommon.GetGACappingConfig();

                // Call Single Source API to get registered MSISDNs
                SingleSourceGACappingResponse singleSourceResponse = await _singleSourceGACappingService.GetRegisteredMsisdnsByNid(model.nid, model.retailer_id);

                if (singleSourceResponse.is_success && singleSourceResponse.data != null && singleSourceResponse.data.Count() > 0)
                {
                    var today = DateTime.Now;

                    if (gaConfig != null)
                    {
                        foreach (var item in gaConfig)
                        {
                            int count = 0;

                            switch (item.cappType.ToUpper())
                            {
                                case "LIFETIME":
                                    // Count everything since the beginning (no date limit)
                                    count = singleSourceResponse.data.Count();
                                    break;

                                case "MONTHLY":
                                    // Count registrations in last 30 days
                                    count = singleSourceResponse.data
                                        .Where(x => x.reg_date >= today.AddDays(-item.cappDayCount) && item.cappType == "MONTHLY")
                                        .Count();
                                    break;

                                case "OTHERS":
                                    // Use cappDayCount to filter last N days (ex: 7 days)
                                    count = singleSourceResponse.data
                                        .Where(x => x.reg_date >= today.AddDays(-item.cappDayCount) && item.cappType == "OTHERS")
                                        .Count();
                                    break;
                            }

                            // Now check limit
                            if (count >= item.capQuantityCount)
                            {
                                response.isError = true;
                                response.message = $"Customer not eligible for new connection. {count} BL connections limit exceed";

                                return response;
                            }
                        }

                    }
                }
                else
                {
                    response.isError = true;
                    response.message = singleSourceResponse.message ?? "Unable to fetch data from Single Source";
                    response.data.user_id = userIdForResponse;
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception in CheckMSISDNCountByNID for NID: {Nid}", model.nid);

                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                response.isError = true;
                response.message = string.IsNullOrEmpty(error.error_custom_msg) ?
                    error.error_description : error.error_custom_msg;

            }
            finally
            {
                log.method_name = "CheckMSISDNCountByNID";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BSS);
                log.user_id = model.retailer_id;


                // Capture the retailer id separately for traceability
                if (!string.IsNullOrWhiteSpace(retailerIdFromRequest))
                {
                    log.remarks = string.IsNullOrWhiteSpace(log.remarks)
                        ? $"retailer_id={retailerIdFromRequest}"
                        : $"{log.remarks};retailer_id={retailerIdFromRequest}";
                }

                // Set other properties to avoid any other NULL issues
                log.req_time = DateTime.Now;
                log.res_time = DateTime.Now;
                log.is_success = response.isError ? 0 : 1;
                log.message = response.message ?? string.Empty;
                log.msisdn = string.Empty; // Since we're checking by NID, not MSISDN
                log.error_code = response.isError ? "GA_CAPPING_ERROR" : string.Empty;
                log.error_source = response.isError ? "GA_Capping_Service" : string.Empty;

                await _bllLog.RAToDBSSLog(log);
            }

            return response;
        }

        #endregion
    }
}
