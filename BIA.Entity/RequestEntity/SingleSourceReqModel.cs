namespace BIA.Entity.RequestEntity
{
    public class SingleSourceReqModel
    {
        public string msisdn { get; set; } = string.Empty;
    }
    public class SingleSourceLoginReq
    {
        public string user_name { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
    }
}
