using BlazorFluentUI;
using FProject.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FProject.Shared.Extensions;
using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using FProject.Shared.Resources;
using Microsoft.AspNetCore.WebUtilities;
using System.Web;

namespace FProject.Client.Pages
{
    public partial class WritepadsAdmin
    {
        [Inject]
        ThemeProvider ThemeProvider { get; set; }
        [Inject]
        HttpClient Http { get; set; }
        [Inject]
        NavigationManager Navigation { get; set; }

        int Page { get; set; } = 1;
        int AllCount { get; set; }
        bool DeleteDialogOpen { get; set; }
        List<WritepadDTO> WritepadList { get; set; }
        IEnumerable<IDropdownOption> PointerTypes { get; set; }
        IEnumerable<IDropdownOption> TextTypes { get; set; }
        WritepadDTO CurrentWritepad { get; set; }

        bool HaveNextPage => Page * 10 < AllCount;

        protected override Task OnInitializedAsync()
        {
            PointerTypes = Enum.GetValues<PointerType>()
                .Select(p => new DropdownOption {
                    Text = p.GetAttribute<DisplayAttribute>().Name,
                    Key = ((int) p).ToString()
                });
            TextTypes = Enum.GetValues<FProject.Shared.WritepadType>()
                .Select(p => new DropdownOption
                {
                    Text = p.GetAttribute<DisplayAttribute>().Name,
                    Key = ((int)p).ToString()
                });

            return base.OnInitializedAsync();
        }

        protected override async Task OnParametersSetAsync()
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

            var result = await Http.GetFromJsonAsync<WritepadsDTO>($"api/Writepad/?page={Page}&admin=true");
            WritepadList = result.Writepads.ToList();
            AllCount = result.AllCount;
        }

        async Task OnPageChangeHandler(bool isNext)
        {
            var uri = new Uri(Navigation.Uri);
            var queries = HttpUtility.ParseQueryString(uri.Query);
            queries["page"] = $"{Page + (isNext ? 1 : -1)}";
            var dic = queries.AllKeys.ToDictionary(k => k, k => queries[k]);
            Navigation.NavigateTo(QueryHelpers.AddQueryString(uri.AbsolutePath, dic));
            WritepadList = null;
            await OnParametersSetAsync();
        }

        void DeleteButtonHandler(MouseEventArgs args, WritepadDTO writepad)
        {
            CurrentWritepad = writepad;
            DeleteDialogOpen = true;
        }

        async Task DeleteWritepad(MouseEventArgs args)
        {
            try
            {
                await Http.DeleteAsync($"api/Writepad/{CurrentWritepad.Id}&admin=true");
                WritepadList.Remove(CurrentWritepad);
                AllCount--;
            }
            finally
            {
                CurrentWritepad = null;
                DeleteDialogOpen = false;
            }
        }

        async Task Approve(MouseEventArgs args, WritepadDTO writepad)
        {
            var response = await Http.PutAsync($"api/Writepad/{writepad.Id}?status={WritepadStatus.Accepted}&admin=true", null);
            if (response.IsSuccessStatusCode)
            {
                writepad.Status = WritepadStatus.Accepted;
            }
        }

        async Task Reject(MouseEventArgs args, WritepadDTO writepad)
        {
            var response = await Http.PutAsync($"api/Writepad/{writepad.Id}?status={WritepadStatus.NeedEdit}&admin=true", null);
            if (response.IsSuccessStatusCode)
            {
                writepad.Status = WritepadStatus.NeedEdit;
            }
        }

        void EditHandler(MouseEventArgs args, int id)
        {
            Navigation.NavigateTo($"/writepad/{id}?adminreview");
        }
    }
}
