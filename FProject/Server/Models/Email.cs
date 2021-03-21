using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Server.Models
{
    public class EmailMessage
    {
        public List<MailboxAddress> To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }

        public EmailMessage(IEnumerable<string> to, string subject, string content)
        {
            To = new List<MailboxAddress>();
            To.AddRange(to.Select(x => MailboxAddress.Parse(x)));
            Subject = subject;
            Body = content;
        }
    }
}
