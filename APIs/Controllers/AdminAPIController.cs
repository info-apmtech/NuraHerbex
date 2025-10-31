using Domain.Interface;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIs.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class AdminAPIController : ControllerBase
	{
		private readonly IAdmin _adminservice;

		public AdminAPIController(IAdmin adminservice)
		{
			_adminservice = adminservice;
		}

		[HttpGet("users/{role}")]
		public async Task<IActionResult> GetUsers(UserRole role)
		{
			var users = await _adminservice.GetUsersByRoleAsync(role);
			return Ok(users);
		}

		[HttpGet("user/{id}")]
		public async Task<IActionResult> GetUser(string id)
		{
			var user = await _adminservice.GetUserByIdAsync(id);
			if (user == null)
				return NotFound();

			return Ok(user);
		}
		[AllowAnonymous]
		[HttpPost("register")]
		public async Task<IActionResult> AddOrUpdateUser([FromBody] RegisterUser user)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _adminservice.AddOrUpdateUserAsync(user);

			if (result.Succeeded)
				return Ok(new { success = true, message = "User saved successfully" });

			return BadRequest(result.Errors);
		}
	}
}
