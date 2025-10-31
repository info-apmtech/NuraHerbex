using Domain.Interface;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Implementation
{
	public class TokenService : ITokenService
	{
		private readonly IHttpContextAccessor _httpContextAccessor;
		public TokenService(IHttpContextAccessor httpContextAccessor)
		{
			_httpContextAccessor = httpContextAccessor;
		}
		public string GetUserIdFromToken(string token)
		{
			var handler = new JwtSecurityTokenHandler();
			var jwtToken = handler.ReadJwtToken(token);

			var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
			return userId;
		}

		public string GetUserIdFromAuthorizationHeader()
		{
			var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();

			if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
				return null;

			var token = authHeader.Substring("Bearer ".Length).Trim();
			return GetUserIdFromToken(token);
		}
	}
}
