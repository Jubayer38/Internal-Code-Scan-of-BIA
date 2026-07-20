using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BIA.BLL.Utility
{
    public static class HomeWifiLeadListResponseMapper
    {
        public static JToken? MapDexLeadListResponseData(JToken? responseData)
        {
            return MapDexLeadResponseData(responseData);
        }

        public static JToken? MapDexLeadDetailsResponseData(JToken? responseData)
        {
            return MapDexLeadResponseData(responseData);
        }

        private static JToken? MapDexLeadResponseData(JToken? responseData)
        {
            if (responseData == null ||
                responseData.Type == JTokenType.Null ||
                responseData.Type == JTokenType.Undefined)
            {
                return responseData;
            }

            // Lead List / Order List: payload.data is array
            if (responseData.Type == JTokenType.Array)
            {
                var mappedArray = new JArray();

                foreach (JToken item in responseData)
                {
                    if (item.Type == JTokenType.Object)
                    {
                        mappedArray.Add(MapSingleLead((JObject)item));
                    }
                    else
                    {
                        mappedArray.Add(item.DeepClone());
                    }
                }

                return mappedArray;
            }

            // Lead Details / Order Details: payload.data is single object
            if (responseData.Type == JTokenType.Object)
            {
                return MapSingleLead((JObject)responseData);
            }

            return responseData.DeepClone();
        }

        private static JObject MapSingleLead(JObject dexLead)
        {
            var appLead = new JObject();

            foreach (var property in dexLead.Properties())
            {
                /*
                    Do not send raw DeX item/items to APP.
                    Also remove old flat device fields because DEVICE can now be multiple.
                */
                if (IsSame(property.Name, "items") ||
                    IsSame(property.Name, "item") ||
                    IsLegacyFlatDeviceField(property.Name))
                {
                    continue;
                }

                /*
                    Root-level offer_name / offer_code will be rebuilt from SIM.offer only.
                    So skip any root-level offer fields from DeX/mock.
                */
                if (IsSame(property.Name, "offer_name") ||
                    IsSame(property.Name, "offer_code") ||
                    IsSame(property.Name, "plan_name"))
                {
                    continue;
                }

                appLead[property.Name] = property.Value.DeepClone();
            }

            JToken? itemsToken =
                GetPropertyIgnoreCase(dexLead, "items") ??
                GetPropertyIgnoreCase(dexLead, "item");

            List<JObject> deviceItems =
                GetItemsByType(itemsToken, "DEVICE").ToList();

            List<JObject> simItems =
                GetItemsByType(itemsToken, "SIM").ToList();

            JObject? simItem = simItems.FirstOrDefault();

            JToken? simAttributes = GetPropertyIgnoreCase(simItem, "attributes");
            JToken? simOffer = GetPropertyIgnoreCase(simItem, "offer");

            /*
                Root-level offer_name and offer_code must always come from SIM.offer only.
                If SIM.offer is null, root-level offer_name and offer_code will be null.

                DEVICE.offer will only be available inside devices[].
            */
            string? offerName =
                GetString(GetPropertyIgnoreCase(simOffer, "offer_details"));

            string? offerCode =
                GetString(GetPropertyIgnoreCase(simOffer, "offer_code"));

            string? subscriptionCode =
                GetString(GetPropertyIgnoreCase(simOffer, "subscription_code"));

            string? orderedMsisdn = FirstNonEmpty(
                GetString(GetPropertyIgnoreCase(simItem, "identifier")),
                GetString(GetPropertyIgnoreCase(dexLead, "ordered_msisdn"))
            );

            string? subscriptionType = FirstNonEmpty(
                GetString(GetPropertyIgnoreCase(simAttributes, "subscription_type")),
                GetString(GetPropertyIgnoreCase(dexLead, "subscription_type"))
            );

            string? simkitType = FirstNonEmpty(
                GetString(GetPropertyIgnoreCase(simAttributes, "simkit_type")),
                GetString(GetPropertyIgnoreCase(dexLead, "simkit_type"))
            );

            SetField(appLead, "offer_name", offerName);
            SetField(appLead, "offer_code", offerCode);
            SetField(appLead, "subscription_code", subscriptionCode);

            // Multiple DEVICE items will be returned here.
            appLead["devices"] = BuildDeviceArray(deviceItems, dexLead);

            SetField(appLead, "subscription_type", subscriptionType);
            SetField(appLead, "simkit_type", simkitType);
            SetField(appLead, "ordered_msisdn", orderedMsisdn);

            return appLead;
        }

        private static JArray BuildDeviceArray(List<JObject> deviceItems, JObject dexLead)
        {
            var devices = new JArray();

            foreach (JObject deviceItem in deviceItems)
            {
                devices.Add(BuildDeviceObject(deviceItem));
            }

            /*
                Fallback for old/mock responses where device data may already be flat
                at root level instead of inside items[].
            */
            if (devices.Count == 0 && HasAnyRootDeviceField(dexLead))
            {
                devices.Add(new JObject
                {
                    ["sku"] = JValue.CreateNull(),
                    ["identifier"] = ToJToken(GetString(GetPropertyIgnoreCase(dexLead, "device_identifier"))),
                    ["name"] = ToJToken(GetString(GetPropertyIgnoreCase(dexLead, "device_name"))),
                    ["brand"] = ToJToken(GetString(GetPropertyIgnoreCase(dexLead, "device_brand"))),
                    ["model"] = ToJToken(GetString(GetPropertyIgnoreCase(dexLead, "device_model"))),
                    ["color"] = ToJToken(GetString(GetPropertyIgnoreCase(dexLead, "device_color"))),
                    ["offer_code"] = JValue.CreateNull(),
                    ["offer_name"] = JValue.CreateNull()
                });
            }

            return devices;
        }

        private static JObject BuildDeviceObject(JObject deviceItem)
        {
            JToken? attributes = GetPropertyIgnoreCase(deviceItem, "attributes");
            JToken? offer = GetPropertyIgnoreCase(deviceItem, "offer");

            return new JObject
            {
                ["sku"] = ToJToken(GetString(GetPropertyIgnoreCase(deviceItem, "sku"))),
                ["identifier"] = ToJToken(GetString(GetPropertyIgnoreCase(deviceItem, "identifier"))),
                ["name"] = ToJToken(GetString(GetPropertyIgnoreCase(deviceItem, "name"))),

                ["brand"] = ToJToken(GetString(GetPropertyIgnoreCase(attributes, "brand"))),
                ["model"] = ToJToken(GetString(GetPropertyIgnoreCase(attributes, "model"))),
                ["color"] = ToJToken(GetString(GetPropertyIgnoreCase(attributes, "color"))),

                /*
                    Device offer stays only inside each device object.
                    It does not populate root-level offer_name / offer_code.
                */
                ["offer_code"] = ToJToken(GetString(GetPropertyIgnoreCase(offer, "offer_code"))),
                ["offer_name"] = ToJToken(GetString(GetPropertyIgnoreCase(offer, "offer_details")))
            };
        }

        private static IEnumerable<JObject> GetItemsByType(JToken? itemsToken, string type)
        {
            if (itemsToken == null ||
                itemsToken.Type == JTokenType.Null ||
                itemsToken.Type == JTokenType.Undefined)
            {
                return Enumerable.Empty<JObject>();
            }

            if (itemsToken.Type == JTokenType.Array)
            {
                return itemsToken
                    .Children<JObject>()
                    .Where(x =>
                        string.Equals(
                            GetString(GetPropertyIgnoreCase(x, "type")),
                            type,
                            StringComparison.OrdinalIgnoreCase
                        )
                    );
            }

            if (itemsToken.Type == JTokenType.Object)
            {
                var singleItem = (JObject)itemsToken;

                string? itemType =
                    GetString(GetPropertyIgnoreCase(singleItem, "type"));

                if (string.Equals(itemType, type, StringComparison.OrdinalIgnoreCase))
                {
                    return new[] { singleItem };
                }
            }

            return Enumerable.Empty<JObject>();
        }

        private static bool HasAnyRootDeviceField(JObject dexLead)
        {
            return
                GetPropertyIgnoreCase(dexLead, "device_identifier") != null ||
                GetPropertyIgnoreCase(dexLead, "device_name") != null ||
                GetPropertyIgnoreCase(dexLead, "device_brand") != null ||
                GetPropertyIgnoreCase(dexLead, "device_model") != null ||
                GetPropertyIgnoreCase(dexLead, "device_color") != null;
        }

        private static bool IsLegacyFlatDeviceField(string propertyName)
        {
            return
                IsSame(propertyName, "device_name") ||
                IsSame(propertyName, "device_identifier") ||
                IsSame(propertyName, "device_color") ||
                IsSame(propertyName, "device_brand") ||
                IsSame(propertyName, "device_model") ||
                IsSame(propertyName, "device_imei");
        }

        private static JToken? GetPropertyIgnoreCase(JToken? token, string propertyName)
        {
            if (token == null || token.Type != JTokenType.Object)
            {
                return null;
            }

            var obj = (JObject)token;

            return obj.Properties()
                .FirstOrDefault(x => IsSame(x.Name, propertyName))
                ?.Value;
        }

        private static string? GetString(JToken? token)
        {
            if (token == null ||
                token.Type == JTokenType.Null ||
                token.Type == JTokenType.Undefined)
            {
                return null;
            }

            string value = token.ToString();

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            if (values == null)
            {
                return null;
            }

            return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        }

        private static void SetField(JObject target, string fieldName, string? value)
        {
            target[fieldName] = ToJToken(value);
        }

        private static JToken ToJToken(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? JValue.CreateNull()
                : new JValue(value);
        }

        private static bool IsSame(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}