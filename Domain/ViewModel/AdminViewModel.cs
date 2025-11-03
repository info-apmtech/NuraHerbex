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
	public class ForgotPasswordViewModel
	{
		//[Required(ErrorMessage = "Email is required.")]
		[EmailAddress(ErrorMessage = "Invalid email address.")]
		public string? Email { get; set; } = string.Empty;

		[StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits.")]
		public string? Otp { get; set; } = string.Empty;
	}

	//public class VerifyOtpViewModel
	//{
	//	[Required, EmailAddress]
	//	public string Email { get; set; }

	//	[Required, StringLength(6, MinimumLength = 6)]
	//	public string Otp { get; set; }
	//}

	public class ResetPasswordViewModel
	{
		//[Required, EmailAddress]
		public string Email { get; set; }
		public string Otp { get; set; } = string.Empty; // Used to validate before reset

		//[Required]
		[StringLength(100, MinimumLength = 6)]
		public string NewPassword { get; set; }

		//[Required]
		[Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
		public string ConfirmPassword { get; set; }
	}
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
    }
    public class IngredientViewModel
    {
        public List<Ingredient> IngredientList { get; set; } = new List<Ingredient>();
        public Ingredient NewIngredient { get; set; } = new Ingredient();
        public IFormFile ImageFile { get; set; }
    }

}
