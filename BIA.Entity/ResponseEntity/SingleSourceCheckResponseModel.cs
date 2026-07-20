namespace BIA.Entity.ResponseEntity
{
    public class SingleSourceCheckResponseModel
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SingleSourceCheckResponseModelRevamp
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
