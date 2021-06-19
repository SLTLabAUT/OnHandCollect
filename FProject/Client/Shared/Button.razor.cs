using BlazorFluentUI;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Client.Shared
{
    public partial class Button : ButtonParameters
    {
        [Parameter]
        public ButtonType ButtonType { get; set; }
        [Parameter]
        public bool IsActing { get; set; }
    }

    public enum ButtonType
    {
        Default,
        Primary,
        Submit,
        Icon
    }
}
