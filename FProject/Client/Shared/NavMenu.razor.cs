using BlazorFluentUI;
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
        private SignOutSessionStateManager SignOutManager { get; set; }

        [Parameter]
        public EventCallback<BFUNavLink> OnLinkClicked { get; set; }

        private async Task BeginSignOut()
        {
            await SignOutManager.SetSignOutState();
            Navigation.NavigateTo("authentication/logout");
        }
    }
}
