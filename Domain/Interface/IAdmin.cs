using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interface
{
	public interface IAdmin
	{
		Task<List<RegisterUser>> GetUsersByRoleAsync(UserRole role);
		Task<RegisterUser?> GetUserByIdAsync(string id);
		Task<IdentityResult> AddOrUpdateUserAsync(RegisterUser user);
        //Task<LoginResponse?> SignInAsync(LoginModel model);

        //BlogCategory
        Task<BlogCategory> GetBlogCategoryByIdAsync(int id);
        Task<List<BlogCategory>> GetBlogCategoriesAsync();
        Task<IdentityResult> AddOrUpdateBlogCategoryAsync(BlogCategory category);
        Task<IdentityResult> DeleteBlogCategoryAsync(int id);

        //Blog
        Task<List<Blog>> GetBlogsAsync();
        Task<Blog> GetBlogByIdAsync(int id);
        Task<IdentityResult> AddOrUpdateBlogAsync(Blog blog);
        Task<IdentityResult> DeleteBlogAsync(int id);

    }
}
