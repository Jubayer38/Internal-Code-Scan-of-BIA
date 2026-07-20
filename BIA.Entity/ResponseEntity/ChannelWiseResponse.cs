using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class ChannelWiseResponse
    {
        public List<ChannelWiseResponseData> data { get; set; } = new List<ChannelWiseResponseData>();
        public bool result { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class ChannelWiseResponseData
    {

        public string payment_amount { get; set; } = string.Empty;
        public string payment_method { get; set; } = string.Empty;
    }

    public class ChannelWiseResponseRev
    {
        public List<ChannelWiseResponseDataRev> data { get; set; } = new List<ChannelWiseResponseDataRev>();
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
    }
     
    public class ChannelWiseResponseDataRev
    {

        public string payment_amount { get; set; } = string.Empty;
        public string payment_method { get; set; } = string.Empty;
    }
}
