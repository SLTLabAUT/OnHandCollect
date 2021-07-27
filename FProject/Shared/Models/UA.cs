using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FProject.Shared.Models
{
    public class UA
    {
        public string Ua { get; set; }
        public UABrowser Browser { get; set; }
        public UADevice Device { get; set; }
        public UAEngine Engine { get; set; }
        public UAOS OS { get; set; }
        public UACPU CPU { get; set; }
    }

    public class UABrowser
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }

    public class UADevice
    {
        public string Model { get; set; }
        public string Type { get; set; }
        public string Vendor { get; set; }
    }

    public class UAEngine
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }

    public class UAOS
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }

    public class UACPU
    {
        public string Architecture { get; set; }
    }
}
