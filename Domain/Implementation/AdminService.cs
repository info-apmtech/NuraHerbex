using Domain.Interface;
using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Implementation
{
	public class AdminService : IAdmin
	{
		private readonly UserManager<RegisterUser> _usermanager;
		//private readonly IAdmin _adminService;
		private readonly IConfiguration _config;
		public AdminService(UserManager<RegisterUser> userManager,IConfiguration config)
		{
			//_adminService = adminservice;
			_usermanager = userManager;
			_config = config;
		}

		public async Task<List<RegisterUser>> GetUsersByRoleAsync(UserRole role)
		{
			var users = await _usermanager.Users.Where(u => u.Role == role).OrderByDescending(u => u.CreatedAt).ToListAsync();
			return users;
		}

		public async Task<RegisterUser?> GetUserByIdAsync(string id)
		{
			return await _usermanager.FindByIdAsync(id);
		}

		public async Task<IdentityResult> AddOrUpdateUserAsync(RegisterUser user)
		{
			//if (string.IsNullOrEmpty(user.Id))
			//{
				user.CreatedAt = DateTime.Now;
				return await _usermanager.CreateAsync(user, user.Password);
			//}
			//else
			//{
			//	var existing = await _adminService.FindByIdAsync(user.Id);
			//	if (existing == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

			//	existing.Email = user.Email;
			//	existing.PhoneNumber = user.PhoneNumber;
			//	existing.Role = user.Role;
			//	existing.UpdatedAt = DateTime.Now;

			//	await _adminService.UpdateAsync(existing);
			//	return IdentityResult.Success;
			//}
		}
		public async Task<LoginResponse?> SignInAsync(LoginModel model)
		{
			var user = await _usermanager.FindByNameAsync(model.Username);
			if (user == null)
				return null;

			var validPassword = await _usermanager.CheckPasswordAsync(user, model.Password);
			if (!validPassword)
				return null;

			var roles = await _usermanager.GetRolesAsync(user);

			// Create claims
			var claims = new List<Claim>
		{
			new Claim(ClaimTypes.NameIdentifier, user.Id),
			new Claim(ClaimTypes.Name, user.UserName)
		};

			foreach (var role in roles)
				claims.Add(new Claim(ClaimTypes.Role, role));

			// Generate JWT
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: _config["Jwt:Issuer"],
				audience: _config["Jwt:Audience"],
				claims: claims,
				expires: DateTime.Now.AddHours(1),
				signingCredentials: creds
			);

			var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

			return new LoginResponse
			{
				Token = tokenString,
				Expiration = token.ValidTo,
				Username = user.UserName,
				Roles = roles.FirstOrDefault() ?? "User"
			};
		}
	}

}
