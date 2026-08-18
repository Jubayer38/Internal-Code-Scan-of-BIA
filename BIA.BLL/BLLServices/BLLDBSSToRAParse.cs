using BIA.DAL.Repositories;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.ENUM;
using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using BIA.Entity.ViewModel;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Threading.Tasks;

namespace BIA.BLL.BLLServices
{
    public class BLLDBSSToRAParse
    {
        private readonly DALBiometricRepo _dataManager;
        private readonly BLLCommon _bllCommon;
        private readonly BLLDivDisThana _bLLDivDisThana;

        public BLLDBSSToRAParse(DALBiometricRepo dataManager, BLLCommon bllCommon, BLLDivDisThana bLLDivDisThana)
        {
            _dataManager = dataManager;
            _bllCommon = bllCommon;
            _bLLDivDisThana = bLLDivDisThana;
        }
        public RACommonResponse QCUpdateRespParsing(QCStatusResponseRootobject data)
        {
            int flag = 0;
            RACommonResponse response = new RACommonResponse();
            try
            {
                for (int i = 0; i < data.data.Count; i++)
                {
                    if (data.data[i].attributes != null)
                    {
                        if (data.data[i].attributes.status == "requested"
                            || data.data[i].attributes.status == "scheduled"
                            || data.data[i].attributes.status == "done"
                            || data.data[i].attributes.status == "new")
                        {
                            flag = 1;
                        }
                        else
                        {
                            flag = 0;
                            break;
                        }
                    }
                }

                if (flag == 1)
                {
                    response.result = true;
                    response.message = "Data updated successfully";
                }
                else
                {
                    response.result = false;
                    response.message = "Data update failed";
                }

            }
            catch (Exception ex)
            {
                response.result = false;
                response.message = ex.Message;
                throw;
            }
            return response;
        }

        public async Task<CherishedMSISDNCheckResponse> UnpairedMSISDNReqParsingV3(JObject dbssRespObj, string retailer_id, string channel_name)
        {
            CherishedMSISDNCheckResponse raResp = new CherishedMSISDNCheckResponse();

            try
            {
                string status = string.Empty;
                int stockId = 0;
                string number_category = string.Empty;
                string reserved_for = string.Empty;
                string cherish_category_config = string.Empty;

                var attributes = dbssRespObj["data"]?["attributes"];
                if (attributes != null &&
                    !string.IsNullOrEmpty((string?)attributes["status"]) &&
                    !string.IsNullOrEmpty((string?)attributes["stock"]))
                {
                    status = (string?)attributes["status"] ?? "";
                    stockId = (int?)attributes["stock"] ?? 0;
                    reserved_for = (string?)attributes["reserved-for"] ?? "";
                    number_category = (string?)attributes["number-category"] ?? "";
                }

                if (stockId == 33)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StockIDMismatch;
                    return raResp;
                }

                if (!string.IsNullOrEmpty(reserved_for))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNReserved;
                    return raResp;
                }

                if (status == "available")
                {
                    cherish_category_config = SettingsValues.GetCherishCategory();

                    var configValues = cherish_category_config.Contains(',') ? cherish_category_config.Split(',') : new string[] { cherish_category_config };

                    if (configValues.Any(x => x == number_category))
                    {
                        var category = configValues.FirstOrDefault(x => x.Equals(number_category));
                        if (category != null)
                        {
                            var catInfo = await _bllCommon.GetDesiredCategoryMessage(category, channel_name);
                            if (catInfo != null)
                            {
                                raResp.data_message = catInfo.message;
                                raResp.category_name = catInfo.name;
                                raResp.isDesiredCategory = true;
                                raResp.result = true;
                                raResp.message = MessageCollection.MSISDNValid;
                                raResp.category_name = number_category;
                            }
                            else
                            {
                                raResp.data_message = $"No amount is configured for {category} category";
                                raResp.category_name = category;
                                raResp.isDesiredCategory = false;
                                raResp.result = false;
                                raResp.message = raResp.data_message;
                                raResp.category_name = number_category;
                            }
                        }
                    }
                    else
                    {

                        raResp = ValidateCherishedNumerV2(dbssRespObj, retailer_id);
                        raResp.category_name = number_category;
                        raResp.isDesiredCategory = false;
                    }

                    raResp.stock_id = stockId;
                    return raResp;
                }
                else if (status == "in_use")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNInUse;
                    return raResp;
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "MSISDN is invalid.";
                    return raResp;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<CherishedMSISDNCheckResponse> UnpairedHomeWifiMSISDNReqParsing(JObject dbssRespObj, string retailer_id, string channel_name, string ext_channel)
        {
            CherishedMSISDNCheckResponse raResp = new CherishedMSISDNCheckResponse();

            try
            {
                string status = string.Empty;
                int stockId = 0;
                string number_category = string.Empty;
                string reserved_for = string.Empty;
                string cherish_category_config = string.Empty;

                var attributes = dbssRespObj["data"]?["attributes"];
                if (attributes != null &&
                    !string.IsNullOrEmpty((string?)attributes["status"]) &&
                    !string.IsNullOrEmpty((string?)attributes["stock"]))
                {
                    status = (string?)attributes["status"] ?? "";
                    stockId = (int?)attributes["stock"] ?? 0;
                    reserved_for = (string?)attributes["reserved-for"] ?? "";
                    number_category = (string?)attributes["number-category"] ?? "";
                }

                if (string.IsNullOrEmpty(reserved_for) && ext_channel.ToUpper().Contains("D2D"))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNNotReserved;
                    return raResp;
                }

                if (status == "available")
                {
                    cherish_category_config = SettingsValues.GetCherishCategory();

                    var configValues = cherish_category_config.Contains(',') ? cherish_category_config.Split(',') : new string[] { cherish_category_config };

                    if (configValues.Any(x => x == number_category))
                    {
                        var category = configValues.FirstOrDefault(x => x.Equals(number_category));
                        if (category != null)
                        {
                            var catInfo = await _bllCommon.GetDesiredCategoryMessage(category, channel_name);
                            if (catInfo != null)
                            {
                                raResp.data_message = catInfo.message;
                                raResp.category_name = catInfo.name;
                                raResp.isDesiredCategory = true;
                                raResp.result = true;
                                raResp.message = MessageCollection.MSISDNValid;
                                raResp.category_name = number_category;
                            }
                            else
                            {
                                raResp.data_message = $"No amount is configured for {category} category";
                                raResp.category_name = category;
                                raResp.isDesiredCategory = false;
                                raResp.result = false;
                                raResp.message = raResp.data_message;
                                raResp.category_name = number_category;
                            }
                        }
                    }
                    else
                    {

                        raResp = ValidateCherishedNumerV2(dbssRespObj, retailer_id);
                        raResp.category_name = number_category;
                        raResp.isDesiredCategory = false;
                    }

                    raResp.stock_id = stockId;
                    return raResp;
                }
                else if (status == "in_use")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNInUse;
                    return raResp;
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "MSISDN is invalid.";
                    return raResp;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public CherishedMSISDNCheckResponse ValidateCherishedNumerV2(JObject dbssRespObj, string retailer_id)
        {

            CherishedMSISDNCheckResponse raResp = new CherishedMSISDNCheckResponse();

            string status = String.Empty;
            string retailer_code = String.Empty;
            string number_category = String.Empty;
            string category_config = String.Empty;
            string[] cofigValue = Array.Empty<string>();

            try
            {
                if (dbssRespObj["data"] != null)
                {
                    if (dbssRespObj["data"]?["attributes"] != null)
                    {
                        category_config = SettingsValues.GetNumberCategory();

                        cofigValue = category_config.Contains(",") ? cofigValue = category_config.Split(',') : new string[] { category_config };

                        if (dbssRespObj["data"]?["attributes"]?["number-category"] != null)
                        {
                            retailer_code = dbssRespObj["data"]?["attributes"]?["salesman-id"]?.ToString() ?? "";
                            number_category = dbssRespObj["data"]?["attributes"]?["number-category"]?.ToString() ?? "";

                            if (!String.IsNullOrEmpty(retailer_code))
                            {
                                if (retailer_code.Length < 6)
                                {
                                    char pad = '0';
                                    retailer_code = retailer_code.PadLeft(6, pad);
                                }
                            }

                            if (!String.IsNullOrEmpty(retailer_code) && !String.IsNullOrEmpty(number_category) && cofigValue.Any(x => x != number_category)) // from Web.config 
                            {
                                if (retailer_id.Equals(retailer_code))
                                {
                                    raResp.result = true;
                                    raResp.message = MessageCollection.ValidCherishedNumber;
                                }
                                else
                                {
                                    raResp.result = false;
                                    raResp.message = MessageCollection.InvalidCherishedNumber;
                                }
                            }
                            else if (String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x == number_category))
                            {
                                raResp.result = true;
                                raResp.message = MessageCollection.ValidCherishedNumber;
                            }
                            else if (!String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x == number_category))
                            {
                                raResp.result = true;
                                raResp.message = MessageCollection.ValidCherishedNumber; ;
                            }
                            else if (String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x != number_category))
                            {
                                raResp.result = false;
                                raResp.message = "MSISDN not tagged with this Retailer (ID: " + retailer_id + ")";
                            }
                            else
                            {
                                raResp.result = false;
                                raResp.message = "MSISDN is not Valid.";
                            }
                        }
                        else
                        {
                            raResp.result = false;
                            raResp.message = "Invalid MSISDN Category!";
                        }
                    }
                    else
                    {
                        raResp.result = false;
                        raResp.message = "No Data found!";
                    }
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "No Data found!";
                }

                return raResp;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public RACommonResponse CustomerUpdateRespParsing(CustomerUpdateRespRootobject data)
        {
            int flag = 0;
            RACommonResponse response = new RACommonResponse();
            try
            {
                for (int i = 0; i < data.data.Count; i++)
                {
                    if (data.data[i].attributes != null)
                    {
                        if (data.data[i].attributes.status == "requested"
                            || data.data[i].attributes.status == "scheduled"
                            || data.data[i].attributes.status == "done"
                            || data.data[i].attributes.status == "new")
                        {
                            flag = 1;
                        }
                        else
                        {
                            flag = 0;
                            break;
                        }
                    }
                }

                if (flag == 1)
                {
                    response.result = true;
                    response.message = "Data updated successfully";
                }
                else
                {
                    response.result = false;
                    response.message = "Data update failed";
                }
            }
            catch (Exception ex)
            {
                response.result = false;
                response.message = ex.Message;
                throw;
            }
            return response;
        }

        public async Task<VMRejectedOrder> RejectionOrdersParsing(string qualityControlId, string customerId, RejectedOrdersAttributes rAattrib
                                                    , CustomerInfoResponseAttributes cIattrib
                                                    , CustomerAddressResponseAttributes cAattrib)
        {
            VMRejectedOrder ro = new VMRejectedOrder();

            try
            {
                ro.quality_control_id = qualityControlId;
                ro.customer_id = customerId;/*bLLCommon.GetTokenNo(rAattrib.msisdn);*///Need to know the business logic for selecting data from DB.
                ro.customer_name = cIattrib.firstname;
                ro.alt_msisdn = cIattrib.altcontactphone;

                var divisions = await Task.Run(() => _bLLDivDisThana.GetDivision());
                ro.division_id = divisions
                    .Where(a => string.Equals(a.DIVISIONNAME, cAattrib.postaldistrict, StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.DIVISIONID)
                    .FirstOrDefault();
                ro.division_name = cAattrib.postaldistrict;

                var districts = await Task.Run(() => _bLLDivDisThana.GetDistrict());
                ro.district_id = districts
                    .Where(a => string.Equals(a.DISTRICTNAME, cAattrib.city, StringComparison.OrdinalIgnoreCase) && a.DIVISIONID == ro.division_id)
                    .Select(a => a.DISTRICTID)
                    .FirstOrDefault();
                ro.district_name = cAattrib.city;

                var thanas = await Task.Run(() => _bLLDivDisThana.GetThana());
                ro.thana_id = thanas
                    .Where(a => string.Equals(a.THANANAME, cAattrib.province, StringComparison.OrdinalIgnoreCase)
                        && a.DISTRICTID == ro.district_id)
                    .Select(a => a.THANAID)
                    .FirstOrDefault();
                ro.thana_name = cAattrib.province;

                ro.email = cIattrib.email;
                ro.flat_number = cAattrib.street;
                ro.gender = cIattrib.gender;
                ro.house_number = cAattrib.building;
                ro.is_over_due = 0;//This must be configarable from webConfig.
                ro.mobile_number = rAattrib.msisdn;
                ro.postal_code = cAattrib.postalcode;
                //We get GMT time from DBSS, thus we need to add 6 hours with the GMT time to show the BD time to end user.
                ro.rejection_date = rAattrib.lastmodified.AddHours(6).ToString();
                ro.reject_reason = rAattrib.reason;
                ro.road_number = cAattrib.road;
                ro.village = cAattrib.area;
            }
            catch (Exception)
            {
                throw;
            }

            return ro;
        }


        public RACommonResponse SIMValidationParsing(JObject dbssResp)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null)
                {
                    response.result = false;
                    response.message = "Data filed empty.";
                }
                var status = dbssResp?["data"]?["status"]?.ToString();
                response.result = status == "success" ? true : false;
                response.message = "SIM is valid.";

                return response;
            }
            catch (Exception)
            {
                //response.result = false;
                //response.message = ex.Message;
                throw;
            }
        }


        public RACommonResponse SIMValidationParsing2(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null)
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (string.Equals(dbssResp?["data"]?["status"]?.ToString(), "failed", StringComparison.OrdinalIgnoreCase))
                {
                    response.result = false;

                    var errorMessage = dbssResp?["data"]?["error_message"]?.ToString();
                    response.message = !string.IsNullOrWhiteSpace(errorMessage)
                                        ? errorMessage
                                        : MessageCollection.SIMIsNotInInventory;

                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Prepaid
                    && isPired == true)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PairedMSISDN.ToLower()/*"paired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower() /*"prepaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PairedMSISDN.ToLower() /*"paired"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPairedSIM;
                        return response;
                    }
                    else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePrepaid.ToLower() /*"prepaid"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPrepaidSIM;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Postpaid
                    && isPired == true)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PairedMSISDN.ToLower()/*"paired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower() /*"postpaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PairedMSISDN.ToLower()/*"paired"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPairedSIM;
                        return response;
                    }
                    else if (dbssResp["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePostpaid.ToLower() /*"postpaid"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPostpaidSIM;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Prepaid
                    && isPired == false)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower() /*"prepaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAnUnpairedSIM;
                        return response;
                    }
                    else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePrepaid.ToLower()/*"prepaid"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPrepaidSIM;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Postpaid
                    && isPired == false)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower()/*"postpaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAnUnpairedSIM;
                        return response;
                    }
                    else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePostpaid.ToLower()/*"postpaid"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPostpaidSIM;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                //----------------SIMReplacement--------------
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.SIMReplacement
                    && !String.IsNullOrEmpty(oldSimType))
                {
                    if (oldSimType.ToLower() == FixedValueCollection.SIMTypeUSIM /*"usim"*/)
                    {
                        if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLower() /*"sim_swap"*/)
                        {
                            response.result = true;
                            response.message = MessageCollection.SIMValid;
                            return response;
                        }
                        else
                        {
                            response.result = false;
                            response.message = MessageCollection.NotASwapSIM;
                            return response;
                        }
                    }
                    else if (oldSimType.ToLower() == FixedValueCollection.SIMTypeSIM/*"sim"*/)
                    {
                        if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeEV_SWAP.ToLower() /*"ev_swap"*/)
                        {
                            response.result = true;
                            response.message = MessageCollection.SIMValid;
                            return response;
                        }
                        else
                        {
                            response.result = false;
                            response.message = MessageCollection.NotAEVSwapSIM;
                            return response;
                        }
                    }
                    //============New SIM Type "PLI" Added=======
                    else if (oldSimType.ToLower() == FixedValueCollection.SIMTypePLI/*"pli"*/)
                    {
                        if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLower() /*"ev_swap"*/)
                        {
                            response.result = true;
                            response.message = MessageCollection.SIMValid;
                            return response;
                        }
                        else
                        {
                            response.result = false;
                            response.message = MessageCollection.NotASwapSIM;
                            return response;
                        }
                    }
                    //==========x============
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMTypeIsNotSIMOrUSIM;
                        return response;
                    }
                }
                else
                {
                    response.result = false;
                    response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public RACommonResponse SIMValidationParsing3(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null)
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower() /*e-sim*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower() /*e_sim_swap*/)
                {
                    {
                        response.result = false;
                        response.message = "This is not Physical SIM.";
                        return response;
                    }
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaidStarTrek.ToLower() /*ryz-prepaid*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESimStarTrek.ToLower() /*ryz-esim*/)
                {
                    {
                        response.result = false;
                        response.message = "Please try with correct SIM card";
                        return response;
                    }
                }
                else if (string.Equals(dbssResp?["data"]?["status"]?.ToString(), "failed", StringComparison.OrdinalIgnoreCase))
                {
                    response.result = false;

                    var errorMessage = dbssResp?["data"]?["error_message"]?.ToString();
                    response.message = !string.IsNullOrWhiteSpace(errorMessage)
                                        ? errorMessage
                                        : MessageCollection.SIMIsNotInInventory;
                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }

                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Prepaid
                    && isPired == true)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PairedMSISDN.ToLower()/*"paired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower() /*"prepaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PairedMSISDN.ToLower() /*"paired"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPairedSIM;
                        return response;
                    }
                    else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePrepaid.ToLower() /*"prepaid"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPrepaidSIM;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }

                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Postpaid
                    && isPired == true)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PairedMSISDN.ToLower()/*"paired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower() /*"postpaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PairedMSISDN.ToLower()/*"paired"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPairedSIM;
                        return response;
                    }
                    else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePostpaid.ToLower() /*"postpaid"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPostpaidSIM;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }

                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Prepaid
                    && isPired == false)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower() /*"prepaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePrepaid.ToLower() /*"prepaid"*/)
                    {
                        response.result = false;
                        response.message = "Please try with correct SIM card";
                        return response;
                    }
                    else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAnUnpairedSIM;
                        return response;
                    }
                    else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePrepaid.ToLower()/*"prepaid"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPrepaidSIM;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }

                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Postpaid
                    && isPired == false)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower()/*"postpaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePostpaid.ToLower()/*"postpaid"*/)
                    {
                        response.result = false;
                        response.message = "Please try with correct SIM card";
                        return response;
                    }
                    else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAnUnpairedSIM;
                        return response;
                    }
                    else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePostpaid.ToLower()/*"postpaid"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPostpaidSIM;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                //----------------SIMReplacement--------------
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.SIMReplacement
                    && !String.IsNullOrEmpty(oldSimType))
                {
                    if (oldSimType.ToLower() == FixedValueCollection.SIMTypeUSIM /*"usim"*/)
                    {
                        if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLower() /*"sim_swap"*/)
                        {
                            response.result = true;
                            response.message = MessageCollection.SIMValid;
                            return response;
                        }
                        else
                        {
                            response.result = false;
                            response.message = MessageCollection.NotASwapSIM;
                            return response;
                        }
                    }
                    else if (oldSimType.ToLower() == FixedValueCollection.SIMTypeSIM/*"sim"*/)
                    {
                        if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeEV_SWAP.ToLower() /*"ev_swap"*/)
                        {
                            response.result = true;
                            response.message = MessageCollection.SIMValid;
                            return response;
                        }
                        else
                        {
                            response.result = false;
                            response.message = MessageCollection.NotAEVSwapSIM;
                            return response;
                        }
                    }
                    //============New SIM Type "PLI" Added=======
                    else if (oldSimType.ToLower() == FixedValueCollection.SIMTypePLI/*"pli"*/)
                    {
                        if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLower() /*"ev_swap"*/)
                        {
                            response.result = true;
                            response.message = MessageCollection.SIMValid;
                            return response;
                        }
                        else
                        {
                            response.result = false;
                            response.message = MessageCollection.NotASwapSIM;
                            return response;
                        }
                    }
                    //==========x============
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMTypeIsNotSIMOrUSIM;
                        return response;
                    }
                }
                else
                {
                    response.result = false;
                    response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public RACommonResponse SIMValidationHomeWifiParsing(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType, string sim_type, string storage_type)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null)
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaidStarTrek.ToLower() /*ryz-prepaid*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESimStarTrek.ToLower() /*ryz-esim*/)
                {
                    {
                        response.result = false;
                        response.message = "Please try with correct SIM card";
                        return response;
                    }
                }
                else if (string.Equals(dbssResp?["data"]?["status"]?.ToString(), "failed", StringComparison.OrdinalIgnoreCase))
                {
                    response.result = false;

                    var errorMessage = dbssResp?["data"]?["error_message"]?.ToString();
                    response.message = !string.IsNullOrWhiteSpace(errorMessage)
                                        ? errorMessage
                                        : MessageCollection.SIMIsNotInInventory;
                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }
                //----------------SIMReplacement--------------
                else if (sim_type.ToUpper().Equals(EnumSimType.Prepaid.GetValue()) && storage_type.ToUpper().Equals(EnumSimStorageType.Physical.GetValue()))
                {
                    if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLower() /*"sim_swap"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.NotASwapSIM;
                        return response;
                    }
                }
                else if (sim_type.ToUpper().Equals(EnumSimType.Postpaid.GetValue()) && storage_type.ToUpper().Equals(EnumSimStorageType.Physical.GetValue()))
                {
                    if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLower() /*"sim_swap"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.NotASwapSIM;
                        return response;
                    }
                }
                else if (sim_type.ToUpper().Equals(EnumSimType.Prepaid.GetValue()) && storage_type.ToUpper().Equals(EnumSimStorageType.Esim.GetValue()))
                {
                    if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower() /*"e_sim_swap"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.NotASwapSIM;
                        return response;
                    }
                }
                else if (sim_type.ToUpper().Equals(EnumSimType.Postpaid.GetValue()) && storage_type.ToUpper().Equals(EnumSimStorageType.Esim.GetValue()))
                {
                    if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower() /*"e_sim_swap"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.NotASwapSIM;
                        return response;
                    }
                }
                else
                {
                    response.result = false;
                    response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public RACommonResponse SIMValidationParsingMNPPortIn(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null)
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (string.Equals(dbssResp?["data"]?["status"]?.ToString(), "failed", StringComparison.OrdinalIgnoreCase))
                {
                    response.result = false;

                    var errorMessage = dbssResp?["data"]?["error_message"]?.ToString();
                    response.message = !string.IsNullOrWhiteSpace(errorMessage)
                                        ? errorMessage
                                        : MessageCollection.SIMIsNotInInventory;
                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Prepaid
                    && isPired == false)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower() /*"prepaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Postpaid
                    && isPired == false)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower()/*"postpaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                else
                {
                    response.result = false;
                    response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        public RACommonResponse DuplicateDialSIMValidationParsing(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null)
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (string.Equals(dbssResp?["data"]?["status"]?.ToString(), "failed", StringComparison.OrdinalIgnoreCase))
                {
                    response.result = false;

                    var errorMessage = dbssResp?["data"]?["error_message"]?.ToString();
                    response.message = !string.IsNullOrWhiteSpace(errorMessage)
                                        ? errorMessage
                                        : MessageCollection.SIMIsNotInInventory;
                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Prepaid
                    && isPired == false)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower() /*"prepaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Postpaid
                    && isPired == false)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower() /*"postpaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                else
                {
                    response.result = false;
                    response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public RACommonResponse DuplicateDialHomeWifiSIMValidationParsing(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType, string sim_type, string storage_type)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null)
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (string.Equals(dbssResp?["data"]?["status"]?.ToString(), "failed", StringComparison.OrdinalIgnoreCase))
                {
                    response.result = false;

                    var errorMessage = dbssResp?["data"]?["error_message"]?.ToString();
                    response.message = !string.IsNullOrWhiteSpace(errorMessage)
                                        ? errorMessage
                                        : MessageCollection.SIMIsNotInInventory;
                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }
                else if (sim_type.ToUpper().Equals(EnumSimType.Prepaid.GetValue()) && storage_type.ToUpper().Equals(EnumSimStorageType.Physical.GetValue()))
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower() /*"prepaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                else if (sim_type.ToUpper().Equals(EnumSimType.Postpaid.GetValue()) && storage_type.ToUpper().Equals(EnumSimStorageType.Physical.GetValue()))
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower() /*"postpaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                else if (sim_type.ToUpper().Equals(EnumSimType.Prepaid.GetValue()) && storage_type.ToUpper().Equals(EnumSimStorageType.Esim.GetValue()))
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower() /*"e-sim"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                else if (sim_type.ToUpper().Equals(EnumSimType.Postpaid.GetValue()) && storage_type.ToUpper().Equals(EnumSimStorageType.Esim.GetValue()))
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower() /*"e-sim"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                else
                {
                    response.result = false;
                    response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        public RACommonResponse SIMValidationParsing4(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null)
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower()/*prepaid*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower() /*postpaid*/)
                {
                    response.result = false;
                    response.message = "This is not eSIM";
                    return response;
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaidStarTrek.ToLower()/*ryz-prepaid*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESimStarTrek.ToLower() /*ryz-esim*/)
                {
                    response.result = false;
                    response.message = "Please try with correct SIM card";
                    return response;
                }
                else if (string.Equals(dbssResp?["data"]?["status"]?.ToString(), "failed", StringComparison.OrdinalIgnoreCase))
                {
                    response.result = false;

                    var errorMessage = dbssResp?["data"]?["error_message"]?.ToString();
                    response.message = !string.IsNullOrWhiteSpace(errorMessage)
                                        ? errorMessage
                                        : MessageCollection.SIMIsNotInInventory;
                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection && isPired == false)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                        && (dbssResp["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower() /*"eSim"*/
                        || dbssResp["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower() /*"e_sim_swap"*/))
                    {
                        if (dbssResp["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                         && dbssResp["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower() /*"eSim"*/)
                        {
                            response.result = true;
                            response.message = MessageCollection.SIMValid;
                            return response;
                        }
                        else if (dbssResp["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                        && dbssResp["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower() /*"e_sim_swap"*/)
                        {
                            response.result = false;
                            response.message = MessageCollection.SIM_Not_Match;
                            return response;
                        }
                        else
                        {
                            response.result = false;
                            response.message = MessageCollection.SIMInvalid;
                            return response;
                        }
                    }
                    response.result = false;
                    response.message = "This is not eSIM.";
                    return response;
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection && isPired == true)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PairedMSISDN.ToLower() /*"paired"*/
                        && (dbssResp["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower() /*"e-sim"*/
                        || dbssResp["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower() /*"e_sim_swap"*/))
                    {
                        if (dbssResp["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PairedMSISDN.ToLower() /*"paired"*/
                         && dbssResp["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower() /*"e-sim"*/)
                        {
                            response.result = true;
                            response.message = MessageCollection.SIMValid;
                            return response;
                        }
                        else if (dbssResp["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PairedMSISDN.ToLower() /*"paired"*/
                        && dbssResp["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower() /*"e_sim_swap"*/)
                        {
                            response.result = false;
                            response.message = MessageCollection.SIM_Not_Match;
                            return response;
                        }
                        else
                        {
                            response.result = false;
                            response.message = MessageCollection.SIMInvalid;
                            return response;
                        }
                    }
                    response.result = false;
                    response.message = "This is not eSIM.";
                    return response;
                }
                //----------------SIMReplacement--------------
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.SIMReplacement && !String.IsNullOrEmpty(oldSimType))
                {
                    if (oldSimType.ToLower() == FixedValueCollection.SIMTypeUSIM /*"usim"*/ || oldSimType.ToLower() == FixedValueCollection.SIMTypePLI/*"pli"*/)
                    {
                        if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower() /*"e_sim_swap"*/
                            || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower()/*e-sim*/)
                        {
                            if (dbssResp["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower())
                            {
                                response.result = true;
                                response.message = MessageCollection.SIMValid;
                                return response;
                            }
                            else if (dbssResp["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower())
                            {
                                response.result = false;
                                response.message = MessageCollection.SIM_Not_Match;
                                return response;
                            }
                            else
                            {
                                response.result = false;
                                response.message = MessageCollection.SIMInvalid;
                                return response;
                            }
                        }
                        else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLower())
                        {
                            response.result = false;
                            response.message = "This is not eSIM.";
                            return response;
                        }
                    }
                    response.result = false;
                    response.message = "Old SIM type should be USIM or PLI.";
                    return response;
                }
                else
                {
                    response.result = false;
                    response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public RACommonResponse SIMValidationParsingDuplicateDialESIM(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null)
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (string.Equals(dbssResp?["data"]?["status"]?.ToString(), "failed", StringComparison.OrdinalIgnoreCase))
                {
                    response.result = false;

                    var errorMessage = dbssResp?["data"]?["error_message"]?.ToString();
                    response.message = !string.IsNullOrWhiteSpace(errorMessage)
                                        ? errorMessage
                                        : MessageCollection.SIMIsNotInInventory;
                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection && isPired == false)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                     && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower() /*"eSim"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                else
                {
                    response.result = false;
                    response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public RACommonResponse SIMValidationParsingMNPESIM(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null)
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (string.Equals(dbssResp?["data"]?["status"]?.ToString(), "failed", StringComparison.OrdinalIgnoreCase))
                {
                    response.result = false;

                    var errorMessage = dbssResp?["data"]?["error_message"]?.ToString();
                    response.message = !string.IsNullOrWhiteSpace(errorMessage)
                                        ? errorMessage
                                        : MessageCollection.SIMIsNotInInventory;
                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection && isPired == false)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                     && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower() /*"eSim"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                else
                {
                    response.result = false;
                    response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public VMBioCancelMSISDNValidationReqParsing BioCancelMSISDNValidationReqParsing(JObject dbssRespObj)
        {
            var raResp = new VMBioCancelMSISDNValidationReqParsing();

            try
            {
                var dataArray = dbssRespObj?["data"] as JArray;
                var includedArray = dbssRespObj?["included"] as JArray;

                if (dataArray?.Count > 0 && includedArray?.Count > 1)
                {
                    var dataObj = dataArray.FirstOrDefault() as JObject;
                    var includedObj = includedArray.FirstOrDefault() as JObject;

                    var dataAttrbObj = dataObj?["attributes"] as JObject;
                    var includeAttrbObj = includedObj?["attributes"] as JObject;

                    if (dataObj != null && dataAttrbObj != null && includedObj != null && includeAttrbObj != null)
                    {
                        var subscriptionIdStr = dataObj["id"]?.ToString();
                        if (string.IsNullOrEmpty(subscriptionIdStr))
                        {
                            raResp.result = false;
                            raResp.message = "Subscription ID field empty!";
                            return raResp;
                        }

                        var docType = includeAttrbObj["id-document-type"]?.ToString();
                        if (string.IsNullOrEmpty(docType))
                        {
                            raResp.result = false;
                            raResp.message = MessageCollection.DataNotFound;
                            return raResp;
                        }

                        if (!string.Equals(docType, "national_id", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(docType, "smart_national_id", StringComparison.OrdinalIgnoreCase))
                        {
                            raResp.result = false;
                            raResp.message = "Customer is not registered with National ID!";
                            return raResp;
                        }

                        var paymentType = dataAttrbObj["payment-type"]?.ToString();
                        if (string.IsNullOrEmpty(paymentType))
                        {
                            raResp.result = false;
                            raResp.message = "payment-type field empty!";
                            return raResp;
                        }

                        var status = dataAttrbObj["status"]?.ToString();
                        if (!string.IsNullOrEmpty(status) && status == "active")
                        {
                            raResp.dob = includeAttrbObj["date-of-birth"]?.ToString();
                            raResp.nid = includeAttrbObj["id-document-number"]?.ToString();
                            raResp.result = true;
                            raResp.message = MessageCollection.Success;

                            raResp.subscription_id = long.TryParse(subscriptionIdStr, out var subId) ? subId : 0;

                            raResp.dest_sim_category = string.Equals(paymentType, "prepaid", StringComparison.OrdinalIgnoreCase)
                                ? (int)EnumSimCategory.Prepaid
                                : (int)EnumSimCategory.Postpaid;
                        }
                        else
                        {
                            raResp.result = false;
                            raResp.message = "MSISDN is not active.";
                        }
                    }
                    else
                    {
                        raResp.result = false;
                        raResp.message = "Data not found.";
                    }
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "Data not found.";
                }

                return raResp;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// in Include attribute for:- 
        ///                index 0: owner-customer
        ///                index 1: sim-cards
        ///                index 2: user-customer
        /// </summary>
        /// <param name="dbssRespObj"></param>
        /// <returns></returns>
        /// 
        public IndividualSIMReplacementMSISDNCheckResponse IndividualSIMReplacementMSISDNReqParsingV3(JObject dbssRespObj)
        {
            var raResp = new IndividualSIMReplacementMSISDNCheckResponse();

            try
            {
                var data = dbssRespObj["data"] as JObject;
                var dataAttributes = data?["attributes"] as JObject;
                var included = dbssRespObj["included"] as JArray;

                if (data == null || dataAttributes == null || data.Count == 0)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.SIMReplNoDataFound;
                    return raResp;
                }

                var status = dataAttributes["status"]?.ToString();
                if (string.IsNullOrEmpty(status))
                {
                    raResp.result = false;
                    raResp.message = "Msisdn status not found!";
                    return raResp;
                }

                if (status == "terminated")
                {
                    raResp.result = false;
                    raResp.message = "Msisdn is not valid for SIM replacemnt!";
                    return raResp;
                }

                if (status != "active" && status != "idle")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNStatusNotActiveOrIdle;
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                if (included == null || (included.Count != 2 && included.Count != 3))
                {
                    raResp.result = false;
                    raResp.message = "Data not found in include field!";
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                var subIdStr = data["id"]?.ToString();
                if (string.IsNullOrEmpty(subIdStr) || !int.TryParse(subIdStr, out int subId))
                {
                    raResp.result = false;
                    raResp.message = "Subscription ID field empty!";
                    return raResp;
                }

                var included0Attr = included[0]?["attributes"] as JObject;
                var included1Attr = included[1]?["attributes"] as JObject;

                if (included0Attr == null || included1Attr == null)
                {
                    raResp.result = false;
                    raResp.message = "Data not found in include field!";
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                var icc = included1Attr["icc"]?.ToString();
                if (string.IsNullOrEmpty(icc))
                {
                    raResp.result = false;
                    raResp.message = "Old SIM number not found!";
                    return raResp;
                }

                var simType = included1Attr["sim-type"]?.ToString();
                if (string.IsNullOrEmpty(simType))
                {
                    raResp.result = false;
                    raResp.message = "sim-type not found!";
                    return raResp;
                }

                var isCompany = included0Attr["is-company"]?.ToObject<bool?>();
                if (isCompany == null)
                {
                    raResp.result = false;
                    raResp.message = "Company information not found!";
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                var idDocType = included0Attr["id-document-type"]?.ToString();
                if (string.IsNullOrEmpty(idDocType))
                {
                    raResp.result = false;
                    raResp.message = "id-document-type not found!";
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                if (idDocType != "national_id" && idDocType != "smart_national_id")
                {
                    raResp.result = false;
                    raResp.message = "Customer is not registered with National ID!";
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                if (isCompany == true)
                {
                    raResp.result = false;
                    raResp.message = "This MSISDN is not eligible for individual SIM replacement.";
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                // Success response
                raResp.saf_status = true;
                raResp.customer_id = string.Empty;
                raResp.dob = included0Attr["date-of-birth"]?.ToString();
                raResp.doc_id_number = included0Attr["id-document-number"]?.ToString();
                raResp.dbss_subscription_id = subId;
                raResp.old_sim_number = icc;
                raResp.old_sim_type = simType;
                raResp.result = true;
                raResp.message = MessageCollection.MSISDNValid;
                return raResp;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public IndividualSIMReplacementMSISDNCheckResponse HomeWifiSIMReplacementMSISDNReqParsing(JObject dbssRespObj)
        {
            var raResp = new IndividualSIMReplacementMSISDNCheckResponse();

            try
            {
                var data = dbssRespObj["data"] as JObject;
                var dataAttributes = data?["attributes"] as JObject;
                var included = dbssRespObj["included"] as JArray;

                if (data == null || dataAttributes == null || data.Count == 0)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.SIMReplNoDataFound;
                    return raResp;
                }

                var status = dataAttributes["status"]?.ToString();
                if (string.IsNullOrEmpty(status))
                {
                    raResp.result = false;
                    raResp.message = "Msisdn status not found!";
                    return raResp;
                }

                if (status == "terminated")
                {
                    raResp.result = false;
                    raResp.message = "Msisdn is not valid for SIM replacemnt!";
                    return raResp;
                }

                if (status != "active" && status != "idle")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNStatusNotActiveOrIdle;
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                if (included == null || (included.Count != 2 && included.Count != 3))
                {
                    raResp.result = false;
                    raResp.message = "Data not found in include field!";
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                var subIdStr = data["id"]?.ToString();
                if (string.IsNullOrEmpty(subIdStr) || !int.TryParse(subIdStr, out int subId))
                {
                    raResp.result = false;
                    raResp.message = "Subscription ID field empty!";
                    return raResp;
                }

                var included0Attr = included[0]?["attributes"] as JObject;
                var included1Attr = included[1]?["attributes"] as JObject;

                if (included0Attr == null || included1Attr == null)
                {
                    raResp.result = false;
                    raResp.message = "Data not found in include field!";
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                var icc = included1Attr["icc"]?.ToString();
                if (string.IsNullOrEmpty(icc))
                {
                    raResp.result = false;
                    raResp.message = "Old SIM number not found!";
                    return raResp;
                }

                var simType = included1Attr["sim-type"]?.ToString();
                if (string.IsNullOrEmpty(simType))
                {
                    raResp.result = false;
                    raResp.message = "sim-type not found!";
                    return raResp;
                }

                var isCompany = included0Attr["is-company"]?.ToObject<bool?>();
                if (isCompany == null)
                {
                    raResp.result = false;
                    raResp.message = "Company information not found!";
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                var idDocType = included0Attr["id-document-type"]?.ToString();
                if (string.IsNullOrEmpty(idDocType))
                {
                    raResp.result = false;
                    raResp.message = "id-document-type not found!";
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                if (idDocType != "national_id" && idDocType != "smart_national_id")
                {
                    raResp.result = false;
                    raResp.message = "Customer is not registered with National ID!";
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                if (isCompany == true)
                {
                    raResp.result = false;
                    raResp.message = "This MSISDN is not eligible for individual SIM replacement.";
                    raResp.dob = "";
                    raResp.doc_id_number = "";
                    raResp.saf_status = false;
                    return raResp;
                }

                // Success response
                raResp.saf_status = true;
                raResp.customer_id = string.Empty;
                raResp.dob = included0Attr["date-of-birth"]?.ToString();
                raResp.doc_id_number = included0Attr["id-document-number"]?.ToString();
                raResp.dbss_subscription_id = subId;
                raResp.old_sim_number = icc;
                raResp.old_sim_type = simType;
                raResp.result = true;
                raResp.message = MessageCollection.MSISDNValid;
                return raResp;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public RACommonResponse UnpairedMSISDNReqParsingForMNPProtIn(JObject dbssRespObj)
        {
            RACommonResponse raResp = new RACommonResponse();
            try
            {
                var data = dbssRespObj["data"];
                if (data == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.NoDataFound;
                    return raResp;
                }

                var attributes = data["attributes"];
                if (attributes == null)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.DataNotFound;
                    return raResp;
                }

                var isControlledToken = attributes["is-controlled"];
                if (isControlledToken == null || string.IsNullOrEmpty(isControlledToken.ToString()))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.DataNotFound;
                    return raResp;
                }

                bool isControlled = false;
                bool.TryParse(isControlledToken.ToString(), out isControlled);

                if (isControlled)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNAlreadyExists;
                }
                else
                {
                    raResp.result = true;
                    raResp.message = MessageCollection.Success;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return raResp;
        }

        //public RACommonResponse UnpairedMSISDNReqParsingForMNPProtIn(JObject dbssRespObj)
        //{
        //    RACommonResponse raResp = new RACommonResponse();
        //    try
        //    {
        //        if (dbssRespObj["data"] == null)
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.NoDataFound;
        //            return raResp;
        //        }

        //        if (dbssRespObj?["data"]?["attributes"] == null)
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.DataNotFound;
        //            return raResp;
        //        }

        //        if (String.IsNullOrEmpty(dbssRespObj?["data"]?["attributes"]?["is-controlled"]?.ToString()))
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.DataNotFound;
        //        }

        //        if (Convert.ToBoolean(dbssRespObj?["data"]?["attributes"]?["is-controlled"]) == true)
        //        {
        //            raResp.result = false;
        //            raResp.message = MessageCollection.MSISDNAlreadyExists;
        //        }
        //        else
        //        {
        //            raResp.result = true;
        //            raResp.message = MessageCollection.Success;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //    return raResp;
        //}

        public MSISDNCheckResponse PreToPostMSISDNReqParsing(JObject dbssRespObj)
        {
            MSISDNCheckResponse raResp = new MSISDNCheckResponse();

            void SetError(string message)
            {
                raResp.result = false;
                raResp.message = message;
                raResp.dob = string.Empty;
                raResp.nid = string.Empty;
                raResp.saf_status = false;
            }

            try
            {
                var data = dbssRespObj["data"];
                if (data == null || !data.HasValues || data.Count() <= 0)
                {
                    SetError(MessageCollection.SIMReplNoDataFound);
                    return raResp;
                }

                string? status = (string?)data["attributes"]?["status"];
                if (string.IsNullOrEmpty(status))
                {
                    SetError("Msisdn status not found!");
                    return raResp;
                }

                if (status == "terminated")
                {
                    SetError("Msisdn is not valid for prepaid to postpaid!");
                    return raResp;
                }

                if (status != "active" && status != "idle")
                {
                    SetError(MessageCollection.MSISDNStatusNotActiveOrIdle);
                    return raResp;
                }

                string? paymentType = (string?)data["attributes"]?["payment-type"];
                if (paymentType == "postpaid")
                {
                    SetError(MessageCollection.PreToPostMigrationFailedMessage);
                    return raResp;
                }

                var included = dbssRespObj["included"];
                if (included == null || !included.HasValues || included.Count() < 2)
                {
                    SetError("Data not found in include field!");
                    return raResp;
                }

                string? subscriptionIdStr = (string?)data["id"];
                if (string.IsNullOrEmpty(subscriptionIdStr))
                {
                    SetError("Subscription ID field empty!");
                    return raResp;
                }

                string? userCustomerId = (string?)data["relationships"]?["user-customer"]?["data"]?["id"];
                string? ownerCustomerId = (string?)data["relationships"]?["owner-customer"]?["data"]?["id"];

                if (userCustomerId == null)
                    throw new Exception("Data not found in user-customer!");
                if (ownerCustomerId == null)
                    throw new Exception("Data not found in owner-customer!");

                string[] dedicatedArr = SettingsValues.Getdedicated_Ac_Id()
                                                       .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                int totalData = included.Count();

                for (int i = 0; i < totalData; i++)
                {
                    var attributes = included[i]?["attributes"];
                    if (attributes == null) continue;

                    string? dedicatedAccountId = (string?)attributes["dedicated-account-id"];
                    string? amountString = (string?)attributes["amount"];
                    if (!string.IsNullOrEmpty(dedicatedAccountId) &&
                        !string.IsNullOrEmpty(amountString) &&
                        decimal.TryParse(amountString, out var amount))
                    {
                        raResp.dedicated_Ac_Id = dedicatedAccountId;
                        raResp.amount = amount;

                        if (dedicatedArr.Any(x => x.Equals(dedicatedAccountId)) && amount > 0)
                        {
                            raResp.result = false;
                            raResp.message = $"Customer is due with loan amount: {amount} Tk";
                            return raResp;
                        }
                    }

                    string? includedId = (string?)included[i]?["id"];
                    string? idDocType = (string?)attributes["id-document-type"];

                    if (ownerCustomerId == includedId)
                    {
                        if (idDocType != "national_id" && idDocType != "smart_national_id")
                        {
                            SetError("Customer is not registered with NID or Smart NID!");
                            return raResp;
                        }
                    }

                    if (userCustomerId == includedId)
                    {
                        string? firstName = (string?)attributes["first-name"];
                        raResp.saf_status = !string.IsNullOrEmpty(firstName);
                        raResp.customer_id = userCustomerId;
                    }

                    if (ownerCustomerId == includedId)
                    {
                        raResp.dob = (string?)attributes["date-of-birth"] ?? "";
                        raResp.nid = (string?)attributes["id-document-number"] ?? "";

                        if (string.IsNullOrEmpty(raResp.dob) || string.IsNullOrEmpty(raResp.nid))
                        {
                            SetError("date-of-birth or id-document-number is not exist!");
                            return raResp;
                        }
                    }
                }

                raResp.dbss_subscription_id = Convert.ToInt32(subscriptionIdStr);
                raResp.result = true;
                raResp.message = MessageCollection.MSISDNValid;
                return raResp;
            }
            catch
            {
                throw;
            }
        }
        internal IndividualSIMReplacementMSISDNCheckResponse PopulateDataForGeneralUserToGeneralUserSIMRepl(JObject dbssRespObj)
        {
            var raResp = new IndividualSIMReplacementMSISDNCheckResponse();

            void SetError(string message)
            {
                raResp.result = false;
                raResp.message = message;
                raResp.dob = string.Empty;
                raResp.doc_id_number = null;
                raResp.saf_status = false;
            }

            try
            {
                var included = dbssRespObj["included"];
                var dataArray = dbssRespObj["data"] as JArray;

                if (included?.Count() == 3 && included.HasValues && dataArray != null && dataArray.Count > 0)
                {
                    var data = dataArray[0];

                    if (data?["id"] == null)
                    {
                        SetError("Subscription ID field empty!");
                        return raResp;
                    }

                    if (data["attributes"] == null)
                    {
                        SetError("Data field empty!");
                        return raResp;
                    }

                    var oldSimAttributes = included[1]?["attributes"];
                    if (string.IsNullOrEmpty((string?)oldSimAttributes?["icc"]))
                    {
                        SetError("Old SIM number not found!");
                        return raResp;
                    }

                    if (string.IsNullOrEmpty((string?)oldSimAttributes?["sim-type"]))
                    {
                        SetError("sim-type not found!");
                        return raResp;
                    }

                    var mainAttributes = data["attributes"];
                    var included0Attrs = included[0]?["attributes"];

                    string? msisdnStatus = (string?)mainAttributes?["status"];
                    bool? isCompany = (bool?)included0Attrs?["is-company"];
                    string? idDocType = (string?)included0Attrs?["id-document-type"];

                    if (!string.IsNullOrEmpty(msisdnStatus) && isCompany != null && !string.IsNullOrEmpty(idDocType))
                    {
                        if (msisdnStatus != "active" && msisdnStatus != "idle")
                        {
                            SetError("This MSISDN is not in active status.");
                        }
                        else if (idDocType != "national_id" && idDocType != "smart_national_id")
                        {
                            SetError("Customer is not registered with National ID.");
                        }
                        else if (isCompany == true)
                        {
                            SetError("This MSISDN is not eligible for individual SIM replacement.");
                        }
                        else
                        {
                            string? firstName = (string?)included0Attrs?["first-name"];
                            string? docIdNumber = (string?)included0Attrs?["id-document-number"];
                            string? dob = (string?)included0Attrs?["date-of-birth"];
                            string? customerId = (string?)included[0]?["id"];
                            string? icc = (string?)oldSimAttributes?["icc"];
                            string? simType = (string?)oldSimAttributes?["sim-type"];
                            int subscriptionId = Convert.ToInt32((string?)data["id"]);

                            raResp.result = true;
                            raResp.message = MessageCollection.MSISDNValid;
                            raResp.saf_status = !string.IsNullOrEmpty(firstName);
                            raResp.customer_id = customerId ?? "";
                            raResp.dob = dob;
                            raResp.doc_id_number = docIdNumber;
                            raResp.dbss_subscription_id = subscriptionId;
                            raResp.old_sim_number = icc ?? "";
                            raResp.old_sim_type = simType ?? "";
                        }
                    }
                    else
                    {
                        SetError(MessageCollection.DataNotFound);
                    }
                }
                else
                {
                    SetError(MessageCollection.DataNotFound);
                }

                return raResp;
            }
            catch
            {
                throw;
            }
        }

        public SIMReplacementMSISDNCheckResponse CorporateSIMReplacementCustomerInfoReqParsing(CorporateSIMReplacemnetCustomerInfoRootobject dbssRespObj, string pocMsisdnNo)
        {
            SIMReplacementMSISDNCheckResponse raResp = new SIMReplacementMSISDNCheckResponse();
            ///SIMReplacementMSISDNCheckResponse raResp = null;
            try
            {
                if (dbssRespObj.data.attributes == null)
                {
                    return raResp = new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = "Customer info not found."
                    };
                }

                if (dbssRespObj.data.attributes.contactphone != null)
                {
                    if (pocMsisdnNo.Substring(0, 2) == "88")
                    {
                        if (!dbssRespObj.data.attributes.contactphone.Trim().Equals(pocMsisdnNo))
                        {
                            return raResp = new SIMReplacementMSISDNCheckResponse()
                            {
                                result = false,
                                message = "Child MSISDN does not belong to the POC."
                            };
                        }
                    }
                    else
                    {
                        if (!dbssRespObj.data.attributes.contactphone.Trim().Equals("88" + pocMsisdnNo))
                        {
                            return raResp = new SIMReplacementMSISDNCheckResponse()
                            {
                                result = false,
                                message = "Child MSISDN does not belong to the POC."
                            };
                        }
                    }

                }

                if (dbssRespObj.data.attributes.iddocumenttype != null
                    && !dbssRespObj.data.attributes.iddocumenttype.Contains("national_id"))
                {
                    return raResp = new SIMReplacementMSISDNCheckResponse()
                    {
                        result = false,
                        message = "Customer is not registired with National ID or Smart National ID."
                    };
                }
            }
            catch (Exception)
            {
                throw;
            }

            return raResp = new SIMReplacementMSISDNCheckResponse()
            {
                doc_id_number = dbssRespObj.data.attributes.iddocumentnumber,
                dob = dbssRespObj.data.attributes.dateofbirth,
                result = true,
                message = MessageCollection.MSISDNValid
            };
        }

        #region OTP Validation Res-Parsing
        /// <summary>
        /// OTP Res-Parsing
        /// </summary>
        /// <param name="dbssRespObj"></param>
        /// <returns></returns>
        public OTPResponse OTPRespParsing(DBSSOTPResponseRootobject dbssRespObj)
        {
            try
            {
                if (dbssRespObj.data == null)
                {
                    return new OTPResponse()
                    {
                        is_otp_valid = false,
                        result = false,
                        message = MessageCollection.InvalidOTP
                    };
                }
                return new OTPResponse()
                {
                    is_otp_valid = true,
                    result = true,
                    message = MessageCollection.ValidOTP
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public OTPResponseRev OTPRespParsingV2(DBSSOTPResponseRootobject dbssRespObj)
        {
            try
            {
                if (dbssRespObj.data == null)
                {
                    return new OTPResponseRev()
                    {
                        isError = true,
                        message = MessageCollection.InvalidOTP,
                        data = new OTPRespData()
                        {
                            is_otp_valid = false
                        }
                    };
                }
                return new OTPResponseRev()
                {
                    isError = false,
                    message = MessageCollection.ValidOTP,
                    data = new OTPRespData()
                    {
                        is_otp_valid = true
                    }
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion


        public CorporateSIMReplacementCheckResponseWithCustomerId CorporateSIMReplacementMSISDNReqParsing(CorporateSIMReplacementResponseRootobject dbssRespObj)
        {
            CorporateSIMReplacementCheckResponseWithCustomerId response = new CorporateSIMReplacementCheckResponseWithCustomerId();
            string customerId = string.Empty;
            try
            {
                if (dbssRespObj.data.Count <= 0)
                {
                    return response = new CorporateSIMReplacementCheckResponseWithCustomerId()
                    {
                        result = false,
                        message = "No data found!"
                    };
                }

                if (dbssRespObj.data[0].id == null)
                {
                    return response = new CorporateSIMReplacementCheckResponseWithCustomerId()
                    {
                        result = false,
                        message = "Subscription Id field empty!"
                    };
                }

                if (dbssRespObj.data[0].attributes == null)
                {
                    return response = new CorporateSIMReplacementCheckResponseWithCustomerId()
                    {
                        result = false,
                        message = "Data not found."
                    };
                }

                if (dbssRespObj.data[0].relationships == null)
                {
                    return response = new CorporateSIMReplacementCheckResponseWithCustomerId()
                    {
                        result = false,
                        message = "Related data field not found."
                    };
                }

                if (dbssRespObj.data[0].attributes.status == null)
                {
                    return response = new CorporateSIMReplacementCheckResponseWithCustomerId()
                    {
                        result = false,
                        message = "Data not found."
                    };
                }

                string msdidnStatus = dbssRespObj.data[0].attributes.status;

                if (msdidnStatus != "active" && msdidnStatus != "idle")
                {
                    return response = new CorporateSIMReplacementCheckResponseWithCustomerId()
                    {
                        result = false,
                        message = "MSISDN is not in active status."
                    };
                }

                if (dbssRespObj.data[0].relationships.coordinatorcustomer == null)
                {
                    return response = new CorporateSIMReplacementCheckResponseWithCustomerId()
                    {
                        result = false,
                        message = "Co-ordinator customer info not found."
                    };
                }

                if (dbssRespObj.data[0].relationships.coordinatorcustomer.data == null)
                {
                    return response = new CorporateSIMReplacementCheckResponseWithCustomerId()
                    {
                        result = false,
                        message = "MSISDN does not belong to a corporate number."
                    };
                }

                if (dbssRespObj.data[0].relationships.coordinatorcustomer.data.id == null)
                {
                    return response = new CorporateSIMReplacementCheckResponseWithCustomerId()
                    {
                        result = false,
                        message = "Customer ID field empty."
                    };
                }

                customerId = dbssRespObj.data[0].relationships.coordinatorcustomer.data.id;

                return response = new CorporateSIMReplacementCheckResponseWithCustomerId()
                {
                    dbss_subscription_id = Convert.ToInt64(dbssRespObj.data[0].id),//intialized subscription id
                    result = true,
                    message = MessageCollection.Success,
                    customer_id = customerId,
                    old_sim_number = ""
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public CorporateSIMReplacementCheckResponseWithCustomerId CorporateSIMReplacementMSISDNReqParsing2(JObject dbssRespObj)
        {
            var raResp = new CorporateSIMReplacementCheckResponseWithCustomerId();

            void SetError(string message)
            {
                raResp.result = false;
                raResp.message = message;
            }

            try
            {
                var data = dbssRespObj["data"];
                var included = dbssRespObj["included"];

                if (data == null || !data.HasValues || data.Type != JTokenType.Object || included == null || !included.HasValues || included.Count() < 2)
                {
                    SetError(MessageCollection.SIMReplNoDataFound);
                    return raResp;
                }

                if (data["id"] == null)
                {
                    SetError("Subscription ID field empty!");
                    return raResp;
                }

                var dataAttributes = data["attributes"];
                var dataRelationships = data["relationships"];
                var included0Attrs = included[0]?["attributes"];
                var included1Attrs = included[1]?["attributes"];

                if (dataAttributes == null || dataRelationships == null || included0Attrs == null || included1Attrs == null)
                {
                    SetError(MessageCollection.SIMReplNoDataFound);
                    return raResp;
                }

                var coordinator = dataRelationships["coordinator-customer"];
                if (coordinator?["data"]?["id"] == null)
                {
                    SetError(coordinator == null ? MessageCollection.POCInfoNotFound : "Customer ID not found!");
                    return raResp;
                }

                bool? isCompany = (bool?)included0Attrs["is-company"];
                if (isCompany != true)
                {
                    SetError("This MSISDN is not eligible for corporate SIM replacement.");
                    return raResp;
                }

                string? oldSim = (string?)included1Attrs["icc"];
                if (string.IsNullOrEmpty(oldSim))
                {
                    SetError(MessageCollection.OldSIMNotFound);
                    return raResp;
                }

                string? status = (string?)dataAttributes["status"];
                string? docType = (string?)included0Attrs["id-document-type"];

                if (string.IsNullOrEmpty(status) || isCompany == null || string.IsNullOrEmpty(docType))
                {
                    SetError(MessageCollection.DataNotFound);
                    return raResp;
                }

                if (status != "active" && status != "idle")
                {
                    SetError("This MSISDN is not in active or idle status.");
                    return raResp;
                }

                raResp.result = true;
                raResp.message = MessageCollection.MSISDNValid;
                raResp.dbss_subscription_id = Convert.ToInt32((string?)data["id"]);
                raResp.old_sim_number = oldSim;
                raResp.old_sim_type = (string?)included1Attrs["sim-type"] ?? "";
                raResp.customer_id = (string?)coordinator["data"]?["id"] ?? "";

                return raResp;
            }
            catch
            {
                throw;
            }
        }

        public PaiedMSISDNCheckResponseDataRev PairedMSISDNReqParsingV3(PairedMSISDNValidationResponseRootobject dbssRespObj)
        {
            PaiedMSISDNCheckResponseDataRev raResp = new PaiedMSISDNCheckResponseDataRev();
            string simNo = String.Empty;
            try
            {
                if (dbssRespObj.data.attributes == null)
                {
                    raResp.isError = true;
                    raResp.message = MessageCollection.DataNotFound;
                    return raResp;
                }

                if (String.IsNullOrEmpty(dbssRespObj.data.attributes.msisdn)
                    || String.IsNullOrEmpty(dbssRespObj.data.attributes.status)
                    || String.IsNullOrEmpty(dbssRespObj.data.attributes.icc)
                    || String.IsNullOrEmpty(dbssRespObj.data.attributes.subscriptionType)
                    )
                {
                    raResp.isError = true;
                    raResp.message = MessageCollection.DataNotFound;
                    return raResp;
                }

                if (dbssRespObj.data.attributes.status != FixedValueCollection.ValidPairedMSISDNStatus)
                {
                    raResp.isError = true;
                    raResp.message = MessageCollection.MSISDNInvalid;
                    return raResp;
                }

                raResp.isError = false;
                if (dbssRespObj.data != null)
                {
                    raResp.data = new PaiedMSISDNCheckResponseRev()
                    {
                        sim_number = dbssRespObj.data.attributes.icc.Remove(0, FixedValueCollection.SIMCode.Length),
                        subscription_type_code = dbssRespObj.data.attributes.subscriptionType,
                        imsi = dbssRespObj.data.attributes.imsi,
                    };
                }
                raResp.message = MessageCollection.MSISDNValid;
            }
            catch (Exception)
            {
                throw;
            }
            return raResp;
        }

        public PaiedMSISDNCheckResponse PairedMSISDNReqParsing(JObject dbssRespObj)
        {
            PaiedMSISDNCheckResponse raResp = new PaiedMSISDNCheckResponse();
            string simNo = String.Empty;
            try
            {
                if (dbssRespObj["data"] != null)
                {
                    if (dbssRespObj["data"]?["attributes"] != null)
                    {
                        if (dbssRespObj["data"]?["attributes"]?["icc"] != null)
                        {
                            simNo = dbssRespObj["data"]?["attributes"]?["icc"]?.ToString() ?? "";
                        }
                    }
                }

                if (!String.IsNullOrEmpty(simNo))
                {
                    raResp.result = true;
                    raResp.message = "MSISDN is Valid.";
                    raResp.sim_number = simNo;
                }
                else
                {
                    raResp.result = false;
                    raResp.sim_number = string.Empty;
                    raResp.message = "SIM number is not attached with MSISDN.";
                }
            }
            catch (Exception)
            {
                throw;
            }
            return raResp;
        }

        public UnpairedMSISDNCheckResponse UnpairedMSISDNReqParsing(JObject dbssRespObj, string retailer_id)
        {
            UnpairedMSISDNCheckResponse raResp = new UnpairedMSISDNCheckResponse();
            try
            {
                string status = String.Empty;
                int stockId = 0;
                string reserved_for = string.Empty;

                if (dbssRespObj["data"] != null)
                {
                    if (dbssRespObj["data"]?["attributes"] != null)
                    {
                        if (!String.IsNullOrEmpty(dbssRespObj["data"]?["attributes"]?["status"]?.ToString())
                            && !String.IsNullOrEmpty(dbssRespObj["data"]?["attributes"]?["stock"]?.ToString()))
                        {
                            status = dbssRespObj["data"]?["attributes"]?["status"]?.ToString() ?? "";
                            stockId = Convert.ToInt32(dbssRespObj["data"]?["attributes"]?["stock"]);
                            reserved_for = dbssRespObj["data"]?["attributes"]?["reserved-for"]?.ToString() ?? "";
                        }
                    }
                }
                if (stockId == 33)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StockIDMismatch;
                    return raResp;
                }
                if (!String.IsNullOrEmpty(reserved_for))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNReserved;
                    return raResp;
                }
                if (status == "available")
                {
                    raResp = ValidateCherishedNumer(dbssRespObj, retailer_id);
                    raResp.stock_id = stockId;
                    return raResp;
                }
                else if (status == "in_use")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNInUse;
                    return raResp;
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "MSISDN is invalid.";
                    return raResp;
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public PairedMSISDNDataRev PairedMSISDNSearchParsing(JObject dbssRespObj)
        {
            PairedMSISDNDataRev raResp = new PairedMSISDNDataRev();
            try
            {
                if (dbssRespObj != null)
                {
                    if (dbssRespObj["data"] != null)
                    {
                        if (dbssRespObj["data"]?["relationships"] != null)
                        {
                            var msisdnData = dbssRespObj["data"]?["relationships"]?["msisdn"]?["data"];

                            if (msisdnData != null)
                            {
                                raResp.data = new ReponseDataRev()
                                {
                                    msisdn = msisdnData["id"]?.ToString() ?? ""
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return raResp;
        }

        public UnpairedMSISDNCheckResponse UnpairedMSISDNReqParsingV2(JObject dbssRespObj, string retailer_id)
        {
            UnpairedMSISDNCheckResponse raResp = new UnpairedMSISDNCheckResponse();

            string status = String.Empty;
            int stockId = 0;
            string retailer_code = String.Empty;
            string number_category = String.Empty;
            string category_config = String.Empty;
            string reserved_for = string.Empty;

            if (dbssRespObj["data"] != null)
            {
                if (dbssRespObj["data"]?["attributes"] != null)
                {
                    if (!String.IsNullOrEmpty(dbssRespObj["data"]?["attributes"]?["status"]?.ToString())
                        && !String.IsNullOrEmpty(dbssRespObj["data"]?["attributes"]?["stock"]?.ToString()))
                    {
                        status = dbssRespObj["data"]?["attributes"]?["status"]?.ToString() ?? "";
                        stockId = Convert.ToInt32(dbssRespObj["data"]?["attributes"]?["stock"]);
                        reserved_for = dbssRespObj["data"]?["attributes"]?["reserved-for"]?.ToString() ?? "";
                    }
                }
                if (stockId == 33)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StockIDMismatch;
                    return raResp;
                }
                if (!String.IsNullOrEmpty(reserved_for))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNReserved;
                    return raResp;
                }
                if (status == "available")
                {
                    raResp = ValidateCherishedNumer(dbssRespObj, retailer_id);
                    raResp.stock_id = stockId;
                    return raResp;
                }
                else if (status == "in_use")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNInUse;
                    return raResp;
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "MSISDN is invalid.";
                    return raResp;
                }
            }
            else
            {
                raResp.result = false;
                raResp.message = "DBSS Error: Incomplete response body.";
                return raResp;
            }
        }

        public UnpairedMSISDNCheckResponse ValidateCherishedNumer(JObject dbssRespObj, string retailer_id)
        {

            UnpairedMSISDNCheckResponse raResp = new UnpairedMSISDNCheckResponse();

            string status = String.Empty;
            string retailer_code = String.Empty;
            string number_category = String.Empty;
            string category_config = String.Empty;
            string[] cofigValue = Array.Empty<string>();

            try
            {
                if (dbssRespObj["data"] != null)
                {
                    if (dbssRespObj["data"]?["attributes"] != null)
                    {
                        category_config = SettingsValues.GetNumberCategory();

                        if (category_config.Contains(","))
                        {
                            cofigValue = category_config.Split(',');
                        }
                        else
                        {
                            cofigValue = category_config.Split(' ');
                        }

                        if (dbssRespObj["data"]?["attributes"]?["number-category"] != null)
                        {
                            retailer_code = dbssRespObj["data"]?["attributes"]?["salesman-id"]?.ToString() ?? "";
                            number_category = dbssRespObj["data"]?["attributes"]?["number-category"]?.ToString() ?? "";

                            if (!String.IsNullOrEmpty(retailer_code))
                            {
                                if (retailer_code.Length < 6)
                                {
                                    char pad = '0';
                                    retailer_code = retailer_code.PadLeft(6, pad);
                                }
                            }

                            if (!String.IsNullOrEmpty(retailer_code) && !String.IsNullOrEmpty(number_category) && cofigValue.Any(x => x != number_category)) // from Web.config 
                            {
                                if (retailer_id.Equals(retailer_code))
                                {
                                    raResp.result = true;
                                    raResp.message = MessageCollection.ValidCherishedNumber;
                                }
                                else
                                {
                                    raResp.result = false;
                                    raResp.message = MessageCollection.InvalidCherishedNumber;
                                }
                            }
                            else if (String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x == number_category))
                            {
                                raResp.result = true;
                                raResp.message = MessageCollection.ValidCherishedNumber;
                            }
                            else if (!String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x == number_category))
                            {
                                raResp.result = true;
                                raResp.message = MessageCollection.ValidCherishedNumber; ;
                            }
                            else if (String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x != number_category))
                            {
                                raResp.result = false;
                                raResp.message = "MSISDN not tagged with this Retailer (ID: " + retailer_id + ")";
                            }
                            else
                            {
                                raResp.result = false;
                                raResp.message = "MSISDN is not Valid.";
                            }
                        }
                        else
                        {
                            raResp.result = false;
                            raResp.message = "Invalid MSISDN Category!";
                        }
                    }
                    else
                    {
                        raResp.result = false;
                        raResp.message = "No Data found!";
                    }
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "No Data found!";
                }

                return raResp;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public CherishMSISDNCheckResponse CherishMSISDNReqParsing(JObject dbssRespObj, string retailer_id)
        {
            CherishMSISDNCheckResponse raResp = new CherishMSISDNCheckResponse();
            try
            {
                string retailer_code = String.Empty;
                string number_category = String.Empty;
                string category_config = String.Empty;
                string[] cofigValue = Array.Empty<string>();

                category_config = SettingsValues.GetNumberCategory();

                if (category_config.Contains(","))
                {
                    cofigValue = category_config.Split(',');
                }
                else
                {
                    cofigValue = category_config.Split(' ');
                }

                if (dbssRespObj?["data"]?["attributes"]?["number-category"] != null)
                {
                    retailer_code = dbssRespObj["data"]?["attributes"]?["salesman-id"]?.ToString() ?? "";
                    number_category = dbssRespObj["data"]?["attributes"]?["number-category"]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(retailer_code) && retailer_code.Length < 6)
                    {
                        retailer_code = retailer_code.PadLeft(6, '0');
                    }

                    bool isRetailerMatch = retailer_id.Equals(retailer_code);
                    bool isCategoryMatch = cofigValue.Any(x => x == number_category);
                    bool isCategoryMismatch = cofigValue.Any(x => x != number_category);

                    if (!string.IsNullOrEmpty(retailer_code) && !string.IsNullOrEmpty(number_category) && isCategoryMismatch)
                    {
                        if (isRetailerMatch)
                        {
                            raResp.result = true;
                            raResp.message = MessageCollection.ValidCherishedNumber;
                        }
                        else
                        {
                            raResp.result = false;
                            raResp.message = MessageCollection.InvalidCherishedNumber;
                        }
                    }
                    else if (isCategoryMatch)
                    {
                        raResp.result = true;
                        raResp.message = MessageCollection.ValidCherishedNumber;
                    }
                    else if (string.IsNullOrEmpty(retailer_code) && isCategoryMismatch)
                    {
                        raResp.result = false;
                        raResp.message = $"MSISDN not tagged with this Retailer (ID: {retailer_id})";
                    }
                    else
                    {
                        raResp.result = false;
                        raResp.message = "MSISDN is not Valid.";
                    }
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "number-category is Empty!";
                }

            }
            catch (Exception)
            {
                throw;
            }
            return raResp;
        }
        public RACommonResponse UnpairedMSISDNReqParsingForTOS(JObject dbssRespObj, string retailer_id)
        {
            CherishMSISDNCheckResponse raResp = new CherishMSISDNCheckResponse();

            try
            {
                string retailerCode = string.Empty;
                string numberCategory = string.Empty;

                string categoryConfig = SettingsValues.GetNumberCategory();
                string[] configValues = categoryConfig.Contains(",")
                    ? categoryConfig.Split(',')
                    : categoryConfig.Split(' ');

                var attributes = dbssRespObj?["data"]?["attributes"];
                if (attributes != null)
                {
                    retailerCode = attributes["salesman-id"]?.ToString() ?? string.Empty;
                    numberCategory = attributes["number-category"]?.ToString() ?? string.Empty;

                    // Normalize retailer code to 6 digits if needed
                    if (!string.IsNullOrEmpty(retailerCode) && retailerCode.Length < 6)
                    {
                        retailerCode = retailerCode.PadLeft(6, '0');
                    }

                    // Evaluate number category logic
                    if (string.IsNullOrEmpty(numberCategory))
                    {
                        raResp.result = false;
                        raResp.message = "Cherish validation: number-category is Empty!";
                    }
                    else if (!configValues.Contains(numberCategory))
                    {
                        raResp.result = true;
                        raResp.message = MessageCollection.ValidCherishedNumber;
                    }
                    else
                    {
                        raResp.result = false;
                        raResp.message = "Cherish validation: number-category is not matched!";
                    }

                    return raResp;
                }
                else
                {
                    raResp.result = false;
                    raResp.message = dbssRespObj?["data"] == null
                        ? "Cherish validation: MSISDN not found!"
                        : "Cherish validation: No Data found!";
                    return raResp;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        //public RACommonResponse UnpairedMSISDNReqParsingForTOS(JObject dbssRespObj, string retailer_id)
        //{
        //    CherishMSISDNCheckResponse raResp = new CherishMSISDNCheckResponse();
        //    try
        //    {
        //        string retailer_code = String.Empty;
        //        string number_category = String.Empty;
        //        string category_config = String.Empty;
        //        string[] cofigValue = Array.Empty<string>();

        //        category_config = SettingsValues.GetNumberCategory();

        //        if (category_config.Contains(","))
        //        {
        //            cofigValue = category_config.Split(',');
        //        }
        //        else
        //        {
        //            cofigValue = category_config.Split(' ');
        //        }

        //        if (dbssRespObj?["data"]?["attributes"] != null)
        //        {
        //            retailer_code = dbssRespObj?["data"]?["attributes"]?["salesman-id"]?.ToString() ?? string.Empty;
        //            number_category = dbssRespObj?["data"]?["attributes"]?["number-category"]?.ToString() ?? string.Empty;

        //            if (!string.IsNullOrEmpty(retailer_code) && retailer_code.Length < 6)
        //            {
        //                retailer_code = retailer_code.PadLeft(6, '0');
        //            }

        //            if (!string.IsNullOrEmpty(number_category) && !cofigValue.Contains(number_category))
        //            {
        //                raResp.result = true;
        //                raResp.message = MessageCollection.ValidCherishedNumber;                        
        //            }
        //            else if (cofigValue.Contains(number_category))
        //            {
        //                raResp.message = "Cherish validation: number-category is not matched!";
        //                raResp.result = false;
        //            }
        //            else
        //            {
        //                raResp.result = false;
        //                raResp.message = "Cherish validation: number-category is Empty!";
        //                return raResp;
        //            }
        //            return raResp;
        //        }
        //        else
        //        {
        //            raResp.result = false;
        //            raResp.message = dbssRespObj?["data"] == null
        //                ? "Cherish validation: MSISDN not found!"
        //                : "Cherish validation: No Data found!";
        //            return raResp;
        //        }                
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }            
        //}
        public List<SubscriptionTypeReponseData> SubscripTypesReqParsing(List<object> dbssRespModel)
        {
            List<SubscriptionTypeReponseData> subscriptionTypes = new List<SubscriptionTypeReponseData>();
            try
            {
                for (int i = 0; i < dbssRespModel.Count; i++)
                {
                    JObject rss = JObject.Parse(dbssRespModel[i].ToString() ?? "");
                    SubscriptionTypeReponseData raResp = new SubscriptionTypeReponseData();
                    raResp.subscription_id = rss["id"]?.ToString() ?? "";
                    raResp.subscription_name = rss["attributes"]?["code"]?.ToString() ?? "";
                    subscriptionTypes.Add(raResp);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return subscriptionTypes;
        }

        public List<SubscriptionTypeReponseDataRev> SubscripTypesReqParsingV2(List<object> dbssRespModel)
        {
            List<SubscriptionTypeReponseDataRev> subscriptionTypes = new List<SubscriptionTypeReponseDataRev>();
            try
            {
                for (int i = 0; i < dbssRespModel.Count; i++)
                {
                    JObject rss = JObject.Parse(dbssRespModel[i].ToString() ?? "");
                    SubscriptionTypeReponseDataRev raResp = new SubscriptionTypeReponseDataRev();
                    raResp.subscription_id = rss["id"]?.ToString() ?? "";
                    raResp.subscription_name = rss["attributes"]?["code"]?.ToString() ?? "";
                    subscriptionTypes.Add(raResp);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return subscriptionTypes;
        }


        public List<SubscriptionTypeByIdReponseData> SubscripTypesByIdReqParsing(List<object> dbssRespModel)
        {
            List<SubscriptionTypeByIdReponseData> subscriptionTypes = new List<SubscriptionTypeByIdReponseData>();
            try
            {
                for (int i = 0; i < dbssRespModel.Count; i++)
                {
                    JObject jessonResponse = JObject.Parse(dbssRespModel[i].ToString() ?? "");
                    SubscriptionTypeByIdReponseData raResp = new SubscriptionTypeByIdReponseData();
                    raResp.subscription_type_id = jessonResponse["id"]?.ToString() ?? "";
                    raResp.subscription_type_name = jessonResponse["attributes"]?["code"]?.ToString() ?? "";
                    subscriptionTypes.Add(raResp);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return subscriptionTypes;
        }
        public List<SubscriptionTypeByIdReponseDataRev> SubscripTypesByIdReqParsingRev(List<object> dbssRespModel)
        {
            List<SubscriptionTypeByIdReponseDataRev> subscriptionTypes = new List<SubscriptionTypeByIdReponseDataRev>();
            try
            {
                for (int i = 0; i < dbssRespModel.Count; i++)
                {
                    JObject jessonResponse = JObject.Parse(dbssRespModel[i].ToString() ?? "");
                    SubscriptionTypeByIdReponseDataRev raResp = new SubscriptionTypeByIdReponseDataRev();
                    raResp.subscription_type_id = jessonResponse["id"]?.ToString() ?? "";
                    raResp.subscription_type_name = jessonResponse["attributes"]?["code"]?.ToString() ?? "";
                    subscriptionTypes.Add(raResp);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return subscriptionTypes;
        }

        public List<PackagesReponseData> PackagesParsing(List<object> dbssRespModel)
        {
            List<PackagesReponseData> packages = new List<PackagesReponseData>();
            try
            {
                for (int i = 0; i < dbssRespModel.Count; i++)
                {
                    JObject rss = JObject.Parse(dbssRespModel[i].ToString() ?? "");

                    PackagesReponseData raResp = new PackagesReponseData();

                    if (rss["id"] != null && rss["attributes"]?["code"] != null)
                    {
                        raResp.package_id = rss["id"]?.ToString() ?? "";
                        raResp.package_name = rss["attributes"]?["code"]?.ToString() ?? "";
                        packages.Add(raResp);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return packages;
        }

        public List<PackagesReponseDataRev> PackagesParsingV2(List<object> dbssRespModel)
        {
            List<PackagesReponseDataRev> packages = new List<PackagesReponseDataRev>();
            try
            {
                for (int i = 0; i < dbssRespModel.Count; i++)
                {
                    JObject rss = JObject.Parse(dbssRespModel[i].ToString() ?? "");

                    PackagesReponseDataRev raResp = new PackagesReponseDataRev();

                    if (rss["id"] != null && rss["attributes"]?["code"] != null)
                    {
                        raResp.package_id = rss["id"]?.ToString() ?? "";
                        raResp.package_name = rss["attributes"]?["code"]?.ToString() ?? "";
                        packages.Add(raResp);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return packages;
        }

        public List<PackagesReponseDataRev> PackagesParsingV4(List<object> dbssRespModel, string offerName)
        {
            List<PackagesReponseDataRev> packages = new List<PackagesReponseDataRev>();
            try
            {
                for (int i = 0; i < dbssRespModel.Count; i++)
                {
                    JObject rss = JObject.Parse(dbssRespModel[i].ToString() ?? "");

                    PackagesReponseDataRev raResp = new PackagesReponseDataRev();

                    if (rss["id"] != null && rss["attributes"]?["code"] != null)
                    {
                        string packageId = rss["id"]?.ToString() ?? "";
                        string packageName = rss["attributes"]?["code"]?.ToString() ?? "";

                        if (!string.IsNullOrEmpty(offerName) &&
                            packageName.Equals(offerName, StringComparison.OrdinalIgnoreCase))
                        {
                            packages.Add(new PackagesReponseDataRev
                            {
                                package_id = packageId,
                                package_name = packageName
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return packages;
        }


        public OldSIMNnumberResponse OldSIMNumberParsing(SIMNumberParsingRootobject rootObj)
        {
            OldSIMNnumberResponse response = new OldSIMNnumberResponse();
            if (rootObj.data.Count <= 0)
            {
                response.result = false;
                response.message = MessageCollection.NoDataFound;
                return response;
            }

            if (rootObj.data[0].attributes == null)
            {
                response.result = false;
                response.message = MessageCollection.DataNotFound;
                return response;
            }

            if (rootObj.data[0].attributes.icc == null)
            {
                response.result = false;
                response.message = MessageCollection.DataNotFound;
                return response;
            }

            response.old_sim_number = rootObj.data[0].attributes.icc;
            response.message = MessageCollection.Success;
            return response;

        }


        public string PaymentTypeFromSubscripTypeReqParsing(JObject dbssRespModel)
        {
            try
            {
                var paymentType = dbssRespModel?["data"]?["attributes"]?["payment-type"]?.ToString();

                return string.IsNullOrEmpty(paymentType) ? string.Empty : paymentType;
            }
            catch
            {
                throw;
            }
        }



        #region TOS NID to NID

        public TosNidToNidMSISDNCheckResponse TosNidToNidMSISDNReqParsingV1(JObject dbssRespObj)
        {
            TosNidToNidMSISDNCheckResponse raResp = new TosNidToNidMSISDNCheckResponse();

            try
            {
                // ===== Basic Validations =====
                var data = dbssRespObj["data"];
                var included = dbssRespObj["included"] as JArray;

                if (data == null || !data.HasValues || data.Count() <= 0)
                    throw new Exception(MessageCollection.SIMReplNoDataFound);

                string? status = (string?)data["attributes"]?["status"];
                if (string.IsNullOrWhiteSpace(status))
                    throw new Exception("Msisdn status not found!");

                if (status == "terminated")
                    throw new Exception("Msisdn is not valid for TOS!");

                if (status.Contains("suspended"))
                    throw new Exception("This Number Is Suspended.");

                if (status != "active" && status != "idle")
                    throw new Exception(MessageCollection.MSISDNStatusNotActiveOrIdle);

                if (included == null || !included.HasValues)
                    throw new Exception("Data not found in include field!");

                if (included.Count < 2)
                    throw new Exception("Customer info or SIM cards info missing in include field!");

                if (data["id"] == null)
                    throw new Exception("Subscription ID field empty!");

                string? src_sim_category = (string?)data["attributes"]?["payment-type"];
                if (string.IsNullOrWhiteSpace(src_sim_category))
                    throw new Exception("Source customer payment type not found!");

                if (included[0]?["attributes"] == null || included[1]?["attributes"] == null)
                    throw new Exception("Data not found in include field!");

                // ===== Extract Relationship IDs =====
                string src_sim_cards_id = (string?)data["relationships"]?["sim-cards"]?["data"]?[0]?["id"]
                    ?? throw new Exception("SIM card ID not found");

                string src_owner_customer_id = (string?)data["relationships"]?["owner-customer"]?["data"]?["id"]
                    ?? throw new Exception("Owner customer ID not found");

                string src_user_customer_id = (string?)data["relationships"]?["user-customer"]?["data"]?["id"]
                    ?? throw new Exception("User customer ID not found");

                string src_payer_customer_id = (string?)data["relationships"]?["payer-customer"]?["data"]?["id"]
                    ?? throw new Exception("Payer customer ID not found");


                // ===== Match Owner/User/Payer Logic =====
                int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(included, src_owner_customer_id);
                int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(included, src_sim_cards_id);

                VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);

                // Common assignment
                raResp.dob = customerAndSimCardsInfo.dob;
                raResp.doc_id_number = customerAndSimCardsInfo.doc_id_number;
                raResp.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
                raResp.old_sim_number = customerAndSimCardsInfo.old_sim_number;
                raResp.old_sim_type = customerAndSimCardsInfo.old_sim_type;

                // Determine mapping logic based on ID equality
                bool ocEqUc = src_owner_customer_id == src_user_customer_id;
                bool ocEqPc = src_owner_customer_id == src_payer_customer_id;
                bool ucEqPc = src_user_customer_id == src_payer_customer_id;

                if (included.Count switch
                {
                    2 when ocEqUc && ucEqPc => true,
                    3 when !ocEqUc && !ocEqPc && ucEqPc => true,
                    3 when ocEqPc && !ocEqUc => true,
                    3 when ocEqUc && !ucEqPc => true,
                    4 when !ocEqUc && !ucEqPc && !ocEqPc => true,
                    _ => false
                })
                {
                    // Assign based on matching scenario
                    if (ocEqUc && ucEqPc)
                    {
                        raResp.src_owner_customer_id = raResp.src_user_customer_id = raResp.src_payer_customer_id = src_owner_customer_id;
                    }
                    else if (!ocEqUc && !ocEqPc && ucEqPc)
                    {
                        raResp.src_owner_customer_id = src_owner_customer_id;
                        raResp.src_user_customer_id = raResp.src_payer_customer_id = src_user_customer_id;
                    }
                    else if (ocEqPc && !ocEqUc)
                    {
                        raResp.src_owner_customer_id = raResp.src_payer_customer_id = src_owner_customer_id;
                        raResp.src_user_customer_id = src_user_customer_id;
                    }
                    else if (ocEqUc && !ucEqPc)
                    {
                        raResp.src_owner_customer_id = raResp.src_user_customer_id = src_owner_customer_id;
                        raResp.src_payer_customer_id = src_payer_customer_id;
                    }
                    else if (!ocEqUc && !ucEqPc && !ocEqPc)
                    {
                        raResp.src_owner_customer_id = src_owner_customer_id;
                        raResp.src_user_customer_id = src_user_customer_id;
                        raResp.src_payer_customer_id = src_payer_customer_id;
                    }

                    raResp.src_sim_category = src_sim_category.ToLower() switch
                    {
                        "prepaid" => (int)EnumSimCategory.Prepaid,
                        "postpaid" => (int)EnumSimCategory.Postpaid,
                        _ => throw new Exception("Unknown source customer payment type (SIM category)!")
                    };

                    raResp.result = true;
                    raResp.message = MessageCollection.MSISDNValid;
                    return raResp;
                }

                throw new Exception("Invalid DBSS Response!");
            }
            catch (Exception ex)
            {
                raResp.result = false;
                raResp.message = ex.Message;
                return raResp;
            }

        }


        /// <summary>
        /// in Include attribute for:- 
        ///                index 0: 
        ///                index 1: 
        ///                index 2: 
        /// </summary>
        /// <param name="dbssRespObj"></param>
        /// <returns></returns>
        /// 
        //public TosNidToNidMSISDNCheckResponse TosNidToNidMSISDNReqParsingV1(JObject dbssRespObj)
        //{
        //    TosNidToNidMSISDNCheckResponse raResp = new TosNidToNidMSISDNCheckResponse();
        //    try
        //    {
        //        if (!dbssRespObj["data"].HasValues
        //            || dbssRespObj["data"].Count() <= 0)
        //        {
        //            throw new Exception(MessageCollection.SIMReplNoDataFound);
        //        }

        //        if (String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["status"]))
        //        {
        //            throw new Exception("Msisdn status not found!");
        //        }

        //        if ((string)dbssRespObj["data"]["attributes"]["status"] == "terminated")
        //        {
        //            throw new Exception("Msisdn is not valid for TOS!");
        //        }

        //        if ((string)dbssRespObj["data"]["attributes"]["status"] != "active"
        //             && (string)dbssRespObj["data"]["attributes"]["status"] != "idle")
        //        {
        //            throw new Exception(MessageCollection.MSISDNStatusNotActiveOrIdle);
        //        }

        //        if (!dbssRespObj["included"].HasValues)
        //        {
        //            throw new Exception("Data not found in include field!");
        //        }

        //        if (dbssRespObj["included"].Count() < 2)
        //        {
        //            throw new Exception("Customer info or SIM cards info missing in include field!");
        //        }
        //        if (dbssRespObj["data"]["id"] == null)
        //        {
        //            throw new Exception("Subscription ID field empty!");
        //        }

        //        if (String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["payment-type"]))
        //        {
        //            throw new Exception("Source customer payment type not found!");
        //        }

        //        if (dbssRespObj["included"][0]["attributes"] == null
        //            || dbssRespObj["included"][1]["attributes"] == null)
        //        {
        //            throw new Exception("Data not found in include field!");
        //        }

        //        //==================
        //        string src_sim_cards_id;
        //        string src_owner_customer_id;
        //        string src_user_customer_id;
        //        string src_payer_customer_id;
        //        string src_sim_category;
        //        try
        //        {
        //            src_sim_cards_id = (string)dbssRespObj["data"]["relationships"]["sim-cards"]["data"][0]["id"];
        //            src_owner_customer_id = (string)dbssRespObj["data"]["relationships"]["owner-customer"]["data"]["id"];
        //            src_user_customer_id = (string)dbssRespObj["data"]["relationships"]["payer-customer"]["data"]["id"];
        //            src_payer_customer_id = (string)dbssRespObj["data"]["relationships"]["user-customer"]["data"]["id"];
        //            src_sim_category = (string)dbssRespObj["data"]["attributes"]["payment-type"];
        //        }
        //        catch (Exception)
        //        {
        //            throw new Exception("Required data not found in relationships field!");
        //        }

        //        switch (dbssRespObj["included"].Count())
        //        {
        //            case 0:
        //                throw new Exception("Required data not found in include field!");

        //            case 1:
        //                throw new Exception("Required data not found in include field!");

        //            case 2: //oc=uc=pc, sim_cards
        //                if (src_owner_customer_id.Equals(src_user_customer_id)
        //                    && src_user_customer_id.Equals(src_payer_customer_id))
        //                {

        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);

        //                    raResp.dob = customerAndSimCardsInfo.dob;
        //                    raResp.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.src_owner_customer_id = raResp.src_user_customer_id = raResp.src_payer_customer_id = src_owner_customer_id;

        //                    if (src_sim_category == "prepaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.result = true;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;
        //                }
        //                else
        //                {
        //                    throw new Exception("Invalid DBSS Repponse!");
        //                }

        //            case 3: //oc, uc=pc, sim_cards || oc=pc, uc, sim_cards || oc=uc, pc, sim_cards
        //                if (!src_owner_customer_id.Equals(src_user_customer_id)
        //                    && !src_owner_customer_id.Equals(src_payer_customer_id)
        //                    && src_user_customer_id.Equals(src_payer_customer_id))
        //                {

        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);

        //                    raResp.dob = customerAndSimCardsInfo.dob;
        //                    raResp.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.src_owner_customer_id = src_owner_customer_id;
        //                    raResp.src_user_customer_id = raResp.src_payer_customer_id = src_user_customer_id;

        //                    if (src_sim_category == "prepaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.result = true;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;

        //                }
        //                //oc = pc, uc, sim_cards
        //                else if (src_owner_customer_id.Equals(src_payer_customer_id)
        //                            && !src_owner_customer_id.Equals(src_user_customer_id))
        //                {

        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);

        //                    raResp.dob = customerAndSimCardsInfo.dob;
        //                    raResp.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.src_owner_customer_id = raResp.src_payer_customer_id = src_owner_customer_id;
        //                    raResp.src_user_customer_id = src_user_customer_id;


        //                    if (src_sim_category == "prepaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.result = true;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;

        //                }
        //                //oc=uc, pc, sim_cards
        //                else if (src_owner_customer_id.Equals(src_user_customer_id)
        //                    && !src_user_customer_id.Equals(src_payer_customer_id))
        //                {
        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);

        //                    raResp.dob = customerAndSimCardsInfo.dob;
        //                    raResp.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.src_owner_customer_id = raResp.src_user_customer_id = src_owner_customer_id;
        //                    raResp.src_payer_customer_id = src_payer_customer_id;


        //                    if (src_sim_category == "prepaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.result = true;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;
        //                }
        //                else
        //                {
        //                    throw new Exception("Invalid DBSS Repponse!");
        //                }

        //            case 4: //ow, uc, pc, sim_cards
        //                if (!src_owner_customer_id.Equals(src_user_customer_id)
        //                    && !src_user_customer_id.Equals(src_payer_customer_id)
        //                    && !src_owner_customer_id.Equals(src_payer_customer_id))
        //                {
        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);
        //                    raResp.dob = customerAndSimCardsInfo.dob;
        //                    raResp.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.src_owner_customer_id = src_owner_customer_id;
        //                    raResp.src_user_customer_id = src_user_customer_id;
        //                    raResp.src_payer_customer_id = src_payer_customer_id;

        //                    if (src_sim_category == "prepaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.result = true;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;
        //                }
        //                else
        //                {
        //                    throw new Exception("Invalid DBSS Repponse!");
        //                }
        //        }
        //        return raResp;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public TosNidToNidMSISDNCheckResponseRevamp TosNidToNidMSISDNReqParsingV3(JObject dbssRespObj)
        //{
        //    TosNidToNidMSISDNCheckResponseRevamp raResp = new TosNidToNidMSISDNCheckResponseRevamp();
        //    try
        //    {
        //        if (!dbssRespObj["data"].HasValues
        //            || dbssRespObj["data"].Count() <= 0)
        //        {
        //            throw new Exception(MessageCollection.SIMReplNoDataFound);
        //        }

        //        if (String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["status"]))
        //        {
        //            throw new Exception("Msisdn status not found!");
        //        }

        //        if ((string)dbssRespObj["data"]["attributes"]["status"] == "terminated")
        //        {
        //            throw new Exception("Msisdn is not valid for TOS!");
        //        }

        //        if ((string)dbssRespObj["data"]["attributes"]["status"] != "active"
        //             && (string)dbssRespObj["data"]["attributes"]["status"] != "idle")
        //        {
        //            throw new Exception(MessageCollection.MSISDNStatusNotActiveOrIdle);
        //        }

        //        if (!dbssRespObj["included"].HasValues)
        //        {
        //            throw new Exception("Data not found in include field!");
        //        }

        //        if (dbssRespObj["included"].Count() < 2)
        //        {
        //            throw new Exception("Customer info or SIM cards info missing in include field!");
        //        }
        //        if (dbssRespObj["data"]["id"] == null)
        //        {
        //            throw new Exception("Subscription ID field empty!");
        //        }

        //        if (String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["payment-type"]))
        //        {
        //            throw new Exception("Source customer payment type not found!");
        //        }

        //        if (dbssRespObj["included"][0]["attributes"] == null
        //            || dbssRespObj["included"][1]["attributes"] == null)
        //        {
        //            throw new Exception("Data not found in include field!");
        //        }

        //        //==================
        //        string src_sim_cards_id;
        //        string src_owner_customer_id;
        //        string src_user_customer_id;
        //        string src_payer_customer_id;
        //        string src_sim_category;
        //        try
        //        {
        //            src_sim_cards_id = (string)dbssRespObj["data"]["relationships"]["sim-cards"]["data"][0]["id"];
        //            src_owner_customer_id = (string)dbssRespObj["data"]["relationships"]["owner-customer"]["data"]["id"];
        //            src_user_customer_id = (string)dbssRespObj["data"]["relationships"]["payer-customer"]["data"]["id"];
        //            src_payer_customer_id = (string)dbssRespObj["data"]["relationships"]["user-customer"]["data"]["id"];
        //            src_sim_category = (string)dbssRespObj["data"]["attributes"]["payment-type"];
        //        }
        //        catch (Exception)
        //        {
        //            throw new Exception("Required data not found in relationships field!");
        //        }

        //        switch (dbssRespObj["included"].Count())
        //        {
        //            case 0:
        //                throw new Exception("Required data not found in include field!");

        //            case 1:
        //                throw new Exception("Required data not found in include field!");

        //            case 2: //oc=uc=pc, sim_cards
        //                if (src_owner_customer_id.Equals(src_user_customer_id)
        //                    && src_user_customer_id.Equals(src_payer_customer_id))
        //                {

        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);

        //                    raResp.data.dob = customerAndSimCardsInfo.dob;
        //                    raResp.data.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.data.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.data.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.data.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.data.src_owner_customer_id = raResp.data.src_user_customer_id = raResp.data.src_payer_customer_id = src_owner_customer_id;

        //                    if (src_sim_category == "prepaid")
        //                        raResp.data.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.data.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.isError = false;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;
        //                }
        //                else
        //                {
        //                    throw new Exception("Invalid DBSS Repponse!");
        //                }

        //            case 3: //oc, uc=pc, sim_cards || oc=pc, uc, sim_cards || oc=uc, pc, sim_cards
        //                if (!src_owner_customer_id.Equals(src_user_customer_id)
        //                    && !src_owner_customer_id.Equals(src_payer_customer_id)
        //                    && src_user_customer_id.Equals(src_payer_customer_id))
        //                {

        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);

        //                    raResp.data = new TosNidToNidMSISDNCheckResponse()
        //                    {
        //                        dob = customerAndSimCardsInfo.dob,
        //                        doc_id_number = customerAndSimCardsInfo.doc_id_number,
        //                        dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id,
        //                        old_sim_number = customerAndSimCardsInfo.old_sim_number,
        //                        old_sim_type = customerAndSimCardsInfo.old_sim_type,
        //                        src_owner_customer_id = src_owner_customer_id,
        //                        src_user_customer_id = raResp.data.src_payer_customer_id = src_user_customer_id
        //                    };


        //                    if (src_sim_category == "prepaid")
        //                    {
        //                        raResp.data.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    }
        //                    else if (src_sim_category == "postpaid")
        //                    {
        //                        raResp.data.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    }
        //                    else
        //                    {
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");
        //                    }
        //                    raResp.isError = false;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;

        //                }
        //                //oc = pc, uc, sim_cards
        //                else if (src_owner_customer_id.Equals(src_payer_customer_id)
        //                            && !src_owner_customer_id.Equals(src_user_customer_id))
        //                {

        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);

        //                    raResp.data.dob = customerAndSimCardsInfo.dob;
        //                    raResp.data.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.data.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.data.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.data.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.data.src_owner_customer_id = raResp.data.src_payer_customer_id = src_owner_customer_id;
        //                    raResp.data.src_user_customer_id = src_user_customer_id;


        //                    if (src_sim_category == "prepaid")
        //                        raResp.data.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.data.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.isError = false;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;

        //                }
        //                //oc=uc, pc, sim_cards
        //                else if (src_owner_customer_id.Equals(src_user_customer_id)
        //                    && !src_user_customer_id.Equals(src_payer_customer_id))
        //                {
        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);

        //                    raResp.data.dob = customerAndSimCardsInfo.dob;
        //                    raResp.data.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.data.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.data.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.data.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.data.src_owner_customer_id = raResp.data.src_user_customer_id = src_owner_customer_id;
        //                    raResp.data.src_payer_customer_id = src_payer_customer_id;


        //                    if (src_sim_category == "prepaid")
        //                        raResp.data.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.data.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.isError = false;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;
        //                }
        //                else
        //                {
        //                    throw new Exception("Invalid DBSS Repponse!");
        //                }

        //            case 4: //ow, uc, pc, sim_cards
        //                if (!src_owner_customer_id.Equals(src_user_customer_id)
        //                    && !src_user_customer_id.Equals(src_payer_customer_id)
        //                    && !src_owner_customer_id.Equals(src_payer_customer_id))
        //                {
        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);
        //                    raResp.data.dob = customerAndSimCardsInfo.dob;
        //                    raResp.data.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.data.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.data.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.data.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.data.src_owner_customer_id = src_owner_customer_id;
        //                    raResp.data.src_user_customer_id = src_user_customer_id;
        //                    raResp.data.src_payer_customer_id = src_payer_customer_id;

        //                    if (src_sim_category == "prepaid")
        //                        raResp.data.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.data.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.isError = false;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;
        //                }
        //                else
        //                {
        //                    throw new Exception("Invalid DBSS Repponse!");
        //                }
        //        }
        //        return raResp;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}


        //public TOSDebtStatusResponse TOSDebtStatusCheckParse(JObject dbssRespObj)
        //{
        //    TOSDebtStatusResponse response = new TOSDebtStatusResponse();
        //    response.result = false;
        //    try
        //    {
        //        if (!dbssRespObj["data"].HasValues
        //            || dbssRespObj["data"].Count() <= 0)
        //        {
        //            response.result = false;
        //            response.message = MessageCollection.MSISDNValid;
        //            return response;
        //        }
        //        int totalData = dbssRespObj["data"].Count();

        //        for (int i = 0; i < totalData; i++)
        //        {
        //            if (!String.IsNullOrEmpty((string)dbssRespObj["data"][i]["attributes"]["debt"]))
        //            {
        //                response.debt = (decimal)dbssRespObj["data"][i]["attributes"]["debt"];

        //                if (response.debt > 0)
        //                {
        //                    response.result = true;
        //                    response.message = "Please pay your due bill to do the transfer of ownership, Thank you!";
        //                    return response;
        //                }
        //            }
        //            response.message = MessageCollection.MSISDNValid;
        //        }
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }
        //}

        public TOSDebtStatusResponse TOSDebtStatusCheckParse(JObject dbssRespObj)
        {
            TOSDebtStatusResponse response = new TOSDebtStatusResponse();
            try
            {
                var data = dbssRespObj["data"] as JArray;

                if (data == null || data.Count == 0)
                {
                    response.result = false;
                    response.message = "Debt check api response is invalid";
                    return response;
                }
                else
                {
                    response.result = true;
                    response.message = "MSISDN is valid";
                    foreach (var item in data)
                    {
                        var debtToken = item["attributes"]?["debt"];
                        if (debtToken != null && decimal.TryParse(debtToken.ToString(), out decimal debtAmount))
                        {
                            if (debtAmount > 0)
                            {
                                response.debt = debtAmount;
                                response.result = false;
                                response.message = "Please pay your due bill to do the transfer of ownership, Thank you!";

                                return response;
                            }
                        }
                    }
                }
                return response;
            }
            catch
            {
                throw;
            }
        }

        public TOSLoanStatusResponse TosNiDtoNIDLoanStatusCheckParsing(JObject dbssRespObj)
        {
            var response = new TOSLoanStatusResponse { result = true };

            try
            {
                var data = dbssRespObj["data"] as JArray;
                if (data == null || data.Count == 0)
                {
                    response.message = MessageCollection.MSISDNValid;
                    return response;
                }

                // Get dedicated account IDs from config
                string[] dedicatedArr = SettingsValues.Getdedicated_Ac_Id_TOS()
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (dedicatedArr == null || dedicatedArr.Length == 0)
                {
                    response.message = MessageCollection.MSISDNValid;
                    return response;
                }

                foreach (var item in data)
                {
                    string? dedicatedId = item["attributes"]?["dedicated-account-id"]?.ToString();
                    string? amountStr = item["attributes"]?["amount"]?.ToString();

                    if (!string.IsNullOrEmpty(dedicatedId) &&
                        !string.IsNullOrEmpty(amountStr) &&
                        decimal.TryParse(amountStr, out decimal amount))
                    {
                        if (dedicatedArr.Any(x => x.Equals(dedicatedId)) && amount > 0)
                        {
                            response.dedicated_Ac_Id = dedicatedId;
                            response.amount = amount;
                            response.result = false;
                            response.message = $"Customer has loan amount: {amount} TK. Pls. recharge to do the TOS.";
                            return response;
                        }
                    }
                }

                response.message = MessageCollection.MSISDNValid;
                return response;
            }
            catch
            {
                throw;
            }
        }


        //public TOSLoanStatusResponse TosNiDtoNIDLoanStatusCheckParsing(JObject dbssRespObj)
        //{
        //    TOSLoanStatusResponse raResp = new TOSLoanStatusResponse();
        //    string dedicatedID = string.Empty;
        //    string[] dedicatedArr = null;
        //    raResp.result = true;
        //    try
        //    {
        //        if (!dbssRespObj["data"].HasValues
        //            || dbssRespObj["data"].Count() <= 0)
        //        {
        //            raResp.message = MessageCollection.MSISDNValid;
        //            return raResp;
        //        }
        //        int totalData = dbssRespObj["data"].Count();

        //        dedicatedID = SettingsValues.Getdedicated_Ac_Id_TOS();

        //        if (dedicatedID.Contains(','))
        //        {
        //            dedicatedArr = dedicatedID.Split(',');
        //        }
        //        else
        //        {
        //            dedicatedArr = dedicatedID.Split(' ');
        //        }

        //        for (int i = 0; i < totalData; i++)
        //        {
        //            if (!String.IsNullOrEmpty((string)dbssRespObj["data"][i]["attributes"]["dedicated-account-id"])
        //                        && !String.IsNullOrEmpty((string)dbssRespObj["data"][i]["attributes"]["amount"]))
        //            {
        //                raResp.dedicated_Ac_Id = (string)dbssRespObj["data"][i]["attributes"]["dedicated-account-id"];
        //                raResp.amount = (decimal)dbssRespObj["data"][i]["attributes"]["amount"];

        //                if (dedicatedArr.Any(x => x.Equals(raResp.dedicated_Ac_Id)) && raResp.amount > 0)
        //                {
        //                    raResp.result = false;
        //                    raResp.message = "Customer has loan amount: " + raResp.amount.ToString() + " TK. Pls. recharge to do the TOS.";
        //                    return raResp;
        //                }
        //            }
        //        }
        //        raResp.message = MessageCollection.MSISDNValid;
        //        return raResp;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public TosNidToNidMSISDNCheckResponse ValidateCherishedMSISDNforTOS(JObject dbssRespObj, string retailer_id)
        //{

        //    TosNidToNidMSISDNCheckResponse raResp = new TosNidToNidMSISDNCheckResponse();

        //    string status = String.Empty;
        //    int stockId = 0;
        //    string retailer_code = String.Empty;
        //    string number_category = String.Empty;
        //    string category_config = String.Empty;
        //    string[] cofigValue = null;

        //    try
        //    {
        //        if (dbssRespObj["data"] != null)
        //        {
        //            if (dbssRespObj["data"]["attributes"] != null)
        //            {
        //                category_config = SettingsValues.GetNumberCategory();

        //                if (category_config.Contains(","))
        //                {
        //                    cofigValue = category_config.Split(',');
        //                }
        //                else
        //                {
        //                    cofigValue = category_config.Split(' ');
        //                }

        //                if (dbssRespObj["data"]["attributes"]["number-category"] != null)
        //                {
        //                    retailer_code = dbssRespObj["data"]["attributes"]["salesman-id"].ToString();
        //                    number_category = dbssRespObj["data"]["attributes"]["number-category"].ToString();

        //                    if (!String.IsNullOrEmpty(retailer_code))
        //                    {
        //                        if (retailer_code.Length < 6)
        //                        {
        //                            char pad = '0';
        //                            retailer_code = retailer_code.PadLeft(6, pad);
        //                        }
        //                    }

        //                    if (!String.IsNullOrEmpty(retailer_code) && !String.IsNullOrEmpty(number_category) && cofigValue.Any(x => x != number_category)) // from Web.config 
        //                    {
        //                        if (retailer_id.Equals(retailer_code))
        //                        {
        //                            raResp.result = true;
        //                            raResp.message = "MSISDN is valid!";
        //                        }
        //                        else
        //                        {
        //                            raResp.result = false;
        //                            raResp.message = "Retailer is not eligible for Cherish Registration.";
        //                        }
        //                    }
        //                    else if (String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x == number_category))
        //                    {
        //                        raResp.result = true;
        //                        raResp.message = "MSISDN is valid!.";
        //                    }
        //                    else if (!String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x == number_category))
        //                    {
        //                        raResp.result = true;
        //                        raResp.message = "MSISDN is valid!.";
        //                    }
        //                    else if (String.IsNullOrEmpty(retailer_code) && cofigValue.Any(x => x != number_category))
        //                    {
        //                        raResp.result = false;
        //                        raResp.message = "salesman-id is null.";
        //                    }
        //                    else
        //                    {
        //                        raResp.result = false;
        //                        raResp.message = "MSISDN not Valid.";
        //                    }
        //                }
        //                else
        //                {
        //                    raResp.result = false;
        //                    raResp.message = "number-category is Empty!";
        //                }
        //            }
        //            else
        //            {
        //                raResp.result = false;
        //                raResp.message = "attributes is empty!";
        //            }
        //        }
        //        else
        //        {
        //            raResp.result = false;
        //            raResp.message = "data is Empty!";
        //        }

        //        return raResp;
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        #region NEW PARSING SYSTEM AFTER FIXATION    
        public TosNidToNidMSISDNCheckResponse TosNidToNidMSISDNReqParsingV2(JObject dbssRespObj)
        {
            var raResp = new TosNidToNidMSISDNCheckResponse();

            try
            {
                var data = dbssRespObj["data"];
                var included = dbssRespObj["included"] as JArray;

                if (data == null || !data.HasValues || data.Count() <= 0)
                    throw new Exception(MessageCollection.SIMReplNoDataFound);

                var attributes = data["attributes"];
                if (string.IsNullOrEmpty((string?)attributes?["status"]))
                    throw new Exception("Msisdn status not found!");

                string status = attributes["status"]?.ToString() ?? "";
                if (status == "terminated")
                    throw new Exception("Msisdn is not valid for TOS!");

                if (status != "active" && status != "idle")
                    throw new Exception(MessageCollection.MSISDNStatusNotActiveOrIdle);

                if (included == null || !included.HasValues || included.Count < 2)
                    throw new Exception("Customer info or SIM cards info missing in include field!");

                if (data["id"] == null)
                    throw new Exception("Subscription ID field empty!");

                var paymentType = (string?)attributes["payment-type"];
                if (string.IsNullOrEmpty(paymentType))
                    throw new Exception("Source customer payment type not found!");

                if (included[0]["attributes"] == null || included[1]["attributes"] == null)
                    throw new Exception("Data not found in include field!");

                string simCardsId = (string?)data["relationships"]?["sim-cards"]?["data"]?[0]?["id"]
                    ?? throw new Exception("SIM card ID missing!");
                string ownerCustomerId = (string?)data["relationships"]?["owner-customer"]?["data"]?["id"]
                    ?? throw new Exception("Owner customer ID missing!");
                string userCustomerId = (string?)data["relationships"]?["user-customer"]?["data"]?["id"]
                    ?? throw new Exception("User customer ID missing!");
                string payerCustomerId = (string?)data["relationships"]?["payer-customer"]?["data"]?["id"]
                    ?? throw new Exception("Payer customer ID missing!");

                // Validate and set indexes
                int ownerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(included, ownerCustomerId);
                int simIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(included, simCardsId);
                var info = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerIndex, simIndex);

                raResp.dob = info.dob;
                raResp.doc_id_number = info.doc_id_number;
                raResp.dbss_subscription_id = info.dbss_subscription_id;
                raResp.old_sim_number = info.old_sim_number;
                raResp.old_sim_type = info.old_sim_type;

                raResp.src_sim_category = paymentType switch
                {
                    "prepaid" => (int)EnumSimCategory.Prepaid,
                    "postpaid" => (int)EnumSimCategory.Postpaid,
                    _ => throw new Exception("Unknown source customer payment type (SIM category)!")
                };

                bool ocEqUc = ownerCustomerId == userCustomerId;
                bool ocEqPc = ownerCustomerId == payerCustomerId;
                bool ucEqPc = userCustomerId == payerCustomerId;

                if (included.Count == 2 && ocEqUc && ucEqPc)
                {
                    raResp.src_owner_customer_id = raResp.src_user_customer_id = raResp.src_payer_customer_id = ownerCustomerId;
                }
                else if (included.Count == 3)
                {
                    if (!ocEqUc && !ocEqPc && ucEqPc)
                    {
                        raResp.src_owner_customer_id = ownerCustomerId;
                        raResp.src_user_customer_id = raResp.src_payer_customer_id = userCustomerId;
                    }
                    else if (ocEqPc && !ocEqUc)
                    {
                        raResp.src_owner_customer_id = raResp.src_payer_customer_id = ownerCustomerId;
                        raResp.src_user_customer_id = userCustomerId;
                    }
                    else if (ocEqUc && !ucEqPc)
                    {
                        raResp.src_owner_customer_id = raResp.src_user_customer_id = ownerCustomerId;
                        raResp.src_payer_customer_id = payerCustomerId;
                    }
                    else
                    {
                        throw new Exception("Invalid DBSS Response!");
                    }
                }
                else if (included.Count == 4 && !ocEqUc && !ucEqPc && !ocEqPc)
                {
                    raResp.src_owner_customer_id = ownerCustomerId;
                    raResp.src_user_customer_id = userCustomerId;
                    raResp.src_payer_customer_id = payerCustomerId;
                }
                else
                {
                    throw new Exception("Invalid DBSS Response!");
                }

                raResp.result = true;
                raResp.message = MessageCollection.MSISDNValid;
                return raResp;
            }
            catch
            {
                throw;
            }
        }

        //public TosNidToNidMSISDNCheckResponse TosNidToNidMSISDNReqParsingV2(JObject dbssRespObj)
        //{
        //    TosNidToNidMSISDNCheckResponse raResp = new TosNidToNidMSISDNCheckResponse();
        //    try
        //    {
        //        if (!dbssRespObj["data"].HasValues
        //            || dbssRespObj["data"].Count() <= 0)
        //        {
        //            throw new Exception(MessageCollection.SIMReplNoDataFound);
        //        }

        //        if (String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["status"]))
        //        {
        //            throw new Exception("Msisdn status not found!");
        //        }

        //        if ((string)dbssRespObj["data"]["attributes"]["status"] == "terminated")
        //        {
        //            throw new Exception("Msisdn is not valid for TOS!");
        //        }

        //        if ((string)dbssRespObj["data"]["attributes"]["status"] != "active"
        //             && (string)dbssRespObj["data"]["attributes"]["status"] != "idle")
        //        {
        //            throw new Exception(MessageCollection.MSISDNStatusNotActiveOrIdle);
        //        }

        //        if (!dbssRespObj["included"].HasValues)
        //        {
        //            throw new Exception("Data not found in include field!");
        //        }

        //        if (dbssRespObj["included"].Count() < 2)
        //        {
        //            throw new Exception("Customer info or SIM cards info missing in include field!");
        //        }
        //        if (dbssRespObj["data"]["id"] == null)
        //        {
        //            throw new Exception("Subscription ID field empty!");
        //        }

        //        if (String.IsNullOrEmpty((string)dbssRespObj["data"]["attributes"]["payment-type"]))
        //        {
        //            throw new Exception("Source customer payment type not found!");
        //        }

        //        if (dbssRespObj["included"][0]["attributes"] == null
        //            || dbssRespObj["included"][1]["attributes"] == null)
        //        {
        //            throw new Exception("Data not found in include field!");
        //        }

        //        //==================
        //        string src_sim_cards_id;
        //        string src_owner_customer_id;
        //        string src_user_customer_id;
        //        string src_payer_customer_id;
        //        string src_sim_category;
        //        try
        //        {
        //            src_sim_cards_id = (string)dbssRespObj["data"]["relationships"]["sim-cards"]["data"][0]["id"];
        //            src_owner_customer_id = (string)dbssRespObj["data"]["relationships"]["owner-customer"]["data"]["id"];
        //            src_user_customer_id = (string)dbssRespObj["data"]["relationships"]["payer-customer"]["data"]["id"];
        //            src_payer_customer_id = (string)dbssRespObj["data"]["relationships"]["user-customer"]["data"]["id"];
        //            src_sim_category = (string)dbssRespObj["data"]["attributes"]["payment-type"];
        //        }
        //        catch (Exception)
        //        {
        //            throw new Exception("Required data not found in relationships field!");
        //        }

        //        switch (dbssRespObj["included"].Count())
        //        {
        //            case 0:
        //                throw new Exception("Required data not found in include field!");

        //            case 1:
        //                throw new Exception("Required data not found in include field!");

        //            case 2: //oc=uc=pc, sim_cards
        //                if (src_owner_customer_id.Equals(src_user_customer_id)
        //                    && src_user_customer_id.Equals(src_payer_customer_id))
        //                {

        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);

        //                    raResp.dob = customerAndSimCardsInfo.dob;
        //                    raResp.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.src_owner_customer_id = raResp.src_user_customer_id = raResp.src_payer_customer_id = src_owner_customer_id;

        //                    if (src_sim_category == "prepaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.result = true;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;

        //                }
        //                else
        //                {
        //                    throw new Exception("Invalid DBSS Repponse!");
        //                }

        //            case 3: //oc, uc=pc, sim_cards || oc=pc, uc, sim_cards || oc=uc, pc, sim_cards
        //                if (!src_owner_customer_id.Equals(src_user_customer_id)
        //                    && !src_owner_customer_id.Equals(src_payer_customer_id)
        //                    && src_user_customer_id.Equals(src_payer_customer_id))
        //                {

        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);

        //                    raResp.dob = customerAndSimCardsInfo.dob;
        //                    raResp.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.src_owner_customer_id = src_owner_customer_id;
        //                    raResp.src_user_customer_id = raResp.src_payer_customer_id = src_user_customer_id;

        //                    if (src_sim_category == "prepaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.result = true;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;

        //                }
        //                //oc = pc, uc, sim_cards
        //                else if (src_owner_customer_id.Equals(src_payer_customer_id)
        //                            && !src_owner_customer_id.Equals(src_user_customer_id))
        //                {

        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);

        //                    raResp.dob = customerAndSimCardsInfo.dob;
        //                    raResp.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.src_owner_customer_id = raResp.src_payer_customer_id = src_owner_customer_id;
        //                    raResp.src_user_customer_id = src_user_customer_id;


        //                    if (src_sim_category == "prepaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.result = true;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;

        //                }
        //                //oc=uc, pc, sim_cards
        //                else if (src_owner_customer_id.Equals(src_user_customer_id)
        //                    && !src_user_customer_id.Equals(src_payer_customer_id))
        //                {
        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);

        //                    raResp.dob = customerAndSimCardsInfo.dob;
        //                    raResp.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.src_owner_customer_id = raResp.src_user_customer_id = src_owner_customer_id;
        //                    raResp.src_payer_customer_id = src_payer_customer_id;


        //                    if (src_sim_category == "prepaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.result = true;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;
        //                }
        //                else
        //                {
        //                    throw new Exception("Invalid DBSS Repponse!");
        //                }


        //            case 4: //ow, uc, pc, sim_cards
        //                if (!src_owner_customer_id.Equals(src_user_customer_id)
        //                    && !src_user_customer_id.Equals(src_payer_customer_id)
        //                    && !src_owner_customer_id.Equals(src_payer_customer_id))
        //                {
        //                    int ownerCustomerIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_owner_customer_id);
        //                    int simCardsIndex = getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(dbssRespObj["included"] as JArray, src_sim_cards_id);

        //                    VMOwnerCustomerAndSimCardsInfo customerAndSimCardsInfo = getOwnerCustomerandSimCardsInfo(dbssRespObj, ownerCustomerIndex, simCardsIndex);
        //                    raResp.dob = customerAndSimCardsInfo.dob;
        //                    raResp.doc_id_number = customerAndSimCardsInfo.doc_id_number;
        //                    raResp.dbss_subscription_id = customerAndSimCardsInfo.dbss_subscription_id;
        //                    raResp.old_sim_number = customerAndSimCardsInfo.old_sim_number;
        //                    raResp.old_sim_type = customerAndSimCardsInfo.old_sim_type;
        //                    raResp.src_owner_customer_id = src_owner_customer_id;
        //                    raResp.src_user_customer_id = src_user_customer_id;
        //                    raResp.src_payer_customer_id = src_payer_customer_id;

        //                    if (src_sim_category == "prepaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Prepaid;
        //                    else if (src_sim_category == "postpaid")
        //                        raResp.src_sim_category = (int)EnumSimCategory.Postpaid;
        //                    else
        //                        throw new Exception("Unknown source customer payment type (SIM categoty)!");

        //                    raResp.result = true;
        //                    raResp.message = MessageCollection.MSISDNValid;
        //                    return raResp;
        //                }
        //                else
        //                {
        //                    throw new Exception("Invalid DBSS Repponse!");
        //                }
        //        }
        //        return raResp;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
        #endregion

        private VMOwnerCustomerAndSimCardsInfo getOwnerCustomerandSimCardsInfo(JObject obj,
                                                                        int ownerCustomerPropertyIndex,
                                                                        int simCardsPropertyIndex)
        {
            try
            {
                string? icc = (string?)obj["included"]?[simCardsPropertyIndex]?["attributes"]?["icc"];
                string? simType = (string?)obj["included"]?[simCardsPropertyIndex]?["attributes"]?["sim-type"];
                bool? isCompany = (bool?)obj["included"]?[ownerCustomerPropertyIndex]?["attributes"]?["is-company"];
                string? idDocumentType = (string?)obj["included"]?[ownerCustomerPropertyIndex]?["attributes"]?["id-document-type"];

                if (string.IsNullOrEmpty(icc))
                    throw new Exception("Old SIM number not found!");

                if (string.IsNullOrEmpty(simType))
                    throw new Exception("sim-type not found!");

                if (isCompany == null)
                    throw new Exception("Company information not found!");

                if (string.IsNullOrEmpty(idDocumentType))
                    throw new Exception("id-document-type not found!");

                if (idDocumentType != "national_id" && idDocumentType != "smart_national_id")
                    throw new Exception("Customer is not registered with National ID!");

                if (isCompany == true)
                    throw new Exception("This MSISDN is not eligible for individual SIM replacement.");

                return new VMOwnerCustomerAndSimCardsInfo
                {
                    dob = (string?)obj["included"]?[ownerCustomerPropertyIndex]?["attributes"]?["date-of-birth"] ?? "",
                    doc_id_number = (string?)obj["included"]?[ownerCustomerPropertyIndex]?["attributes"]?["id-document-number"] ?? "",
                    dbss_subscription_id = (int?)obj["data"]?["id"] ?? throw new Exception("Subscription ID not found!"),
                    old_sim_number = icc,
                    old_sim_type = simType
                };
            }
            catch
            {
                throw;
            }
        }

        #endregion

        private int getIndexNumberByObjectPropertyIdFromIncludeTagForGetSubscriptionByMsisdnDbssResponse(JArray array, string id)
        {
            int index = 0;
            try
            {
                foreach (var item in array.Children())
                {
                    if (item["id"]?.ToString() == id)
                    {
                        break;
                    }
                    index++;
                }
                return index;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public GetImsiRespObj GetImsiRespParsingAsync(JObject dbssRespObj)
        {
            GetImsiRespObj raResp = new GetImsiRespObj();
            string imsi = String.Empty;

            if (dbssRespObj["data"] != null)
            {
                if (dbssRespObj["data"]?["attributes"] != null)
                {
                    if (dbssRespObj["data"]?["attributes"]?["imsi"] != null)
                    {
                        imsi = dbssRespObj["data"]?["attributes"]?["imsi"]?.ToString() ?? "";
                    }
                }
            }

            if (String.IsNullOrEmpty(imsi))
            {
                raResp.result = false;
                raResp.message = "IMSI not found!";
            }
            else
            {
                raResp.result = true;
                raResp.imsi = imsi;
                raResp.message = MessageCollection.Success;
            }
            return raResp;
        }

        public List<ReponseData> UnpairedMSISDNListDataParsing(List<object> dbssRespModel)
        {
            List<ReponseData> reponseList = new List<ReponseData>();
            try
            {
                for (int i = 0; i < dbssRespModel.Count; i++)
                {
                    JObject rss = JObject.Parse(dbssRespModel[i].ToString() ?? "");
                    ReponseData raResp = new ReponseData();
                    raResp.msisdn = rss["attributes"]?["msisdn"]?.ToString() ?? "";
                    reponseList.Add(raResp);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return reponseList;
        }

        public List<ReponseDataRev> UnpairedMSISDNListDataParsingV2(List<object> dbssRespModel)
        {
            List<ReponseDataRev> reponseList = new List<ReponseDataRev>();
            try
            {
                for (int i = 0; i < dbssRespModel.Count; i++)
                {


                    JObject rss = JObject.Parse(dbssRespModel[i].ToString() ?? "");
                    ReponseDataRev raResp = new ReponseDataRev();
                    raResp.msisdn = rss["attributes"]?["msisdn"]?.ToString() ?? "";

                    reponseList.Add(raResp);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return reponseList;
        }

        public List<SIMReponseData> UnpairedSIMListDataParsing(List<object> dmsRespModel)
        {
            List<SIMReponseData> reponseList = new List<SIMReponseData>();
            try
            {
                for (int i = 0; i < dmsRespModel.Count; i++)
                {
                    JObject rss = JObject.Parse(dmsRespModel[i].ToString() ?? "");
                    SIMReponseData raResp = new SIMReponseData();
                    raResp.sim_serial = rss["sim_serial"]?.ToString() ?? "";
                    reponseList.Add(raResp);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return reponseList;
        }

        public List<SIMReponseDataRev> UnpairedSIMListDataParsingV2(List<object> dmsRespModel)
        {
            List<SIMReponseDataRev> reponseList = new List<SIMReponseDataRev>();
            try
            {
                for (int i = 0; i < dmsRespModel.Count; i++)
                {
                    JObject rss = JObject.Parse(dmsRespModel[i].ToString() ?? "");
                    SIMReponseDataRev raResp = new SIMReponseDataRev();
                    raResp.sim_serial = rss["sim_serial"]?.ToString() ?? "";
                    reponseList.Add(raResp);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return reponseList;
        }

        public async Task<string> GetStockResponses(string channelName)
        {
            StockResponse stock = new StockResponse();
            try
            {
                DataTable data = await _dataManager.GetStockAvailable(channelName);

                if (data.Rows.Count > 0)
                {
                    for (int i = 0; i < data.Rows.Count; i++)
                    {
                        stock.channelId = Convert.ToString(data.Rows[i]["CHANNELID"] == DBNull.Value ? null : data.Rows[i]["CHANNELID"]) ?? "";
                    }
                }

            }
            catch (Exception)
            {
                throw;
            }

            return stock.channelId;
        }

        public RACommonResponse SIMValidationParsingForCherish(JObject dbssResp, int purposeOfSIMCheck, int? simCategory, bool? isPired, string oldSimType, int channel_id)
        {
            RACommonResponse response = new RACommonResponse();
            try
            {
                if (dbssResp?["data"]?["status"] == null
                    && dbssResp?["data"]?["logical_inventory_status"] == null
                    && dbssResp?["data"]?["physical_inventory_status"] == null)
                {
                    response.result = false;
                    response.message = MessageCollection.DataNotFound;
                    return response;
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESim.ToLower() /*e-sim*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeE_SIM_SWAP.ToLower() /*e_sim_swap*/)
                {
                    {
                        response.result = false;
                        response.message = "This is not Physical SIM.";
                        return response;
                    }
                }
                else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaidStarTrek.ToLower() /*ryz-prepaid*/
                    || dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypeESimStarTrek.ToLower() /*ryz-esim*/)
                {
                    {
                        response.result = false;
                        response.message = "Please try with correct SIM card";
                        return response;
                    }
                }
                else if (dbssResp?["data"]?["status"]?.ToString().ToLower() == "failed")
                {
                    response.result = false;

                    var errorMessage = dbssResp?["data"]?["error_message"]?.ToString();
                    response.message = !string.IsNullOrWhiteSpace(errorMessage)
                                        ? errorMessage
                                        : MessageCollection.SIMIsNotInInventory;
                    return response;
                }
                else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == "used")
                {
                    response.result = false;
                    response.message = MessageCollection.SIMIsUsed;
                    return response;
                }

                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && channel_id == 7)
                {
                    if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower() /*prepaid*/)
                    {
                        {
                            response.result = false;
                            response.message = "Please try with correct SIM card";
                            return response;
                        }
                    }
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower()/*"postpaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAnUnpairedSIM;
                        return response;
                    }
                    else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePostpaid.ToLower()/*"postpaid"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPostpaidSIM;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }

                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Prepaid
                    && isPired == true)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PairedMSISDN.ToLower()/*"paired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower() /*"prepaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PairedMSISDN.ToLower() /*"paired"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPairedSIM;
                        return response;
                    }
                    else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePrepaid.ToLower() /*"prepaid"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPrepaidSIM;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }

                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Postpaid
                    && isPired == true)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PairedMSISDN.ToLower()/*"paired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower() /*"postpaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PairedMSISDN.ToLower()/*"paired"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPairedSIM;
                        return response;
                    }
                    else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePostpaid.ToLower() /*"postpaid"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPostpaidSIM;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }

                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Prepaid
                    && isPired == false)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePrepaid.ToLower() /*"prepaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.UnairedMSISDN.ToLower() /*"unpaired"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAnUnpairedSIM;
                        return response;
                    }
                    else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePrepaid.ToLower()/*"prepaid"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPrepaidSIM;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }

                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.NewConnection
                    && simCategory == (int)EnumSimCategory.Postpaid
                    && isPired == false)
                {
                    if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/
                        && dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PaymentTypePostpaid.ToLower()/*"postpaid"*/)
                    {
                        response.result = true;
                        response.message = MessageCollection.SIMValid;
                        return response;
                    }
                    else if (dbssResp?["data"]?["logical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.UnairedMSISDN.ToLower()/*"unpaired"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAnUnpairedSIM;
                        return response;
                    }
                    else if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() != FixedValueCollection.PaymentTypePostpaid.ToLower()/*"postpaid"*/)
                    {
                        response.result = false;
                        response.message = MessageCollection.NotAPostpaidSIM;
                        return response;
                    }
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMInvalid;
                        return response;
                    }
                }
                //----------------SIMReplacement--------------
                else if (purposeOfSIMCheck == (int)EnumPurposeOfSIMCheck.SIMReplacement
                    && !String.IsNullOrEmpty(oldSimType))
                {
                    if (oldSimType.ToLower() == FixedValueCollection.SIMTypeUSIM /*"usim"*/)
                    {
                        if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLower() /*"sim_swap"*/)
                        {
                            response.result = true;
                            response.message = MessageCollection.SIMValid;
                            return response;
                        }
                        else
                        {
                            response.result = false;
                            response.message = MessageCollection.NotASwapSIM;
                            return response;
                        }
                    }
                    else if (oldSimType.ToLower() == FixedValueCollection.SIMTypeSIM/*"sim"*/)
                    {
                        if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeEV_SWAP.ToLower() /*"ev_swap"*/)
                        {
                            response.result = true;
                            response.message = MessageCollection.SIMValid;
                            return response;
                        }
                        else
                        {
                            response.result = false;
                            response.message = MessageCollection.NotAEVSwapSIM;
                            return response;
                        }
                    }
                    //============New SIM Type "PLI" Added=======
                    else if (oldSimType.ToLower() == FixedValueCollection.SIMTypePLI/*"pli"*/)
                    {
                        if (dbssResp?["data"]?["physical_inventory_status"]?.ToString().ToLower() == FixedValueCollection.PhycalInventorySIMTypeSIM_SWAP.ToLower() /*"ev_swap"*/)
                        {
                            response.result = true;
                            response.message = MessageCollection.SIMValid;
                            return response;
                        }
                        else
                        {
                            response.result = false;
                            response.message = MessageCollection.NotASwapSIM;
                            return response;
                        }
                    }
                    //==========x============
                    else
                    {
                        response.result = false;
                        response.message = MessageCollection.SIMTypeIsNotSIMOrUSIM;
                        return response;
                    }
                }
                else
                {
                    response.result = false;
                    response.message = MessageCollection.InvalidAttempt + " while checking SIM!";
                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        #region Cherish Number Sell
        public UnpairedMSISDNCheckResponse MSISDNReqParsingCherish(JObject dbssRespObj, string retailer_id, string selectedCategory)
        {
            UnpairedMSISDNCheckResponse raResp = new UnpairedMSISDNCheckResponse();
            try
            {
                string status = String.Empty;
                int stockId = 0;
                string retailer_code = String.Empty;
                string number_category = String.Empty;
                string category_config = String.Empty;
                string[] cofigValue = Array.Empty<string>();
                string reserved_for = string.Empty;

                if (dbssRespObj["data"] != null)
                {
                    if (dbssRespObj["data"]?["attributes"] != null)
                    {
                        if (!String.IsNullOrEmpty((string?)dbssRespObj["data"]?["attributes"]?["status"])
                            && !String.IsNullOrEmpty((string?)dbssRespObj["data"]?["attributes"]?["stock"]))
                        {
                            status = (string?)dbssRespObj["data"]?["attributes"]?["status"] ?? "";
                            stockId = Convert.ToInt32(dbssRespObj["data"]?["attributes"]?["stock"]);
                            reserved_for = (string?)dbssRespObj["data"]?["attributes"]?["reserved-for"] ?? "";
                            number_category = (string?)dbssRespObj["data"]?["attributes"]?["number-category"] ?? "";
                        }
                    }
                }
                if (selectedCategory.ToLower() != number_category.ToLower())
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.CherishCategoryMismatch;
                    return raResp;
                }
                if (stockId == 33)
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.StockIDMismatch;
                    return raResp;
                }
                if (!String.IsNullOrEmpty(reserved_for))
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNReserved;
                    return raResp;
                }
                if (status == "available")
                {
                    raResp.result = true;
                    raResp.stock_id = stockId;
                    return raResp;
                }
                else if (status == "in_use")
                {
                    raResp.result = false;
                    raResp.message = MessageCollection.MSISDNInUse;
                    return raResp;
                }
                else
                {
                    raResp.result = false;
                    raResp.message = "MSISDN is invalid.";
                    return raResp;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public PaiedMSISDNCheckResponseDataRevV1 PairedMSISDNReqParsingV4(PairedMSISDNValidationResponseRootobject dbssRespObj)
        {
            PaiedMSISDNCheckResponseDataRevV1 raResp = new PaiedMSISDNCheckResponseDataRevV1();
            string simNo = String.Empty;
            try
            {
                if (dbssRespObj.data.attributes == null)
                {
                    raResp.isError = true;
                    raResp.message = MessageCollection.DataNotFound;
                    return raResp;
                }

                if (String.IsNullOrEmpty(dbssRespObj.data.attributes.msisdn)
                    || String.IsNullOrEmpty(dbssRespObj.data.attributes.status)
                    || String.IsNullOrEmpty(dbssRespObj.data.attributes.icc)
                    || String.IsNullOrEmpty(dbssRespObj.data.attributes.subscriptionType)
                    )
                {
                    raResp.isError = true;
                    raResp.message = MessageCollection.DataNotFound;
                    return raResp;
                }

                if (dbssRespObj.data.attributes.status != FixedValueCollection.ValidPairedMSISDNStatus)
                {
                    raResp.isError = true;
                    raResp.message = MessageCollection.MSISDNInvalid;
                    return raResp;
                }

                raResp.isError = false;
                if (dbssRespObj.data != null)
                {
                    raResp.data = new PaiedMSISDNCheckResponseRevV1()
                    {
                        sim_number = dbssRespObj.data.attributes.icc.Remove(0, FixedValueCollection.SIMCode.Length),
                        subscription_type_code = dbssRespObj.data.attributes.subscriptionType,
                        imsi = dbssRespObj.data.attributes.imsi,
                        number_category = dbssRespObj.data.attributes.numbercategory,
                        category = dbssRespObj.data.attributes.numbercategory
                    };
                }
                raResp.message = MessageCollection.MSISDNValid;
            }
            catch (Exception)
            {
                throw;
            }
            return raResp;
        }

        public async Task<List<PackagesReponseDataRev>> PackagesParsingV3(List<object> dbssRespModel, string category)
        {

            List<PackagesReponseDataRev> packages = new List<PackagesReponseDataRev>();
            try
            {
                string mintAmount = await _bllCommon.GetCategoryMinAmount(category);

                if (dbssRespModel != null)
                {
                    for (int i = 0; i < dbssRespModel.Count; i++)
                    {
                        JObject rss = JObject.Parse(dbssRespModel[i].ToString());

                        PackagesReponseDataRev raResp = new PackagesReponseDataRev();

                        string typeProducts = !String.IsNullOrEmpty(rss["type"].ToString()) ? Convert.ToString(rss["type"]) : "";

                        if (!String.IsNullOrEmpty(typeProducts))
                        {
                            if (typeProducts.Equals("subscription-type-products"))
                            {
                                string productPrice = Convert.ToString((string)rss["attributes"]["price"]);
                                double pp = Convert.ToDouble(productPrice);

                                if (pp >= Convert.ToInt32(mintAmount))
                                {
                                    string typeId = (string)rss["id"];

                                    if (typeId.Contains("-"))
                                    {
                                        int lastIndex = typeId.LastIndexOf('-');
                                        string productId = typeId.Substring(lastIndex + 1);

                                        for (int j = 0; j < dbssRespModel.Count; j++)
                                        {
                                            JObject rssSecond = JObject.Parse(dbssRespModel[j].ToString());

                                            string pkId = (string)rssSecond["id"];

                                            if (productId.Equals(pkId))
                                            {
                                                if (rss["id"] != null && rssSecond["attributes"]["code"] != null)
                                                {
                                                    raResp.package_id = (string)rssSecond["id"];
                                                    raResp.package_name = (string)rssSecond["attributes"]["code"];
                                                    packages.Add(raResp);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return packages;
        }
        #endregion
        //}

        public DateTime? ParseActivationDateFromNinetyDaysApi(JObject response)
        {
            try
            {
                var activationStr = response["data"]?[0]?["attributes"]?["activation-time"]?.ToString();
                if (!string.IsNullOrEmpty(activationStr) && DateTime.TryParse(activationStr, out DateTime activationDate))
                {
                    return activationDate;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public ParsedBillingAccountInfo ParseBillingAccountInfo(JObject response)
        {
            var parsedBillingAccountInfo = new ParsedBillingAccountInfo();

            try
            {
                if (response["data"] is not JArray dataArray || !dataArray.HasValues)
                {
                    return new ParsedBillingAccountInfo
                    {
                        isError = true,
                        message = "combined-usage-reports API response has no value!"
                    };
                }

                // Find first active subscription id
                string? subscriptionId = dataArray
                    .FirstOrDefault(x => x?["attributes"]?["status"]?.ToString() == "active")?["id"]?.ToString();

                if (response["included"] is not JArray includedArray || !includedArray.HasValues)
                {
                    return new ParsedBillingAccountInfo
                    {
                        isError = true,
                        message = "combined-usage-reports API response has no value!"
                    };
                }

                // Find the first "local" billing account
                var localAcc = includedArray
                    .FirstOrDefault(x => x?["attributes"]?["billing-account-type"]?.ToString() == "local");

                if (localAcc != null)
                {
                    parsedBillingAccountInfo = new ParsedBillingAccountInfo
                    {
                        SubscriptionId = subscriptionId,
                        BillingAccountType = localAcc["attributes"]?["billing-account-type"]?.ToString(),
                        BillingAccountId = localAcc["id"]?.ToString(),
                        isError = false,
                        message = "API response success!"
                    };
                }
                else
                {
                    parsedBillingAccountInfo = new ParsedBillingAccountInfo
                    {
                        isError = true,
                        message = "No local billing account found!"
                    };
                }

                return parsedBillingAccountInfo;
            }
            catch
            {
                return new ParsedBillingAccountInfo
                {
                    isError = true,
                    message = "Exception occurred while parsing API response."
                };
            }
        }

        public TOSBillingReportResponse ParseTOSBillingReport(JObject billingResp, string billingAccountId)
        {
            var response = new TOSBillingReportResponse();

            try
            {
                response.Result = false;
                if (billingResp["data"] is not JArray attributesValues)
                    return response;

                foreach (var acc in attributesValues)
                {
                    var billingid = acc["relationships"]?["billing-account"]?["data"]?["id"]?.ToString();

                    if (billingid == billingAccountId)
                    {
                        response.Debt = acc["attributes"]?["debt"]?.Value<decimal>() ?? 0m;
                        response.Unbilled = acc["attributes"]?["unbilled"]?.Value<decimal>() ?? 0m;
                        response.Deposit = acc["attributes"]?["deposits"]?["default"]?.Value<decimal>() ?? 0m;
                        response.Result = true;
                        break;
                    }
                }

                return response;
            }
            catch
            {
                return response;
            }
        }

        public ParsedPrepaidBalanceInfo? ParsePrepaidBalanceResponse(JObject response)
        {
            try
            {
                var included = response["included"] as JArray;
                if (included == null || !included.HasValues)
                    return null;

                var mainBalance = included.FirstOrDefault(x =>
                    x["attributes"]?["is-main-balance"]?.ToObject<bool>() == true);

                if (mainBalance == null)
                    return null;

                decimal amount = decimal.Parse(mainBalance["attributes"]?["amount"]?.ToString() ?? "0");

                string supervisionDateStr = mainBalance["attributes"]?["lifecycle"]?["supervision-expiry-date"]?.ToString() ?? string.Empty;
                DateTime? supervisionExpiryDate = null;

                if (!string.IsNullOrEmpty(supervisionDateStr) && DateTime.TryParse(supervisionDateStr, out DateTime parsedDate))
                {
                    supervisionExpiryDate = parsedDate;
                }

                return new ParsedPrepaidBalanceInfo
                {
                    Amount = amount,
                    SupervisionExpiryDate = supervisionExpiryDate
                };
            }
            catch
            {
                return null;
            }
        }

    }
}
