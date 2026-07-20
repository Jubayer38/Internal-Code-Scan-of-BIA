using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{

    public class CustomerInfoResponse
    {
        public CustomerInfoResponseAttributes CustomerInfo { get; set; } = new CustomerInfoResponseAttributes();
        public CustomerAddressResponseAttributes CustomerAddressInfo { get; set; } = new CustomerAddressResponseAttributes();
    }


    public class CustomerInfoResponseRootobject
    {
        public CustomerInfoResponseData data { get; set; } = new CustomerInfoResponseData();
    }

    public class CustomerInfoResponseData
    {
        public CustomerInfoResponseAttributes attributes { get; set; } = new CustomerInfoResponseAttributes();
        public CustomerInfoResponseRelationships relationships { get; set; } = new CustomerInfoResponseRelationships();
        public CustomerInfoResponseLinks5 links { get; set; } = new CustomerInfoResponseLinks5();
        public string id { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
    }

    public class CustomerInfoResponseAttributes
    {
        public string idexpiry { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public object bankaccountnumber { get; set; } = new object();
        public object accounttype { get; set; } = new object();
        public string dateofbirth { get; set; } = string.Empty;
        public object ban { get; set; } = new object();
        public string iddocumenttype { get; set; } = string.Empty;
        public bool iscompany { get; set; }
        public object onlineid { get; set; } = new object();
        public object frameagreementendedat { get; set; } = new object();
        public string paymentmethod { get; set; } = string.Empty;
        public object agreementstartdate { get; set; } = new object();
        public string language { get; set; } = string.Empty;
        public bool isloyaltymanager { get; set; }
        public string iddocumentnumber { get; set; } = string.Empty;
        public string invoicedeliverytype { get; set; } = string.Empty;
        public object frameagreementstartedat { get; set; } = new object();
        public string nationality { get; set; } = string.Empty;
        public object traderegisterid { get; set; } = new object();
        public object businessuid { get; set; } = new object();
        public bool marketingown { get; set; }
        [JsonProperty(PropertyName = "alt-contact-phone")]
        public string altcontactphone { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "first-name")]
        public string firstname { get; set; } = string.Empty;
        public bool iscoordinator { get; set; }
        public object occupation { get; set; }  = new object();         
        public object middlename { get; set; } = new object();
        public string segmentationcategory { get; set; } = string.Empty;
        public bool isfleetmanager { get; set; }
        public bool marketingthirdparty { get; set; }
        public string lastname { get; set; } = string.Empty;
        public string contactphone { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
    }

    public class CustomerInfoResponseRelationships
    {
        public CustomerInfoResponseInventory inventory { get; set; } = new CustomerInfoResponseInventory();
        public CustomerInfoResponseCompanyPeople companypeople { get; set; } = new CustomerInfoResponseCompanyPeople();
        public CustomerInfoResponseCustomerEditPermission customereditpermission { get; set; } = new CustomerInfoResponseCustomerEditPermission();
        public CustomerInfoResponseOrders orders { get; set; } = new CustomerInfoResponseOrders();
        public CustomerInfoResponseAddresses addresses { get; set; } = new CustomerInfoResponseAddresses();
    }

    public class CustomerInfoResponseInventory
    {
        public CustomerInfoResponseData1 data { get; set; } = new CustomerInfoResponseData1();
        public CustomerInfoResponseLinks links { get; set; } = new CustomerInfoResponseLinks();
    }

    public class CustomerInfoResponseData1
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class CustomerInfoResponseLinks
    {
        public string related { get; set; } = string.Empty;
    }

    public class CustomerInfoResponseCompanyPeople
    {
        public CustomerInfoResponseLinks1 links { get; set; } = new CustomerInfoResponseLinks1();
    }

    public class CustomerInfoResponseLinks1
    {
        public string related { get; set; } = string.Empty;
    }

    public class CustomerInfoResponseCustomerEditPermission
    {
        public CustomerInfoResponseData2 data { get; set; } = new CustomerInfoResponseData2();
        public CustomerInfoResponseLinks2 links { get; set; } = new CustomerInfoResponseLinks2();
    }

    public class CustomerInfoResponseData2
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class CustomerInfoResponseLinks2
    {
        public string related { get; set; } = string.Empty;
    }

    public class CustomerInfoResponseOrders
    {
        public CustomerInfoResponseLinks3 links { get; set; } = new CustomerInfoResponseLinks3();
    }

    public class CustomerInfoResponseLinks3
    {
        public string related { get; set; } = string.Empty;
    }

    public class CustomerInfoResponseAddresses
    {
        public CustomerInfoResponseLinks4 links { get; set; } = new CustomerInfoResponseLinks4();   
    }

    public class CustomerInfoResponseLinks4
    {
        public string related { get; set; } = string.Empty;
    }

    public class CustomerInfoResponseLinks5
    {
        public string self { get; set; } = string.Empty;            
    }

}
