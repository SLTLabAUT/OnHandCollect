using FProject.Shared.Resources;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FProject.Shared.Models
{
    public class UserDTO
    {
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [EmailAddress(ErrorMessageResourceName = "EmailAddress", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        public string Email { get; set; }
        [RegularExpression(@"^(?:\+98|0)\d{10}$", ErrorMessageResourceName = "PhoneNumber", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        public string PhoneNumber { get; set; }
        public Sex? Sex { get; set; }
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }
    }

    public class ForgotPasswordDTO
    {
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [EmailAddress(ErrorMessageResourceName = "EmailAddress", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [Display(Name = "رایانامه")]
        public string Email { get; set; }
    }

    public class ResetPasswordDTO
    {
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessageResourceName = "Password", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; }
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessageResourceName = "ConfirmPassword", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [Display(Name = "تکرار رمز عبور")]
        public string ConfirmPassword { get; set; }

        [Required]
        [EmailAddress(ErrorMessageResourceName = "EmailAddress", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        public string Email { get; set; }
        [Required]
        public string Token { get; set; }
    }

    public class LoginDTO
    {
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [EmailAddress(ErrorMessageResourceName = "EmailAddress", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [Display(Name = "رایانامه")]
        public string Email { get; set; }
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; }
        [Display(Name = "به‌یادسپاری")]
        public bool RememberMe { get; set; }
    }

    public class RegisterDTO
    {
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [EmailAddress(ErrorMessageResourceName = "EmailAddress", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        public string Email { get; set; }
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessageResourceName = "Password", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        public string Password { get; set; }
        [RegularExpression("True", ErrorMessageResourceName = "Terms", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        public bool AcceptTerms { get; set; }
        [RegularExpression(@"^(?:\+98|0)\d{10}$", ErrorMessageResourceName = "PhoneNumber", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        public string PhoneNumber { get; set; }
        public Sex? Sex { get; set; }
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }
    }

    public class LoginResponse
    {
        public bool LoggedIn { get; set; }
        public string AccessToken { get; set; }
        public bool NeedEmailConfirm { get; set; }
    }

    public class RegisterResponse {
        public bool Registered { get; set; }
        public UserDTO User { get; set; }
        public IEnumerable<IdentityError> Errors { get; set; }
    }

    public class IdentityErrorsResponse
    {
        public IEnumerable<IdentityError> Errors { get; set; }
    }

    public enum Sex
    {
        [Display(Name = "مرد")]
        Man,
        [Display(Name = "زن")]
        Woman
    }
}
