using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.StaticClass
{
    public sealed class AppSettingsKeys
    {
        public string WWWRootPath { get; set; } = string.Empty;
        public bool IsWindows { get; set; }
        public string ApplicationTitle { get; set; } = string.Empty;
    }
}
