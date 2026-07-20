using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class CorporateSIMReplacementResponseRootobject
    {
        public List<CorporateSIMReplacementResponseData> data { get; set; } = new List<CorporateSIMReplacementResponseData>();
    }

    public class CorporateSIMReplacementResponseData
    {
        public CorporateSIMReplacementResponseAttributes attributes { get; set; } = new CorporateSIMReplacementResponseAttributes();
        public CorporateSIMReplacementResponseRelationships relationships { get; set; } = new CorporateSIMReplacementResponseRelationships();
        public CorporateSIMReplacementResponseLinks28 links { get; set; } = new CorporateSIMReplacementResponseLinks28();
        public string id { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseAttributes
    {
        public float monthlycosts { get; set; }
        public bool allowreactivation { get; set; }
        public string contractstatus { get; set; } = string.Empty;
        public object firstcalldate { get; set; } = string.Empty;
        public object terminationtime { get; set; } = string.Empty;
        public string contractid { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public DateTime activationtime { get; set; }
        public string status { get; set; } = string.Empty;
        public DateTime latestcontractterminationtime { get; set; }
        public string directorylisting { get; set; } = string.Empty;
        public string paymenttype { get; set; } = string.Empty;
        public string originalcontractconfirmationcode { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseRelationships
    {
        public CorporateSIMReplacementResponseSimCards simcards { get; set; } = new CorporateSIMReplacementResponseSimCards();
        public CorporateSIMReplacementResponseBillingAccounts billingaccounts { get; set; } = new CorporateSIMReplacementResponseBillingAccounts();
        public CorporateSIMReplacementResponseServices services { get; set; } = new CorporateSIMReplacementResponseServices();
        public CorporateSIMReplacementResponseSubscriptionDiscounts subscriptiondiscounts { get; set; } = new CorporateSIMReplacementResponseSubscriptionDiscounts();
        public CorporateSIMReplacementResponseNetworkServices networkservices { get; set; } = new CorporateSIMReplacementResponseNetworkServices();
        public CorporateSIMReplacementResponseAvailableLoanProducts availableloanproducts { get; set; } = new CorporateSIMReplacementResponseAvailableLoanProducts();
        public CorporateSIMReplacementResponseOwnerCustomer ownercustomer { get; set; } = new CorporateSIMReplacementResponseOwnerCustomer();
        public CorporateSIMReplacementResponseProducts products { get; set; } = new CorporateSIMReplacementResponseProducts();
        public CorporateSIMReplacementResponsePayerCustomer payercustomer { get; set; } = new CorporateSIMReplacementResponsePayerCustomer();
        public CorporateSIMReplacementResponseAvailableSubscriptionTypes availablesubscriptiontypes { get; set; } = new CorporateSIMReplacementResponseAvailableSubscriptionTypes();
        public CorporateSIMReplacementResponseDocumentValidations documentvalidations { get; set; } = new CorporateSIMReplacementResponseDocumentValidations();
        [JsonProperty(PropertyName = "coordinator-customer")]
        public CorporateSIMReplacementResponseCoordinatorCustomer coordinatorcustomer { get; set; } = new CorporateSIMReplacementResponseCoordinatorCustomer();
        public CorporateSIMReplacementResponseProductUsages productusages { get; set; } = new CorporateSIMReplacementResponseProductUsages();
        public CorporateSIMReplacementResponsePortingRequests portingrequests { get; set; } = new CorporateSIMReplacementResponsePortingRequests();
        public CorporateSIMReplacementResponseBillingRatePlan billingrateplan { get; set; } = new CorporateSIMReplacementResponseBillingRatePlan();
        public CorporateSIMReplacementResponseCombinedUsageReport combinedusagereport { get; set; } = new CorporateSIMReplacementResponseCombinedUsageReport();
        public CorporateSIMReplacementResponseUserCustomer usercustomer { get; set; } = new CorporateSIMReplacementResponseUserCustomer();
        public CorporateSIMReplacementResponseGsmServiceUsages gsmserviceusages { get; set; } = new CorporateSIMReplacementResponseGsmServiceUsages();
        public CorporateSIMReplacementResponseBalances balances { get; set; } = new CorporateSIMReplacementResponseBalances();
        public CorporateSIMReplacementResponseBillingUsages billingusages { get; set; } = new CorporateSIMReplacementResponseBillingUsages();
        public CorporateSIMReplacementResponseBarrings barrings { get; set; } = new CorporateSIMReplacementResponseBarrings();
        public CorporateSIMReplacementResponseSubscriptionType subscriptiontype { get; set; } = new CorporateSIMReplacementResponseSubscriptionType();
        public CorporateSIMReplacementResponseAvailableProducts availableproducts { get; set; } = new CorporateSIMReplacementResponseAvailableProducts();
        public CorporateSIMReplacementResponseCatalogSimCards catalogsimcards { get; set; } = new CorporateSIMReplacementResponseCatalogSimCards();
        public CorporateSIMReplacementResponseConnectedProducts connectedproducts { get; set; } = new CorporateSIMReplacementResponseConnectedProducts();
        public CorporateSIMReplacementResponseConnectionType connectiontype { get; set; } = new CorporateSIMReplacementResponseConnectionType();
        public CorporateSIMReplacementResponseAvailableChildProducts availablechildproducts { get; set; } = new CorporateSIMReplacementResponseAvailableChildProducts();
        public CorporateSIMReplacementResponseSimCardOrders simcardorders { get; set; } = new CorporateSIMReplacementResponseSimCardOrders();
    }

    public class CorporateSIMReplacementResponseSimCards
    {
        public CorporateSIMReplacementResponseLinks links { get; set; } = new CorporateSIMReplacementResponseLinks();
    }

    public class CorporateSIMReplacementResponseLinks
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseBillingAccounts
    {
        public CorporateSIMReplacementResponseLinks1 links { get; set; } = new CorporateSIMReplacementResponseLinks1();
        public List<CorporateSIMReplacementResponseDatum> data { get; set; } = new List<CorporateSIMReplacementResponseDatum>();
    }

    public class CorporateSIMReplacementResponseLinks1
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseDatum
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseServices
    {
        public CorporateSIMReplacementResponseLinks2 links { get; set; } = new CorporateSIMReplacementResponseLinks2();
    }

    public class CorporateSIMReplacementResponseLinks2
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseSubscriptionDiscounts
    {
        public CorporateSIMReplacementResponseLinks3 links { get; set; } = new CorporateSIMReplacementResponseLinks3();
    }

    public class CorporateSIMReplacementResponseLinks3
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseNetworkServices
    {
        public CorporateSIMReplacementResponseLinks4 links { get; set; } = new CorporateSIMReplacementResponseLinks4();
    }

    public class CorporateSIMReplacementResponseLinks4
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseAvailableLoanProducts
    {
        public CorporateSIMReplacementResponseLinks5 links { get; set; } = new CorporateSIMReplacementResponseLinks5();
    }

    public class CorporateSIMReplacementResponseLinks5
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseOwnerCustomer
    {
        public CorporateSIMReplacementResponseData1 data { get; set; } = new CorporateSIMReplacementResponseData1();
        public CorporateSIMReplacementResponseLinks6 links { get; set; } = new CorporateSIMReplacementResponseLinks6();
    }

    public class CorporateSIMReplacementResponseData1
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseLinks6
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseProducts
    {
        public CorporateSIMReplacementResponseLinks7 links { get; set; } = new CorporateSIMReplacementResponseLinks7();
    }

    public class CorporateSIMReplacementResponseLinks7
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponsePayerCustomer
    {
        public CorporateSIMReplacementResponseData2 data { get; set; } = new CorporateSIMReplacementResponseData2();
        public CorporateSIMReplacementResponseLinks8 links { get; set; } = new CorporateSIMReplacementResponseLinks8();
    }

    public class CorporateSIMReplacementResponseData2
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseLinks8
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseAvailableSubscriptionTypes
    {
        public CorporateSIMReplacementResponseLinks9 links { get; set; } = new CorporateSIMReplacementResponseLinks9();
    }

    public class CorporateSIMReplacementResponseLinks9
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseDocumentValidations
    {
        public CorporateSIMReplacementResponseLinks10 links { get; set; } = new CorporateSIMReplacementResponseLinks10();
    }

    public class CorporateSIMReplacementResponseLinks10
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseCoordinatorCustomer
    {
        public CorporateSIMReplacementResponseData3 data { get; set; } = new CorporateSIMReplacementResponseData3();
        public CorporateSIMReplacementResponseLinks11 links { get; set; } = new CorporateSIMReplacementResponseLinks11();
    }

    public class CorporateSIMReplacementResponseData3
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseLinks11
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseProductUsages
    {
        public CorporateSIMReplacementResponseLinks12 links { get; set; } = new CorporateSIMReplacementResponseLinks12();
    }

    public class CorporateSIMReplacementResponseLinks12
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponsePortingRequests
    {
        public CorporateSIMReplacementResponseLinks13 links { get; set; } = new CorporateSIMReplacementResponseLinks13();
    }

    public class CorporateSIMReplacementResponseLinks13
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseBillingRatePlan
    {
        public CorporateSIMReplacementResponseData4 data { get; set; } = new CorporateSIMReplacementResponseData4();
        public CorporateSIMReplacementResponseLinks14 links { get; set; } = new CorporateSIMReplacementResponseLinks14();
    }

    public class CorporateSIMReplacementResponseData4
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseLinks14
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseCombinedUsageReport
    {
        public CorporateSIMReplacementResponseData5 data { get; set; } = new CorporateSIMReplacementResponseData5();
        public CorporateSIMReplacementResponseLinks15 links { get; set; } = new CorporateSIMReplacementResponseLinks15();
    }

    public class CorporateSIMReplacementResponseData5
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseLinks15
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseUserCustomer
    {
        public CorporateSIMReplacementResponseData6 data { get; set; } = new CorporateSIMReplacementResponseData6();
        public CorporateSIMReplacementResponseLinks16 links { get; set; } = new CorporateSIMReplacementResponseLinks16();
    }

    public class CorporateSIMReplacementResponseData6
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseLinks16
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseGsmServiceUsages
    {
        public CorporateSIMReplacementResponseLinks17 links { get; set; } = new CorporateSIMReplacementResponseLinks17();
    }

    public class CorporateSIMReplacementResponseLinks17
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseBalances
    {
        public CorporateSIMReplacementResponseLinks18 links { get; set; } = new CorporateSIMReplacementResponseLinks18();
    }

    public class CorporateSIMReplacementResponseLinks18
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseBillingUsages
    {
        public CorporateSIMReplacementResponseLinks19 links { get; set; } = new CorporateSIMReplacementResponseLinks19();
    }

    public class CorporateSIMReplacementResponseLinks19
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseBarrings
    {
        public CorporateSIMReplacementResponseLinks20 links { get; set; } = new CorporateSIMReplacementResponseLinks20();
    }

    public class CorporateSIMReplacementResponseLinks20
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseSubscriptionType
    {
        public CorporateSIMReplacementResponseData7 data { get; set; } = new CorporateSIMReplacementResponseData7();
        public CorporateSIMReplacementResponseLinks21 links { get; set; } = new CorporateSIMReplacementResponseLinks21();
    }

    public class CorporateSIMReplacementResponseData7
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseLinks21
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseAvailableProducts
    {
        public CorporateSIMReplacementResponseLinks22 links { get; set; } = new CorporateSIMReplacementResponseLinks22();
    }

    public class CorporateSIMReplacementResponseLinks22
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseCatalogSimCards
    {
        public CorporateSIMReplacementResponseLinks23 links { get; set; } = new CorporateSIMReplacementResponseLinks23();
    }

    public class CorporateSIMReplacementResponseLinks23
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseConnectedProducts
    {
        public CorporateSIMReplacementResponseLinks24 links { get; set; } = new CorporateSIMReplacementResponseLinks24();
    }

    public class CorporateSIMReplacementResponseLinks24
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseConnectionType
    {
        public CorporateSIMReplacementResponseData8 data { get; set; } = new CorporateSIMReplacementResponseData8();
        public CorporateSIMReplacementResponseLinks25 links { get; set; } = new CorporateSIMReplacementResponseLinks25();
    }

    public class CorporateSIMReplacementResponseData8
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseLinks25
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseAvailableChildProducts
    {
        public CorporateSIMReplacementResponseLinks26 links { get; set; } = new CorporateSIMReplacementResponseLinks26();
    }

    public class CorporateSIMReplacementResponseLinks26
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseSimCardOrders
    {
        public CorporateSIMReplacementResponseLinks27 links { get; set; } = new CorporateSIMReplacementResponseLinks27();
    }

    public class CorporateSIMReplacementResponseLinks27
    {
        public string related { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementResponseLinks28
    {
        public string self { get; set; } = string.Empty;
    }
}
