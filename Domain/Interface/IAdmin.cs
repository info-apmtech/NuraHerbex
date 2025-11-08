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
        Task<List<RegisterUser>> GetAllUsersAsync();
        Task<List<RegisterUser>> GetUsersByRoleAsync(UserRole role);
		Task<RegisterUser?> GetUserByIdAsync(string id);
		Task<IdentityResult> AddOrUpdateUserAsync(RegisterUser user);
        //Task<IdentityResult> DeleteUserAsync(string userId);
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
        //Products
        Task<List<Product>> GetProductsAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task<IdentityResult> AddOrUpdateProductAsync(Product product, string? actingUser = null);
        Task<IdentityResult> DeleteProductAsync(int id);

        //Gst
        Task<List<GST>> GetGSTEntriesAsync();
        Task<GST> GetGSTEntryByIdAsync(int id);
        Task<IdentityResult> AddOrUpdateGSTEntryAsync(GST gst);
        Task<IdentityResult> DeleteGSTEntryAsync(int id);

        //Address
        Task<List<AddressDetail>> GetAddressesByUserAsync(string userId);
        Task<AddressDetail?> GetAddressByIdAsync(int id);
        Task<IdentityResult> AddOrUpdateAddressAsync(AddressDetail address);
        Task<IdentityResult> DeleteAddressAsync(int id);

        //State Country
        Task<List<Country>> GetCountriesAsync();
        Task<List<State>> GetStatesByCountryAsync(int countryId);
        Task<List<State>> GetAllStatesAsync();

        //Plans
        Task<List<PricingPlan>> GetPricingPlansAsync();
        Task<PricingPlan> GetPricingPlanByIdAsync(int id);
        Task<IdentityResult> AddOrUpdatePricingPlanAsync(PricingPlan plan);
        Task<IdentityResult> DeletePricingPlanAsync(int id);
        //subscription
        Task<NewsletterSubscriptionResult> SaveNewsletterSubscriptionAsync(string rawEmail);
        Task<List<NewsletterSubscription>> GetAllSubscription();
    }
}
