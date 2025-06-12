using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MailKit.Net.Smtp;

using System.Threading.Tasks;

namespace Backend
{
    public class AlertMessage
    {
        public string DeviceId { get; set; }
        public double? GoodProductionPercent { get; set; }
        public int? ErrorCount { get; set; }
        public int? ErrorCode { get; set; } 
    }
    
}
