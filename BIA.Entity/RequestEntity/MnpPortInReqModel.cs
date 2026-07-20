using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class MnpPortInReqModel
    {
        public MnpPortInData data { get; set; } = new MnpPortInData();
        public MnpPortInMeta meta { get; set; } = new MnpPortInMeta();
    }

    public class MnpPortInData
    {
        public MnpPortInAttributes attributes { get; set; } = new MnpPortInAttributes();
        public string type { get; set; } = string.Empty;
    }

    public class MnpPortInAttributes
    {
        public int brand { get; set; }
        public string correction_for { get; set; } = string.Empty;
        public string delivery_type { get; set; } = string.Empty;
        public string offer { get; set; } = string.Empty;   
        public string biometric_request_id { get; set; } = string.Empty;
    }

    public class MnpPortInMeta
    {
        public MnpPortInCustomer customer { get; set; } = new MnpPortInCustomer();
        public System.Collections.ArrayList products { get; set; } = new System.Collections.ArrayList();
        public MnpPortInSales_Info sales_info { get; set; } = new MnpPortInSales_Info();
    }

    public class MnpPortInCustomer
    {
        public string alt_contact_phone { get; set; } = string.Empty;
        public string area { get; set; } = string.Empty;
        public string birthday { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string co_address { get; set; } = string.Empty;
        public string contact_phone { get; set; } = string.Empty;
        public string country { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string first_name { get; set; } = string.Empty;
        public string house_number { get; set; } = string.Empty;
        //public string id_expiry { get; set; }
        public string id_number { get; set; } = string.Empty;   
        public string id_type { get; set; } = string.Empty;
        public string invoice_delivery_method { get; set; } = string.Empty;
        public bool is_company { get; set; }
        public string language { get; set; } = string.Empty;
        public string last_name { get; set; } = string.Empty;
        public bool marketing_own { get; set; }
        public string nationality { get; set; } = string.Empty;
        public string occupation { get; set; } = string.Empty;
        public string post_code { get; set; } = string.Empty;
        public string postal_code { get; set; } = string.Empty;
        public string province { get; set; } = string.Empty;
        public string road { get; set; } = string.Empty;
        public string street { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
    }

    public class MnpPortInSales_Info
    {
        public string chain { get; set; } = string.Empty;
        public string channel { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string reseller { get; set; } = string.Empty;
        public string sales_type { get; set; } = string.Empty;
        public string salesman { get; set; } = string.Empty;
    }

    public class MnpPortInProduct
    {
        public object[] barrings { get; set; } = Array.Empty<object>();
        public int initial_period { get; set; }
        public MnpPortIn mnp { get; set; } = new MnpPortIn();
        public string msisdn { get; set; } = string.Empty;
        public dynamic packages { get; set; } = string.Empty;
        //public string payer { get; set; }
        public bool paying_monthly { get; set; }
        public string product_type { get; set; } = string.Empty;
        public MnpPortInPayer payer { get; set; } = new MnpPortInPayer();
        public int recurring_period { get; set; }
        public float retention_penalty_fee { get; set; }
        public float termination_penalty_fee { get; set; }
        public string type { get; set; } = string.Empty;
        //public string user { get; set; }
        public MnpPortInUser user { get; set; } = new MnpPortInUser();
        public string user_privacy { get; set; } = string.Empty;
    }
    public class MnpPortInProduct1
    {
        public string type { get; set; } = string.Empty;
        public string product_type { get; set; } = string.Empty;
        public string article_id { get; set; } = string.Empty;  
        public MnpPortInData_Dict data_dict { get; set; } = new MnpPortInData_Dict();   
        public int price { get; set; }
    }
    public class MnpPortIn
    {
        public string document_id { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string portation_time { get; set; } = string.Empty;
        public string recipient_operator { get; set; } = string.Empty;
        public bool is_emergency_return { get; set; }
    }
    public class MnpPortInData_Dict
    {
        public string msisdn { get; set; } = string.Empty;
    }

    public class MnpPortInPayer
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

    public class MnpPortInUser
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
