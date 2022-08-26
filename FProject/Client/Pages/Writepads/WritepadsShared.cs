using BlazorFluentUI;
using FProject.Shared;
using FProject.Shared.Extensions;
using Microsoft.AspNetCore.Components;
using System.Net.Http;

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
