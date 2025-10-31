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
        private readonly NuraDbContext _db;
        public AdminService(UserManager<RegisterUser> userManager,IConfiguration config, NuraDbContext db)
		{
			//_adminService = adminservice;
			_usermanager = userManager;
			_config = config;
			_db = db;
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
        //public async Task<LoginResponse?> SignInAsync(LoginModel model)
        //{
        //	var user = await _usermanager.FindByNameAsync(model.Username);
        //	if (user == null)
        //		return null;

        //	var validPassword = await _usermanager.CheckPasswordAsync(user, model.Password);
        //	if (!validPassword)
        //		return null;

        //	var roles = await _usermanager.GetRolesAsync(user);

        //	// Create claims
        //	var claims = new List<Claim>
        //{
        //	new Claim(ClaimTypes.NameIdentifier, user.Id),
        //	new Claim(ClaimTypes.Name, user.UserName)
        //};

        //	foreach (var role in roles)
        //		claims.Add(new Claim(ClaimTypes.Role, role));

        //	// Generate JWT
        //	var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        //	var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //	var token = new JwtSecurityToken(
        //		issuer: _config["Jwt:Issuer"],
        //		audience: _config["Jwt:Audience"],
        //		claims: claims,
        //		expires: DateTime.Now.AddHours(1),
        //		signingCredentials: creds
        //	);

        //	var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

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


    }


}
