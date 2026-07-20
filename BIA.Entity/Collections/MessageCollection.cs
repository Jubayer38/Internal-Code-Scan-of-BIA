using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.Collections
{
    public static class MessageCollection
    {
        public static string Success
        {
            get
            {
                return "Success!";
            }
        }


        public static string Failed
        {
            get
            {
                return "Failed!";
            }
        }

        public static string DataNotFound
        {
            get
            {
                return "Data not found!";
            }
        }

        public static string NoDataFound
        {
            get
            {
                return "No data found!";
            }
        }

        public static string InvalidSecurityToken
        {
            get
            {
                return SettingsValues.GetInvalidSecurityToken();
            }
        }

        public static string ValidAccessToken
        {
            get
            {
                return SettingsValues.GetValidAccessToken();
            }
        }

        public static string MSISDNInvalid
        {
            get
            {
                return "MSISDN is invalid!";
            }
        }

        public static string MSISDNValid
        {
            get
            {
                return "MSISDN is valid!";
            }
        }

        public static string SIMValid
        {
            get
            {
                return "SIM is valid.";
            }
        }

        public static string SIMInvalid
        {
            get
            {
                return "Wrong SIM or Not in Inventory.";
            }
        }

        public static string SIMIsUsed
        {
            get
            {
                return "SIM is used!";
            }
        }

        public static string NotASwapSIM
        {
            get
            {
                return "This is not a SWAP SIM!";
            }
        }

        public static string NotASwapSIMStarTrek
        {
            get
            { 
                return "Please try with correct SIM card!";
            }
        }

        public static string NotAEVSwapSIM
        {
            get
            {
                return "This is not a EV-SWAP SIM!";
            }
        }

        public static string SIM_Not_Match
        {
            get
            {
                return "SIM Category Not Matched.";
            }
        }
        public static string NotAPrepaidSIM
        {
            get
            {
                return "Wrong SIM or Not in Inventory.";
            }
        }

        public static string NotAPostpaidSIM
        {
            get
            {
                return "Wrong SIM or Not in Inventory.";
            }
        }

        public static string NotAPairedSIM
        {
            get
            {
                return "Wrong SIM or Not in Inventory.";
            }
        }

        public static string NotAnUnpairedSIM
        {
            get
            {
                return "Wrong SIM or Not in Inventory.";
            }
        }

        public static string OTPFailed
        {
            get
            {
                return "OTP generation failed!";
            }
        }


        public static string InvalidOTP
        {
            get
            {
                return "Invalid OTP! ";
            }
        }


        public static string ValidOTP
        {
            get
            {
                return "OTP is valid!";
            }
        }

        public static string InvalidUserName
        {
            get
            {
                return SettingsValues.GetInvalidUserName();
            }
        }

        public static string InvalidUserCridential
        {
            get
            {
                return SettingsValues.GetInvalidUserCridential();
            }
        }

        public static string UserValidted
        {
            get
            {
                return SettingsValues.GetUserValidted();
            }
        }

        public static string DBError
        {
            get
            {
                return "Database operation failed!";
            }
        }

        public static string DBSSError
        {
            get
            {
                return "DBSS error!";
            }
        }

        public static string PWDSentToMobile
        {
            get
            {
                return SettingsValues.GetPaMessage();
            }
        }

        public static string SMSSendFailed
        {
            get
            {
                return "SMS send failed!";
            }
        }

        public static string SomethingWrongHappend
        {
            get
            {
                return "Something wrong happend!";
            }
        }


        public static string UpdateFailed
        {
            get
            {
                return "Update failed!";
            }
        }

        public static string UserNotFound
        {
            get
            {
                return SettingsValues.GetUserNotFound();
            }
        }

        public static string SIMCategoryMismatch
        {
            get
            {
                return "MSISDN is not {0}!";
            }
        }

        public static string MSISDNandSIMBothValid
        {
            get
            {
                return "Mobile and SIM no. both are valid.";
            }
        }

        public static string OrderSubmitSuccessfull
        {
            get
            {
                return "Order submitted successfully.";
            }
        }

        public static string OrderUpdateSuccessfull
        {
            get
            {
                return "Order updated successfully.";
            }
        }

        public static string MSISDNAlreadyExists
        {
            get
            {
                return "MSISDN is already exists in inventory!";
            }
        }


        public static string MSISDNInUse
        {
            get
            {
                return "MSISDN already Activated.";
            }
        }

        public static string MSISDNReserved
        {
            get
            {
                return "MSISDN already reserved. Please try activation with another MSISDN";
            }
        }

        public static string MSISDNNotReserved
        {
            get
            {
                return "MSISDN is not reserved. Please try activation with correct reserve number";
            }
        }

        public static string StockIDMismatch
        {
            get
            {
                return "You are not authorized for this connection!";
            }
        }


        public static string InvalidAttempt
        {
            get
            {
                return "Invalid attempt";
            }
        }


        public static string SIMTypeIsNotSIMOrUSIM
        {
            get
            {
                return "This MSISDN is not allowed for this type of SIM!";
            }
        }

        public static string SIMTypeIsNotUSIM
        {
            get
            {
                return "Old SIM type must be USIM or PLI";
            }
        }

        public static string SIMIsNotInInventory
        {
            get
            {
                return "This SIM is not in your inventory!";
            }
        }


        public static string PleaseTryAgain
        {
            get
            {
                return "Please try again. ";
            }
        }

        public static string SIMReplNoDataFound
        {
            get
            {
                return "Invalid MSISDN.";
            }
        }

        public static string MSISDNStatusNotActiveOrIdle
        {
            get
            {
                return "MSISDN is not yet registered.";
            }
        }


        public static string POCInfoNotFound
        {
            get
            {
                return "POC info not found!";
            }
        }


        public static string OldSIMNotFound
        {
            get
            {
                return "Old SIM number not found!";
            }
        }

        public static string DBSSIncorrectErrorResp
        {
            get
            {
                return "DBSS resent incorrect API error response!";
            }
        }

        public static string OrderAlreadyInProcess
        {
            get
            {
                return "This order is already in process.";
            }
        }

        public static string OrderCreationFaild
        {
            get
            {
                return "Unable to generate request token.";
            }
        }

        public static string OrderUpdateFaild
        {
            get
            {
                return "Database Error! Unable to update token.";
            }
        }

        public static string QCStatusUpdateFailed
        {
            get
            {
                return "QC status update failed!";
            }
        }
        public static string ValidCherishedNumber
        {
            get
            {
                return "MSISDN and SIM no. both are valid!";
            }
        }
        public static string InvalidCherishedNumber
        {
            get
            {
                return "Retailer is not eligible for Cherish Registration!";
            }
        }
        public static string PreToPostMigrationFailedMessage
        {
            get
            {
                return "This connection is already Postpaid";
            }
        }
        public static string NotAESim
        {
            get
            {
                return "This is not a eSim";
            }
        }



        public static string NotMatchedSimCategory
        {
            get
            {
                return "Sim Category Not Matched";
            }
        }

        public static string NotESIM
        {
            get
            {
                return "This is not E-SIM";
            }
        }
        public static string StarTrekNotInError
        {
            get
            {
                return "Incorrect Product!";
            } 
        }
        public static string StarTrekNotEligible
        {
            get
            {
                return "Not eligible from shop!";
            }
        }
        public static string StarTrekNotEligibleOnline
        {
            get
            {
                return "Not eligible from online shop!";
            }
        }
        public static string CherishCategoryMismatch
        {
            get
            {
                return "Please select right category.";
            }
        }
        public static string NinetyDaysLock
        {
            get
            {
                return "TOS Not Eligible within 90 days of activation";
            }
        }
    } 
}
