using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class ScannerInfoRespModel
    {
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
        public ScannerData data { get; set; } = new ScannerData();
    }

    public class ScannerData
    {
        public string is_bl_scanner { get; set; } = string.Empty;
    }
}
 