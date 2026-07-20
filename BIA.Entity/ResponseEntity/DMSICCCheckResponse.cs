using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class DMSICCCheckResponse
    {
        public string Message { get; set; } = string.Empty;
        public int Status { get; set; }
        public List<ICCCheckData> Data { get; set; } = new List<ICCCheckData>();
        public Errors Errors { get; set; } = new Errors();
    }

    public class ICCCheckData
    {
        public string ProductCode { get; set; } = string.Empty;
        public string OfferName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
