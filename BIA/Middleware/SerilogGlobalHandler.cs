using System.Text;
using Serilog.Context;
using Serilog;
using System.Text.Json;
using BIA.Entity.ResponseEntity;
using BIA.Entity.Collections;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;

namespace BIA.Middleware
{
    public class SerilogGlobalHandler
    {
        private readonly RequestDelegate _next;

        public SerilogGlobalHandler(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var originalBodyStream = context.Response.Body;

            await using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                // =========================
                // Read Request
                // =========================
                string requestData = await MaskSensitiveData(context);

                await _next(context);

                // =========================
                // Read Response
                // =========================
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                string responseText;
                using (var reader = new StreamReader(context.Response.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
                {
                    responseText = await reader.ReadToEndAsync();
                }

                responseText = MaskSensitiveDataV2(responseText);

                context.Response.Body.Seek(0, SeekOrigin.Begin);

                // =========================
                // Clean Single Line Data
                // =========================
                string? singleLineRequest = requestData?
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", " ")
                    .Trim();

                string? singleLineResponse = responseText?
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", " ")
                    .Trim();

                stopwatch.Stop();

                // =========================
                // Build Structured JSON Log
                // =========================
                var logObject = new
                {
                    LogType = "Internal",
                    TraceId = context.TraceIdentifier,
                    Time = DateTime.UtcNow,
                    DurationMs = stopwatch.ElapsedMilliseconds,

                    Request = new
                    {
                        Method = context.Request.Method,
                        Url = context.Request.Path + context.Request.QueryString,
                        //Scheme = context.Request.Scheme,
                        //Host = context.Request.Host.ToString(),
                        //IpAddress = context.Connection.RemoteIpAddress?.ToString(),

                        Headers = context.Request.Headers
                            .ToDictionary(h => h.Key, h => h.Value.ToString()),

                        RequestBody = NormalizeJson(singleLineRequest)
                    },

                    Response = new
                    {
                        StatusCode = context.Response.StatusCode,
                        //Headers = context.Response.Headers
                        //    .ToDictionary(h => h.Key, h => h.Value.ToString()),
                        ResponseBody = NormalizeJson(singleLineResponse)
                    }
                };

                // =========================
                // Write JSON Log
                // =========================
                Log.ForContext("LogTag", "ApiRequestResponse")
                   .Information("{@ApiLog}", logObject);

                // =========================
                // Copy Response Back
                // =========================
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                await responseBody.CopyToAsync(originalBodyStream);

                context.Response.Body = originalBodyStream;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                context.Response.Body = originalBodyStream;

                if (ex.Message.Contains("token is expired!"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }

                context.Response.ContentType = "application/json";

                var errorResponse = GetCustomResponse(
                    context,
                    ex.InnerException?.Message ?? ex.Message);

                // =========================
                // Error Log JSON
                // =========================
                var errorLog = new
                {
                    LogTag = "ApiRequestResponse",
                    TraceId = context.TraceIdentifier,
                    Time = DateTime.UtcNow,
                    DurationMs = stopwatch.ElapsedMilliseconds,

                    Request = new
                    {
                        Method = context.Request.Method,
                        Url = context.Request.Path + context.Request.QueryString,
                        Headers = context.Request.Headers
                            .ToDictionary(h => h.Key, h => h.Value.ToString())
                    },

                    Exception = new
                    {
                        Message = ex.Message,
                        InnerMessage = ex.InnerException?.Message,
                        StackTrace = ex.StackTrace
                    },

                    Response = errorResponse
                };

                Log.ForContext("LogTag", "ApiRequestResponse")
                   .Error("{@ApiError}", errorLog);

                await JsonSerializer.SerializeAsync(
                    context.Response.Body,
                    errorResponse);
            }
        }

        private static string? NormalizeJson(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(text);

                return JsonSerializer.Serialize(
                    doc.RootElement,
                    new JsonSerializerOptions
                    {
                        WriteIndented = false
                    });
            }
            catch
            {
                return text;
            }
        }

        // =======================================
        // Helper Method
        // =======================================
        private static object? TryParseJson(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            try
            {
                return JsonSerializer.Deserialize<object>(text);
            }
            catch
            {
                return text;
            }
        }
        //public async Task InvokeAsync(HttpContext context)
        //{
        //    var originalBodyStream = context.Response.Body;
        //    await using var responseBody = new MemoryStream();
        //    context.Response.Body = responseBody;

        //    try
        //    {
        //        // Read & extract MSISDN from request body
        //        string requestData = await MaskSensitiveData(context);

        //        await _next(context);

        //        context.Response.Body.Seek(0, SeekOrigin.Begin);
        //        string responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        //        responseText = MaskSensitiveDataV2(responseText);

        //        string? singleLineRequest = requestData?.Replace("\r", "").Replace("\n", "").Replace("\t", " ").Trim();
        //        string? singleLineResponse = responseText?.Replace("\r", "").Replace("\n", "").Replace("\t", " ").Trim();

        //        Log.ForContext("LogTag", "ApiRequestResponse")
        //           //.ForContext("Msisdn", msisdn)  //  <<=== ADD THIS FIELD
        //           .Information(
        //               "[Internal] | Method: {Method} | URL: {Url} | Headers: {@Headers} \nRequest: {Request} \nResponse: {Response}",
        //               context.Request.Method,
        //               context.Request.Path + context.Request.QueryString,
        //               context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
        //               singleLineRequest,
        //               singleLineResponse);

        //        context.Response.Body.Seek(0, SeekOrigin.Begin);
        //        await responseBody.CopyToAsync(originalBodyStream);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.ForContext("LogTag", "ApiRequestResponse")
        //           .Error(ex, "Unhandled exception");

        //        context.Response.Body = originalBodyStream;
        //        if(ex.Message.Contains("token is expired!"))
        //        {
        //            context.Response.StatusCode = 401;
        //        }
        //        else
        //        {
        //            context.Response.StatusCode = 500;
        //        }                
        //        context.Response.ContentType = "application/json";

        //        var errorResponse = GetCustomResponse(context, ex.InnerException?.Message ?? ex.Message);
        //        await JsonSerializer.SerializeAsync(context.Response.Body, errorResponse);
        //    }
        //}

        //public async Task InvokeAsync(HttpContext context)
        //{
        //    var originalBodyStream = context.Response.Body;
        //    await using var responseBody = new MemoryStream();
        //    context.Response.Body = responseBody;

        //    try
        //    {
        //        string requestData = await MaskSensitiveData(context);

        //        await _next(context);

        //        context.Response.Body.Seek(0, SeekOrigin.Begin);
        //        string responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        //        responseText = MaskSensitiveDataV2(responseText);

        //        // Convert request and response to single-line JSON-safe strings
        //        string? singleLineRequest = requestData?.Replace("\r", "").Replace("\n", "").Replace("\t", " ").Trim();
        //        string? singleLineResponse = responseText?.Replace("\r", "").Replace("\n", "").Replace("\t", " ").Trim();

        //        Log.ForContext("LogTag", "ApiRequestResponse")
        //           .Information(
        //               "[Internal] | Method: {Method} | URL: {Url} | Headers: {@Headers} \nRequest: {Request} \nResponse: {Response}",
        //               context.Request.Method,
        //               context.Request.Path + context.Request.QueryString,
        //               context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
        //               singleLineRequest,
        //               singleLineResponse);

        //        context.Response.Body.Seek(0, SeekOrigin.Begin);
        //        await responseBody.CopyToAsync(originalBodyStream);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.ForContext("LogTag", "ApiRequestResponse")
        //           .Error(ex, "Unhandled exception");

        //        context.Response.Body = originalBodyStream;
        //        context.Response.StatusCode = 500;
        //        context.Response.ContentType = "application/json";

        //        var errorResponse = GetCustomResponse(context, ex.InnerException?.Message ?? ex.Message);
        //        await JsonSerializer.SerializeAsync(context.Response.Body, errorResponse);
        //    }
        //}

        private async Task<string> MaskSensitiveData(HttpContext context)
        {
            try
            {
                context.Request.EnableBuffering(bufferThreshold: 1024 * 30); // Limit in memory to 30KB

                context.Request.Body.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Seek(0, SeekOrigin.Begin);

                var contentType = context.Request.ContentType ?? "";
                var sensitiveKeys = new[]
                {
                    "dest_left_thumb", "dest_left_index", "dest_right_thumb", "dest_right_index",
                    "src_left_thumb", "src_left_index", "src_right_thumb", "src_right_index","finger_print","password"
                };

                if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) && IsJson(body))
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
                    if (dict != null)
                    {
                        foreach (var key in sensitiveKeys)
                        {
                            if (dict.ContainsKey(key))
                                dict[key] = "***MASKED***";
                        }

                        return JsonSerializer.Serialize(dict);
                    }
                }
                else if (contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                {
                    var parsedForm = QueryHelpers.ParseQuery(body);
                    var maskedDict = new Dictionary<string, string?>();

                    foreach (var kvp in parsedForm)
                    {
                        maskedDict[kvp.Key] = sensitiveKeys.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase)
                            ? "***MASKED***"
                            : kvp.Value;
                    }

                    return JsonSerializer.Serialize(maskedDict);
                }

                return body;
            }
            catch (Exception ex)
            {
                return $"[Error parsing request body: {ex.Message}]";
            }
        }
        private string ExtractMsisdn(string requestBody)
        {
            if (string.IsNullOrWhiteSpace(requestBody))
                return string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(requestBody);
                var root = doc.RootElement;

                string[] possibleKeys = new[]
                {
                    "msisdn",
                    "MSISDN",
                    "mobile_number",
                    "mobileNumber",
                    "MobileNumber",
                    "MobileNo",
                    "mobileNo"
                };

                foreach (var key in possibleKeys)
                {
                    if (root.TryGetProperty(key, out var val))
                        return val.ToString();
                }
            }
            catch
            {
                // swallow parsing errors silently
            }

            return string.Empty;
        }

        private string MaskSensitiveDataV2(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;

            var sensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "dest_left_thumb", "dest_left_index", "dest_right_thumb", "dest_right_index",
                    "src_left_thumb", "src_left_index", "src_right_thumb", "src_right_index","finger_print","password"
                };

            try
            {
                var token = JToken.Parse(json);
                MaskTokens(token, sensitiveKeys);
                return token.ToString();
            }
            catch
            {
                // If parsing fails (e.g., not JSON), return original body
                return json;
            }
        }
        private void MaskTokens(JToken token, HashSet<string> sensitiveKeys)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                foreach (var property in obj.Properties().ToList())
                {
                    if (sensitiveKeys.Contains(property.Name) && property.Value.Type == JTokenType.String)
                    {
                        property.Value = "****MASKED****";
                    }
                    else
                    {
                        MaskTokens(property.Value, sensitiveKeys); // Recurse
                    }
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in (JArray)token)
                {
                    MaskTokens(item, sensitiveKeys); // Recurse into arrays
                }
            }
        }
        private object GetCustomResponse(HttpContext context, string errorMessage)
        {
            var endpoint = context.Request.Path.ToString().ToLower();

            //if (endpoint.Contains("/api/security/loginv4") || endpoint.Contains("/api/security/loginv3"))
            //{
            //    return new { result = false, message = errorMessage, data = new { } };
            //}
            if (endpoint.Contains("/api/security/dbsslogin"))
            {
                return new { ISAuthenticate = false, AuthenticationMessage = errorMessage };
            }
            if (endpoint.Contains("api/dbssnotification/biostatusupdatefromdbss"))
            {
                return new { is_success = false, message = errorMessage };
            }

            return new { isError = true, message = errorMessage, data = "" };
        }

        private static bool IsJson(string input)
        {
            input = input.Trim();
            return (input.StartsWith("{") && input.EndsWith("}")) || (input.StartsWith("[") && input.EndsWith("]"));
        }
    }

}
