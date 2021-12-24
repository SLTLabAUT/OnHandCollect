using BlazorFluentUI;
using FProject.Client.Shared;
using FProject.Shared;
using FProject.Shared.Extensions;
using FProject.Shared.Models;
using FProject.Shared.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FProject.Client.Pages
{
    public partial class Export
    {
        [Inject]
        HttpClient Http { get; set; }

        int? ExportedCount { get; set; }
        bool IsExporting { get; set; }
        IEnumerable<IDropdownOption> ModeOptions { get; set; }
        IEnumerable<IDropdownOption> TextTypeOptions { get; set; }
        ExportModel Model { get; set; }
        EditContext EditContext { get; set; }
        Button SubmitButton { get; set; }

        protected override void OnInitialized()
        {
            ModeOptions = Enum.GetValues<ExportMode>()
                .Select(p => new DropdownOption
                {
                    Text = p.GetAttribute<DisplayAttribute>().Name,
                    Key = ((int)p).ToString()
                });
            TextTypeOptions = Enum.GetValues<FProject.Shared.TextType>()
                .Select(p => new DropdownOption
                {
                    Text = p.GetAttribute<DisplayAttribute>().Name,
                    Key = ((int)p).ToString()
                });
        }

        protected override void OnParametersSet()
        {
            Model = new ExportModel
            {
                Mode = ModeOptions.First(),
                TextType = TextTypeOptions.First()
            };
            EditContext = new EditContext(Model);
        }

        async Task FormHandler()
        {
            IsExporting = true;
            SubmitButton.State = ButtonState.Acting;
            try
            {
                //var lastTimeout = Http.Timeout;
                //Http.Timeout = Timeout.InfiniteTimeSpan;
                var url = $"api/Writepad/Export?mode={Model.Mode.Key}&textType={Model.TextType.Key}";
                if (Model.Start is not null)
                {
                    url += $"&start={Uri.EscapeDataString(Model.Start.ToString())}";
                }
                if (Model.End is not null)
                {
                    url += $"&end={Uri.EscapeDataString(Model.End.ToString())}";
                }
                ExportedCount = await Http.GetFromJsonAsync<int?>(url);
                //Http.Timeout = lastTimeout;
                //Console.WriteLine(lastTimeout);
            }
            finally
            {
                IsExporting = false;
                SubmitButton.State = ButtonState.None;
            }
        }

        void DismissMessagebarHandler()
        {
            ExportedCount = null;
            IsExporting = false;
        }

        public class ExportModel
        {
            [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
            [Display(Name = "نوع")]
            public IDropdownOption Mode { get; set; }
            [Display(Name = "ابتدای بازه‌ی زمانی")]
            public DateTime? Start { get; set; }
            [Display(Name = "انتهای بازه‌ی زمانی")]
            public DateTime? End { get; set; }
            [Display(Name = "نوع نویسه‌ی مرجع")]
            public IDropdownOption TextType { get; set; }
        }
    }
}
