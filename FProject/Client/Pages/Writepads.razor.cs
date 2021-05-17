﻿using BlazorFluentUI;
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
    public partial class Writepads
    {
        [Inject]
        ThemeProvider ThemeProvider { get; set; }
        [Inject]
        HttpClient Http { get; set; }
        [Inject]
        NavigationManager Navigation { get; set; }

        int Page { get; set; } = 1;
        int AllCount { get; set; }
        bool CreateDialogOpen { get; set; }
        bool DeleteDialogOpen { get; set; }
        bool SubmitDisabled { get; set; }
        bool IsSignSelected { get; set; }
        NewWritepadModel NewWritepad { get; set; }
        ValidationMessageStore CreateErrors { get; set; }
        EditContext EditContext { get; set; }
        List<WritepadDTO> WritepadList { get; set; }
        BFUCommandBarItem[] Items { get; set; }
        IEnumerable<IBFUDropdownOption> PointerTypes { get; set; }
        IEnumerable<IBFUDropdownOption> TextTypes { get; set; }
        WritepadDTO CurrentWritepad { get; set; }

        bool HaveNextPage => Page * 10 < AllCount;

        protected override Task OnInitializedAsync()
        {
            Items = new BFUCommandBarItem[] {
                new BFUCommandBarItem() { Text = "ایجاد تخته‌ی جدید", IconName = "Add", Key = "add", OnClick = AddOnClickHandler }
            };
            PointerTypes = Enum.GetValues<PointerType>()
                .Select(p => new BFUDropdownOption {
                    Text = p.GetAttribute<DisplayAttribute>().Name,
                    Key = ((int) p).ToString()
                });
            TextTypes = Enum.GetValues<FProject.Shared.TextType>()
                .Select(p => new BFUDropdownOption
                {
                    Text = p.GetAttribute<DisplayAttribute>().Name,
                    Key = ((int)p).ToString()
                });

            return base.OnInitializedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            NewWritepad = new NewWritepadModel();
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
                Console.WriteLine("OnValidationStateChanged");
            };
            EditContext.OnFieldChanged += (sender, eventArgs) =>
            {
                CreateErrors.Clear();
                Console.WriteLine("OnFieldChanged");
            };

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

            var result = await Http.GetFromJsonAsync<WritepadsDTO>($"api/Writepad/?page={Page}");
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

        void AddOnClickHandler(ItemClickedArgs args)
        {
            if (!CreateDialogOpen) {
                CreateDialogOpen = true;
                StateHasChanged();
            }
        }

        async Task SubmitWritepad()
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
                    if (error == WritepadCreationError.SignNotAllowed)
                    {
                        CreateErrors.Add(new FieldIdentifier(EditContext.Model, fieldName: string.Empty), "ایجاد تخته‌ی امضا با نوع ورودی یکسان هر ۱۲ ساعت یک‌بار مجاز است.");
                        EditContext.NotifyValidationStateChanged();
                    }
                    break;
            }
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
                await Http.DeleteAsync($"api/Writepad/{CurrentWritepad.SpecifiedNumber}");
                WritepadList.Remove(CurrentWritepad);
                AllCount--;
            }
            catch (AccessTokenNotAvailableException exception)
            {
                exception.Redirect();
            }
            finally
            {
                CurrentWritepad = null;
                DeleteDialogOpen = false;
            }
        }

        async Task SubmitForApproval(MouseEventArgs args, WritepadDTO writepad)
        {
            if (writepad.Status != WritepadStatus.Editing)
            {
                return;
            }

            try
            {
                var response = await Http.PutAsync($"api/Writepad/{writepad.SpecifiedNumber}?status={WritepadStatus.WaitForAcceptance}", null);
                if (response.IsSuccessStatusCode)
                {
                    writepad.Status = WritepadStatus.WaitForAcceptance;
                }
            }
            catch (AccessTokenNotAvailableException exception)
            {
                exception.Redirect();
            }
        }

        async Task CancelApprovalRequest(MouseEventArgs args, WritepadDTO writepad)
        {
            if (writepad.Status != WritepadStatus.WaitForAcceptance)
            {
                return;
            }

            try
            {
                var response = await Http.PutAsync($"api/Writepad/{writepad.SpecifiedNumber}?status={WritepadStatus.Editing}", null);
                if (response.IsSuccessStatusCode)
                {
                    writepad.Status = WritepadStatus.Editing;
                }
            }
            catch (AccessTokenNotAvailableException exception)
            {
                exception.Redirect();
            }
        }

        void EditHandler(MouseEventArgs args, int id)
        {
            Navigation.NavigateTo($"/writepad/{id}");
        }

        void TextTypeChangeHandler(BFUDropdownChangeArgs args)
        {
            if (args.Option.Key == ((int)FProject.Shared.TextType.Sign).ToString())
            {
                IsSignSelected = true;
                NewWritepad.Number = 1;
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
            public IBFUDropdownOption PointerType { get; set; }
            [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "نوع داده")]
            public IBFUDropdownOption TextType { get; set; }
            [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Range(1, 25, ErrorMessageResourceName = "Range", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "تعداد")]
            public double Number { get; set; } = 1;

            public static explicit operator NewWritepadDTO(NewWritepadModel model)
            {
                return new NewWritepadDTO
                {
                    PointerType = Enum.Parse<PointerType>(model.PointerType.Key),
                    TextType = Enum.Parse<FProject.Shared.TextType>(model.TextType.Key),
                    Number = (int)model.Number
                };
            }
        }
    }
}
