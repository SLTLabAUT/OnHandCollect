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
        private string description = string.Empty;
        private string textDescription = string.Empty;

        public string Title { get; set; }
        public bool IsFullDescription { get; set; }
        public string Description
        {
            get
            {
                if (IsFullDescription)
                {
                    return description.Replace("\n", "<br>");
                }
                else
                {
                    return $"هم‌یار گرامی،<br>{description.Replace("\n", "<br>")}، روی دکمه‌ی زیر کلیک کنید.";
                }
            }
            set
            {
                description = value;
            }
        }
        public string TextDescription
        {
            get
            {
                if (IsFullDescription)
                {
                    return textDescription;
                }
                else
                {
                    return $"هم‌یار گرامی،\n{description}، وارد لینک زیر بشوید:\n{ButtonUri}"; ;
                }
            }
            set
            {
                textDescription = value;
            }
        }
        public string ButtonLabel { get; set; }
        public string ButtonUri { get; set; }
        public string BaseUri { get; set; }
    }
}
