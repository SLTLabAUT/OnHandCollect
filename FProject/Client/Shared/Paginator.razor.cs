using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Client.Shared
{
    public partial class Paginator
    {
        [Parameter]
        public int Page { get; set; }
        [Parameter]
        public EventCallback<bool> OnPageChangeHandler { get; set; }
        [Parameter]
        public bool NextDisabled { get; set; }

        bool BackDisabled => Page == 1;

        async Task OnClickHandler(bool isNext)
        {
            await OnPageChangeHandler.InvokeAsync(isNext);
        }
    }
}
