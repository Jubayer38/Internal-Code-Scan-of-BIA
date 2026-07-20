using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.CommonEntity
{
    public class GACappingConfig
    {
        public string cappType { get; set; } = string.Empty;
        public int cappDayCount { get; set; }
        public int capQuantityCount { get; set; }
    }

    public class SingleSourceSessionModel
    {
        public string SessionToken { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
