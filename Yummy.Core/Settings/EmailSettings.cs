using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.Settings
{
    public class EmailSettings
    {
        public string Host { get; set; } = null!;
        public int Port { get; set; }
        public string SenderName { get; set; } = null!;
        public string SenderEmail { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
