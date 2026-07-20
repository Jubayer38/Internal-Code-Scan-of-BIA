namespace BIA.Entity.Collections
{
    public static class GetAPICollection
    {
        public static string GetSubscriptionTypes
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscription-types?filter%5Bpayment-type%5D={0}&filter%5Bchannel%5D={1}";
            }
        }

        public static string GetSubscriptionTypesById
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscriptions/{0}/available-subscription-types";
            }
        }

        public static string GetPackagesBySubscriptionTypeId
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscription-types/{0}/?include=option-group-products.product";
            }
        }

        public static string PairedMSISDNValidation
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/msisdns/{0}/validate-paired";
            }
        }

            #region Cherish API
        public static string CherishMSISDNValidation
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/msisdns/{0}";
                //return AppSettingsWrapper.ApiBaseUrl + "/api/v1/msisdns?filter%5Btext-search%5D={0}";
            }
        }
        #endregion

        public static string UnpairedMSISDNValidation
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/msisdns/{0}";
            }
        }

        public static string GetSubscriptionByMSISDN
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscriptions?filter%5Bmsisdn%5D={0}";
            }
        }


        public static string GetSubscriptionByMSISDNIncludingCustomerInfo
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscriptions/{0}?identifier-type=msisdn&include=sim-cards,owner-customer";
            }
        }

        public static string GetSubscriptionByMSISDNIncludingOwnerCustomerUserCustomerSimCardInfo
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscriptions/{0}?identifier-type=msisdn&include=sim-cards,owner-customer,user-customer";
            }
        }

        public static string GetRejectedQCOrders
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/quality-controls?filter%5bstatus%5d={0}&filter%5breseller%5d={1}";
            }
        }


        public static string ValidateOTP
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/otp/{0}";
            }
        }

        public static string BLOTPThroughSMS
        {
            get
            {
                return AppSettingsWrapper.BLOTPApiBaseUrl + "/cgi-bin/sendsms?username=ivrtest&password=ivrtest&from=banglalink&to={0}&text={1}";
            }
        }


        public static string GetCustomerInfoById
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/customers/{0}";
            }
        }

        public static string GetPaymentTypeFromGetSubscriptionType
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscription-types/{0}";
            }
        }

        public static string GetSubscriptionByMSISDNIncludingSimCardsPayerCustomerOwnerCustomerUserCustomer
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscriptions/{0}?identifier-type=msisdn&include=sim-cards,payer-customer,owner-customer,user-customer";
            }
        }

        public static string GetNinetyDaysChecking
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscriptions?filter%5Bmsisdn%5D={0}&include=balances";
            }
        }

        public static string GetImsiBySim
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/inventory-sim-cards/{0}";
            }
        }

        public static string GetSubscriptionIdWithValidateMSISDN
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscriptions/{0}?identifier-type=msisdn&include=owner-customer,user-customer,balances";
            }
        }
        public static string GetBARExceptionChecking
        {
            get
            {
                return "/api/v1/subscriptions/{0}/barrings";
            }
        }
        public static string GetLoanStatusForTOS
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscriptions?filter%5Bmsisdn%5D={0}&include=balances";
            }
        }

        public static string GetLoanStatusForTOSV2
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscriptions/{0}/balances";
            }
        }

        public static string PrepaidBalanceCheck
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscriptions?filter%5Bmsisdn%5D={0}&include=balances";
            }
        }
        public static string DebtCheckApi
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl()  + "/api/v1/subscriptions/{0}/billing-reports";
            }
        }

        public static string SubIdDebtCheckApi
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscriptions?filter%5Bmsisdn%5D={0}&include=combined-usage-reports";
            }
        }
        public static string BillingReportForDebt
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/subscriptions/{0}/billing-reports";
            }
        }
    }


    public static class PostAPICollection
    {
        public static string CheckSIM
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/stock";
            }
        }
    }

    public static class PatchAPICollection
    {
        public static string VerifyOTP
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/otp/{0}";
            }
        }


        public static string CustomerInfoUpdate
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/customers/{0}";
            }
        }


        public static string QCStatusUpdate
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/quality-controls";
            }
        }

    }


    public static class DeleteAPICollection
    {
        public static string UnreserveMSISDN
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/msisdn-reservations";
            }
        }
    }
    public static class UnpairedMSISDNList
    {
        public static string GetUnpairedMSISDNList
        {
            get
            {
                if(AppSettingsWrapper.FilterAllow == 1)
                {
                    return SettingsValues.GetDbssBaseUrl() + "/api/v1/msisdns?page%5Bnumber%5D={0}&page%5Bsize%5D={1}&filtr%5Bstatus%5D=available&filter%5Btext-search%5D={2}*&filter%5Bstock%5D={3}&filter%5Bnumber-category%5D=N&filter%5Breserved%5D=false";
                }
                else
                {
                    return SettingsValues.GetDbssBaseUrl() + "/api/v1/msisdns?page%5Bnumber%5D={0}&page%5Bsize%5D={1}&filtr%5Bstatus%5D=available&filter%5Btext-search%5D={2}*&filter%5Bstock%5D={3}&filter%5Breserved%5D=false";
                }
            }
        } 
         
        public static string GetCYNListOnline
        {
            get
            {
                if (AppSettingsWrapper.FilterAllow == 1)
                {
                    return SettingsValues.GetDbssBaseUrl() + "/api/v1/msisdns?page%5Bnumber%5D={0}&page%5Bsize%5D={1}&filtr%5Bstatus%5D=available&filter%5Btext-search%5D={2}*&filter%5Bstock%5D=33&filter%5Bnumber-category%5D=N&filter%5Breserved%5D=true";
                }
                else
                {
                    return SettingsValues.GetDbssBaseUrl() + "/api/v1/msisdns?page%5Bnumber%5D={0}&page%5Bsize%5D={1}&filtr%5Bstatus%5D=available&filter%5Btext-search%5D={2}*&filter%5Bstock%5D=33&filter%5Breserved%5D=true";
                }
            }
        }
        public static string GetCYNListPhysical
        {
            get
            {
                if (AppSettingsWrapper.FilterAllow == 1)
                {
                    return SettingsValues.GetDbssBaseUrl() + "/api/v1/msisdns?page%5Bnumber%5D={0}&page%5Bsize%5D={1}&filtr%5Bstatus%5D=available&filter%5Btext-search%5D={2}*&filter%5Bstock%5D=16&filter%5Bnumber-category%5D=N&filter%5Breserved%5D=false";
                }
                else
                {
                    return SettingsValues.GetDbssBaseUrl() + "/api/v1/msisdns?page%5Bnumber%5D={0}&page%5Bsize%5D={1}&filtr%5Bstatus%5D=available&filter%5Btext-search%5D={2}*&filter%5Bstock%5D=16&filter%5Breserved%5D=false";
                }
            }
        }
        public static string GetCYNListPhysicalStock16
        {
            get
            { 
                if (AppSettingsWrapper.FilterAllow == 1)
                {
                    return SettingsValues.GetDbssBaseUrl() + "/api/v1/msisdns?page%5Bnumber%5D={0}&page%5Bsize%5D={1}&filtr%5Bstatus%5D=available&filter%5Btext-search%5D={2}*&filter%5Bstock%5D={3}&filter%5Bnumber-category%5D=N&filter%5Breserved%5D=false";
                }
                else
                {
                    return SettingsValues.GetDbssBaseUrl() + "/api/v1/msisdns?page%5Bnumber%5D={0}&page%5Bsize%5D={1}&filtr%5Bstatus%5D=available&filter%5Btext-search%5D={2}*&filter%5Bstock%5D={3}&filter%5Breserved%5D=false";
                }
            }
        }


        public static string GetUnpairedMSISDNListCherished
        {
            get
            {
                return AppSettingsWrapper.ApiBaseUrl + "/api/v1/msisdns?page%5Bnumber%5D={0}&page%5Bsize%5D={1}&filtr%5Bstatus%5D=available&filter%5Btext-search%5D={2}*&filter%5Bstock%5D={3}&filter%5Bnumber-category%5D={4}&filter%5Breserved%5D=false";
            }
        }

        //public static string GetCYNListOnline
        //{
        //    get
        //    {
        //        if (AppSettingsWrapper.FilterAllow == 1)
        //        {
        //            return AppSettingsWrapper.ApiBaseUrl + "/api/v1/msisdns?page%5Bnumber%5D={0}&page%5Bsize%5D={1}&filtr%5Bstatus%5D=available&filter%5Btext-search%5D={2}*&filter%5Bstock%5D=33&filter%5Bnumber-category%5D=N&filter%5Breserved%5D=true";
        //        }
        //        else
        //        {
        //            return AppSettingsWrapper.ApiBaseUrl + "/api/v1/msisdns?page%5Bnumber%5D={0}&page%5Bsize%5D={1}&filtr%5Bstatus%5D=available&filter%5Btext-search%5D={2}*&filter%5Bstock%5D=33&filter%5Breserved%5D=true";
        //        }
        //    }
        //}
        //public static string GetCYNListPhysical
        //{
        //    get
        //    {
        //        if (AppSettingsWrapper.FilterAllow == 1)
        //        {
        //            return AppSettingsWrapper.ApiBaseUrl + "/api/v1/msisdns?page%5Bnumber%5D={0}&page%5Bsize%5D={1}&filtr%5Bstatus%5D=available&filter%5Btext-search%5D={2}*&filter%5Bstock%5D=33&filter%5Bnumber-category%5D=N&filter%5Breserved%5D=false";
        //        }
        //        else
        //        {
        //            return AppSettingsWrapper.ApiBaseUrl + "/api/v1/msisdns?page%5Bnumber%5D={0}&page%5Bsize%5D={1}&filtr%5Bstatus%5D=available&filter%5Btext-search%5D={2}*&filter%5Bstock%5D=33&filter%5Breserved%5D=false";
        //        }
        //    }
        //}
        public static string CheckUnpairedSIM
        {
            get
            {
                return SettingsValues.GetDMSBaseUrl() + "/api/RetailerAPI/GetSIMSerial";
            }
        }
    } 
         
    public static class PairedMSISDN
    {
        public static string PairedMSISDNURL
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl() + "/api/v1/inventory-sim-cards/{0}";
            }
        }
    }
    public static class RetailerAPI
    {
        public static string RechargeAPI
        {
            get
            {
                return SettingsValues.GetRetailerAppBaseUrl();// + "/api/v2/EvRecharge";
            }
        }
    }

    public static class RSOAPI
    {
        public static string ComplaintAPI
        {
            get
            {
                return SettingsValues.GetRSOBaseUrl();
            }
        }
    }
    public static class SingleSourceAPI
    {
        public static string LoginAPI
        {
            get
            {
                return SettingsValues.GetSingleSourceUrl() + "api/Security/InternalLogin";
            }
        }

        public static string BiometricInfoAPI
        {
            get
            {
                return SettingsValues.GetSingleSourceUrl() + "api/BiometricStatus/GetBiometricInfoByMsisdn";
            }
        }
    }

    public static class ICCCheckDMS
    {
        public static string LoginAPI
        {
            get
            {
                return SettingsValues.GetDMSBaseUrlSIMValidation() + "/api/account/login";
            }
        }
        public static string ICCCheckAPI
        {
            get
            {
                return SettingsValues.GetDMSBaseUrlSIMValidation() + "/api/sim-offer/validate";
            }
        }
    }

    public static class SingleSourceGACappingAPI
    {

        public static string LoginAPI
        {
            get
            {
                return SettingsValues.GetSingleSourceUrl() + "api/Security/InternalLogin";
            }
        }
        public static string MSISDNCountCheck
        {
            get
            {
                return SettingsValues.GetSingleSourceUrl() + "api/BiometricStatus/GetRegisteredMsisdnsByNid?nid={0}&is_corporate={1}";
            }
        }
    }
}
