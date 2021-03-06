using FProject.Server.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Cryptography;
using Razor.Templating.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Server.Services
{
    public class EmailService
    {
        private MailServerOptions _options;
        private DkimSigner _signer;
        private HeaderId[] _headers;

        public EmailService(IOptions<MailServerOptions> options)
        {
            _options = options.Value;
            _headers = new HeaderId[] { HeaderId.From, HeaderId.Subject, HeaderId.Date };
            if (_options.PKPath is not null)
            {
                _signer = new DkimSigner(_options.PKPath, _options.Domain, _options.Selector);
            }
        }

        public async Task SendEmailAsync(EmailMessage message)
        {
            var email = new MimeMessage();
            email.Sender = new MailboxAddress(_options.DisplayName, _options.From);
            email.From.Add(email.Sender);
            email.To.AddRange(message.To);
            email.Subject = message.Subject;
            var builder = new BodyBuilder();
            builder.TextBody = message.TextBody;
            builder.HtmlBody = message.HtmlBody;
            email.Body = builder.ToMessageBody();
            using var smtp = new SmtpClient();
            smtp.ServerCertificateValidationCallback = (_, _, _, _) => true;
            smtp.Connect(_options.SmtpServer, _options.Port);
            smtp.Authenticate(_options.Username, _options.Password);
            if (_signer is not null)
            {
                email.Prepare(EncodingConstraint.SevenBit);
                _signer.Sign(email, _headers);
            }
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }

        public async Task<string> GetHtmlBody(EmailTemplate template)
        {
            var content = await RazorTemplateEngine.RenderAsync("EmailTemplate", template);
            return content;
            //return PreMailer.Net.PreMailer.MoveCssInline(content, ignoreElements: "link", removeComments: true).Html;
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
        public string PKPath { get; set; }
        public string Domain { get; set; }
        public string Selector { get; set; }
    }
}
