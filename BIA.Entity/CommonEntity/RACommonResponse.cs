using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.CommonEntity
{
    public class RACommonResponse
    {
        /// <summary>
        /// result contains if api request success or not!
        /// </summary>
        public bool result { get; set; }
        /// <summary>
        /// message contains api request result's message (i.e. "Success", "Security token invalid!")
        /// </summary>
        public string message { get; set; } = string.Empty;
    }

    public class RACommonResponseRevamp
    {
        /// <summary>
        /// isError contains if api request success or not!
        /// </summary>  
        public bool isError { get; set; }
        /// <summary>
        /// message contains api request result's message (i.e. "Success", "Security token invalid!")
        /// </summary>
        public string message { get; set; } = string.Empty;

        /// <summary>
        /// Data contains object response from the API.
        /// </summary>
        public Datas data { get; set; } = new Datas();
    }

    public class RACommonResponseRevampResp2 //: ResponseData
    {
        /// <summary>
        /// isError contains if api request success or not!
        /// </summary>  
        public bool isError { get; set; }
        /// <summary>
        /// message contains api request result's message (i.e. "Success", "Security token invalid!")
        /// </summary>
        public string message { get; set; } = string.Empty;
        /// <summary>
        /// reservationId contains MSISDN reservation id (GUID) from DBSS .
        /// </summary>
        public string reservationId { get; set; } = string.Empty;

        /// <summary>
        /// Data contains object response from the API.
        /// </summary>
        public Datas data { get; set; } = new Datas();
    }

    public class Datas
    {
        public int isEsim { get; set; }
        /// <summary>
        /// request_id contains submitted request Id. 
        /// </summary>
        public string request_id { get; set; } = string.Empty;
        /// <summary>
        /// reservationId contains MSISDN reservation id (GUID) from DBSS .
        /// </summary>
        public string reservation_id { get; set; } = string.Empty;

        public string details_message { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;
        public string offer_name { get; set; } = string.Empty;
    }

    public class RACommonResponseRevampV3
    {/// <summary>
     /// Data contains if api request success or not!
     /// </summary>  
        public bool isError { get; set; }
        /// <summary>
        /// Data contains api request result's message (i.e. "Success", "Security token invalid!")
        /// </summary>
        public string message { get; set; } = string.Empty;
        public DesiredCategoryData data { get; set; } = new DesiredCategoryData();
    }

    public class DesiredCategoryData
    {
        public string message { get; set; } = string.Empty;
        public bool isDesiredCategory { get; set; }
        public string category { get; set; } = string.Empty;
        public string reservation_id { get; set; } = string.Empty;
        public string details_message { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;
        public string offer_name { get; set; } = string.Empty;
    }

    public class RACommonResponseRetailLoginUpdateToken //: ResponseData
    {
        /// <summary>
        /// isError contains if api request success or not!
        /// </summary>  
        public bool isError { get; set; }
        /// <summary>
        /// message contains api request result's message (i.e. "Success", "Security token invalid!")
        /// </summary>
        public string message { get; set; } = string.Empty;

        /// <summary>
        /// data contains session token as inner object!
        /// </summary>  
        public SessionForRetailToBiometric data { get; set; } = new SessionForRetailToBiometric();
    }
    public class SessionForRetailToBiometric
    {
        public string session_token { get; set; } = string.Empty;
    } 
}
