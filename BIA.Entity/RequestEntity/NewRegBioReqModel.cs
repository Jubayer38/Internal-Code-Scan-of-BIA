using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class NewRegBioReqModel
    {
        public NewRegData data { get; set; } = new NewRegData();
    }

    public class NewRegData
    {
        public string type { get; set; } = string.Empty;
        public int id { get; set; }
        public NewRegAttributes attributes { get; set; } = new NewRegAttributes();
    }
    public class NewRegAttributes
    {
        public int purpose_no { get; set; }
        public string dest_doc_type_no { get; set; } = string.Empty;
        public string dest_doc_id { get; set; } = string.Empty;     
        public string user_name { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string reg_date { get; set; } = string.Empty;
        public int dest_ec_verification_required { get; set; }
        public string dest_imsi { get; set; } = string.Empty;
        public int dest_foreign_flag { get; set; }
        //public string dest_id_type_exp_time { get; set; }
        public string dest_sim_category { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string dest_left_thumb { get; set; } = string.Empty;
        public string dest_left_index { get; set; } = string.Empty;
        public string dest_right_thumb { get; set; } = string.Empty;
        public string dest_right_index { get; set; } = string.Empty;            
        public bool is_b2b { get; set; }
    }
}
