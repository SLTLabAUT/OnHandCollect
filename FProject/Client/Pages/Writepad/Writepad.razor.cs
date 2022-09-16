using FProject.Shared;
using FProject.Shared.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Timers;

namespace FProject.Client.Pages
{
    public partial class Writepad : IAsyncDisposable
    {
        [Inject]
        IJSRuntime JS { get; set; }
        [Inject]
        HttpClient Http { get; set; }
        [Inject]
        NavigationManager Navigation { get; set; }

        [Parameter]
        public int Id { get; set; }
        [Parameter]
        public bool AdminReview { get; set; }
        [Parameter]
        public string WritepadsQuery { get; set; }

        WritepadPanel PanelRef { get; set; }
        float PadRatio { get; set; } = 0.7f;
        bool PanelCollapsed { get; set; }
        bool InitiationDone { get; set; }
        string WritepadCompressedJson { get; set; }
        public WritepadDTO WritepadInstance { get; set; }
        public bool AutoSaveChecked { get; set; }
        public bool IsSaving { get; set; }
        bool ForceNotRender { get; set; }
        public IJSObjectReference JSRef { get; set; }
        public Timer SaveTimer { get; set; } = new Timer(30000);

        private DotNetObjectReference<Writepad> componentRef;

        protected override async Task OnInitializedAsync()
        {
            componentRef = DotNetObjectReference.Create(this);

            JSRef = await JS.InvokeAsync<IJSObjectReference>("FProject.ImportGlobal", "Writepad", "/Pages/Writepad/Writepad.razor.js");

            SaveTimer.Elapsed += SaveTimerElapsedHandler;
        }

        protected override async Task OnParametersSetAsync()
        {
            var query = new Uri(Navigation.Uri).Query;
            foreach (var queryItem in QueryHelpers.ParseQuery(query))
            {
                switch (queryItem.Key)
                {
                    case "adminreview":
                        AdminReview = true;
                        break;
                    case "writepadsQuery":
                        WritepadsQuery = queryItem.Value;
                        break;
                }
            }

            var taskWritepadInstance = Http.GetFromJsonAsync<WritepadDTO>($"api/Writepad/{Id}?admin={AdminReview}");
            var taskWritepadCompressedJson = Http.GetStringAsync($"api/Writepad/{Id}?withPoints=true&admin={AdminReview}");
            await Task.WhenAll(taskWritepadInstance, taskWritepadCompressedJson);
            WritepadInstance = taskWritepadInstance.Result;
            WritepadCompressedJson = taskWritepadCompressedJson.Result;

            if (WritepadInstance.Text?.Type.IsGroupedText() == true)
            {
                var lines = WritepadInstance.Text.Content.Split("\n");
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = $"<span class=\"line-indicator\">خط {(i + 1).ToPersianNumber()}</span>:<br /><strong class=\"{(WritepadInstance.Text.Type == TextType.NumberGroup ? "ltr-text" : "")}\">{lines[i]}</strong>";
                }
                WritepadInstance.Text.Content = string.Join("<br />", lines);
            }
            else if (WritepadInstance.Text?.Content is not null)
            {
                WritepadInstance.Text.Content = $"<strong>{WritepadInstance.Text.Content}</strong>";
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (JSRef is null || InitiationDone)
            {
                return;
            }

            var currentTime = await Http.GetFromJsonAsync<long>($"api/Writepad/CurrentTime");
            await JSRef.InvokeVoidAsync("init", componentRef, PadRatio, currentTime - 1616060000000, WritepadCompressedJson);
            InitiationDone = true;
        }

        protected override bool ShouldRender()
        {
            return WritepadInstance is not null;
        }

        public async void SaveTimerElapsedHandler(object s = default, ElapsedEventArgs e = default)
        {
            if (!IsSaving)
            {
                var success = await JSRef.InvokeAsync<bool>("save");
                if (!success)
                {
                    throw new JSException("Couldn't Save!");
                }
            }
        }

        [JSInvokable]
        public async Task<SaveResponseDTO> Save(string savePointsDTOCompressedJson)
        //public async Task<SaveResponseDTO> Save(DateTimeOffset lastModified, DrawingPoint[] drawingPoints, DeletedDrawing[] deletedDrawings)
        {
            IsSaving = true;
            PanelRef.SaveButton.State = Shared.ButtonState.Acting;
            PanelRef.StateHasChangedPublic();
            try
            {
                //var response = await Http.PostAsJsonAsync($"api/Writepad/{Id}", new SavePointsDTO
                //{
                //    LastModified = lastModified,
                //    NewPoints = drawingPoints,
                //    DeletedDrawings = deletedDrawings
                //});
                //var stringContent = new StringContent(savePointsDTOCompressedJson, Encoding.ASCII, "application/json");
                //var stringContent = new StringContent(savePointsDTOJson, Encoding.UTF8, "application/json");
                var response = await Http.PostAsJsonAsync($"api/Writepad/{Id}?admin={AdminReview}", savePointsDTOCompressedJson);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var saveResponse = await response.Content.ReadFromJsonAsync<SavePointsResponseDTO>();
                        WritepadInstance.LastModified = saveResponse.LastModified;
                        if (saveResponse.LastSavedDrawingNumber != -1)
                        {
                            WritepadInstance.LastSavedDrawingNumber = saveResponse.LastSavedDrawingNumber;
                        }
                        break;
                    case HttpStatusCode.BadRequest:
                        var error = await response.Content.ReadFromJsonAsync<WritepadEditionError>();
                        if (error == WritepadEditionError.SignNotAllowed)
                        {
                            PanelRef.NotAllowedDialogOpen = true;
                        }
                        break;
                }

                return new SaveResponseDTO
                {
                    StatusCode = response.StatusCode,
                    ThrowError = !PanelRef.NotAllowedDialogOpen,
                    LastModified = WritepadInstance.LastModified,
                    LastSavedDrawingNumber = WritepadInstance.LastSavedDrawingNumber
                };
            }
            catch (AccessTokenNotAvailableException)
            {
                return new SaveResponseDTO
                {
                    StatusCode = HttpStatusCode.Unauthorized
                };
            }
        }

        [JSInvokable]
        public void ReleaseSaveToken()
        {
            IsSaving = false;
            PanelRef.SaveButton.State = Shared.ButtonState.Done;
            PanelRef.StateHasChangedPublic();
        }

        [JSInvokable]
        public void UndoRedoUpdator(bool undo, bool redo)
        {
            PanelRef.UndoRedoUpdator(undo, redo);
        }

        [JSInvokable]
        public void DefaultModeUpdator(DrawingMode mode)
        {
            PanelRef.DefaultModeUpdator(mode);
        }

        public async ValueTask DisposeAsync()
        {
            if (JSRef is not null)
            {
                if (InitiationDone)
                {
                    await JSRef.InvokeVoidAsync("dispose");
                }
                await JSRef.DisposeAsync();
            }
            SaveTimer.Dispose();
        }

        public class SaveResponseDTO
        {
            public HttpStatusCode StatusCode { get; set; }
            public bool ThrowError { get; set; } = true;
            public DateTimeOffset LastModified { get; set; }
            public int LastSavedDrawingNumber { get; set; }
        }
    }
}
