
using FProject.Client.Services;
using FProject.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FProject.Client.Pages.Identity
{
    public partial class Login
    {
        [Inject]
        HttpClient Http { get; set; }
        [Inject]
        NavigationManager Navigation { get; set; }
        [Inject]
        AuthorizeApi AuthorizeApi { get; set; }

        [Parameter]
        public string ReturnUrl { get; set; } = "/index";

        LoginDTO LoginDTO { get; set; }
        EditContext EditContext { get; set; }
        ValidationMessageStore Errors { get; set; }
        bool EmailNotConfirmed { get; set; }
        Shared.Button SubmitButton { get; set; }

        protected override void OnParametersSet()
        {
            LoginDTO = new LoginDTO();
            EditContext = new EditContext(LoginDTO);

            Errors = new ValidationMessageStore(EditContext);
            EditContext.OnFieldChanged += (sender, eventArgs) =>
            {
                Errors.Clear();
            };
        }

        async Task LoginHandler()
        {
            SubmitButton.State = Shared.ButtonState.Acting;
            try
            {
                EmailNotConfirmed = false;
                var loginResponse = await AuthorizeApi.Login(LoginDTO);
                if (loginResponse.LoggedIn)
                {
                    NavigateToReturnUrl();
                }
                else if (loginResponse.NeedEmailConfirm)
                {
                    EmailNotConfirmed = true;
                }
                else
                {
                    Errors.Add(new FieldIdentifier(EditContext.Model, fieldName: string.Empty), "رایانامه یا رمز عبور وارد شده اشتباه است.");
                    EditContext.NotifyValidationStateChanged();
                }
            }
            finally
            {
                SubmitButton.State = Shared.ButtonState.None;
            }
        }

        void DismissMessagebarHandler()
        {
            EmailNotConfirmed = false;
        }

        void NavigateToReturnUrl()
        {
            Navigation.NavigateTo(ReturnUrl, ReturnUrl == "/");
        }
    }
}
