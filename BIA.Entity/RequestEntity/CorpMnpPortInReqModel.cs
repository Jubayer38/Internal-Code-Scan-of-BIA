using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class CorpMnpPortInReqModel
    {
        public CorpMnpPortInData data { get; set; } = new CorpMnpPortInData();
    }
    public class CorpMnpPortInData
    {
        public string type { get; set; } = string.Empty;
        public int id { get; set; }
        public CorpMnpPortInAttributes attributes { get; set; } = new CorpMnpPortInAttributes();
    }

    public class CorpMnpPortInAttributes
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
        public bool is_b2b { get; set; }
        public string reg_date { get; set; } = string.Empty;
    }
}
