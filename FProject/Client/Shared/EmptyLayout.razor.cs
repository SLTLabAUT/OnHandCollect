using FProject.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Client.Shared
{
    public partial class EmptyLayout
    {
        [Inject]
        IJSRuntime JS { get; set; }

        protected UA ua;
        protected bool showBrowserWarning;
        protected string browserWarningDescription;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                Task.Run(DetectDevice);
            }
        }

        async Task DetectDevice()
        {
            await Task.Delay(1).ConfigureAwait(false);
            var jsInProcess = (IJSInProcessRuntime)JS; //TODO: Should become async on wasm 2
            ua = jsInProcess.Invoke<UA>("FProject.GetParsedUA");

            if (ua is null || ua.Browser is null || string.IsNullOrWhiteSpace(ua.Browser.Name))
            {
                return;
            }

            switch (ua.Browser.Name)
            {
                case "IE":
                case "Opera Mini":
                case "UCBrowser":
                case "Baidu":
                    showBrowserWarning = true;
                    browserWarningDescription = "مرورگر شما جزو مرورگرهای تحت پشتیبانی سامانه نمی‌باشد. لطفا از مرورگر دیگری استفاده کنید.";
                    break;
            }
            
            if (!showBrowserWarning && !string.IsNullOrWhiteSpace(ua.Browser.Version))
            {
                var version = Version.Parse(ua.Browser.Version);

                Version desired = null;
                switch (ua.Browser.Name)
                {
                    case "Mobile Safari":
                        desired = new Version(13, 0);
                        break;
                    case "Opera":
                        desired = new Version(44, 0);
                        break;
                    case "Safari":
                        desired = new Version(13, 0);
                        break;
                    case "Chrome":
                        desired = new Version(57, 0);
                        break;
                    case "Firefox":
                        desired = new Version(59, 0);
                        break;
                    case "Edge":
                        desired = new Version(16, 0);
                        break;
                }
                if (desired is not null)
                {
                    showBrowserWarning = version < desired;
                }
            }

            if (!showBrowserWarning && ua.Engine is not null && ua.Engine.Name == "WebKit" && !string.IsNullOrWhiteSpace(ua.Engine.Version))
            {
                var version = Version.Parse(ua.Engine.Version);
                Version desired = new Version(608, 0);
                showBrowserWarning = version < desired;
            }

            if (showBrowserWarning)
            {
                StateHasChanged();
            }
        }
    }
}
