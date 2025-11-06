using Domain.Implementation;
using Domain.Interface;
using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace APIs.Controllers
{

	[Route("api/[controller]")]
	[ApiController]
	public class AuthenticationAPIController : ControllerBase
	{
		private readonly UserManager<RegisterUser> _userManager;
		private readonly IAdmin _adminService;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly IConfiguration _configuration;
		private readonly NuraDbContext _dbContext;
		//private readonly INotificationClientService _notificationService;
		//private readonly IEmailService _emailService;
		public AuthenticationAPIController(UserManager<RegisterUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration,NuraDbContext dbContext,IAdmin adminservice)//, INotificationClientService notificationClientService)
		{
			_userManager = userManager;
			_adminService = adminservice;
			_roleManager = roleManager;
			_configuration = configuration;
			_dbContext = dbContext;
			//_notificationService = notificationClientService;
			//_emailService = emailService;
		}
		[HttpPost("DefaultUser")]
		public async Task<IActionResult> CreateDefaultUser()
		{
			foreach (var roleName in Enum.GetNames(typeof(UserRole)))
			{
				if (!await _roleManager.RoleExistsAsync(roleName))
					await _roleManager.CreateAsync(new IdentityRole(roleName));
			}

			var existingUser = await _userManager.FindByNameAsync("Admin");
			if (existingUser != null)
				return BadRequest("Default admin user already exists.");
			var defaultUser = new RegisterUser
			{
				UserName = "Admin",
				Email = "admin@NuraHerbex.com",
				FirstName = "Nura Herbex",
				Role = UserRole.Admin,
				CreatedAt = DateTime.UtcNow,
				CreatedBy = "Admin",
				Password = "Admin@123",
				EmailConfirmed = true,
			};

			var result = await _userManager.CreateAsync(defaultUser, "Admin@123");
			if (!result.Succeeded)
				return BadRequest(result.Errors);
			await _userManager.AddToRoleAsync(defaultUser, defaultUser.Role.ToString());
			return Ok("Default admin user created successfully.");
		}

		[HttpPost("SignIn")]
		public async Task<IActionResult> SignIn([FromBody] RegisterUserViewModel model)
		{
			var result = await _adminService.SignInAsync(model);
			if (result == null)
				return Unauthorized("Invalid username or password.");

			return Ok(result);
		}
		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[HttpPost("Logout")]
		public IActionResult Logout()
		{
			return Ok(new
			{
				Success = true,
				Message = "Logged out successfully. Please remove token on client side."
			});
		}
		[HttpPost("SendOtp")]
		public async Task<IActionResult> SendOtp([FromBody] ForgotPasswordViewModel model)
		{
			var success = await _adminService.SendOtpAsync(model.Email);
			if (!success)
				return BadRequest("Email not found.");
			return Ok(new { Message = "OTP sent to your email." });
		}

		[HttpPost("VerifyOtp")]
		public async Task<IActionResult> VerifyOtp([FromBody] ForgotPasswordViewModel model)
		{
			var verified = await _adminService.VerifyOtpAsync(model.Email, model.Otp);
			if (!verified)
				return BadRequest("Invalid or expired OTP.");
			return Ok(new { Message = "OTP verified successfully." });
		}

		[HttpPost("ResetPasswordWithOtp")]
		public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordViewModel model)
		{
			var result = await _adminService.ResetPasswordWithOtpAsync(model);
			if (result.StartsWith("Password reset failed") || result.Contains("Invalid"))
				return BadRequest(result);

			return Ok(result);
		}
		//[HttpPost("VerifyPhoneOTP")]
		//[AllowAnonymous]
		//public async Task<IActionResult> VerifyPhoneOTP([FromForm] string phone)
		//{
		//	if (string.IsNullOrWhiteSpace(phone))
		//		return BadRequest(new { success = false, message = "invalid phone number" });

		//	const string ApiKey = "apm_forgetkey_nuraherbex";
		//	var otp = await _notificationService.SendOtpSms(ApiKey, phone, SMSTemplateType.Registration);

		//	if (string.IsNullOrWhiteSpace(otp)) // or use C#
		//		return StatusCode(502, new { success = false, message = "failed to send otp" });

		//	// For production, remove `otp` from response
		//	return Ok(new
		//	{
		//		success = true,
		//		mobileNumber = phone,
		//		otp,
		//		createdDate = DateTime.UtcNow,
		//		isActive = true
		//	});
		//}

	}
}
