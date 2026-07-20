using BIA.Entity.CommonEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class SiteIdRequestModelV2 : RACommonRequest
    {
        public decimal lac { get; set; }
        public decimal cid { get; set; }
        public string number_category { get; set; } = string.Empty;
    }
}
