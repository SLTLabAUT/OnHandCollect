using Blazored.LocalStorage;
using BlazorFluentUI;
using FProject.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
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
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<IdentityAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<IdentityAuthenticationStateProvider>());
            builder.Services.AddScoped<AuthorizeApi>();

            builder.Services.AddTransient<UnauthorizedMessageHandler>();
            builder.Services.AddHttpClient("FProject.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
                .AddHttpMessageHandler<UnauthorizedMessageHandler>();
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("FProject.ServerAPI"));

            //builder.Services.AddApiAuthorization()
            //    .AddAccountClaimsPrincipalFactory<CustomAccountClaimsPrincipalFactory>();

            builder.Services.AddBlazorFluentUI();

            builder.Services.AddBlazoredLocalStorage();
        }

        public static void ConfigureProviders(IServiceProvider services)
        {
            //var themeProvider = services.GetService<ThemeProvider>();
            //themeProvider.UpdateTheme(new DefaultPaletteDark());

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
