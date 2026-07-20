using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class CorpSimReplacebyPocReqModel
    {
        public CorpSimReplacebyPocData data { get; set; } = new CorpSimReplacebyPocData();
    }

    public class CorpSimReplacebyPocData
    {
        public string type { get; set; } = string.Empty;
        public int id { get; set; }
        public CorpSimReplacebyPocAttributes attributes { get; set; } = new CorpSimReplacebyPocAttributes();
    }

    public class CorpSimReplacebyPocAttributes
    {
        public int purpose_no { get; set; }
        public string dest_doc_type_no { get; set; } = string.Empty;
        public string dest_doc_id { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public int dest_ec_verification_required { get; set; }
        public string dest_dob { get; set; } = string.Empty;
        public string dest_left_thumb { get; set; } = string.Empty;
        public string dest_left_index { get; set; } = string.Empty;
        public string dest_right_thumb { get; set; } = string.Empty;
        public string dest_right_index { get; set; } = string.Empty;
        public string reg_date { get; set; } = string.Empty;
        public string dest_imsi { get; set; } = string.Empty;
        public string corp_sim_replace_type { get; set; } = string.Empty;
        public bool is_b2b { get; set; }
        public string user_name { get; set; } = string.Empty;   
    }
}
