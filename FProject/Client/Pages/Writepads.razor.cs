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
using Microsoft.AspNetCore.Components.Authorization;
using FProject.Shared.Models;
using System.Security.Claims;
using FProject.Client.Shared;

namespace FProject.Client.Pages
{
    public partial class Writepads
    {
        [Inject]
        ThemeProvider ThemeProvider { get; set; }
        [Inject]
        HttpClient Http { get; set; }
        [Inject]
        NavigationManager Navigation { get; set; }

        [CascadingParameter]
        Task<AuthenticationState> AuthenticationStateTask { get; set; }

        Uri Uri { get; set; }
        int Page { get; set; } = 1;
        int AllCount { get; set; }
        bool CreateDialogOpen { get; set; }
        bool DeleteDialogOpen { get; set; }
        bool CommentsDialogOpen { get; set; }
        bool InfoDialogOpen { get; set; }
        bool EmptyWritepadDialogOpen { get; set; }
        bool SubmitDisabled { get; set; }
        Button SaveButton { get; set; }
        Button SendCommentButton { get; set; }
        bool IsSignSelected { get; set; }
        Handedness Handedness { get; set; }
        NewWritepadModel NewWritepad { get; set; }
        ValidationMessageStore CreateErrors { get; set; }
        EditContext EditContext { get; set; }
        List<WritepadDTO> WritepadList { get; set; }
        CommandBarItem[] Items { get; set; }
        IEnumerable<IDropdownOption> PointerTypes { get; set; }
        IEnumerable<IDropdownOption> TextTypes { get; set; }
        IEnumerable<IDropdownOption> HandOptions { get; set; }
        WritepadDTO CurrentWritepad { get; set; }

        CommentDTO CommentDTO { get; set; }
        EditContext CommentEditContext { get; set; }

        bool HaveNextPage => Page * 10 < AllCount;

        protected override void OnInitialized()
        {
            Items = new CommandBarItem[] {
                new CommandBarItem() { Text = "ایجاد تخته‌ی جدید", IconName = "Add", Key = "add", OnClick = AddOnClickHandler }
            };
            PointerTypes = Enum.GetValues<PointerType>()
                .Select(p => new DropdownOption {
                    Text = p.GetAttribute<DisplayAttribute>().Name,
                    Key = ((int) p).ToString()
                });
            TextTypes = Enum.GetValues<WritepadType>()
                .Select(p => new DropdownOption
                {
                    Text = p.GetAttribute<DisplayAttribute>().Name,
                    Key = ((int)p).ToString()
                });
            HandOptions = Enum.GetValues<Hand>()
                .Select(p => new DropdownOption
                {
                    Text = p.GetAttribute<DisplayAttribute>().Name,
                    Key = ((int)p).ToString()
                });
        }

        protected override async Task OnParametersSetAsync()
        {
            if (Enum.TryParse<Handedness>((await AuthenticationStateTask).User.FindFirstValue(ClaimTypeConstants.Handedness), out var handedness))
            {
                Handedness = handedness;
            }

            NewWritepad = new NewWritepadModel()
            {
                Hand = Handedness == Handedness.Both ? null : new DropdownOption
                {
                    Text = Handedness.ToHand().GetAttribute<DisplayAttribute>().Name,
                    Key = ((int)Handedness.ToHand()).ToString()
                }
            };
            EditContext = new EditContext(NewWritepad);
            CreateErrors = new ValidationMessageStore(EditContext);
            EditContext.OnValidationStateChanged += (sender, eventArgs) =>
            {
                if (EditContext.GetValidationMessages().IsNullOrEmpty())
                {
                    SubmitDisabled = false;
                }
                else
                {
                    SubmitDisabled = true;
                }
            };
            EditContext.OnFieldChanged += (sender, eventArgs) =>
            {
                CreateErrors.Clear();
            };

            Uri = new Uri(Navigation.Uri);
            foreach (var queryItem in QueryHelpers.ParseQuery(Uri.Query))
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
                var result = await Http.GetFromJsonAsync<WritepadsDTO>($"api/Writepad/?page={Page}");
                WritepadList = result.Writepads.ToList();
                AllCount = result.AllCount;
                StateHasChanged();
            }
        }

        protected string GetWritepadTextContent(WritepadDTO writepad)
        {
            var text = string.Empty;

            if (writepad is null)
            {
                return text;
            }

            if (writepad.Type == WritepadType.WordGroup)
            {
                text = writepad.Text.Content.Replace(" ", " - ");
            }
            else
            {
                text = writepad.Text?.Content ?? "امضاء.";
            }
            return text;
        }

        async Task OnPageChangeHandler(bool isNext)
        {
            var queries = HttpUtility.ParseQueryString(Uri.Query);
            queries["page"] = $"{Page + (isNext ? 1 : -1)}";
            var dic = queries.AllKeys.ToDictionary(k => k, k => queries[k]);
            Navigation.NavigateTo(QueryHelpers.AddQueryString(Uri.AbsolutePath, dic));
            WritepadList = null;
            await OnParametersSetAsync();
        }

        void AddOnClickHandler(ItemClickedArgs args)
        {
            if (!CreateDialogOpen) {
                CreateDialogOpen = true;
                StateHasChanged();
            }
        }

        async Task SubmitWritepad()
        {
            SaveButton.State = ButtonState.Acting;
            try
            {
                var isValid = EditContext.Validate();

                if (!isValid)
                {
                    return;
                }

                var result = await Http.PostAsJsonAsync($"api/Writepad", (NewWritepadDTO)NewWritepad);

                switch (result.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        var writepads = await result.Content.ReadFromJsonAsync<IEnumerable<WritepadDTO>>();

                        WritepadList.InsertRange(0, writepads.OrderByDescending(e => e.SpecifiedNumber));
                        WritepadList = WritepadList.Take(10).ToList();
                        AllCount += (int)NewWritepad.Number;

                        CreateDialogOpen = false;
                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                        var error = await result.Content.ReadFromJsonAsync<WritepadCreationError>();
                        var errorText = error switch
                        {
                            WritepadCreationError.SignNotAllowed => "ایجاد تخته‌ی امضاء با نوع ورودی و دست یکسان تنها ۷ عدد در هر ۱۲ ساعت مجاز است.",
                            _ => null,
                        };
                        if (errorText is not null)
                        {
                            CreateErrors.Add(new FieldIdentifier(EditContext.Model, fieldName: string.Empty), errorText);
                            EditContext.NotifyValidationStateChanged();
                        }
                        break;
                }
            }
            finally
            {
                SaveButton.State = ButtonState.None;
            }
        }

        async Task SendCommentHandler()
        {
            SendCommentButton.State = ButtonState.Acting;
            try
            {
                var result = await Http.PostAsJsonAsync($"api/Comment", CommentDTO);
                result.EnsureSuccessStatusCode();
                CurrentWritepad.CommentsStatus = WritepadCommentsStatus.NewFromUser;

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

            var comments = await Http.GetFromJsonAsync<ICollection<CommentDTO>>($"api/Comment?writepadId={writepad.SpecifiedNumber}");
            writepad.Comments = comments;
            if (writepad.CommentsStatus == WritepadCommentsStatus.NewFromAdmin)
            {
                writepad.CommentsStatus = WritepadCommentsStatus.None;
            }

            CommentDTO = new CommentDTO()
            {
                WritepadId = writepad.SpecifiedNumber,
            };
            CommentEditContext = new EditContext(CommentDTO);

            CommentsDialogOpen = true;
        }

        void InfoButtonHandler(MouseEventArgs args, WritepadDTO writepad)
        {
            CurrentWritepad = writepad;
            InfoDialogOpen = true;
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
                var result = await Http.DeleteAsync($"api/Writepad/{CurrentWritepad.SpecifiedNumber}");
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

        async Task SubmitForApproval(MouseEventArgs args, WritepadDTO writepad)
        {
            try
            {
                var result = await Http.PutAsync($"api/Writepad/{writepad.SpecifiedNumber}?status={WritepadStatus.WaitForAcceptance}", null);
                switch (result.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        writepad.Status = WritepadStatus.WaitForAcceptance;
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

        async Task CancelApprovalRequest(MouseEventArgs args, WritepadDTO writepad)
        {
            try
            {
                var result = await Http.PutAsync($"api/Writepad/{writepad.SpecifiedNumber}?status={WritepadStatus.Draft}", null);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadFromJsonAsync<WritepadStatus>();
                    writepad.Status = response;
                }
            }
            finally
            {
            }
        }

        void EditHandler(MouseEventArgs args, int id)
        {
            var query = Uri.Query;
            Navigation.NavigateTo($"/writepad/{id}{(string.IsNullOrWhiteSpace(query) ? "" : $"?writepadsQuery={Uri.EscapeDataString(query.TrimStart('?'))}")}");
        }

        void TextTypeChangeHandler(DropdownChangeArgs args)
        {
            if (args.Option.Key == ((int)WritepadType.Sign).ToString())
            {
                IsSignSelected = true;
                NewWritepad.Number = Math.Min(7, NewWritepad.Number);
            }
            else
            {
                IsSignSelected = false;
            }
        }

        public class NewWritepadModel
        {
            [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "نوع ورودی")]
            public IDropdownOption PointerType { get; set; }
            [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "نوع داده")]
            public IDropdownOption WritepadType { get; set; }
            [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "نوع دست")]
            public IDropdownOption Hand { get; set; }
            [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Range(1, 14, ErrorMessageResourceName = "Range", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "تعداد")]
            public double Number { get; set; } = 1;

            public static explicit operator NewWritepadDTO(NewWritepadModel model)
            {
                return new NewWritepadDTO
                {
                    PointerType = Enum.Parse<PointerType>(model.PointerType.Key),
                    Type = Enum.Parse<WritepadType>(model.WritepadType.Key),
                    Hand = Enum.Parse<Hand>(model.Hand.Key),
                    Number = (int)model.Number
                };
            }
        }
    }
}
