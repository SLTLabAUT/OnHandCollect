using BlazorFluentUI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Client.Shared
{
    public partial class Button : ButtonParameters
    {
        [Inject]
        IJSRuntime JS { get; set; }

        [Parameter]
        public ButtonType ButtonType { get; set; }
        [Parameter]
        public bool ShowDoneOnEnd { get; set; }
        [Parameter]
        public bool ManualStateControl { get; set; }
        public ButtonState State
        {
            get => _state;
            set
            {
                if (ManualStateControl && _state != value)
                {
                    _state = value;
                }
            }
        }

        protected ButtonState _state;
        protected Icon doneRef { get; set; }
        protected Icon lastDone { get; set; }
        protected DotNetObjectReference<Button> component { get; set; }

        public bool DisabledCondition => Disabled || State != ButtonState.None;
        public string ButtonStyle => $"{Style} {(State != ButtonState.None ? ".is-Blur" : "")}";

        protected override void OnInitialized()
        {
            component = DotNetObjectReference.Create(this);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (State == ButtonState.Done && doneRef != lastDone)
            {
                lastDone = doneRef;
                await JS.InvokeVoidAsync("FProject.AddDoneEndHandler", component, doneRef.RootElementReference);
            }
        }

        public async Task ClickHandler(MouseEventArgs args)
        {
            if (!OnClick.HasDelegate)
            {
                return;
            }

            if (!ManualStateControl)
            {
                _state = ButtonState.Acting;
            }

            await OnClick.InvokeAsync();

            if (!ManualStateControl)
            {
                _state = GetDoneState();
            }
        }

        public ButtonState GetDoneState()
        {
            if (ShowDoneOnEnd)
            {
                return ButtonState.Done;
            }
            else
            {
                return ButtonState.None;
            }
        }

        [JSInvokable]
        public void DoneEndHandler()
        {
            _state = ButtonState.None;
            StateHasChanged();
        }
    }

    public enum ButtonType
    {
        Default,
        Primary,
        Submit,
        Icon
    }

    public enum ButtonState
    {
        None,
        Acting,
        Done
    }
}
