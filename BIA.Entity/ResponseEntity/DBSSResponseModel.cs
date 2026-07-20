using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class MSISDNRootData
    {
        public object data { get; set; } = new object();
    }

    public class MSISDNRootDataForError
    {
        public object error { get; set; } = new object();
    }

    public class SubscriptionTypeRootData
    {
        public object data { get; set; } = new object();
    }

    public class UnpairedMSISDNRootData
    {
        public object data { get; set; } = new object();
    }
    public class PairedMSISDNRootData
    {
        public object data { get; set; } = new object();
    }
    public class PackageRootData
    {
        public object included { get; set; } = new object();
    }

    public class Name
    {
        public string en { get; set; } = string.Empty;
        public string bn { get; set; } = string.Empty;
    }
}
