using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class DMSRetailerReqModel
    {
        public string userName { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string retailerCode { get; set; } = string.Empty;
        public string iTopUpNumber { get; set; } = string.Empty;
        public int isActive { get; set; }
        public string typeName { get; set; } = string.Empty;
    }
}
