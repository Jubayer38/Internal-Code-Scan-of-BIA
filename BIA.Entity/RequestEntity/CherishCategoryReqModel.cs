using BIA.Entity.CommonEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class CherishCategoryReqModel : RACommonRequest
    {
        public string channel_name { get; set; } = string.Empty;
        public string retailer_code { get; set; } = string.Empty;
    }
}
