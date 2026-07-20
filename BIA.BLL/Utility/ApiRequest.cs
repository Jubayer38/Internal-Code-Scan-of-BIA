using BIA.Entity.Collections;
using BIA.Entity.ResponseEntity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BIA.BLL.Utility;

public class ApiRequest
{
    public static string baseUrl = String.Empty;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly int defaultTimeout;

    public ApiRequest(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        defaultTimeout = 60;
    }

    public async Task<JObject> HttpPostRequest(object requestObject, string apiUrl, string innerMethod, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var jsonContent = JsonConvert.SerializeObject(requestObject);
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/vnd.api+json");

            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = content
            };

            using var response = await client.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return !string.IsNullOrWhiteSpace(responseContent)
                    ? JsonConvert.DeserializeObject<JObject>(responseContent) ?? new JObject()
                    : new JObject();
            }

            string errorMessage = $"DBSS Error: {(int)response.StatusCode} - {response.ReasonPhrase}";
            if (!string.IsNullOrWhiteSpace(responseContent) && responseContent.Length < 1000)
            {
                errorMessage += $" | Details: {responseContent}";
            }

            var httpException = new HttpRequestException(errorMessage);
            httpException.Data["RequestUrl"] = apiUrl;
            httpException.Data["InnerMethod"] = innerMethod;
            httpException.Data["ResponseBody"] = responseContent;
            throw httpException;
        }
        catch (HttpRequestException httpEx)
        {
            var errorResponse = new Dictionary<string, string?>();

            foreach (DictionaryEntry entry in httpEx.Data)
            {
                errorResponse[entry.Key?.ToString() ?? ""] = entry.Value?.ToString();
            }
            var errorDetails = new
            {
                request_time = DateTime.UtcNow,
                method_name = nameof(HttpPostRequest),
                inner_method_name = innerMethod,
                error_source = httpEx.Source,
                error_code = httpEx.HResult,
                error_description = httpEx.Message,
                error_response = errorResponse
            };

            throw new Exception($"OuterDetails: {JsonConvert.SerializeObject(errorDetails)}", httpEx);
        }
        catch (Exception ex)
        {
            var errorDetails = new
            {
                request_time = DateTime.UtcNow,
                method_name = nameof(HttpPostRequest),
                inner_method_name = innerMethod,
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            };

            throw new Exception($"OuterDetails: {JsonConvert.SerializeObject(errorDetails)}", ex);
        }
    }

    public async Task<JObject> HttpPostRequestRSO(object obj, string apiUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var jsonString = JsonConvert.SerializeObject(obj);

            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = new StringContent(jsonString, Encoding.UTF8, "application/json")
            };

            using var response = await client.SendAsync(request, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<JObject>(responseString) ?? new JObject();
            }
            else
            {
                string message = !string.IsNullOrEmpty(responseString) && responseString.Length < 1000
                    ? responseString
                    : response.ToString();

                throw new Exception("RSO API Error: " + message);
            }
        }
        catch (WebException ex)
        {
            var errorInfo = JsonConvert.SerializeObject(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestRSO",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });

            throw new Exception("OuterDetails: " + errorInfo, ex);
        }
        catch (Exception ex)
        {
            var errorInfo = JsonConvert.SerializeObject(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestRSO",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });

            throw new Exception("OuterDetails: " + errorInfo, ex);
        }
    }

    public async Task<eShopOrderResponseModel> HttpPostRequesteSHOP(object obj, string apiUrl, string authorizationHeader, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            string requestJson = JsonConvert.SerializeObject(obj);
            string responseContent = string.Empty;

            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = content
            };

            HttpResponseMessage response = await client.SendAsync(requestMessage, cancellationToken);
            responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseModel = JsonConvert.DeserializeObject<eShopOrderResponseModel>(responseContent);

                if (responseModel == null)
                {
                    throw new Exception("Failed to deserialize or invalid response from eShop API.");
                }

                return responseModel;
            }
            else
            {
                string message = string.Empty;

                if (responseContent != null && responseContent.Length > 0 && responseContent.Length < 1000)
                {
                    message = responseContent;
                    throw new Exception("eShop API Error: " + message);
                }
                else
                {
                    message = response.ToString();
                    throw new Exception("eShop API Error: " + message);
                }
            }
        }
        catch (WebException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequesteSHOP",
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
                method_name = "HttpPostRequesteSHOP",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HttpPostRequestSIMSerial(object obj, string apiUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var jsonString = JsonConvert.SerializeObject(obj);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiUrl, content, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<object>(responseString) ?? new object();
            }
            else
            {
                var resp = await response.Content.ReadAsStringAsync(cancellationToken);
                string message = string.Empty;

                if (resp != null && resp.Length > 0 && resp.Length < 1000)
                {
                    message = resp;
                    throw new Exception("DMS Error: " + message);
                }
                else
                {
                    message = response.ToString();
                    throw new Exception("DMS Error: " + message);
                }
            }
        }
        catch (WebException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestSIMSerial",
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
                method_name = "HttpPostRequestSIMSerial",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HttpPostRequestFirstRecharge(object obj, string apiUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var jsonString = JsonConvert.SerializeObject(obj);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiUrl, content, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<object>(responseString) ?? new object();
            }
            else
            {
                var resp = await response.Content.ReadAsStringAsync(cancellationToken);
                string message = string.Empty;

                if (resp != null && resp.Length > 0 && resp.Length < 1000)
                {
                    message = resp;
                    throw new Exception("Retailer API Error: " + message);
                }
                else
                {
                    message = response.ToString();
                    throw new Exception("Retailer API Error: " + message);
                }
            }
        }
        catch (WebException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestFirstRecharge",
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
                method_name = "HttpPostRequestFirstRecharge",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HttpPostRequestXML(string xmlData, string apiUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var content = new StringContent(xmlData, Encoding.UTF8, "application/xml");

            var response = await client.PostAsync(apiUrl, content, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return responseString;
            }
            else
            {
                string message = string.Empty;

                if (!string.IsNullOrEmpty(responseString) && responseString.Length < 1000)
                {
                    message = responseString;
                    throw new Exception("Pretups Error: " + message);
                }
                else
                {
                    message = response.ToString();
                    throw new Exception("Pretups Error: " + message);
                }
            }
        }
        catch (WebException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestXML",
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
                method_name = "HttpPostRequestXML",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HttpPatchRequest(object obj, string apiUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var jsonString = JsonConvert.SerializeObject(obj);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/vnd.api+json");

            var request = new HttpRequestMessage(HttpMethod.Patch, apiUrl)
            {
                Content = content
            };

            var response = await client.SendAsync(request, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<object>(responseString) ?? new object();
            }
            else
            {
                var resp = await response.Content.ReadAsStringAsync(cancellationToken);
                string message = string.Empty;

                if (resp != null && resp.Length > 0 && resp.Length < 1000)
                {
                    message = resp;
                    throw new Exception("DBSS Error: " + message);
                }
                else
                {
                    message = response.ToString();
                    throw new Exception("DBSS Error: " + message);
                }
            }
        }
        catch (WebException ex)
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

    public async Task<object> HttpDeleteRequest(object obj, string apiUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var jsonString = JsonConvert.SerializeObject(obj);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/vnd.api+json");

            var request = new HttpRequestMessage(HttpMethod.Delete, apiUrl)
            {
                Content = content
            };

            var response = await client.SendAsync(request, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<object>(responseString) ?? new object();
            }
            else
            {
                var resp = await response.Content.ReadAsStringAsync(cancellationToken);
                string message = string.Empty;

                if (resp != null && resp.Length > 0 && resp.Length < 1000)
                {
                    message = resp;
                    throw new Exception("DBSS Error: " + message);
                }
                else
                {
                    message = response.ToString();
                    throw new Exception("DBSS Error: " + message);
                }
            }
        }
        catch (WebException ex)
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
        string message = string.Empty;
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            using var response = await client.GetAsync(apiUrl, cancellationToken).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(responseString))
                {
                    var resObj = JsonConvert.DeserializeObject<JObject>(responseString);
                    return resObj ?? new JObject();
                }

                return new JObject();
            }
            else
            {
                var resp = await response.Content.ReadAsStringAsync(cancellationToken);

                if (resp != null && resp.Length > 0 && resp.Length < 1000)
                {
                    message = resp;
                    throw new Exception("DBSS Error: " + message);
                }
                else
                {
                    try
                    {
                        message = response.ToString();
                    }
                    catch
                    {
                    }
                    throw new Exception("DBSS Error: " + message);
                }
            }
        }
        catch (HttpRequestException httpEx)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                main_method_name = "HttpGetRequest",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = httpEx.Source,
                error_code = httpEx.HResult,
                error_description = httpEx.Message
            });
            throw new Exception("OuterDetails: " + text, httpEx);
        }
        catch (JsonException jsonEx)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                main_method_name = "HttpGetRequest",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = jsonEx.Source,
                error_code = jsonEx.HResult,
                error_description = jsonEx.Message
            });
            throw new Exception("OuterDetails: " + text, jsonEx);
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                main_method_name = "HttpGetRequest",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<object> HttpGetRequestForMNPPortIn(string apiUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var response = await client.GetAsync(apiUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                var resObj = JsonConvert.DeserializeObject<object>(responseString);

                return resObj ?? new object();
            }
            else
            {
                var resp = await response.Content.ReadAsStringAsync(cancellationToken);
                string message = string.Empty;

                if (resp != null && resp.Length > 0 && resp.Length < 1000)
                {
                    message = resp;
                    throw new Exception(message);
                }
                else
                {
                    message = response.ToString();
                    throw new Exception(message);
                }
            }
        }
        catch (Exception ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpGetRequestForMNPPortIn",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<DMSLoginResponse> HttpPostRequestDMSLogin(object obj, string methodUrl, string inner_method, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            var uri = new Uri(baseUrl + methodUrl);
            var jsonString = JsonConvert.SerializeObject(obj);
            var data = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(uri, data, cancellationToken).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<DMSLoginResponse>(responseString) ?? new DMSLoginResponse();
            }
            else
            {
                var resp = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception(resp);
            }
        }
        catch (HttpRequestException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestDMSLogin",
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
                method_name = "HttpPostRequestDMSLogin",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    public async Task<DMSICCCheckResponse> HttpPostRequestDMSICCCheck(object obj, string methodUrl, string inner_method, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LoggedClient");
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var uri = new Uri(baseUrl + methodUrl);
            var jsonString = JsonConvert.SerializeObject(obj);
            var data = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(uri, data, cancellationToken).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<DMSICCCheckResponse>(responseString) ?? new DMSICCCheckResponse();
            }
            else
            {
                try
                {
                    return JsonConvert.DeserializeObject<DMSICCCheckResponse>(responseString) ?? new DMSICCCheckResponse();
                }
                catch
                {
                    var resp = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new Exception(resp);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            string? text = Convert.ToString(new
            {
                request_time = DateTime.Now,
                method_name = "HttpPostRequestDMSLogin",
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
                method_name = "HttpPostRequestDMSLogin",
                inner_method_name = inner_method,
                procedure_name = "",
                error_source = ex.Source,
                error_code = ex.HResult,
                error_description = ex.Message
            });
            throw new Exception("OuterDetails: " + text, ex);
        }
    }

    private bool isDBSSErrorOccurred(WebException exception)
    {
        var error = exception.Response as HttpWebResponse;
        if (error != null)
        {
            return error.StatusCode != HttpStatusCode.BadRequest;
        }
        else
        {
            return true;
        }
    }
}
