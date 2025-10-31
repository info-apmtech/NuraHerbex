using Domain.Extensions;
using Domain.Interface;
using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace NuraHerbex.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult UserCreation()
        {
            return View();
        }
        public IActionResult AdminBlog()
        {
            return View();
        }
        public IActionResult AdminBlogCategory()
        {
            return View();
        } 
        public IActionResult AdminIncredient()
        {
            return View();
        }
        public IActionResult Product()
        {
            return View();
        }
        public IActionResult DoctorConsultation()
        {
            return View();
        }

    }
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly ITokenService _tokenService;


		public AdminController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ITokenService tokenService)
		{
			_httpClientFactory = httpClientFactory;
			_httpContextAccessor = httpContextAccessor;
			_tokenService = tokenService;
		}
		private HttpClient AuthorizedClient => _httpClientFactory.CreateAuthorizedClient(_httpContextAccessor);
		private string GetUserId() => _httpContextAccessor.GetUserId(_tokenService);


		[HttpGet]
		public async Task<IActionResult> UserRegistration(RegisterUserViewModel model, string? id = null)
		{
			if (model == null)
				model = new RegisterUserViewModel();

			model.RegisteredUser = new RegisterUser
			{
				Role = model.role
			};

			// Call API to get user list
			var response = await AuthorizedClient.GetAsync($"AdminAPI/users/{model.role}");
			model.UserList = response.IsSuccessStatusCode ? JsonConvert.DeserializeObject<List<RegisterUser>>(await response.Content.ReadAsStringAsync()) ?? new() : new List<RegisterUser>();
			// If ID provided, load selected user
			if (!string.IsNullOrEmpty(id))
			{
				model.RegisteredUser = model.UserList.FirstOrDefault(u => u.Id == id);
				if (model.RegisteredUser != null)
					model.role = model.RegisteredUser.Role;
			}
			// Example dropdown loading
			await LoadDropdownsAsync(model, model.role);
			// Date filtering
			if (model.FromDate != null && model.ToDate != null)
				model.UserList = model.UserList.Where(u => u.CreatedAt.Date >= model.FromDate.Value.Date && u.CreatedAt.Date <= model.ToDate.Value.Date).ToList();
			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> UserRegistration(RegisterUserViewModel model)
		{
			if (!ModelState.IsValid)
				return View(model);

			var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/register", model.RegisteredUser);

			if (response.IsSuccessStatusCode)
			{
				TempData["Success"] = "User Registered Successfully!";
				//return RedirectToAction("Authentication","SignIn", new { role = model.RegisteredUser.Role });
				return RedirectToAction("SignIn","Authentication");
			}

			var errorMsg = await response.Content.ReadAsStringAsync();
			ModelState.AddModelError("", $"Error: {errorMsg}");
			return View(model);
		}

		private async Task LoadDropdownsAsync(RegisterUserViewModel model, UserRole role)
		{
			// Example: load countries/states/specialties
			await Task.CompletedTask;
		}
	}
}
