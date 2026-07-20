using BIA.BLL.BLLServices;
using BIA.BLL.Utility;
using BIA.Common;
using BIA.Controllers;
using BIA.DAL.DBManager;
using BIA.DAL.Repositories;
using BIA.Entity.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace BIA.Helper;

public static class ServiceRegistration
{
    public static void RegisterBiaServices(this IServiceCollection services)
    {
        // Database & Repository Layer
        services.AddScoped<OracleDataManagerV2>();
        services.AddScoped<DALBiometricRepo>();

        // Business Logic Services
        services.AddScoped<BLLCommon>();
        services.AddScoped<ApiRequest>();
        services.AddScoped<BLLOrder>();
        services.AddScoped<BLLLog>();
        services.AddScoped<BiometricApiCall>();
        services.AddScoped<GeoFencingValidation>();
        services.AddScoped<BLLUserAuthenticaion>();
        services.AddScoped<BLLSIMReplacement>();
        services.AddScoped<BLLRetailerUserSync>();
        services.AddScoped<BLLResubmit>();
        services.AddScoped<BLLRAToDBSSParse>();
        services.AddScoped<BLLRaiseComplaint>();
        services.AddScoped<BllOrderBssService>();
        services.AddScoped<BllHandleException>();
        services.AddScoped<BLLGeofencing>();
        services.AddScoped<BLLFTRRestriction>();
        services.AddScoped<BLLFirstRecharge>();
        services.AddScoped<BLLDynamic>();
        services.AddScoped<BLLDivDisThana>();
        services.AddScoped<BLLDBSSToRAParse>();
        services.AddScoped<BLLDBSSNotification>();
        services.AddScoped<BllBiometricBssService>();
        services.AddScoped<BL_Json>();
        services.AddScoped<ApiManager>();
        services.AddScoped<OrderScheduler>();
        services.AddScoped<OrderApiCall>();
        services.AddScoped<eShopAPICall>();
        services.AddScoped<ApiCall>();
        services.AddScoped<DataPopulateModel>();
        services.AddScoped<FTR_Restrict>();
        services.AddScoped<SingleSourceGACappingService>();
        services.AddScoped<BLLHomeWifiService>();

        // TECHNICAL DEBT: BaseController is registered as a dependency 
        // because child controllers inject it rather than using a shared helper service.
        // Keeping this to prevent breaking existing controller constructors.
        services.AddScoped<BaseController>();
    }
}
