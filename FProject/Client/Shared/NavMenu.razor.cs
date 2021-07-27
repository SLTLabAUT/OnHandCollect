using BlazorFluentUI;
using BlazorFluentUI.Routing;
using FProject.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FProject.Client.Shared
{
    public partial class NavMenu
    {
        [Inject]
        HttpClient Http { get; set; }
        [CascadingParameter]
        Task<AuthenticationState> AuthenticationStateTask { get; set; }

        int AcceptedWordCount { get; set; } = -1;

        [Parameter]
        public string Style { get; set; }
        [Parameter]
        public EventCallback<NavLink> OnLinkClicked { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            await UpdateAcceptedWordCount().ConfigureAwait(false);
        }

        protected async Task UpdateAcceptedWordCount()
        {
            if ((await AuthenticationStateTask.ConfigureAwait(false)).User.Identity.IsAuthenticated)
            {
                var result = await Http.GetAsync($"api/Identity/AcceptedWordCount/").ConfigureAwait(false);
                if (result.IsSuccessStatusCode)
                {
                    var count = await result.Content.ReadFromJsonAsync<int>().ConfigureAwait(false);
                    if (AcceptedWordCount != count)
                    {
                        AcceptedWordCount = count;
                    }
                }
            }
        }

        protected async Task OnLinkClickHandler(MouseEventArgs args)
        {
            var updateTask = UpdateAcceptedWordCount();
            await OnLinkClicked.InvokeAsync();
            await updateTask;
        }

        protected async Task OnNavLinkClickHandler(NavLink link)
        {
            var updateTask = UpdateAcceptedWordCount();
            await OnLinkClicked.InvokeAsync(link);
            await updateTask;
        }
    }
}
