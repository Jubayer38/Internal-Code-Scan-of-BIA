using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class NewRegUnPairReqModel
    {
        public NewRegUnPairData data { get; set; } = new NewRegUnPairData();
        public NewRegUnPairMeta meta { get; set; } = new NewRegUnPairMeta();
    }

    public class NewRegUnPairData
    {
        public string type { get; set; } = string.Empty;
        public NewRegUnPairAttributes attributes { get; set; } = new NewRegUnPairAttributes();
    }

    public class NewRegUnPairAttributes
    {
        public string offer { get; set; } = string.Empty;
        public int brand { get; set; }
        public string delivery_type { get; set; } = string.Empty;
        public string correction_for { get; set; } = string.Empty;          
        public string ordered_at { get; set; } = string.Empty;
        public string biometric_request_id { get; set; } = string.Empty;
    }

    public class NewRegUnPairMeta
    {
        public NewRegUnPairCustomer customer { get; set; } = new NewRegUnPairCustomer();
        public NewRegUnPairSales_Info sales_info { get; set; } = new NewRegUnPairSales_Info();
        public ArrayList products { get; set; } = new ArrayList();
    }

    public class NewRegUnPairCustomer
    {
        public string province { get; set; } = string.Empty;
        public string post_code { get; set; } = string.Empty;
        public string area { get; set; } = string.Empty;
        //public string id_expiry { get; set; }
        public string alt_contact_phone { get; set; } = string.Empty;
        public string road { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string house_number { get; set; } = string.Empty;
        public string co_address { get; set; } = string.Empty;
        public string street { get; set; } = string.Empty;
        public string last_name { get; set; } = string.Empty;
        public string language { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public bool is_company { get; set; }
        public string country { get; set; } = string.Empty;
        public bool marketing_own { get; set; }
        public string id_type { get; set; } = string.Empty;
        public string id_number { get; set; } = string.Empty;
        public string birthday { get; set; } = string.Empty;
        public string contact_phone { get; set; } = string.Empty;
        public string nationality { get; set; } = string.Empty;
        public string postal_code { get; set; } = string.Empty;
        public string invoice_delivery_method { get; set; } = string.Empty;
        public string first_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string occupation { get; set; } = string.Empty;
    }

    public class NewRegUnPairSales_Info
    {
        public string reseller { get; set; } = string.Empty;
        public string salesman { get; set; } = string.Empty;
        public string channel { get; set; } = string.Empty;
        public string chain { get; set; } = string.Empty;
        public string sales_type { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
    }

    public class NewRegUnPairProduct
    {
        public object[] barrings { get; set; } = Array.Empty<object>();
        public int termination_penalty_fee { get; set; }
        public string connection_type { get; set; } = string.Empty;
        //public string payer { get; set; }
        public int initial_period { get; set; }
        public string user_privacy { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public bool paying_monthly { get; set; }
        public int recurring_period { get; set; }
        public int retention_penalty_fee { get; set; }
        public string type { get; set; } = string.Empty;
        public string product_type { get; set; } = string.Empty;
        public NewRegUnPairPayer payer { get; set; } = new NewRegUnPairPayer();
        //public string user { get; set; }
        public NewRegUnPairUser user { get; set; } = new NewRegUnPairUser();
        public dynamic packages { get; set; } = string.Empty;

    }
    public class NewRegUnPairProduct1
    {
        public string type { get; set; } = string.Empty;
        public string product_type { get; set; } = string.Empty;
        public string article_id { get; set; } = string.Empty;
        public NewRegUnPairData_Dict data_dict { get; set; } = new NewRegUnPairData_Dict(); 
        public int price { get; set; }
    }

    public class NewRegUnPairData_Dict
    {
        public string msisdn { get; set; } = string.Empty;
    }

    public class NewRegUnPairPayer
    {
        public string province { get; set; } = string.Empty;
        public string post_code { get; set; } = string.Empty;
        public string area { get; set; } = string.Empty;
        //public string id_expiry { get; set; }
        public string alt_contact_phone { get; set; } = string.Empty;
        public string road { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string house_number { get; set; } = string.Empty;
        public string co_address { get; set; } = string.Empty;
        public string street { get; set; } = string.Empty;
        public string last_name { get; set; } = string.Empty;
        public string language { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        //public bool is_company { get; set; }
        public string country { get; set; } = string.Empty;
        //public bool marketing_own { get; set; }
        //public string id_type { get; set; }
        //public string id_number { get; set; }
        //public string birthday { get; set; }
        public string contact_phone { get; set; } = string.Empty;
        public string nationality { get; set; } = string.Empty;
        public string postal_code { get; set; } = string.Empty;
        public string invoice_delivery_method { get; set; } = string.Empty;
        public string first_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string occupation { get; set; } = string.Empty;
    }

    public class NewRegUnPairUser
    {
        public string province { get; set; } = string.Empty;
        public string post_code { get; set; } = string.Empty;
        public string area { get; set; } = string.Empty;
        //public string id_expiry { get; set; }
        public string alt_contact_phone { get; set; } = string.Empty;
        public string road { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string house_number { get; set; } = string.Empty;
        public string co_address { get; set; } = string.Empty;
        public string street { get; set; } = string.Empty;
        public string last_name { get; set; } = string.Empty;
        public string language { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        //public bool is_company { get; set; }
        public string country { get; set; } = string.Empty;
        //public bool marketing_own { get; set; }
        //public string id_type { get; set; }
        //public string id_number { get; set; }
        //public string birthday { get; set; }
        public string contact_phone { get; set; } = string.Empty;
        public string nationality { get; set; } = string.Empty;
        public string postal_code { get; set; } = string.Empty;
        public string invoice_delivery_method { get; set; } = string.Empty;
        public string first_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string occupation { get; set; } = string.Empty;
    }
}
