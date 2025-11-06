using Domain.Extensions;
using Domain.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
//using System.Text;
namespace NuraHerbex.Controllers
{
    public class AuthenticationController : Controller
    {
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMemoryCache _cache;
		private readonly IHostEnvironment _env;
		public AuthenticationController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IMemoryCache cache, IHostEnvironment env)
		{
			_httpClientFactory = httpClientFactory;
			_httpContextAccessor = httpContextAccessor;
			_cache = cache;
			_env = env;
		}
		private const string PurposeSignup = "signup";
		private static string CacheKey(string phone, string purpose) => $"otp:{purpose}:{phone}";

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
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SignIn(RegisterUserViewModel model)
		{
			if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Username) ||string.IsNullOrWhiteSpace(model.Password))
			{
				ModelState.AddModelError(string.Empty, "Username and Password are required.");
				return View(model); 
			}
			var client = _httpClientFactory.CreateClient("NuraHerbexApi");
			//var logininfo = new
			//{
			//	username = model.Username,
			//	password = model.Password
			//};

			var response = await client.PostAsJsonAsync("AuthenticationAPI/SignIn", model);

			//if (!response.IsSuccessStatusCode)
			//{
			//	return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
			//	ModelState.AddModelError("", "Invalid username or password.");
			//	return View(model);
			//}
			if (!response.IsSuccessStatusCode)
			{
				var msg = await response.Content.ReadAsStringAsync();
			}

			var result = await response.Content.ReadFromJsonAsync<LoginResponseModel>();
			if (result == null || string.IsNullOrEmpty(result.Token))
			{
				ModelState.AddModelError("", "Login failed. Token missing.");
				return View(model);
			}

			// ✅ Step 5: Store token if needed
			HttpContext.Session.SetString("JwtToken", result.Token);

			// ✅ Step 6: Build user claims
			var roles = (result.Roles ?? Enumerable.Empty<string>())
				.Where(r => !string.IsNullOrWhiteSpace(r))
				.Select(r => r.Trim());

			var claims = new List<Claim>
	{
		new Claim(ClaimTypes.NameIdentifier, result.User?.Id ?? string.Empty),
		new Claim(ClaimTypes.Name, result.User?.UserName ?? string.Empty)
	};

			foreach (var role in roles)
				claims.Add(new Claim(ClaimTypes.Role, role));

			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var principal = new ClaimsPrincipal(identity);

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
			{
				IsPersistent = model.IsActive
			});

			HttpContext.User = principal;
			if (User.IsInRole("Admin") || User.IsInRole("Employee") || User.IsInRole("Doctor"))
				return RedirectToAction("UserCreation", "Admin");
			else
				return RedirectToAction("MyOrders", "Home");
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
				return RedirectToAction("CreatePassword", new { email = model.Email,otp=model.Otp });

			ModelState.AddModelError("", "Invalid OTP. Please try again.");
			return View(model);
		}

		[HttpGet]
		public IActionResult CreatePassword(string email,string otp)
		{
			return View(new ResetPasswordViewModel { Email = email,Otp=otp });
		}

		[HttpPost]
		//[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreatePassword(ResetPasswordViewModel model)
		{
			var json = JsonConvert.SerializeObject(model);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var response = await AuthorizedClient.PostAsync("AuthenticationAPI/ResetPasswordWithOtp", content);
			var msg = await response.Content.ReadAsStringAsync();

			// If you want the JS to see a redirect on success:
			if (response.IsSuccessStatusCode)
				return RedirectToAction("SignIn", "Authentication");

			// Otherwise return the same view with a message
			ViewBag.Message = msg;
			return View(model);
		}
		//[HttpPost]
		//[ValidateAntiForgeryToken]
		//public async Task<IActionResult> SendSignupOtp(string phone)
		//{
		//	if (string.IsNullOrWhiteSpace(phone))
		//		return BadRequest(new { success = false, message = "Phone number is required." });

		//	var client = _httpClientFactory.CreateClient("NuraHerbexApi");

		//	var content = new FormUrlEncodedContent(new Dictionary<string, string>
		//	{
		//		["phonenumber"] = phone
		//	});

		//	var response = await client.PostAsync("VerifyPhoneOTP", content);

		//	var json = await response.Content.ReadAsStringAsync();
		//	if (!response.IsSuccessStatusCode)
		//		return StatusCode((int)response.StatusCode, new { success = false, message = json });

		//	return Content(json, "application/json");
		//}

		private static string GenerateNumericOtp(int length = 6)
		{
			var sb = new StringBuilder(length);
			for (int i = 0; i < length; i++)
				sb.Append(RandomNumberGenerator.GetInt32(0, 10));
			return sb.ToString();
		}

		private static string HashOtp(string otp, string phone, string purpose)
		{
			// simple HMAC for demo; keep the key in config in real apps
			var key = Encoding.UTF8.GetBytes("DEMO_ONLY_CHANGE_ME");
			using var hmac = new HMACSHA256(key);
			var data = Encoding.UTF8.GetBytes($"{purpose}|{phone}|{otp}");
			return Convert.ToHexString(hmac.ComputeHash(data));
		}

		private sealed class OtpEntry
		{
			public string Hash { get; init; } = default!;
			public DateTime ExpiresUtc { get; init; }
			public int Attempts { get; set; }
			public int MaxAttempts { get; init; } = 5;
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult SendSignupOtp(string phone)
		{
			if (string.IsNullOrWhiteSpace(phone))
				return BadRequest(new { success = false, message = "Phone number is required." });

			// Normalize if you want: phone = NormalizeToE164(phone);

			var otp = GenerateNumericOtp(6);
			var hash = HashOtp(otp, phone, PurposeSignup);

			var entry = new OtpEntry
			{
				Hash = hash,
				ExpiresUtc = DateTime.UtcNow.AddMinutes(15),
				Attempts = 0
			};

			_cache.Set(CacheKey(phone, PurposeSignup), entry,
				new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });

			// In real app: send SMS here. For testing: reveal otp only in Development.
			if (_env.IsDevelopment())
				return Ok(new { success = true, debugOtp = otp, message = "OTP generated (DEV only)" });

			return Ok(new { success = true, message = "OTP sent" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult VerifyPhoneOtp([FromForm] string phone, [FromForm] string otp)
		{
			if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(otp))
				return BadRequest(new { success = false, message = "Phone and OTP are required." });

			var key = CacheKey(phone, PurposeSignup);
			if (!_cache.TryGetValue<OtpEntry>(key, out var entry))
				return BadRequest(new { success = false, message = "OTP not found. Please resend." });

			if (DateTime.UtcNow > entry.ExpiresUtc)
			{
				_cache.Remove(key);
				return BadRequest(new { success = false, message = "OTP expired. Please resend." });
			}

			if (entry.Attempts >= entry.MaxAttempts)
				return BadRequest(new { success = false, message = "Too many attempts. Please resend OTP." });

			entry.Attempts++;

			var isMatch = string.Equals(entry.Hash, HashOtp(otp, phone, PurposeSignup), StringComparison.Ordinal);
			if (!isMatch)
			{
				// write back attempts
				_cache.Set(key, entry, new MemoryCacheEntryOptions { AbsoluteExpiration = entry.ExpiresUtc });
				return BadRequest(new { success = false, message = "Invalid OTP." });
			}

			// success → clear OTP and mark session as verified if you want
			_cache.Remove(key);
			HttpContext.Session.SetString("SignupVerifiedPhone", phone);

			return Ok(new { success = true });
		}

	}
}
