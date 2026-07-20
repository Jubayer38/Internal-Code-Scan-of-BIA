using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using BIA.Entity.Utility;
using System;
using System.Collections.Concurrent;
using Serilog;

namespace BIA.Entity.Collections
{
    public static class SettingsValues
    {
        private static IConfiguration _config;

        private static readonly ConcurrentDictionary<string, Lazy<string>> _decryptedCache = new();
        private static readonly ConcurrentDictionary<string, Lazy<string>> _plainCache = new();
        private static readonly ConcurrentDictionary<string, Lazy<List<string>>> _listCache = new();

       static SettingsValues()
        {
            try
            {
                Directory.CreateDirectory("/app/log/applicationLogs");
                File.AppendAllText(
                    "/app/log/applicationLogs/bootstrap.log",
                    $"[BOOTSTRAP] Startup at {DateTime.UtcNow:O}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            var basePath = ResolveBasePath();

            var rawEnvironment = ResolveRawEnvironment();
            var rawCluster = Environment.GetEnvironmentVariable("CLUSTER");

            var normalizedCluster = NormalizeClusterName(rawCluster);

            // If CLUSTER is null/empty, force Development
            var normalizedEnvironment = string.IsNullOrWhiteSpace(normalizedCluster)
                ? "Development"
                : NormalizeEnvironmentName(rawEnvironment);

            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", normalizedEnvironment);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", normalizedEnvironment);

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            if (string.IsNullOrWhiteSpace(normalizedCluster))
            {
                configBuilder.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false);
            }
            else
            {
                configBuilder.AddJsonFile($"appsettings.{normalizedEnvironment}.json", optional: true, reloadOnChange: false);

                // Example:
                // CLUSTER=tst + ENV=dev  => appsettings.tstDevelopment.json
                // CLUSTER=gz  + ENV=prod => appsettings.gzProduction.json
                configBuilder.AddJsonFile(
                    $"appsettings.{normalizedCluster}{normalizedEnvironment}.json",
                    optional: true,
                    reloadOnChange: false);
            }

            configBuilder.AddEnvironmentVariables();

            _config = configBuilder.Build();

            try
            {
                Directory.CreateDirectory("/app/log/applicationLogs");

                File.AppendAllText(
                    "/app/log/applicationLogs/bootstrap.log",
                    $"[SettingsValues] BasePath = {basePath}{Environment.NewLine}");

                File.AppendAllText(
                    "/app/log/applicationLogs/bootstrap.log",
                    $"[SettingsValues] Raw Environment = {rawEnvironment}{Environment.NewLine}");

                File.AppendAllText(
                    "/app/log/applicationLogs/bootstrap.log",
                    $"[SettingsValues] Resolved Environment = {normalizedEnvironment}{Environment.NewLine}");

                File.AppendAllText(
                    "/app/log/applicationLogs/bootstrap.log",
                    $"[SettingsValues] Resolved Cluster = {normalizedCluster ?? "NULL"}{Environment.NewLine}");

                File.AppendAllText(
                    "/app/log/applicationLogs/bootstrap.log",
                    $"[SettingsValues] CONFIG MARKER = {_config["ConfigMarker"]}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            CreateDirectoryIfConfigured("AppSettings:ErrTxtLogFilePath");
            CreateDirectoryIfConfigured("AppSettings:ReportExportLocation");
        }

        public static void Initialize(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _decryptedCache.Clear();
            _plainCache.Clear();
            _listCache.Clear();
        }


        private static string ResolveRawEnvironment()
        {
            var environment =
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            return string.IsNullOrWhiteSpace(environment)
                ? "dev"
                : environment.Trim();
        }


        private static string NormalizeEnvironmentName(string? environment)
        {
            if (string.IsNullOrWhiteSpace(environment))
                return "Development";

            var value = environment.Trim().ToLowerInvariant();

            return value switch
            {
                "prod" or "production" or "prd" or "live" => "Production",

                "dev" or "development" or "developmen" or "devel" or "local" => "Development",

                _ when value.StartsWith("prod") => "Production",
                _ when value.StartsWith("dev") => "Development",

                _ => "Development"
            };
        }


        private static string? NormalizeClusterName(string? cluster)
        {
            if (string.IsNullOrWhiteSpace(cluster))
                return null;

            return cluster.Trim().ToLowerInvariant();
        }


        private static void CreateDirectoryIfConfigured(string key)
        {
            try
            {
                var path = _config[key];
                if (!string.IsNullOrWhiteSpace(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());   // ✅ LOG FIX
            }
        }


        private static string ResolveBasePath()
        {
            static bool HasConfig(string path) => File.Exists(Path.Combine(path, "appsettings.json"));

            var basePath = AppContext.BaseDirectory;
            if (HasConfig(basePath))
                return basePath;

            var currentDirectory = Directory.GetCurrentDirectory();
            if (HasConfig(currentDirectory))
                return currentDirectory;

            var directoryInfo = new DirectoryInfo(basePath);
            while (directoryInfo != null)
            {
                if (HasConfig(directoryInfo.FullName))
                    return directoryInfo.FullName;

                directoryInfo = directoryInfo.Parent;
            }

            return basePath;
        }


        private static string DecryptSection(string sectionKey)
        {
            if (_decryptedCache.TryGetValue(sectionKey, out var lazyValue))
            {
                var val = lazyValue.Value;
                if (!string.IsNullOrEmpty(val))
                {
                    return val;
                }
                _decryptedCache.TryRemove(sectionKey, out _);
            }

            var rawValue = _config[sectionKey];
            if (string.IsNullOrEmpty(rawValue))
            {
                return string.Empty;
            }

            string decryptedValue = string.Empty;
            try
            {
                decryptedValue = AESCryptography.Decrypt(rawValue);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AppSettings Parsing Exception: SupportId: {SupportId}");
            }

            if (!string.IsNullOrEmpty(decryptedValue))
            {
                _decryptedCache[sectionKey] = new Lazy<string>(() => decryptedValue);
            }
            return decryptedValue;
        }

        private static string GetSectionValue(string sectionKey)
        {
            if (_plainCache.TryGetValue(sectionKey, out var lazyValue))
            {
                var val = lazyValue.Value;
                if (!string.IsNullOrEmpty(val))
                {
                    return val;
                }
                _plainCache.TryRemove(sectionKey, out _);
            }

            var value = _config[sectionKey] ?? string.Empty;
            if (!string.IsNullOrEmpty(value))
            {
                _plainCache[sectionKey] = new Lazy<string>(() => value);
            }
            return value;
        }

        public static List<string> GetSectionList(string sectionKey)
        {
            if (_listCache.TryGetValue(sectionKey, out var lazyValue))
            {
                var val = lazyValue.Value;
                if (val != null && val.Count > 0)
                {
                    return val;
                }
                _listCache.TryRemove(sectionKey, out _);
            }

            var list = new List<string>();
            try
            {
                var section = _config.GetSection(sectionKey);
                var children = section.GetChildren();
                foreach (var child in children)
                {
                    if (!string.IsNullOrWhiteSpace(child.Value))
                        list.Add(child.Value);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AppSettings Parsing Exception: SupportId: {SupportId}");
            }

            if (list.Count > 0)
            {
                _listCache[sectionKey] = new Lazy<List<string>>(() => list);
            }
            return list;
        }

        private static int GetIntValue(string key) =>
            int.TryParse(_config[key], out var result) ? result : 0;

        private static long GetLongValue(string key) =>
            long.TryParse(_config[key], out var result) ? result : 0;

        private static double GetDoubleValue(string key) =>
            double.TryParse(_config[key], out var result) ? result : 0;

        // Connection Strings
        public static string GetConnectionString() =>
            DecryptSection("ConnectionStrings:DefaultConnectionString");

        // App Settings Decrypted
        public static string GetDbssBaseUrl() => DecryptSection("AppSettings:dbssBaseUri");
        public static string GetDMSBaseUrl() => DecryptSection("AppSettings:dmsBaseUrl");
        public static string GetDMSBaseUrlSIMValidation() => DecryptSection("AppSettings:DMSSIMValidationAPI");
        public static string GetDMSUserName() => DecryptSection("AppSettings:userName");
        public static string GetDMSPassword() => DecryptSection("AppSettings:dms_pas");
        public static string GetRetailerAppBaseUrl() => DecryptSection("AppSettings:RetailerBaseAPI");
        public static string GetRSOBaseUrl() => DecryptSection("AppSettings:RSOBaseAPI");
        public static string GetSingleSourceUrl() => DecryptSection("AppSettings:sigleSourceAPI");
        public static string GetSingleSourceUserName() => DecryptSection("AppSettings:single_source_userName");
        public static string GetSingleSourcePassword() => DecryptSection("AppSettings:single_source_pas");
        public static string GetJWTSequrityKey() => DecryptSection("AppSettings:tokenServiceKey");
        public static string GetRSOAppUserName() => DecryptSection("AppSettings:rso_user_Name");
        public static string GeteShopBaseUrl() => DecryptSection("AppSettings:eShopBaseUrl");
        public static string GeteShopCredential() => DecryptSection("AppSettings:eShopCredential");
        public static string GetEV_API_URL() => DecryptSection("AppSettings:BalanceCheckURL");
        public static string GetEV_API_QueryString() => DecryptSection("AppSettings:BalanceCheckQueryString");
        public static string GetEV_API_RequestBody() => DecryptSection("AppSettings:BalanceCheckBody");
        public static string GetAirBaseUrl() => DecryptSection("AppSettings:AirBaseUrl");
        public static string GetAirUserName() => DecryptSection("AppSettings:AirUserName");
        public static string GetAirPassword() => DecryptSection("AppSettings:AirCred");
        public static string GettokenSignK() => DecryptSection("SessionKeysJwetAES:SignKey");
        public static string GettokenEncK() => DecryptSection("SessionKeysJwetAES:EncryptKey");
        public static string GetIccCheckUserName() => DecryptSection("AppSettings:IccCheckUserName");
        public static string GetIccCheckPassword() => DecryptSection("AppSettings:IccCheckPassword");
        public static string GetTransactionDetails() => DecryptSection("AppSettings:EVTransactionDetailsURL");
        public static string GetDPEBaseUrl() => DecryptSection("AppSettings:DPEBaseUrl");
        public static string GetDPEClientId() => DecryptSection("AppSettings:DPEClientID");
        public static string GetDPEClientSecret() => DecryptSection("AppSettings:DPEClientSecret");
        public static string GetDPEClientCred() => DecryptSection("AppSettings:DPEClientCred");
        public static string GetDPEChannel() => DecryptSection("AppSettings:DPEChannel");
        public static string GetDPEOrderType() => DecryptSection("AppSettings:DPEOrderType");
        public static string GetDPELeadListEndPoint() => DecryptSection("AppSettings:DPELeadListEndPoint");
        public static string GetDPELeadDetailsEndPoint() => DecryptSection("AppSettings:DPELeadDetailsEndPoint");
        public static string GetDPEUpdateOrderEndPoint() => DecryptSection("AppSettings:DPEUpdateOrderEndPoint");
        public static string GetDPESendPaymentLinkEndPoint() => DecryptSection("AppSettings:DPESendPaymentLinkEndPoint");
        public static string GetDPELoginEndPoint() => DecryptSection("AppSettings:DPELoginEndPoint");
        public static string GetDPEImeiUpdateEndPoint() => DecryptSection("AppSettings:DPEImeiUpdateEndPoint");
        public static string GetDPELeadPreloadAPIendPoint() => DecryptSection("AppSettings:LeadPreloadAPIendPoint");
        public static string GetReferLeadAPIendPoint() => DecryptSection("AppSettings:ReferladAPIendPoint");
        public static string GetDPEOrderCancellationLockEndPoint() => DecryptSection("AppSettings:DPEOrderCancellationLockEndPoint");

       


        // App Settings Plain  
        public static string GetUserStatusUpdateUserName() => GetSectionValue("AppSettings:biometricRETUserName");
        public static string GetUserStatusUpdatePassword() => GetSectionValue("AppSettings:biometricRETPas");
        public static string GetSingleSourceMessage() => GetSectionValue("AppSettings:single_source_active_message");
        public static string GetFTRRequestType() => GetSectionValue("AppSettings:FTRRequestType");
        public static string GetFTRRequestChannel() => GetSectionValue("AppSettings:FTRRequestChannel");
        public static string GetAirAuthToken() => GetSectionValue("AppSettings:AirAuthToken");
        public static string GetFPDefaultScore() => GetSectionValue("AppSettings:FPDefaultScore");
        public static string GetSessionMessage() => GetSectionValue("AppSettings:session_message");
        public static string GetPaMessage() => GetSectionValue("AppSettings:pa_message");
        public static string GetNumberCategory() => GetSectionValue("AppSettings:number_category");
        public static string Getdedicated_Ac_Id() => GetSectionValue("AppSettings:dedicated_Ac_Id");
        public static string Getcherish_categories() => GetSectionValue("AppSettings:cherish_categories");
        public static string Getdedicated_Ac_Id_TOS() => GetSectionValue("AppSettings:dedicated_Ac_Id_TOS");
        public static string GetIsEligibleAES() => GetSectionValue("AppSettings:IsEligibleAES");
        public static string GeB2BtoB2CTwoPaertyValidationOTPNote() => GetSectionValue("AppSettings:B2BtoB2CTwoPaertyValidationOTPNote");
        public static string Getdefault_category() => GetSectionValue("AppSettings:default_category");
        public static string GetStockNotAllowFromRyze() => GetSectionValue("AppSettings:stocksNotAllowForRyze");
        public static string GetoriginNodeType() => GetSectionValue("AppSettings:originNodeType");
        public static string GetCreateCustomerRetry() => GetSectionValue("AppSettings:CreateCustomerRetry");
        public static string GetRSOComplainCred() => GetSectionValue("AppSettings:rso_app_pas");
        public static string GetRSOComplainType() => GetSectionValue("AppSettings:complaint_type");
        public static string GetRSOComplainTitle() => GetSectionValue("AppSettings:complaint_title");
        public static string GetRSOComplainPreferedLabel() => GetSectionValue("AppSettings:preferred_level");
        public static string GetRSOComplainPreferedLabelName() => GetSectionValue("AppSettings:preferred_level_name");
        public static string GetRSOComplainPreferedLabelContact() => GetSectionValue("AppSettings:preferrred_level_contact");
        public static string GetChannelId() => GetSectionValue("AppSettings:ChannelId");
        public static string GetChannelStockId() => GetSectionValue("AppSettings:ChannelStockId");
        public static string GetChannelStockDefault() => GetSectionValue("AppSettings:ChannelStockIdDefault");
        public static string Getproduct_code_prepaid() => GetSectionValue("AppSettings:product_code_prepaid");
        public static string Getproduct_code_Postpaid() => GetSectionValue("AppSettings:product_code_Postpaid");
        public static string Getproduct_category_prepaid() => GetSectionValue("AppSettings:product_category_prepaid");
        public static string Getproduct_category_postpaid() => GetSectionValue("AppSettings:product_category_postpaid");
        public static string Getproduct_category_simReplacment() => GetSectionValue("AppSettings:product_category_simReplacment");
        public static string Getproduct_code_simReplacment() => GetSectionValue("AppSettings:product_code_simReplacment");
        public static string Getp_code_starTrek_prepaid() => GetSectionValue("AppSettings:p_code_starTrek_prepaid");
        public static string Getp_code_starTrek_prepaid_esim() => GetSectionValue("AppSettings:p_code_starTrek_prepaid_esim");
        public static string Getproduct_category_StarTrekPrepaid() => GetSectionValue("AppSettings:product_category_StarTrekPrepaid");
        public static string Getproduct_category_StarTrekPrepaid_esim() => GetSectionValue("AppSettings:product_category_StarTrekPrepaid_esim");
        public static string GetJWETLoginProvider() => GetSectionValue("AppSettings:JWETLoginProvider");
        public static string GetAccessTokenFormat() => GetSectionValue("AppSettings:AccessTokenFormat");
        public static string GetAccessTokenFormatV2() => GetSectionValue("AppSettings:AccessTokenFormatV2");
        public static string GetBLOTPApiBaseUrl() => GetSectionValue("AppSettings:BLOTPApiBaseUrl");
        public static string GetInvalidSecurityToken() => GetSectionValue("AppSettings:InvalidSecurityToken");
        public static string GetValidAccessToken() => GetSectionValue("AppSettings:ValidAccessToken");
        public static string GetInvalidUserName() => GetSectionValue("AppSettings:InvalidUserName");
        public static string GetInvalidUserCridential() => GetSectionValue("AppSettings:InvalidUserCridential");
        public static string GetUserValidted() => GetSectionValue("AppSettings:UserValidted");
        public static string GetUserNotFound() => GetSectionValue("AppSettings:UserNotFound");
        public static string GetBP_DeviceLatlonNot_Found() => GetSectionValue("AppSettings:BP_DeviceLatlonNot_Found");
        public static string GetRetLatLonNotFound() => GetSectionValue("AppSettings:RetLatLonNotFound");
        public static string GetCrossesTheArea() => GetSectionValue("AppSettings:CrossesTheArea");
        public static string GetAESKey()
        {
            var envKey = Environment.GetEnvironmentVariable("BIA_KEY");
            return !string.IsNullOrWhiteSpace(envKey) ? envKey.Trim() : GetSectionValue("SessionKeysJwetAES:aes");
        }

        public static string GetAESIv()
        {
            var envIv = Environment.GetEnvironmentVariable("BIA_IV");
            return !string.IsNullOrWhiteSpace(envIv) ? envIv.Trim() : GetSectionValue("SessionKeysJwetAES:aes_iv");
        }
        public static string GetCherishCategory() => GetSectionValue("AppSettings:cherish_categories");
        public static string GetCherishDefaultCategory() => GetSectionValue("AppSettings:default_category");
        public static string GetActiveNumberCountEligibility() => GetSectionValue("AppSettings:ActiveNumberCountEligibility");
        public static string GetMMSTDProduct() => GetSectionValue("AppSettings:SIMProduct");
        public static string GetTOSRechargeNotFoundMessage() => GetSectionValue("AppSettings:TOSRechargeNotFoundMessage");
        public static string GetSubscriptionCode() => GetSectionValue("AppSettings:SubscriptionCodes");
        public static string GetReferChannelCode() => GetSectionValue("AppSettings:xchannel");
        public static string GetNWAssessOrderStatus() => GetSectionValue("AppSettings:nwAssessOrderStatus");

        // Int/Long/Double Values
        public static int GetDexPerPageValue() => GetIntValue("AppSettings:dexPerPage");
        public static int GetBTSCodeShowingOrNot() => GetIntValue("AppSettings:is_bts_show");
        public static int GetFTRRequestID() => GetIntValue("AppSettings:FTRRequestID");
        public static int GetIsRetailerAPICore() => GetIntValue("AppSettings:IsRetailerAppCore");
        public static int GetsubscriberNumberNAI() => GetIntValue("AppSettings:subscriberNumberNAI");
        public static long GetnegotiatedCapabilities() => GetLongValue("AppSettings:negotiatedCapabilities");
        public static int GetFTRExpairyDate() => GetIntValue("AppSettings:FTRExpairationMinute");
        public static int GetETSAFValidationValue() => GetIntValue("AppSettings:is_esafValidationNeed");
        public static int GetRyzeAllowOrNot() => GetIntValue("AppSettings:is_ryze_stock_allow");
        public static double GetallowedDistanceForGeo() => GetDoubleValue("AppSettings:GeofencingDistance");
        public static int GetgeoFencEnableEnability() => GetIntValue("AppSettings:GeofencingDistanceCalculateEnable");
        public static int GetaddMinutesForJWET() => GetIntValue("AppSettings:addMinutesForJWET");
        public static int GetsubstarctMinutesForJWET() => GetIntValue("AppSettings:substarctMinutesForJWET");
        public static int GetisFtrFeatureOn() => GetIntValue("AppSettings:isFtrFeatureOn");
        public static int Getcyn_cherished_filter_allow() => GetIntValue("AppSettings:cyn_cherished_filter_allow");
        public static int GetTOSValidationTime() => GetIntValue("AppSettings:TOSValidationTime");
        public static int GetTOSByPassTimeTime() => GetIntValue("AppSettings:TOSBypassTimeInMinutes");
        public static int GetSessionTokenExiprationTime() => GetIntValue("AppSettings:sessionExpirationInMinutes");
        public static int GetDbssSessionTokenExiprationTime() => GetIntValue("AppSettings:dbssSessionExpirationInMinutes");
        public static int GetMaxLoginAttempt() => GetIntValue("AppSettings:maxInvalidAttempt");
        public static int GetLockTime() => GetIntValue("AppSettings:lockTimeInMin");
        public static int GetExternalLoginExiprationTime() => GetIntValue("AppSettings:ExternalLoginExiprationTime");
        public static int GetIsRecycleCheckingNeeded() => GetIntValue("AppSettings:IsRecycleBaseCheckingNeeded");
        public static string GetLegacyCryptoKey()
        {
            var key = GetSectionValue("AppSettings:LegacyCryptoKey");
            return string.IsNullOrEmpty(key) ? "bl_smart_pos" : key;
        }
    }
}

