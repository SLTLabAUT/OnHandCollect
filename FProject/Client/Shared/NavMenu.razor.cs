using BlazorFluentUI;
using FProject.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Client.Shared
{
    public partial class NavMenu
    {
        [Inject]
        private NavigationManager Navigation { get; set; }
        [Inject]
        AuthorizeApi AuthorizeApi { get; set; }

        [Parameter]
        public string Style { get; set; }
        [Parameter]
        public EventCallback<BlazorFluentUI.Routing.NavLink> OnLinkClicked { get; set; }

        private async Task BeginSignOut()
        {
            await AuthorizeApi.Logout();
            Navigation.NavigateTo("/index");
        }
    }
}
