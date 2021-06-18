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
        public string HtmlBody { get; set; }
        public string TextBody { get; set; }

        public EmailMessage(IEnumerable<string> to, string subject, string textBody = default, string htmlBody = default)
        {
            To = new List<MailboxAddress>();
            To.AddRange(to.Select(x => MailboxAddress.Parse(x)));
            Subject = subject;
            TextBody = textBody;
            HtmlBody = htmlBody;
        }
    }

    public class EmailTemplate
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ButtonLabel { get; set; }
        public string Uri { get; set; }
    }
}
