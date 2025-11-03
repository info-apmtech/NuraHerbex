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
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _environment;  

        //private readonly ITokenService _tokenService;
        public AdminController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment/*, ITokenService tokenService*/)
		{
			_httpClientFactory = httpClientFactory;
			_httpContextAccessor = httpContextAccessor;
            _environment = environment;
            //_tokenService = tokenService;
        }
		private HttpClient AuthorizedClient => _httpClientFactory.CreateAuthorizedClient(_httpContextAccessor);
		//private string GetUserId() => _httpContextAccessor.GetUserId(_tokenService);
		public IActionResult Index()
		{
			return View();
		}
		public IActionResult UserCreation()
		{
			return View();
		}


        //Blog
        [HttpGet]
        public async Task<IActionResult> AdminBlog(int id = 0)
        {
            var categoryResp = await AuthorizedClient.GetAsync("AdminAPI/blogcategories");
            var categories = categoryResp.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<BlogCategory>>(await categoryResp.Content.ReadAsStringAsync()) ?? new List<BlogCategory>()
                : new List<BlogCategory>();

            var blogResp = await AuthorizedClient.GetAsync("AdminAPI/blogs");
            var blogs = blogResp.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<Blog>>(await blogResp.Content.ReadAsStringAsync()) ?? new List<Blog>()
                : new List<Blog>();

            var vm = new BlogViewModel
            {
                Categories = categories,
                BlogList = blogs,
                NewBlog = new Blog()
            };

            if (id > 0)
            {
                var singleBlogResp = await AuthorizedClient.GetAsync($"AdminAPI/blog/{id}");
                if (singleBlogResp.IsSuccessStatusCode)
                {
                    var blog = JsonConvert.DeserializeObject<Blog>(await singleBlogResp.Content.ReadAsStringAsync());
                    if (blog != null)
                    {
                        vm.NewBlog = blog;
                        vm.SelectedCategoryIds = vm.NewBlog.BlogCategoryIds?.Split(',').Select(int.Parse).ToList() ?? new List<int>();
                    }
                }
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminBlog(BlogViewModel model)
        {
            // Handle image upload
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/blogimages");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    model.NewBlog.ImagePath = "/uploads/blogimages/" + uniqueFileName;
                }
                catch (IOException ex)
                {
                    ModelState.AddModelError("NewBlog.ImagePath", "Error saving image file: " + ex.Message);
                }
            }

            // Category selection
            if (model.SelectedCategoryIds != null && model.SelectedCategoryIds.Any())
                model.NewBlog.BlogCategoryIds = string.Join(",", model.SelectedCategoryIds);
            else
                model.NewBlog.BlogCategoryIds = null;

            ModelState.Remove("ImageFile");
            ModelState.Remove("NewBlog.BlogCategoryIds");
            ModelState.Remove("NewBlog.ImagePath");

            // Validation
            if (string.IsNullOrWhiteSpace(model.NewBlog.ImagePath))
                ModelState.AddModelError("NewBlog.ImagePath", "Image is required.");

            if (string.IsNullOrWhiteSpace(model.NewBlog.BlogCategoryIds))
                ModelState.AddModelError("NewBlog.BlogCategoryIds", "At least one category must be selected.");

            if (!ModelState.IsValid)
            {
                var categoryResponse = await AuthorizedClient.GetAsync("AdminAPI/blogcategories");
                model.Categories = categoryResponse.IsSuccessStatusCode
                    ? JsonConvert.DeserializeObject<List<BlogCategory>>(await categoryResponse.Content.ReadAsStringAsync())
                    : new List<BlogCategory>();

                TempData["Error"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(model);
            }

            // Save
            var json = JsonConvert.SerializeObject(model.NewBlog);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await AuthorizedClient.PostAsync("AdminAPI/blog", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = model.NewBlog.Id != 0
                    ? "Blog updated successfully!"
                    : "Blog added successfully!";

                return RedirectToAction("AdminBlog", "Admin", new { id = 0 });
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error saving blog: {errorMsg}";

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/blog/{id}");
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Blog deleted successfully!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Delete failed: {error}";
            }
            return RedirectToAction(nameof(AdminBlog));
        }

        //Blog Category
        [HttpGet]
        public async Task<IActionResult> AdminBlogCategory(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("AdminAPI/blogcategories");
            var categories = response.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<BlogCategory>>(await response.Content.ReadAsStringAsync()) ?? new List<BlogCategory>()
                : new List<BlogCategory>();

            var model = new BlogCategoryViewModel
            {
                CategoryList = categories,
                NewCategory = new BlogCategory()
            };

            if (id > 0)
            {
                var catResponse = await AuthorizedClient.GetAsync($"AdminAPI/blogcategory/{id}");
                if (catResponse.IsSuccessStatusCode)
                {
                    var category = JsonConvert.DeserializeObject<BlogCategory>(await catResponse.Content.ReadAsStringAsync());
                    if (category != null) model.NewCategory = category;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminBlogCategory(BlogCategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Validation failed";
                return View(model);
            }

            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/blogcategory", model.NewCategory);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = model.NewCategory.Id > 0 ? "Category updated successfully" : "Category added successfully";
                return RedirectToAction(nameof(AdminBlogCategory), new { id = 0 });
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Error: {error}";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/blogcategory/{id}");
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Category deleted successfully!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Delete failed: {error}";
            }
            return RedirectToAction(nameof(AdminBlogCategory));
        }


        //Incredients
        [HttpGet]
        public async Task<IActionResult> AdminIngredient(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("AdminAPI/ingredients");
            var ingredients = response.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<Ingredient>>(await response.Content.ReadAsStringAsync())
                : new List<Ingredient>();

            var vm = new IngredientViewModel
            {
                IngredientList = ingredients,
                NewIngredient = new Ingredient()
            };

            if (id > 0)
            {
                var ingResponse = await AuthorizedClient.GetAsync($"AdminAPI/ingredient/{id}");
                if (ingResponse.IsSuccessStatusCode)
                {
                    var ingredient = JsonConvert.DeserializeObject<Ingredient>(await ingResponse.Content.ReadAsStringAsync());
                    if (ingredient != null)
                        vm.NewIngredient = ingredient;
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminIngredient(IngredientViewModel model, IFormFile ImageFile)
        {
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/ingredients");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                model.NewIngredient.ImagePath = "/uploads/ingredients/" + uniqueFileName;
            }

            var json = JsonConvert.SerializeObject(model.NewIngredient);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await AuthorizedClient.PostAsync("AdminAPI/ingredient", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = model.NewIngredient.Id != 0
                    ? "Ingredient updated successfully!"
                    : "Ingredient added successfully!";
                return RedirectToAction("AdminIngredient", "Admin", new { id = 0 });
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, errorMsg);

            var ingredientResponse = await AuthorizedClient.GetAsync("AdminAPI/ingredients");
            model.IngredientList = ingredientResponse.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<Ingredient>>(await ingredientResponse.Content.ReadAsStringAsync())
                : new List<Ingredient>();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/ingredient/{id}");
            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Ingredient deleted successfully!";
            else
                TempData["Error"] = $"Delete failed: {await response.Content.ReadAsStringAsync()}";

            return RedirectToAction(nameof(AdminIngredient));
        }

        public IActionResult Product()
		{
			return View();
		}
		public IActionResult DoctorConsultation()
		{
			return View();
		}

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
				return RedirectToAction("SignIn", "Authentication");
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
