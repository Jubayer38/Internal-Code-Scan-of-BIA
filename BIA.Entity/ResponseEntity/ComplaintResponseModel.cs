namespace BIA.Entity.ResponseEntity
{
    public class ComplaintResponseModel
    {
        public bool isError { get; set; }
        public string? message { get; set; } = string.Empty;
        //public dynamic data { get; set; } = string.Empty;
    }
    public class ComplaintResp
    {
        public bool result { get; set; }
        public string message { get; set; } = string.Empty;

    }
}
