using Domain.Extensions;
using Domain.Interface;
using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using static ServiceStack.Diagnostics.Events;

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
        private System.Net.Http.HttpClient AuthorizedClient => _httpClientFactory.CreateAuthorizedClient(_httpContextAccessor);
        //private string GetUserId() => _httpContextAccessor.GetUserId(_tokenService);
        public IActionResult Index()
		{
			return View();
		}
		//public IActionResult UserCreation()
		//{
		//	return View();
		//}
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

            var categoryResponse = await AuthorizedClient.GetAsync("AdminAPI/ingredientcategories");
            vm.IngredientCategories = categoryResponse.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<IngredientCategory>>(await categoryResponse.Content.ReadAsStringAsync())
                : new List<IngredientCategory>();

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

        //Ingredient Category
        [HttpGet]
        public async Task<IActionResult> AdminIngredientCategory(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("AdminAPI/ingredientcategories");
            var categories = response.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<IngredientCategory>>(await response.Content.ReadAsStringAsync()) ?? new List<IngredientCategory>()
                : new List<IngredientCategory>();

            var vm = new IngredientCategoryViewModel
            {
                CategoryList = categories,
                NewCategory = new IngredientCategory()
            };

            if (id > 0)
            {
                var catResp = await AuthorizedClient.GetAsync($"AdminAPI/ingredientcategory/{id}");
                if (catResp.IsSuccessStatusCode)
                {
                    var category = JsonConvert.DeserializeObject<IngredientCategory>(await catResp.Content.ReadAsStringAsync());
                    if (category != null)
                        vm.NewCategory = category;
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminIngredientCategory(IngredientCategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Validation failed";
                return View(model);
            }

            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/ingredientcategory", model.NewCategory);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = model.NewCategory.Id > 0 ? "Category updated successfully" : "Category added successfully";
                return RedirectToAction(nameof(AdminIngredientCategory), new { id = 0 });
            }

            var error = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error: {error}";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIngredientCategory(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/ingredientcategory/{id}");
            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Category deleted successfully!";
            else
                TempData["Error"] = $"Delete failed: {await response.Content.ReadAsStringAsync()}";

            return RedirectToAction(nameof(AdminIngredientCategory));
        }
        [HttpGet]
        public async Task<IActionResult> AdminGSTEntry(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("AdminAPI/gstentries");
            var gstList = response.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<GST>>(await response.Content.ReadAsStringAsync()) ?? new List<GST>()
                : new List<GST>();

            var vm = new GSTViewModel
            {
                GSTList = gstList,
                NewGST = new GST()
            };

            if (id > 0)
            {
                var gstResp = await AuthorizedClient.GetAsync($"AdminAPI/gstentry/{id}");
                if (gstResp.IsSuccessStatusCode)
                {
                    var gst = JsonConvert.DeserializeObject<GST>(await gstResp.Content.ReadAsStringAsync());
                    if (gst != null)
                        vm.NewGST = gst;
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminGSTEntry(GSTViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Error"] = "Validation failed: " + string.Join("; ", errors);
                return View(model);
            }

            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/gstentry", model.NewGST);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = model.NewGST.Id > 0 ? "GST entry updated successfully" : "GST entry added successfully";
                return RedirectToAction(nameof(AdminGSTEntry), new { id = 0 });
            }

            var error = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error: {error}";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGSTEntry(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/gstentry/{id}");
            if (response.IsSuccessStatusCode)
                TempData["Success"] = "GST entry deleted successfully!";
            else
                TempData["Error"] = $"Delete failed: {await response.Content.ReadAsStringAsync()}";

            return RedirectToAction(nameof(AdminGSTEntry));
        }

        //Plans

        [HttpGet]
        public async Task<IActionResult> AdminPricingPlan(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("AdminAPI/pricingplans");
            var planList = response.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<PricingPlan>>(await response.Content.ReadAsStringAsync()) ?? new List<PricingPlan>()
                : new List<PricingPlan>();

            var vm = new PricingPlanViewModel
            {
                PlanList = planList ?? new List<PricingPlan>(), 
                NewPlan = new PricingPlan()
            };

            if (id > 0)
            {
                var planResp = await AuthorizedClient.GetAsync($"AdminAPI/pricingplan/{id}");
                if (planResp.IsSuccessStatusCode)
                {
                    var plan = JsonConvert.DeserializeObject<PricingPlan>(await planResp.Content.ReadAsStringAsync());
                    if (plan != null)
                        vm.NewPlan = plan;
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminPricingPlan(PricingPlanViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Error"] = "Validation failed: " + string.Join("; ", errors);
                return View(model);
            }

            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/pricingplan", model.NewPlan);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = model.NewPlan.Id > 0 ? "Pricing plan updated successfully" : "Pricing plan added successfully";
                return RedirectToAction(nameof(AdminPricingPlan), new { id = 0 });
            }

            var error = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error: {error}";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePricingPlan(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/pricingplan/{id}");
            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Pricing plan deleted successfully!";
            else
                TempData["Error"] = $"Delete failed: {await response.Content.ReadAsStringAsync()}";

            return RedirectToAction(nameof(AdminPricingPlan));
        }
        public async Task<IActionResult> DoctorConsultation()
        {
            var jsonOptions = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() } 
            };

            // 1️⃣ Get all consultations from API
            var consultationResponse = await AuthorizedClient.GetAsync("AdminAPI/consultationbooking/all");
            List<ConsultationBooking> consultations = new();
            if (consultationResponse.IsSuccessStatusCode)
                consultations = await consultationResponse.Content.ReadFromJsonAsync<List<ConsultationBooking>>(jsonOptions);

            // 2️⃣ Get all doctors from API
            var doctorResponse = await AuthorizedClient.GetAsync("AdminAPI/users/doctor");
            List<RegisterUser> doctors = new();
            if (doctorResponse.IsSuccessStatusCode)
                doctors = await doctorResponse.Content.ReadFromJsonAsync<List<RegisterUser>>(jsonOptions);

            // 3️⃣ Map consultations with doctors
            var consultationWithDoctors = consultations.Select(c => new ConsultationWithAssignedDoctorViewModel
            {
                Consultation = c,
                Doctor = doctors.FirstOrDefault(d =>
                    !string.IsNullOrEmpty(d.Id) &&
                    !string.IsNullOrEmpty(c.PreferredDoctorId) &&
                    d.Id.Trim() == c.PreferredDoctorId.Trim())
            }).ToList();


            // 4️⃣ Create view model
            var model = new ConsultationListViewModel
            {
                Consultations = consultationWithDoctors
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> UserCreation(string? id = null)
        {
            var model = new RegisterUserViewModel();

            var response = await AuthorizedClient.GetAsync("AdminAPI/users");

            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                model.UserList = JsonConvert.DeserializeObject<List<RegisterUser>>(jsonData);
            }
            else
            {
                model.UserList = new List<RegisterUser>();
            }

            if (!string.IsNullOrEmpty(id)) 
            {
                var userResponse = await AuthorizedClient.GetAsync($"AdminAPI/user/{id}");
                if (userResponse.IsSuccessStatusCode)
                {
                    var userData = await userResponse.Content.ReadAsStringAsync();
                    model.RegisteredUser = JsonConvert.DeserializeObject<RegisterUser>(userData);
                }
            }

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> UserCreation(RegisterUserViewModel model)
        {
            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/register", model.RegisteredUser);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "User Registered Successfully!";
				if (User.Identity.IsAuthenticated) 
					return RedirectToAction("UserCreation", new { role = model.RegisteredUser.Role });
				else
					return RedirectToAction("SignIn", "Authentication");
			}
            var errorMsg = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, errorMsg);
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {

            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/delete/{id}");
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "User deleted successfully!";
                return RedirectToAction(nameof(UserCreation));
            }

            TempData["Error"] = "Failed to delete user.";
            return RedirectToAction(nameof(UserCreation));
        }

        private async Task LoadDropdownsAsync(RegisterUserViewModel model, UserRole role)
        {
            // Example: load countries/states/specialties
            await Task.CompletedTask;
        }

        //public async Task<IActionResult> Product(int id = 0)
        //{
        //    // Get all products
        //    var response = await AuthorizedClient.GetAsync("AdminAPI/products");
        //    var products = response.IsSuccessStatusCode
        //        ? JsonConvert.DeserializeObject<List<Product>>(await response.Content.ReadAsStringAsync())
        //        : new List<Product>();

        //    var vm = new ProductViewModel
        //    {
        //        ProductList = products,
        //        NewProduct = new Product()
        //    };
        //    if (id > 0)
        //    {
        //        var prodResponse = await AuthorizedClient.GetAsync($"AdminAPI/product/{id}");
        //        if (prodResponse.IsSuccessStatusCode)
        //        {
        //            var product = JsonConvert.DeserializeObject<Product>(await prodResponse.Content.ReadAsStringAsync());
        //            if (product != null)
        //                vm.NewProduct = product;
        //        }
        //    }

        //    return View(vm);

        //}
        public async Task<IActionResult> Product(int id = 0)
        {
            var vm = new ProductViewModel();

            var listRes = await AuthorizedClient.GetAsync("AdminAPI/products");
            if (listRes.IsSuccessStatusCode)
            {
                var json = await listRes.Content.ReadAsStringAsync();
                vm.ProductList = JsonConvert.DeserializeObject<List<Product>>(json) ?? new List<Product>();
            }
            else
            {
                vm.ProductList = new List<Product>();
            }
            var response = await AuthorizedClient.GetAsync("AdminAPI/gstentries");
            var gstList = response.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<GST>>(await response.Content.ReadAsStringAsync()) ?? new List<GST>()
                : new List<GST>();
            if (id > 0)
            {
                var prodRes = await AuthorizedClient.GetAsync($"AdminAPI/product/{id}");
                if (prodRes.IsSuccessStatusCode)
                {
                    vm.NewProduct = JsonConvert.DeserializeObject<Product>(await prodRes.Content.ReadAsStringAsync()) ?? new Product();
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Product(ProductViewModel model)
        {
            var currentUser =User.Identity?.Name?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.NameIdentifier)?? "system";
            if (model.NewProduct.Id == 0)
                model.NewProduct.CreatedBy = currentUser;

            model.NewProduct.UpdatedBy = currentUser;
            var ProductFiles = model.ProductFiles;
            if (ProductFiles != null && ProductFiles.Count > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var relativePaths = new List<string>();

                foreach (var file in ProductFiles)
                {
                    if (file?.Length > 0)
                    {
                        var uniqueFile = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFile);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        relativePaths.Add("/uploads/products/" + uniqueFile);
                    }
                }

                if (relativePaths.Count > 0)
                    model.NewProduct.ProductImages = string.Join(";", relativePaths);
            }

            // Send JSON to AdminAPI/product (same pattern as Ingredients)
            var json = JsonConvert.SerializeObject(model.NewProduct);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await AuthorizedClient.PostAsync("AdminAPI/product", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = model.NewProduct.Id != 0
                    ? "Product updated successfully!"
                    : "Product added successfully!";
                return RedirectToAction(nameof(Product), new { id = 0 });
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, errorMsg);

            // Refill list on error
            var productResponse = await AuthorizedClient.GetAsync("AdminAPI/products");
            model.ProductList = productResponse.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<Product>>(await productResponse.Content.ReadAsStringAsync())
                : new List<Product>();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/product/{id}");
            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Product deleted successfully!";
            else
                TempData["Error"] = $"Delete failed: {await response.Content.ReadAsStringAsync()}";

            return RedirectToAction(nameof(Product));
        }

        [HttpGet]
        public async Task<IActionResult> SubscribeDetail(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("/api/AdminAPI/subscriptions");
            var list = response.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<NewsletterSubscription>>(await response.Content.ReadAsStringAsync())
                : new List<NewsletterSubscription>();

            var vm = new SubscriptionViewModel
            {
                SubscriptionList = list
            };

            return View(vm);
        }

        // Speciality
        [HttpGet]
        public async Task<IActionResult> AdminDoctorSpeciality(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("AdminAPI/doctorspecialities");
            var specialities = response.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<DoctorSpeciality>>(await response.Content.ReadAsStringAsync()) ?? new List<DoctorSpeciality>()
                : new List<DoctorSpeciality>();

            var vm = new DoctorSpecialityViewModel
            {
                SpecialityList = specialities,
                NewSpeciality = new DoctorSpeciality()
            };

            if (id > 0)
            {
                var specResp = await AuthorizedClient.GetAsync($"AdminAPI/doctorspeciality/{id}");
                if (specResp.IsSuccessStatusCode)
                {
                    var speciality = JsonConvert.DeserializeObject<DoctorSpeciality>(await specResp.Content.ReadAsStringAsync());
                    if (speciality != null)
                        vm.NewSpeciality = speciality;
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminDoctorSpeciality(DoctorSpecialityViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Validation failed";
                return View(model);
            }

            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/doctorspeciality", model.NewSpeciality);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = model.NewSpeciality.Id > 0 ? "Speciality updated successfully" : "Speciality added successfully";
                return RedirectToAction(nameof(AdminDoctorSpeciality), new { id = 0 });
            }

            var error = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error: {error}";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctorSpeciality(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/doctorspeciality/{id}");
            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Speciality deleted successfully!";
            else
                TempData["Error"] = $"Delete failed: {await response.Content.ReadAsStringAsync()}";

            return RedirectToAction(nameof(AdminDoctorSpeciality));
        }

        //DoctorDetails
        [HttpGet]
        public async Task<IActionResult> AdminDoctorDetail(int id = 0)
        {
            // Get doctors (only where Role == Doctor)
            var doctorsResp = await AuthorizedClient.GetAsync("AdminAPI/users"); // adjust endpoint to get all users
            var users = doctorsResp.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<RegisterUser>>(await doctorsResp.Content.ReadAsStringAsync()) ?? new List<RegisterUser>()
                : new List<RegisterUser>();

            var doctors = users.Where(u => u.Role == UserRole.Doctor).ToList();

            // Get specialities
            var specResp = await AuthorizedClient.GetAsync("AdminAPI/doctorspecialities");
            var specialities = specResp.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<DoctorSpeciality>>(await specResp.Content.ReadAsStringAsync()) ?? new List<DoctorSpeciality>()
                : new List<DoctorSpeciality>();

            // Get doctor details
            var detailsResp = await AuthorizedClient.GetAsync("AdminAPI/doctordetails");
            var details = detailsResp.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<List<DoctorDetail>>(await detailsResp.Content.ReadAsStringAsync()) ?? new List<DoctorDetail>()
                : new List<DoctorDetail>();

            var vm = new DoctorDetailViewModel
            {
                DoctorDetailList = details ?? new List<DoctorDetail>(),
                DoctorDetail = new DoctorDetail(), 
                Doctors = doctors ?? new List<RegisterUser>(),
                Specialities = specialities ?? new List<DoctorSpeciality>()
            };


            if (id > 0)
            {
                var detailResp = await AuthorizedClient.GetAsync($"AdminAPI/doctordetail/{id}");
                if (detailResp.IsSuccessStatusCode)
                {
                    var detail = JsonConvert.DeserializeObject<DoctorDetail>(await detailResp.Content.ReadAsStringAsync());
                    if (detail != null)
                    {
                        if (!string.IsNullOrEmpty(detail.SpecalityIds))
                            detail.SelectedSpecialityIds = detail.SpecalityIds.Split(',').Select(int.Parse).ToList();
                        vm.DoctorDetail = detail;
                    }
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminDoctorDetail(DoctorDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Validation failed";
                return View(model);
            }

            // Handle file upload
            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.PhotoFile.FileName)}";
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/doctors");
                Directory.CreateDirectory(uploadFolder);

                var filePath = Path.Combine(uploadFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.PhotoFile.CopyToAsync(stream);
                }

                // ✅ Save relative path instead of just filename
                model.DoctorDetail.PhotoPath = $"/uploads/doctors/{fileName}";
            }

            // Convert selected specialties to comma-separated string
            model.DoctorDetail.SpecalityIds = string.Join(",", model.DoctorDetail.SelectedSpecialityIds ?? new List<int>());

            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/doctordetail", model.DoctorDetail);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = model.DoctorDetail.Id > 0
                    ? "Doctor detail updated successfully"
                    : "Doctor detail added successfully";
                return RedirectToAction(nameof(AdminDoctorDetail), new { id = 0 });
            }

            TempData["Error"] = "Error while saving doctor detail";
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctorDetail(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/doctordetail/{id}");
            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Doctor detail deleted successfully!";
            else
                TempData["Error"] = $"Delete failed: {await response.Content.ReadAsStringAsync()}";

            return RedirectToAction(nameof(AdminDoctorDetail));
        }

    }
}
