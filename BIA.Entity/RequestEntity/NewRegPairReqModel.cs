using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class NewRegPairReqModel
    {
        public NewRegPairData data { get; set; } = new NewRegPairData();
        public NewRegPairMeta meta { get; set; } = new NewRegPairMeta();
    }
    public class NewRegPairData
    {
        public string type { get; set; } = string.Empty;
        public NewRegPairAttributes attributes { get; set; } = new NewRegPairAttributes();
    }
    public class NewRegPairMeta
    {
        public NewRegPairCustomer customer { get; set; } = new NewRegPairCustomer();
        public NewRegPairSales_Info sales_info { get; set; } = new NewRegPairSales_Info();
        public ArrayList products { get; set; } = new ArrayList();
    }
    public class NewRegPairAttributes
    {
        public string offer { get; set; } = string.Empty;
        public int brand { get; set; }
        public string delivery_type { get; set; } = string.Empty;
        public string correction_for { get; set; } = string.Empty;
        public string ordered_at { get; set; } = string.Empty;
        public string biometric_request_id { get; set; } = string.Empty;
    }
    public class NewRegPairCustomer
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
        public string language { get; set; }    = string.Empty;
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
    public class NewRegPairSales_Info
    {
        public string reseller { get; set; } = string.Empty;
        public string salesman { get; set; } = string.Empty;
        public string channel { get; set; } = string.Empty;             
        public string chain { get; set; } = string.Empty;
        public string sales_type { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
    }
    public class NewRegPairProduct
    {
        public object[] barrings { get; set; } = Array.Empty<object>();
        public int termination_penalty_fee { get; set; }
        //public string payer { get; set; }
        public int initial_period { get; set; }
        public string user_privacy { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public bool paying_monthly { get; set; }
        public int recurring_period { get; set; }
        public int retention_penalty_fee { get; set; }
        public string type { get; set; } = string.Empty;
        public string product_type { get; set; } = string.Empty;
        public NewRegPairPayer payer { get; set; } = new NewRegPairPayer();
        //public string user { get; set; }
        public NewRegPairUser user { get; set; } = new NewRegPairUser();
        public string connection_type { get; set; } = string.Empty;
        public object packages { get; set; } = new object();

    }
    public class NewRegPairPayer
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
    public class NewRegPairUser
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
    public class NewRegPairProduct1
    {
        public string type { get; set; } = string.Empty;
        public string product_type { get; set; } = string.Empty;
        public string article_id { get; set; } = string.Empty;
        public NewRegPairData_Dict data_dict { get; set; } = new NewRegPairData_Dict();     
        public int price { get; set; }
    }
    public class NewRegPairData_Dict
    {
        public string msisdn { get; set; } = string.Empty;          
    }
}
