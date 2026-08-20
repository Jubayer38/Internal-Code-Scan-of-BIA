using BIA.BLL.BLLServices;
using BIA.BLL.Utility;
using BIA.Common;
using BIA.Controllers;
using BIA.DAL.DBManager;
using BIA.DAL.Repositories;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.StaticClass;
using BIA.Entity.Utility;
using BIA.Helper;
using BIA.Middleware;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;

#region Configuration Setup
// Determine environment from command line or ASPNETCORE_ENVIRONMENT
var rawEnvironment =
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? GetEnvironmentFromArgs(args)
    ?? "dev";

var rawCluster = Environment.GetEnvironmentVariable("CLUSTER");

var normalizedEnvironment = NormalizeEnvironmentName(rawEnvironment);
var normalizedCluster = NormalizeClusterName(rawCluster);

// Set framework environment variables properly
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", normalizedEnvironment);
Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", normalizedEnvironment);

var configBuilder = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

if (string.IsNullOrWhiteSpace(normalizedCluster))
{
    // If CLUSTER is null/empty, load Development config
    configBuilder.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
}
else
{
    // Generic environment config
    // Example: appsettings.Development.json / appsettings.Production.json
    configBuilder.AddJsonFile($"appsettings.{normalizedEnvironment}.json", optional: true, reloadOnChange: true);

    // Cluster-specific config
    // Example:
    // CLUSTER=tst + ENV=dev/prod
    // appsettings.tstDevelopment.json / appsettings.tstProduction.json
    configBuilder.AddJsonFile($"appsettings.{normalizedCluster}{normalizedEnvironment}.json", optional: true, reloadOnChange: true);
}

// Environment variables should be added last so they can override JSON values
configBuilder.AddEnvironmentVariables();

// Temporarily build config to retrieve the encrypted connection string.
// IConfigurationRoot is IDisposable (it owns file-watching providers created by
// reloadOnChange: true), so dispose this short-lived instance once we're done with it.
string? encryptedConnString;
var tempConfig = configBuilder.Build();
try
{
    encryptedConnString = tempConfig["ConnectionStrings:DefaultConnectionString"];
}
finally
{
    (tempConfig as IDisposable)?.Dispose();
}

if (!string.IsNullOrWhiteSpace(encryptedConnString))
{
    try
    {
        // Decrypt connection string and append database configuration provider matching the resolved environment
        string decryptedConnString = AESCryptography.Decrypt(encryptedConnString);
        var dbEnvironment = ResolveDbEnvironmentName(normalizedEnvironment, normalizedCluster);
        
        Log.Information($"Bootstrapping Oracle configuration database provider for environment: {dbEnvironment}");
        configBuilder.Add(new DbConfigurationSource(decryptedConnString, dbEnvironment));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Warning: Failed to decrypt database connection string at startup. Error: {ex.Message}");
    }
}

var config = configBuilder.Build();
SettingsValues.Initialize(config);


static string? GetEnvironmentFromArgs(string[] args)
{
    if (args == null || args.Length == 0)
        return null;

    // Supports: --env=prod
    var envWithEquals = args.FirstOrDefault(a =>
        a.StartsWith("--env=", StringComparison.OrdinalIgnoreCase));

    if (!string.IsNullOrWhiteSpace(envWithEquals))
    {
        var parts = envWithEquals.Split('=', 2);
        return parts.Length == 2 ? parts[1] : null;
    }

    // Supports: --env prod
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals("--env", StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }

    return null;
}

static string NormalizeEnvironmentName(string? environment)
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

        // Unknown environment will be treated as Development
        _ => "Development"
    };
}

static string? NormalizeClusterName(string? cluster)
{
    if (string.IsNullOrWhiteSpace(cluster))
        return null;

    return cluster.Trim().ToLowerInvariant();
}

static string ResolveDbEnvironmentName(string environment, string? cluster)
{
    if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
    {
        return "Development";
    }

    if (!string.IsNullOrWhiteSpace(cluster))
    {
        var lowerCluster = cluster.ToLowerInvariant();
        if (lowerCluster == "gz") return "gzProduction";
        if (lowerCluster == "cu") return "cuProduction";
        if (lowerCluster == "tst") return "Test";
    }

    if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
    {
        return "Gazipur_Production"; 
    }

    return "Test";
}


var builder = WebApplication.CreateBuilder(args);
// Kestrel limits and keep-alive (re-applied)
builder.WebHost.ConfigureKestrel((options) =>
{
    options.Limits.MaxConcurrentConnections = 50000;
    options.Limits.MaxConcurrentUpgradedConnections = 10000;
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(15);
});
builder.Configuration.AddConfiguration(config);

// Simplify configuration - remove potential circular dependencies
var appSettings = new AppSettingsKeys();
config.GetSection("AppSettingsKeys").Bind(appSettings);
appSettings.IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

builder.Services.AddSingleton(appSettings);
builder.Services.Configure<AppSettingsKeys>(options =>
{
    config.GetSection("AppSettingsKeys").Bind(options);
    options.IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
});
string platform = PlatformHelper.GetPlatformName().ToLower();

string containerExceptionLog = string.Empty;
string containerApiLog = string.Empty;
string CommonLogPath = string.Empty;
string containerApiLog_DBSS = string.Empty;
string containerApiLog_DMS = string.Empty;
string containerApiLog_AIR = string.Empty;
string containerApiLog_RSO = string.Empty;
string containerApiLog_EV = string.Empty;
string containerApiLog_RETAPP = string.Empty;
string containerApiLog_SSAPI = string.Empty;
string containerApiLog_ESHOP = string.Empty;
string containerApiLog_DEX = string.Empty;
string containerOtherLog = string.Empty;
string dataProtectionKeyPath = string.Empty;

string ResolveBasePath(string? requestedPath, string fallbackPath)
{
    requestedPath = string.IsNullOrWhiteSpace(requestedPath) ? null : requestedPath;

    try
    {
        var fullPath = Path.GetFullPath(requestedPath ?? string.Empty);
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }
    catch (Exception ex)
    {
        var safeFallback = Path.GetFullPath(fallbackPath);
        Console.WriteLine($"Warning: Could not create path '{requestedPath}': {ex.Message}. Falling back to '{safeFallback}'.");
        Directory.CreateDirectory(safeFallback);
        return safeFallback;
    }
}

if (platform == "linux")
{
    var baseLogPath = ResolveBasePath(
            config.GetSection("LogPaths:linux").Value,
            Path.Combine(AppContext.BaseDirectory, "logs"));
    var podName = Environment.GetEnvironmentVariable("POD_NAME") ?? "apiPod";
    var logPath = Path.Combine(baseLogPath, podName);

    containerExceptionLog = Path.Combine(logPath, "ExceptionLogs", "exceptions-.log");
    containerApiLog = Path.Combine(logPath, "ApiReqResLogs", "api-.log");
    CommonLogPath = Path.Combine(logPath, "ApiReqResLogs");
    containerApiLog_DBSS = Path.Combine(CommonLogPath, "dbss", "dbss-.log");
    containerApiLog_DMS = Path.Combine(CommonLogPath, "dms", "dms-.log");
    containerApiLog_AIR = Path.Combine(CommonLogPath, "air", "air-.log");
    containerApiLog_RSO = Path.Combine(CommonLogPath, "rsoapp", "rso-.log");
    containerApiLog_EV = Path.Combine(CommonLogPath, "ev", "ev-.log");
    containerApiLog_RETAPP = Path.Combine(CommonLogPath, "retapp", "retapp-.log");
    containerApiLog_SSAPI = Path.Combine(CommonLogPath, "ssapi", "ss-.log");
    containerApiLog_ESHOP = Path.Combine(CommonLogPath, "eshop", "eshop-.log");
    containerApiLog_DEX = Path.Combine(CommonLogPath, "dex", "dex-.log");

    containerOtherLog = Path.Combine(logPath, "OtherLogs", "other-.log");
    dataProtectionKeyPath = ResolveBasePath(Path.Combine(baseLogPath, "DataProtection-Keys"), Path.Combine(AppContext.BaseDirectory, "DataProtection-Keys"));

    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(containerExceptionLog));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog));
        Directory.CreateDirectory(Path.GetDirectoryName(containerOtherLog));

        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_DBSS));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_DMS));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_AIR));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_RSO));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_EV));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_RETAPP));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_SSAPI));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_ESHOP));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_DEX));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not create directories: {ex.Message}");
    }
}
else
{
    var baseLogPath = ResolveBasePath(
            config.GetSection("LogPaths:windows").Value,
            Path.Combine(AppContext.BaseDirectory, "logs"));

    containerExceptionLog = Path.Combine(baseLogPath, "ExceptionLogs", "exceptions-.log");
    containerApiLog = Path.Combine(baseLogPath, "ApiReqResLogs", "api-.log");

    CommonLogPath = Path.Combine(baseLogPath, "ApiReqResLogs");
    containerApiLog_DBSS = Path.Combine(CommonLogPath, "dbss", "dbss-.log");
    containerApiLog_DMS = Path.Combine(CommonLogPath, "dms", "dms-.log");
    containerApiLog_AIR = Path.Combine(CommonLogPath, "air", "air-.log");
    containerApiLog_RSO = Path.Combine(CommonLogPath, "rsoapp", "rso-.log");
    containerApiLog_EV = Path.Combine(CommonLogPath, "ev", "ev-.log");
    containerApiLog_RETAPP = Path.Combine(CommonLogPath, "retapp", "retapp-.log");
    containerApiLog_SSAPI = Path.Combine(CommonLogPath, "ssapi", "ss-.log");
    containerApiLog_ESHOP = Path.Combine(CommonLogPath, "eshop", "eshop-.log");
    containerApiLog_DEX = Path.Combine(CommonLogPath, "dex", "dex-.log");

    containerOtherLog = Path.Combine(baseLogPath, "OtherLogs", "other-.log");
    dataProtectionKeyPath = ResolveBasePath(Path.Combine(baseLogPath, "DataProtection-Keys"), Path.Combine(AppContext.BaseDirectory, "DataProtection-Keys"));

    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(containerExceptionLog));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog));
        Directory.CreateDirectory(Path.GetDirectoryName(containerOtherLog));

        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_DBSS));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_DMS));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_AIR));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_RSO));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_EV));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_RETAPP));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_SSAPI));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_ESHOP));
        Directory.CreateDirectory(Path.GetDirectoryName(containerApiLog_DEX));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not create directories: {ex.Message}");
    }
}

// Simplified Data Protection configuration
try
{
    var dp = builder.Services.AddDataProtection();
    if (!string.IsNullOrWhiteSpace(dataProtectionKeyPath))
    {
        dp.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath));
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            dp.ProtectKeysWithDpapi();
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: DataProtection configuration failed: {ex.Message}");
}


var applicationTitle = string.IsNullOrWhiteSpace(appSettings.ApplicationTitle)
    ? "SingleSourceApi"
    : appSettings.ApplicationTitle;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information() // Optimized for high load (ignores debug/verbose logs)
    .Enrich.FromLogContext()    // FIX 1: Enables capturing properties pushed via LogContext
    .ReadFrom.Configuration(config)
    .WriteTo.Console()          // FIX 2: Replaces builder.Logging.AddConsole() in a Serilog-native way
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Level >= LogEventLevel.Error &&
            !LogFilter.ShouldIgnoreException(e))
        .WriteTo.File(
            containerExceptionLog,
            rollingInterval: RollingInterval.Day,
            shared: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    // DBSS
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Level == LogEventLevel.Information &&
            e.Properties.TryGetValue("LogTag", out var logTag) &&
            logTag.ToString().Contains(AppConstants.LogTags.ApiRequest) &&
            !LogFilter.ShouldIgnoreApiLog(e) &&
            e.Properties.TryGetValue("Platform", out var platform) &&
            platform.ToString().Contains("DBSS", StringComparison.OrdinalIgnoreCase))
        .WriteTo.File(
            containerApiLog_DBSS,
            rollingInterval: RollingInterval.Day,
            shared: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    // DMS
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Level == LogEventLevel.Information &&
            e.Properties.TryGetValue("LogTag", out var logTag) &&
            logTag.ToString().Contains(AppConstants.LogTags.ApiRequest) &&
            !LogFilter.ShouldIgnoreApiLog(e) &&
            e.Properties.TryGetValue("Platform", out var platform) &&
            platform.ToString().Contains("DMS", StringComparison.OrdinalIgnoreCase))
        .WriteTo.File(
            containerApiLog_DMS,
            rollingInterval: RollingInterval.Day,
            shared: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    // AIR
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Level == LogEventLevel.Information &&
            e.Properties.TryGetValue("LogTag", out var logTag) &&
            logTag.ToString().Contains(AppConstants.LogTags.ApiRequest) &&
            !LogFilter.ShouldIgnoreApiLog(e) &&
            e.Properties.TryGetValue("Platform", out var platform) &&
            platform.ToString().Contains("AIR", StringComparison.OrdinalIgnoreCase))
        .WriteTo.File(
            containerApiLog_AIR,
            rollingInterval: RollingInterval.Day,
            shared: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    // RSO
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Level == LogEventLevel.Information &&
            e.Properties.TryGetValue("LogTag", out var logTag) &&
            logTag.ToString().Contains(AppConstants.LogTags.ApiRequest) &&
            !LogFilter.ShouldIgnoreApiLog(e) &&
            e.Properties.TryGetValue("Platform", out var platform) &&
            platform.ToString().Contains("RSOAPP", StringComparison.OrdinalIgnoreCase))
        .WriteTo.File(
            containerApiLog_RSO,
            rollingInterval: RollingInterval.Day,
            shared: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    // EV
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Level == LogEventLevel.Information &&
            e.Properties.TryGetValue("LogTag", out var logTag) &&
            logTag.ToString().Contains(AppConstants.LogTags.ApiRequest) &&
            !LogFilter.ShouldIgnoreApiLog(e) &&
            e.Properties.TryGetValue("Platform", out var platform) &&
            platform.ToString().Contains("EV", StringComparison.OrdinalIgnoreCase))
        .WriteTo.File(
            containerApiLog_EV,
            rollingInterval: RollingInterval.Day,
            shared: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    // RETAPP
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Level == LogEventLevel.Information &&
            e.Properties.TryGetValue("LogTag", out var logTag) &&
            logTag.ToString().Contains(AppConstants.LogTags.ApiRequest) &&
            !LogFilter.ShouldIgnoreApiLog(e) &&
            e.Properties.TryGetValue("Platform", out var platform) &&
            platform.ToString().Contains("RETAPP", StringComparison.OrdinalIgnoreCase))
        .WriteTo.File(
            containerApiLog_RETAPP,
            rollingInterval: RollingInterval.Day,
            shared: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    // SSAPI
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Level == LogEventLevel.Information &&
            e.Properties.TryGetValue("LogTag", out var logTag) &&
            logTag.ToString().Contains(AppConstants.LogTags.ApiRequest) &&
            !LogFilter.ShouldIgnoreApiLog(e) &&
            e.Properties.TryGetValue("Platform", out var platform) &&
            platform.ToString().Contains("SSAPI", StringComparison.OrdinalIgnoreCase))
        .WriteTo.File(
            containerApiLog_SSAPI,
            rollingInterval: RollingInterval.Day,
            shared: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    // ESHOP
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Level == LogEventLevel.Information &&
            e.Properties.TryGetValue("LogTag", out var logTag) &&
            logTag.ToString().Contains(AppConstants.LogTags.ApiRequest) &&
            !LogFilter.ShouldIgnoreApiLog(e) &&
            e.Properties.TryGetValue("Platform", out var platform) &&
            platform.ToString().Contains("ESHOP", StringComparison.OrdinalIgnoreCase))
        .WriteTo.File(
            containerApiLog_ESHOP,
            rollingInterval: RollingInterval.Day,
            shared: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    // DEX
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Level == LogEventLevel.Information &&
            e.Properties.TryGetValue("LogTag", out var logTag) &&
            logTag.ToString().Contains(AppConstants.LogTags.ApiRequest) &&
            !LogFilter.ShouldIgnoreApiLog(e) &&
            e.Properties.TryGetValue("Platform", out var platform) &&
            platform.ToString().Contains("DEX", StringComparison.OrdinalIgnoreCase))
        .WriteTo.File(
            containerApiLog_DEX,
            rollingInterval: RollingInterval.Day,
            shared: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    // INTERNAL (Fallback for Unknown platform OR completely missing Platform property)
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Level == LogEventLevel.Information &&
            e.Properties.TryGetValue("LogTag", out var logTag) &&
            logTag.ToString().Contains(AppConstants.LogTags.ApiRequest) &&
            !LogFilter.ShouldIgnoreApiLog(e) &&
            // FIX 3: Safe fallback if Platform doesn't exist OR evaluates to "Unknown"
            (!e.Properties.TryGetValue("Platform", out var platform) ||
             platform.ToString().Contains("Unknown", StringComparison.OrdinalIgnoreCase)))
        .WriteTo.File(
            containerApiLog,
            rollingInterval: RollingInterval.Day,
            shared: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    // General other logs
    .WriteTo.Logger(lc => lc
        .Filter.ByExcluding(e => e.Level == LogEventLevel.Error)
        .Filter.ByExcluding(e =>
            e.Properties.TryGetValue("LogTag", out var logTag) &&
            logTag.ToString().Contains(AppConstants.LogTags.ApiRequest))
        .WriteTo.File(
            containerOtherLog,
            rollingInterval: RollingInterval.Day,
            shared: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    .CreateLogger();


builder.Host.UseSerilog();

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddSerilog();
});
// Allow model binding without automatic 400
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
// Add Response Compression for UseResponseCompression()
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Register CORS policy referenced in middleware
builder.Services.AddCors(options =>
{
    options.AddPolicy(AppConstants.Cors.AllowAll, policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

#region MohiBhai
//builder.Services.AddHttpClient("LoggedClient").ConfigureHttpClient(client =>
//   {
//	   client.Timeout = TimeSpan.FromSeconds(60);
//	   client.DefaultRequestHeaders.Accept.Clear();
//   })
//	.ConfigurePrimaryHttpMessageHandler(() =>
//		new HttpClientHandler
//		{
//			SslProtocols = SslProtocols.Tls12,
//			ServerCertificateCustomValidationCallback =
//				HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
//		});
#endregion


builder.Services.Configure<HttpClientSettings>(config.GetSection("HttpClientSettings"));
builder.Services.RegisterBiaServices();
// Restore named HttpClient and logging handler used by downstream calls
builder.Services.AddTransient<HttpListenerLog>();
builder.Services.AddHttpClient("LoggedClient").AddHttpMessageHandler<HttpListenerLog>();


#endregion
// Add Controllers and views with JSON and custom Input Formatters (consolidated registration)
builder.Services.AddAuthorization();
builder.Services
    .AddControllersWithViews(options =>
    {
        options.InputFormatters.Add(new FormInputFormatter());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });


var app = builder.Build();

// Middleware Pipeline
// PathBase handling is disabled because the reverse proxy strips the '/dbssbi' prefix.
// Ensure the proxy continues rewriting '/dbssbi' -> '/' when forwarding to this app.
var pathBase = "/"; // effective app root
Log.Information("PathBase handling disabled; app routes at root '/'");

// Return JSON for API errors (avoid HTML to clients expecting JSON)
app.Use(async (ctx, next) =>
{
    try
    {
        await next();
        if (ctx.Request.Path.StartsWithSegments("/api") && !ctx.Response.HasStarted)
        {
            if (ctx.Response.StatusCode == StatusCodes.Status404NotFound)
            {
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync("{\"error\":\"Not found\"}");
            }
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unhandled exception");
        if (ctx.Request.Path.StartsWithSegments("/api") && !ctx.Response.HasStarted)
        {
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"error\":\"Server error\"}");
            return;
        }
        throw;
    }
});
app.UseStaticFiles();
app.UseResponseCompression();
// Enable CORS policy
app.UseCors(AppConstants.Cors.AllowAll);
app.UseMiddleware<SerilogGlobalHandler>();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Application Startup Logging
try
{
    Log.Information($"Application started on {platform}.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly!");
}
finally
{
    Log.CloseAndFlush();
}
