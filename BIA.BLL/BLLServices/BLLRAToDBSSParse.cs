using BIA.BLL.Utility;
using BIA.Entity.Collections;
using BIA.Entity.ENUM;
using BIA.Entity.RequestEntity;

namespace BIA.BLL.BLLServices
{
    public class BLLRAToDBSSParse
    {
        public UnreserveMSISDNRequestRootobject UnreserveMSISDNReqParsing(string biTokenNumber)
        {
            UnreserveMSISDNRequestRootobject rootobject = new UnreserveMSISDNRequestRootobject();
            try
            {
                rootobject = new UnreserveMSISDNRequestRootobject()
                {
                    data = new UnreserveMSISDNRequestData()
                    {
                        id = biTokenNumber.Trim()
                    }
                };
            }
            catch (Exception)
            {
                throw;
            }
            return rootobject;
        }

        public UnreserveMSISDNRequestRootobject UnreserveMSISDNPopulate(string reservationId)
        {
            UnreserveMSISDNRequestRootobject rootobject = new UnreserveMSISDNRequestRootobject();
            try
            {
                rootobject = new UnreserveMSISDNRequestRootobject()
                {
                    data = new UnreserveMSISDNRequestData()
                    {
                        id = reservationId.Trim()
                    }
                };
            }
            catch (Exception)
            {
                throw;
            }
            return rootobject;
        }


        public QCStatusUpdateRequestRootobject QCStatusUpdateReqParsing(string quality_control_id, string retailer_id)
        {
            QCStatusUpdateRequestRootobject reqRootObj = new QCStatusUpdateRequestRootobject();
            try
            {
                reqRootObj = new QCStatusUpdateRequestRootobject()
                {
                    data = new QCStatusUpdateRequestData()
                    {
                        id = quality_control_id,
                        type = "quality-controls",
                        attributes = new QCStatusUpdateRequestAttributes()
                        {
                            status = FixedValueCollection.QCStatusUnverified,
                            reseller = retailer_id
                        }
                    }
                };
            }
            catch (Exception)
            {
                throw;
            }
            return reqRootObj;
        }


        public CustomerInfoUpdaterRequestRootobject CustomerInfoReqParsing(RACustomerInfoUpdateRequest reqModel)
        {
            CustomerInfoUpdaterRequestRootobject requestRootobject = new CustomerInfoUpdaterRequestRootobject();
            try
            {
                requestRootobject = new CustomerInfoUpdaterRequestRootobject()
                {
                    data = new CustomerInfoUpdaterRequestData()
                    {
                        id = reqModel.customer_id ?? "",
                        type = "customers",
                        attributes = new CustomerInfoUpdaterRequestAttributes()
                        {
                            firstname = reqModel.customer_name ?? "",
                            email = reqModel.email ?? "",
                            alt_contact_phone = reqModel.alt_msisdn ?? "",

                            gender = !String.IsNullOrEmpty(reqModel.gender) ? reqModel.gender.ToLower() : reqModel.gender ?? "",

                            legaladdress = new CustomerInfoUpdaterRequestLegalAddress()
                            {
                                type = "DEF", //here 'DEF' is hard coded static value [according to  DBSS Spec].
                                area = reqModel.village ?? "",
                                country = "BD",
                                district = ConverterHelper.UpperLowerWithSpaceConverter(reqModel.district_name ?? ""),
                                division = ConverterHelper.UpperLowerWithSpaceConverter(reqModel.division_name ?? ""),
                                flatnumber = reqModel.flat_number ?? "",
                                housenumber = reqModel.house_number ?? "",
                                postcode = String.IsNullOrEmpty(reqModel.postal_code) ? "0" : reqModel.postal_code,
                                road = reqModel.road_number ?? "",
                                thana = ConverterHelper.UpperLowerWithSpaceConverter(reqModel.thana_name ?? "")
                            }
                        }
                    }
                };
            }
            catch (Exception)
            {
                throw;
            }

            return requestRootobject;
        }


        public string ValidateMSISDNReqParsing(MSISDNCheckRequest model)
        {
            return model.mobile_number;
        }
         
        public string ValidateMSISDNReqParsingMNP(unpairedMSISDNCheckReq model)
        {
            return model.mobile_number;
        }

        public string ValidateMSISDNReqParsingHomeWifi(MNPValidationRequestModel model)
        {
            return model.mobile_number;
        }

        public SIMValidationRequestRootobject ValidateSIMReqParsing(SIMNumberCheckRequest model)
        {
            SIMValidationRequestRootobject rootobject = new SIMValidationRequestRootobject();
            SIMValidationRequestAttributes attributes = new SIMValidationRequestAttributes("","","","","");
            try
            {
                //string distCode = bLLCommon.GetDistributorCodeFromSessionToken(model.session_token);
                //if (model.sim_number.Substring(0, FixedValueCollection.SIMCode.Length) != FixedValueCollection.MSISDNCountryCode)
                if (model.sim_number.Substring(0, FixedValueCollection.SIMCode.Length) != FixedValueCollection.SIMCode)
                {
                    model.sim_number = FixedValueCollection.SIMCode + model.sim_number;
                }

                if (model.channel_name == FixedValueCollection.ResellerChannel
                    || model.channel_name == FixedValueCollection.SMEChannel
                    || model.channel_name == FixedValueCollection.CorporateChannel)
                {
                    attributes = new SIMValidationRequestAttributes("", "", FixedValueCollection.ResellerCodeText + model.retailer_id, "", model.sim_number);
                }
                else if (model.channel_name == FixedValueCollection.MonobrandChannel
                        || model.channel_name == FixedValueCollection.EshopChannel)
                {//Discussion not made yet for this condition(Sifur vai told.date: 14-11-19)
                    attributes = new SIMValidationRequestAttributes(model.center_code, "", "", "", model.sim_number);
                }
                string type = "stock";
                string id = "1";//default mock value.
                SIMValidationRequestData data = new SIMValidationRequestData(type, id, attributes);
                rootobject.data = data;
            }
            catch (Exception)
            {
                throw;
            }
            return rootobject;
        }


        public SIMValidationRequestRootobject ValidateSIMReqParsing2(SIMNumberCheckRequest model)
        {
            SIMValidationRequestRootobject rootobject = new SIMValidationRequestRootobject();
            SIMValidationRequestAttributes attributes = new SIMValidationRequestAttributes("", "", "", "", "");

            model.inventory_id = !model.inventory_id.HasValue ? (int)EnumInventoryId.DMS : model.inventory_id;

            if (model.sim_number.Substring(0, FixedValueCollection.SIMCode.Length) != FixedValueCollection.SIMCode)
            {
                model.sim_number = FixedValueCollection.SIMCode + model.sim_number;
            }

            if (model.inventory_id == (int)EnumInventoryId.DMS)//for channel RESELLER, Telesales, SME, Corporate.

            {
                attributes = new SIMValidationRequestAttributes("", "", FixedValueCollection.ResellerCodeText + model.retailer_id, "", model.sim_number);
            }
            else if (model.inventory_id == (int)EnumInventoryId.POS)//for channel POS, eshop.
            {
                attributes = new SIMValidationRequestAttributes(model.center_code, "", "", "", model.sim_number);
            }
            string type = "stock";
            string id = "1";//default mock value.
            SIMValidationRequestData data = new SIMValidationRequestData(type, id, attributes);
            rootobject.data = data;
            return rootobject;
        }


        public DBSSOTPValidationRequestRootobject DBSSOTPValidationReqParsing(DBSSOTPValidationRequest model)
        {
            try
            {
                return new DBSSOTPValidationRequestRootobject()
                {
                    data = new DBSSOTPValidationRequestData()
                    {
                        id = 1,
                        type = "otp",
                        attributes = new DBSSOTPValidationRequestAttributes()
                        {
                            otp = model.otp,
                            msisdn = model.poc_msisdn,
                            identifier = model.auth_msisdn,
                            purpose = model.purpose
                        }
                    }
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        //public UnpairedSIMreqRootModel UnpairedSIMReqModelParse(UnpairedSIMsearchReqModel model)
        //{
        //    UnpairedSIMreqRootModel rootModel = new UnpairedSIMreqRootModel();

        //    try
        //    {
        //        if (model.retailer_code != null && model.retailer_code.Substring(0, 1) != "R")
        //        {
        //            rootModel.retailer_code = "R" + model.retailer_code;
        //        }
        //        else
        //        {
        //            rootModel.retailer_code = model.retailer_code ?? "";
        //        }
        //        rootModel.sim_serial = model.sim_serial;

        //        if (model.right_id == 34 || model.right_id == 37) //unpaired prepaid (duplicate dial and mnp port in prepaid)
        //        {
        //            if (model.product_category_prepaid != null && model.product_category_prepaid.Contains(","))
        //            {
        //                rootModel.product_category = model.product_category_prepaid.Split(',');
        //            }
        //            else
        //            {
        //                rootModel.product_category = model.product_category_prepaid.Split(' ');
        //            }
        //        }
        //        if (model.right_id == 30 || model.right_id == 38) //unpaired postpaid (Postpaid, Mnp port in postpaid)
        //        {
        //            if (model.product_category_postpaid.Contains(","))
        //            {
        //                rootModel.product_category = model.product_category_postpaid.Split(',');
        //            }
        //            else
        //            {
        //                rootModel.product_category = model.product_category_postpaid.Split(' ');
        //            }
        //        }
        //        if (model.right_id == 4)
        //        {
        //            if (model.product_category_simReplacement.Contains(","))
        //            {
        //                rootModel.product_category = model.product_category_simReplacement.Split(',');
        //            }
        //            else if (model.product_category_simReplacement.Contains("-"))
        //            {
        //                rootModel.product_category = model.product_category_simReplacement.Replace("-", " ").Split('-');
        //            }
        //            else
        //            {
        //                rootModel.product_category = model.product_category_simReplacement.Split(' ');
        //            }
        //        }
        //        if (model.right_id == 30 || model.right_id == 38) //unpaired postpaid (Postpaid, Mnp port in postpaid)
        //        {
        //            if (model.product_code_postpaid.Contains(","))
        //            {
        //                rootModel.product_code = model.product_code_postpaid.Split(',');
        //            }
        //            else
        //            {
        //                rootModel.product_code = model.product_code_postpaid.Split(' ');
        //            }
        //        }
        //        else if (model.right_id == 4)
        //        {
        //            if (model.product_code_simReplacement.Contains(","))
        //            {
        //                rootModel.product_code = model.product_code_simReplacement.Split(',');
        //            }
        //            else
        //            {
        //                rootModel.product_code = model.product_code_simReplacement.Split(' ');
        //            }
        //        }
        //        else if (model.right_id == 114 || model.right_id == 116 || model.right_id == 118)
        //        {
        //            if (model.product_code_StarTrekPrepaid.Contains(","))
        //            {
        //                rootModel.product_code = model.product_code_StarTrekPrepaid.Split(',');
        //            }
        //            else
        //            {
        //                rootModel.product_code = model.product_code_StarTrekPrepaid.Split(' ');
        //            }

        //            if (model.product_category_StarTrekPrepaid.Contains(","))
        //            {
        //                rootModel.product_category = model.product_category_StarTrekPrepaid.Split(',');
        //            }
        //            else
        //            {
        //                rootModel.product_category = model.product_category_StarTrekPrepaid.Split(' ');
        //            }
        //        }
        //        else if (model.right_id == 115 || model.right_id == 117 || model.right_id == 119)
        //        {
        //            if (model.product_code_StarTrekEsim.Contains(","))
        //            {
        //                rootModel.product_code = model.product_code_StarTrekEsim.Split(',');
        //            }
        //            else
        //            {
        //                rootModel.product_code = model.product_code_StarTrekEsim.Split(' ');
        //            }

        //            if (model.product_category_StarTrekEsim.Contains(","))
        //            {
        //                rootModel.product_category = model.product_category_StarTrekEsim.Split(',');
        //            }
        //            else
        //            {
        //                rootModel.product_category = model.product_category_StarTrekEsim.Split(' ');
        //            }
        //        }
        //        else
        //        {

        //            if (model.product_code_prepaid.Contains(","))
        //            {
        //                rootModel.product_code = model.product_code_prepaid.Split(',');
        //            }
        //            else
        //            {
        //                rootModel.product_code = model.product_code_prepaid.Split(' ');
        //            }
        //        }
        //        rootModel.user_name = model.user_name;
        //        rootModel.password = model.password;

        //        return rootModel;
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //}

        public UnpairedSIMreqRootModel UnpairedSIMReqModelParse(UnpairedSIMsearchReqModel model)
        {
            var rootModel = new UnpairedSIMreqRootModel();

            try
            {
                // Retailer code formatting
                rootModel.retailer_code = !string.IsNullOrEmpty(model.retailer_code) && !model.retailer_code.StartsWith("R")
                    ? "R" + model.retailer_code
                    : model.retailer_code ?? string.Empty;

                rootModel.sim_serial = model.sim_serial;

                // Helper function to split string by ',' or whitespace
                string[] SafeSplit(string input)
                {
                    return input?.Contains(",") == true
                        ? input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        : input?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                }

                // Product category
                switch (model.right_id)
                {
                    case 34:
                    case 37:
                        rootModel.product_category = SafeSplit(model.product_category_prepaid ?? "");
                        break;

                    case 30:
                    case 38:
                        rootModel.product_category = SafeSplit(model.product_category_postpaid ?? "");
                        break;

                    case 4:
                        if (!string.IsNullOrEmpty(model.product_category_simReplacement))
                        {
                            if (model.product_category_simReplacement.Contains("-"))
                            {
                                rootModel.product_category = model.product_category_simReplacement
                                    .Replace("-", " ")
                                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            }
                            else
                            {
                                rootModel.product_category = SafeSplit(model.product_category_simReplacement);
                            }
                        }
                        break;

                    case 114:
                    case 116:
                    case 118:
                        rootModel.product_category = SafeSplit(model.product_category_StarTrekPrepaid ?? "");
                        break;

                    case 115:
                    case 117:
                    case 119:
                        rootModel.product_category = SafeSplit(model.product_category_StarTrekEsim ?? "");
                        break;

                    default:
                        rootModel.product_category = SafeSplit(model.product_category_prepaid ?? "");
                        break;
                }

                // Product code
                switch (model.right_id)
                {
                    case 30:
                    case 38:
                        rootModel.product_code = SafeSplit(model.product_code_postpaid ?? "");
                        break;

                    case 4:
                        rootModel.product_code = SafeSplit(model.product_code_simReplacement ?? "");
                        break;

                    case 114:
                    case 116:
                    case 118:
                        rootModel.product_code = SafeSplit(model.product_code_StarTrekPrepaid ?? "");
                        break;

                    case 115:
                    case 117:
                    case 119:
                        rootModel.product_code = SafeSplit(model.product_code_StarTrekEsim ?? "");
                        break;

                    default:
                        rootModel.product_code = SafeSplit(model.product_code_prepaid ?? "");
                        break;
                }

                rootModel.user_name = model.user_name ?? "";
                rootModel.password = model.password ?? "";

                return rootModel;
            }
            catch
            {
                throw;
            }
        }


        #region First Recharge
        public RechargeReqModel RechargeReqPargeModel(RechargeRequestModel model)
        {
            RechargeReqModel recharge = new RechargeReqModel();

            try
            {
                if (model.retailerCode.Substring(0, 1) != "R")
                {
                    recharge.retailerCode = "R" + model.retailerCode;
                }
                else
                {
                    recharge.retailerCode = model.retailerCode;
                }

                recharge.subscriberNo = model.subscriberNo;
                recharge.sessionToken = model.sessionToken ?? "";
                recharge.amount = model.amount;
                recharge.userPin = model.userPin;
                recharge.deviceId = model.deviceId;
                recharge.paymentType = model.paymentType.HasValue ? model.paymentType : 0;
                recharge.lat = model.lat.HasValue ? model.lat : 0;
                recharge.lan = model.lan != null ? model.lan : "en";
                recharge.lng = model.lng.HasValue ? model.lng : 0;
            }
            catch
            {
            }
            return recharge;
        }
        public UnpairedSIMreqRootModel UnpairedSIMReqModelParseV2(UnpairedSIMsearchReqModelV2 model)
        {
            UnpairedSIMreqRootModel rootModel = new UnpairedSIMreqRootModel();

            try
            {
                if (model != null)
                {
                    if (!String.IsNullOrEmpty(model.retailer_code))
                    {
                        if (model.retailer_code.Substring(0, 1) != "R")
                        {
                            rootModel.retailer_code = "R" + model.retailer_code;
                        }
                        else
                        {
                            rootModel.retailer_code = model.retailer_code;
                        }
                    }

                    rootModel.sim_serial = model.sim_serial;

                    if (!String.IsNullOrEmpty(model.product_category))
                    {
                        if (model.product_category.Contains(","))
                        {
                            rootModel.product_category = model.product_category.Split(',');
                        }
                        else
                        {
                            rootModel.product_category = new string[] { model.product_category };
                        }
                    }
                    if (!String.IsNullOrEmpty(model.product_code))
                    {
                        if (model.product_code.Contains(","))
                        {
                            rootModel.product_code = model.product_code.Split(',');
                        }
                        else
                        {
                            rootModel.product_code = new string[] { model.product_code };
                        }
                    }
                    if (!String.IsNullOrEmpty(model.user_name) && !String.IsNullOrEmpty(model.password))
                    {
                        rootModel.user_name = model.user_name;
                        rootModel.password = model.password;
                    }
                }

                return rootModel;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region Raise_Complain
        public RSOComplaintRequestModel ComplaintReqPargeModel(SubmitComplaintModel model)
        {
            RSOComplaintRequestModel reqModel = new RSOComplaintRequestModel();
            try
            {
                if (model.retailerCode.Substring(0, 1) != "R")
                {
                    reqModel.retailerCode = "R" + model.retailerCode;
                }
                else
                {
                    reqModel.retailerCode = model.retailerCode;
                }
                reqModel.userName = model.userName;
                reqModel.password = model.password;
                reqModel.description = model.description;
                reqModel.complaintTitle = model.complaintTitle;
                reqModel.complaintType = model.complaintType;
                reqModel.preferredLevel = model.preferredLevel;
                reqModel.preferredLevelContact = model.preferredLevelContact;
                reqModel.preferredLevelName = model.preferredLevelName;
                reqModel.raiseComplaintID = model.raiseComplaintID;

            }
            catch
            {
            }
            return reqModel;
        }
        #endregion
    }
}
