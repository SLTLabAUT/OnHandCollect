using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FProject.Client.Services
{
    public class UnauthorizedMessageHandler : MessageProcessingHandler
    {
        private NavigationManager _navigation;

        public UnauthorizedMessageHandler(NavigationManager navigation)
        {
            _navigation = navigation;
        }

        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _navigation.NavigateTo("/identity/postlogout");
            }

            return response;
        }
    }
}
