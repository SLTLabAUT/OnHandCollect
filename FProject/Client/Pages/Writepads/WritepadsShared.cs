using BlazorFluentUI;
using FProject.Shared;
using FProject.Shared.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FProject.Client.Pages
{
    public class WritepadsShared : ComponentBase
    {
        [Inject]
        protected ThemeProvider ThemeProvider { get; set; }
        [Inject]
        protected HttpClient Http { get; set; }
        [Inject]
        protected NavigationManager Navigation { get; set; }

        [Parameter]
        [SupplyParameterFromQuery]
        public int? Page { get; set; }
        [Parameter]
        public WritepadStatus? Status { get; set; }
        [Parameter]
        public WritepadType? Type { get; set; }
        [Parameter]
        [SupplyParameterFromQuery]
        public string? UserEmail { get; set; }
        [Parameter]
        [SupplyParameterFromQuery]
        public int? WritepadId { get; set; }

        protected Uri Uri { get; set; }
        protected List<WritepadDTO> WritepadList { get; set; }
        protected bool ShouldLoadWritepadList { get; set; } = true;

        protected int AllCount { get; set; }
        protected bool IsAdminPage { get; set; }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);

            if (Page == default)
            {
                Page = 1;
            }
        }

        protected override void OnParametersSet()
        {
            Uri = new Uri(Navigation.Uri);
            foreach (var queryItem in QueryHelpers.ParseQuery(Uri.Query))
            {
                switch (queryItem.Key)
                {
                    case "status":
                        Status = Enum.Parse<WritepadStatus>(queryItem.Value);
                        break;
                    case "type":
                        Type = Enum.Parse<WritepadType>(queryItem.Value);
                        break;
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (ShouldLoadWritepadList && Page != default)
            {
                ShouldLoadWritepadList = false;
                string uri = $"api/Writepad/?page={Page}&status={Status}&type={Type}";
                if (IsAdminPage)
                {
                    uri += $"&admin={IsAdminPage}&userEmail={UserEmail}&writepadId={WritepadId}";
                }
                WritepadsDTO result;
                try
                {
                    result = await Http.GetFromJsonAsync<WritepadsDTO>(uri);
                }
                catch
                {
                    ShouldLoadWritepadList = true;
                    throw;
                }
                WritepadList = result.Writepads.ToList();
                AllCount = result.AllCount;
                StateHasChanged();
            }
        }

        protected async Task OnPageChangeHandler(bool isNext)
        {
            string uri = Navigation.GetUriWithQueryParameter("page", Page + (isNext ? 1 : -1));
            Navigation.NavigateTo(uri);
            ShouldLoadWritepadList = true;
            WritepadList = null;
            await Task.CompletedTask;
        }

        protected string GetWritepadTextContent(WritepadDTO writepad)
        {
            var text = string.Empty;

            if (writepad is null)
            {
                return text;
            }

            if (writepad.Text?.Type.IsGroupedText() == true)
            {
                text = writepad.Text.Content.Replace("\n", " - ");
            }
            else
            {
                text = writepad.Text?.Content ?? "امضاء.";
            }
            return text;
        }

        protected string GetWritepadTextContentClassName(WritepadDTO writepad)
        {
            if (writepad.Type == WritepadType.NumberGroup)
            {
                return "ltr-text";
            }
            return string.Empty;
        }

        protected (string tooltip, string iconName) GetPointerTypeUIElementValues(WritepadDTO writepad)
        {
            string tooltip = string.Empty;
            string iconName = string.Empty;
            switch (writepad.PointerType)
            {
                case PointerType.Mouse:
                    tooltip = "موس";
                    iconName = "TouchPointer";
                    break;
                case PointerType.Touchpad:
                    tooltip = "تاچ‌پد";
                    iconName = "TouchPointer";
                    break;
                case PointerType.Pen:
                    tooltip = "قلم دیجیتالی";
                    iconName = "PenWorkspace";
                    break;
                case PointerType.Touch:
                    tooltip = "لمس";
                    iconName = "Touch";
                    break;
                case PointerType.TouchPen:
                    tooltip = "قلم لمسی";
                    iconName = "EditMirrored";
                    break;
            }
            return (tooltip, iconName);
        }

        protected (string tooltip, string iconName) GetTextTypeUIElementValues(WritepadDTO writepad)
        {
            string tooltip = string.Empty;
            string iconName = string.Empty;
            switch (writepad.Type)
            {
                case WritepadType.Text:
                    tooltip = "متن";
                    iconName = "TextDocument";
                    break;
                case WritepadType.WordGroup:
                    tooltip = "گروه کلمات تکی";
                    iconName = "TextBox";
                    break;
                case WritepadType.Sign:
                    tooltip = "امضاء";
                    iconName = "InsertSignatureLine";
                    break;
                case WritepadType.WordGroup2:
                    tooltip = "گروه کلمات دوتایی";
                    iconName = "TextBox";
                    break;
                case WritepadType.WordGroup3:
                    tooltip = "گروه کلمات سه‌تایی";
                    iconName = "TextBox";
                    break;
                case WritepadType.NumberGroup:
                    tooltip = "گروه ارقام";
                    iconName = "NumberField";
                    break;
            }
            return (tooltip, iconName);
        }
    }
}
