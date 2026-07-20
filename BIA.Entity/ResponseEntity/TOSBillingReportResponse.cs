using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class TOSBillingReportResponse
{
    public decimal Debt { get; set; }
    public decimal Unbilled { get; set; }
    public decimal Deposit { get; set; }
    public bool Result { get; set; }
    public string Message { get; set; }
}
}