using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class PurposeNumberReponse
    {
        public List<PurposeNumberReponseData> data { get; set; } = new List<PurposeNumberReponseData>();
        public bool result { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class PurposeNumberReponseData
    {
        public int purpose_id { get; set; }
        public string purpose_name { get; set; } = string.Empty;
    }

    public class PurposeNumberReponseRev
    {
        public List<PurposeNumberReponseDataRev> data { get; set; } = new List<PurposeNumberReponseDataRev>();
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class PurposeNumberReponseDataRev
    {
        public int purpose_id { get; set; }
        public string purpose_name { get; set; } = string.Empty;
    }
}
