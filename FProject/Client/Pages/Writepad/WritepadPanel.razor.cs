using BlazorFluentUI;
using FProject.Client.Shared;
using FProject.Shared;
using FProject.Shared.Models;
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
        [Inject]
        ThemeProvider ThemeProvider { get; set; }

        [Parameter]
        public Writepad Parent { get; set; }

        bool PanelCollapsed { get; set; }
        bool CollapsedChanged { get; set; }
        bool UndoDisabled { get; set; }
        bool RedoDisabled { get; set; } = true;
        DrawingMode CurrentDefaultMode { get; set; }
        public bool NotAllowedDialogOpen { get; set; }
        bool LeaveConfirmDialogOpen { get; set; }
        bool HelpDialogOpen { get; set; }
        public Button SaveButton { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (CollapsedChanged)
            {
                CollapsedChanged = false;
                await Parent.JSRef.InvokeVoidAsync("redraw");
            }
        }

        async Task HelpDismissHandler(EventArgs args)
        {
            await Parent.JSRef.InvokeVoidAsync("pauseVideo");
            HelpDialogOpen = false;
        }

        void OpenCollapseHandler(MouseEventArgs args)
        {
            PanelCollapsed = !PanelCollapsed;
            CollapsedChanged = true;
        }

        async Task LeaveHandler(MouseEventArgs args)
        {
            if (await Parent.JSRef.InvokeAsync<bool>("isSaveRequired"))
            {
                LeaveConfirmDialogOpen = true;
            }
            else
            {
                Leave();
            }
        }

        void Leave()
        {
            Navigation.NavigateTo($"/{(Parent.AdminReview ? "writepadsadmin" : "writepads")}{(string.IsNullOrWhiteSpace(Parent.WritepadsQuery) ? "" : $"?{Parent.WritepadsQuery}")}");
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

        async Task DeleteSelectedHander()
        {
            await Parent.JSRef.InvokeVoidAsync("deleteSelected");
        }

        void AutoSaveChangedHandler(bool? checkedBool)
        {
            Parent.SaveTimer.Enabled = checkedBool ?? false;
            Parent.AutoSaveChecked = checkedBool ?? false;
        }

        async Task ChangeDefaultModeHandler(DrawingMode mode)
        {
            await Parent.JSRef.InvokeVoidAsync("changeDefaultMode", mode);
            CurrentDefaultMode = mode;
        }

        public void StateHasChangedPublic()
        {
            StateHasChanged();
        }

        public void DefaultModeUpdator(DrawingMode mode)
        {
            CurrentDefaultMode = mode;
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
