using Microsoft.Extensions.Configuration;

namespace BIA.Entity.Collections
{
    public class AppSettingsWrapper
    {
        public static string ApiBaseUrl
        {
            get
            {
                return SettingsValues.GetDbssBaseUrl();
            }
        }
        public static int FilterAllow
        {
            get
            {
                return SettingsValues.Getcyn_cherished_filter_allow();
            }
        }

        public static string BLOTPApiBaseUrl
        {
            get
            {
                return SettingsValues.GetBLOTPApiBaseUrl();
            }
        }

        public static string DMSApiBaseUrl
        {
            get
            {
                return SettingsValues.GetDMSBaseUrl();
            }
        }
    }
}
