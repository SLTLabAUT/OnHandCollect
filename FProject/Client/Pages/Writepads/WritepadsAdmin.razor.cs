using FProject.Client.Shared;
using FProject.Shared;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FProject.Client.Pages
{
    public partial class WritepadsAdmin : WritepadsShared
    {
        bool DeleteDialogOpen { get; set; }
        bool CommentsDialogOpen { get; set; }
        bool EmptyWritepadDialogOpen { get; set; }
        Button SendCommentButton { get; set; }
        WritepadDTO CurrentWritepad { get; set; }

        CommentDTO CommentDTO { get; set; }
        EditContext CommentEditContext { get; set; }

        bool HaveNextPage => Page * 10 < AllCount;

        protected override void OnInitialized()
        {
            IsAdminPage = true;
        }

        async Task SendCommentHandler()
        {
            SendCommentButton.State = ButtonState.Acting;
            try
            {
                var result = await Http.PostAsJsonAsync($"api/Comment?admin=true", CommentDTO);
                result.EnsureSuccessStatusCode();
                CurrentWritepad.CommentsStatus = WritepadCommentsStatus.NewFromAdmin;

                CommentsDialogOpen = false;
            }
            finally
            {
                SendCommentButton.State = ButtonState.None;
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
            }

            if (WritepadList.Count == 0)
            {
                WritepadList = null;
            }
        }

        async Task Approve(MouseEventArgs args, WritepadDTO writepad)
        {
            try
            {
                var result = await Http.PutAsync($"api/Writepad/{writepad.Id}?status={WritepadStatus.Accepted}&admin=true", null);
                switch (result.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        writepad.Status = WritepadStatus.Accepted;
                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                        var error = await result.Content.ReadFromJsonAsync<WritepadChangeStatusError>();
                        if (error == WritepadChangeStatusError.EmptyWritepad)
                        {
                            CurrentWritepad = writepad;
                            EmptyWritepadDialogOpen = true;
                        }
                        break;
                }
            }
            finally
            {
            }
        }

        async Task Reject(MouseEventArgs args, WritepadDTO writepad)
        {
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
            }
        }

        void EditHandler(MouseEventArgs args, int id)
        {
            var query = Uri.Query;
            Navigation.NavigateTo($"/writepad/{id}?adminreview{(string.IsNullOrWhiteSpace(query) ? "" : $"&writepadsQuery={Uri.EscapeDataString(query.TrimStart('?'))}")}");
        }
    }
}
