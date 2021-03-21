using FProject.Server.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Server.Services
{
    public class EmailService
    {
        private MailServerOptions _options;

        public EmailService(IOptions<MailServerOptions> options)
        {
            _options = options.Value;
        }

        public async Task SendEmailAsync(EmailMessage message)
        {
            var email = new MimeMessage();
            email.Sender = new MailboxAddress(_options.DisplayName, _options.From);
            email.From.Add(email.Sender);
            email.To.AddRange(message.To);
            email.Subject = message.Subject;
            var builder = new BodyBuilder();
            builder.HtmlBody = message.Body;
            email.Body = builder.ToMessageBody();
            using var smtp = new SmtpClient();
            smtp.Connect(_options.SmtpServer, _options.Port);
            smtp.Authenticate(_options.Username, _options.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }
    }

    public class MailServerOptions
    {
        public string From { get; set; }
        public string SmtpServer { get; set; }
        public int Port { get; set; }
        public string DisplayName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
