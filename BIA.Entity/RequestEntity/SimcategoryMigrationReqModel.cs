using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class SimcategoryMigrationReqModel
    {
        public SimcategoryMigrationData data { get; set; } = new SimcategoryMigrationData();
    }
    public class SimcategoryMigrationData
    {
        public string type { get; set; } = string.Empty;    
        public string id { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "biometric-request")]
        public string biometric_request { get; set; } = string.Empty;
        public SimcategoryMigrationMeta meta { get; set; } = new SimcategoryMigrationMeta();
    }
    public class SimcategoryMigrationMeta
    {
        [JsonProperty(PropertyName = "change-date")]
        public string change_date { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "send-sms")]
        public bool send_sms { get; set; }
        public string channel { get; set; } = string.Empty;
        public List<Packages> packages { get; set; } = new List<Packages>();
    }
    public class Packages
    {
        public string name { get; set; } = string.Empty;
    }


    public class SimcategoryMigrationReqModelWithoutPackage
    {
        public SimcategoryMigrationWithoutPackageData data { get; set; } = new SimcategoryMigrationWithoutPackageData();
    }
    public class SimcategoryMigrationWithoutPackageData
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;              
        [JsonProperty(PropertyName = "biometric-request")]
        public string biometric_request { get; set; } = string.Empty;
        public SimcategoryMigrationWithoutPackageMeta meta { get; set; } = new SimcategoryMigrationWithoutPackageMeta();
    }
    public class SimcategoryMigrationWithoutPackageMeta
    {
        [JsonProperty(PropertyName = "change-date")]
        public string change_date { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "send-sms")]
        public bool send_sms { get; set; }
        public string channel { get; set; } = "";
        public string[] packages { get; set; } = Array.Empty<string>();
    }
}
