using Domain.Extensions;
using Domain.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

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
		public async Task<IActionResult> SignIn(LoginModel model, string? returnUrl = null)
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

			if (string.IsNullOrEmpty(result?.Token))
			{
				ModelState.AddModelError("", "Login failed. No token received.");
				return View(model);
			}
			Console.WriteLine(result);
			// Save token & user info in session
			HttpContext.Session.SetString("JwtToken", result.Token);
			HttpContext.Session.SetString("TokenExpiration", result.Expiration.ToString("o"));
			HttpContext.Session.SetString("UserId", result.User.Id);
			HttpContext.Session.SetString("UserName", result.User.UserName ?? "");
			if (result.Roles != null && result.Roles.Any())
			{
				HttpContext.Session.SetString("UserRoles", string.Join(",", result.Roles));
				TempData["UserRoles"] = string.Join(",", result.Roles);
			}
			var claims = new List<Claim>
	        {
		        new Claim(ClaimTypes.NameIdentifier, result.User.Id),
		        new Claim(ClaimTypes.Name, result.User.UserName ?? ""),
	        };
			if (result.Roles != null)
			{
				foreach (var role in result.Roles)
				{
					claims.Add(new Claim(ClaimTypes.Role, role));
				}
			}
			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			// Sign in user with cookie authentication
			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(claimsIdentity), new AuthenticationProperties { IsPersistent = true });
			if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
				return Redirect(returnUrl);
			return RedirectToAction("Index", "Home");
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
		public IActionResult ForgetPassword()
        {
            return View();
        }
        public IActionResult CreatePassword()
        {
            return View();
        }
    }
}
