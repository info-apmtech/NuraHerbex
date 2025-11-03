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
	}
}
