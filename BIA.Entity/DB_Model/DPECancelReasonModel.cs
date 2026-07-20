using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.DB_Model
{
    public class DPECancelReasonModel
    {
        public decimal id { get; set; }
        public string reason { get; set; } = string.Empty;
    }
}
