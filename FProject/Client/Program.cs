using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FProject.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            Configure(builder);

            var host = builder.Build();

            ConfigureProviders(host.Services);

            await host.RunAsync();
        }

        public static void Configure(WebAssemblyHostBuilder builder)
        {
            builder.Services.AddHttpClient("FProject.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
                .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

            // Supply HttpClient instances that include access tokens when making requests to the server project
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("FProject.ServerAPI"));

            builder.Services.AddApiAuthorization();

        }

        public static void ConfigureProviders(IServiceProvider services)
        {
            // AuthenticationService.js only works with camelCase naming policy
            //try
            //{
            //    var jsRuntime = services.GetService<IJSRuntime>();
            //    var prop = typeof(JSRuntime).GetProperty("JsonSerializerOptions", BindingFlags.NonPublic | BindingFlags.Instance);
            //    JsonSerializerOptions value = (JsonSerializerOptions)Convert.ChangeType(prop.GetValue(jsRuntime, null), typeof(JsonSerializerOptions));
            //    value.PropertyNamingPolicy = null;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"SOME ERROR: {ex}");
            //}
        }
    }
}
