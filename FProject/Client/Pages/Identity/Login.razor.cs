﻿using FProject.Client.Services;
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
        public string ReturnUrl { get; set; } = "/";

        LoginDTO LoginDTO { get; set; }
        EditContext EditContext { get; set; }
        ValidationMessageStore Errors { get; set; }
        bool EmailNotConfirmed { get; set; }

        protected override void OnParametersSet()
        {
            LoginDTO = new LoginDTO();
            EditContext = new EditContext(LoginDTO);

            Errors = new ValidationMessageStore(EditContext);
            EditContext.OnValidationRequested += (sender, eventArgs) =>
            {
                Errors.Clear();
            };
            EditContext.OnFieldChanged += (sender, eventArgs) =>
            {
                Errors.Clear();
            };
            Console.WriteLine("ParameterSet");
        }

        async Task LoginHandler()
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

        void DismissMessagebarHandler()
        {
            EmailNotConfirmed = false;
        }

        void NavigateToReturnUrl()
        {
            Navigation.NavigateTo(ReturnUrl);
        }
    }
}