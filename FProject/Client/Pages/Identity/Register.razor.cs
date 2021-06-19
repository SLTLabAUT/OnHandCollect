using BlazorFluentUI;
using FProject.Client.Services;
using FProject.Shared.Extensions;
using FProject.Shared.Models;
using FProject.Shared.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Client.Pages.Identity
{
    public partial class Register
    {
        [Inject]
        NavigationManager Navigation { get; set; }
        [Inject]
        AuthorizeApi AuthorizeApi { get; set; }

        IEnumerable<IDropdownOption> SexOptions { get; set; }
        IEnumerable<IDropdownOption> EducationOptions { get; set; }
        IEnumerable<IDropdownOption> HandednessOptions { get; set; }
        RegisterModel Model { get; set; }
        EditContext EditContext { get; set; }
        ValidationMessageStore Errors { get; set; }
        bool TermsDialogOpen { get; set; }
        bool Done { get; set; }
        bool SubmitButtonIsActing { get; set; }

        protected override void OnInitialized()
        {
            SexOptions = Enum.GetValues<Sex>()
                .Select(p => new DropdownOption
                {
                    Text = p.GetAttribute<DisplayAttribute>().Name,
                    Key = ((int)p).ToString()
                });
            EducationOptions = Enum.GetValues<Education>()
                .Select(p => new DropdownOption
                {
                    Text = p.GetAttribute<DisplayAttribute>().Name,
                    Key = ((int)p).ToString()
                });
            HandednessOptions = Enum.GetValues<Handedness>()
                .Select(p => new DropdownOption
                {
                    Text = p.GetAttribute<DisplayAttribute>().Name,
                    Key = ((int)p).ToString()
                });
        }

        protected override void OnParametersSet()
        {
            Model = new RegisterModel();
            EditContext = new EditContext(Model);

            Errors = new ValidationMessageStore(EditContext);
            EditContext.OnFieldChanged += (sender, eventArgs) =>
            {
                Errors.Clear();
            };
        }

        async Task RegisterHandler()
        {
            SubmitButtonIsActing = true;
            try
            {
                Done = false;

                var registerResponse = await AuthorizeApi.Register((RegisterDTO)Model);
                if (registerResponse.Registered)
                {
                    Done = true;
                }
                else if (registerResponse.Errors.Any(e => e.Code == "DuplicateEmail"))
                {
                    Errors.Add(new FieldIdentifier(EditContext.Model, fieldName: string.Empty), "شما قبلا ثبت‌نام کرده‌اید.");
                    EditContext.NotifyValidationStateChanged();
                }
                else
                {
                    Errors.Add(new FieldIdentifier(EditContext.Model, fieldName: string.Empty), "رایانامه یا رمز عبور وارد شده شرایط کافی را ندارد.");
                    EditContext.NotifyValidationStateChanged();
                }
            }
            finally
            {
                SubmitButtonIsActing = false;
            }
        }

        void ShowHideTerms(MouseEventArgs args)
        {
            if (!TermsDialogOpen)
            {
                TermsDialogOpen = true;
                StateHasChanged();
            }
        }

        public class RegisterModel
        {
            [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [EmailAddress(ErrorMessageResourceName = "EmailAddress", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "رایانامه")]
            public string Email { get; set; }
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
            [RegularExpression("True", ErrorMessageResourceName = "Terms", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "پذیرش شرایط استفاده")]
            public bool AcceptTerms { get; set; }
            [RegularExpression(@"^(?:\+98|0)\d{10}$", ErrorMessageResourceName = "PhoneNumber", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "شماره‌ی تلفن همراه")]
            public string PhoneNumber { get; set; }
            [Display(Name = "جنسیت")]
            public IDropdownOption Sex { get; set; }
            [Range(1200, 1400, ErrorMessageResourceName = "Range", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "سال تولد")]
            public short? BirthYear { get; set; }
            [Display(Name = "سطح تحصیلات")]
            public IDropdownOption Education { get; set; }
            [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "راست‌دستی و چپ‌دستی")]
            public IDropdownOption Handedness { get; set; }

            public static explicit operator RegisterDTO(RegisterModel model)
            {
                return new RegisterDTO
                {
                    Email = model.Email,
                    Password = model.Password,
                    AcceptTerms = model.AcceptTerms,
                    PhoneNumber = model.PhoneNumber,
                    Sex = model.Sex is null ? null : Enum.Parse<Sex>(model.Sex.Key),
                    BirthYear = model.BirthYear,
                    Education = model.Education is null ? null : Enum.Parse<Education>(model.Education.Key),
                    Handedness = Enum.Parse<Handedness>(model.Handedness.Key),
                };
            }
        }
    }
}
