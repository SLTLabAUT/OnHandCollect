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
        bool CommentsDialogOpen { get; set; }
        bool DeleteButtonIsActing { get; set; }
        bool SendCommentButtonIsActing { get; set; }
        bool ChangeStatusButtonIsActing { get; set; }
        List<WritepadDTO> WritepadList { get; set; }
        IEnumerable<IDropdownOption> PointerTypes { get; set; }
        IEnumerable<IDropdownOption> TextTypes { get; set; }
        WritepadDTO CurrentWritepad { get; set; }

        CommentDTO CommentDTO { get; set; }
        EditContext CommentEditContext { get; set; }

        bool HaveNextPage => Page * 10 < AllCount;

        protected override void OnInitialized()
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
            if (WritepadList is null)
            {
                var result = await Http.GetFromJsonAsync<WritepadsDTO>($"api/Writepad/?page={Page}&admin=true");
                WritepadList = result.Writepads.ToList();
                AllCount = result.AllCount;
                StateHasChanged();
            }
        }

        async Task OnPageChangeHandler(bool isNext)
        {
            var uri = new Uri(Navigation.Uri);
            var queries = HttpUtility.ParseQueryString(uri.Query);
            queries["page"] = $"{Page + (isNext ? 1 : -1)}";
            var dic = queries.AllKeys.ToDictionary(k => k, k => queries[k]);
            Navigation.NavigateTo(QueryHelpers.AddQueryString(uri.AbsolutePath, dic));
            WritepadList = null;
            OnParametersSet();
        }

        async Task SendCommentHandler()
        {
            SendCommentButtonIsActing = true;
            try
            {
                var result = await Http.PostAsJsonAsync($"api/Comment?admin=true", CommentDTO);
                result.EnsureSuccessStatusCode();
                CurrentWritepad.CommentsStatus = WritepadCommentsStatus.NewFromAdmin;

                CommentsDialogOpen = false;
            }
            finally
            {
                SendCommentButtonIsActing = false;
            }
        }

        async Task CommentsButtonHandler(MouseEventArgs args, WritepadDTO writepad)
        {
            CurrentWritepad = writepad;

            var comments = await Http.GetFromJsonAsync<ICollection<CommentDTO>>($"api/Comment?writepadId={writepad.Id}&admin=true");
            writepad.Comments = comments;
            if (writepad.CommentsStatus == WritepadCommentsStatus.NewFromUser)
            {
                writepad.CommentsStatus = WritepadCommentsStatus.None;
            }

            CommentDTO = new CommentDTO()
            {
                WritepadId = writepad.Id,
            };
            CommentEditContext = new EditContext(CommentDTO);

            CommentsDialogOpen = true;
        }

        void DeleteButtonHandler(MouseEventArgs args, WritepadDTO writepad)
        {
            CurrentWritepad = writepad;
            DeleteDialogOpen = true;
        }

        async Task DeleteWritepad(MouseEventArgs args)
        {
            DeleteButtonIsActing = true;
            try
            {
                var result = await Http.DeleteAsync($"api/Writepad/{CurrentWritepad.Id}?admin=true");
                result.EnsureSuccessStatusCode();
                WritepadList.Remove(CurrentWritepad);
                AllCount--;
            }
            finally
            {
                CurrentWritepad = null;
                DeleteDialogOpen = false;
                DeleteButtonIsActing = false;
            }

            if (WritepadList.Count == 0)
            {
                WritepadList = null;
            }
        }

        async Task Approve(MouseEventArgs args, WritepadDTO writepad)
        {
            ChangeStatusButtonIsActing = true;
            try
            {
                var response = await Http.PutAsync($"api/Writepad/{writepad.Id}?status={WritepadStatus.Accepted}&admin=true", null);
                if (response.IsSuccessStatusCode)
                {
                    writepad.Status = WritepadStatus.Accepted;
                }
            }
            finally
            {
                ChangeStatusButtonIsActing = false;
            }
        }

        async Task Reject(MouseEventArgs args, WritepadDTO writepad)
        {
            ChangeStatusButtonIsActing = true;
            try
            {
                var response = await Http.PutAsync($"api/Writepad/{writepad.Id}?status={WritepadStatus.NeedEdit}&admin=true", null);
                if (response.IsSuccessStatusCode)
                {
                    writepad.Status = WritepadStatus.NeedEdit;
                }
            }
            finally
            {
                ChangeStatusButtonIsActing = false;
            }
        }

        void EditHandler(MouseEventArgs args, int id)
        {
            Navigation.NavigateTo($"/writepad/{id}?adminreview");
        }
    }
}
