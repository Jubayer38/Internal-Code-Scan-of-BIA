namespace BIA.Entity.RequestEntity
{
    public class RechargeRequestModel
    {
        public string session_token { get; set; } = string.Empty;
        public string? sessionToken { get; set; }
        public string retailerCode { get; set; } = string.Empty;
        public string subscriberNo { get; set; } = string.Empty;
        public string amount { get; set; } = string.Empty;
        public string userPin { get; set; } = string.Empty;
        public string deviceId { get; set; } = string.Empty;
        public int? paymentType { get; set; }
        public double? lat { get; set; }
        public double? lng { get; set; }
        public string? lan { get; set; }
        public string? userId { get; set; } = "0";
        public string bi_token_number { get; set; } = string.Empty;
    }

    public class RechargeReqModel
    {
        public string sessionToken { get; set; } = string.Empty;
        public string retailerCode { get; set; } = string.Empty;
        public string subscriberNo { get; set; } = string.Empty;
        public string amount { get; set; } = string.Empty;
        public string userPin { get; set; } = string.Empty;
        public string deviceId { get; set; } = string.Empty;            
        public int? paymentType { get; set; }
        public double? lat { get; set; }
        public double? lng { get; set; }
        public string? lan { get; set; }
    }
}
