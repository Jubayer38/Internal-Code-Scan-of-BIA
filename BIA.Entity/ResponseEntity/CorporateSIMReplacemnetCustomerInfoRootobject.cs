using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{

    public class CorporateSIMReplacemnetCustomerInfoRootobject
    {
        public CorporateSIMReplacemnetCustomerInfoData data { get; set; } = new CorporateSIMReplacemnetCustomerInfoData();
    }

    public class CorporateSIMReplacemnetCustomerInfoData
    {
        public CorporateSIMReplacemnetCustomerInfoAttributes attributes { get; set; } = new CorporateSIMReplacemnetCustomerInfoAttributes();
        public CorporateSIMReplacemnetCustomerInfoRelationships relationships { get; set; } = new CorporateSIMReplacemnetCustomerInfoRelationships();
        public CorporateSIMReplacemnetCustomerInfoLinks5 links { get; set; } = new CorporateSIMReplacemnetCustomerInfoLinks5();
        public string id { get; set; } = string.Empty;
        //[JsonPropertyName("type")]
        [JsonProperty("type")]
        public string type { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacemnetCustomerInfoAttributes
    {
        //[JsonPropertyName("id-expiry")]
        [JsonProperty(PropertyName = "id-expiry")]
        public string idexpiry { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "bank-account-number")]
        public string bankaccountnumber { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "account-type")]
        public string accounttype { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "date-of-birth")]
        public string dateofbirth { get; set; } = string.Empty;
        public string ban { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "id-document-type")]
        public string iddocumenttype { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "is-company")]
        public bool iscompany { get; set; }
        [JsonProperty(PropertyName = "online-id")]
        public string onlineid { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "vat-usage-code")]
        public string vatusagecode { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "coordinator-id")]
        public string coordinatorid { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "frame-agreement-ended-at")]
        public string frameagreementendedat { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "payment-method")]
        public string paymentmethod { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "agreement-start-date")]
        public string agreementstartdate { get; set; } = string.Empty;
        public string language { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "is-loyalty-manager")]
        public bool isloyaltymanager { get; set; }
        [JsonProperty(PropertyName = "id-document-number")]
        public string iddocumentnumber { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "invoice-delivery-type")]
        public string invoicedeliverytype { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "frame-agreement-started-at")]
        public string frameagreementstartedat { get; set; } = string.Empty;
        public string nationality { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "trade-register-id")]
        public string traderegisterid { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "business-uid")]
        public string businessuid { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "marketing-own")]
        public bool marketingown { get; set; }
        [JsonProperty(PropertyName = "alt-contact-phone")]
        public string altcontactphone { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "first-name")]
        public string firstname { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "is-coordinator")]
        public bool iscoordinator { get; set; }
        public string occupation { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "middle-name")]
        public string middlename { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "segmentation-category")]
        public string segmentationcategory { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "is-fleet-manager")]
        public bool isfleetmanager { get; set; }
        [JsonProperty(PropertyName = "marketing-third-party")]
        public bool marketingthirdparty { get; set; }
        [JsonProperty(PropertyName = "last-name")]
        public string lastname { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "contact-phone")]
        public string contactphone { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacemnetCustomerInfoRelationships
    {
        public CorporateSIMReplacemnetCustomerInfoInventory inventory { get; set; } = new CorporateSIMReplacemnetCustomerInfoInventory();
        [JsonProperty(PropertyName = "company-people")]
        public CorporateSIMReplacemnetCustomerInfoCompanyPeople companypeople { get; set; } = new CorporateSIMReplacemnetCustomerInfoCompanyPeople();
        [JsonProperty(PropertyName = "coordinator-customer")]
        public CorporateSIMReplacemnetCustomerInfoInventory coordinatorcustomer { get; set; } = new CorporateSIMReplacemnetCustomerInfoInventory();

        [JsonProperty(PropertyName = "customer-edit-permission")]
        public CorporateSIMReplacemnetCustomerInfoCustomerEditPermission customereditpermission { get; set; } = new CorporateSIMReplacemnetCustomerInfoCustomerEditPermission();
        [JsonProperty(PropertyName = "contact-companies")]
        public CorporateSIMReplacemnetCustomerInfoCompanyPeople contactcompanies { get; set; } = new CorporateSIMReplacemnetCustomerInfoCompanyPeople();
        public CorporateSIMReplacemnetCustomerInfoOrders orders { get; set; } = new CorporateSIMReplacemnetCustomerInfoOrders();
        public CorporateSIMReplacemnetCustomerInfoAddresses addresses { get; set; } = new CorporateSIMReplacemnetCustomerInfoAddresses();
    }

    public class CorporateSIMReplacemnetCustomerInfoInventory
    {
        public CorporateSIMReplacemnetCustomerInfoData1 data { get; set; } = new CorporateSIMReplacemnetCustomerInfoData1();
        public CorporateSIMReplacemnetCustomerInfoLinks links { get; set; } = new CorporateSIMReplacemnetCustomerInfoLinks();
    }

    public class CorporateSIMReplacemnetCustomerInfoData1
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacemnetCustomerInfoLinks
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacemnetCustomerInfoCompanyPeople
    {
        public CorporateSIMReplacemnetCustomerInfoLinks1 links { get; set; } = new CorporateSIMReplacemnetCustomerInfoLinks1();
    }

    public class CorporateSIMReplacemnetCustomerInfoLinks1
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacemnetCustomerInfoCustomerEditPermission
    {
        public CorporateSIMReplacemnetCustomerInfoData2 data { get; set; } = new CorporateSIMReplacemnetCustomerInfoData2();
        public CorporateSIMReplacemnetCustomerInfoLinks2 links { get; set; } = new CorporateSIMReplacemnetCustomerInfoLinks2();
    }

    public class CorporateSIMReplacemnetCustomerInfoData2
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacemnetCustomerInfoLinks2
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacemnetCustomerInfoOrders
    {
        public CorporateSIMReplacemnetCustomerInfoLinks3 links { get; set; } = new CorporateSIMReplacemnetCustomerInfoLinks3();
    }

    public class CorporateSIMReplacemnetCustomerInfoLinks3
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacemnetCustomerInfoAddresses
    {
        public CorporateSIMReplacemnetCustomerInfoLinks4 links { get; set; } = new CorporateSIMReplacemnetCustomerInfoLinks4(); 
    }

    public class CorporateSIMReplacemnetCustomerInfoLinks4
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacemnetCustomerInfoLinks5
    {
        public string self { get; set; } = string.Empty;
    }
}
