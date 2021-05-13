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

        IEnumerable<IBFUDropdownOption> SexOptions { get; set; }
        RegisterModel Model { get; set; }
        EditContext EditContext { get; set; }
        ValidationMessageStore Errors { get; set; }
        bool TermsDialogOpen { get; set; }
        bool Done { get; set; }

        protected override Task OnInitializedAsync()
        {
            SexOptions = Enum.GetValues<Sex>()
                .Select(p => new BFUDropdownOption
                {
                    Text = p.GetAttribute<DisplayAttribute>().Name,
                    Key = ((int)p).ToString()
                });

            return base.OnInitializedAsync();
        }

        protected override Task OnParametersSetAsync()
        {
            Model = new RegisterModel();
            EditContext = new EditContext(Model);

            Errors = new ValidationMessageStore(EditContext);
            EditContext.OnValidationRequested += (sender, eventArgs) =>
            {
                Errors.Clear();
            };
            EditContext.OnFieldChanged += (sender, eventArgs) =>
            {
                Errors.Clear();
            };

            return base.OnParametersSetAsync();
        }

        async Task RegisterHandler()
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
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessageResourceName = "Password", ErrorMessageResourceType = typeof(ErrorMessageResource))]
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
            public IBFUDropdownOption Sex { get; set; }
            [DataType(DataType.Date)]
            [Display(Name = "تاریخ تولد")]
            public DateTime BirthDate { get; set; }

            public static explicit operator RegisterDTO(RegisterModel model)
            {
                return new RegisterDTO
                {
                    Email = model.Email,
                    Password = model.Password,
                    AcceptTerms = model.AcceptTerms,
                    PhoneNumber = model.PhoneNumber,
                    Sex = model.Sex is null ? null : Enum.Parse<Sex>(model.Sex.Key),
                    BirthDate = model.BirthDate.Ticks == 0 ? null : model.BirthDate
                };
            }
        }
    }
}
