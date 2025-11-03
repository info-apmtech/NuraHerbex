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
		Task<LoginResponseModel?> SignInAsync(RegisterUserViewModel model);
		Task<bool> SendOtpAsync(string email);
		Task<bool> VerifyOtpAsync(string email, string otp);
		Task<string> ResetPasswordWithOtpAsync(ResetPasswordViewModel model);
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

        //Ingredients
        Task<List<Ingredient>> GetIngredientsAsync();
        Task<Ingredient> GetIngredientByIdAsync(int id);
        Task<IdentityResult> AddOrUpdateIngredientAsync(Ingredient ingredient);
        Task<IdentityResult> DeleteIngredientAsync(int id);

        //Ingredient Category
        Task<List<IngredientCategory>> GetIngredientCategoriesAsync();
        Task<IngredientCategory> GetIngredientCategoryByIdAsync(int id);
        Task<IdentityResult> AddOrUpdateIngredientCategoryAsync(IngredientCategory category);
        Task<IdentityResult> DeleteIngredientCategoryAsync(int id);
    }
}
