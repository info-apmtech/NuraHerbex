using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Extensions
{
	public static class HttpClientFactoryExtensions
	{
		public static HttpClient CreateAuthorizedClient(this IHttpClientFactory factory, IHttpContextAccessor httpContextAccessor)
		{
			var client = factory.CreateClient("NuraHerbexApi"); // << use the named client
			var token = httpContextAccessor.HttpContext?.Session.GetString("JwtToken");

			if (!string.IsNullOrEmpty(token))
			{
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			}

			return client;
		}

	}
}
