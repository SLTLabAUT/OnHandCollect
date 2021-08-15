using FProject.Shared;
using FProject.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FProject.Client.Pages
{
    public partial class Export
    {
        int? ExportedCount { get; set; }
        ExportMode Mode { get; set; }
        DateTimeOffset Start { get; set; }
        DateTimeOffset End { get; set; }
        TextType TextType { get; set; }

        [Inject]
        NavigationManager Navigation { get; set; }
        [Inject]
        HttpClient Http { get; set; }

        protected override void OnParametersSet()
        {
            foreach (var queryItem in QueryHelpers.ParseQuery(new Uri(Navigation.Uri).Query))
            {
                switch (queryItem.Key)
                {
                    case "mode":
                        Mode = Enum.Parse<ExportMode>(queryItem.Value);
                        break;
                    case "start":
                        Start = DateTimeOffset.Parse(queryItem.Value);
                        break;
                    case "end":
                        End = DateTimeOffset.Parse(queryItem.Value);
                        break;
                    case "textType":
                        TextType = Enum.Parse<TextType>(queryItem.Value);
                        break;
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                Http.Timeout = Timeout.InfiniteTimeSpan;
                ExportedCount = await Http.GetFromJsonAsync<int?>($"api/Writepad/Export?mode={Mode}&start={Uri.EscapeDataString(Start.ToString())}&end={Uri.EscapeDataString(End.ToString())}&textType={TextType}");
                StateHasChanged();
            }
        }
    }
}
