namespace BIA.Entity.ResponseEntity
{
    public class SingleSourceLoginRes
    {
        public bool is_success { get; set; }
        public string session_token { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
    }
    public class SingleSourceRes
    {
        public bool is_success { get; set; }
        public string message { get; set; } = string.Empty;
        public SingleSourceData Data { get; set; } = new SingleSourceData();
    }
    public class SingleSourceData
    {
        public string msisdn { get; set; } = string.Empty;
        public string imsi { get; set; } = string.Empty;
        public string nid { get; set; } = string.Empty;
        public string dob { get; set; } = string.Empty;
        public string reseller_code { get; set; } = string.Empty;       
        public bool is_active { get; set; }

    }
}
