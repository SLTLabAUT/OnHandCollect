using FProject.Server.Models;
using FProject.Server.Services;
using FProject.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = IdentityRoleConstants.Admin)]
    public class EmailController : Controller
    {
        private readonly ILogger<EmailController> _logger;
        private readonly EmailService _emailService;

        private static bool IsSending = false;

        public EmailController(
            ILogger<EmailController> logger,
            EmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail(EmailSenderDTO dto)
        {
            if (IsSending)
            {
                return BadRequest("Service is sending emails!");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            IsSending = true;

            var random = new Random();
            var link = $"{Request.Scheme}://{Request.Host.Value}/writepads";
            var template = new EmailTemplate
            {
                Title = dto.Subject,
                IsFullDescription = true,
                Description = dto.Description,
                TextDescription = $"{dto.TextDescription}\n{link}",
                ButtonLabel = "هدایت به سامانه",
                ButtonUri = link,
                BaseUri = $"{Request.Scheme}://{Request.Host.Value}"
            };
            var htmlBody = await _emailService.GetHtmlBody(template);
            foreach (var to in dto.Tos)
            {
                _logger.LogInformation("Sending email to {} with subject \"{}\".", to, dto.Subject);
                var message = new EmailMessage(new string[] { to }, template.Title, template.TextDescription, htmlBody);
                await _emailService.SendEmailAsync(message);
                await Task.Delay(random.Next(10, 50) * 1000);
            }

            IsSending = false;
            return Ok();
        }
    }
}
