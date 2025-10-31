using Domain.Implementation;
using Domain.Interface;
using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
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
		//private readonly IEmailService _emailService;
		public AuthenticationAPIController(UserManager<RegisterUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration,NuraDbContext dbContext,IAdmin adminservice)
		{
			_userManager = userManager;
			_adminService = adminservice;
			_roleManager = roleManager;
			_configuration = configuration;
			_dbContext = dbContext;
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
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var user = await _userManager.FindByNameAsync(model.Username);
			if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
				return Unauthorized("Invalid email or password.");
			var roles = (await _userManager.GetRolesAsync(user)).ToList();
			//var findDesignationId = _dbContext.MappingEmployeeDesignations.FirstOrDefaultAsync(e => e.EmployeeId == user.Id)?.Result?.DesignationId;

			//var designation = await _dbContext.Designations
				//.FirstOrDefaultAsync(d => d.Id == findDesignationId);

			//if (designation != null && !string.IsNullOrEmpty(designation.RoleAccess))
			//{
			//	var designationRoles = designation.RoleAccess
			//		.Split(",", StringSplitOptions.RemoveEmptyEntries)
			//		.Select(r => r.Trim());

			//	foreach (var dr in designationRoles)
			//	{
			//		if (!roles.Contains(dr))
			//			roles.Add(dr);
			//	}
			//}

			// Build JWT claims
			var authClaims = new List<Claim>
	        {
		        new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
		        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
		        new Claim(ClaimTypes.NameIdentifier, user.Id),
		        new Claim(ClaimTypes.Name, user.UserName)
	        };

			foreach (var role in roles)
			{
				authClaims.Add(new Claim(ClaimTypes.Role, role));
			}
			var jwtSecret = "this_is_a_super_secure_key_12345678";
			var jwtIssuer = "https://nura.apmtechnologies.in";
			var jwtAudience = "https://nura.apmtechnologies.in";
			var expiryMinutes = 60;

			var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

			var token = new JwtSecurityToken(
				issuer: jwtIssuer,
				audience: jwtAudience,
				expires: DateTime.Now.AddMinutes(expiryMinutes),
				claims: authClaims,
				signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
			);

			var responseData = new LoginResponseModel
			{
				BaseUrl = _configuration["ApiBaseUrl"],
				Token = new JwtSecurityTokenHandler().WriteToken(token),
				User = user,
				Roles = roles,
				Expiration = token.ValidTo
			};

			return Ok(responseData);
		}
		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[HttpPost("logout")]
		public IActionResult Logout()
		{
			return Ok(new
			{
				Success = true,
				Message = "Logged out successfully. Please remove token on client side."
			});
		}
	}
}
