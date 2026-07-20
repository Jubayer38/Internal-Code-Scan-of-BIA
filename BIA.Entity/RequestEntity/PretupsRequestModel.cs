using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class PretupsRequestModel
    {
        public string TYPE { get; set; } = "EXLST3TRFREQ";
        public string EXTNWCODE { get; set; } = "BD";
        public string MSISDN { get; set; } =string.Empty;
        public string PIN { get; set; } = string.Empty;
        public string LOGINID { get; set; } = string.Empty;
        public string PASSWORD { get; set; } = string.Empty;
        public string EXTCODE { get; set; } = string.Empty;
        public string EXTREFNUM { get; set; } = "11111";
        public string LANGUAGE1 { get; set; } = "0";
        public int NUMBER_OF_LAST_X_TXN { get; set; }
        public string RECEIVER_MSISDN { get; set; } = string.Empty;
    }
}
