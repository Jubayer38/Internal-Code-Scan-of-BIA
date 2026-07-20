using BIA.BLL.BLLServices;
using BIA.Entity.Collections;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.PopulateModel;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BIA.BLL.Utility
{
    public class FTR_Restrict
    {
        private readonly BLLFTRRestriction _bLLFTRRestriction;
        private readonly BllHandleException _manageExecption;
        private readonly BLLLog _bLLLog;
        private readonly BLLCommon _bLLCommon;
        private readonly ApiCall _genericApiCall;
        public FTR_Restrict(BLLFTRRestriction bLLFTRRestriction, BllHandleException manageExecption, BLLLog bLLLog, BLLCommon bLLCommon, ApiCall genericApiCall2)
        {
            _bLLFTRRestriction = bLLFTRRestriction;
            _manageExecption = manageExecption;
            _bLLLog = bLLLog;
            _bLLCommon = bLLCommon;
            _genericApiCall = genericApiCall2;
        }
        public async Task<RetailerBalanceRespModel> CheckEVBalance(string userName, string TransactionMsisdn, string ev_pin, string bi_token_no, string subscriberNo)
        {
            LogModel log = new LogModel();
            string eTopUPValue = string.Empty;
            DateTime reqTime = DateTime.Now;
            DateTime resTime = DateTime.Now;
            BSS_Json byteArrayConverter = new BSS_Json();
            string responseContent = string.Empty;
            RetailerBalanceRespModel respModel = new RetailerBalanceRespModel();

            try
            {
                if (!TransactionMsisdn.StartsWith("88"))
                {
                    TransactionMsisdn = "88" + TransactionMsisdn;
                }
                
                log.msisdn = TransactionMsisdn;

                string msisdn = await _bLLFTRRestriction.GetRetailerItopUpNumber(userName);

                if (msisdn.Substring(0, 3) == "880")
                {
                    msisdn = msisdn.Substring(3);
                }
                else if (msisdn.Substring(0, 1) == "0")
                {
                    msisdn = msisdn.Substring(1);
                }

                string apiUrl = SettingsValues.GetEV_API_URL();

                string requestBody = SettingsValues.GetEV_API_RequestBody();

                requestBody = string.Format(requestBody, msisdn, ev_pin, msisdn);
                log.req_time = DateTime.Now;
                log.req_blob = byteArrayConverter.GetGenericJsonData(requestBody);

                var resp = await _genericApiCall.HttpPostEVRequestAsync(requestBody, apiUrl, "CheckEVBalance");

                if (resp.IsSuccessStatusCode)
                {
                    responseContent = await resp.Content.ReadAsStringAsync();

                    log.res_blob = byteArrayConverter.GetGenericJsonData(responseContent);

                    log.res_time = DateTime.Now;

                    string[] keyValuePairs = responseContent.Split('&');

                    var dictionary = new System.Collections.Generic.Dictionary<string, string>();

                    foreach (string pair in keyValuePairs)
                    {
                        string[] parts = pair.Split('=');

                        dictionary.Add(parts[0], parts[1]);
                    }
                    var jsonResponse = JsonConvert.SerializeObject(dictionary, Formatting.Indented);
                    JObject jsonObject = JObject.Parse(jsonResponse);

                    string message = jsonObject["MESSAGE"]?.ToString() ?? "";

                    if (message != null && message.Contains("eTopUP"))
                    {
                        string pattern = @"eTopUP:(\d+)";

                        Match match = Regex.Match(message, pattern);

                        if (match.Success)
                        {
                            respModel.Balance = match.Groups[1].Value;
                            log.is_success = 1;
                        }
                        else
                        {
                            respModel.Balance = "0";
                            log.is_success = 1;
                        }
                    }
                    else
                    {
                        respModel.Balance = "0";
                        respModel.message = message ?? "EV System message is empty!";
                        log.is_success = 1;
                    }
                }
                else
                {
                    responseContent = await resp.Content.ReadAsStringAsync();
                    respModel.Balance = "0";
                    respModel.message = "Balance check API response does not indicate the success!";
                    log.is_success = 1;
                    log.res_blob = byteArrayConverter.GetGenericJsonData(responseContent);
                    log.res_time = DateTime.Now;
                }
                resTime = DateTime.Now;

                return respModel;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");

                respModel.Balance = "0";
                respModel.message = ex.Message;

                ErrorDescription error;
                try
                {
                    error = await _manageExecption.ManageException(ex, ex.HResult, "BIA") ?? new ErrorDescription();
                }
                catch
                {
                    error = new ErrorDescription();
                }

                log.req_time = reqTime;
                log.res_time = resTime;
                log.res_blob = byteArrayConverter.GetGenericJsonData(responseContent != null ? responseContent : ex.Message);

                log.message = error.error_custom_msg ?? error.error_description ?? "Unknown error";
                log.error_code = error.error_code ?? "";
                log.error_source = error.error_source ?? "BIA";
                log.is_success = 0;

                throw;
            }
            finally
            {
                log.bi_token_number = bi_token_no;
                log.msisdn = TransactionMsisdn;
                log.user_id = userName;
                log.res_time = resTime;                
                log.integration_point_from = (int)IntegrationPoint.bss_service;
                log.integration_point_to = (int)IntegrationPoint.bss;
                log.method_name = "CheckEVBalance";
                log.purpose_number = "2";
                await _bLLLog.BALogInsert(log);
            }
        }

        public async Task<FTRAirResponseModel> FTRRestrictionRequsetToAIR(RechargeRequestModel item, string channelName, string userName)
        {
            LogModel log = new LogModel();
            object status_response = new object();
            DateTime reqTime = DateTime.Now;
            DateTime resTime = DateTime.Now;
            FTR_RestrictionPopulateModel populateModel = new FTR_RestrictionPopulateModel();
            BSS_Json byteArrayConverter = new BSS_Json();
            FTRDBUpdateModel model = new FTRDBUpdateModel();
            XDocument respXML = new XDocument();
            XmlToByteConverter xmlToByte = new XmlToByteConverter();
            FTRAirResponseModel fTRAir = new FTRAirResponseModel();
            FTROfferIdRespModel fTROfferId = new FTROfferIdRespModel();
            int responseCode = 1;
            string responseString = string.Empty;
            try
            {
                // Safe string checks for Subscriber Number (prevents crash on short/null numbers)
                if (string.IsNullOrWhiteSpace(item.subscriberNo))
                {
                    throw new ArgumentException("Subscriber number cannot be null or empty.");
                }
                if (string.IsNullOrWhiteSpace(channelName))
                {
                    throw new ArgumentException("Channel Name cannot be null or empty. Please retry!");
                }
                if (string.IsNullOrWhiteSpace(userName))
                {
                    throw new ArgumentException("Retailer Id cannot be null or empty. Please retry!");
                }
                if (string.IsNullOrWhiteSpace(item.bi_token_number))
                {
                    throw new ArgumentException("token_number cannot be null or empty. Please retry!");
                }

                if (item.subscriberNo.StartsWith("01"))
                {
                    item.subscriberNo = "88" + item.subscriberNo;
                }
                else if (!item.subscriberNo.StartsWith("88"))
                {
                    item.subscriberNo = "88" + item.subscriberNo;
                }
                else if (!item.subscriberNo.StartsWith("880"))
                {
                    item.subscriberNo = "880" + item.subscriberNo;
                }
                fTROfferId = await _bLLCommon.GetOfferIdforFTR(channelName, userName, item.bi_token_number);
                string hostName = Dns.GetHostName();
                string hostLastPart = hostName.Split('-').Last();
                UpdateOfferRequest request = new UpdateOfferRequest();
                request.OriginNodeType = SettingsValues.GetoriginNodeType();
                request.OriginHostName = hostLastPart;
                request.OriginTransactionID = item.bi_token_number.ToString();
                request.OriginTimeStamp = DateTime.Now;
                DateTime expDate = request.OriginTimeStamp.AddMinutes(SettingsValues.GetFTRExpairyDate());
                request.expiryDateTime = expDate;
                request.SubscriberNumberNAI = SettingsValues.GetsubscriberNumberNAI();
                request.NegotiatedCapabilities = SettingsValues.GetnegotiatedCapabilities();
                request.SubscriberNumber = item.subscriberNo;
                request.OfferID = Convert.ToInt32(fTROfferId.offer_id);
                request.OfferType = 2;
                log.req_time = DateTime.Now;
                // Build XML
                var xml = new XDocument(
                    new XElement("methodCall",
                        new XElement("methodName", "UpdateOffer"),
                        new XElement("params",
                            new XElement("param",
                                new XElement("value",
                                    new XElement("struct",
                                        new XElement("member",
                                            new XElement("name", "originNodeType"),
                                            new XElement("value", new XElement("string", request.OriginNodeType))
                                        ),
                                        new XElement("member",
                                            new XElement("name", "originHostName"),
                                            new XElement("value", new XElement("string", request.OriginHostName))
                                        ),
                                        new XElement("member",
                                            new XElement("name", "originTransactionID"),
                                            new XElement("value", new XElement("string", request.OriginTransactionID))
                                        ),
                                        new XElement("member",
                                            new XElement("name", "originTimeStamp"),
                                            new XElement("value", new XElement("dateTime.iso8601", request.OriginTimeStamp.ToString("yyyyMMddTHH:mm:ss+0600")))
                                        ),
                                        new XElement("member",
                                            new XElement("name", "subscriberNumberNAI"),
                                            new XElement("value", new XElement("int", request.SubscriberNumberNAI))
                                        ),
                                        new XElement("member",
                                            new XElement("name", "negotiatedCapabilities"),
                                            new XElement("value",
                                                new XElement("array",
                                                    new XElement("data",
                                                        new XElement("value", new XElement("int", request.NegotiatedCapabilities))
                                                    )
                                                )
                                            )
                                        ),
                                        new XElement("member",
                                            new XElement("name", "subscriberNumber"),
                                            new XElement("value", new XElement("string", request.SubscriberNumber))
                                        ),
                                        new XElement("member",
                                            new XElement("name", "offerID"),
                                            new XElement("value", new XElement("int", request.OfferID))
                                        ),
                                        new XElement("member",
                                            new XElement("name", "offerType"),
                                            new XElement("value", new XElement("int", request.OfferType))
                                        ),
                                        new XElement("member",
                                            new XElement("name", "expiryDateTime"),
                                            new XElement("value", new XElement("dateTime.iso8601", request.expiryDateTime.ToString("yyyyMMddTHH:mm:ss+0600")))
                                        )
                                    )
                                )
                            )
                        )
                    )
                );
                string xmlString = xml.ToString();

                // 1. Populate request blob immediately (ensures log gets stored if the POST fails)
                log.req_blob = xmlToByte.ConvertXmlToByteArray(xmlString);

                var response = await _genericApiCall.HttpPostAiRRequestAsync(xmlString, "FTRRestrictionRequsetToAIR");

                responseString = await response.Content.ReadAsStringAsync();
                
                try
                {
                    respXML = SecureXmlParser.Parse(responseString);
                    var responseCodeElement = respXML
                        .Descendants("member")
                        .FirstOrDefault(m => m.Element("name")?.Value == "responseCode")
                        ?.Element("value")
                        ?.Element("i4");
                    if (responseCodeElement != null && int.TryParse(responseCodeElement.Value, out int parsedCode))
                    {
                        responseCode = parsedCode;
                    }
                    else
                    {
                        responseCode = -1;
                    }
                    fTRAir.responseCode = responseCode;
                    if (responseCode == 0)
                    {
                        fTRAir.Message = "Restricted!";
                        model.is_ftr_restricted = 1;
                        model.ftr_message = "Restricted!";
                    }
                    else
                    {
                        string safeRespMsg = SafeSubstring(responseString, 200);
                        fTRAir.Message = safeRespMsg;
                        model.is_ftr_restricted = 0;
                        model.ftr_message = safeRespMsg;
                    }
                    log.res_blob = xmlToByte.ConvertXmlToByteArray(responseString);
                    model.bi_token_no = (long)Convert.ToDouble(item.bi_token_number);
                    await _bLLFTRRestriction.FTR_UPdateData(model);
                }
                catch (Exception)
                {
                    // Fallback response string log
                    fTRAir.responseCode = responseCode;
                    string safeRespMsg = SafeSubstring(responseString, 200);
                    fTRAir.Message = safeRespMsg;
                    model.is_ftr_restricted = 0;
                    model.bi_token_no = (long)Convert.ToDouble(item.bi_token_number);
                    model.ftr_message = safeRespMsg;
                    log.res_blob = byteArrayConverter.GetGenericJsonData(responseString);

                    await _bLLFTRRestriction.FTR_UPdateData(model);
                }
                log.is_success = 1;
                log.res_time = DateTime.Now;
            }
            catch (Exception ex)
            {
                // Handle error states without crashing via SafeSubstring
                fTRAir.responseCode = responseCode;
                string safeExceptionMsg = SafeSubstring(ex.Message, 200);
                fTRAir.Message = safeExceptionMsg;
                model.is_ftr_restricted = 0;
                model.bi_token_no = (long)Convert.ToDouble(item.bi_token_number);
                model.ftr_message = safeExceptionMsg;
                ErrorDescription error = await _manageExecption.ManageException(ex, ex.HResult, "BIA");
                log.res_time = DateTime.Now;
                log.res_string = JsonConvert.SerializeObject(respXML);

                string resblob = ex.Message +" Details_response: "+ responseString;

                log.res_blob = byteArrayConverter.GetGenericJsonData(resblob);
                log.message = error.error_custom_msg ?? error.error_description;
                log.error_code = error.error_code;
                log.error_source = error.error_source ?? "BIA";
                log.is_success = 0;
                await _bLLFTRRestriction.FTR_UPdateData(model);
                throw;
            }
            finally
            {
                // Safe string conversions (prevents crash if bi_token_number is null)
                log.bi_token_number = item.bi_token_number?.ToString() ?? string.Empty;
                log.msisdn = item.subscriberNo;
                log.user_id = item.retailerCode;
                log.res_time = DateTime.Now;
                log.integration_point_from = (int)IntegrationPoint.bss_service;
                log.integration_point_to = (int)IntegrationPoint.bss;
                log.method_name = "FTRRestrictionRequsetToAIR";
                log.purpose_number = "2";
                await _bLLLog.BALogInsert(log);
            }
            return fTRAir;
        }

        private static string SafeSubstring(string? input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input.Length <= maxLength ? input : input.Substring(0, maxLength);
        }
        // Thread-safe singleton HttpClient to prevent socket exhaustion
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public async Task<FTRAirResponseModel> FTRRestrictionRequsetToAIRLUS(RechargeRequestModel item, string channelName, string userName)
        {
            LogModel log = new LogModel();
            object status_response = new object();
            DateTime reqTime = DateTime.Now;
            DateTime resTime = DateTime.Now;
            FTR_RestrictionPopulateModel populateModel = new FTR_RestrictionPopulateModel();
            BSS_Json byteArrayConverter = new BSS_Json();
            FTRDBUpdateModel model = new FTRDBUpdateModel();
            XDocument respXML = new XDocument();
            XmlToByteConverter xmlToByte = new XmlToByteConverter();
            FTRAirResponseModel fTRAir = new FTRAirResponseModel();
            FTROfferIdRespModel fTROfferId = new FTROfferIdRespModel();
            int responseCode = 1;
            try
            {
                if (item.subscriberNo.Substring(0, 2) == "01")
                {
                    item.subscriberNo = "88" + item.subscriberNo;
                }
                else if (item.subscriberNo.Substring(0, 2) != "88")
                {
                    item.subscriberNo = "88" + item.subscriberNo;
                }
                else if (item.subscriberNo.Substring(0, 3) != "880")
                {
                    item.subscriberNo = "880" + item.subscriberNo;
                }

                fTROfferId = await _bLLCommon.GetOfferIdforFTRV2(channelName, userName, item.bi_token_number,1);

                UpdateOfferRequest request = new UpdateOfferRequest();
                request.OriginNodeType = SettingsValues.GetoriginNodeType();
                request.OriginHostName = Dns.GetHostName();
                request.OriginTransactionID = item.bi_token_number.ToString();
                request.OriginTimeStamp = DateTime.Now;
                DateTime expDate = request.OriginTimeStamp.AddMinutes(SettingsValues.GetFTRExpairyDate());
                request.expiryDateTime = expDate;
                request.SubscriberNumberNAI = SettingsValues.GetsubscriberNumberNAI();
                request.NegotiatedCapabilities = SettingsValues.GetnegotiatedCapabilities();
                request.SubscriberNumber = item.subscriberNo;

                request.OfferID = Convert.ToInt32(fTROfferId.offer_id);

                request.OfferType = 2;
                log.req_time = DateTime.Now;
                #region Request String             
                var xml = new XDocument(
                    new XElement("methodCall",
                        new XElement("methodName", "UpdateOffer"),
                        new XElement("params",
                            new XElement("param",
                                new XElement("value",
                                    new XElement("struct",
                                        new XElement("member",
                                            new XElement("name", "originNodeType"),
                                            new XElement("value",
                                                new XElement("string", request.OriginNodeType)
                                            )
                                        ),
                                        new XElement("member",
                                            new XElement("name", "originHostName"),
                                            new XElement("value",
                                                new XElement("string", request.OriginHostName)
                                            )
                                        ),
                                        new XElement("member",
                                            new XElement("name", "originTransactionID"),
                                            new XElement("value",
                                                new XElement("string", request.OriginTransactionID)
                                            )
                                        ),
                                        new XElement("member",
                                            new XElement("name", "originTimeStamp"),
                                            new XElement("value",
                                                new XElement("dateTime.iso8601", request.OriginTimeStamp.ToString("yyyyMMddTHH:mm:ss+0600"))
                                            )
                                        ),
                                        new XElement("member",
                                            new XElement("name", "subscriberNumberNAI"),
                                            new XElement("value",
                                                new XElement("int", request.SubscriberNumberNAI)
                                            )
                                        ),
                                        new XElement("member",
                                            new XElement("name", "negotiatedCapabilities"),
                                            new XElement("value",
                                                new XElement("array",
                                                    new XElement("data",
                                                        new XElement("value",
                                                            new XElement("int", request.NegotiatedCapabilities)
                                                        )
                                                    )
                                                )
                                            )
                                        ),
                                        new XElement("member",
                                            new XElement("name", "subscriberNumber"),
                                            new XElement("value",
                                                new XElement("string", request.SubscriberNumber)
                                            )
                                        ),
                                        new XElement("member",
                                            new XElement("name", "offerID"),
                                            new XElement("value",
                                                new XElement("int", request.OfferID)
                                            )
                                        ),
                                        new XElement("member",
                                            new XElement("name", "offerType"),
                                            new XElement("value",
                                                new XElement("int", request.OfferType)
                                            )
                                        ),
                                        new XElement("member",
                                            new XElement("name", "expiryDateTime"),
                                            new XElement("value",
                                                new XElement("dateTime.iso8601", request.expiryDateTime.ToString("yyyyMMddTHH:mm:ss+0600"))
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                );
                #endregion

                var airUrl = SettingsValues.GetAirBaseUrl();
                var base64AuthToken = SettingsValues.GetAirAuthToken();

                using (var client = new HttpClient())
                {
                    //var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64AuthToken);
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Ugw Server/5.0/1.0");
                    client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
                    var content = new StringContent(xml.ToString(), Encoding.UTF8, "text/xml");

                    XDocument xmlDoc = XDocument.Parse(xml.ToString());
                    log.req_time = DateTime.Now;
                    log.req_blob = xmlToByte.ConvertXmlToByteArray(xml.ToString());

                    var response = await client.PostAsync(airUrl, content);

                    try
                    {
                        var responseString = await response.Content.ReadAsStringAsync();

                        try
                        {
                            respXML = SecureXmlParser.Parse(responseString);

                            var responseCodeElement = respXML
                                .Descendants("member")
                                .FirstOrDefault(m => m.Element("name")?.Value == "responseCode")
                                ?.Element("value")
                                ?.Element("i4");

                            if (responseCodeElement != null && int.TryParse(responseCodeElement.Value, out int parsedCode))
                            {
                                responseCode = parsedCode;
                            }
                            else
                            {
                                responseCode = -1;
                            }

                            if (responseCode == 0)
                            {
                                fTRAir.responseCode = responseCode;
                                fTRAir.Message = "LUS Offer Attached!";
                                model.is_ftr_restricted = 1;
                                model.bi_token_no = (long)Convert.ToDouble(item.bi_token_number);
                                model.ftr_message = "LUS Offer Attached!";
                                await _bLLFTRRestriction.LUS_FTR_UpdateData(model);
                            }
                            else
                            {
                                fTRAir.responseCode = responseCode;
                                fTRAir.Message = responseString.ToString().Substring(1, 200);
                                model.is_ftr_restricted = 0;
                                model.bi_token_no = (long)Convert.ToDouble(item.bi_token_number);
                                model.ftr_message = responseString.ToString().Substring(1, 200);
                                await _bLLFTRRestriction.LUS_FTR_UpdateData(model);
                            }
                            log.res_blob = xmlToByte.ConvertXmlToByteArray(responseString);
                        }
                        catch (Exception)
                        {
                            fTRAir.responseCode = responseCode;
                            fTRAir.Message = responseString.ToString().Substring(1, 200);
                            model.is_ftr_restricted = 0;
                            model.bi_token_no = (long)Convert.ToDouble(item.bi_token_number);
                            model.ftr_message = responseString.ToString().Substring(1, 200);
                            log.res_blob = byteArrayConverter.GetGenericJsonData(responseString);
                            await _bLLFTRRestriction.LUS_FTR_UpdateData(model);
                        }

                        log.is_success = 1;
                    }
                    catch (Exception ex)
                    {
                        fTRAir.responseCode = responseCode;
                        fTRAir.Message = ex.Message.ToString().Substring(1, 200);
                        log.res_blob = byteArrayConverter.GetGenericJsonData(ex.Message.ToString());
                        model.is_ftr_restricted = 0;
                        model.bi_token_no = (long)Convert.ToDouble(item.bi_token_number);
                        model.ftr_message = ex.Message.ToString().Substring(1, 200);
                        log.res_time = DateTime.Now;
                        await _bLLFTRRestriction.LUS_FTR_UpdateData(model);
                        throw;
                    }

                    log.res_time = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                fTRAir.responseCode = responseCode;
                fTRAir.Message = ex.Message.ToString().Substring(1, 200);
                model.is_ftr_restricted = 0;
                ErrorDescription error = await _manageExecption.ManageException(ex, ex.HResult, "BIA");

                //log.req_time = reqTime;
                log.res_time = DateTime.Now;
                log.res_string = JsonConvert.SerializeObject(respXML).ToString();
                log.res_blob = byteArrayConverter.GetGenericJsonData(ex.Message.ToString());
                log.message = error.error_custom_msg ?? error.error_description;
                log.error_code = error.error_code;
                log.error_source = error.error_source ?? "BIA";
                log.is_success = 0;
                model.bi_token_no = (long)Convert.ToDouble(item.bi_token_number);
                model.ftr_message = ex.Message.ToString().Substring(1, 200);
                await _bLLFTRRestriction.LUS_FTR_UpdateData(model);
                throw;
            }
            finally
            {
                log.bi_token_number = item.bi_token_number.ToString();
                log.msisdn = item.subscriberNo;
                log.user_id = item.retailerCode;
                log.res_time = DateTime.Now;
                log.integration_point_from = (int)IntegrationPoint.bss_service;
                log.integration_point_to = (int)IntegrationPoint.bss;
                log.method_name = "FTRRestrictionRequsetToAIRLUS";
                log.purpose_number = "2";
                await _bLLLog.BALogInsert(log);
            }

            return fTRAir;
        }
    }
}
