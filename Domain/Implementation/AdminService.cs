using Domain.Interface;
using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Implementation
{
	public class AdminService : IAdmin
	{
		private readonly UserManager<RegisterUser> _usermanager;
		private readonly IConfiguration _config;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly NuraDbContext _dbContext;
		private readonly IEmailService _emailService;

		// In-memory OTP store (You can store this in DB/Redis for production)
		private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry)> _otpStore = new();


		public AdminService(UserManager<RegisterUser> userManager,IConfiguration config,IHttpClientFactory httpClientFactory, NuraDbContext dbContext, IEmailService emailService)
		{
			_usermanager = userManager;
			_config = config;
			_httpClientFactory = httpClientFactory;
			_dbContext = dbContext;
			_emailService = emailService;
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
		public async Task<LoginResponseModel?> SignInAsync(RegisterUserViewModel model)
		{
			var user = await _usermanager.FindByNameAsync(model.Username);
			if (user == null || !await _usermanager.CheckPasswordAsync(user, model.Password))
				return null;

			var roles = (await _usermanager.GetRolesAsync(user)).ToList();

			var authClaims = new List<Claim>
			{
				new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
				new Claim(ClaimTypes.NameIdentifier, user.Id),
				new Claim(ClaimTypes.Name, user.UserName)
			};

			foreach (var role in roles)
				authClaims.Add(new Claim(ClaimTypes.Role, role));

			var secretKey = _config["JWT:Secret"] ?? "this_is_a_super_secure_key_12345678";
			var issuer = _config["JWT:ValidIssuer"] ?? "https://nura.apmtechnologies.in";
			var audience = _config["JWT:ValidAudience"] ?? "https://nura.apmtechnologies.in";

			var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

			var token = new JwtSecurityToken(
				issuer: issuer,
				audience: audience,
				expires: DateTime.Now.AddMinutes(60),
				claims: authClaims,
				signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
			);

			return new LoginResponseModel
			{
				Token = new JwtSecurityTokenHandler().WriteToken(token),
				User = user,
				Roles = roles,
				Expiration = token.ValidTo
			};
		}
		// ✅ Step 1: Send OTP
		public async Task<bool> SendOtpAsync(string email)
		{
			var user = await _usermanager.Users.FirstOrDefaultAsync(u => u.Email == email);
			if (user == null) return false;

			var otp = new Random().Next(100000, 999999).ToString();
			_otpStore[email] = (otp, DateTime.UtcNow.AddMinutes(5));

			var body = $@"
                <p>Hi {user.UserName},</p>
                <p>Your password reset OTP is: <strong>{otp}</strong></p>
                <p>This OTP is valid for 5 minutes.</p>";

			await _emailService.SendAsync(email, "Password Reset OTP", body);

			return true;
		}

		// ✅ Step 2: Verify OTP
		public async Task<bool> VerifyOtpAsync(string email, string otp)
		{
			if (_otpStore.TryGetValue(email, out var entry))
			{
				if (entry.Expiry < DateTime.UtcNow)
				{
					_otpStore.TryRemove(email, out _);
					return false;
				}

				if (entry.Otp == otp)
				{
					_otpStore.TryRemove(email, out _);
					return true;
				}
			}
			return false;
		}

		// ✅ Step 3: Reset Password (after OTP verified)
		public async Task<string> ResetPasswordWithOtpAsync(ResetPasswordViewModel model)
		{
			var user = await _usermanager.FindByEmailAsync(model.Email);
			if (user == null)
				return "Invalid email address.";

			var resetToken = await _usermanager.GeneratePasswordResetTokenAsync(user);
			var result = await _usermanager.ResetPasswordAsync(user, resetToken, model.NewPassword);

			if (!result.Succeeded)
			{
				var errors = string.Join(", ", result.Errors.Select(e => e.Description));
				return $"Password reset failed: {errors}";
			}

        //	return new LoginResponse
        //	{
        //		Token = tokenString,
        //		Expiration = token.ValidTo,
        //		Username = user.UserName,
        //		Roles = roles.FirstOrDefault() ?? "User"
        //	};
        //}

        //BlogCategory
        public async Task<BlogCategory> GetBlogCategoryByIdAsync(int id)
        {
            return await _db.BlogCategoryDetails.FindAsync(id);
        }
        public async Task<List<BlogCategory>> GetBlogCategoriesAsync()
        {
            return await _db.BlogCategoryDetails
                            .OrderByDescending(c => c.CreatedAt)
                            .ToListAsync();
        }
        public async Task<IdentityResult> AddOrUpdateBlogCategoryAsync(BlogCategory category)
        {
            if (category == null) return IdentityResult.Failed(new IdentityError { Description = "Category cannot be null" });

            if (category.Id > 0)
            {
                var existing = await _db.BlogCategoryDetails.FindAsync(category.Id);
                if (existing == null)
                    return IdentityResult.Failed(new IdentityError { Description = "Category not found" });

                existing.Name = category.Name;
                existing.IsActive = category.IsActive;
                _db.BlogCategoryDetails.Update(existing);
            }
            else
            {
                category.CreatedAt = DateTime.UtcNow;
                await _db.BlogCategoryDetails.AddAsync(category);
            }

            await _db.SaveChangesAsync();
            return IdentityResult.Success;
        }
        public async Task<IdentityResult> DeleteBlogCategoryAsync(int id)
        {
            var existing = await _db.BlogCategoryDetails.FindAsync(id);
            if (existing == null) return IdentityResult.Failed(new IdentityError { Description = "Category not found" });

            _db.BlogCategoryDetails.Remove(existing);
            await _db.SaveChangesAsync();

            return IdentityResult.Success;
        }

        //Blogs
        public async Task<List<Blog>> GetBlogsAsync()
        {
            return await _db.BlogDetails.OrderByDescending(b => b.CreatedAt).ToListAsync();
        }

        public async Task<Blog> GetBlogByIdAsync(int id)
        {
            return await _db.BlogDetails.FindAsync(id);
        }

        public async Task<IdentityResult> AddOrUpdateBlogAsync(Blog blog)
        {
            if (blog == null)
                return IdentityResult.Failed(new IdentityError { Description = "Blog cannot be null" });

            if (blog.Id > 0)
            {
                var existing = await _db.BlogDetails.FindAsync(blog.Id);
                if (existing == null)
                    return IdentityResult.Failed(new IdentityError { Description = "Blog not found" });

                existing.Title = blog.Title;
                existing.Description = blog.Description;
                existing.ReadTime = blog.ReadTime;
                existing.WrittenBy = blog.WrittenBy;
                existing.BlogCategoryIds = blog.BlogCategoryIds;
                existing.ImagePath = blog.ImagePath;
                existing.IsFeatured = blog.IsFeatured;
                _db.BlogDetails.Update(existing);
            }
            else
            {
                blog.CreatedAt = DateTime.UtcNow;
                await _db.BlogDetails.AddAsync(blog);
            }

            await _db.SaveChangesAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteBlogAsync(int id)
        {
            var existing = await _db.BlogDetails.FindAsync(id);
            if (existing == null)
                return IdentityResult.Failed(new IdentityError { Description = "Blog not found" });

            _db.BlogDetails.Remove(existing);
            await _db.SaveChangesAsync();

            return IdentityResult.Success;
        }

        //Incredients
        public async Task<List<Ingredient>> GetIngredientsAsync()
        {
            return await _db.Ingredients.OrderByDescending(i => i.CreatedAt).ToListAsync();
        }

        public async Task<Ingredient> GetIngredientByIdAsync(int id)
        {
            return await _db.Ingredients.FindAsync(id);
        }
			return "Password has been reset successfully.";
		}

        public async Task<IdentityResult> AddOrUpdateIngredientAsync(Ingredient ingredient)
        {
            if (ingredient == null)
                return IdentityResult.Failed(new IdentityError { Description = "Ingredient cannot be null" });

            if (ingredient.Id > 0)
            {
                var existing = await _db.Ingredients.FindAsync(ingredient.Id);
                if (existing == null)
                    return IdentityResult.Failed(new IdentityError { Description = "Ingredient not found" });

                existing.IngredientName = ingredient.IngredientName;
                existing.ScientificName = ingredient.ScientificName;
                existing.Benefits = ingredient.Benefits;
                existing.Evidence = ingredient.Evidence;
                existing.ImagePath = ingredient.ImagePath;
                existing.IsActive = ingredient.IsActive;

                _db.Ingredients.Update(existing);
            }
            else
            {
                ingredient.CreatedAt = DateTime.UtcNow;
                await _db.Ingredients.AddAsync(ingredient);
            }

            await _db.SaveChangesAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteIngredientAsync(int id)
        {
            var existing = await _db.Ingredients.FindAsync(id);
            if (existing == null)
                return IdentityResult.Failed(new IdentityError { Description = "Ingredient not found" });

            _db.Ingredients.Remove(existing);
            await _db.SaveChangesAsync();

            return IdentityResult.Success;
        }


    }


}
