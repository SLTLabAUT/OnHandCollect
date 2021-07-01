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
        [Range(1200, 1400, ErrorMessageResourceName = "Range", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        public short? BirthYear { get; set; }
        public Education? Education { get; set; }
        public Handedness Handedness { get; set; }
        public int AcceptedWordCount { get; set; }
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
        //[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessageResourceName = "Password", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [MinLength(6, ErrorMessageResourceName = "MinLength", ErrorMessageResourceType = typeof(ErrorMessageResource))]
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
        //[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessageResourceName = "Password", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [MinLength(6, ErrorMessageResourceName = "MinLength", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        public string Password { get; set; }
        [RegularExpression("True", ErrorMessageResourceName = "Terms", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        public bool AcceptTerms { get; set; }
        [RegularExpression(@"^(?:\+98|0)\d{10}$", ErrorMessageResourceName = "PhoneNumber", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        public string PhoneNumber { get; set; }
        public Sex? Sex { get; set; }
        [Range(1200, 1400, ErrorMessageResourceName = "Range", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        public short? BirthYear { get; set; }
        public Education? Education { get; set; }
        public Handedness Handedness { get; set; }
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

    public class IdentityError // Who knows what is difference between this and that. Fuck you Text.Json!
    {
        public string Code { get; set; }
        public string Description { get; set; }

        public static explicit operator IdentityError(Microsoft.AspNetCore.Identity.IdentityError model)
        {
            return new IdentityError
            {
                Code = model.Code,
                Description = model.Description
            };
        }
    }

    public enum Sex
    {
        [Display(Name = "مرد")]
        Man,
        [Display(Name = "زن")]
        Woman
    }

    public enum Education
    {
        [Display(Name = "بدون تحصیلات رسمی")]
        None,
        [Display(Name = "ابتدایی")]
        Primary,
        [Display(Name = "متوسطه‌ی اول")]
        LowerSecondary,
        [Display(Name = "متوسطه‌ی دوم")]
        UpperSecondary,
        [Display(Name = "کاردانی")]
        ShortCycleTertiary,
        [Display(Name = "کارشناسی")]
        Bachelor,
        [Display(Name = "کارشناسی ارشد")]
        Master,
        [Display(Name = "دکترا")]
        Doctoral
    }

    public enum Handedness
    {
        [Display(Name = "راست‌دست")]
        Right,
        [Display(Name = "چپ‌دست")]
        Left,
        [Display(Name = "یکسان‌دست")]
        Both,
    }

    public static class IdentityRoleConstants
    {
        public const string User = "User";
        public const string Admin = "Admin";
    }

    public static class ClaimTypeConstants
    {
        public const string Handedness = "Handedness";
    }
}
