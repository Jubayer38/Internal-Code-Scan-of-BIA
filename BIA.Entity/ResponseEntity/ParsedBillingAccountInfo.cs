using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class ParsedBillingAccountInfo
    {
        public string? SubscriptionId { get; set; }
        public string? BillingAccountType { get; set; }
        public string? BillingAccountId { get; set; }
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
