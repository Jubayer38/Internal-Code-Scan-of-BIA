using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    /// <summary>
    /// Biometric status update request type.
    /// </summary>
    public class BIAFinishNotiRequest
    {
        public string session_token { get; set; } = string.Empty;
        public string bio_request_id { get; set; } = string.Empty;
        public int? is_Success { get; set; }
        public string? error_code { get; set; } = string.Empty;
        public string? description { get; set; } =string.Empty;
        public string? error_source { get; set; } = string.Empty;
    }
}
