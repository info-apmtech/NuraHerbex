using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModel
{
    //public class LoginModel
    //{
    //    [Required]
    //    public string Username { get; set; } = null!;
    //    [Required]
    //    [DataType(DataType.Password)]
    //    public string Password { get; set; } = null!;
    //}
	public class LoginResponseModel
	{
		public string Token { get; set; }
		public RegisterUser User { get; set; }
		public List<string> Roles { get; set; } = new List<string>();
		public DateTime Expiration { get; set; }
		public int AccountId { get; set; }
		public string BasketId { get; set; }
		public string BaseUrl { get; set; }
	}
	public class RegisterUserViewModel
	{
		[Required]
		public string Username { get; set; } = null!;
		[Required]
		[DataType(DataType.Password)]
		public string Password { get; set; } = null!;
		public bool IsActive { get; set; } = true;
		public UserRole role { get; set; }
		public RegisterUser? RegisteredUser { get; set; }
		public List<RegisterUser>? UserList { get; set; }
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
	}
	public enum ForgotFlowStep { Request = 0, Verify = 1 }

	public class ForgotPasswordViewModel : IValidatableObject
	{
		[Required, EmailAddress]
		public string Email { get; set; } = string.Empty;

		// Only required when Step == Verify
		[StringLength(6, MinimumLength = 6)]
		public string? Otp { get; set; }

		public ForgotFlowStep Step { get; set; } = ForgotFlowStep.Request;

		public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
		{
			if (Step == ForgotFlowStep.Verify && string.IsNullOrWhiteSpace(Otp))
			{
				yield return new ValidationResult("OTP is required.", new[] { nameof(Otp) });
			}
		}
	}

	public class ResetPasswordViewModel
	{
		[Required, EmailAddress]
		public string Email { get; set; } = string.Empty;

		// Keep OTP on the Create Password page too (server will re-verify atomically)
		[Required, StringLength(6, MinimumLength = 6)]
		public string Otp { get; set; } = string.Empty;

		[Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
		public string NewPassword { get; set; } = string.Empty;

		[Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
		public string ConfirmPassword { get; set; } = string.Empty;
	}


    //public class VerifyOtpViewModel
    //{
    //	[Required, EmailAddress]
    //	public string Email { get; set; }

    //	[Required, StringLength(6, MinimumLength = 6)]
    //	public string Otp { get; set; }
    //}

    //public class ResetPasswordViewModel
    //{
    //	//[Required, EmailAddress]
    //	public string Email { get; set; }
    //	public string Otp { get; set; } = string.Empty; // Used to validate before reset

    //	//[Required]
    //	[StringLength(100, MinimumLength = 6)]
    //	public string NewPassword { get; set; }

    //	//[Required]
    //	[Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    //	public string ConfirmPassword { get; set; }
    //}
    //public class ForgotPasswordViewModel
    //{
    //	// Step 1: Request OTP
    //	[Required(ErrorMessage = "Email is required.")]
    //	[EmailAddress(ErrorMessage = "Invalid email address.")]
    //	public string Email { get; set; } = string.Empty;

    //	// Step 2: Verify OTP
    //	[StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits.")]
    //	public string Otp { get; set; } = string.Empty;

    //	// Step 3: Reset Password
    //	[StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    //	public string NewPassword { get; set; } = string.Empty;

    //	[Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    //	public string ConfirmPassword { get; set; } = string.Empty;

    //}
    public class HomeViewModel
    {
        public List<Blog> BlogList { get; set; } = new List<Blog>();
        public List<Ingredient> Ingredients { get; set; } = new();

    }
    public class BlogCategoryViewModel
    {
        public List<BlogCategory> CategoryList { get; set; } = new();
        public BlogCategory NewCategory { get; set; } = new();
    }
    public class BlogViewModel
    {
        public Blog NewBlog { get; set; } = new Blog();
        public List<Blog> BlogList { get; set; } = new List<Blog>();
        public List<BlogCategory> Categories { get; set; } = new List<BlogCategory>();
        public IFormFile ImageFile { get; set; }
        public List<int> SelectedCategoryIds { get; set; } = new List<int>();
        public int CategoryId { get; set; }
    }
    public class IngredientViewModel
    {
        public List<IngredientCategory> IngredientCategories { get; set; } = new List<IngredientCategory>();
        public List<Ingredient> IngredientList { get; set; } = new List<Ingredient>();
        public Ingredient NewIngredient { get; set; } = new Ingredient();
        public IFormFile ImageFile { get; set; }
    }
    public class IngredientCategoryViewModel
    {
        public List<IngredientCategory> CategoryList { get; set; } = new List<IngredientCategory>();
        public IngredientCategory NewCategory { get; set; } = new IngredientCategory();
    }
	public class ProductViewModel
	{
        public List<Product> ProductList { get; set; } = new List<Product>();
        public Product NewProduct { get; set; } = new Product();
        public List<IFormFile> ProductFiles { get; set; } = new();
        public List<GST> GSTDetails { get; set; } = new List<GST>();
    }
    public class GSTViewModel
    {
        public List<GST> GSTList { get; set; } = new();
        public GST NewGST { get; set; } = new();
    }
    public class EmailSettings
    {
        public string FromAddress { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool UseSSL { get; set; }
    }
}
