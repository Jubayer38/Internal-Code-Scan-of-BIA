using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ViewModel
{
    public class JwtTokenModel
    {
        public string ITopUpNumber { get; set; } = string.Empty;
        public string RetailerCode { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string LoginProvider { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;

    }
}
