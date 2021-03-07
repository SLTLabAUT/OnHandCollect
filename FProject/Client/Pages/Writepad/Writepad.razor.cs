using BlazorFluentUI;
using FProject.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Timers;

namespace FProject.Client.Pages
{
    public partial class Writepad
    {
        [Inject]
        IJSRuntime JS { get; set; }
        [Inject]
        HttpClient Http { get; set; }
        [Inject]
        NavigationManager Navigation { get; set; }

        [Parameter]
        public int Id { get; set; }

        WritepadPanel PanelRef { get; set; }
        float PadRatio { get; set; } = 0.7f;
        bool PanelCollapsed { get; set; }
        string WritepadCompressedJson { get; set; }
        public WritepadDTO WritepadInstance { get; set; }
        public int Number { get; set; }
        public bool AutoSaveChecked { get; set; } = true;
        public bool IsSaving { get; set; }
        bool ForceNotRender { get; set; }
        public IJSObjectReference JSRef { get; set; }
        public Timer SaveTimer { get; set; } = new Timer(5000);

        private DotNetObjectReference<Writepad> componentRef;

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Init");
            componentRef = DotNetObjectReference.Create(this);

            JSRef = await JS.InvokeAsync<IJSObjectReference>("ImportGlobal", "Writepad", "/ts/Pages/Writepad/Writepad.razor.js");

            SaveTimer.Elapsed += SaveTimerElapsedHandler;
            //saveTimer.Start();
        }

        protected override async Task OnParametersSetAsync()
        {
            Console.WriteLine("Parameter");
            try
            {
                var taskWritepadInstance = Http.GetFromJsonAsync<WritepadWithNumberDTO>($"api/Writepad/{Id}?withNumber=true");
                var taskWritepadCompressedJson = Http.GetStringAsync($"api/Writepad/{Id}?withPoints=true");
                await Task.WhenAll(taskWritepadInstance, taskWritepadCompressedJson);
                var writepadResult = taskWritepadInstance.Result;
                WritepadInstance = writepadResult.Writepad;
                Number = writepadResult.Number;
                WritepadCompressedJson = taskWritepadCompressedJson.Result;
            }
            catch (AccessTokenNotAvailableException exception)
            {
                exception.Redirect();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            Console.WriteLine("Render");

            if (firstRender)
            {
                Console.WriteLine("First");
                return;
            }

            await JSRef.InvokeVoidAsync("init", componentRef, PadRatio, string.Empty);
        }

        protected override bool ShouldRender()
        {
            return WritepadInstance is not null;
        }

        public void SaveTimerElapsedHandler(object s = default, ElapsedEventArgs e = default)
        {
            if (!IsSaving)
            {
                IsSaving = true;
                //JSRef.InvokeVoidAsync("save");
            }
        }

        async Task UndoRedoHander(bool isRedo = false)
        {
            if (isRedo)
            {
                await JSRef.InvokeVoidAsync("redo");
            }
            else
            {
                await JSRef.InvokeVoidAsync("undo");
            }
        }

        void AutoSaveChangedHandler(bool checkedBool)
        {
            SaveTimer.Stop();
            AutoSaveChecked = checkedBool;
        }

        [JSInvokable]
        public async Task<SaveResponseDTO> Save(string savePointsDTOCompressedJson)
        //public async Task<SaveResponseDTO> Save(DateTimeOffset lastModified, DrawingPoint[] drawingPoints, DeletedDrawing[] deletedDrawings)
        {
            Console.WriteLine("Middle2!" + DateTime.Now);
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
                var response = await Http.PostAsJsonAsync($"api/Writepad/{Id}", savePointsDTOCompressedJson);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var saveResponse = await response.Content.ReadFromJsonAsync<SavePointsResponseDTO>();
                        WritepadInstance.LastModified = saveResponse.LastModified;
                        break;
                }

                return new SaveResponseDTO
                {
                    StatusCode = response.StatusCode,
                    LastModified = WritepadInstance.LastModified
                };
            }
            catch (AccessTokenNotAvailableException exception)
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
        }

        [JSInvokable]
        public void UndoRedoUpdator(bool undo, bool redo)
        {
            PanelRef.UndoRedoUpdator(undo, redo);
        }

        public class SaveResponseDTO
        {
            public HttpStatusCode StatusCode { get; set; }
            public DateTimeOffset LastModified { get; set; }
        }
    }
}
