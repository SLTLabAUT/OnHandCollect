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
using FProject.Client.Resources;
using Microsoft.AspNetCore.Components.Web;

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

        bool CreateDialogOpen { get; set; }
        bool DeleteDialogOpen { get; set; }
        bool SubmitDisabled { get; set; }
        CreateWritepadModel NewWritepad { get; set; }
        EditContext EditContext { get; set; }
        List<WritepadDTO> WritepadList { get; set; } = new List<WritepadDTO>();
        BFUCommandBarItem[] Items { get; set; }
        IEnumerable<IBFUDropdownOption> PointerTypes { get; set; }
        IEnumerable<IBFUDropdownOption> TextTypes { get; set; }
        WritepadDTO CurrentWritepad { get; set; }

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
            NewWritepad = new CreateWritepadModel()
            {
                PointerType = PointerTypes.First(),
                TextType = TextTypes.First()
            };
            NewWritepad = new CreateWritepadModel();
            EditContext = new EditContext(NewWritepad);
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

            return base.OnInitializedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            try
            {
                WritepadList = await Http.GetFromJsonAsync<List<WritepadDTO>>("api/Writepad");
            }
            catch (AccessTokenNotAvailableException exception)
            {
                exception.Redirect();
            }
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

            try
            {
                var response = await Http.PostAsync($"api/Writepad?pointerType={NewWritepad.PointerType.Key}&textType={NewWritepad.TextType.Key}", null);
                var writepad = await response.Content.ReadFromJsonAsync<WritepadDTO>();
                WritepadList.Add(writepad);
            }
            catch (AccessTokenNotAvailableException exception)
            {
                exception.Redirect();
            }
            finally
            {
                CreateDialogOpen = false;
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
                await Http.DeleteAsync($"api/Writepad/{CurrentWritepad.Id}");
                WritepadList.Remove(CurrentWritepad);
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
                var response = await Http.PutAsync($"api/Writepad/{writepad.Id}?status={WritepadStatus.WaitForAcceptance}", null);
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
                var response = await Http.PutAsync($"api/Writepad/{writepad.Id}?status={WritepadStatus.Editing}", null);
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

        public class CreateWritepadModel
        {
            [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "نوع ورودی")]
            public IBFUDropdownOption PointerType { get; set; }
            [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "نوع داده")]
            public IBFUDropdownOption TextType { get; set; }
        }
    }
}
