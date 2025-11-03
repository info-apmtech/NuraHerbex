using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ServiceAPI
{
    public class CommonAPIService
    {
		private readonly UserManager<RegisterUser> _usermanager;
		public CommonAPIService(UserManager<RegisterUser> userManager)
		{
			_usermanager = userManager;
		}
		public async Task<bool> SendPasswordResetLinkAsync(string email)
		{
			var user = await _usermanager.FindByEmailAsync(email);
			if (user == null) return false;

			var token = await _usermanager.GeneratePasswordResetTokenAsync(user);
			var resetLink = $"https://localhost:7292/Authentication/ResetPassword?email={email}&token={Uri.EscapeDataString(token)}";

			// You can send this via EmailService (SMTP / SendGrid / etc.)
			Console.WriteLine($"Password reset link: {resetLink}");

			return true;
		}
	}
}
