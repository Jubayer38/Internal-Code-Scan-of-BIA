using Newtonsoft.Json;

namespace BIA.Entity.ResponseEntity
{

    public class FPLoginResponse
    {
        public bool is_error { get; set; }
        public string message { get; set; } = string.Empty;
        public LogInResponse data { get; set; } = new LogInResponse();
    }

    /// <summary>
    /// User login response type.
    /// </summary>
    public class LogInResponse
    {
        /// <summary>
        /// Security token. A valid token can be used to request other API.  
        /// </summary>
        public string SessionToken { get; set; } = string.Empty;
        /// <summary>
        /// Contains true or false. Is the user is valid/ authenticated then returns true, other wise false.
        /// </summary>
        public bool ISAuthenticate { get; set; }
        /// <summary>
        /// Contains user validation message.(i.e. "User Successfully Validated.")
        /// </summary>
        /// 
        public string? AuthenticationMessage { get; set; } = string.Empty;
        /// <summary>
        /// Reseller user name. (i.e. "201949")
        /// </summary>
        /// 
        public string UserName { get; set; } = string.Empty;
        /// <summary>
        /// User password (encripted) (i.e. "dsasbda6567ara")
        /// </summary>
        /// 
        public string Password { get; set; } = string.Empty;
        /// <summary>
        /// Device IMEI number.
        /// </summary>
        /// 
        public object? DeviceId { get; set; }
        /// <summary>
        /// Unknown usage
        /// </summary>
        /// 
        public bool HasUpdate { get; set; }
        /// <summary>
        /// Fingure print minimum score. While captureing FP for submitting order 
        /// by FP device through reseller app, this value is used (i.e. "65"). 
        /// </summary>
        /// 
        public string MinimumScore { get; set; } = string.Empty;
        /// <summary>
        /// Unknown usage
        /// </summary>
        /// 
        public string OptionalMinimumScore { get; set; } = string.Empty;
        /// <summary>
        /// Unknown usage 
        /// </summary>
        /// 
        public string MaximumRetry { get; set; } = string.Empty;
        /// <summary>
        /// Contains role right access id.
        /// </summary>
        /// 
        public string RoleAccess { get; set; } = string.Empty;
        /// <summary>
        /// Reseller channel id.
        /// </summary>
        /// 
        public int? ChannelId { get; set; }
        /// <summary>
        /// Reseller channel name (i.e. "RESELLER", "Corporate")
        /// </summary>
        /// 
        public string ChannelName { get; set; } = string.Empty;
        /// <summary>
        /// Reseller inventory id.
        /// </summary>
        /// 
        public int InventoryId { get; set; }
        /// <summary>
        /// Reseller center code.
        /// </summary>
        /// 
        public string CenterCode { get; set; } = string.Empty;
        public string itopUpNumber { get; set; } = string.Empty;
        public int is_default_Password { get; set; }
        public string ExpiredDate { get; set; } = string.Empty; 
        public string Designation { get; set; } = string.Empty;
        public int is_etsaf_validation_need { get; set; }
        public int FWA_channel_id { get; set; }
        public string FWA_channel_name { get; set; } = string.Empty;
    }




    /// <summary>
    /// DBSS login response type.
    /// </summary>
    public class DBSSLogInResponse
    {
        /// <summary>
        /// Security token.
        /// </summary>
        public string SessionToken { get; set; } = string.Empty;
        /// <summary>
        /// Is user valid/ authenticated.
        /// </summary>
        public bool ISAuthenticate { get; set; }
        /// <summary>
        /// User validation success or failure message.
        /// </summary>
        public string AuthenticationMessage { get; set; } = string.Empty;       
    }
}
