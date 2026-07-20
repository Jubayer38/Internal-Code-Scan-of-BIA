namespace BIA.Entity.ResponseEntity
{
    public class ComplaintResponse
    {
        public long complaint_id { get; set; }
        public bool is_success { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
