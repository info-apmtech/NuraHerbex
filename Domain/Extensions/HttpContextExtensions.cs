using Domain.Interface;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Extensions
{
	public static class HttpContextExtensions
	{
		public static string GetUserId(this IHttpContextAccessor httpContextAccessor, ITokenService tokenService)
		{
			var token = httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
			if (string.IsNullOrEmpty(token))
				return null;

			return tokenService.GetUserIdFromToken(token);
		}

	}
}
