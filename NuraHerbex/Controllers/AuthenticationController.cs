using Domain.Extensions;
using Domain.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text;
namespace NuraHerbex.Controllers
{
    public class AuthenticationController : Controller
    {
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IHttpContextAccessor _httpContextAccessor;
		public AuthenticationController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
		{
			_httpClientFactory = httpClientFactory;
			_httpContextAccessor = httpContextAccessor;
		}
		private HttpClient AuthorizedClient => _httpClientFactory.CreateAuthorizedClient(_httpContextAccessor);
		public IActionResult Index()
        {
            return View();
        }
		[HttpGet]
		public IActionResult SignIn()
        {
            return View();
        }
		[HttpPost]
		public async Task<IActionResult> SignIn(RegisterUserViewModel model, string? returnUrl = null)
		{
			if (!ModelState.IsValid)
				return View(model);

			var client = _httpClientFactory.CreateClient("NuraHerbexApi");
			var response = await client.PostAsJsonAsync("AuthenticationAPI/SignIn", model);

			if (!response.IsSuccessStatusCode)
			{
				ModelState.AddModelError("", "Invalid username or password.");
				return View(model);
			}

			var result = await response.Content.ReadFromJsonAsync<LoginResponseModel>();
			if (result == null || string.IsNullOrEmpty(result.Token))
			{
				ModelState.AddModelError("", "Login failed. Token missing.");
				return View(model);
			}

			// Save token
			HttpContext.Session.SetString("JwtToken", result.Token);

			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, result.User.Id),
				new Claim(ClaimTypes.Name, result.User.UserName ?? "")
			};
			foreach (var role in result.Roles)
				claims.Add(new Claim(ClaimTypes.Role, role));

			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
			if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
				return Redirect(returnUrl);

			return RedirectToAction("Index", "Admin");
		}
		public async Task<IActionResult> LogOut()
		{
			var client = _httpClientFactory.CreateAuthorizedClient(_httpContextAccessor);
			var response = await client.PostAsync("AuthenticationAPI/logout", null);
			if (response.IsSuccessStatusCode)
			{
				// Clear server-side session (optional)
				HttpContext.Session.Clear();

				// Redirect to login page
				return RedirectToAction("SignIn", "Authentication");
			}

			// Handle API failure
			return RedirectToAction("Index", "Home");
		}
		[HttpGet]
		public IActionResult ForgotPassword()
        {
            return View();
        }
		[HttpPost]
		public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
		{
			//if (!ModelState.IsValid)
			if (model == null)
				return View(model);
			var client = _httpClientFactory.CreateClient("NuraHerbexApi");
			var response = await AuthorizedClient.PostAsJsonAsync("AuthenticationAPI/SendOtp", model);
			if (response.IsSuccessStatusCode)
				return RedirectToAction("ForgotPassword", new { email = model.Email });

			ModelState.AddModelError("", "Failed to send OTP. Try again.");
			return View(model);
		}

		[HttpGet]
		public IActionResult VerifyOtp(string email) => View(new ForgotPasswordViewModel { Email = email });

		[HttpPost]
		public async Task<IActionResult> VerifyOtp(ForgotPasswordViewModel model)
		{
			var response = await AuthorizedClient.PostAsJsonAsync("AuthenticationAPI/VerifyOtp", model);
			if (response.IsSuccessStatusCode)
				return RedirectToAction("CreatePassword", new { email = model.Email });

			ModelState.AddModelError("", "Invalid OTP. Please try again.");
			return View(model);
		}

		[HttpGet]
		public IActionResult CreatePassword(string email)
		{
			return View(new ResetPasswordViewModel { Email = email });
		}

		[HttpPost("ResetPassword")]
		public async Task<IActionResult> CreatePassword(ResetPasswordViewModel model)
		{
			var json = JsonConvert.SerializeObject(model);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var response = await AuthorizedClient.PostAsync("AuthenticationAPI/ResetPasswordWithOtp", content);
			ViewBag.Message = await response.Content.ReadAsStringAsync();

			return View();
		}

	}
}
