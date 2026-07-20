using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class DMSLoginRequest
    {
        public string userName { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
    }
}
