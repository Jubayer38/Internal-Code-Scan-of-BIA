using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class FPDeleteResponseModel
    {
        public bool isError { get; set; }
        public string message { get; set; }        
        public FPDeletedData data { get; set; }
    }

    public class FPDeletedData
    {
        public List<FailedRetailer> failedRetailers { get; set; } = new();
        public List<string> successRetailers { get; set; } = new();
    }

    public class FailedRetailer
    {
        public string retailer_id { get; set; }
        public string error_message { get; set; }
    }

    public class FPDeleteDBResponseModel
    {
        public bool isError { get; set; }
        public string message { get; set; }
        public string details_message { get; set; }
    }
}
