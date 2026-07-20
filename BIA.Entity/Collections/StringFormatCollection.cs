using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.Collections
{
    public class StringFormatCollection
    {
        public static string DBSSDOBFormat
        {
            get
            {
                return "yyyy-MM-dd";
            }
        }
        public static string AccessTokenFormat
        {
            get
            {
                return SettingsValues.GetAccessTokenFormat();               
            }
        }
        public static string AccessTokenFormatV2
        {
            get
            {
                return SettingsValues.GetAccessTokenFormatV2();
            }
        }
        public static string[] AccessTokenPropertyArray
        {
            get
            {
                return new string[] { ",uid:", ",uname:", ",dc:", ",deviceId:" };
            }
        }
        public static string[] AccessTokenPropertyArrayV2
        {
            get
            {
                return new string[] { ",uid:", ",uname:", ",dc:", ",deviceId:", ",random:" };
            }
        }

        public static string[] SecurityTokenPropertyArray
        {
            get
            {
                return new string[] { ",uid:", ",uname:", ",dc:", ",deviceId:" };
            }
        }
    }
}
