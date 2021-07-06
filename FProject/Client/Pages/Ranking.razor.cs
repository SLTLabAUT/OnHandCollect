using BlazorFluentUI;
using FProject.Shared.Extensions;
using FProject.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;

namespace FProject.Client.Pages
{
    public partial class Ranking
    {
        [Inject]
        HttpClient Http { get; set; }
        [Inject]
        NavigationManager Navigation { get; set; }

        int Page { get; set; } = 1;
        int AllCount { get; set; }
        List<DetailsRowColumn<UserRankInfo>> ColumnSource { get; set; } = new();
        List<UserRankInfo> ItemSource { get; set; }

        bool HaveNextPage => Page * 10 < AllCount;

        protected override void OnInitialized()
        {
            ColumnSource.Add(new DetailsRowColumn<UserRankInfo>($" {Utils.GetDisplayName<UserRankInfo, int>(b => b.Rank)}", u => u.Rank) { Index = 0, IconName = "NumberSymbol", IconClassName = "ms-Icon--NumberSymbol", MaxWidth = 75 });
            ColumnSource.Add(new DetailsRowColumn<UserRankInfo>($" {Utils.GetDisplayName<UserRankInfo, int>(b => b.AcceptedWordCount)}", u => u.AcceptedWordCount) { Index = 1, IconName = "TextField", IconClassName = "ms-Icon--TextField", MaxWidth = 100 });
            ColumnSource.Add(new DetailsRowColumn<UserRankInfo>($" {Utils.GetDisplayName<UserRankInfo>(b => b.Username)}", u => u.Username) {
                Index = 2,
                IconName = "Contact",
                IconClassName = "ms-Icon--Contact",
                ColumnItemTemplate = username => builder =>
                {
                    //Console.WriteLine("1");
                    //var value = (entry?.Value as UserRankInfo)?.Username;
                    //Console.WriteLine("2");
                    builder.OpenComponent<TooltipHost>(0);
                    builder.AddAttribute(1, "TooltipContent", (RenderFragment)(builder2 =>
                    {
                        builder2.AddContent(2, username.Value);
                    }));
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(builder2 =>
                    {
                        builder2.AddContent(4, username.Value);
                    }));
                    builder.CloseComponent();
                }
            });
        }

        protected override void OnParametersSet()
        {
            var query = new Uri(Navigation.Uri).Query;
            foreach (var queryItem in QueryHelpers.ParseQuery(query))
            {
                switch (queryItem.Key)
                {
                    case "page":
                        Page = int.Parse(queryItem.Value);
                        break;
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (ItemSource is null)
            {
                var response = await Http.GetFromJsonAsync<UserRankInfoDTO>($"api/Identity/Ranking?page={Page}");
                ItemSource = response.UserRankInfos;
                AllCount = response.AllCount;
                StateHasChanged();
            }
        }

        void OnPageChangeHandler(bool isNext)
        {
            var uri = new Uri(Navigation.Uri);
            var queries = HttpUtility.ParseQueryString(uri.Query);
            queries["page"] = $"{Page + (isNext ? 1 : -1)}";
            var dic = queries.AllKeys.ToDictionary(k => k, k => queries[k]);
            Navigation.NavigateTo(QueryHelpers.AddQueryString(uri.AbsolutePath, dic));
            ItemSource = null;
            OnParametersSet();
        }
    }
}
