using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Context;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BIA.Middleware
{
    public class HttpListenerLog : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            string requestBody = string.Empty;
            string responseBody = string.Empty;

            HttpResponseMessage response;

            string platform = PlatformResolver.Resolve(request.RequestUri);

            try
            {
                // =========================================
                // Read Request Body
                // =========================================
                if (request.Content != null)
                {
                    requestBody = await request.Content
                        .ReadAsStringAsync(cancellationToken);

                    requestBody = MaskSensitiveData(requestBody);
                }

                // =========================================
                // Call External API
                // =========================================
                response = await base.SendAsync(request, cancellationToken);

                // =========================================
                // Read Response Body
                // =========================================
                if (response.Content != null)
                {
                    responseBody = await response.Content
                        .ReadAsStringAsync(cancellationToken);

                    responseBody = MaskSensitiveData(responseBody);
                }

                stopwatch.Stop();

                // =========================================
                // Convert to Single Line
                // =========================================
                string? singleLineRequest = requestBody?
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", " ")
                    .Trim();

                string? singleLineResponse = responseBody?
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", " ")
                    .Trim();

                // =========================================
                // Mask Sensitive Headers
                // =========================================
                var headers = request.Headers.ToDictionary(
                    h => h.Key,
                    h => h.Key.ToLower() switch
                    {
                        "authorization" => "***MASKED***",
                        "token" => "***MASKED***",
                        _ => string.Join(",", h.Value)
                    });

                // =========================================
                // Structured JSON Log
                // =========================================
                var logObject = new
                {
                    LogType = "External",
                    Time = DateTime.UtcNow,
                    DurationMs = stopwatch.ElapsedMilliseconds,

                    Request = new
                    {
                        Method = request.Method.Method,
                        Url = request.RequestUri?.ToString(),
                        Headers = headers,
                        RequestBody = NormalizeJson(singleLineRequest)
                    },

                    Response = new
                    {
                        StatusCode = (int)response.StatusCode,
                        //ReasonPhrase = response.ReasonPhrase,
                        //Headers = response.Headers.ToDictionary(
                        //    h => h.Key,
                        //    h => string.Join(",", h.Value)),

                        ResponseBody = NormalizeJson(singleLineResponse)
                    }
                };

                // =========================================
                // Write Log
                // =========================================

                using (LogContext.PushProperty("Platform", platform))
                {
                    Log.ForContext("LogTag", "ApiRequestResponse")
                       .Information("{@ApiLog}", logObject);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var errorLog = new
                {
                    LogType = "External",
                    Time = DateTime.UtcNow,
                    DurationMs = stopwatch.ElapsedMilliseconds,

                    Request = new
                    {
                        Method = request.Method.Method,
                        Url = request.RequestUri?.ToString()
                    },

                    Exception = new
                    {
                        Message = ex.Message,
                        InnerMessage = ex.InnerException?.Message
                    }
                };

                using (LogContext.PushProperty("Platform", platform))
                {
                    Log.ForContext("LogTag", "ApiRequestResponse")
                       .Error("{@ApiError}", errorLog);
                }

                throw;
            }

            return response;
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

        private string MaskSensitiveData(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;

            var sensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "dest_left_thumb", "dest_left_index", "dest_right_thumb", "dest_right_index",
                    "src_left_thumb", "src_left_index", "src_right_thumb", "src_right_index","finger_print","password","pin"
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


    }



    //public class HttpListenerLog : DelegatingHandler
    //{
    //    private static readonly HashSet<string> SensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    //{
    //    "dest_left_thumb", "dest_left_index", "dest_right_thumb", "dest_right_index",
    //    "src_left_thumb", "src_left_index", "src_right_thumb", "src_right_index",
    //    "finger_print", "password", "pin"
    //};
    //    private const long MaxLogBodySize = 150 * 1024; // Protect RAM: 100 KB limit for logging bodies
    //    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    //    {
    //        var stopwatch = Stopwatch.StartNew();
    //        string requestBody = string.Empty;
    //        string responseBody = string.Empty;
    //        HttpResponseMessage response; // Avoid instantiation here
    //        string platform = PlatformResolver.Resolve(request.RequestUri);
    //        bool isInfoEnabled = Log.IsEnabled(Serilog.Events.LogEventLevel.Information);
    //        try
    //        {
    //            // 1. Read and process Request Body
    //            if (request.Content != null && ShouldLogContent(request.Content))
    //            {
    //                var rawRequest = await request.Content.ReadAsStringAsync(cancellationToken);
    //                requestBody = MaskAndMinifyJson(rawRequest);
    //            }

    //            // 2. Call External API
    //            response = await base.SendAsync(request, cancellationToken);
    //            // 3. Read and process Response Body
    //            if (response.Content != null && ShouldLogContent(response.Content))
    //            {
    //                var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
    //                responseBody = MaskAndMinifyJson(rawResponse);
    //            }

    //            stopwatch.Stop();
    //            if (isInfoEnabled)
    //            {
    //                // 4. Safe, allocation-free header masking
    //                var headers = request.Headers.ToDictionary(
    //                    h => h.Key,
    //                    h => string.Equals(h.Key, "authorization", StringComparison.OrdinalIgnoreCase) ||
    //                         string.Equals(h.Key, "token", StringComparison.OrdinalIgnoreCase)
    //                        ? "***MASKED***"
    //                        : string.Join(",", h.Value));
    //                var logObject = new
    //                {
    //                    LogType = "External",
    //                    Time = DateTime.UtcNow,
    //                    DurationMs = stopwatch.ElapsedMilliseconds,
    //                    Request = new
    //                    {
    //                        Method = request.Method.Method,
    //                        Url = request.RequestUri?.OriginalString ?? request.RequestUri?.ToString(),
    //                        Headers = headers,
    //                        RequestBody = requestBody
    //                    },
    //                    Response = new
    //                    {
    //                        StatusCode = (int)response.StatusCode,
    //                        ResponseBody = responseBody
    //                    }
    //                };
    //                using (LogContext.PushProperty("Platform", platform))
    //                {
    //                    Log.ForContext("LogTag", "ApiRequestResponse")
    //                       .Information("{@ApiLog}", logObject);
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            stopwatch.Stop();
    //            var errorLog = new
    //            {
    //                LogType = "External",
    //                Time = DateTime.UtcNow,
    //                DurationMs = stopwatch.ElapsedMilliseconds,
    //                Request = new
    //                {
    //                    Method = request.Method.Method,
    //                    Url = request.RequestUri?.OriginalString ?? request.RequestUri?.ToString()
    //                },
    //                Exception = new
    //                {
    //                    Message = ex.Message,
    //                    InnerMessage = ex.InnerException?.Message
    //                }
    //            };
    //            using (LogContext.PushProperty("Platform", platform))
    //            {
    //                // FIX: Pass 'ex' to preserve the stack trace in your logger
    //                Log.ForContext("LogTag", "ApiRequestResponse")
    //                   .Error(ex, "{@ApiError}", errorLog);
    //            }
    //            throw;
    //        }
    //        return response;
    //    }
    //    private static bool ShouldLogContent(HttpContent content)
    //    {
    //        var mediaType = content.Headers.ContentType?.MediaType;
    //        if (mediaType == null || !mediaType.Contains("json", StringComparison.OrdinalIgnoreCase) || !mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase) || !mediaType.Contains("form-urlencoded", StringComparison.OrdinalIgnoreCase))
    //            return false;
    //        var contentLength = content.Headers.ContentLength;
    //        if (contentLength.HasValue && contentLength.Value > MaxLogBodySize)
    //            return false;
    //        return true;
    //    }
    //    private static string MaskAndMinifyJson(string json)
    //    {
    //        if (string.IsNullOrWhiteSpace(json))
    //            return string.Empty;
    //        try
    //        {
    //            using (JsonDocument document = JsonDocument.Parse(json))
    //            {
    //                // Indented = false ensures output is a single-line minified JSON string
    //                var options = new JsonWriterOptions { Indented = false };
    //                using (var stream = new MemoryStream(json.Length))
    //                using (var writer = new Utf8JsonWriter(stream, options))
    //                {
    //                    MaskElement(document.RootElement, writer, null);
    //                    writer.Flush();
    //                    return Encoding.UTF8.GetString(stream.ToArray());
    //                }
    //            }
    //        }
    //        catch
    //        {
    //            // Fallback: strip newlines if parsing fails
    //            return ToSingleLine(json);
    //        }
    //    }
    //    private static void MaskElement(JsonElement element, Utf8JsonWriter writer, string? propertyName)
    //    {
    //        if (propertyName != null && SensitiveKeys.Contains(propertyName) && element.ValueKind == JsonValueKind.String)
    //        {
    //            writer.WriteStringValue("****MASKED****");
    //            return;
    //        }
    //        switch (element.ValueKind)
    //        {
    //            case JsonValueKind.Object:
    //                writer.WriteStartObject();
    //                foreach (var property in element.EnumerateObject())
    //                {
    //                    writer.WritePropertyName(property.Name);
    //                    MaskElement(property.Value, writer, property.Name);
    //                }
    //                writer.WriteEndObject();
    //                break;
    //            case JsonValueKind.Array:
    //                writer.WriteStartArray();
    //                foreach (var item in element.EnumerateArray())
    //                {
    //                    MaskElement(item, writer, propertyName);
    //                }
    //                writer.WriteEndArray();
    //                break;
    //            default:
    //                element.WriteTo(writer);
    //                break;
    //        }
    //    }
    //    private static string ToSingleLine(string? input)
    //    {
    //        if (string.IsNullOrWhiteSpace(input))
    //            return string.Empty;
    //        int length = input.Length;
    //        char[] buffer = ArrayPool<char>.Shared.Rent(length);
    //        try
    //        {
    //            int idx = 0;
    //            int start = 0;
    //            while (start < length && char.IsWhiteSpace(input[start]))
    //                start++;
    //            int end = length - 1;
    //            while (end >= start && char.IsWhiteSpace(input[end]))
    //                end--;
    //            for (int i = start; i <= end; i++)
    //            {
    //                char c = input[i];
    //                if (c == '\r' || c == '\n')
    //                    continue;
    //                buffer[idx++] = (c == '\t') ? ' ' : c;
    //            }
    //            return new string(buffer, 0, idx);
    //        }
    //        finally
    //        {
    //            ArrayPool<char>.Shared.Return(buffer);
    //        }
    //    }
    //}

    public static class PlatformResolver
    {
        private static readonly (string Key, string Value)[] PlatformMappings = new[]
        {
        ("simserial_api", "DMS"),
        ("simoffer", "DMS"),
        ("dms_ga_offer", "DMS"),
        ("dmsapi", "DMS"),
        ("bssapi", "DBSS"),
        ("air", "AIR"),
        ("retailerselfapp", "RETAPP"),
        ("rechargeapi", "RETAPP"),
        ("blsalesforceapp.banglalink.net/SfaAppApi", "RSOAPP"),
        ("BioSingleSourceApiCore", "SSAPI"),
        ("pretups/c2sreceiver", "EV"),
        ("pretups/c2sreceiver", "ESHOP"),
        ("delivery-xpress", "DEX"),
        ("10.74.43.13", "DBSS"),
        ("10.74.10.10", "DBSS"),
        ("dex", "DEX")
    };

        public static string Resolve(Uri? uri)
        {
            if (uri == null)
                return "Unknown";

            string url = uri.OriginalString ?? uri.ToString();

            for (int i = 0; i < PlatformMappings.Length; i++)
            {
                if (url.Contains(PlatformMappings[i].Key, StringComparison.OrdinalIgnoreCase))
                    return PlatformMappings[i].Value;
            }

            return "Unknown";
        }
    }
}
