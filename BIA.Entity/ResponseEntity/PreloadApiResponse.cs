using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class PreloadApiResponse
    {
        public string status { get; set; }
        public int status_code { get; set; }
        public string message { get; set; }
        public Payload payload { get; set; }
    }
    public class Payload
    {
        public PreloadData data { get; set; }
        public object _metadata { get; set; }
    }

    public class PreloadData
    {
        public List<Plan> plans { get; set; }
        public List<Device> devices { get; set; }
        public List<Coverage> coverage { get; set; }
        public List<Nationality> nationality { get; set; }
    }
    public class Plan
    {
        public string plan_code { get; set; }
        public string plan_name { get; set; }
        public decimal price { get; set; }
    }

    public class Device
    {
        public string device_code { get; set; }
        public string name { get; set; }
        public List<string> plan_code_list { get; set; }
        public decimal price { get; set; }
    }

    public class Coverage
    {
        public string district_code { get; set; }
        public string name { get; set; }
        public List<Area> areas { get; set; }
    }

    public class Area
    {
        public string area_code { get; set; }
        public string area_name { get; set; }
    }

    public class Nationality
    {
        public string slug { get; set; }
        public string name { get; set; }
    }

    public class DeviceResponse
    {
        public string device_code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public List<Plan> plan_code_list { get; set; }
        public decimal price { get; set; }
    }

    public class PreloadDataListResponse
    {
        public bool isError { get; set; } 
        public string message { get; set; }
        public AppResponsePreloadData data { get; set; }
    }

    public class AppResponsePreloadData
    {
        public List<DeviceResponse> devices { get; set; }
        public List<Coverage> coverage { get; set; }
        public List<Nationality> nationality { get; set; }
    }
}
