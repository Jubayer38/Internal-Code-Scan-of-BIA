///************************************************************************
///	|| Creation History ||
///-----------------------------------------------------------------------
///	Copyright     :	Copyright© NAAS Solutions Limited. All rights reserved.
///	Author	      :	Mohiuddin
///	Purpose	      :	For Authentication for all channel from Biometric App, DBSS and DMS also credential changes happen from this controller
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
using BIA.Entity.ENUM;
using BIA.Entity.PopulateModel;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Entity.Utility;
using BIA.Entity.ViewModel;
using BIA.Helper;
using BIA.JWET;
using BIA.JWT;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace BIA.Controllers
{
    [Route("api/Security")]
    [ApiController]
    public class SecurityController : ControllerBase
    {
        private readonly BLLUserAuthenticaion _bLLUserAuthenticaion;
        private readonly BLLLog _bllLog;
        private readonly BaseController _bio;
        private readonly ApiManager _apiManager;

        public SecurityController(BLLUserAuthenticaion auth, BLLLog bllLog, BaseController bio, ApiManager apiManager)
        {
            _bLLUserAuthenticaion = auth;
            _bllLog = bllLog;
            _bio = bio;
            _apiManager = apiManager;
        }
        // POST: api/Security/Login
        /// <summary>
        /// Authentication API for external user. ***Single user login.***
        /// </summary>
        /// <param name="loginInfo">Requesting parameter with username and password</param>
        /// <returns>Return the authentication information of requesting user</returns>        
        //[ValidateModel]
        //[Route("LoginV1")]
        //public async Task<IActionResult> LoginAsyncV1([FromBody][Bind("DeviceId,FermwareVersion,KernelVersion,Lan,OSVersion,Password,Type,UserName,VersionCode,VersionName")] LoginRequests login)
        //{
        //    string encriptedPwd = Cryptography.Encrypt(login.Password, true);
        //    LoginUserInfoResponse user = await _bLLUserAuthenticaion.ValidateUser(login.UserName, encriptedPwd);

        //    if (user.user_name == null)
        //    {
        //        return Ok(new LogInResponse()
        //        {
        //            ISAuthenticate = false,
        //            AuthenticationMessage = MessageCollection.InvalidUserCridential,
        //            HasUpdate = false,
        //        });
        //    }

        //    string loginProvider = Guid.NewGuid().ToString();


        //    UserLogInAttempt loginAtmInfo = new UserLogInAttempt()
        //    {
        //        userid = user.user_id,
        //        is_success = user != null ? 1 : 0,
        //        ip_address = GetIP(),
        //        loginprovider = loginProvider,
        //        deviceid = login.DeviceId,
        //        lan = login.Lan,
        //        versioncode = login.VersionCode,
        //        versionname = login.VersionName,
        //        osversion = login.OSVersion,
        //        kernelversion = login.KernelVersion,
        //        fermwarevirsion = login.FermwareVersion,
        //        //installapps = login.InstalledApps
        //    };

        //    _bLLUserAuthenticaion.SaveLoginAtmInfo(loginAtmInfo);

        //    return Ok(new LogInResponse()
        //    {
        //        SessionToken = GetEncriptedSecurityToken(loginProvider, user != null ? user.user_id : "", user != null ? user.user_name : "", user != null ? user.distributor_code : "", login.DeviceId),
        //        ISAuthenticate = true,
        //        AuthenticationMessage = MessageCollection.UserValidted,
        //        UserName = login.UserName,
        //        Password = login.Password,
        //        DeviceId = login.DeviceId,
        //        HasUpdate = false,
        //        MinimumScore = SettingsValues.GetFPDefaultScore(),
        //        OptionalMinimumScore = "30",
        //        MaximumRetry = "2",
        //        RoleAccess = user.role_access,
        //        ChannelId = user.channel_id,
        //        ChannelName = user.channel_name
        //    });
        //}
        //private string GetIP()
        //{
        //    var feature = HttpContext.Features.Get<IHttpConnectionFeature>();
        //    string LocalIPAddr = feature?.LocalIpAddress?.ToString();

        //    if (!String.IsNullOrEmpty(LocalIPAddr))
        //    {
        //        return LocalIPAddr;
        //    }
        //    else
        //    {
        //        return "";
        //    }
        //}

        private string GetIP()
        {
            var feature = HttpContext?.Features?.Get<IHttpConnectionFeature>();

            if (feature?.LocalIpAddress != null)
            {
                return feature.LocalIpAddress.ToString();
            }

            return string.Empty;
        }

        private string GetIP_V2()
        {
            // 1. Try X-Forwarded-For header (may contain multiple IPs)
            string xForwardedFor = HttpContext?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();
            string clientIp = "";

            if (!string.IsNullOrWhiteSpace(xForwardedFor))
            {
                // If multiple IPs: client, proxy1, proxy2...
                clientIp = xForwardedFor.Split(',')[0].Trim();
            }

            // 2. Try remote source IP
            var connFeature = HttpContext?.Features?.Get<IHttpConnectionFeature>();
            string remoteIp = connFeature?.RemoteIpAddress?.ToString();

            // 3. Try local destination IP
            string localIp = connFeature?.LocalIpAddress?.ToString();

            string combineIPaddress = "";

            // Build response based on availability
            if (!string.IsNullOrWhiteSpace(remoteIp) && !string.IsNullOrWhiteSpace(localIp) && !string.IsNullOrWhiteSpace(clientIp))
            {
                combineIPaddress = $"{clientIp} -> {remoteIp} -> {localIp}";
            }
            else if (!string.IsNullOrWhiteSpace(remoteIp) && !string.IsNullOrWhiteSpace(localIp))
            {
                combineIPaddress = $"{clientIp} -> {remoteIp} -> {localIp}";
            }
            else if (!string.IsNullOrWhiteSpace(localIp))
            {
                combineIPaddress = $"{clientIp} -> {remoteIp} -> {localIp}";
            }
            else
            {
                // 4. Nothing available
                return string.Empty;
            }

            return combineIPaddress;
        }

        //public async Task<IActionResult> OnGetAsync()
        //{
        //    // Retreive server/local IP address
        //    var feature = HttpContext.Features.Get<IHttpConnectionFeature>();
        //    string LocalIPAddr = feature?.LocalIpAddress?.ToString();

        //    return Ok(LocalIPAddr);
        //}

        //=====================Multiple User Login==================
        /// <summary>
        /// Verify user and generates security token.
        /// 1. If the user logged in for first time new security token will generate.  
        /// 2. One user can login from diferrent device. 
        /// 3. If the user is already logged in, then no new security token will generate. Last time generated token will resend.  
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        //[ResponseType(typeof(LogInResponse))]
        //[GzipCompression]
        //[ValidateModel]
        //[Route("Login")]
        //public async Task<IActionResult> LoginAsyncV2([FromBody][Bind("DeviceId,FermwareVersion,KernelVersion,Lan,OSVersion,Password,Type,UserName,VersionCode,VersionName")] LoginRequests login)
        //{
        //    try
        //    {
        //        string encriptedPwd = Cryptography.Encrypt(login.Password, true);
        //        LoginUserInfoResponse user = await _bLLUserAuthenticaion.ValidateUser(login.UserName, encriptedPwd);

        //        if (user.user_name == null)
        //        {
        //            return Ok(new LogInResponse()
        //            {
        //                ISAuthenticate = false,
        //                AuthenticationMessage = MessageCollection.InvalidUserCridential,
        //                HasUpdate = false,
        //            });
        //        }



        //        #region Password Policy Checking

        //        var validationResult = await _bLLUserAuthenticaion.IsPasswordFormatValid(login.Password);

        //        if (validationResult.Item1 == false)
        //        {
        //            return Ok(new LogInResponse()
        //            {
        //                ISAuthenticate = false,
        //                AuthenticationMessage = validationResult.Item2,
        //                HasUpdate = false,
        //            });
        //        }

        //        #endregion

        //        string loginProviderId = await _bLLUserAuthenticaion.IsUserCurrentlyLoggedIn(user.user_id);

        //        UserLogInAttempt loginAtmInfo;
        //        string loginProvider = Guid.NewGuid().ToString();

        //        if (String.IsNullOrEmpty(loginProviderId))
        //        {

        //            loginAtmInfo = new UserLogInAttempt()
        //            {
        //                userid = user.user_id,
        //                is_success = user != null ? 1 : 0,
        //                ip_address = GetIP(),
        //                loginprovider = loginProvider,
        //                deviceid = login.DeviceId,
        //                lan = login.Lan,
        //                versioncode = login.VersionCode,
        //                versionname = login.VersionName,
        //                osversion = login.OSVersion,
        //                kernelversion = login.KernelVersion,
        //                fermwarevirsion = login.FermwareVersion
        //            };
        //        }
        //        else
        //        {
        //            loginProvider = loginProviderId;

        //            loginAtmInfo = new UserLogInAttempt()
        //            {
        //                userid = user.user_id,
        //                is_success = user != null ? 1 : 0,
        //                ip_address = GetIP(),
        //                loginprovider = loginProvider,
        //                deviceid = login.DeviceId,
        //                lan = login.Lan,
        //                versioncode = login.VersionCode,
        //                versionname = login.VersionName,
        //                osversion = login.OSVersion,
        //                kernelversion = login.KernelVersion,
        //                fermwarevirsion = login.FermwareVersion
        //            };
        //        }

        //        Thread logThread = new Thread(() => _bLLUserAuthenticaion.SaveLoginAtmInfo(loginAtmInfo));
        //        logThread.Start();

        //        return Ok(new LogInResponse()
        //        {
        //            SessionToken = GetEncriptedSecurityToken(loginProvider, user.user_id, user.user_name, user.distributor_code, login.DeviceId),
        //            ISAuthenticate = true,
        //            AuthenticationMessage = MessageCollection.UserValidted,
        //            UserName = login.UserName,
        //            Password = login.Password,
        //            DeviceId = login.DeviceId,
        //            HasUpdate = false,
        //            MinimumScore = SettingsValues.GetFPDefaultScore(),
        //            OptionalMinimumScore = "30",
        //            MaximumRetry = "2",
        //            RoleAccess = user.role_access,
        //            ChannelId = user.channel_id,
        //            ChannelName = user.channel_name,
        //            InventoryId = user.inventory_id,
        //            CenterCode = user.center_code
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorDescription error;

        //        try
        //        {
        //            error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

        //            return Ok(new RACommonResponse
        //            {
        //                result = false,
        //                message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
        //            });
        //        }
        //        catch (Exception)
        //        {
        //            return Ok(new RACommonResponse
        //            {
        //                result = false,
        //                message = ex.Message
        //            });
        //        }
        //    }
        //}

        ////=====================Multiple User Login including BP==================
        ///// <summary>
        ///// Verify user and generates security token.
        ///// 1. If the user logged in for first time new security token will generate.  
        ///// 2. One user can login from diferrent device. 
        ///// 3. If the user is already logged in, then no new security token will generate. Last time generated token will resend.  
        ///// </summary>
        ///// <param name="login"></param>
        ///// <returns></returns>
        ////[ResponseType(typeof(LogInResponse))]
        ////[GzipCompression]

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("LoginV3")]
        public async Task<IActionResult> LoginAsyncV3([FromBody][Bind("BPMSISDN,DeviceId,DeviceModel,FermwareVersion,KernelVersion,Lan,OSVersion,Password,Type,UserName,VersionCode,VersionName,cid,lac,latitude,longitude")] LoginRequestModel model)
        {
            LogInResponse response = new LogInResponse();
            string encriptedPwd = string.Empty;
            LoginRequestsV2 login = new LoginRequestsV2();
            CommonRequestPopulateModel populateModel = new CommonRequestPopulateModel();
            try
            {
                login = populateModel.LoginRequestPopulateModel(model);

                int isEligible = Convert.ToInt32(SettingsValues.GetIsEligibleAES());

                if (isEligible == 1)
                {
                    bool isEligibleUser = await _bLLUserAuthenticaion.IsAESEligibleUser(login.UserName);
                    if (isEligibleUser)
                    {
                        encriptedPwd = AESCryptography.Encrypt(login.Password);
                        response = await LoginByAESEncription(login, encriptedPwd);
                        return Ok(response);
                    }
                    else
                    {
                        response = new LogInResponse();
                        encriptedPwd = Cryptography.Encrypt(login.Password, true);
                        response = await LoginByMD5Encription(login, encriptedPwd);
                        return Ok(response);
                    }
                }
                else
                {
                    response = new LogInResponse();
                    encriptedPwd = AESCryptography.Encrypt(login.Password);
                    response = await LoginByAESEncription(login, encriptedPwd);
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                ErrorDescription error;

                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    return Ok(new LogInResponse
                    {
                        ISAuthenticate = false,
                        AuthenticationMessage = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                        HasUpdate = false
                    });
                }
                catch (Exception ex2)
                {
                    return Ok(new LogInResponse
                    {
                        ISAuthenticate = false,
                        AuthenticationMessage = ex2.InnerException?.Message,
                        HasUpdate = false
                    });
                }
            }
        }

        private async Task<LogInResponse> LoginByAESEncription(LoginRequestsV2 login, string encPwd)
        {
            try
            {
                var user = await _bLLUserAuthenticaion.ValidateUser(login.UserName, encPwd);

                if (string.IsNullOrEmpty(user?.user_name))
                {
                    return new LogInResponse
                    {
                        ISAuthenticate = false,
                        AuthenticationMessage = MessageCollection.InvalidUserCridential,
                        HasUpdate = false,
                    };
                }

                #region Password Policy Checking

                var (isValidFormat, validationMessage) = await _bLLUserAuthenticaion.IsPasswordFormatValid(login.Password);

                if (!isValidFormat)
                {
                    return new LogInResponse
                    {
                        ISAuthenticate = false,
                        AuthenticationMessage = validationMessage,
                        HasUpdate = false,
                    };
                }

                #endregion

                string loginProvider = Guid.NewGuid().ToString();

                var loginAtmInfo = new UserLogInAttemptV2
                {
                    userid = user.user_id,
                    is_success = 1,
                    ip_address = GetIP(),
                    loginprovider = loginProvider,
                    deviceid = login.DeviceId,
                    lan = login.Lan ?? "",
                    versioncode = login.VersionCode,
                    versionname = login.VersionName,
                    osversion = login.OSVersion,
                    kernelversion = login.KernelVersion,
                    fermwarevirsion = login.FermwareVersion,
                    latitude = login.latitude,
                    longitude = login.longitude,
                    lac = login.lac,
                    cid = login.cid,
                    is_bp = string.IsNullOrEmpty(login.BPMSISDN) ? 0 : 1,
                    bp_msisdn = login.BPMSISDN ?? "",
                    device_model = login.DeviceModel
                };

                if (string.IsNullOrEmpty(login.BPMSISDN))
                {
                    await _bLLUserAuthenticaion.SaveLoginAtmInfoV2(loginAtmInfo);

                    return new LogInResponse
                    {
                        SessionToken = GetEncriptedSecurityTokenV2(loginProvider, user.user_id, user.user_name, user.distributor_code, login.DeviceId),
                        ISAuthenticate = true,
                        AuthenticationMessage = MessageCollection.UserValidted,
                        UserName = login.UserName,
                        Password = login.Password,
                        DeviceId = login.DeviceId,
                        HasUpdate = false,
                        MinimumScore = SettingsValues.GetFPDefaultScore(),
                        OptionalMinimumScore = "30",
                        MaximumRetry = "2",
                        RoleAccess = user.role_access,
                        ChannelId = user.channel_id,
                        ChannelName = user.channel_name,
                        InventoryId = user.inventory_id,
                        CenterCode = user.center_code
                    };
                }
                else
                {
                    string bp_msisdn = ConverterHelper.MSISDNCountryCodeAddition(login.BPMSISDN, FixedValueCollection.MSISDNCountryCode);
                    var bpUser = await _bLLUserAuthenticaion.ValidateBPUser(bp_msisdn, login.UserName);

                    if (!bpUser.is_valid)
                    {
                        return new LogInResponse
                        {
                            ISAuthenticate = false,
                            AuthenticationMessage = bpUser.err_msg,
                            HasUpdate = false,
                        };
                    }

                    await _bLLUserAuthenticaion.SaveLoginAtmInfoV2(loginAtmInfo);
                    await _bLLUserAuthenticaion.GenerateBPLoginOTP(loginProvider);

                    return new LogInResponse
                    {
                        SessionToken = GetEncriptedSecurityTokenV2(loginProvider, user.user_id, user.user_name, user.distributor_code, login.DeviceId),
                        ISAuthenticate = true,
                        AuthenticationMessage = MessageCollection.UserValidted,
                        UserName = login.UserName,
                        Password = login.Password,
                        DeviceId = login.DeviceId,
                        HasUpdate = false,
                        MinimumScore = SettingsValues.GetFPDefaultScore(),
                        OptionalMinimumScore = "30",
                        MaximumRetry = "2",
                        RoleAccess = user.role_access,
                        ChannelId = user.channel_id,
                        ChannelName = user.channel_name,
                        InventoryId = user.inventory_id,
                        CenterCode = user.center_code
                    };
                }
            }
            catch (Exception ex)
            {
                return new LogInResponse
                {
                    ISAuthenticate = false,
                    AuthenticationMessage = ex.Message, // .ToString() not needed
                    HasUpdate = false,
                };
            }
        }

        private async Task<LogInResponse> LoginByMD5Encription(LoginRequestsV2 login, string encPwd)
        {
            try
            {
                var user = await _bLLUserAuthenticaion.ValidateUser(login.UserName, encPwd);
                if (string.IsNullOrWhiteSpace(user?.user_name))
                {
                    return new LogInResponse
                    {
                        ISAuthenticate = false,
                        AuthenticationMessage = MessageCollection.InvalidUserCridential,
                        HasUpdate = false
                    };
                }

                #region Password Policy Checking

                var (isValidFormat, validationMessage) = await _bLLUserAuthenticaion.IsPasswordFormatValid(login.Password);
                if (!isValidFormat)
                {
                    return new LogInResponse
                    {
                        ISAuthenticate = false,
                        AuthenticationMessage = validationMessage,
                        HasUpdate = false
                    };
                }

                #endregion

                var loginProvider = Guid.NewGuid().ToString();

                var loginAtmInfo = new UserLogInAttemptV2
                {
                    userid = user.user_id,
                    is_success = 1,
                    ip_address = GetIP(),
                    loginprovider = loginProvider,
                    deviceid = login.DeviceId,
                    lan = login.Lan ?? string.Empty,
                    versioncode = login.VersionCode,
                    versionname = login.VersionName,
                    osversion = login.OSVersion,
                    kernelversion = login.KernelVersion,
                    fermwarevirsion = login.FermwareVersion,
                    latitude = login.latitude,
                    longitude = login.longitude,
                    lac = login.lac,
                    cid = login.cid,
                    is_bp = string.IsNullOrWhiteSpace(login.BPMSISDN) ? 0 : 1,
                    bp_msisdn = login.BPMSISDN ?? string.Empty,
                    device_model = login.DeviceModel
                };

                await _bLLUserAuthenticaion.SaveLoginAtmInfoV2(loginAtmInfo);

                if (!string.IsNullOrWhiteSpace(login.BPMSISDN))
                {
                    string bpMsisdn = ConverterHelper.MSISDNCountryCodeAddition(login.BPMSISDN, FixedValueCollection.MSISDNCountryCode);
                    var bpValidation = await _bLLUserAuthenticaion.ValidateBPUser(bpMsisdn, login.UserName);

                    if (!bpValidation.is_valid)
                    {
                        return new LogInResponse
                        {
                            ISAuthenticate = false,
                            AuthenticationMessage = bpValidation.err_msg,
                            HasUpdate = false
                        };
                    }

                    await _bLLUserAuthenticaion.GenerateBPLoginOTP(loginProvider);
                }

                return new LogInResponse
                {
                    SessionToken = GetEncriptedSecurityToken(loginProvider, user.user_id, user.user_name, user.distributor_code, login.DeviceId),
                    ISAuthenticate = true,
                    AuthenticationMessage = MessageCollection.UserValidted,
                    UserName = login.UserName,
                    Password = login.Password,
                    DeviceId = login.DeviceId,
                    HasUpdate = false,
                    MinimumScore = SettingsValues.GetFPDefaultScore(),
                    OptionalMinimumScore = "30",
                    MaximumRetry = "2",
                    RoleAccess = user.role_access,
                    ChannelId = user.channel_id,
                    ChannelName = user.channel_name,
                    InventoryId = user.inventory_id,
                    CenterCode = user.center_code
                };
            }
            catch (Exception ex)
            {
                return new LogInResponse
                {
                    ISAuthenticate = false,
                    AuthenticationMessage = ex.Message,
                    HasUpdate = false
                };
            }
        }


        //private async Task<LogInResponse> LoginByAESEncription(LoginRequestsV2 login, string encPwd)
        //{
        //    try
        //    {
        //        LoginUserInfoResponse user = await _bLLUserAuthenticaion.ValidateUser(login.UserName, encPwd);

        //        if (user.user_name == null)
        //        {
        //            return (new LogInResponse()
        //            {
        //                ISAuthenticate = false,
        //                AuthenticationMessage = MessageCollection.InvalidUserCridential,
        //                HasUpdate = false,
        //            });
        //        }

        //        #region Password Policy Checking

        //        var validationResult = await _bLLUserAuthenticaion.IsPasswordFormatValid(login.Password);

        //        if (validationResult.Item1 == false)
        //        {
        //            return (new LogInResponse()
        //            {
        //                ISAuthenticate = false,
        //                AuthenticationMessage = validationResult.Item2,
        //                HasUpdate = false,
        //            });
        //        }

        //        #endregion

        //        if (string.IsNullOrEmpty(login.BPMSISDN))
        //        {
        //            UserLogInAttemptV2 loginAtmInfo;
        //            string loginProvider = Guid.NewGuid().ToString();

        //            loginAtmInfo = new UserLogInAttemptV2()
        //            {
        //                userid = user.user_id,
        //                is_success = user != null ? 1 : 0,
        //                ip_address = GetIP(),
        //                loginprovider = loginProvider,
        //                deviceid = login.DeviceId,
        //                lan = login.Lan,
        //                versioncode = login.VersionCode,
        //                versionname = login.VersionName,
        //                osversion = login.OSVersion,
        //                kernelversion = login.KernelVersion,
        //                fermwarevirsion = login.FermwareVersion,
        //                latitude = login.latitude,
        //                longitude = login.longitude,
        //                lac = login.lac,
        //                cid = login.cid,
        //                is_bp = 0,
        //                bp_msisdn = login.BPMSISDN ?? "",
        //                device_model = login.DeviceModel
        //            };

        //             await _bLLUserAuthenticaion.SaveLoginAtmInfoV2(loginAtmInfo);

        //            return (new LogInResponse()
        //            {
        //                SessionToken = GetEncriptedSecurityTokenV2(loginProvider, user.user_id, user.user_name, user.distributor_code, login.DeviceId),
        //                ISAuthenticate = true,
        //                AuthenticationMessage = MessageCollection.UserValidted,
        //                UserName = login.UserName,
        //                Password = login.Password,
        //                DeviceId = login.DeviceId,
        //                HasUpdate = false,
        //                MinimumScore = SettingsValues.GetFPDefaultScore(),
        //                OptionalMinimumScore = "30",
        //                MaximumRetry = "2",
        //                RoleAccess = user.role_access,
        //                ChannelId = user.channel_id,
        //                ChannelName = user.channel_name,
        //                InventoryId = user.inventory_id,
        //                CenterCode = user.center_code
        //            });
        //        }
        //        else
        //        {
        //            string bp_msisdn = ConverterHelper.MSISDNCountryCodeAddition(login.BPMSISDN, FixedValueCollection.MSISDNCountryCode);

        //            BPUserValidationResponse bPUserValidationResponse = await _bLLUserAuthenticaion.ValidateBPUser(bp_msisdn, login.UserName);

        //            if (!bPUserValidationResponse.is_valid)
        //            {
        //                return (new LogInResponse()
        //                {
        //                    ISAuthenticate = bPUserValidationResponse.is_valid,
        //                    AuthenticationMessage = bPUserValidationResponse.err_msg,
        //                    HasUpdate = false,
        //                });
        //            }
        //            else
        //            {
        //                UserLogInAttemptV2 loginAtmInfo;
        //                string loginProvider = Guid.NewGuid().ToString();

        //                loginAtmInfo = new UserLogInAttemptV2()
        //                {
        //                    userid = user.user_id,
        //                    is_success = user != null ? 1 : 0,
        //                    ip_address = GetIP(),
        //                    loginprovider = loginProvider,
        //                    deviceid = login.DeviceId,
        //                    lan = login.Lan ?? "",
        //                    versioncode = login.VersionCode,
        //                    versionname = login.VersionName,
        //                    osversion = login.OSVersion,
        //                    kernelversion = login.KernelVersion,
        //                    fermwarevirsion = login.FermwareVersion,
        //                    latitude = login.latitude,
        //                    longitude = login.longitude,
        //                    lac = login.lac,
        //                    cid = login.cid,
        //                    is_bp = 1,
        //                    bp_msisdn = login.BPMSISDN,
        //                    device_model = login.DeviceModel
        //                };

        //                await _bLLUserAuthenticaion.SaveLoginAtmInfoV2(loginAtmInfo);

        //                await _bLLUserAuthenticaion.GenerateBPLoginOTP(loginProvider);

        //                return (new LogInResponse()
        //                {
        //                    SessionToken = GetEncriptedSecurityTokenV2(loginProvider, user.user_id, user.user_name, user.distributor_code, login.DeviceId),
        //                    ISAuthenticate = true,
        //                    AuthenticationMessage = MessageCollection.UserValidted,
        //                    UserName = login.UserName,
        //                    Password = login.Password,
        //                    DeviceId = login.DeviceId,
        //                    HasUpdate = false,
        //                    MinimumScore = SettingsValues.GetFPDefaultScore(),
        //                    OptionalMinimumScore = "30",
        //                    MaximumRetry = "2",
        //                    RoleAccess = user.role_access,
        //                    ChannelId = user.channel_id,
        //                    ChannelName = user.channel_name,
        //                    InventoryId = user.inventory_id,
        //                    CenterCode = user.center_code
        //                });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return (new LogInResponse()
        //        {
        //            ISAuthenticate = false,
        //            AuthenticationMessage = ex.Message.ToString(),
        //            HasUpdate = false,
        //        });
        //    }
        //}
        //private async Task<LogInResponse> LoginByMD5Encription(LoginRequestsV2 login, string encPwd)
        //{
        //    try
        //    {
        //        LoginUserInfoResponse user = await _bLLUserAuthenticaion.ValidateUser(login.UserName, encPwd);

        //        if (user.user_name == null)
        //        {
        //            return (new LogInResponse()
        //            {
        //                ISAuthenticate = false,
        //                AuthenticationMessage = MessageCollection.InvalidUserCridential,
        //                HasUpdate = false,
        //            });
        //        }

        //        #region Password Policy Checking

        //        var validationResult = await _bLLUserAuthenticaion.IsPasswordFormatValid(login.Password);

        //        if (validationResult.Item1 == false)
        //        {
        //            return (new LogInResponse()
        //            {
        //                ISAuthenticate = false,
        //                AuthenticationMessage = validationResult.Item2,
        //                HasUpdate = false,
        //            });
        //        }

        //        #endregion

        //        if (string.IsNullOrEmpty(login.BPMSISDN))
        //        {
        //            UserLogInAttemptV2 loginAtmInfo;
        //            string loginProvider = Guid.NewGuid().ToString();

        //            loginAtmInfo = new UserLogInAttemptV2()
        //            {
        //                userid = user.user_id,
        //                is_success = user != null ? 1 : 0,
        //                ip_address = GetIP(),
        //                loginprovider = loginProvider,
        //                deviceid = login.DeviceId,
        //                lan = login.Lan ?? "",
        //                versioncode = login.VersionCode,
        //                versionname = login.VersionName,
        //                osversion = login.OSVersion,
        //                kernelversion = login.KernelVersion,
        //                fermwarevirsion = login.FermwareVersion,
        //                latitude = login.latitude,
        //                longitude = login.longitude,
        //                lac = login.lac,
        //                cid = login.cid,
        //                is_bp = 0,
        //                bp_msisdn = login.BPMSISDN ?? "",
        //                device_model = login.DeviceModel
        //            };

        //            await _bLLUserAuthenticaion.SaveLoginAtmInfoV2(loginAtmInfo);

        //            return (new LogInResponse()
        //            {
        //                SessionToken = GetEncriptedSecurityToken(loginProvider, user.user_id, user.user_name, user.distributor_code, login.DeviceId),
        //                ISAuthenticate = true,
        //                AuthenticationMessage = MessageCollection.UserValidted,
        //                UserName = login.UserName,
        //                Password = login.Password,
        //                DeviceId = login.DeviceId,
        //                HasUpdate = false,
        //                MinimumScore = SettingsValues.GetFPDefaultScore(),
        //                OptionalMinimumScore = "30",
        //                MaximumRetry = "2",
        //                RoleAccess = user.role_access,
        //                ChannelId = user.channel_id,
        //                ChannelName = user.channel_name,
        //                InventoryId = user.inventory_id,
        //                CenterCode = user.center_code
        //            });
        //        }
        //        else
        //        {
        //            string bp_msisdn = ConverterHelper.MSISDNCountryCodeAddition(login.BPMSISDN, FixedValueCollection.MSISDNCountryCode);

        //            BPUserValidationResponse bPUserValidationResponse = await _bLLUserAuthenticaion.ValidateBPUser(bp_msisdn, login.UserName);

        //            if (!bPUserValidationResponse.is_valid)
        //            {
        //                return (new LogInResponse()
        //                {
        //                    ISAuthenticate = bPUserValidationResponse.is_valid,
        //                    AuthenticationMessage = bPUserValidationResponse.err_msg,
        //                    HasUpdate = false,
        //                });
        //            }
        //            else
        //            {
        //                UserLogInAttemptV2 loginAtmInfo;
        //                string loginProvider = Guid.NewGuid().ToString();

        //                loginAtmInfo = new UserLogInAttemptV2()
        //                {
        //                    userid = user.user_id,
        //                    is_success = user != null ? 1 : 0,
        //                    ip_address = GetIP(),
        //                    loginprovider = loginProvider,
        //                    deviceid = login.DeviceId,
        //                    lan = login.Lan??"",
        //                    versioncode = login.VersionCode,
        //                    versionname = login.VersionName,
        //                    osversion = login.OSVersion,
        //                    kernelversion = login.KernelVersion,
        //                    fermwarevirsion = login.FermwareVersion,
        //                    latitude = login.latitude,
        //                    longitude = login.longitude,
        //                    lac = login.lac,
        //                    cid = login.cid,
        //                    is_bp = 1,
        //                    bp_msisdn = login.BPMSISDN,
        //                    device_model = login.DeviceModel
        //                };

        //                await _bLLUserAuthenticaion.SaveLoginAtmInfoV2(loginAtmInfo);

        //                await _bLLUserAuthenticaion.GenerateBPLoginOTP(loginProvider);

        //                return (new LogInResponse()
        //                {
        //                    SessionToken = GetEncriptedSecurityToken(loginProvider, user.user_id, user.user_name, user.distributor_code, login.DeviceId),
        //                    ISAuthenticate = true,
        //                    AuthenticationMessage = MessageCollection.UserValidted,
        //                    UserName = login.UserName,
        //                    Password = login.Password,
        //                    DeviceId = login.DeviceId,
        //                    HasUpdate = false,
        //                    MinimumScore = SettingsValues.GetFPDefaultScore(),
        //                    OptionalMinimumScore = "30",
        //                    MaximumRetry = "2",
        //                    RoleAccess = user.role_access,
        //                    ChannelId = user.channel_id,
        //                    ChannelName = user.channel_name,
        //                    InventoryId = user.inventory_id,
        //                    CenterCode = user.center_code
        //                });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return (new LogInResponse()
        //        {
        //            ISAuthenticate = false,
        //            AuthenticationMessage = ex.Message.ToString(),
        //            HasUpdate = false,
        //        });
        //    }
        //}
        ////=================x===================================

        #region Revamp Login
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("LoginV4Old")]
        public async Task<IActionResult> LoginAsyncV4([FromBody][Bind("BPMSISDN,DeviceId,DeviceModel,FermwareVersion,InstalledApps,KernelVersion,Lan,OSVersion,Password,Type,UserName,VersionCode,VersionName,cid,lac,latitude,longitude")] LoginRequestsV2 login)
        {
            LogInResponse response = new LogInResponse();
            string encriptedPwd = string.Empty;
            try
            {
                int isEligible = 0;
                isEligible = Convert.ToInt32(SettingsValues.GetIsEligibleAES());

                if (isEligible == 1)
                {
                    bool isEligibleUser = await _bLLUserAuthenticaion.IsAESEligibleUser(login.UserName);
                    if (isEligibleUser)
                    {
                        encriptedPwd = AESCryptography.Encrypt(login.Password);
                        response = await LoginByAESEncriptionV1(login, encriptedPwd);

                        return Ok(response);
                    }
                    else
                    {
                        response = new LogInResponse();
                        encriptedPwd = Cryptography.Encrypt(login.Password, true);
                        response = await LoginByMD5EncriptionV1(login, encriptedPwd);

                        return Ok(response);
                    }
                }
                else
                {
                    response = new LogInResponse();
                    encriptedPwd = AESCryptography.Encrypt(login.Password);
                    response = await LoginByAESEncriptionV1(login, encriptedPwd);

                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");

                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                return Ok(new LogInResponse
                {
                    ISAuthenticate = false,
                    AuthenticationMessage = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    HasUpdate = false
                });
            }
        }

        #region UserLockMechanism
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("LoginV4")]
        public async Task<IActionResult> LoginAsyncV5([FromBody][Bind("BPMSISDN,DeviceId,DeviceModel,FermwareVersion,InstalledApps,KernelVersion,Lan,OSVersion,Password,Type,UserName,VersionCode,VersionName,cid,lac,latitude,longitude")] LoginRequestsV2 login)
        {
            LogInResponse response = new LogInResponse();
            string encriptedPwd = string.Empty;
         
            try
            {
                int isEligible = 0;
                isEligible = Convert.ToInt32(SettingsValues.GetIsEligibleAES());
                int currentAttempt = 0;
                int minutesLeft = 0;
                string message2 = string.Empty;


                if (isEligible == 1)
                {
                    bool isEligibleUser = await _bLLUserAuthenticaion.IsAESEligibleUser(login.UserName);
                    if (isEligibleUser)
                    {
                        encriptedPwd = AESCryptography.Encrypt(login.Password);
                        response = await LoginByAESEncriptionV2(login, encriptedPwd);

                        return Ok(response);
                    }
                    else
                    {
                        response = new LogInResponse();
                        encriptedPwd = Cryptography.Encrypt(login.Password, true);
                        response = await LoginByMD5EncriptionV2(login, encriptedPwd);

                        return Ok(response);
                    }
                }
                else
                {
                    response = new LogInResponse();
                    encriptedPwd = AESCryptography.Encrypt(login.Password);
                    response = await LoginByAESEncriptionV2(login, encriptedPwd);

                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");

                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                return Ok(new LogInResponse
                {
                    ISAuthenticate = false,
                    AuthenticationMessage = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description,
                    HasUpdate = false
                });
            }
        }
        #endregion

        private async Task<LogInResponse> LoginByAESEncriptionV1(LoginRequestsV2 login, string encPwd)
        {
            try
            {
                string secretKey = SettingsValues.GetJWTSequrityKey();
                TokenService tokenService = new TokenService(secretKey);

                var user = await _bLLUserAuthenticaion.ValidateUserV2(login, login.UserName, encPwd);
                if (user == null)
                    return CreateErrorResponse("Invalid user credentials!");

                if (string.IsNullOrEmpty(user.user_name))
                {
                    string message = user.isValidUser == 2 ? user.message : MessageCollection.InvalidUserCridential;
                    return CreateErrorResponse(message);
                }

                var (isPasswordValid, passwordMessage) = await _bLLUserAuthenticaion.IsPasswordFormatValid(login.Password);
                if (!isPasswordValid)
                    return CreateErrorResponse(passwordMessage);

                string loginProvider = Guid.NewGuid().ToString();
                var loginAttempt = CreateLoginAttempt(user.user_id, login, loginProvider);

                if (string.IsNullOrEmpty(login.BPMSISDN))
                {
                    loginAttempt.is_bp = 0;

                    if (!string.IsNullOrEmpty(user.user_id))
                        await _bLLUserAuthenticaion.SaveLoginAtmInfoV2(loginAttempt);

                    return CreateLoginResponse(user, login, tokenService, loginProvider);
                }
                else
                {
                    return await HandleBPLogin(loginAttempt, loginProvider, login, user, tokenService);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<LogInResponse> LoginByAESEncriptionV2(LoginRequestsV2 login, string encPwd)
        {
            try
            {
                int currentAttempt = 0;
                int minutesLeft = 0;
                string message2 = string.Empty;
                string secretKey = SettingsValues.GetJWTSequrityKey();
                TokenService tokenService = new TokenService(secretKey);

                string loginProvider = Guid.NewGuid().ToString();

                var user = await _bLLUserAuthenticaion.ValidateUserV2(login, login.UserName, encPwd);

                if (user == null)
                {
                    (currentAttempt, minutesLeft, message2) = await _bLLUserAuthenticaion.UserLoginAttemptCount(login.UserName, 0);

                    string errorMessage = !string.IsNullOrEmpty(message2)
                        ? message2
                        : "Invalid user credentials!";

                    return await SaveFailedLoginAndReturnResponse(
                        login,
                        loginProvider,
                        errorMessage);
                }

                if (string.IsNullOrEmpty(user.user_name))
                {
                    string message = user.isValidUser == 2
                        ? user.message
                        : MessageCollection.InvalidUserCridential;

                    (currentAttempt, minutesLeft, message2) = await _bLLUserAuthenticaion.UserLoginAttemptCount(login.UserName, 0);

                    string errorMessage = !string.IsNullOrEmpty(message2)
                        ? message2
                        : "Invalid user credentials!";

                    return await SaveFailedLoginAndReturnResponse(
                        login,
                        loginProvider,
                        errorMessage);
                }

                (currentAttempt, minutesLeft, message2) = await _bLLUserAuthenticaion.UserLoginAttemptCount(login.UserName, 1);

                if (minutesLeft > 0)
                {
                    return await SaveFailedLoginAndReturnResponse(
                        login,
                        loginProvider,
                        message2);
                }

                var (isPasswordValid, passwordMessage) = await _bLLUserAuthenticaion.IsPasswordFormatValid(login.Password);

                if (!isPasswordValid)
                {
                    return await SaveFailedLoginAndReturnResponse(
                        login,
                        loginProvider,
                        passwordMessage);
                }

                var loginAttempt = CreateLoginAttempt(user.user_id, login, loginProvider);

                if (string.IsNullOrEmpty(login.BPMSISDN))
                {
                    loginAttempt.is_bp = 0;

                    if (!string.IsNullOrEmpty(user.user_id))
                        await _bLLUserAuthenticaion.SaveLoginAtmInfoV2(loginAttempt);

                    return CreateLoginResponse(user, login, tokenService, loginProvider);
                }
                else
                {
                    return await HandleBPLogin(loginAttempt, loginProvider, login, user, tokenService);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        private UserLogInAttemptV2 CreateLoginAttempt(string userId, LoginRequestsV2 login, string loginProvider)
        {
            return new UserLogInAttemptV2
            {
                userid = userId,
                is_success = 1,
                ip_address = GetIP(),
                loginprovider = loginProvider,
                deviceid = login.DeviceId,
                lan = login.Lan ?? "",
                versioncode = login.VersionCode,
                versionname = login.VersionName,
                osversion = login.OSVersion,
                kernelversion = login.KernelVersion,
                fermwarevirsion = login.FermwareVersion,
                latitude = login.latitude,
                longitude = login.longitude,
                lac = login.lac,
                cid = login.cid,
                bp_msisdn = login.BPMSISDN ?? "",
                device_model = login.DeviceModel
            };
        }

        private async Task<LogInResponse> HandleBPLogin(UserLogInAttemptV2 loginAttempt, string loginProvider, LoginRequestsV2 login, LoginUserInfoResponseRev user, TokenService tokenService)
        {
            string formattedBPMSISDN = ConverterHelper.MSISDNCountryCodeAddition(login.BPMSISDN ?? "", FixedValueCollection.MSISDNCountryCode);
            BPUserValidationResponse bpValidation = await _bLLUserAuthenticaion.ValidateBPUserV1(formattedBPMSISDN, login.UserName);

            if (!bpValidation.is_valid)
            {
                return CreateErrorResponse(bpValidation.err_msg);
            }

            loginAttempt.is_bp = 1;

            if (!string.IsNullOrEmpty(user.user_id))
            {
                await _bLLUserAuthenticaion.SaveLoginAtmInfoV2(loginAttempt);
            }

            await _bLLUserAuthenticaion.GenerateBPLoginOTPV2(loginProvider);

            var response = CreateLoginResponse(user, login, tokenService, loginProvider);
            return response;
        }

        private LogInResponse CreateErrorResponse(string message)
        {
            return new LogInResponse
            {
                ISAuthenticate = false,
                AuthenticationMessage = message,
                HasUpdate = false
            };
        }
        private LogInResponse CreateLoginResponse(LoginUserInfoResponseRev user, LoginRequestsV2 login, TokenService tokenService, string loginProvider)
        {
            return new LogInResponse
            {
                SessionToken = user.isValidUser == 1 ? tokenService.GenerateToken(user, loginProvider) : "",
                ISAuthenticate = user.isValidUser == 1,
                AuthenticationMessage = user.message,
                UserName = login.UserName,
                Password = "******",
                DeviceId = login.DeviceId,
                HasUpdate = false,
                MinimumScore = SettingsValues.GetFPDefaultScore(),
                OptionalMinimumScore = "30",
                MaximumRetry = "2",
                RoleAccess = user.role_access,
                ChannelId = user.channel_id,
                ChannelName = user.channel_name,
                InventoryId = user.inventory_id,
                CenterCode = user.center_code,
                itopUpNumber = user.itopUpNumber,
                is_default_Password = user.is_default_Password,
                ExpiredDate = user.ExpiredDate,
                Designation = user.designation,
                is_etsaf_validation_need = SettingsValues.GetETSAFValidationValue(),
                FWA_channel_id = user.FWA_channel_id,
                FWA_channel_name = user.FWA_channel_name
            };
        }

        private async Task<LogInResponse> LoginByMD5EncriptionV1(LoginRequestsV2 login, string encPwd)
        {
            try
            {
                string secretKey = SettingsValues.GetJWTSequrityKey();
                TokenService tokenService = new TokenService(secretKey);

                var user = await _bLLUserAuthenticaion.ValidateUserV2(login, login.UserName, encPwd);
                if (user == null)
                    return CreateErrorResponse("Invalid user credentials!");

                if (string.IsNullOrEmpty(user.user_name))
                {
                    string message = user.isValidUser == 2 ? user.message : MessageCollection.InvalidUserCridential;
                    return CreateErrorResponse(message);
                }

                var (isPasswordValid, passwordMessage) = await _bLLUserAuthenticaion.IsPasswordFormatValid(login.Password);
                if (!isPasswordValid)
                    return CreateErrorResponse(passwordMessage);

                string loginProvider = Guid.NewGuid().ToString();
                var loginAttempt = CreateLoginAttempt(user.user_id, login, loginProvider);

                if (string.IsNullOrEmpty(login.BPMSISDN))
                {
                    loginAttempt.is_bp = 0;

                    if (!string.IsNullOrEmpty(user.user_id))
                        await _bLLUserAuthenticaion.SaveLoginAtmInfoV2(loginAttempt);

                    return CreateLoginResponse(user, login, tokenService, loginProvider);
                }
                else
                {
                    return await HandleBPLogin(loginAttempt, loginProvider, login, user, tokenService);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<LogInResponse> LoginByMD5EncriptionV2(LoginRequestsV2 login, string encPwd)
        {
            try
            {
                int currentAttempt = 0;
                int minutesLeft = 0;
                string message2 = string.Empty;
                string secretKey = SettingsValues.GetJWTSequrityKey();
                TokenService tokenService = new TokenService(secretKey);

                string loginProvider = Guid.NewGuid().ToString();

                var user = await _bLLUserAuthenticaion.ValidateUserV2(login, login.UserName, encPwd);

                if (user == null)
                {
                    (currentAttempt, minutesLeft, message2) = await _bLLUserAuthenticaion.UserLoginAttemptCount(login.UserName, 0);

                    string errorMessage = !string.IsNullOrEmpty(message2)
                        ? message2
                        : "Invalid user credentials!";

                    return await SaveFailedLoginAndReturnResponse(
                        login,
                        loginProvider,
                        errorMessage);
                }

                if (string.IsNullOrEmpty(user.user_name))
                {
                    string message = user.isValidUser == 2
                        ? user.message
                        : MessageCollection.InvalidUserCridential;

                    (currentAttempt, minutesLeft, message2) = await _bLLUserAuthenticaion.UserLoginAttemptCount(login.UserName, 0);

                    string errorMessage = !string.IsNullOrEmpty(message2)
                        ? message2
                        : "Invalid user credentials!";

                    return await SaveFailedLoginAndReturnResponse(
                        login,
                        loginProvider,
                        errorMessage);
                }

                (currentAttempt, minutesLeft, message2) = await _bLLUserAuthenticaion.UserLoginAttemptCount(login.UserName, 1);

                if (minutesLeft > 0)
                {
                    return await SaveFailedLoginAndReturnResponse(
                        login,
                        loginProvider,
                        message2);
                }

                var (isPasswordValid, passwordMessage) = await _bLLUserAuthenticaion.IsPasswordFormatValid(login.Password);

                if (!isPasswordValid)
                {
                    return await SaveFailedLoginAndReturnResponse(
                        login,
                        loginProvider,
                        passwordMessage);
                }

                var loginAttempt = CreateLoginAttempt(user.user_id, login, loginProvider);

                if (string.IsNullOrEmpty(login.BPMSISDN))
                {
                    loginAttempt.is_bp = 0;

                    if (!string.IsNullOrEmpty(user.user_id))
                        await _bLLUserAuthenticaion.SaveLoginAtmInfoV2(loginAttempt);

                    return CreateLoginResponse(user, login, tokenService, loginProvider);
                }
                else
                {
                    return await HandleBPLogin(loginAttempt, loginProvider, login, user, tokenService);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion


        // POST: api/Security/Login
        /// <summary>
        /// Authentication API for external user(DBSS)
        /// </summary>
        /// <param name="loginInfo">Requesting parameter with username and password</param>
        /// <returns>Return the authentication information of requesting user</returns>  
        //[ResponseType(typeof(DBSSLogInResponse))]
        [ValidateModel]
        [IgnoreAntiforgeryToken]
        [Route("DBSSLoginOld")]
        public async Task<IActionResult> DBSSLoginAsyncOld([FromBody][Bind("Password,UserName")] DBSSLoginRequests login)
        {
            BIAToDBSSLog biaLogObj = new BIAToDBSSLog();
            BL_Json bllJson = new BL_Json();
            DBSSLogInResponse response = new DBSSLogInResponse();
            string txtReq = string.Empty, txtResp = string.Empty;

            try
            {
                string secretKey = SettingsValues.GetJWTSequrityKey();
                TokenService tokenService = new TokenService(secretKey);

                biaLogObj.req_blob = bllJson.GetGenericJsonData(login);
                biaLogObj.req_time = DateTime.Now;
                txtReq = JsonConvert.SerializeObject(login);

                LoginUserInfoResponse user = await _bLLUserAuthenticaion.ValidateDbssUser(login.UserName, login.Password);

                if (user == null || user.user_name == null)
                {
                    biaLogObj.message = "Invalid Credential!";

                    return Ok(new DBSSLogInResponse()
                    {
                        ISAuthenticate = false,
                        AuthenticationMessage = MessageCollection.InvalidUserCridential
                    });
                }

                string loginProvider = Guid.NewGuid().ToString();

                string generatedToken = await tokenService.GenerateTokenDBSS_V2(user, loginProvider);

                if (generatedToken == null || generatedToken == "")
                {
                    biaLogObj.message = "Token regeneration max count exceeded! Last login provider " + loginProvider;

                    return Ok(new DBSSLogInResponse()
                    {
                        ISAuthenticate = false,
                        AuthenticationMessage = MessageCollection.InvalidUserCridential
                    });
                }

                response = new DBSSLogInResponse()
                {
                    SessionToken = generatedToken,
                    ISAuthenticate = true,
                    AuthenticationMessage = MessageCollection.UserValidted
                };

                UserLogInAttempt loginAtmInfo = new UserLogInAttempt()
                {
                    userid = user.user_id,
                    is_success = 1,
                    ip_address = GetIP_V2(),
                    loginprovider = loginProvider
                };

                await _bLLUserAuthenticaion.SaveLoginAtmInfo(loginAtmInfo);

                biaLogObj.res_blob = bllJson.GetGenericJsonData(response);
                biaLogObj.res_time = DateTime.Now;
                txtResp = JsonConvert.SerializeObject(response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                biaLogObj.res_blob = bllJson.GetGenericJsonData(error);
                biaLogObj.res_time = DateTime.Now;
                biaLogObj.is_success = 0;
                biaLogObj.error_code = error.error_code ?? string.Empty;
                biaLogObj.error_source = error.error_source ?? string.Empty;
                biaLogObj.message = error.error_custom_msg ?? string.Empty;

                return Ok(new DBSSLogInResponse()
                {
                    SessionToken = string.Empty,
                    ISAuthenticate = false,
                    AuthenticationMessage = MessageCollection.Failed
                });
            }
            finally
            {
                biaLogObj.method_name = "DBSSLoginAsync";
                biaLogObj.error_source = "BIA";
                biaLogObj.user_id = login.UserName;
                biaLogObj.remarks = "";
                biaLogObj.integration_point_from = Convert.ToDecimal(IntegrationPoints.BSS);
                biaLogObj.integration_point_to = Convert.ToDecimal(IntegrationPoints.RA);

                await _bllLog.RAToDBSSLog(biaLogObj);
            }
        }

        // POST: api/Security/Login
        /// <summary>
        /// Authentication API for external user(DBSS)
        /// </summary>
        /// <param name="loginInfo">Requesting parameter with username and password</param>
        /// <returns>Return the authentication information of requesting user</returns>  
        //[ResponseType(typeof(DBSSLogInResponse))]
        [ValidateModel]
        [IgnoreAntiforgeryToken]
        [Route("DBSSLogin")]
        public async Task<IActionResult> DBSSLoginAsync([FromBody][Bind("Password,UserName")] DBSSLoginRequests login)
        {
            BIAToDBSSLog biaLogObj = new BIAToDBSSLog();
            BL_Json bllJson = new BL_Json();
            DBSSLogInResponse response = new DBSSLogInResponse();
            string txtReq = string.Empty, txtResp = string.Empty;

            try
            {
                string secretKey = SettingsValues.GetJWTSequrityKey();
                TokenService tokenService = new TokenService(secretKey);

                biaLogObj.req_blob = bllJson.GetGenericJsonData(login);
                biaLogObj.req_time = DateTime.Now;
                txtReq = JsonConvert.SerializeObject(login);

                LoginUserInfoResponse user = await _bLLUserAuthenticaion.ValidateDbssUser(login.UserName, login.Password);

                if (user == null || user.user_name == null)
                {
                    biaLogObj.message = "Invalid Credential!";

                    return Ok(new DBSSLogInResponse()
                    {
                        ISAuthenticate = false,
                        AuthenticationMessage = MessageCollection.InvalidUserCridential
                    });
                }

                string loginProvider = Guid.NewGuid().ToString();

                string generatedToken = await tokenService.GenerateTokenDBSS_V2(user, loginProvider);

                if (generatedToken == null || generatedToken == "")
                {
                    biaLogObj.message = "Token regeneration max count exceeded! Last login provider " + loginProvider;

                    return Ok(new DBSSLogInResponse()
                    {
                        ISAuthenticate = false,
                        AuthenticationMessage = MessageCollection.InvalidUserCridential
                    });
                }

                response = new DBSSLogInResponse()
                {
                    SessionToken = generatedToken,
                    ISAuthenticate = true,
                    AuthenticationMessage = MessageCollection.UserValidted
                };

                UserLogInAttempt loginAtmInfo = new UserLogInAttempt()
                {
                    userid = user.user_id,
                    is_success = 1,
                    ip_address = GetIP_V2(),
                    loginprovider = loginProvider
                };

                await _bLLUserAuthenticaion.SaveLoginAtmInfo(loginAtmInfo);

                biaLogObj.res_blob = bllJson.GetGenericJsonData(response);
                biaLogObj.res_time = DateTime.Now;
                txtResp = JsonConvert.SerializeObject(response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                biaLogObj.res_blob = bllJson.GetGenericJsonData(error);
                biaLogObj.res_time = DateTime.Now;
                biaLogObj.is_success = 0;
                biaLogObj.error_code = error.error_code ?? string.Empty;
                biaLogObj.error_source = error.error_source ?? string.Empty;
                biaLogObj.message = error.error_custom_msg ?? string.Empty;

                return Ok(new DBSSLogInResponse()
                {
                    SessionToken = string.Empty,
                    ISAuthenticate = false,
                    AuthenticationMessage = MessageCollection.Failed
                });
            }
            finally
            {
                biaLogObj.method_name = "DBSSLoginAsync";
                biaLogObj.error_source = "BIA";
                biaLogObj.user_id = login.UserName;
                biaLogObj.remarks = "";
                biaLogObj.integration_point_from = Convert.ToDecimal(IntegrationPoints.BSS);
                biaLogObj.integration_point_to = Convert.ToDecimal(IntegrationPoints.RA);

                await _bllLog.RAToDBSSLog(biaLogObj);
            }
        }


        /// <summary>
        /// Get Reseller app info. 
        /// This api is called by reseller app before login api call.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //[GzipCompression]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetAPIServer")]
        public async Task<IActionResult> GetAPIServerV2([FromBody][Bind("appVersion,username")] APIVersionRequestWithAppUpdateCheck model)
        {
            APIVersionResponse apiVersionRespObj = new APIVersionResponse();
            int apiVersion = 0;
            try
            {
                apiVersion = await _bLLUserAuthenticaion.GetUserAPIVersion(new APIVersionRequest { username = model.username });

                if (apiVersion == 0)
                {
                    return Ok(new APIVersionResponseWithAppUpdateCheck()
                    {
                        api_version = 0,
                        message = MessageCollection.UserNotFound,
                        result = false,
                        app_update_info = new AppUpdateInfo
                        {
                            is_update_exists = false,
                            is_update_mandatory = 0,
                            update_url = string.Empty
                        }

                    });
                }

                if (model.appVersion.HasValue)
                {
                    var apiUpdateData = await _bLLUserAuthenticaion.GetUserAPIVersionWithAppUpdateCheck(new APIVersionRequestWithAppUpdateCheck
                    {
                        username = model.username,
                        appVersion = model.appVersion
                    });

                    apiUpdateData.api_version = apiVersion;
                    apiUpdateData.message = apiVersion == 1 ? "Old version." : "New version.";
                    return Ok(apiUpdateData);
                }
                //=========for change pwd=========
                if (apiVersion == 1)
                {
                    apiVersionRespObj.result = true;
                    apiVersionRespObj.message = "Old version.";
                    apiVersionRespObj.api_version = apiVersion;
                }
                else
                {
                    apiVersionRespObj.result = true;
                    apiVersionRespObj.message = "New version.";
                    apiVersionRespObj.api_version = apiVersion;
                }
                return Ok(apiVersionRespObj);
                //=========x==============
            }
            catch (Exception ex)
            {
                apiVersionRespObj.result = false;
                apiVersionRespObj.api_version = 0;

                try
                {
                    var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    apiVersionRespObj.message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description;

                    return Ok(apiVersionRespObj);
                }
                catch (Exception)
                {
                    apiVersionRespObj.message = MessageCollection.Failed;
                    return Ok(apiVersionRespObj);
                }
            }
        }

        /// <summary>
        /// Get Reseller app info. 
        /// This api is called by reseller app before login api call.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //[GzipCompression]
        //[HttpPost]
        //[IgnoreAntiforgeryToken]
        ////[ResponseType(typeof(APIVersionResponseWithAppUpdateCheck))]
        //[Route("GetAPIServerV2")]
        //public async Task<IActionResult> GetAPIServerV3([FromBody][Bind("appVersion,username")] APIVersionRequestWithAppUpdateCheck model)
        //{
        //    APIVersionResponseRev apiVersionRespObj = new APIVersionResponseRev();
        //    int apiVersion = 0;
        //    try
        //    {
        //        apiVersion = await _bLLUserAuthenticaion.GetUserAPIVersion(new APIVersionRequest { username = model.username });

        //        if (apiVersion == 0)
        //        {
        //            return Ok(new APIVersionResponseWithAppUpdateCheckRev()
        //            {
        //                message = MessageCollection.UserNotFound,
        //                isError = true,
        //                data = new AppUpdateInfoV2
        //                {
        //                    is_update_exists = false,
        //                    is_update_mandatory = 0,
        //                    api_version = 0,
        //                    update_url = string.Empty
        //                }

        //            });
        //        }

        //        if (model.appVersion.HasValue)
        //        {
        //            var apiUpdateData = await _bLLUserAuthenticaion.GetUserAPIVersionWithAppUpdateCheckV2(new APIVersionRequestWithAppUpdateCheck
        //            {
        //                username = model.username,
        //                appVersion = model.appVersion
        //            });

        //            apiUpdateData.data.api_version = apiVersion;
        //            apiUpdateData.message = apiVersion == 1 ? "Old version." : "New version.";
        //            return Ok(apiUpdateData);
        //        }
        //        //=========for change pwd=========
        //        if (apiVersion == 1)
        //        {
        //            apiVersionRespObj.isError = false;
        //            apiVersionRespObj.message = "Old version.";
        //            apiVersionRespObj.data = new APIVersionData()
        //            {
        //                api_version = apiVersion
        //            };
        //        }
        //        else
        //        {
        //            apiVersionRespObj.isError = false;
        //            apiVersionRespObj.message = "New version.";
        //            apiVersionRespObj.data = new APIVersionData()
        //            {
        //                api_version = apiVersion
        //            };
        //        }
        //        return Ok(apiVersionRespObj);
        //        //=========x==============
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        apiVersionRespObj.isError = true;
        //        apiVersionRespObj.data.api_version = 0;

        //        try
        //        {
        //            var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

        //            apiVersionRespObj.message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description;

        //            return Ok(apiVersionRespObj);
        //        }
        //        catch (Exception ex2)
        //        {
        //            apiVersionRespObj.message = ex2.InnerException?.Message ?? ex2.Message;
        //            return Ok(apiVersionRespObj);
        //        }
        //    }
        //}

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetAPIServerV2")]
        public async Task<IActionResult> GetAPIServerV3([FromBody][Bind("appVersion,username")] APIVersionRequestWithAppUpdateCheck model)
        {
            if (model == null)
            {
                return Ok(new APIVersionResponseWithAppUpdateCheckRev()
                {
                    message = "Request model cannot be null.",
                    isError = true
                });
            }

            APIVersionResponseRev apiVersionRespObj = new APIVersionResponseRev();
            int apiVersion = 0;

            Log.Information("GetAPIServerV3 Started. Username: {Username}", model.username);

            try
            {
                apiVersion = await _bLLUserAuthenticaion.GetUserAPIVersion(new APIVersionRequest { username = model.username });

                if (apiVersion == 0)
                {
                    Log.Warning("User not found for Username: {Username}", model.username);

                    return Ok(new APIVersionResponseWithAppUpdateCheckRev()
                    {
                        message = MessageCollection.UserNotFound,
                        isError = true,
                        data = new AppUpdateInfoV2
                        {
                            is_update_exists = false,
                            is_update_mandatory = 0,
                            api_version = 0,
                            update_url = string.Empty
                        }
                    });
                }

                if (model.appVersion.HasValue)
                {
                    var apiUpdateData = await _bLLUserAuthenticaion.GetUserAPIVersionWithAppUpdateCheckV2(
                        new APIVersionRequestWithAppUpdateCheck
                        {
                            username = model.username,
                            appVersion = model.appVersion
                        });

                    apiUpdateData.data.api_version = apiVersion;
                    apiUpdateData.message = apiVersion == 1 ? "Old version." : "New version.";

                    Log.Information("GetAPIServerV3 Completed With App Update Check. Username: {Username}", model.username);

                    return Ok(apiUpdateData);
                }

                if (apiVersion == 1)
                {
                    apiVersionRespObj.isError = false;
                    apiVersionRespObj.message = "Old version.";
                    apiVersionRespObj.data = new APIVersionData()
                    {
                        api_version = apiVersion
                    };
                }
                else
                {
                    apiVersionRespObj.isError = false;
                    apiVersionRespObj.message = "New version.";
                    apiVersionRespObj.data = new APIVersionData()
                    {
                        api_version = apiVersion
                    };
                }

                Log.Information("GetAPIServerV3 Completed Successfully. Username: {Username}", model.username);

                return Ok(apiVersionRespObj);
            }
            catch (Exception ex)
            {
                // ===== 504 / Timeout Detection =====
                if (ex.Message.Contains("504") ||
                    ex.InnerException?.Message.Contains("504") == true ||
                    ex is TaskCanceledException ||
                    ex is TimeoutException ||
                    ex is OperationCanceledException)
                {
                    Log.Error(ex, "504 or Timeout detected in GetAPIServerV3 for Username: {Username}", model.username);
                }
                else
                {
                    Log.Error(ex, "Unhandled Exception in GetAPIServerV3 for Username: {Username}", model.username);
                }

                apiVersionRespObj.isError = true;
                apiVersionRespObj.data.api_version = 0;

                try
                {
                    var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    apiVersionRespObj.message = !String.IsNullOrEmpty(error.error_custom_msg)
                        ? error.error_custom_msg
                        : error.error_description;

                    return Ok(apiVersionRespObj);
                }
                catch (Exception ex2)
                {
                    Log.Error(ex2, "Secondary Exception while Managing Exception.");

                    apiVersionRespObj.message = ex2.InnerException?.Message ?? ex2.Message;
                    return Ok(apiVersionRespObj);
                }
            }
            finally
            {
                Log.Information("GetAPIServerV3 Finished Execution. Username: {Username}", model?.username);
            }
        }


        //[GzipCompression]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        //[ResponseType(typeof(APIVersionResponseWithAppUpdateCheck))]
        [Route("GetAPIServerV3")]
        public async Task<IActionResult> GetAPIServerV4([FromBody][Bind("username")] APIVersionRequestWithAppUpdateCheckForGotPass model)
        {
            APIVersionResponseRev apiVersionRespObj = new APIVersionResponseRev();
            int apiVersion = 0;
            try
            {
                apiVersion = await _bLLUserAuthenticaion.GetUserAPIVersion(new APIVersionRequest { username = model.username });

                if (apiVersion == 0)
                {
                    return Ok(new APIVersionResponseWithAppUpdateCheckRev()
                    {
                        message = MessageCollection.UserNotFound,
                        isError = true,
                        data = new AppUpdateInfoV2
                        {
                            is_update_exists = false,
                            is_update_mandatory = 0,
                            api_version = 0,
                            update_url = string.Empty
                        }

                    });
                }
                //=========for change pwd=========
                if (apiVersion == 1)
                {
                    apiVersionRespObj.isError = false;
                    apiVersionRespObj.message = "Old version.";
                    apiVersionRespObj.data = new APIVersionData()
                    {
                        api_version = apiVersion
                    };
                }
                else
                {
                    apiVersionRespObj.isError = false;
                    apiVersionRespObj.message = "New version.";
                    apiVersionRespObj.data = new APIVersionData()
                    {
                        api_version = apiVersion
                    };
                }
                return Ok(apiVersionRespObj);
                //=========x==============
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                apiVersionRespObj.isError = true;
                apiVersionRespObj.data.api_version = 0;

                try
                {
                    var error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                    apiVersionRespObj.message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description;

                    return Ok(apiVersionRespObj);
                }
                catch (Exception ex2)
                {
                    apiVersionRespObj.message = ex2.InnerException?.Message ?? ex2.Message;
                    return Ok(apiVersionRespObj);
                }
            }
        }


        /// <summary>
        /// API for change password
        /// </summary>
        /// <param name="changePassword">Requesting parameter with old password and new password</param>
        /// <returns>Return reuslt of logout request</returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody][Bind("new_password,old_password,session_token,user_id")] ChangePasswordRequests changePasswordReq)
        {
            try
            {
                if (!await _apiManager.ValidUserBySecurityToken(changePasswordReq.session_token))
                    throw new Exception("Invalid Session Token.");

                var validationResult = await _bLLUserAuthenticaion.IsPasswordFormatValid(changePasswordReq.new_password);

                if (validationResult.Item1 == false)
                    return Ok(new RACommonResponse
                    {
                        result = validationResult.Item1,
                        message = validationResult.Item2
                    });
                else
                    return Ok(_bLLUserAuthenticaion.ChangePassword(changePasswordReq));
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
                ErrorDescription error;
                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    return Ok(new RACommonResponse
                    {
                        result = false,
                        message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                    });
                }
                catch (Exception)
                {
                    return Ok(new RACommonResponse
                    {
                        result = false,
                        message = "Failed! " + ex.Message
                    });
                }
            }
        }

        /// <summary>
        /// API for change password
        /// </summary>
        /// <param name="changePassword">Requesting parameter with old password and new password</param>
        /// <returns>Return reuslt of logout request</returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ChangePasswordV2")]
        public async Task<IActionResult> ChangePasswordV2([FromBody][Bind("new_password,old_password,session_token,user_id")] ChangePasswordRequests changePasswordReq)
        {
            try
            {
                if (!await _apiManager.ValidUserBySecurityTokenV2(changePasswordReq.session_token))
                    throw new Exception("Invalid Session Token.");

                var validationResult = await _bLLUserAuthenticaion.IsPasswordFormatValidV2(changePasswordReq.new_password);

                if (validationResult.Item1 == false)
                    return Ok(new RACommonResponse
                    {
                        result = validationResult.Item1,
                        message = validationResult.Item2
                    });
                else
                    return Ok(_bLLUserAuthenticaion.ChangePasswordV2(changePasswordReq));
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
                ErrorDescription error;
                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    return Ok(new RACommonResponse
                    {
                        result = false,
                        message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                    });
                }
                catch (Exception)
                {
                    return Ok(new RACommonResponse
                    {
                        result = false,
                        message = "Failed! " + ex.Message
                    });
                }
            }
        }

        /// <summary>
        /// API for change password
        /// </summary>
        /// <param name="changePassword">Requesting parameter with old password and new password</param>
        /// <returns>Return reuslt of logout request</returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ChangePasswordV3")]
        public async Task<IActionResult> ChangePasswordV3([FromBody][Bind("new_password,old_password,session_token,user_id")] ChangePasswordRequests changePasswordReq)
        {
            ValidTokenResponse security = new ValidTokenResponse();
            RACommonResponseRevamp response = new RACommonResponseRevamp();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(changePasswordReq.session_token);

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

                var validationResult = await _bLLUserAuthenticaion.IsPasswordFormatValidV2(changePasswordReq.new_password);

                if (validationResult.Item1 == true)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = validationResult.Item1,
                        message = validationResult.Item2
                    });
                }
                else
                {
                    response = await _bLLUserAuthenticaion.ChangePasswordV4(changePasswordReq);
                    return Ok(response);
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
                ErrorDescription error;
                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                    });
                }
                catch (Exception)
                {
                    return Ok(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = "Failed! " + ex.Message
                    });
                }
            }
        }

        /// <summary>
        /// Get-Password-Length
        /// </summary>
        /// <param name="changePassword"></param>
        /// <returns>Return reuslt of logout request</returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetPasswordLength")]
        public async Task<IActionResult> GetPasswordLength([FromBody][Bind("right_id,session_token")] RACommonRequest raRequest)
        {
            try
            {
                if (!await _apiManager.ValidUserBySecurityToken(raRequest.session_token))
                    throw new Exception("Invalid Session Token.");

                return Ok(_bLLUserAuthenticaion.GetPasswordLength());
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
                return Ok(new RACommonResponse
                {
                    result = false,
                    message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                });
            }
        }


        /// <summary>
        /// Get-Password-Length
        /// </summary>
        /// <param name="changePassword"></param>
        /// <returns>Return reuslt of logout request</returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetPasswordLengthV2")]
        public async Task<IActionResult> GetPasswordLengthV2([FromBody][Bind("right_id,session_token")] RACommonRequest radReq)
        {
            try
            {
                if (!await _apiManager.ValidUserBySecurityTokenV2(radReq.session_token))
                    throw new Exception("Invalid Session Token.");

                return Ok(_bLLUserAuthenticaion.GetPasswordLengthV2());
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
                return Ok(new RACommonResponse
                {
                    result = false,
                    message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                });
            }
        }

        /// <summary>
        /// Get-Password-Length 
        /// </summary>
        /// <param name="changePassword"></param>
        /// <returns>Return reuslt of logout request</returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("GetPasswordLengthV3")]
        public async Task<IActionResult> GetPasswordLengthV3([FromBody][Bind("right_id,session_token")] RACommonRequest radReq)
        {
            ValidTokenResponse security = new ValidTokenResponse();
            RAPassLenResponse rAPassLenResponse = new RAPassLenResponse();
            RAPassLenResponseV2 rAPass = new RAPassLenResponseV2();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(radReq.session_token);

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

                rAPassLenResponse = await _bLLUserAuthenticaion.GetPasswordLengthV2();

                return Ok(new RAPassLenResponseV2()
                {
                    data = new PasswordLenthData()
                    {
                        length = rAPassLenResponse.length,
                    },
                    isError = rAPassLenResponse.result == true ? false : true,
                    message = rAPassLenResponse.message
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
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                return Ok(new RACommonResponse
                {
                    result = false,
                    message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                });
            }
        }


        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ForgetPwd")]
        public async Task<IActionResult> ForgetPwd(VMUserInfoForForgetPWD model)
        {
            RACommonResponse raResp = new RACommonResponse();
            try
            {
                var userInfo = await _bLLUserAuthenticaion.GetUserMobileNoAndNewPWD(model.user_name);

                if (userInfo.user_id > 0)
                {
                    raResp = await _bLLUserAuthenticaion.FORGETPWD(new VMForgetPWD()
                    {
                        user_id = userInfo.user_id,
                        mobile_no = userInfo.mobile_no,
                        new_pwd = userInfo.PWD,
                        new_hashed_pwd = Cryptography.Encrypt(userInfo.PWD, true)
                    });

                    return Ok(raResp);
                }
                else
                {
                    raResp.result = false;
                    raResp.message = userInfo.message;
                    return Ok(raResp);
                }
            }
            catch (Exception ex)
            {
                ErrorDescription error;
                try
                {
                    error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                    return Ok(new RACommonResponse
                    {
                        result = false,
                        message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                    });
                }
                catch (Exception)
                {
                    return Ok(new RACommonResponse
                    {
                        result = false,
                        message = "Failed! " + ex.Message
                    });
                }
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ForgetPwdV2")]
        public async Task<IActionResult> ForgetPwdV2(VMUserInfoForForgetPWD model)
        {
            RACommonResponse raResp = new RACommonResponse();
            try
            {
                var userInfo = await _bLLUserAuthenticaion.GetUserMobileNoAndNewPWDV2(model.user_name);

                if (userInfo.user_id > 0)
                {
                    int isEligible = Convert.ToInt32(SettingsValues.GetIsEligibleAES());

                    if (isEligible == 1)
                    {
                        bool isEligibleUser = await _bLLUserAuthenticaion.IsAESEligibleUser(model.user_name);
                        if (isEligibleUser == true)
                        {
                            raResp = await _bLLUserAuthenticaion.FORGETPWDV2(new VMForgetPWD()
                            {
                                user_id = userInfo.user_id,
                                mobile_no = userInfo.mobile_no,
                                new_pwd = userInfo.PWD,
                                new_hashed_pwd = AESCryptography.Encrypt(userInfo.PWD)
                            });
                        }
                        else
                        {
                            raResp = await _bLLUserAuthenticaion.FORGETPWDV2(new VMForgetPWD()
                            {
                                user_id = userInfo.user_id,
                                mobile_no = userInfo.mobile_no,
                                new_pwd = userInfo.PWD,
                                new_hashed_pwd = Cryptography.Encrypt(userInfo.PWD, true)
                            });
                        }
                    }
                    else
                    {
                        raResp = await _bLLUserAuthenticaion.FORGETPWDV2(new VMForgetPWD()
                        {
                            user_id = userInfo.user_id,
                            mobile_no = userInfo.mobile_no,
                            new_pwd = userInfo.PWD,
                            new_hashed_pwd = AESCryptography.Encrypt(userInfo.PWD)
                        });
                    }

                    return Ok(raResp);
                }
                else
                {
                    raResp.result = false;
                    raResp.message = userInfo.message;
                    return Ok(raResp);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                return Ok(new RACommonResponse
                {
                    result = false,
                    message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("ForgetPwdV3")]
        public async Task<IActionResult> ForgetPwdV3(VMUserInfoForForgetPWD model)
        {
            RACommonResponseRevamp raResp = new RACommonResponseRevamp();
            try
            {
                var userInfo = await _bLLUserAuthenticaion.GetUserMobileNoAndNewPWDV2(model.user_name);

                if (userInfo.user_id > 0)
                {
                    int isEligible = 0;
                    isEligible = Convert.ToInt32(SettingsValues.GetIsEligibleAES());

                    if (isEligible == 1)
                    {
                        bool isEligibleUser = await _bLLUserAuthenticaion.IsAESEligibleUser(model.user_name);
                        if (isEligibleUser == true)
                        {
                            raResp = await _bLLUserAuthenticaion.FORGETPWDV3(new VMForgetPWD()
                            {
                                user_id = userInfo.user_id,
                                mobile_no = userInfo.mobile_no,
                                new_pwd = userInfo.PWD,
                                new_hashed_pwd = AESCryptography.Encrypt(userInfo.PWD)
                            });
                        }
                        else
                        {
                            raResp = await _bLLUserAuthenticaion.FORGETPWDV3(new VMForgetPWD()
                            {
                                user_id = userInfo.user_id,
                                mobile_no = userInfo.mobile_no,
                                new_pwd = userInfo.PWD,
                                new_hashed_pwd = Cryptography.Encrypt(userInfo.PWD, true)
                            });
                        }
                    }
                    else
                    {
                        raResp = await _bLLUserAuthenticaion.FORGETPWDV3(new VMForgetPWD()
                        {
                            user_id = userInfo.user_id,
                            mobile_no = userInfo.mobile_no,
                            new_pwd = userInfo.PWD,
                            new_hashed_pwd = AESCryptography.Encrypt(userInfo.PWD)
                        });
                    }

                    return Ok(raResp);
                }
                else
                {
                    raResp.isError = true;
                    raResp.message = userInfo.message;
                    return Ok(raResp);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                return Ok(new RACommonResponseRevamp
                {
                    isError = true,
                    message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
                });
            }
        }

        private string GetEncriptedSecurityToken(string loginProvider, string userId, string userName, string distributorCode, object? deviceId)
        {
            try
            {
                return Cryptography.Encrypt(String.Format(StringFormatCollection.AccessTokenFormat, loginProvider, userId, userName, distributorCode, deviceId), true);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string GetEncriptedSecurityTokenV2(string loginProvider, string userId, string userName, string distributorCode, object? deviceId)
        {
            try
            {
                return AESCryptography.Encrypt(String.Format(StringFormatCollection.AccessTokenFormatV2, loginProvider, userId, userName, distributorCode, deviceId, Guid.NewGuid()));
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Reseller Login [without password] 
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        //[ResponseType(typeof(LogInResponse))]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("ResellerLogin")]
        public async Task<IActionResult> ResellerLogin([FromBody][Bind("DeviceId,FermwareVersion,InstalledApps,KernelVersion,Lan,OSVersion,Type,UserName,VersionCode,VersionName")] ResellerLoginRequests login)
        {
            try
            {
                ResellerLoginUserInfoResponse? user = await _bLLUserAuthenticaion.ValidateUserReseller(login.UserName);

                if (user == null || user.user_name == null)
                {
                    return Ok(CreateFailedLoginResponse(MessageCollection.InvalidUserCridential));
                }

                string? loginProviderId = string.Empty;
                //string? loginProviderId = await _bLLUserAuthenticaion.IsUserCurrentlyLoggedIn(user.user_id);
                string loginProvider = string.IsNullOrEmpty(loginProviderId) ? Guid.NewGuid().ToString() : loginProviderId;

                var loginAttempt = CreateLoginAttempt(login, user, loginProvider);
                await _bLLUserAuthenticaion.SaveLoginAtmInfo(loginAttempt);

                var response = CreateSuccessfulLoginResponse(login, user, loginProvider);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");

                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                string errorMessage = !string.IsNullOrEmpty(error.error_custom_msg)
                    ? error.error_custom_msg
                    : error.error_description;

                return Ok(new RACommonResponse
                {
                    result = false,
                    message = errorMessage
                });
            }
        }

        private LogInResponse CreateFailedLoginResponse(string message)
        {
            return new LogInResponse
            {
                ISAuthenticate = false,
                AuthenticationMessage = message,
                HasUpdate = false,
            };
        }
        private UserLogInAttempt CreateLoginAttempt(ResellerLoginRequests login, ResellerLoginUserInfoResponse user, string loginProvider)
        {
            return new UserLogInAttempt
            {
                userid = user.user_id,
                is_success = 1,
                ip_address = GetIP(),
                loginprovider = loginProvider,
                deviceid = login.DeviceId ?? "",
                lan = login.Lan,
                versioncode = login.VersionCode,
                versionname = login.VersionName,
                osversion = login.OSVersion,
                kernelversion = login.KernelVersion,
                fermwarevirsion = login.FermwareVersion
            };
        }

        private LogInResponse CreateSuccessfulLoginResponse(ResellerLoginRequests login, ResellerLoginUserInfoResponse user, string loginProvider)
        {
            string secretKey = SettingsValues.GetJWTSequrityKey();
            TokenService tokenService = new TokenService(secretKey);
            return new LogInResponse
            {
                SessionToken = GetEncriptedSecurityToken(loginProvider, user.user_id, user.user_name, user.distributor_code, 0), //tokenService.GenerateTokenForRetailerLogin(user, loginProvider),
                ISAuthenticate = true,
                AuthenticationMessage = MessageCollection.UserValidted,
                UserName = login.UserName,
                Password = "******",
                DeviceId = login.DeviceId,
                HasUpdate = false,
                MinimumScore = SettingsValues.GetFPDefaultScore(),
                OptionalMinimumScore = "30",
                MaximumRetry = "2",
                RoleAccess = user.role_access,
                ChannelId = user.channel_id,
                ChannelName = user.channel_name,
                InventoryId = user.inventory_id,
                CenterCode = user.center_code,
                is_etsaf_validation_need = SettingsValues.GetETSAFValidationValue()
            };
        }

        //public async Task<IActionResult> ResellerLogin([FromBody][Bind("DeviceId,FermwareVersion,InstalledApps,KernelVersion,Lan,OSVersion,Type,UserName,VersionCode,VersionName")] ResellerLoginRequests login)
        //{
        //    try
        //    {
        //        ResellerLoginUserInfoResponse user = await _bLLUserAuthenticaion.ValidateUser(login.UserName);

        //        if (user.user_name == null)
        //        {
        //            return Ok(new LogInResponse()
        //            {
        //                ISAuthenticate = false,
        //                AuthenticationMessage = MessageCollection.InvalidUserCridential,
        //                HasUpdate = false,
        //            });
        //        }

        //        string loginProviderId = await _bLLUserAuthenticaion.IsUserCurrentlyLoggedIn(user.user_id);

        //        UserLogInAttempt loginAtmInfo;
        //        string loginProvider = Guid.NewGuid().ToString();

        //        if (String.IsNullOrEmpty(loginProviderId))
        //        {

        //            loginAtmInfo = new UserLogInAttempt()
        //            {
        //                userid = user.user_id,
        //                is_success = user != null ? 1 : 0,
        //                ip_address = GetIP(),
        //                loginprovider = loginProvider,
        //                deviceid = login.DeviceId,
        //                lan = login.Lan,
        //                versioncode = login.VersionCode,
        //                versionname = login.VersionName,
        //                osversion = login.OSVersion,
        //                kernelversion = login.KernelVersion,
        //                fermwarevirsion = login.FermwareVersion
        //            };
        //        }
        //        else
        //        {
        //            loginProvider = loginProviderId;

        //            loginAtmInfo = new UserLogInAttempt()
        //            {
        //                userid = user.user_id,
        //                is_success = user != null ? 1 : 0,
        //                ip_address = GetIP(),
        //                loginprovider = loginProvider,
        //                deviceid = login.DeviceId,
        //                lan = login.Lan,
        //                versioncode = login.VersionCode,
        //                versionname = login.VersionName,
        //                osversion = login.OSVersion,
        //                kernelversion = login.KernelVersion,
        //                fermwarevirsion = login.FermwareVersion
        //            };
        //        }

        //        await _bLLUserAuthenticaion.SaveLoginAtmInfo(loginAtmInfo);                

        //        return Ok(new LogInResponse()
        //        {
        //            SessionToken = GetEncriptedSecurityToken(loginProvider, user.user_id, user.user_name, user.distributor_code, login.DeviceId),
        //            ISAuthenticate = true,
        //            AuthenticationMessage = MessageCollection.UserValidted,
        //            UserName = login.UserName,
        //            Password = user.password,
        //            DeviceId = login.DeviceId,
        //            HasUpdate = false,
        //            MinimumScore = SettingsValues.GetFPDefaultScore(),
        //            OptionalMinimumScore = "30",
        //            MaximumRetry = "2",
        //            RoleAccess = user.role_access,
        //            ChannelId = user.channel_id,
        //            ChannelName = user.channel_name,
        //            InventoryId = user.inventory_id,
        //            CenterCode = user.center_code,
        //            //Designation = user.designation,
        //            is_etsaf_validation_need = SettingsValues.GetETSAFValidationValue()
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "ExMessage");
        //        ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

        //        return Ok(new RACommonResponse
        //        {
        //            result = false,
        //            message = !String.IsNullOrEmpty(error.error_custom_msg) ? error.error_custom_msg : error.error_description
        //        });
        //    }
        //}

        /// <summary>
        /// Reseller Login [without password] 
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        //[ResponseType(typeof(LogInResponse))]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("ResellerLoginV2")]
        public async Task<IActionResult> ResellerLoginV2([FromBody][Bind("DeviceId,FermwareVersion,InstalledApps,KernelVersion,Lan,OSVersion,Type,UserName,VersionCode,VersionName")] ResellerLoginRequests login)
        {
            try
            {
                ResellerLoginUserInfoResponse? user = await _bLLUserAuthenticaion.ValidateUser(login.UserName);

                if (user == null || user.user_name == null)
                {
                    return Ok(CreateFailedLoginResponse(MessageCollection.InvalidUserCridential));
                }

                string? loginProviderId = await _bLLUserAuthenticaion.IsUserCurrentlyLoggedIn(user.user_id);
                string loginProvider = string.IsNullOrEmpty(loginProviderId) ? Guid.NewGuid().ToString() : loginProviderId;

                var loginAttempt = CreateLoginAttempt(login, user, loginProvider);
                await _bLLUserAuthenticaion.SaveLoginAtmInfo(loginAttempt);

                var response = CreateSuccessfulLoginResponse(login, user, loginProvider);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");

                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                string errorMessage = !string.IsNullOrEmpty(error.error_custom_msg)
                    ? error.error_custom_msg
                    : error.error_description;

                return Ok(new RACommonResponse
                {
                    result = false,
                    message = errorMessage
                });
            }
        }



        /// <summary>
        /// DBSS OTP Validation API for Reseller App
        /// </summary>
        /// <param name="otpValidationReq"></param>
        /// <returns></returns>
        //[ResponseType(typeof(OTPResponse))]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("ValidateDBSSOTP")]
        public async Task<IActionResult> ValidateDBSSOTPV1([FromBody][Bind("dest_msisdn,otp,purpose_number,retailer_id,right_id,session_token,src_msisdn")] DBSSOTPValidationReq otpValidationReq)
        {
            try
            {
                if (!await _apiManager.ValidUserBySecurityToken(otpValidationReq.session_token))
                    throw new Exception(MessageCollection.InvalidSecurityToken);

                OTPResponse otpResponse = await _bio.ValidateOTP(new DBSSOTPValidationRequest()
                {
                    otp = otpValidationReq.otp,
                    poc_msisdn = ConverterHelper.MSISDNCountryCodeAddition(otpValidationReq.src_msisdn, FixedValueCollection.MSISDNCountryCode),
                    auth_msisdn = ConverterHelper.MSISDNCountryCodeAddition(otpValidationReq.dest_msisdn, FixedValueCollection.MSISDNCountryCode),
                    purpose = Convert.ToInt16(otpValidationReq.purpose_number)
                }, otpValidationReq.retailer_id);

                return Ok(otpResponse);
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
                return Ok(new RACommonResponse()
                {
                    result = false,
                    message = ex.Message
                });
            }
        }

        //[ResponseType(typeof(OTPResponse))]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("ValidateDBSSOTPV2")]
        public async Task<IActionResult> ValidateDBSSOTPV2([FromBody][Bind("dest_msisdn,otp,purpose_number,retailer_id,right_id,session_token,src_msisdn")] DBSSOTPValidationReq otpValidationReq)
        {
            try
            {
                if (!await _apiManager.ValidUserBySecurityTokenV2(otpValidationReq.session_token))
                    throw new Exception(MessageCollection.InvalidSecurityToken);

                OTPResponse otpResp = await _bio.ValidateOTP(new DBSSOTPValidationRequest()
                {
                    otp = otpValidationReq.otp,
                    poc_msisdn = ConverterHelper.MSISDNCountryCodeAddition(otpValidationReq.src_msisdn, FixedValueCollection.MSISDNCountryCode),
                    auth_msisdn = ConverterHelper.MSISDNCountryCodeAddition(otpValidationReq.dest_msisdn, FixedValueCollection.MSISDNCountryCode),
                    purpose = Convert.ToInt16(otpValidationReq.purpose_number)
                }, otpValidationReq.retailer_id);

                return Ok(otpResp);
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
                return Ok(new RACommonResponse()
                {
                    result = false,
                    message = ex.Message
                });
            }
        }

        //[ResponseType(typeof(OTPResponse))]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("ValidateDBSSOTPV3")]
        public async Task<IActionResult> ValidateDBSSOTPV3([FromBody][Bind("dest_msisdn,otp,purpose_number,retailer_id,right_id,session_token,src_msisdn")] DBSSOTPValidationReq otpValidationReq)
        {
            ValidTokenResponse security = new ValidTokenResponse();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(otpValidationReq.session_token);

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

                OTPResponseRev otpResp = await _bio.ValidateOTPV2(new DBSSOTPValidationRequest()
                {
                    otp = otpValidationReq.otp,
                    poc_msisdn = ConverterHelper.MSISDNCountryCodeAddition(otpValidationReq.src_msisdn, FixedValueCollection.MSISDNCountryCode),
                    auth_msisdn = ConverterHelper.MSISDNCountryCodeAddition(otpValidationReq.dest_msisdn, FixedValueCollection.MSISDNCountryCode),
                    purpose = Convert.ToInt16(otpValidationReq.purpose_number)
                }, otpValidationReq.retailer_id);

                return Ok(otpResp);
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
                string errorMessage = !string.IsNullOrEmpty(error.error_custom_msg)
                    ? error.error_custom_msg
                    : error.error_description;
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = errorMessage
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("ValidateBPOTP")]
        public async Task<IActionResult> ValidateBPOTP([FromBody][Bind("bp_otp,retailer_otp,right_id,session_token")] BPOtpValidationReq bPOtpValidationReq)
        {
            try
            {
                if (!await _apiManager.ValidUserBySecurityTokenForBPLogin(bPOtpValidationReq.session_token))
                    throw new Exception(MessageCollection.InvalidSecurityToken);

                string id = _bio.GetDecryptedSecurityToken(bPOtpValidationReq.session_token);

                if (id.Equals("Fail"))
                {
                    return Ok(new RACommonResponse()
                    {
                        result = false,
                        message = "Invalid Security Token"
                    });
                }

                BPOtpValidationRes otpResp = new BPOtpValidationRes();

                otpResp = await _bLLUserAuthenticaion.ValidateBPOtp(Convert.ToDecimal(bPOtpValidationReq.bp_otp), Convert.ToDecimal(bPOtpValidationReq.retailer_otp), id);

                return Ok(new RACommonResponse()
                {
                    result = otpResp.is_otp_valid,
                    message = otpResp.err_msg
                });
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
                return Ok(new RACommonResponse()
                {
                    result = false,
                    message = ex.Message
                });
            }
        }


        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("ValidateBPOTPV2")]
        public async Task<IActionResult> ValidateBPOTPV2([FromBody][Bind("bp_otp,retailer_otp,right_id,session_token")] BPOtpValidationReq bPOtpValidationReq)
        {
            ValidTokenResponse security = new ValidTokenResponse();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();

                TokenValidationService token = new TokenValidationService(secreteKey);

                security = token.ValidateToken(bPOtpValidationReq.session_token);

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

                BPOtpValidationRes otpResp = new BPOtpValidationRes();

                otpResp = await _bLLUserAuthenticaion.ValidateBPOtpV2(Convert.ToDecimal(bPOtpValidationReq.bp_otp), Convert.ToDecimal(bPOtpValidationReq.retailer_otp), loginProviderId);

                return Ok(new RACommonResponseRevamp()
                {
                    isError = otpResp.is_otp_valid,
                    message = otpResp.err_msg
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
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                string errorMessage = !string.IsNullOrEmpty(error.error_custom_msg)
                    ? error.error_custom_msg
                    : error.error_description;
                return Ok(new RACommonResponseRevamp()
                {
                    isError = true,
                    message = errorMessage
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("ResendBPOTP")]
        public async Task<IActionResult> ResendBPOTP([FromBody][Bind("right_id,session_token")] RACommonRequest model)
        {
            try
            {
                if (!await _apiManager.ValidUserBySecurityTokenForBPLogin(model.session_token))
                    throw new Exception(MessageCollection.InvalidSecurityToken);

                string id = _bio.GetDecryptedSecurityToken(model.session_token);

                if (id.Equals("Fail"))
                {
                    return Ok(new RACommonResponse()
                    {
                        result = false,
                        message = "Invalid Security Token"
                    });
                }

                bool is_success = await _bLLUserAuthenticaion.ResendBPOTP(id);

                if (is_success)
                {
                    return Ok(new RACommonResponse()
                    {
                        result = true,
                        message = "OTP Resent Successfully"
                    });
                }
                else
                {
                    return Ok(new RACommonResponse()
                    {
                        result = true,
                        message = "Failed to send OTP."
                    });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    result = true,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Ok(new RACommonResponse()
                {
                    result = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("ResendBPOTPV2")]
        public async Task<IActionResult> ResendBPOTPV2([FromBody][Bind("right_id,session_token")] RACommonRequest model)
        {
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

                bool is_success = await _bLLUserAuthenticaion.ResendBPOTPV2(loginProviderId);

                if (is_success)
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = false,
                        message = "OTP Resent Successfully"
                    });
                }
                else
                {
                    return Ok(new RACommonResponseRevamp()
                    {
                        isError = true,
                        message = "Failed to send OTP."
                    });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    result = true,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                string errorMessage = !string.IsNullOrEmpty(error.error_custom_msg)
                    ? error.error_custom_msg
                    : error.error_description;
                return Ok(new RACommonResponse()
                {
                    result = false,
                    message = errorMessage
                });
            }
        }

        /// <summary>
        /// Reseller Login [without password] 
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        //[ResponseType(typeof(LogInResponse))]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("Logout")]
        public async Task<IActionResult> Logout([FromBody][Bind("right_id,session_token")] RACommonRequest model)
        {
            ValidTokenResponse security = new ValidTokenResponse();
            try
            {
                string secreteKey = string.Empty;
                string loginProviderId = string.Empty;

                Thread logThread = new Thread(async () => await _bLLUserAuthenticaion.Logout(loginProviderId));
                logThread.Start();

                return Ok(new RACommonResponseRevamp
                {
                    isError = false,
                    message = "Successfully logout!"
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                string errorMessage = !string.IsNullOrEmpty(error.error_custom_msg)
                    ? error.error_custom_msg
                    : error.error_description;
                return Ok(new RACommonResponseRevamp
                {
                    isError = true,
                    message = errorMessage
                });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ValidateModel]
        [Route("externalLogin")] 
        public async Task<IActionResult> ExternalLogin([FromBody][Bind("password,username")] ExternalLoginReqModel login)
        {
            try
            {
                login.password = AESCryptography.Encrypt(login.password);
                ExternalUserValidationRespModel? user = await _bLLUserAuthenticaion.ValidateExternalUser(login);

                if (user == null || user.user_name == null)
                {
                    return Ok(CreateExternalFailedLoginResponse(MessageCollection.InvalidUserCridential));
                }
                else if(user.is_valid == false)
                {
                    return Ok(CreateExternalFailedLoginResponse(user.message));
                }

                string? loginProviderId = string.Empty;
                string loginProvider = string.IsNullOrEmpty(loginProviderId) ? Guid.NewGuid().ToString() : loginProviderId;

                var loginAttempt = CreateExternalLoginAttempt(login, user, loginProvider);
                await _bLLUserAuthenticaion.SaveLoginAtmInfo(loginAttempt);

                var response = CreateExternalSuccessfulLoginResponse(login, user, loginProvider);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");

                ErrorDescription error = await _bllLog.ManageException(ex, ex.HResult, "BIA");
                string errorMessage = !string.IsNullOrEmpty(error.error_custom_msg)
                    ? error.error_custom_msg
                    : error.error_description;
                return Ok(CreateExternalFailedLoginResponse(errorMessage));
            }
        }

        private ExternalLoginRespModel CreateExternalFailedLoginResponse(string message)
        {
            return new ExternalLoginRespModel
            {
                sessiontoken = "",
                isValid = false,
                message = message,
                validity = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                validity_time = 0
            };
        }
        private UserLogInAttempt CreateExternalLoginAttempt(ExternalLoginReqModel login, ExternalUserValidationRespModel user, string loginProvider)
        {
            return new UserLogInAttempt
            {
                userid = user.user_id,
                is_success = 1,
                ip_address = GetIP(),
                loginprovider = loginProvider,
                deviceid = "dms_fp_deleted",
                lan = "",
                versioncode = 0,
                versionname = "",
                osversion = "",
                kernelversion = "",
                fermwarevirsion = ""
            };
        }

        private ExternalLoginRespModel CreateExternalSuccessfulLoginResponse(ExternalLoginReqModel login, ExternalUserValidationRespModel user, string loginProvider)
        {
            string secretKey = SettingsValues.GetJWTSequrityKey();
            TokenService tokenService = new TokenService(secretKey);
            int validityMinutes = SettingsValues.GetExternalLoginExiprationTime(); // e.g. 30

            DateTime validityDateTime = DateTime.Now.AddMinutes(validityMinutes);
            return new ExternalLoginRespModel
            {
                sessiontoken = tokenService.GenerateTokenForExternal(user, loginProvider),
                isValid = true,
                message = MessageCollection.UserValidted,
                validity = validityDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                validity_time = SettingsValues.GetExternalLoginExiprationTime()
            };
        }


        #region helper methods
        private async Task<LogInResponse> SaveFailedLoginAndReturnResponse(LoginRequestsV2 login,string loginProvider,string errorMessage)
        {
            LogInResponse failedResponse = CreateErrorResponse(errorMessage);

            var failedLoginAttempt = CreateLoginAttempt(
                login.UserName,
                login,
                loginProvider);

            failedLoginAttempt.is_success = 0;

            if (string.IsNullOrEmpty(login.BPMSISDN))
            {
                failedLoginAttempt.is_bp = 0;
                failedLoginAttempt.bp_msisdn = null;
            }
            else
            {
                failedLoginAttempt.is_bp = 1;
                failedLoginAttempt.bp_msisdn = login.BPMSISDN;
            }

            await _bLLUserAuthenticaion.SaveFailedLoginAtmInfo(
                failedLoginAttempt,
                login,
                failedResponse,
                errorMessage);

            return failedResponse;
        }
        #endregion
    }
}
