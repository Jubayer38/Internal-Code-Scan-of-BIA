using BIA.Entity.CommonEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class UnpairedSIMsearchReqModelV2 : RACommonRequest
    {
        public string? user_name { get; set; }
        public string? password { get; set; }
        public string? product_code { get; set; }
        public string? product_category { get; set; }

        public string? retailer_code { get; set; }
        [Required]
        public string sim_serial { get; set; } = string.Empty;

        public int channel_id { get; set; }
        public string channel_name { get; set; } = string.Empty;

    }
}
