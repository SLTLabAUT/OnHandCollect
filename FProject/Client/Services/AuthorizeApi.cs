using Blazored.LocalStorage;
using FProject.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FProject.Client.Services
{
    public class AuthorizeApi
    {
        private HttpClient _httpClient;
        private IdentityAuthenticationStateProvider _authenticationStateProvider;

        public AuthorizeApi(HttpClient httpClient, IdentityAuthenticationStateProvider authenticationStateProvider)
        {
            _httpClient = httpClient;
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<LoginResponse> Login(LoginDTO loginDTO)
        {
            var result = await _httpClient.PostAsJsonAsync("api/Identity/Login", loginDTO).ConfigureAwait(false);
            var response = await result.Content.ReadFromJsonAsync<LoginResponse>().ConfigureAwait(false);

            if (response.LoggedIn)
            {
                var token = response.AccessToken;
                await _authenticationStateProvider.MarkUserAsAuthenticated(token).ConfigureAwait(false);
            }

            return response;
        }

        public async Task UpdateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException();
            }

            await _authenticationStateProvider.MarkUserAsAuthenticated(token).ConfigureAwait(false);
        }

        public async Task Logout()
        {
            var result = await _httpClient.PostAsync("api/Identity/Logout", null).ConfigureAwait(false);
            if (!result.IsSuccessStatusCode && result.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException();
            }
            await PostLogout().ConfigureAwait(false);
        }

        public async Task PostLogout()
        {
            await _authenticationStateProvider.MarkUserAsLoggedOut().ConfigureAwait(false);
        }

        public async Task<RegisterResponse> Register(RegisterDTO registerDTO)
        {
            var result = await _httpClient.PostAsJsonAsync("api/Identity/Register", registerDTO).ConfigureAwait(false);
            var response = await result.Content.ReadFromJsonAsync<RegisterResponse>().ConfigureAwait(false);
            return response;
        }
    }
}
