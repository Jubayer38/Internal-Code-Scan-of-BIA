using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class ICCDetailsRequestModel
    {
        public string mobile_number { get; set; } = "";
        public string? purpose_number { get; set; } = "";
        public string retailer_id { get; set; } = "";
        public string channel_name { get; set; } = "";
        public int? inventory_id { get; set; } = 0;
        public string? center_code { get; set; } = "";
        public string icc { get; set; } = "";
    }
}
