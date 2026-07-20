using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class HomeWifiReferOrderRequest
    {
        public string customer_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string mobile { get; set; } = string.Empty;
        public string alternate_mobile { get; set; } = string.Empty;
        public string nid_number { get; set; } = string.Empty;
        public string nationality { get; set; } = string.Empty;
        public string district_code { get; set; } = string.Empty;
        public string area_code { get; set; } = string.Empty;
        public string delivery_address { get; set; } = string.Empty;
        public DateTime appointment_date { get; set; }
        public string plan_code { get; set; } = string.Empty;
        public string plan_name { get; set; } = string.Empty;
        public string device_code { get; set; } = string.Empty;
        public string device_name { get; set; } = string.Empty;
        public string remarks { get; set; } = string.Empty;
        public string subscription_code { get; set; } = string.Empty;
        public string package_code { get; set; } = string.Empty;
        public string retailer_id { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
    }
}
