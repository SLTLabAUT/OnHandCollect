using FProject.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace FProject.Client.Pages
{
    public partial class WritepadPanel
    {
        [Inject]
        NavigationManager Navigation { get; set; }

        [Parameter]
        public Writepad Parent { get; set; }

        bool PanelCollapsed { get; set; }
        bool CollapsedChanged { get; set; }
        bool UndoDisabled { get; set; }
        bool RedoDisabled { get; set; } = true;
        bool MoveDisabled { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (CollapsedChanged)
            {
                CollapsedChanged = false;
                await Parent.JSRef.InvokeVoidAsync("redraw");
            }
        }

        void OpenCollapseHandler(MouseEventArgs args)
        {
            PanelCollapsed = !PanelCollapsed;
            CollapsedChanged = true;
        }

        async Task UndoRedoHander(bool isRedo = false)
        {
            if (isRedo)
            {
                await Parent.JSRef.InvokeVoidAsync("redo");
            }
            else
            {
                await Parent.JSRef.InvokeVoidAsync("undo");
            }
        }

        void AutoSaveChangedHandler(bool checkedBool)
        {
            Parent.SaveTimer.Enabled = checkedBool;
            Parent.AutoSaveChecked = checkedBool;
        }

        async Task ChangeDefaultModeHandler(DrawingMode mode)
        {
            await Parent.JSRef.InvokeVoidAsync("changeDefaultMode", mode);
            MoveDisabled = !MoveDisabled;
        }

        public void StateHasChangedPublic()
        {
            StateHasChanged();
        }

        public void UndoRedoUpdator(bool undo, bool redo)
        {
            UndoDisabled = !undo;
            RedoDisabled = !redo;
            StateHasChanged();
        }
    }
}
