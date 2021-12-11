using FProject.Shared;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FProject.Client.Pages
{
    public partial class Dataset
    {
        [Inject]
        HttpClient Http { get; set; }

        Stats Stats { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (Stats is null)
            {
                var result = await Http.GetAsync($"api/Stats/").ConfigureAwait(false);
                if (result.IsSuccessStatusCode)
                {
                    Stats = await result.Content.ReadFromJsonAsync<Stats>().ConfigureAwait(false);
                }
            }
        }
    }
}
