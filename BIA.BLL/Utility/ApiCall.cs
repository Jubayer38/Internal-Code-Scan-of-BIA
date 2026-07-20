using BIA.BLL.BLLServices;
using BIA.DAL.Repositories;
using BIA.Entity.Collections;
using BIA.Entity.DB_Model;
using BIA.Entity.Interfaces;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Entity.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Data;
using System.Data.SqlTypes;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BIA.BLL.Utility;

public class ApiCall
{
    private readonly string baseUrl;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly SemaphoreSlim _dpeTokenLock = new SemaphoreSlim(1, 1);
    private readonly DALBiometricRepo _dataManager;
    private readonly BLLLog _bllLog;
    private readonly int defaultTimeout;
    private readonly BL_Json _blJson;

    public ApiCall(IHttpClientFactory httpClientFactory, DALBiometricRepo dataManager, BL_Json blJson, BLLLog bllLog)
    {
        _httpClientFactory = httpClientFactory;
        baseUrl = SettingsValues.GetDbssBaseUrl();
        defaultTimeout = 60;
        _dataManager = dataManager;
        _blJson = blJson;
        _bllLog = bllLog;
    }

    public async Task<object> HttpPostRequestSingleSourceCheck(object obj, string methodUrl, string sessionToken, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var uri = new Uri(methodUrl);
            var jsonString = JsonConvert.SerializeObject(obj);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
            var data = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(uri, data, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<object>(responseContent) ?? new object();
            }
            else
            {
                throw new Exception(responseContent);
            }
        }
        catch (HttpRequestException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestSingleSourceCheck",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequest",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HttpPostRequest(object obj, string methodUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var uri = new Uri(baseUrl + methodUrl);
            var jsonString = JsonConvert.SerializeObject(obj);
            var data = new StringContent(jsonString, Encoding.UTF8, "application/vnd.api+json");

            var response = await client.PostAsync(uri, data, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<object>(responseContent) ?? new object();
            }
            else
            {
                throw new Exception(responseContent);
            }
        }
        catch (HttpRequestException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequest",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequest",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HttpPostRequestCDT(object obj, string methodUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var uri = new Uri(baseUrl + methodUrl);
            var jsonString = JsonConvert.SerializeObject(obj);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(uri, content, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<object>(responseString) ?? new object();
            }
            else
            {
                throw new Exception(responseString);
            }
        }
        catch (HttpRequestException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestCDT",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestCDT",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HttpPostRequestOrderDBSS(object obj, string methodUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var uri = new Uri(baseUrl + methodUrl);
            var jsonString = JsonConvert.SerializeObject(obj);
            var data = new StringContent(jsonString, Encoding.UTF8, "application/vnd.api+json");

            var response = await client.PostAsync(uri, data, cancellationToken).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<object>(responseString) ?? new object();
            }
            else
            {
                throw new Exception(responseString);
            }
        }
        catch (HttpRequestException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestOrderDBSS",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestOrderDBSS",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HttpPatchRequest(object obj, string methodUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var uri = new Uri(baseUrl + methodUrl);
            var jsonString = JsonConvert.SerializeObject(obj);
            var data = new StringContent(jsonString, Encoding.UTF8, "application/vnd.api+json");

            using var request = new HttpRequestMessage(HttpMethod.Patch, uri)
            {
                Content = data
            };

            using var response = await client.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<object>(responseContent) ?? new object();
            }
            else
            {
                throw new Exception(responseContent);
            }
        }
        catch (HttpRequestException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPatchRequest",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPatchRequest",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<JObject> HttpGetRequest(string apiUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var uri = new Uri(baseUrl + apiUrl);
            var response = await client.GetAsync(uri, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<JObject>(responseContent) ?? new JObject();
            }
            else
            {
                throw new Exception(responseContent);
            }
        }
        catch (HttpRequestException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpGetRequest",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpGetRequest",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HttpGetRequestAsync(string apiUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var uri = new Uri(baseUrl + apiUrl);
            var response = await client.GetAsync(uri, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<object>(responseContent) ?? new object();
            }
            else
            {
                throw new Exception(responseContent);
            }
        }
        catch (HttpRequestException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpGetRequestAsync",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpGetRequestAsync",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HttpGetRequestSingleSource(string apiUrl, string sessionToken, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);

            var uri = new Uri(apiUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(uri, cancellationToken).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var resObj = JsonConvert.DeserializeObject<object>(responseString);
                return resObj ?? new object();
            }
            else
            {
                throw new Exception(responseString);
            }
        }
        catch (HttpRequestException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpGetRequestSingleSource",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpGetRequestSingleSource",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<PreloadApiResponse?> HTTPGetRequestPreloadData(string endpoint, string retailerCode, string inner_method, string xChannel, string? orderType = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            string baseUrlDpe = SettingsValues.GetDPEBaseUrl()?.TrimEnd('/') ?? string.Empty;

            if (string.IsNullOrWhiteSpace(baseUrlDpe))
            {
                throw new Exception("DeliveryXpress BaseUrl is not configured.");
            }

            string apiUrl = $"{baseUrlDpe}{endpoint}";

            Log.ForContext("LogTag", "ApiCall")
                .Information("HomeWifi GET Request: {@Request}", new
                {
                    apiUrl,
                    retailerCode,
                    orderType,
                    xChannel
                });

            if (!retailerCode.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                retailerCode = "R" + retailerCode;
            }

            async Task<HttpResponseMessage> SendAsync(string token)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                request.Headers.Add("x-client-uid", SettingsValues.GetDPEChannel());
                request.Headers.Add("x-username", retailerCode);
                request.Headers.Add("x-channel", xChannel);

                return await client.SendAsync(request, cancellationToken);
            }

            string token = await GetValidDpeJwtToken(cancellationToken);
            using var response = await SendAsync(token);

            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            Log.ForContext("LogTag", "ApiCall")
                .Information("HomeWifi GET Response: {Response}", responseContent);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                string newToken = await RefreshDpeJwtToken(cancellationToken);

                using var retryResponse = await SendAsync(newToken);
                string retryContent = await retryResponse.Content.ReadAsStringAsync(cancellationToken);

                Log.ForContext("LogTag", "ApiCall")
                    .Information("HomeWifi GET Retry Response: {Response}", retryContent);

                if (!string.IsNullOrWhiteSpace(retryContent))
                {
                    return JsonConvert.DeserializeObject<PreloadApiResponse>(retryContent);
                }

                return new PreloadApiResponse
                {
                    status = "FAILED",
                    message = "Empty response from API after retry"
                };
            }

            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                return JsonConvert.DeserializeObject<PreloadApiResponse>(responseContent);
            }

            return new PreloadApiResponse
            {
                status = "FAILED",
                message = "Empty response from API"
            };
        }
        catch (Exception ex)
        {
            string errorDetails = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HTTPGetRequestPreloadData",
                inner_method_name = inner_method,
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });

            throw new Exception("OuterDetails: " + errorDetails, ex);
        }
    }

    public async Task<object> HTTPPostRequestHomeWifiRefer(string endpoint, string retailerCode, object requestBody, string inner_method, string? orderType = null, string? orderNumber = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            string? baseUrlDpe = SettingsValues.GetDPEBaseUrl()?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(baseUrlDpe))
            {
                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "DeliveryXpress BaseUrl is not configured.",
                    data = null
                };
            }

            string apiUrl = $"{baseUrlDpe}{endpoint}";

            if (!retailerCode.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                retailerCode = "R" + retailerCode;
            }

            async Task<HttpResponseMessage> SendAsync(string token)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                request.Headers.Add("x-client-uid", SettingsValues.GetDPEChannel());
                request.Headers.Add("x-username", retailerCode);

                if (!string.IsNullOrWhiteSpace(orderType))
                {
                    request.Headers.Add("x-order-type", orderType);
                }

                request.Content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                return await client.SendAsync(request, cancellationToken);
            }

            string token = await GetValidDpeJwtToken(cancellationToken);
            using var response = await SendAsync(token);

            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                string newToken = await RefreshDpeJwtToken(cancellationToken);

                using var retryResponse = await SendAsync(newToken);
                string retryContent = await retryResponse.Content.ReadAsStringAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(retryContent))
                {
                    return JsonConvert.DeserializeObject<object>(retryContent) ?? new object();
                }

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Empty response from Dex Portal API after retry.",
                    data = null
                };
            }

            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                return JsonConvert.DeserializeObject<object>(responseContent) ?? new object();
            }

            return new HomeWifiCommonResponseModel
            {
                isError = true,
                message = "Empty response from Dex Portal API.",
                data = null
            };
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HTTPPostRequestHomeWifiRefer",
                inner_method_name = inner_method,
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });

            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HTTPPatchRequestHomeWifi(string endpoint, string retailerCode, object requestBody, string inner_method, string? orderType = null, string? orderNumber = null, Action<object>? captureDexRequestLog = null, Action<string>? captureDexRawResponseLog = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            string? baseUrlDpe = SettingsValues.GetDPEBaseUrl()?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(baseUrlDpe))
            {
                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "DeliveryXpress BaseUrl is not configured.",
                    data = null
                };
            }

            string apiUrl = $"{baseUrlDpe}{endpoint}";

            if (!retailerCode.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                retailerCode = "R" + retailerCode;
            }

            async Task<(HttpResponseMessage Response, object LogObject)> SendAsync(string token, bool isRetry)
            {
                using var request = new HttpRequestMessage(HttpMethod.Patch, apiUrl);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                request.Headers.Add("x-client-uid", SettingsValues.GetDPEChannel());
                request.Headers.Add("x-username", retailerCode);

                if (!string.IsNullOrWhiteSpace(orderType))
                {
                    request.Headers.Add("x-order-type", orderType);
                }

                request.Content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var logObj = BuildDexActualRequestLog(request, requestBody, inner_method, isRetry);

                using var response = await client.SendAsync(request, cancellationToken);
                return (response, logObj);
            }

            string token = await GetValidDpeJwtToken(cancellationToken);
            var (response, logObj) = await SendAsync(token, false);

            captureDexRequestLog?.Invoke(logObj);

            using (response)
            {
                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                captureDexRawResponseLog?.Invoke(responseContent);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    string newToken = await RefreshDpeJwtToken(cancellationToken);

                    var (retryResponse, retryLogObj) = await SendAsync(newToken, true);
                    captureDexRequestLog?.Invoke(retryLogObj);

                    using (retryResponse)
                    {
                        string retryContent = await retryResponse.Content.ReadAsStringAsync(cancellationToken);
                        captureDexRawResponseLog?.Invoke(retryContent);

                        if (!string.IsNullOrWhiteSpace(retryContent))
                        {
                            return JsonConvert.DeserializeObject<object>(retryContent) ?? new object();
                        }

                        return new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = "Empty response from Dex Portal API after retry.",
                            data = null
                        };
                    }
                }

                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    return JsonConvert.DeserializeObject<object>(responseContent) ?? new object();
                }

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Empty response from Dex Portal API.",
                    data = null
                };
            }
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HTTPPatchRequestHomeWifi",
                inner_method_name = inner_method,
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });

            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<string> HTTPPostRequestCancelationLockHomeWifi(string endpoint, string retailerCode, object requestBody, string inner_method, string? orderType = null, string? orderNumber = null, Action<object>? captureDexRequestLog = null, Action<string>? captureDexRawResponseLog = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            string? baseUrlDpe = SettingsValues.GetDPEBaseUrl()?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(baseUrlDpe))
            {
                throw new Exception("DeliveryXpress BaseUrl is not configured.");
            }

            string apiUrl = $"{baseUrlDpe}{endpoint}";

            if (!retailerCode.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                retailerCode = "R" + retailerCode;
            }

            async Task<(HttpResponseMessage Response, object LogObject)> SendAsync(string token, bool isRetry)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                request.Headers.Add("x-client-uid", SettingsValues.GetDPEChannel());
                request.Headers.Add("x-username", retailerCode);

                if (!string.IsNullOrWhiteSpace(orderType))
                {
                    request.Headers.Add("x-order-type", orderType);
                }

                request.Content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var logObj = BuildDexActualRequestLog(request, requestBody, inner_method, isRetry);

                using var response = await client.SendAsync(request, cancellationToken);
                return (response, logObj);
            }

            string token = await GetValidDpeJwtToken(cancellationToken);
            var (response, logObj) = await SendAsync(token, false);

            captureDexRequestLog?.Invoke(logObj);

            using (response)
            {
                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                captureDexRawResponseLog?.Invoke(responseContent);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    string newToken = await RefreshDpeJwtToken(cancellationToken);

                    var (retryResponse, retryLogObj) = await SendAsync(newToken, true);
                    captureDexRequestLog?.Invoke(retryLogObj);

                    using (retryResponse)
                    {
                        string retryContent = await retryResponse.Content.ReadAsStringAsync(cancellationToken);
                        captureDexRawResponseLog?.Invoke(retryContent);

                        return retryContent;
                    }
                }

                return responseContent;
            }
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HTTPPostRequestCancelationLockHomeWifi",
                inner_method_name = inner_method,
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });

            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HTTPGetRequestHomeWifi(string endpoint, string retailerCode, string inner_method, string? orderType = null, Action<object>? captureDexRequestLog = null, Action<string>? captureDexRawResponseLog = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            string baseUrlDpe = SettingsValues.GetDPEBaseUrl()?.TrimEnd('/') ?? string.Empty;

            if (string.IsNullOrWhiteSpace(baseUrlDpe))
            {
                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "DeliveryXpress BaseUrl is not configured.",
                    data = null
                };
            }

            string apiUrl = $"{baseUrlDpe}{endpoint}";

            if (!retailerCode.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                retailerCode = "R" + retailerCode;
            }

            async Task<(HttpResponseMessage Response, object LogObject)> SendAsync(string token, bool isRetry)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                request.Headers.Add("x-client-uid", SettingsValues.GetDPEChannel());
                request.Headers.Add("x-username", retailerCode);

                if (!string.IsNullOrWhiteSpace(orderType))
                {
                    request.Headers.Add("x-order-type", orderType);
                }

                var logObj = BuildDexActualRequestLog(request, null, inner_method, isRetry);

                using var response = await client.SendAsync(request, cancellationToken);
                return (response, logObj);
            }

            string token = await GetValidDpeJwtToken(cancellationToken);
            var (response, logObj) = await SendAsync(token, false);

            captureDexRequestLog?.Invoke(logObj);

            using (response)
            {
                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                captureDexRawResponseLog?.Invoke(responseContent);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    string newToken = await RefreshDpeJwtToken(cancellationToken);

                    var (retryResponse, retryLogObj) = await SendAsync(newToken, true);
                    captureDexRequestLog?.Invoke(retryLogObj);

                    using (retryResponse)
                    {
                        string retryContent = await retryResponse.Content.ReadAsStringAsync(cancellationToken);
                        captureDexRawResponseLog?.Invoke(retryContent);

                        if (!string.IsNullOrWhiteSpace(retryContent))
                        {
                            return JsonConvert.DeserializeObject<object>(retryContent) ?? new object();
                        }

                        return new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = "Empty response from Dex Portal API after retry.",
                            data = null
                        };
                    }
                }

                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    return JsonConvert.DeserializeObject<object>(responseContent) ?? new object();
                }

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Empty response from Dex Portal API.",
                    data = null
                };
            }
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HTTPGetRequestHomeWifi",
                inner_method_name = inner_method,
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });

            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<DexPortalRawApiResponse> HTTPPatchRequestIMEIUpdateHomeWifi(string endpoint, string retailerCode, object requestBody, string inner_method, string? orderNumber = null, bool sendOrderTypeHeader = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            string? baseUrlDpe = SettingsValues.GetDPEBaseUrl()?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(baseUrlDpe))
            {
                string errorResponse = JsonConvert.SerializeObject(
                    new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "DeliveryXpress BaseUrl is not configured.",
                        data = null
                    });

                return new DexPortalRawApiResponse
                {
                    StatusCode = 0,
                    RawResponse = errorResponse,
                    ParsedResponse = JsonConvert.DeserializeObject<object>(errorResponse)
                };
            }

            string apiUrl = $"{baseUrlDpe}{endpoint}";
            string finalRetailerCode = retailerCode ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(finalRetailerCode) &&
                !finalRetailerCode.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                finalRetailerCode = "R" + finalRetailerCode;
            }

            string? orderType = null;

            if (sendOrderTypeHeader && !string.IsNullOrWhiteSpace(orderNumber))
            {
                orderType = await GetDPEOrderTypeFromDbOrDex(orderNumber, retailerCode);
            }

            async Task<(HttpResponseMessage Response, object LogObject)> SendAsync(string token, bool isRetry)
            {
                using var request = new HttpRequestMessage(HttpMethod.Patch, apiUrl);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                request.Headers.Add("x-client-uid", SettingsValues.GetDPEChannel());

                if (sendOrderTypeHeader && !string.IsNullOrWhiteSpace(orderType))
                {
                    request.Headers.Add("x-order-type", orderType);
                }

                if (!string.IsNullOrWhiteSpace(finalRetailerCode))
                {
                    request.Headers.Add("x-username", finalRetailerCode);
                }

                request.Content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var logObj = BuildDexActualRequestLog(request, requestBody, inner_method, isRetry);

                using var response = await client.SendAsync(request, cancellationToken);
                return (response, logObj);
            }

            string token = await GetValidDpeJwtToken(cancellationToken);
            var (response, logObj) = await SendAsync(token, false);

            using (response)
            {
                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    string newToken = await RefreshDpeJwtToken(cancellationToken);

                    var (retryResponse, retryLogObj) = await SendAsync(newToken, true);

                    using (retryResponse)
                    {
                        string retryContent = await retryResponse.Content.ReadAsStringAsync(cancellationToken);
                        object? retryParsed = null;

                        if (!string.IsNullOrWhiteSpace(retryContent))
                        {
                            try
                            {
                                retryParsed = JsonConvert.DeserializeObject<object>(retryContent);
                            }
                            catch
                            {
                                retryParsed = new HomeWifiCommonResponseModel
                                {
                                    isError = true,
                                    message = retryContent,
                                    data = null
                                };
                            }
                        }
                        else
                        {
                            retryContent = JsonConvert.SerializeObject(
                                new HomeWifiCommonResponseModel
                                {
                                    isError = true,
                                    message = "Empty response from Dex Portal API.",
                                    data = null
                                });

                            retryParsed = JsonConvert.DeserializeObject<object>(retryContent);
                        }

                        return new DexPortalRawApiResponse
                        {
                            StatusCode = (int)retryResponse.StatusCode,
                            RawResponse = retryContent,
                            ParsedResponse = retryParsed,
                            IsUnauthorizedRetry = true,
                            ActualRequest = retryLogObj
                        };
                    }
                }

                object? parsedResponse = null;

                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    try
                    {
                        parsedResponse = JsonConvert.DeserializeObject<object>(responseContent);
                    }
                    catch
                    {
                        parsedResponse = new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = responseContent,
                            data = null
                        };
                    }
                }
                else
                {
                    responseContent = JsonConvert.SerializeObject(
                        new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = "Empty response from Dex Portal API.",
                            data = null
                        });

                    parsedResponse = JsonConvert.DeserializeObject<object>(responseContent);
                }

                return new DexPortalRawApiResponse
                {
                    StatusCode = (int)response.StatusCode,
                    RawResponse = responseContent,
                    ParsedResponse = parsedResponse,
                    IsUnauthorizedRetry = false,
                    ActualRequest = logObj
                };
            }
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HTTPPatchRequestIMEIUpdateHomeWifi",
                inner_method_name = inner_method,
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });

            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HTTPPostMultipartHomeWifi(string endpoint, string retailerCode, IFormFile payslipImage, string inner_method, string? orderNumber = null, string? orderType = null, Action<object>? captureDexRequestLog = null, Action<string>? captureDexRawResponseLog = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            string? baseUrlDpe = SettingsValues.GetDPEBaseUrl()?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(baseUrlDpe))
            {
                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "DeliveryXpress BaseUrl is not configured.",
                    data = null
                };
            }

            string apiUrl = $"{baseUrlDpe}{endpoint}";

            if (!retailerCode.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                retailerCode = "R" + retailerCode;
            }

            var nwInfo = await GetNwInfoForPayslipIfRequired(orderNumber, retailerCode);

            async Task<(HttpResponseMessage Response, object LogObject)> SendAsync(string token, bool isRetry)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                request.Headers.Add("x-client-uid", SettingsValues.GetDPEChannel());
                request.Headers.Add("x-username", retailerCode);

                if (!string.IsNullOrWhiteSpace(orderType))
                {
                    request.Headers.Add("x-order-type", orderType);
                }

                var content = new MultipartFormDataContent();

                var streamContent = new StreamContent(payslipImage.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(payslipImage.ContentType);
                content.Add(streamContent, "payslip_image", payslipImage.FileName);

                if (nwInfo.ShouldSend)
                {
                    content.Add(new StringContent(nwInfo.NwAssessId), "nw_assess_id");
                    content.Add(new StringContent(nwInfo.NwAssessStatus), "nw_assess_status");
                }

                request.Content = content;

                var logObj = BuildDexActualRequestLog(
                    request,
                    new
                    {
                        FileName = payslipImage.FileName,
                        Length = payslipImage.Length,
                        ContentType = payslipImage.ContentType,
                        NwAssessId = nwInfo.ShouldSend ? nwInfo.NwAssessId : null,
                        NwAssessStatus = nwInfo.ShouldSend ? nwInfo.NwAssessStatus : null
                    },
                    inner_method,
                    isRetry
                );

                using var response = await client.SendAsync(request, cancellationToken);
                return (response, logObj);
            }

            string token = await GetValidDpeJwtToken(cancellationToken);
            var (response, logObj) = await SendAsync(token, false);

            captureDexRequestLog?.Invoke(logObj);

            using (response)
            {
                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                captureDexRawResponseLog?.Invoke(responseContent);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    string newToken = await RefreshDpeJwtToken(cancellationToken);

                    var (retryResponse, retryLogObj) = await SendAsync(newToken, true);
                    captureDexRequestLog?.Invoke(retryLogObj);

                    using (retryResponse)
                    {
                        string retryContent = await retryResponse.Content.ReadAsStringAsync(cancellationToken);
                        captureDexRawResponseLog?.Invoke(retryContent);

                        if (!string.IsNullOrWhiteSpace(retryContent))
                        {
                            return JsonConvert.DeserializeObject<object>(retryContent) ?? new object();
                        }

                        return new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = "Empty response from Dex Portal API after retry.",
                            data = null
                        };
                    }
                }

                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    return JsonConvert.DeserializeObject<object>(responseContent) ?? new object();
                }

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Empty response from Dex Portal API.",
                    data = null
                };
            }
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HTTPPostMultipartHomeWifi",
                inner_method_name = inner_method,
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });

            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HTTPPostRequestHomeWifi(string endpoint, string retailerCode, object requestBody, string inner_method, string? orderType = null, Action<object>? captureDexRequestLog = null, Action<string>? captureDexRawResponseLog = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            string? baseUrlDpe = SettingsValues.GetDPEBaseUrl()?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(baseUrlDpe))
            {
                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "DeliveryXpress BaseUrl is not configured.",
                    data = null
                };
            }

            string apiUrl = $"{baseUrlDpe}{endpoint}";

            if (!retailerCode.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                retailerCode = "R" + retailerCode;
            }

            async Task<(HttpResponseMessage Response, object LogObject)> SendAsync(string token, bool isRetry)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                request.Headers.Add("x-client-uid", SettingsValues.GetDPEChannel());
                request.Headers.Add("x-username", retailerCode);

                if (!string.IsNullOrWhiteSpace(orderType))
                {
                    request.Headers.Add("x-order-type", orderType);
                }

                request.Content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var logObj = BuildDexActualRequestLog(request, requestBody, inner_method, isRetry);

                using var response = await client.SendAsync(request, cancellationToken);
                return (response, logObj);
            }

            string token = await GetValidDpeJwtToken(cancellationToken);
            var (response, logObj) = await SendAsync(token, false);

            captureDexRequestLog?.Invoke(logObj);

            using (response)
            {
                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                captureDexRawResponseLog?.Invoke(responseContent);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    string newToken = await RefreshDpeJwtToken(cancellationToken);

                    var (retryResponse, retryLogObj) = await SendAsync(newToken, true);
                    captureDexRequestLog?.Invoke(retryLogObj);

                    using (retryResponse)
                    {
                        string retryContent = await retryResponse.Content.ReadAsStringAsync(cancellationToken);
                        captureDexRawResponseLog?.Invoke(retryContent);

                        if (!string.IsNullOrWhiteSpace(retryContent))
                        {
                            return JsonConvert.DeserializeObject<object>(retryContent) ?? new object();
                        }

                        return new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = "Empty response from Dex Portal API after retry.",
                            data = null
                        };
                    }
                }

                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    return JsonConvert.DeserializeObject<object>(responseContent) ?? new object();
                }

                return new HomeWifiCommonResponseModel
                {
                    isError = true,
                    message = "Empty response from Dex Portal API.",
                    data = null
                };
            }
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HTTPPostRequestHomeWifi",
                inner_method_name = inner_method,
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });

            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<DexPortalRawApiResponse> HTTPGetRawRequestHomeWifi(string endpoint, string retailerCode, string inner_method, string? orderNumber = null, bool sendOrderTypeHeader = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            string? baseUrlDpe = SettingsValues.GetDPEBaseUrl()?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(baseUrlDpe))
            {
                string errorResponse = JsonConvert.SerializeObject(
                    new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "DeliveryXpress BaseUrl is not configured.",
                        data = null
                    });

                return new DexPortalRawApiResponse
                {
                    StatusCode = 0,
                    RawResponse = errorResponse,
                    ParsedResponse = JsonConvert.DeserializeObject<object>(errorResponse)
                };
            }

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                string errorResponse = JsonConvert.SerializeObject(
                    new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Dex Portal endpoint is not configured.",
                        data = null
                    });

                return new DexPortalRawApiResponse
                {
                    StatusCode = 0,
                    RawResponse = errorResponse,
                    ParsedResponse = JsonConvert.DeserializeObject<object>(errorResponse)
                };
            }

            if (!endpoint.StartsWith("/"))
            {
                endpoint = "/" + endpoint;
            }

            string apiUrl = $"{baseUrlDpe}{endpoint}";
            string finalRetailerCode = retailerCode ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(finalRetailerCode) &&
                !finalRetailerCode.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                finalRetailerCode = "R" + finalRetailerCode;
            }

            string? orderType = null;

            if (sendOrderTypeHeader && !string.IsNullOrWhiteSpace(orderNumber))
            {
                orderType = await GetDPEOrderTypeFromDbOrDex(orderNumber, retailerCode);
            }

            async Task<HttpResponseMessage> SendAsync(string token)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                request.Headers.Add("x-client-uid", SettingsValues.GetDPEChannel());

                if (sendOrderTypeHeader && !string.IsNullOrWhiteSpace(orderType))
                {
                    request.Headers.Add("x-order-type", orderType);
                }

                if (!string.IsNullOrWhiteSpace(finalRetailerCode))
                {
                    request.Headers.Add("x-username", finalRetailerCode);
                }

                return await client.SendAsync(request, cancellationToken);
            }

            string token = await GetValidDpeJwtToken(cancellationToken);
            using var response = await SendAsync(token);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                string newToken = await RefreshDpeJwtToken(cancellationToken);

                using var retryResponse = await SendAsync(newToken);
                string retryContent = await retryResponse.Content.ReadAsStringAsync(cancellationToken);

                object? retryParsed = null;

                if (!string.IsNullOrWhiteSpace(retryContent))
                {
                    try
                    {
                        retryParsed = JsonConvert.DeserializeObject<object>(retryContent);
                    }
                    catch
                    {
                        retryParsed = new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = retryContent,
                            data = null
                        };
                    }
                }
                else
                {
                    retryContent = JsonConvert.SerializeObject(
                        new HomeWifiCommonResponseModel
                        {
                            isError = true,
                            message = "Empty response from Dex Portal API.",
                            data = null
                        });

                    retryParsed = JsonConvert.DeserializeObject<object>(retryContent);
                }

                return new DexPortalRawApiResponse
                {
                    StatusCode = (int)retryResponse.StatusCode,
                    RawResponse = retryContent,
                    ParsedResponse = retryParsed,
                    IsUnauthorizedRetry = true
                };
            }

            object? parsedResponse = null;

            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                try
                {
                    parsedResponse = JsonConvert.DeserializeObject<object>(responseContent);
                }
                catch
                {
                    parsedResponse = new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = responseContent,
                        data = null
                    };
                }
            }
            else
            {
                responseContent = JsonConvert.SerializeObject(
                    new HomeWifiCommonResponseModel
                    {
                        isError = true,
                        message = "Empty response from Dex Portal API.",
                        data = null
                    });

                parsedResponse = JsonConvert.DeserializeObject<object>(responseContent);
            }

            return new DexPortalRawApiResponse
            {
                StatusCode = (int)response.StatusCode,
                RawResponse = responseContent,
                ParsedResponse = parsedResponse,
                IsUnauthorizedRetry = false
            };
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HTTPGetRawRequestHomeWifi",
                inner_method_name = inner_method,
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });

            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    private object BuildDexActualRequestLog(HttpRequestMessage request, object? requestBody, string innerMethodName, bool isRetry)
    {
        var headers = new Dictionary<string, string>();

        foreach (var header in request.Headers)
        {
            headers[header.Key] = string.Join(",", header.Value ?? Enumerable.Empty<string>());
        }

        if (request.Content?.Headers != null)
        {
            foreach (var header in request.Content.Headers)
            {
                headers[header.Key] = string.Join(",", header.Value ?? Enumerable.Empty<string>());
            }
        }

        if (request.Headers.Authorization != null)
        {
            headers["Authorization"] = request.Headers.Authorization.ToString();
        }

        return new
        {
            request_time = DateTime.Now,
            inner_method_name = innerMethodName,
            is_retry = isRetry,
            method = request.Method.Method,
            url = request.RequestUri?.ToString(),
            headers = headers,
            body = requestBody
        };
    }

    public async Task<HttpResponseMessage> HttpPostEVRequestAsync(string requestString, string apiURL, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var urlBuilder = new UriBuilder(apiURL)
            {
                Query = SettingsValues.GetEV_API_QueryString()
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, urlBuilder.Uri)
            {
                Content = new StringContent(requestString, System.Text.Encoding.UTF8, "text/plain")
            };

            HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
            return response;
        }
        catch (HttpRequestException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestSingleSourceCheck",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequest",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<HttpResponseMessage> HttpPostAiRRequestAsync(string requestString, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var airUrl = SettingsValues.GetAirBaseUrl();
            var base64AuthToken = SettingsValues.GetAirAuthToken();
            var content = new StringContent(requestString, Encoding.UTF8, "text/xml");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, airUrl) { Content = content };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64AuthToken);
            httpRequest.Headers.TryAddWithoutValidation("User-Agent", "Ugw Server/5.0/1.0");
            httpRequest.Headers.Connection.Add("Keep-Alive");
            var response = await client.SendAsync(httpRequest, cancellationToken);

            return response;
        }
        catch (HttpRequestException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestSingleSourceCheck",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequest",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    private async Task<string> GetValidDpeJwtToken(CancellationToken cancellationToken = default)
    {
        DpeSessionTokenModel? dbToken =
            await _dataManager.GetDPEValidSessionToken();

        if (dbToken != null &&
            !string.IsNullOrWhiteSpace(dbToken.access_token))
        {
            return dbToken.access_token;
        }

        await _dpeTokenLock.WaitAsync(cancellationToken);

        try
        {
            dbToken = await _dataManager.GetDPEValidSessionToken();

            if (dbToken != null &&
                !string.IsNullOrWhiteSpace(dbToken.access_token))
            {
                return dbToken.access_token;
            }

            return await LoginAndSaveDpeJwtToken(cancellationToken);
        }
        finally
        {
            _dpeTokenLock.Release();
        }
    }

    private async Task<string> RefreshDpeJwtToken(CancellationToken cancellationToken = default)
    {
        await _dpeTokenLock.WaitAsync(cancellationToken);

        try
        {
            return await LoginAndSaveDpeJwtToken(cancellationToken);
        }
        finally
        {
            _dpeTokenLock.Release();
        }
    }

    private async Task<string> LoginAndSaveDpeJwtToken(CancellationToken cancellationToken = default)
    {
        string baseUrlDpe = SettingsValues.GetDPEBaseUrl()?.TrimEnd('/')
            ?? throw new Exception("DeliveryXpress BaseUrl is not configured.");

        string loginEndpoint = SettingsValues.GetDPELoginEndPoint();

        if (string.IsNullOrWhiteSpace(loginEndpoint))
        {
            throw new Exception("DeliveryXpress Login End Point is not configured.");
        }

        string clientId = SettingsValues.GetDPEClientId();
        string clientSecret = SettingsValues.GetDPEClientSecret();
        string clientUid = SettingsValues.GetDPEChannel();
        string clientCred = SettingsValues.GetDPEClientCred();

        if (string.IsNullOrWhiteSpace(clientUid))
        {
            clientUid = "smartpos";
        }

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new Exception("DeliveryXpress client_id/client_secret is not configured.");
        }

        string apiUrl = $"{baseUrlDpe}{loginEndpoint}";

        var client = _httpClientFactory.CreateClient("LoggedClient");
        client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

        var loginRequest = new
        {
            client_id = clientId,
            client_secret = clientSecret,
            grant_type = clientCred
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        request.Headers.Add("x-client-uid", clientUid);

        request.Content = new StringContent(
            JsonConvert.SerializeObject(loginRequest),
            Encoding.UTF8,
            "application/json"
        );

        Log.ForContext("LogTag", "ApiCall")
            .Information("DPE Token Request: {@Request}", new
            {
                apiUrl,
                client_uid = clientUid,
                grant_type = "client_credentials"
            });

        using var response = await client.SendAsync(request, cancellationToken);

        string responseContent =
            await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Log.ForContext("LogTag", "ApiCall")
                .Error("DPE Token Response Failed: {Response}", responseContent);

            throw new Exception($"DPE token generation failed: {responseContent}");
        }

        Log.ForContext("LogTag", "ApiCall")
            .Information("DPE Token Response received successfully. StatusCode: {StatusCode}", response.StatusCode);

        JObject loginJson =
            JsonConvert.DeserializeObject<JObject>(responseContent)
            ?? throw new Exception("Invalid DPE token response.");

        string accessToken = ExtractAccessToken(loginJson);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new Exception("DPE token response does not contain access token.");
        }

        string tokenType = ExtractTokenType(loginJson);

        int expiresInSeconds = ExtractExpiresInSeconds(loginJson);

        DateTime expiresAt =
            DateTime.Now.AddSeconds(expiresInSeconds <= 0 ? 3300 : expiresInSeconds);

        HomeWifiCommonResponseModel dbResponse =
            await _dataManager.UpsertDPELoginToken(
                accessToken,
                tokenType,
                expiresAt
            );

        if (dbResponse.isError)
        {
            throw new Exception($"Failed to save DPE token: {dbResponse.message}");
        }

        return accessToken;
    }

    private string ExtractAccessToken(JObject loginJson)
    {
        return
            loginJson["access_token"]?.ToString()
            ?? loginJson["token"]?.ToString()
            ?? loginJson["jwt"]?.ToString()
            ?? loginJson["payload"]?["data"]?["access_token"]?.ToString()
            ?? loginJson["payload"]?["access_token"]?.ToString()
            ?? loginJson["payload"]?["token"]?.ToString()
            ?? loginJson["data"]?["access_token"]?.ToString()
            ?? loginJson["data"]?["token"]?.ToString()
            ?? string.Empty;
    }

    private string ExtractTokenType(JObject loginJson)
    {
        string tokenType =
            loginJson["token_type"]?.ToString()
            ?? loginJson["payload"]?["token_type"]?.ToString()
            ?? loginJson["data"]?["token_type"]?.ToString()
            ?? "Bearer";

        return string.IsNullOrWhiteSpace(tokenType)
            ? "Bearer"
            : tokenType;
    }

    private int ExtractExpiresInSeconds(JObject loginJson)
    {
        string? rawValue =
            loginJson["expires_in"]?.ToString()
            ?? loginJson["payload"]?["expires_in"]?.ToString()
            ?? loginJson["data"]?["expires_in"]?.ToString();

        if (int.TryParse(rawValue, out int expiresIn))
        {
            return expiresIn;
        }

        return 3300;
    }

    private async Task<string?> GetDPEOrderTypeFromDbOrDex(string? orderNumber, string retailerCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                return null;
            }

            string? orderType = await _dataManager.GetDPEOrderType(orderNumber);

            if (!string.IsNullOrWhiteSpace(orderType))
            {
                return orderType.Trim();
            }

            string endpoint = SettingsValues.GetDPELeadDetailsEndPoint();

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return null;
            }

            string finalEndPoint = $"{endpoint}/{orderNumber.Trim()}";

            object dexDetailsResponse = await HTTPGetRequestHomeWifi(
                finalEndPoint,
                retailerCode,
                "GetDPEOrderTypeFromDbOrDex"
            );

            string dexDetailsText = JsonConvert.SerializeObject(dexDetailsResponse);

            JObject? dexJson = JsonConvert.DeserializeObject<JObject>(dexDetailsText);

            if (dexJson == null)
            {
                return null;
            }

            JToken? data =
                dexJson["payload"]?.Type == JTokenType.Object
                    ? dexJson["payload"]?["data"]
                    : null;

            if (data == null ||
                data.Type == JTokenType.Null ||
                data.Type == JTokenType.Undefined)
            {
                return null;
            }

            data = HomeWifiLeadListResponseMapper.MapDexLeadDetailsResponseData(data);

            orderType = data?["order_type"]?.ToString();

            return string.IsNullOrWhiteSpace(orderType)
                ? null
                : orderType.Trim();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetDPEOrderTypeFromDbOrDex Exception");

            return null;
        }
    }

    private async Task<(bool ShouldSend, string NwAssessId, string NwAssessStatus)> GetNwInfoForPayslipIfRequired(string? orderNumber, string retailerCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                return (false, string.Empty, string.Empty);
            }

            string endpoint = SettingsValues.GetDPELeadDetailsEndPoint();

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return (false, string.Empty, string.Empty);
            }

            string finalEndPoint = $"{endpoint}/{orderNumber.Trim()}";

            object dexDetailsResponse = await HTTPGetRequestHomeWifi(
                finalEndPoint,
                retailerCode,
                "GetNwInfoForPayslipIfRequired"
            );

            string dexDetailsText = JsonConvert.SerializeObject(dexDetailsResponse);

            JObject? dexJson = JsonConvert.DeserializeObject<JObject>(dexDetailsText);

            if (dexJson == null)
            {
                return (false, string.Empty, string.Empty);
            }

            JToken? data =
                dexJson["payload"]?.Type == JTokenType.Object
                    ? dexJson["payload"]?["data"]
                    : null;

            if (data == null ||
                data.Type == JTokenType.Null ||
                data.Type == JTokenType.Undefined)
            {
                return (false, string.Empty, string.Empty);
            }

            data = HomeWifiLeadListResponseMapper.MapDexLeadDetailsResponseData(data);

            string dexNwAssessId =
                data?["nw_assess_id"]?.ToString()?.Trim() ?? string.Empty;

            string dexNwAssessStatus =
                data?["nw_assess_status"]?.ToString()?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(dexNwAssessId) &&
                !string.IsNullOrWhiteSpace(dexNwAssessStatus))
            {
                return (false, string.Empty, string.Empty);
            }

            DataTable dtNwInfo =
                await _dataManager.GetDPENwInfoByOrderNumber(orderNumber.Trim());

            if (dtNwInfo == null || dtNwInfo.Rows.Count == 0)
            {
                return (false, string.Empty, string.Empty);
            }

            DataRow row = dtNwInfo.Rows[0];

            string dbNwAssessId =
                row.Table.Columns.Contains("NW_ASSESS_ID")
                    ? row["NW_ASSESS_ID"]?.ToString()?.Trim() ?? string.Empty
                    : string.Empty;

            string dbNwAssessStatus =
                row.Table.Columns.Contains("NW_ASSESS_STATUS")
                    ? row["NW_ASSESS_STATUS"]?.ToString()?.Trim() ?? string.Empty
                    : string.Empty;

            if (string.IsNullOrWhiteSpace(dbNwAssessId) ||
                string.IsNullOrWhiteSpace(dbNwAssessStatus))
            {
                return (false, string.Empty, string.Empty);
            }

            return (true, dbNwAssessId, dbNwAssessStatus);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetNwInfoForPayslipIfRequired Exception");
            return (false, string.Empty, string.Empty);
        }
    }
}

public class DexPortalRawApiResponse
{
    public int StatusCode { get; set; }
    public string RawResponse { get; set; } = string.Empty;
    public object? ParsedResponse { get; set; }
    public bool IsUnauthorizedRetry { get; set; }
    public object? ActualRequest { get; set; }
}
