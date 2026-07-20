using BIA.BLL.BLLServices;
using BIA.BLL.Utility;
using BIA.Common;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.Interfaces;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Entity.Utility;
using BIA.JWT;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using BIA.Helper;
using Microsoft.AspNetCore.Http.Features;
using System.Reflection;
using Serilog;
using BIA.Entity.PopulateModel;

namespace BIA.Controllers
{
    [Route("api/FPBase")]
    [ApiController]
    public class FPBaseController : ControllerBase
    {
        private readonly BL_Json _blJson;
        private readonly BLLUserAuthenticaion _bLLUser;
        private readonly BLLOrder _orderManager;
        private readonly BLLLog _bllLog;
        private readonly BaseController _bio;
        public FPBaseController(BL_Json blJson, BLLUserAuthenticaion bLLUser, BLLOrder orderManager, BLLLog bllLog, BaseController bio)
        {
            _blJson = blJson;
            _bLLUser = bLLUser;
            _orderManager = orderManager;
            _bllLog = bllLog;
            _bio = bio;
        }

        [HttpPost]
        [Route("check-user")]
        public async Task<IActionResult> ChekcUser([FromBody][Bind("bpmsisdn,user_name")] UserCheckModel model)
        {
            DBResponseModel dBResponseModel = new DBResponseModel();
            CheckUserResponseModel checkUserResponse = new CheckUserResponseModel();
            string secreteKey = string.Empty;

            try
            {
                dBResponseModel = await _bLLUser.CheckUser(model);

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenService token = new TokenService(secreteKey);

                string loginProvider = Guid.NewGuid().ToString();

                if (dBResponseModel != null)
                {
                    if (dBResponseModel.is_user_valid == 1)
                    {
                        return Ok(new CheckUserResponseModel()
                        {
                            isError = false,
                            message = dBResponseModel.message,
                            data = new RespData()
                            {
                                is_fp_validation_need = dBResponseModel.is_fp_validation_need == 1 ? true : false,
                                is_registered = dBResponseModel.is_registered == 1 ? true : false,
                                msisdn = dBResponseModel.msisdn,
                                SessionToken = token.GenerateTokenV3(model.user_name, loginProvider),
                                MinimumScore = SettingsValues.GetFPDefaultScore(),
                                MaximumRetry = "2"
                            }
                        });
                    }
                    else
                    {
                        return Ok(new CheckUserResponseModel()
                        {
                            isError = true,
                            message = dBResponseModel.message,
                            data = new RespData()
                            {
                                is_fp_validation_need = dBResponseModel.is_fp_validation_need == 1 ? true : false,
                                is_registered = dBResponseModel.is_registered == 1 ? true : false,
                                msisdn = dBResponseModel.msisdn,
                                SessionToken = "",
                                MinimumScore = SettingsValues.GetFPDefaultScore(),
                                MaximumRetry = "2"
                            }
                        });
                    }
                }
                else
                {
                    return Ok(new CheckUserResponseModel()
                    {
                        isError = true,
                        message = "User Checking API Response Data is not valid. Please try again.",
                        data = new RespData()
                        {
                            is_fp_validation_need = false,
                            is_registered = false,
                            msisdn = "",
                            SessionToken = "",
                            MinimumScore = SettingsValues.GetFPDefaultScore(),
                            MaximumRetry = "2"
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                return Ok(new CheckUserResponseModel()
                {
                    isError = true,
                    message = ex.InnerException?.Message ?? ex.Message,
                    data = new RespData()
                    {
                        is_fp_validation_need = false,
                        is_registered = false,
                        msisdn = "",
                        SessionToken = ""
                    }
                });
            }
        }

        [HttpPost]
        [Route("validate-fp-new-failed-attempts")]
        public async Task<IActionResult> ValidateFPV2([FromBody][Bind("BPMSISDN,cid,deviceId,device_model,fermware_version,finger_print,kernel_version,lac,lan,latitude,longitude,os_version,session_token,user_name,version_code,version_name")] FPValidationReqModel model)
        {
            ValidTokenResponse security = new ValidTokenResponse();
            FPDataResponse dBResponseModel = new FPDataResponse();
            FP_Matching fP_Matching = new FP_Matching();
            string secreteKey = SettingsValues.GetJWTSequrityKey();
            TokenService token = new TokenService(secreteKey);
            int currentAttempt = 0;
            int minutesLeft = 0;
            string message2 = string.Empty;

            try
            {
                byte[] decryptedFingerprint = Convert.FromBase64String(model.finger_print);
                //decryptedFingerprint = AESCryptography.Decrypts(decryptedFingerprint);
                decryptedFingerprint = AESCryptography.DecryptFingerData(decryptedFingerprint);

                dBResponseModel = await _bLLUser.FetchFingerPrint(model);
                List<byte[]> decryptedStoredFingerprints = ConvertToByteArrayList(dBResponseModel);

                bool isMatch = fP_Matching.MatchFingerprint(decryptedFingerprint, decryptedStoredFingerprints);

                if (!isMatch)
                {
                    (currentAttempt, minutesLeft, message2) = await _bLLUser.UserLoginAttemptCount(model.user_name, 0);

                    if (!string.IsNullOrEmpty(message2) && message2.StartsWith("Invalid user credentials. User will be locked after"))
                    {
                        var parts = message2.Split("locked after");
                        string count = null;

                        if (parts.Length > 1)
                        {
                            string remaining = parts[1].Trim();
                            count = remaining.Split(' ')[0];    
                        }
                        return Ok(new FPLoginResponse
                        {
                            is_error = true,
                            message = $"Fingerprint not matched! User will be locked after {count} more attempt(s)",
                            data = new LogInResponse
                            {
                                SessionToken = "",
                                ISAuthenticate = false,
                                AuthenticationMessage = $"Fingerprint not matched! User will be locked after {count} more attempt(s)",
                                UserName = "",
                                Password = "",
                                DeviceId = "",
                                HasUpdate = false,
                                MinimumScore = "",
                                OptionalMinimumScore = "0",
                                MaximumRetry = "0",
                                RoleAccess = "",
                                ChannelId = 0,
                                ChannelName = "",
                                InventoryId = 0,
                                CenterCode = "",
                                itopUpNumber = "",
                                is_default_Password = 0,
                                ExpiredDate = "",
                                Designation = "",
                                is_etsaf_validation_need = 0
                            }
                        });
                    }
                    else if (minutesLeft > 0)
                    {
                        return Ok(new FPLoginResponse
                        {
                            is_error = true,
                            message = message2,
                            data = new LogInResponse
                            {
                                SessionToken = "",
                                ISAuthenticate = false,
                                AuthenticationMessage = message2,
                                UserName = "",
                                Password = "",
                                DeviceId = "",
                                HasUpdate = false,
                                MinimumScore = "",
                                OptionalMinimumScore = "0",
                                MaximumRetry = "0",
                                RoleAccess = "",
                                ChannelId = 0,
                                ChannelName = "",
                                InventoryId = 0,
                                CenterCode = "",
                                itopUpNumber = "",
                                is_default_Password = 0,
                                ExpiredDate = "",
                                Designation = "",
                                is_etsaf_validation_need = 0
                            }
                        });
                    }
                    else
                    {
                        return Ok(new FPLoginResponse
                        {
                            is_error = true,
                            message = "Fingerprint not matched! Please try again.",
                            data = new LogInResponse
                            {
                                SessionToken = "",
                                ISAuthenticate = false,
                                AuthenticationMessage = "Fingerprint not matched! Please try again.",
                                UserName = "",
                                Password = "",
                                DeviceId = "",
                                HasUpdate = false,
                                MinimumScore = "",
                                OptionalMinimumScore = "0",
                                MaximumRetry = "0",
                                RoleAccess = "",
                                ChannelId = 0,
                                ChannelName = "",
                                InventoryId = 0,
                                CenterCode = "",
                                itopUpNumber = "",
                                is_default_Password = 0,
                                ExpiredDate = "",
                                Designation = "",
                                is_etsaf_validation_need = 0
                            }
                        });
                    }     
                }

                LoginUserInfoResponseRev user = await _bLLUser.ValidateUserV3(model);
                string loginProvider = Guid.NewGuid().ToString();

                (currentAttempt, minutesLeft, message2) = await _bLLUser.UserLoginAttemptCount(model.user_name, 1);
                if (minutesLeft > 0)
                {
                    return Ok(new FPLoginResponse
                    {
                        is_error = true,
                        message = message2,
                        data = new LogInResponse
                        {
                            SessionToken = "",
                            ISAuthenticate = false,
                            AuthenticationMessage = message2,
                            UserName = "",
                            Password = "",
                            DeviceId = "",
                            HasUpdate = false,
                            MinimumScore = "",
                            OptionalMinimumScore = "0",
                            MaximumRetry = "0",
                            RoleAccess = "",
                            ChannelId = 0,
                            ChannelName = "",
                            InventoryId = 0,
                            CenterCode = "",
                            itopUpNumber = "",
                            is_default_Password = 0,
                            ExpiredDate = "",
                            Designation = "",
                            is_etsaf_validation_need = 0
                        }
                    });
                }

                if (string.IsNullOrEmpty(model.BPMSISDN))
                {
                    var loginAtmInfo = new UserLogInAttemptV2
                    {
                        userid = user != null ? user.user_id : "",
                        is_success = user != null && user.isValidUser == 1 ? 1 : 0,
                        ip_address = GetIP(),
                        loginprovider = loginProvider,
                        deviceid = model.deviceId,
                        lan = model.lan ?? "",
                        versioncode = model.version_code,
                        versionname = model.version_name,
                        osversion = model.os_version,
                        kernelversion = model.kernel_version,
                        fermwarevirsion = model.fermware_version,
                        latitude = model.latitude,
                        longitude = model.longitude,
                        lac = model.lac,
                        cid = model.cid,
                        is_bp = 0,
                        bp_msisdn = model.BPMSISDN ?? "",
                        device_model = model.device_model
                    };

                    if (user != null && !string.IsNullOrEmpty(user.user_id))
                    {
                        await _bLLUser.SaveLoginAtmInfoV2(loginAtmInfo);
                    }

                    return Ok(new FPLoginResponse
                    {
                        is_error = user == null || user.isValidUser == 0,
                        message = user?.message ?? "",
                        data = new LogInResponse
                        {
                            SessionToken = user != null && user.isValidUser == 1 ? token.GenerateToken(user, loginProvider) : "",
                            ISAuthenticate = user != null && user.isValidUser == 1,
                            AuthenticationMessage = user?.message ?? "",
                            UserName = model.user_name,
                            Password = "******",
                            DeviceId = model.deviceId,
                            HasUpdate = false,
                            MinimumScore = SettingsValues.GetFPDefaultScore(),
                            OptionalMinimumScore = "30",
                            MaximumRetry = "2",
                            RoleAccess = user?.role_access ?? "",
                            ChannelId = user?.channel_id ?? 0,
                            ChannelName = user?.channel_name ?? "",
                            InventoryId = user?.inventory_id ?? 0,
                            CenterCode = user?.center_code ?? "",
                            itopUpNumber = user?.itopUpNumber ?? "",
                            is_default_Password = user?.is_default_Password ?? 0,
                            ExpiredDate = user?.ExpiredDate ?? "",
                            Designation = user?.designation ?? "",
                            is_etsaf_validation_need = SettingsValues.GetETSAFValidationValue()
                        }
                    });
                }
                else
                {
                    string bp_msisdn = ConverterHelper.MSISDNCountryCodeAddition(model.BPMSISDN, FixedValueCollection.MSISDNCountryCode);
                    BPUserValidationResponse bPUserValidationResponse = await _bLLUser.ValidateBPUserV1(model.BPMSISDN, model.user_name);

                    if (!bPUserValidationResponse.is_valid)
                    {
                        return Ok(new FPLoginResponse
                        {
                            is_error = true,
                            message = bPUserValidationResponse.err_msg,
                            data = new LogInResponse
                            {
                                SessionToken = "",
                                ISAuthenticate = false,
                                AuthenticationMessage = bPUserValidationResponse.err_msg,
                                UserName = "",
                                Password = "",
                                DeviceId = "",
                                HasUpdate = false,
                                MinimumScore = "",
                                OptionalMinimumScore = "0",
                                MaximumRetry = "0",
                                RoleAccess = "",
                                ChannelId = 0,
                                ChannelName = "",
                                InventoryId = 0,
                                CenterCode = "",
                                itopUpNumber = "",
                                is_default_Password = 0,
                                ExpiredDate = "",
                                Designation = "",
                                is_etsaf_validation_need = 0
                            }
                        });
                    }

                    var loginAtmInfo = new UserLogInAttemptV2
                    {
                        userid = user != null ? user.user_id : "",
                        is_success = user != null && user.isValidUser == 1 ? 1 : 0,
                        ip_address = GetIP(),
                        loginprovider = loginProvider,
                        deviceid = model.deviceId,
                        lan = model.lan ?? "",
                        versioncode = model.version_code,
                        versionname = model.version_name,
                        osversion = model.os_version,
                        kernelversion = model.kernel_version,
                        fermwarevirsion = model.fermware_version,
                        latitude = model.latitude,
                        longitude = model.longitude,
                        lac = model.lac,
                        cid = model.cid,
                        is_bp = 1,
                        bp_msisdn = model.BPMSISDN,
                        device_model = model.device_model
                    };

                    if (user != null && !string.IsNullOrEmpty(user.user_id))
                    {
                        await _bLLUser.SaveLoginAtmInfoV2(loginAtmInfo);
                    }

                    await _bLLUser.GenerateBPLoginOTPV2(loginProvider);

                    return Ok(new FPLoginResponse
                    {
                        is_error = user == null || !bPUserValidationResponse.is_valid,
                        message = user?.message ?? "",
                        data = new LogInResponse
                        {
                            SessionToken = user != null && user.isValidUser == 1 ? token.GenerateToken(user, loginProvider) : "",
                            ISAuthenticate = user != null && user.isValidUser == 1,
                            AuthenticationMessage = user?.message ?? "",
                            UserName = model.user_name,
                            Password = "******",
                            DeviceId = model.deviceId,
                            HasUpdate = false,
                            MinimumScore = SettingsValues.GetFPDefaultScore(),
                            OptionalMinimumScore = "30",
                            MaximumRetry = "2",
                            RoleAccess = user?.role_access ?? "",
                            ChannelId = user?.channel_id ?? 0,
                            ChannelName = user?.channel_name ?? "",
                            InventoryId = user?.inventory_id ?? 0,
                            CenterCode = user?.center_code ?? "",
                            itopUpNumber = user?.itopUpNumber ?? "",
                            is_default_Password = user?.is_default_Password ?? 0,
                            ExpiredDate = user?.ExpiredDate ?? "",
                            Designation = user?.designation ?? "",
                            is_etsaf_validation_need = SettingsValues.GetETSAFValidationValue()
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                return Ok(new FPLoginResponse
                {
                    is_error = true,
                    message = ex.InnerException?.Message ?? ex.Message,
                    data = new LogInResponse
                    {
                        SessionToken = "",
                        ISAuthenticate = false,
                        AuthenticationMessage = ex.InnerException?.Message ?? "",
                        UserName = "",
                        Password = "",
                        DeviceId = "",
                        HasUpdate = false,
                        MinimumScore = "",
                        OptionalMinimumScore = "0",
                        MaximumRetry = "0",
                        RoleAccess = "",
                        ChannelId = 0,
                        ChannelName = "",
                        InventoryId = 0,
                        CenterCode = "",
                        itopUpNumber = "",
                        is_default_Password = 0,
                        ExpiredDate = "",
                        Designation = "",
                        is_etsaf_validation_need = 0
                    }
                });
            }
        }

        [HttpPost]
        [Route("validate-fp")]
        public async Task<IActionResult> ValidateFP([FromBody][Bind("BPMSISDN,cid,deviceId,device_model,fermware_version,finger_print,kernel_version,lac,lan,latitude,longitude,os_version,session_token,user_name,version_code,version_name")] FPValidationReqModel model)
        {
            ValidTokenResponse security = new ValidTokenResponse();
            FPDataResponse dBResponseModel = new FPDataResponse();
            FP_Matching fP_Matching = new FP_Matching();
            string secreteKey = SettingsValues.GetJWTSequrityKey();
            TokenService token = new TokenService(secreteKey);
            try
            {
               

                byte[] decryptedFingerprint = Convert.FromBase64String(model.finger_print);
                //decryptedFingerprint = AESCryptography.Decrypts(decryptedFingerprint);
                decryptedFingerprint = AESCryptography.DecryptFingerData(decryptedFingerprint);

                dBResponseModel = await _bLLUser.FetchFingerPrint(model);
                List<byte[]> decryptedStoredFingerprints = ConvertToByteArrayList(dBResponseModel);

                bool isMatch = fP_Matching.MatchFingerprint(decryptedFingerprint, decryptedStoredFingerprints);

                if (!isMatch)
                {
                    
                    return Ok(new FPLoginResponse
                    {
                        is_error = true,
                        message = "Fingerprint not matched! Please try again.",
                        data = new LogInResponse
                        {
                            SessionToken = "",
                            ISAuthenticate = false,
                            AuthenticationMessage = "Fingerprint not matched! Please try again.",
                            UserName = "",
                            Password = "",
                            DeviceId = "",
                            HasUpdate = false,
                            MinimumScore = "",
                            OptionalMinimumScore = "0",
                            MaximumRetry = "0",
                            RoleAccess = "",
                            ChannelId = 0,
                            ChannelName = "",
                            InventoryId = 0,
                            CenterCode = "",
                            itopUpNumber = "",
                            is_default_Password = 0,
                            ExpiredDate = "",
                            Designation = "",
                            is_etsaf_validation_need = 0
                        }
                    });
                }

                LoginUserInfoResponseRev user = await _bLLUser.ValidateUserV3(model);
                string loginProvider = Guid.NewGuid().ToString();

                if (string.IsNullOrEmpty(model.BPMSISDN))
                {
                    var loginAtmInfo = new UserLogInAttemptV2
                    {
                        userid = user != null ? user.user_id : "",
                        is_success = user != null && user.isValidUser == 1 ? 1 : 0,
                        ip_address = GetIP(),
                        loginprovider = loginProvider,
                        deviceid = model.deviceId,
                        lan = model.lan ?? "",
                        versioncode = model.version_code,
                        versionname = model.version_name,
                        osversion = model.os_version,
                        kernelversion = model.kernel_version,
                        fermwarevirsion = model.fermware_version,
                        latitude = model.latitude,
                        longitude = model.longitude,
                        lac = model.lac,
                        cid = model.cid,
                        is_bp = 0,
                        bp_msisdn = model.BPMSISDN ?? "",
                        device_model = model.device_model,
                        machine_name="FP"
                    };

                    if (user != null && !string.IsNullOrEmpty(user.user_id))
                    {
                        await _bLLUser.SaveLoginAtmInfoV2(loginAtmInfo);
                    }

                    return Ok(new FPLoginResponse
                    {
                        is_error = user == null || user.isValidUser == 0,
                        message = user?.message ?? "",
                        data = new LogInResponse
                        {
                            SessionToken = user != null && user.isValidUser == 1 ? token.GenerateToken(user, loginProvider) : "",
                            ISAuthenticate = user != null && user.isValidUser == 1,
                            AuthenticationMessage = user?.message ?? "",
                            UserName = model.user_name,
                            Password = "******",
                            DeviceId = model.deviceId,
                            HasUpdate = false,
                            MinimumScore = SettingsValues.GetFPDefaultScore(),
                            OptionalMinimumScore = "30",
                            MaximumRetry = "2",
                            RoleAccess = user?.role_access ?? "",
                            ChannelId = user?.channel_id ?? 0,
                            ChannelName = user?.channel_name ?? "",
                            InventoryId = user?.inventory_id ?? 0,
                            CenterCode = user?.center_code ?? "",
                            itopUpNumber = user?.itopUpNumber ?? "",
                            is_default_Password = user?.is_default_Password ?? 0,
                            ExpiredDate = user?.ExpiredDate ?? "",
                            Designation = user?.designation ?? "",
                            is_etsaf_validation_need = SettingsValues.GetETSAFValidationValue()
                        }
                    });
                }
                else
                {
                    string bp_msisdn = ConverterHelper.MSISDNCountryCodeAddition(model.BPMSISDN, FixedValueCollection.MSISDNCountryCode);
                    BPUserValidationResponse bPUserValidationResponse = await _bLLUser.ValidateBPUserV1(model.BPMSISDN, model.user_name);

                    if (!bPUserValidationResponse.is_valid)
                    {
                        return Ok(new FPLoginResponse
                        {
                            is_error = true,
                            message = bPUserValidationResponse.err_msg,
                            data = new LogInResponse
                            {
                                SessionToken = "",
                                ISAuthenticate = false,
                                AuthenticationMessage = bPUserValidationResponse.err_msg,
                                UserName = "",
                                Password = "",
                                DeviceId = "",
                                HasUpdate = false,
                                MinimumScore = "",
                                OptionalMinimumScore = "0",
                                MaximumRetry = "0",
                                RoleAccess = "",
                                ChannelId = 0,
                                ChannelName = "",
                                InventoryId = 0,
                                CenterCode = "",
                                itopUpNumber = "",
                                is_default_Password = 0,
                                ExpiredDate = "",
                                Designation = "",
                                is_etsaf_validation_need = 0
                            }
                        });
                    }

                    var loginAtmInfo = new UserLogInAttemptV2
                    {
                        userid = user != null ? user.user_id : "",
                        is_success = user != null && user.isValidUser == 1 ? 1 : 0,
                        ip_address = GetIP(),
                        loginprovider = loginProvider,
                        deviceid = model.deviceId,
                        lan = model.lan ?? "",
                        versioncode = model.version_code,
                        versionname = model.version_name,
                        osversion = model.os_version,
                        kernelversion = model.kernel_version,
                        fermwarevirsion = model.fermware_version,
                        latitude = model.latitude,
                        longitude = model.longitude,
                        lac = model.lac,
                        cid = model.cid,
                        is_bp = 1,
                        bp_msisdn = model.BPMSISDN,
                        device_model = model.device_model,
                        machine_name= "FP"
                    };

                    if (user != null && !string.IsNullOrEmpty(user.user_id))
                    {
                        await _bLLUser.SaveLoginAtmInfoV2(loginAtmInfo);
                    }

                    await _bLLUser.GenerateBPLoginOTPV2(loginProvider);

                    return Ok(new FPLoginResponse
                    {
                        is_error = user == null || !bPUserValidationResponse.is_valid,
                        message = user?.message ?? "",
                        data = new LogInResponse
                        {
                            SessionToken = user != null && user.isValidUser == 1 ? token.GenerateToken(user, loginProvider) : "",
                            ISAuthenticate = user != null && user.isValidUser == 1,
                            AuthenticationMessage = user?.message ?? "",
                            UserName = model.user_name,
                            Password = "******",
                            DeviceId = model.deviceId,
                            HasUpdate = false,
                            MinimumScore = SettingsValues.GetFPDefaultScore(),
                            OptionalMinimumScore = "30",
                            MaximumRetry = "2",
                            RoleAccess = user?.role_access ?? "",
                            ChannelId = user?.channel_id ?? 0,
                            ChannelName = user?.channel_name ?? "",
                            InventoryId = user?.inventory_id ?? 0,
                            CenterCode = user?.center_code ?? "",
                            itopUpNumber = user?.itopUpNumber ?? "",
                            is_default_Password = user?.is_default_Password ?? 0,
                            ExpiredDate = user?.ExpiredDate ?? "",
                            Designation = user?.designation ?? "",
                            is_etsaf_validation_need = SettingsValues.GetETSAFValidationValue()
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                return Ok(new FPLoginResponse
                {
                    is_error = true,
                    message = ex.InnerException?.Message ?? ex.Message,
                    data = new LogInResponse
                    {
                        SessionToken = "",
                        ISAuthenticate = false,
                        AuthenticationMessage = ex.InnerException?.Message ?? "",
                        UserName = "",
                        Password = "",
                        DeviceId = "",
                        HasUpdate = false,
                        MinimumScore = "",
                        OptionalMinimumScore = "0",
                        MaximumRetry = "0",
                        RoleAccess = "",
                        ChannelId = 0,
                        ChannelName = "",
                        InventoryId = 0,
                        CenterCode = "",
                        itopUpNumber = "",
                        is_default_Password = 0,
                        ExpiredDate = "",
                        Designation = "",
                        is_etsaf_validation_need = 0
                    }
                });
            }
        }

        private string GetIP()
        {
            var feature = HttpContext?.Features?.Get<IHttpConnectionFeature>();

            if (feature?.LocalIpAddress != null)
            {
                return feature.LocalIpAddress.ToString();
            }

            return string.Empty;
        }

        [HttpPost]
        [Route("fp-registration")]
        public async Task<IActionResult> RegisterWithFP([FromBody][Bind("left_index,left_index_score,left_thumb,left_thumb_score,mobile_no,right_index,right_index_score,right_thumb,right_thumb_score,session_token,user_name")] FPRegistrationRequestModel request)
        {
            long? result = 0;
            ValidTokenResponse security = new ValidTokenResponse();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            FPRegistrationModel model = new FPRegistrationModel();
            try
            {
                model = populateModel.FPRegistrationRequestPopulateModel(request);

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateTokenV2(model.session_token);

                if (security != null)
                {
                    if (security.IsVallid == true)
                    {
                        if (!model.user_name.Equals(security.UserName))
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
                result = await _bLLUser.SaveFingerPrint(model);

                if (result != null && result == 1)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = false,
                        message = "Successfully registered!",
                        data = new Datas()
                        {

                        }
                    });
                }
                else
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "Database error!",
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
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
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = ex.InnerException?.Message ?? ex.Message,
                    data = new Datas()
                    {
                        isEsim = 0,
                        request_id = "0"
                    }
                });
            }
        }

        [HttpPost]
        [Route("ec-verification")]
        public async Task<IActionResult> FPRegisterECVerification([FromBody][Bind("channel_name,dest_dob,dest_left_index,dest_left_index_score,dest_left_thumb,dest_left_thumb_score,dest_nid,dest_right_index,dest_right_index_score,dest_right_thumb,dest_right_thumb_score,msisdn,purpose_number,retailer_id,right_id,session_token")] FPRegistrationEcVerificationRequestModel request)
        {
            SendOrderResponseRev orderRes = new SendOrderResponseRev();
            SendOrderResponse2 response2 = new SendOrderResponse2();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BioVerifyResp verifyResp = new BioVerifyResp();
            ModelValidation modelValidation = new ModelValidation();
            ValidTokenResponse security = new ValidTokenResponse();
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            FP_Get_Status status = new FP_Get_Status();
            RetailerNIDDOBRespModel respModel = new RetailerNIDDOBRespModel();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            RAOrderRequestV2 model = new RAOrderRequestV2();
            try
            {
                model = populateModel.FPRegistrationEcVerificationRequest(request);

                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateTokenV2(model.session_token);

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
                log.req_time = DateTime.Now;
                #region request_blob

                RequiestModelCasting modelCasting = new RequiestModelCasting();
                RequestModelBLOBConversion requestModelBLOB = new RequestModelBLOBConversion();

                requestModelBLOB = modelCasting.CastRequestModel(model);

                log.req_blob = await _blJson.GetGenericJsonDataAsync(requestModelBLOB);

                #endregion

                //======Only for POC & TwoParty Varification (purpose: SIM Transfer(5), B2C to B2B(11)): MSISDN = POC_NUMBER======= 

                if (model.msisdn.Substring(0, 2) != FixedValueCollection.MSISDNCountryCode)
                {
                    model.msisdn = FixedValueCollection.MSISDNCountryCode + model.msisdn;
                }
                model.poc_msisdn_number = model.msisdn;

                string isUserNameExist = await _bLLUser.GetIsRegistered(model.retailer_id);

                if (!String.IsNullOrEmpty(isUserNameExist))
                {
                    orderRes.message = "User already registered with Fingerprint!";

                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "User already registered with Fingerprint!",
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });

                }

                respModel = await _bLLUser.GetRetailerNIDDOB(model.retailer_id);

                if (respModel == null || String.IsNullOrEmpty(respModel.DOB) || String.IsNullOrEmpty(respModel.NID))
                {
                    orderRes.message = "User information (NID/DOB) not found!";

                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "User information (NID/DOB) not found!",
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }
                else
                {
                    model.dest_dob = respModel.DOB;
                    model.dest_nid = respModel.NID;
                    model.channel_name = "RESELLER";
                    model.right_id = 88;
                    model.purpose_number = "2";
                    model.distributor_code = respModel.ddCode;
                }

                if (!String.IsNullOrEmpty(model.poc_msisdn_number)
                && Convert.ToInt16(model.purpose_number).Equals((int)EnumPurposeNumber.SIMReplacement))
                {
                    model.sim_replacement_type = (int)EnumSIMReplacementType.BulkSIMReplacment;
                }
                #region Insert_Order
                model.status = (int)EnumRAOrderStatus.RequestSubmitted;
                model.order_booking_flag = 800;
                int isRegisteredReq = 1;
                model.prov_id = loginProviderId;
                orderRes = await _orderManager.SubmitOrderForRegistration(model, isRegisteredReq);

                if (orderRes.isError)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = orderRes.message,
                        data = new Datas()
                        {
                            isEsim = 0,
                            request_id = "0"
                        }
                    });
                }
                else
                {
                    try
                    {
                        model.bi_token_number = Convert.ToDouble(orderRes.data.request_id);
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
                        Log.Error(ex, "ExMessage");
                        ErrorDescription errorDescription = new ErrorDescription();
                        model.status = (int)EnumRAOrderStatus.Failed;
                        errorDescription = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                        model.err_code = errorDescription.error_code;
                        model.error_id = errorDescription.error_id;
                        orderRes.message = String.IsNullOrEmpty(errorDescription.error_custom_msg) ? errorDescription.error_description : errorDescription.error_custom_msg;
                        model.err_msg = String.IsNullOrEmpty(errorDescription.error_custom_msg) ? errorDescription.error_description : errorDescription.error_custom_msg;
                        orderRes.isError = true;
                        return Ok(new RACommonResponseRevamp()
                        {
                            isError = orderRes.isError,
                            message = orderRes.message,
                            data = new Datas()
                            {
                                isEsim = 0,
                                request_id = orderRes.data != null ? orderRes.data.request_id : "0",
                            }
                        });
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
                ErrorDescription error;
                log.is_success = 0;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

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
                            request_id = ""
                        };
                    }
                    orderRes.isError = true;
                    orderRes.message = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
                    model.err_msg = String.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;

                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                }
                catch (Exception ex2)
                {
                    orderRes.isError = true;
                    orderRes.message = ex.InnerException?.Message ?? ex2.Message;
                    model.err_msg = ex.InnerException?.Message ?? ex2.Message;

                    log.res_time = DateTime.Now;
                    log.res_blob = _blJson.GetGenericJsonData(orderRes);
                }
            }
            finally
            {
                log.purpose_number = model.purpose_number;
                log.msisdn = _bllLog.FormatMSISDN(model.msisdn);
                log.req_time = DateTime.Now;
                if (model.bi_token_number != null && model.bi_token_number > 1)
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
                if (orderRes.data != null)
                {
                    log.is_success = orderRes.data.request_id.Length > 1 ? 1 : 0;
                    log.bi_token_number = orderRes.data.request_id;
                }
                log.res_time = DateTime.Now;
                log.res_blob = _blJson.GetGenericJsonData(orderRes);
                log.method_name = "FPRegisterECVerification";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;
                log.remarks = model.bi_token_number != null && model.bi_token_number > 1 ? "Resubmit order" : String.Empty;

                await _bllLog.RAToDBSSLog(log);

                await Task.Delay(4000);

                status = await _bLLUser.FetchFingerPrintResult(model.bi_token_number);

                if (status.status != 0 && status.status == 30)
                {
                    orderRes.isError = false;
                    orderRes.message = "EC Verification Successfull";
                }
                else if (status.status == 150)
                {
                    orderRes.isError = true;
                    orderRes.message = status.message;
                }
                else
                {
                    orderRes.isError = true;
                    orderRes.message = "EC Verification failed, response is invalid from EC/DBSS.";
                }
            }
            return Ok(new RACommonResponseRevamp()
            {
                isError = orderRes.isError,
                message = orderRes.message,
                data = new Datas()
                {
                    isEsim = 0,
                    request_id = orderRes.data != null ? orderRes.data.request_id : "0",
                }
            });
        }

        public BiomerticDataModel bioverifyDataMapp(OrderRequest2 order)
        {
            BiomerticDataModel resp = new BiomerticDataModel();
            resp.status = order.status;
            resp.create_date = DateTime.Now.ToString();
            if (order.purpose_number != null)
                resp.purpose_number = (int)order.purpose_number;
            if (order.dest_doc_type_no != null)
                resp.dest_doc_type_no = order.dest_doc_type_no.ToString()??"";
            if (!String.IsNullOrEmpty(order.dest_nid))
                resp.dest_doc_id = order.dest_nid;
            resp.user_id = order.retailer_id;
            resp.msisdn = order.msisdn;
            if (order.dest_ec_verifi_reqrd != null)
                resp.dest_ec_verification_required = (int)order.dest_ec_verifi_reqrd;
            if (!String.IsNullOrEmpty(order.dest_imsi))
                resp.dest_imsi = order.dest_imsi;
            if (order.dest_foreign_flag != null)
                resp.dest_foreign_flag = (int)order.dest_foreign_flag;
            if (order.sim_category != null)
            {
                resp.sim_category = (int)order.sim_category;
            }
            else
            {
                resp.sim_category = 0;
            }
            resp.dest_dob = order.dest_dob;
            resp.dest_left_thumb = order.dest_left_thumb;
            resp.dest_left_index = order.dest_left_index;
            resp.dest_right_thumb = order.dest_right_thumb;
            resp.dest_right_index = order.dest_right_index;

            if (!String.IsNullOrEmpty(order.poc_number))
                resp.poc_number = order.poc_number;
            if (order.sim_replacement_type != null)
                resp.sim_replacement_type = (int)order.sim_replacement_type;
            if (order.src_doc_type_no != null)
                resp.src_doc_type_no = order.src_doc_type_no.ToString() ?? "";

            return resp;
        }
               
        private List<byte[]> ConvertToByteArrayList(FPDataResponse response)
        {
            var byteArrayList = new List<byte[]>();

            if (response.right_thumb != null)
            {
                byte[] thumb = Convert.FromBase64String(response.right_thumb);
                byteArrayList.Add(AESCryptography.DecryptFingerData(thumb));
            }
            if (response.right_index != null)
            {
                byte[] thumb_index = Convert.FromBase64String(response.right_index);
                byteArrayList.Add(AESCryptography.DecryptFingerData(thumb_index));
            }
            if (response.left_thumb != null)
            {
                byte[] left_thumb = Convert.FromBase64String(response.left_thumb);
                byteArrayList.Add(AESCryptography.DecryptFingerData(left_thumb));
            }
            if (response.left_index != null)
            {
                byte[] left_index = Convert.FromBase64String(response.left_index);
                byteArrayList.Add(AESCryptography.DecryptFingerData(left_index));
            }

            return byteArrayList;
        }
        
        [HttpPost]
        [Route("removeFingerprint")]
        [CustomAuthorizationFilter]

        public async Task<IActionResult> RemoveFingerprint([FromBody][Bind("retailer_id")] RemoveFPRequestModel model)
        {
            var finalResponse = new FPDeleteResponseModel
            {
                isError = false,
                message = "Completed",
                data = new FPDeletedData()
            };

            try
            {
                if (model == null || model.retailer_id == null || !model.retailer_id.Any())
                {
                    finalResponse.isError = true;
                    finalResponse.message = "No retailer found in request body!";
                    return Ok(finalResponse);
                }

                foreach (var retailerId in model.retailer_id)
                {
                    if (string.IsNullOrWhiteSpace(retailerId))
                    {
                        finalResponse.data.failedRetailers.Add(new FailedRetailer
                        {
                            retailer_id = retailerId,
                            error_message = "Retailer ID is empty"
                        });
                        continue;
                    }

                    var response = await _bLLUser.RemoveFPFromLocal(retailerId);

                    if (response == null || !response.isError)
                    {
                        finalResponse.data.failedRetailers.Add(new FailedRetailer
                        {
                            retailer_id = retailerId,
                            error_message = response?.details_message ?? "Fingerprint deletion failed"
                        });
                    }
                    else
                    {
                        finalResponse.data.successRetailers.Add(retailerId);
                    }
                }
                if (finalResponse.data.successRetailers.Any() && !finalResponse.data.failedRetailers.Any())
                {
                    finalResponse.message = "Successfully Completed";
                }
                else if (finalResponse.data.failedRetailers.Any() && finalResponse.data.successRetailers.Any())
                {
                    finalResponse.message = "Partially Completed";
                }
                else if (finalResponse.data.failedRetailers.Any() && !finalResponse.data.successRetailers.Any())
                {
                    finalResponse.message = "No retailer FP found in the system";
                }

                return Ok(finalResponse);
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

                return Ok(new FPDeleteResponseModel
                {
                    isError = true,
                    message = "Failed",
                    data = new FPDeletedData
                    {
                        failedRetailers = new List<FailedRetailer>
                {
                    new FailedRetailer
                    {
                        retailer_id = "SYSTEM",
                        error_message = error.error_custom_msg ?? error.error_description
                    }
                }
                    }
                });
            }
        }


        //public async Task<IActionResult> RemoveFingerprint([FromBody][Bind("retailer_id")] RemoveFPRequestModel model)
        //{
        //    var finalResponse = new FPDeleteResponseModel
        //    {
        //        isError = false,
        //        message = "Completed",
        //        data = new List<FPDeletedData>()
        //    };

        //    try
        //    {
        //        if (model == null || model.retailer_id.Count == 0)
        //        {
        //            finalResponse.isError = true;
        //            finalResponse.message = "No retailer found in request body!";
        //            return Ok(finalResponse);
        //        }

        //        foreach (var item in model.retailer_id)
        //        {
        //            if (!String.IsNullOrEmpty(item))
        //            {
        //                var response = await _bLLUser.RemoveFPFromLocal(item);

        //                if (response != null && response.isError == false)
        //                {
        //                    finalResponse.data.failedRetailers.Add(item);
        //                }
        //            }
        //            else
        //            {
        //                finalResponse.data.failedRetailers.Add(item);
        //            }                    
        //        }

        //        if (finalResponse.data.failedRetailers.Any())
        //        {
        //            finalResponse.message = "Partially Completed";
        //        }

        //        return Ok(finalResponse);
        //    }
        //    catch (UnauthorizedAccessException ex)
        //    {
        //        return Unauthorized(new
        //        {
        //            isError = true,
        //            message = ex.Message
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

        //        return Ok(new FPDeleteResponseModel
        //        {
        //            isError = true,
        //            message = "Failed",
        //            data = new FPDeletedData
        //            {
        //                message = error.error_custom_msg ?? error.error_description
        //            }
        //        });
        //    }
        //}

    }
}
