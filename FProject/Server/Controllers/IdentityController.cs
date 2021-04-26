﻿using FProject.Server.Models;
using FProject.Server.Services;
using FProject.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FProject.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class IdentityController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IdentityController> _logger;
        private readonly EmailService _emailService;
        private readonly LinkGenerator _linkGenerator;

        public IdentityController(SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<IdentityController> logger,
            EmailService emailService,
            LinkGenerator linkGenerator)
        {
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
            _linkGenerator = linkGenerator;
        }

        [Authorize]
        public async Task<ActionResult<UserDTO>> UserInfo()
        {
            var user = await _signInManager.UserManager.GetUserAsync(User);

            return (UserDTO)user;
        }

        [Authorize]
        public async Task<IActionResult> UpdateProfile(UserDTO userInfo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (User.FindFirstValue(ClaimTypes.Email) != userInfo.Email)
            {
                return BadRequest();
            }

            var user = await _signInManager.UserManager.FindByEmailAsync(userInfo.Email);
            if (user is null)
            {
                return BadRequest();
            }
            user.PhoneNumber = userInfo.PhoneNumber;
            user.Sex = userInfo.Sex;
            user.BirthDate = userInfo.BirthDate;

            var result = await _signInManager.UserManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDTO registerDTO)
        {
            var response = new RegisterResponse();

            if (!ModelState.IsValid)
            {
                return BadRequest(response);
            }

            var newUser = new ApplicationUser
            {
                UserName = registerDTO.Email,
                Email = registerDTO.Email,
                PhoneNumber = registerDTO.PhoneNumber,
                Sex = registerDTO.Sex,
                BirthDate = registerDTO.BirthDate
            };
            var result = await _signInManager.UserManager.CreateAsync(newUser, registerDTO.Password);

            if (!result.Succeeded)
            {
                response.Errors = result.Errors;
                return BadRequest(response);
            }

            await _signInManager.UserManager.AddToRoleAsync(newUser, IdentityRoleConstants.User);

            await SendConfirmationEmail(newUser);

            response.Registered = true;
            response.User = (UserDTO)newUser;
            return CreatedAtAction(nameof(UserInfo), new { email = newUser.Email }, response);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            var response = new LoginResponse();

            if (!ModelState.IsValid)
            {
                return BadRequest(response);
            }

            var result = await _signInManager.PasswordSignInAsync(loginDTO.Email, loginDTO.Password, loginDTO.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");

                var user = await _signInManager.UserManager.FindByEmailAsync(loginDTO.Email);
                var roles = await _signInManager.UserManager.GetRolesAsync(user);
                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtToken:JwtSecurityKey"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expiry = DateTime.UtcNow.AddDays(Convert.ToInt32(_configuration["JwtToken:JwtExpiryInDays"]));
                var token = new JwtSecurityToken(
                    _configuration["JwtToken:JwtIssuer"],
                    _configuration["JwtToken:JwtAudience"],
                    claims,
                    expires: expiry,
                    signingCredentials: creds
                );

                response.LoggedIn = true;
                response.AccessToken = new JwtSecurityTokenHandler().WriteToken(token);
                return Ok(response);
            }
            else if (result.IsLockedOut)
            {
                var forgotPasswordLink = Url.Link("/identity/forgotpassword", null);
                var content = $"حساب کاربری شما به دلیل ورود متعدد رمز عبور اشتباه قفل شده است، برای بازنشانی رمز عبور خود بر روی لینک روبرو کلیک کنید: {forgotPasswordLink}";
                var message = new EmailMessage(new string[] { loginDTO.Email }, "قفل شدن حساب کاربری", content);
                await _emailService.SendEmailAsync(message);
            }
            else if (result.IsNotAllowed)
            {
                response.NeedEmailConfirm = true;
                return BadRequest(response);
            }

            return BadRequest(response);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var user = await _signInManager.UserManager.FindByEmailAsync(dto.Email);
            if (user is null)
            {
                return Ok();
            }

            var token = HttpUtility.UrlEncode(await _signInManager.UserManager.GeneratePasswordResetTokenAsync(user));
            var passwordResetLink = $"{Request.Scheme}://{Request.Host.Value}/identity/resetpassword?token={token}&email={user.Email}";

            var message = new EmailMessage(new string[] { user.Email }, "لینک بازنشانی رمز عبور", passwordResetLink);
            await _emailService.SendEmailAsync(message);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO dto)
        {
            var response = new IdentityErrorsResponse();

            if (!ModelState.IsValid)
            {
                return BadRequest(response);
            }

            var user = await _signInManager.UserManager.FindByEmailAsync(dto.Email);
            if (user is null)
            {
                return Ok(response);
            }
            var result = await _signInManager.UserManager.ResetPasswordAsync(user, dto.Token, dto.Password);
            if (!result.Succeeded)
            {
                response.Errors = result.Errors;
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> ResendConfirmationEmail(string email)
        {
            var user = await _signInManager.UserManager.FindByEmailAsync(email);
            if (user is null)
            {
                return Ok();
            }
            var isConfirmed = await _signInManager.UserManager.IsEmailConfirmedAsync(user);
            if (isConfirmed)
            {
                return Ok();
            }

            await SendConfirmationEmail(user);

            return Ok();
        }

        private async Task SendConfirmationEmail(ApplicationUser user)
        {
            var token = await _signInManager.UserManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = $"{Request.Scheme}://{Request.Host.Value}/identity/confirmemail?token={token}&email={user.Email}";

            var message = new EmailMessage(new string[] { user.Email }, "لینک تایید رایانامه", confirmationLink);
            await _emailService.SendEmailAsync(message);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var response = new IdentityErrorsResponse();
            if (token is null || email is null)
            {
                return BadRequest();
            }
            var user = await _signInManager.UserManager.FindByEmailAsync(email);
            if (user is null)
            {
                return BadRequest(response);
            }
            var result = await _signInManager.UserManager.ConfirmEmailAsync(user, HttpUtility.HtmlDecode(token));
            if (!result.Succeeded)
            {
                response.Errors = result.Errors;
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}