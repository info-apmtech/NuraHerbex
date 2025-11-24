using AspNetCoreHero.ToastNotification.Abstractions;
using Domain.Extensions;
using Domain.Implementation;
using Domain.Interface;
using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ServiceStack.Messaging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Domain.ViewModel.CartItemViewModel;
using static ServiceStack.Diagnostics.Events;

namespace NuraHerbex.Controllers
{
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _environment;
		//private readonly ITokenService _tokenService;
		private readonly INotyfService _notyf;
		public AdminController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment,INotyfService notyf/*, ITokenService tokenService*/)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
			//_tokenService = tokenService;
			_notyf = notyf;
		}
        private System.Net.Http.HttpClient AuthorizedClient => _httpClientFactory.CreateAuthorizedClient(_httpContextAccessor);
        //private string GetUserId() => _httpContextAccessor.GetUserId(_tokenService);
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            // Call your Admin API – adjust URL to match your route
            var response = await AuthorizedClient.GetAsync("AdminAPI/dashboard-stats", ct);

            DashboardStatsDto stats;

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                stats = JsonConvert.DeserializeObject<DashboardStatsDto>(json);
            }
            else
            {
                // Fallback if API fails
                stats = new DashboardStatsDto();
            }

            var vm = new DashboardViewModel
            {
                TotalCustomers = stats.TotalCustomers,
                TotalDoctors = stats.TotalDoctors,
                TotalOrders = stats.TotalOrders
            };

            return View(vm);
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
            var categories = categoryResp.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<BlogCategory>>(await categoryResp.Content.ReadAsStringAsync()) ?? new List<BlogCategory>(): new List<BlogCategory>();
            var blogResp = await AuthorizedClient.GetAsync("AdminAPI/blogs");
            var blogs = blogResp.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<Blog>>(await blogResp.Content.ReadAsStringAsync()) ?? new List<Blog>(): new List<Blog>();
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
                model.Categories = categoryResponse.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<BlogCategory>>(await categoryResponse.Content.ReadAsStringAsync()): new List<BlogCategory>();
				_notyf.Error(string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), 5);
				return View(model);
            }

            // Save
            var json = JsonConvert.SerializeObject(model.NewBlog);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await AuthorizedClient.PostAsync("AdminAPI/blog", content);

            if (response.IsSuccessStatusCode)
            {
				_notyf.Success(model.NewBlog.Id != 0 ? "Blog updated successfully!": "Blog added successfully!",5);
				return RedirectToAction("AdminBlog", "Admin", new { id = 0 });
            }
            var errorMsg = await response.Content.ReadAsStringAsync();
			_notyf.Error($"Error saving blog: {errorMsg}", 5);
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/blog/{id}");
            if (response.IsSuccessStatusCode)
				_notyf.Success("Blog deleted successfully!", 5);
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _notyf.Error($"Delete failed: {error}", 5);
			}
            return RedirectToAction(nameof(AdminBlog));
        }

        //Blog Category
        [HttpGet]
        public async Task<IActionResult> AdminBlogCategory(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("AdminAPI/blogcategories");
            var categories = response.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<BlogCategory>>(await response.Content.ReadAsStringAsync()) ?? new List<BlogCategory>(): new List<BlogCategory>();
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
                _notyf.Error("Validation failed", 5);
				return View(model);
            }
            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/blogcategory", model.NewCategory);
            if (response.IsSuccessStatusCode)
            {
                _notyf.Success(model.NewCategory.Id > 0 ? "Category updated successfully" : "Category added successfully", 5);
				return RedirectToAction(nameof(AdminBlogCategory), new { id = 0 });
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _notyf.Error($"Error: {error}", 5);
				return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/blogcategory/{id}");
            if (response.IsSuccessStatusCode)
                _notyf.Success("Category deleted successfully!", 5);
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _notyf.Error($"Delete failed: {error}", 5);
			}
            return RedirectToAction(nameof(AdminBlogCategory));
        }


        //Incredients
        public async Task<IActionResult> AdminIngredient(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("AdminAPI/ingredients");
            var ingredients = response.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<Ingredient>>(await response.Content.ReadAsStringAsync()): new List<Ingredient>();
            var vm = new IngredientViewModel
            {
                IngredientList = ingredients,
                NewIngredient = new Ingredient()
            };

            var categoryResponse = await AuthorizedClient.GetAsync("AdminAPI/ingredientcategories");
            vm.IngredientCategories = categoryResponse.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<IngredientCategory>>(await categoryResponse.Content.ReadAsStringAsync()): new List<IngredientCategory>();
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
                _notyf.Success(model.NewIngredient.Id != 0? "Ingredient updated successfully!": "Ingredient added successfully!", 5);
				return RedirectToAction("AdminIngredient", "Admin", new { id = 0 });
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, errorMsg);

            var ingredientResponse = await AuthorizedClient.GetAsync("AdminAPI/ingredients");
            model.IngredientList = ingredientResponse.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<Ingredient>>(await ingredientResponse.Content.ReadAsStringAsync()): new List<Ingredient>();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/ingredient/{id}");
            if (response.IsSuccessStatusCode)
            _notyf.Success("Ingredient deleted successfully!", 5);
			else
            _notyf.Error($"Delete failed: {await response.Content.ReadAsStringAsync()}", 5);

			return RedirectToAction(nameof(AdminIngredient));
        }

        //Ingredient Category
        [HttpGet]
        public async Task<IActionResult> AdminIngredientCategory(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("AdminAPI/ingredientcategories");
            var categories = response.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<IngredientCategory>>(await response.Content.ReadAsStringAsync()) ?? new List<IngredientCategory>() : new List<IngredientCategory>();
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
                _notyf.Error("Validation failed", 5);
				return View(model);
            }
            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/ingredientcategory", model.NewCategory);
            if (response.IsSuccessStatusCode)
            {
                _notyf.Success(model.NewCategory.Id > 0 ? "Category updated successfully" : "Category added successfully", 5);
				return RedirectToAction(nameof(AdminIngredientCategory), new { id = 0 });
            }

            var error = await response.Content.ReadAsStringAsync();
            _notyf.Error($"Error: {error}", 5);
			return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIngredientCategory(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/ingredientcategory/{id}");
            if (response.IsSuccessStatusCode)
            _notyf.Success("Category deleted successfully!", 5);
			else
            _notyf.Error($"Delete failed: {await response.Content.ReadAsStringAsync()}", 5);
			return RedirectToAction(nameof(AdminIngredientCategory));
        }
        [HttpGet]
        public async Task<IActionResult> AdminGSTEntry(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("AdminAPI/gstentries");
            var gstList = response.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<GST>>(await response.Content.ReadAsStringAsync()) ?? new List<GST>(): new List<GST>();
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
                _notyf.Error("Validation failed: " + string.Join("; ", errors), 5);
				return View(model);
            }

            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/gstentry", model.NewGST);

            if (response.IsSuccessStatusCode)
            {
                _notyf.Success(model.NewGST.Id > 0 ? "GST entry updated successfully" : "GST entry added successfully", 5);
				return RedirectToAction(nameof(AdminGSTEntry), new { id = 0 });
            }

            var error = await response.Content.ReadAsStringAsync();
            _notyf.Error($"Error: {error}", 5);
			return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGSTEntry(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/gstentry/{id}");
            if (response.IsSuccessStatusCode)
            _notyf.Success("GST entry deleted successfully!", 5);
			else
            _notyf.Error($"Delete failed: {await response.Content.ReadAsStringAsync()}", 5);
			return RedirectToAction(nameof(AdminGSTEntry));
        }
        //Plans

        [HttpGet]
        public async Task<IActionResult> AdminPricingPlan(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("AdminAPI/pricingplans");
            var planList = response.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<PricingPlan>>(await response.Content.ReadAsStringAsync()) ?? new List<PricingPlan>(): new List<PricingPlan>();

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
                _notyf.Error("Validation failed :" + string.Join("; ", errors), 5);
				return View(model);
            }

            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/pricingplan", model.NewPlan);

            if (response.IsSuccessStatusCode)
            {
                _notyf.Success(model.NewPlan.Id > 0 ? "Pricing plan updated successfully" : "Pricing plan added successfully", 5);
				return RedirectToAction(nameof(AdminPricingPlan), new { id = 0 });
            }
            var error = await response.Content.ReadAsStringAsync();
            _notyf.Error($"Error: {error}", 5);
			return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePricingPlan(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/pricingplan/{id}");
            if (response.IsSuccessStatusCode)
            _notyf.Success("Pricing plan deleted successfully!", 5);
			else
            _notyf.Error($"Delete failed: {await response.Content.ReadAsStringAsync()}", 5);
			return RedirectToAction(nameof(AdminPricingPlan));
        }

        public async Task<IActionResult> DoctorConsultation()
        {
            var jsonOptions = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            // Get logged user id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var client = AuthorizedClient;

            // Get full user info
            var userResponse = await client.GetAsync($"AdminAPI/user/{userId}");
            if (!userResponse.IsSuccessStatusCode)
                return Unauthorized();

            var loggedInUser = await userResponse.Content.ReadFromJsonAsync<RegisterUser>(jsonOptions);
            if (loggedInUser == null)
                return Unauthorized();

            var userRole = loggedInUser.Role;

            // Get all doctors
            var doctorResponse = await client.GetAsync("AdminAPI/users/doctor");
            var doctors = doctorResponse.IsSuccessStatusCode ? await doctorResponse.Content.ReadFromJsonAsync<List<RegisterUser>>(jsonOptions): new List<RegisterUser>();
            // Get consultations depending on role
            HttpResponseMessage consultationResponse;

            if (userRole == UserRole.Admin)
                consultationResponse = await client.GetAsync("AdminAPI/consultationbooking/all");
            else if (userRole == UserRole.Doctor)
                consultationResponse = await client.GetAsync($"AdminAPI/consultationbooking/{userId}");
            else
                consultationResponse = await client.GetAsync($"AdminAPI/consultationbooking/user/{userId}");

            var consultations = consultationResponse.IsSuccessStatusCode ? await consultationResponse.Content.ReadFromJsonAsync<List<ConsultationBooking>>(jsonOptions): new List<ConsultationBooking>();
            // Map consultations
            var consultationWithDoctors = consultations.Select(c => new ConsultationWithAssignedDoctorViewModel
            {
                Consultation = c,
                Doctor = doctors.FirstOrDefault(d =>d.Id.Trim().Equals(c.PreferredDoctorId?.Trim(), StringComparison.OrdinalIgnoreCase))
            }).ToList();

            var model = new ConsultationListViewModel
            {
                Consultations = consultationWithDoctors,
                UserRole = userRole.ToString()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptConsultation(int consultationId, string meetingLink)
        {
            if (string.IsNullOrWhiteSpace(meetingLink))
            {
                _notyf.Error("Meeting link is required.", 5);
				return RedirectToAction("DoctorConsultation");
            }

            var updateModel = new ConsultationStatusUpdateModel
            {
                Status = ConsultationStatus.Accepted,
                MeetingLink = meetingLink
            };

            var response = await AuthorizedClient.PostAsJsonAsync( $"AdminAPI/consultationbooking/{consultationId}/status",updateModel);

            if (!response.IsSuccessStatusCode)
            _notyf.Error("Failed to update consultation.", 5);

			return RedirectToAction("DoctorConsultation");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectConsultation(int consultationId)
        {
            var updateModel = new ConsultationStatusUpdateModel
            {
                Status = ConsultationStatus.Rejected,
                MeetingLink = null
            };

            var response = await AuthorizedClient.PostAsJsonAsync($"AdminAPI/consultationbooking/{consultationId}/status",updateModel);

            if (!response.IsSuccessStatusCode)
            _notyf.Error("Failed to reject consultation.", 5);

			return RedirectToAction("DoctorConsultation");
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
                model.UserList = new List<RegisterUser>();

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
			if (model.RegisteredUser == null)
				model.RegisteredUser = new RegisterUser();

			//  NEW user (Add): Id is empty → generate a string Id
			var isNew = string.IsNullOrWhiteSpace(model.RegisteredUser.Id);
			if (isNew)
				model.RegisteredUser.Id = Guid.NewGuid().ToString();  
			var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/register", model.RegisteredUser);
            if (response.IsSuccessStatusCode)
            {
                _notyf.Success("User Registered Successfully!", 5);
				if (User.Identity.IsAuthenticated) 
					//return RedirectToAction("UserCreation", new { role = model.RegisteredUser.Role });
				return RedirectToAction(nameof(UserCreation), new { id = (string)null });
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
                _notyf.Success("User deleted successfully!", 5);
				return RedirectToAction(nameof(UserCreation));
            }

            _notyf.Error("Failed to delete user.", 5);
			return RedirectToAction(nameof(UserCreation));
        }

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
                vm.ProductList = new List<Product>();
            var response = await AuthorizedClient.GetAsync("AdminAPI/gstentries");
            var gstList = response.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<GST>>(await response.Content.ReadAsStringAsync()) ?? new List<GST>() : new List<GST>();
            vm.GSTDetails = gstList;

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
                _notyf.Success(model.NewProduct.Id != 0 ? "Product updated successfully!" : "Product added successfully!", 5);
				return RedirectToAction(nameof(Product), new { id = 0 });
            }
            var errorMsg = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, errorMsg);

            // Refill list on error
            var productResponse = await AuthorizedClient.GetAsync("AdminAPI/products");
            model.ProductList = productResponse.IsSuccessStatusCode ? JsonConvert.DeserializeObject<List<Product>>(await productResponse.Content.ReadAsStringAsync()): new List<Product>();
            var gstResponse = await AuthorizedClient.GetAsync("AdminAPI/gstentries");
            model.GSTDetails = gstResponse.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<GST>>(await gstResponse.Content.ReadAsStringAsync()) : new List<GST>();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/product/{id}");
            if (response.IsSuccessStatusCode)
            _notyf.Success("Product deleted successfully!", 5);
			else
            _notyf.Error($"Delete failed: {await response.Content.ReadAsStringAsync()}", 5);

			return RedirectToAction(nameof(Product));
        }

        [HttpGet]
        public async Task<IActionResult> SubscribeDetail(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("/api/AdminAPI/subscriptions");
            var list = response.IsSuccessStatusCode ? JsonConvert.DeserializeObject<List<NewsletterSubscription>>(await response.Content.ReadAsStringAsync()): new List<NewsletterSubscription>();

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
            var specialities = response.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<DoctorSpeciality>>(await response.Content.ReadAsStringAsync()) ?? new List<DoctorSpeciality>() : new List<DoctorSpeciality>();

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
                _notyf.Error("Validation failed", 5);
				return View(model);
            }

            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/doctorspeciality", model.NewSpeciality);

            if (response.IsSuccessStatusCode)
            {
                _notyf.Success(model.NewSpeciality.Id > 0 ? "Speciality updated successfully" : "Speciality added successfully", 5);
				return RedirectToAction(nameof(AdminDoctorSpeciality), new { id = 0 });
            }

            var error = await response.Content.ReadAsStringAsync();
            _notyf.Error($"Error: {error}", 5);
			return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctorSpeciality(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/doctorspeciality/{id}");
            if (response.IsSuccessStatusCode)
            _notyf.Success("Speciality deleted successfully!", 5);
			else
            _notyf.Error($"Delete failed: {await response.Content.ReadAsStringAsync()}", 5);

			return RedirectToAction(nameof(AdminDoctorSpeciality));
        }

        //DoctorDetails
        [HttpGet]
        public async Task<IActionResult> AdminDoctorDetail(int id = 0)
        {
            // Get doctors (only where Role == Doctor)
            var doctorsResp = await AuthorizedClient.GetAsync("AdminAPI/users"); // adjust endpoint to get all users
            var users = doctorsResp.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<RegisterUser>>(await doctorsResp.Content.ReadAsStringAsync()) ?? new List<RegisterUser>() : new List<RegisterUser>();
            var doctors = users.Where(u => u.Role == UserRole.Doctor).ToList();
            // Get specialities
            var specResp = await AuthorizedClient.GetAsync("AdminAPI/doctorspecialities");
            var specialities = specResp.IsSuccessStatusCode ? JsonConvert.DeserializeObject<List<DoctorSpeciality>>(await specResp.Content.ReadAsStringAsync()) ?? new List<DoctorSpeciality>() : new List<DoctorSpeciality>();
            // Get doctor details
            var detailsResp = await AuthorizedClient.GetAsync("AdminAPI/doctordetails");
            var details = detailsResp.IsSuccessStatusCode ? JsonConvert.DeserializeObject<List<DoctorDetail>>(await detailsResp.Content.ReadAsStringAsync()) ?? new List<DoctorDetail>(): new List<DoctorDetail>();
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
                _notyf.Error("Validation failed", 5);
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
                _notyf.Success(model.DoctorDetail.Id > 0 ? "Doctor detail updated successfully" : "Doctor detail added successfully", 5);
				return RedirectToAction(nameof(AdminDoctorDetail), new { id = 0 });
            }

            _notyf.Error("Error while saving doctor detail", 5);
			return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctorDetail(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/doctordetail/{id}");
            if (response.IsSuccessStatusCode)
            _notyf.Success("Doctor detail deleted successfully!", 5);
			else
            _notyf.Error($"Delete failed: {await response.Content.ReadAsStringAsync()}", 5);

			return RedirectToAction(nameof(AdminDoctorDetail));
        }

        //Pincode

        [HttpGet]
        public async Task<IActionResult> AdminPincode(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("AdminAPI/pincodes");
            var pincodes = response.IsSuccessStatusCode? JsonConvert.DeserializeObject<List<Pincode>>(await response.Content.ReadAsStringAsync()) ?? new List<Pincode>() : new List<Pincode>();

            var vm = new PincodeViewModel
            {
                PincodeList = pincodes,
                NewPincode = new Pincode()
            };

            if (id > 0)
            {
                var pinResp = await AuthorizedClient.GetAsync($"AdminAPI/pincode/{id}");
                if (pinResp.IsSuccessStatusCode)
                {
                    var pincode = JsonConvert.DeserializeObject<Pincode>(await pinResp.Content.ReadAsStringAsync());
                    if (pincode != null)
                        vm.NewPincode = pincode;
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminPincode(PincodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _notyf.Error("Validation failed", 5);
				return View(model);
            }

            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/pincode", model.NewPincode);

            if (response.IsSuccessStatusCode)
            {
                _notyf.Success(model.NewPincode.Id > 0 ? "Pincode updated successfully" : "Pincode added successfully", 5);
				return RedirectToAction(nameof(AdminPincode), new { id = 0 });
            }

            var error = await response.Content.ReadAsStringAsync();
            _notyf.Error($"Error: {error}", 5);
			return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePincode(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/pincode/{id}");
            if (response.IsSuccessStatusCode)
            _notyf.Success("Pincode deleted successfully!", 5);
			else
            _notyf.Error($"Delete failed: {await response.Content.ReadAsStringAsync()}", 5);

			return RedirectToAction(nameof(AdminPincode));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentDetails([FromBody] PaymentGatewayDetails payment, CancellationToken ct)
        {
            if (payment is null)
                return BadRequest("Invalid payload.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);
            using var res = await AuthorizedClient.PostAsJsonAsync("PaymentDetails", payment, ct);
            var contentType = res.Content.Headers.ContentType?.MediaType ?? MediaTypeNames.Application.Json;
            var body = await res.Content.ReadAsStringAsync(ct);

            return new ContentResult
            {
                Content = body,
                ContentType = contentType,
                StatusCode = (int)res.StatusCode
            };
        }
        [HttpGet]
        public async Task<IActionResult> PaymentDetails(CancellationToken ct)
        {

            var response = await AuthorizedClient.GetAsync("/api/AdminAPI/paymentlist", ct);

            var list = response.IsSuccessStatusCode ? JsonConvert.DeserializeObject<List<PaymentGatewayDetails>>(await response.Content.ReadAsStringAsync(ct)): new List<PaymentGatewayDetails>();
            var vm = new PaymentGatewayViewModel
            {
                PaymentGatewayList = list
            };

            return View(vm);
        }
        // Order Status Update Only
        public async Task<IActionResult> AdminOrderStatus()
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var client = AuthorizedClient;
            var response = await client.GetAsync("AdminAPI/orders");
            if (!response.IsSuccessStatusCode)
                return View(new OrderListViewModel());

            var orders = await response.Content.ReadFromJsonAsync<List<Order>>(jsonOptions) ?? new List<Order>();

            var userResponse = await client.GetAsync("AdminAPI/users");
            var users = userResponse.IsSuccessStatusCode ? await userResponse.Content.ReadFromJsonAsync<List<RegisterUser>>(jsonOptions): new List<RegisterUser>();

            foreach (var order in orders)
            {
                if (!string.IsNullOrEmpty(order.UserId))
                {
                    var user = users.FirstOrDefault(u => string.Equals(u.Id?.Trim(), order.UserId.Trim(), StringComparison.OrdinalIgnoreCase));
                    order.UserId = user?.FullName ?? "Unknown User";
                }
                else
                    order.UserId = "Unknown User";
            }

            var vm = new OrderListViewModel
            {
                Orders = orders,
                UserRole = "Admin"
            };

            return View(vm);
        }

        // Filtered Report
        public async Task<IActionResult> AdminOrderReport(string? orderIdFilter,DateTime? startDate,DateTime? endDate,string? statusFilter,string? userNameFilter)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var client = AuthorizedClient;

            // Fetch orders
            var response = await client.GetAsync("AdminAPI/orders");
            if (!response.IsSuccessStatusCode)
                return View(new OrderListViewModel());

            var orders = await response.Content.ReadFromJsonAsync<List<Order>>(jsonOptions) ?? new List<Order>();

            // Fetch users
            var userResponse = await client.GetAsync("AdminAPI/users");
            var users = userResponse.IsSuccessStatusCode ? await userResponse.Content.ReadFromJsonAsync<List<RegisterUser>>(jsonOptions): new List<RegisterUser>();

            // Build UserId -> FullName map
            var userMap = users.Where(u => !string.IsNullOrEmpty(u.Id)).ToDictionary(u => u.Id.Trim(), u => u.FullName ?? "Unknown User", StringComparer.OrdinalIgnoreCase);

            // Map orders to include UserName for filtering
            var ordersWithUserName = orders.Select(o =>
            {
                var userName = !string.IsNullOrEmpty(o.UserId) && userMap.ContainsKey(o.UserId.Trim()) ? userMap[o.UserId.Trim()]: "Unknown User";
                return new
                {
                    Order = o,
                    UserName = userName
                };
            }).ToList();

            bool filtersApplied =!string.IsNullOrEmpty(orderIdFilter) ||startDate.HasValue ||endDate.HasValue ||!string.IsNullOrEmpty(statusFilter) ||!string.IsNullOrEmpty(userNameFilter);
            // DEFAULT: last 7 days excluding Delivered and Cancelled
            if (!filtersApplied)
            {
                DateTime lastWeek = DateTime.Now.AddDays(-7);
                ordersWithUserName = ordersWithUserName.Where(x => x.Order.OrderDate >= lastWeek && x.Order.Status != OrderStatus.Delivered &&x.Order.Status != OrderStatus.Cancelled) .OrderByDescending(x => x.Order.OrderDate).ToList();
            }
            else
            {
                // Order ID filter
                if (!string.IsNullOrEmpty(orderIdFilter) && int.TryParse(orderIdFilter, out int orderIdVal))
                    ordersWithUserName = ordersWithUserName.Where(x => x.Order.Id == orderIdVal).ToList();

                // Username filter
                if (!string.IsNullOrEmpty(userNameFilter))
                    ordersWithUserName = ordersWithUserName.Where(x => x.UserName.Contains(userNameFilter.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
				// Date filters
				if (startDate.HasValue)
                    ordersWithUserName = ordersWithUserName.Where(x => x.Order.OrderDate.Date >= startDate.Value.Date).ToList();

                if (endDate.HasValue)
                    ordersWithUserName = ordersWithUserName.Where(x => x.Order.OrderDate.Date <= endDate.Value.Date).ToList();

                // Status filter
                if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<OrderStatus>(statusFilter, out var statusEnum))
					ordersWithUserName = ordersWithUserName.Where(x => x.Order.Status == statusEnum).ToList();
            }

            // Persist filter values
            ViewBag.OrderIdFilter = orderIdFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.StatusFilter = statusFilter;
            ViewBag.UserNameFilter = userNameFilter;
            ViewBag.UserMap = userMap; // For display in table

            var vm = new OrderListViewModel
            {
                Orders = ordersWithUserName.Select(x => x.Order).ToList(),
                UserRole = "Admin"
            };

            return View(vm);
        }

        public async Task<IActionResult> AdminDispatchedOrders(string? orderIdFilter,DateTime? startDate,DateTime? endDate, string? userNameFilter)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var client = AuthorizedClient;

            // Fetch orders
            var response = await client.GetAsync("AdminAPI/orders");
            if (!response.IsSuccessStatusCode)
                return View(new OrderListViewModel());

            var orders = await response.Content.ReadFromJsonAsync<List<Order>>(jsonOptions)?? new List<Order>();

            // Fetch users
            var userResponse = await client.GetAsync("AdminAPI/users");
            var users = userResponse.IsSuccessStatusCode? await userResponse.Content.ReadFromJsonAsync<List<RegisterUser>>(jsonOptions): new List<RegisterUser>();

            // Build map UserId -> FullName
            var userMap = users.Where(u => !string.IsNullOrEmpty(u.Id)).ToDictionary(u => u.Id.Trim(), u => u.FullName ?? "Unknown User", StringComparer.OrdinalIgnoreCase);
			// Filter ONLY SHIPPED orders
			var shippedOrders = orders.Where(o => o.Status == OrderStatus.Shipped).ToList();
			// Map with username for filtering
			var mappedOrders = shippedOrders.Select(o =>
            {
                var name = (!string.IsNullOrEmpty(o.UserId) && userMap.ContainsKey(o.UserId.Trim())) ? userMap[o.UserId.Trim()] : "Unknown User";
				return new
                {
                    Order = o,
                    UserName = name
                };
            }).ToList();

            // Apply filters
            if (!string.IsNullOrEmpty(orderIdFilter) && int.TryParse(orderIdFilter, out int oid))
                mappedOrders = mappedOrders.Where(x => x.Order.Id == oid).ToList();

            if (!string.IsNullOrEmpty(userNameFilter))
                mappedOrders = mappedOrders.Where(x =>
                    x.UserName.Contains(userNameFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            if (startDate.HasValue)
                mappedOrders = mappedOrders.Where(x =>
                    x.Order.OrderDate.Date >= startDate.Value.Date).ToList();

            if (endDate.HasValue)
                mappedOrders = mappedOrders.Where(x =>
                    x.Order.OrderDate.Date <= endDate.Value.Date).ToList();

            // Persist filter values
            ViewBag.OrderIdFilter = orderIdFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.UserNameFilter = userNameFilter;
            ViewBag.UserMap = userMap;

            // Prepare view model
            var vm = new OrderListViewModel
            {
                Orders = mappedOrders.Select(x => x.Order).ToList(),
                UserRole = "Admin"
            };

            return View(vm);
        }


        public async Task<IActionResult> AdminOrderDetails(int orderId)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var client = AuthorizedClient;

            var orderResponse = await client.GetAsync($"AdminAPI/orders/full/{orderId}");
            if (!orderResponse.IsSuccessStatusCode)
            {
                ViewBag.Error = "Unable to load order.";
                return View(null);
            }

            var orderSummary = await orderResponse.Content.ReadFromJsonAsync<OrderSummaryViewModel>(jsonOptions);
            if (orderSummary == null || orderSummary.Order == null)
            {
                _notyf.Error("Order not found.", 5);
                return View(null);
            }

            var order = orderSummary.Order;
            var orderDetails = orderSummary.Details ?? new List<OrderDetail>();

            var userResponse = await client.GetAsync("AdminAPI/users");
            if (userResponse.IsSuccessStatusCode)
            {
                var users = await userResponse.Content.ReadFromJsonAsync<List<RegisterUser>>(jsonOptions);
                if (!string.IsNullOrEmpty(order.UserId))
                {
                    var user = users.FirstOrDefault(u => string.Equals(u.Id?.Trim(), order.UserId.Trim(), StringComparison.OrdinalIgnoreCase));
                    order.UserId = user?.FullName ?? "Unknown User";
                }
                else
                    order.UserId = "Unknown User";
            }

            var productsResponse = await client.GetAsync("AdminAPI/products");
            List<Product> products = new List<Product>();
            if (productsResponse.IsSuccessStatusCode)
            {
                products = await productsResponse.Content.ReadFromJsonAsync<List<Product>>(jsonOptions);
            }

            foreach (var detail in orderDetails)
            {
                var product = products.FirstOrDefault(p => p.Id == detail.ProductId);
                detail.productName = product?.ProductName ?? "Product not found";
            }

            var vm = new TrackOrderViewModel
            {
                Order = order,
                OrderDetails = orderDetails
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status)
        {
            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/updateStatus", new
            {
                OrderId = orderId,
                Status = status
            });

            if (response.IsSuccessStatusCode)
                _notyf.Success("Order status updated successfully.", 5);
            else
            _notyf.Error("Failed to update order status.", 5);

            return RedirectToAction(nameof(AdminOrderStatus));
        }
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ToggleActive(string id)
		{
			var response = await AuthorizedClient.PostAsync($"AdminAPI/user/{id}/toggle-active", null);

			if (response.IsSuccessStatusCode)
            _notyf.Success("User status updated successfully!", 5);
			else
				_notyf.Success("Failed to update user status.", 5);
			return RedirectToAction(nameof(UserCreation));
		}

        // ====================== ADMIN QUIZ CATEGORY ========================= //

        [HttpGet]
        public async Task<IActionResult> AdminQuizCategory(int id = 0)
        {
            var response = await AuthorizedClient.GetAsync("AdminAPI/quizcategories");

            List<QuizCategory> categories = new List<QuizCategory>();

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                categories = JsonConvert.DeserializeObject<List<QuizCategory>>(json) ?? new List<QuizCategory>();
            }

            var model = new QuizCategoryViewModel
            {
                CategoryList = categories,
                NewCategory = new QuizCategory()
            };

            if (id > 0)
            {
                var catResponse = await AuthorizedClient.GetAsync($"AdminAPI/quizcategory/{id}");

                if (catResponse.IsSuccessStatusCode)
                {
                    var json = await catResponse.Content.ReadAsStringAsync();
                    var category = JsonConvert.DeserializeObject<QuizCategory>(json);
                    if (category != null)
                        model.NewCategory = category;
                }
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminQuizCategory(QuizCategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Validation failed";
                return View(model);
            }

            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/quizcategory", model.NewCategory);

            if (response.IsSuccessStatusCode)
            {
                _notyf.Success(model.NewCategory.Id > 0 ? "Quiz category updated successfully" : "Quiz category added successfully");
				return RedirectToAction(nameof(AdminQuizCategory), new { id = 0 });
            }

            var error = await response.Content.ReadAsStringAsync();
            _notyf.Error($"Error: {error}");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuizCategory(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/quizcategory/{id}");

            if (response.IsSuccessStatusCode)
            _notyf.Success("Quiz category deleted successfully!", 5);
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _notyf.Error($"Delete failed: {error}");
            }

            return RedirectToAction(nameof(AdminQuizCategory));
        }

        // ====================== ADMIN QUIZ QUESTION ========================= //

        [HttpGet]
        public async Task<IActionResult> AdminQuizQuestion(int id = 0)
        {
            var questionResponse = await AuthorizedClient.GetAsync("AdminAPI/quizquestions");
            var categoryResponse = await AuthorizedClient.GetAsync("AdminAPI/quizcategories");

            var model = new QuizQuestionViewModel();

            if (questionResponse.IsSuccessStatusCode)
            {
                var json = await questionResponse.Content.ReadAsStringAsync();
                model.QuestionList = JsonConvert.DeserializeObject<List<QuizQuestion>>(json) ?? new();
            }

            if (categoryResponse.IsSuccessStatusCode)
            {
                var json = await categoryResponse.Content.ReadAsStringAsync();
                model.Categories = JsonConvert.DeserializeObject<List<QuizCategory>>(json) ?? new();
            }

            if (id > 0)
            {
                var qResponse = await AuthorizedClient.GetAsync($"AdminAPI/quizquestion/{id}");
                if (qResponse.IsSuccessStatusCode)
                {
                    var json = await qResponse.Content.ReadAsStringAsync();
                    model.NewQuestion = JsonConvert.DeserializeObject<QuizQuestion>(json) ?? new();
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminQuizQuestion(QuizQuestionViewModel model)
        {
            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/quizquestion", model.NewQuestion);

            if (response.IsSuccessStatusCode)
            {
                _notyf.Success(model.NewQuestion.Id > 0 ? "Quiz question updated successfully" : "Quiz question added successfully");
				return RedirectToAction(nameof(AdminQuizQuestion), new { id = 0 });

            }
            _notyf.Error(await response.Content.ReadAsStringAsync());
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuizQuestion(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/quizquestion/{id}");

            if (response.IsSuccessStatusCode)
            _notyf.Success("Question deleted successfully!", 5);
            else
            _notyf.Error(await response.Content.ReadAsStringAsync(), 5);

            return RedirectToAction(nameof(AdminQuizQuestion));
        }

        // ====================== ADMIN QUIZ OPTION ========================= //

        [HttpGet]
        public async Task<IActionResult> AdminQuizOption(int id = 0)
        {
            var optionResponse = await AuthorizedClient.GetAsync("AdminAPI/quizoptions");
            var questionResponse = await AuthorizedClient.GetAsync("AdminAPI/quizquestions");

            var model = new QuizOptionViewModel();

            if (optionResponse.IsSuccessStatusCode)
            {
                var json = await optionResponse.Content.ReadAsStringAsync();
                model.OptionList = JsonConvert.DeserializeObject<List<QuizOption>>(json) ?? new();
            }

            if (questionResponse.IsSuccessStatusCode)
            {
                var json = await questionResponse.Content.ReadAsStringAsync();
                model.Questions = JsonConvert.DeserializeObject<List<QuizQuestion>>(json) ?? new();
            }

            if (id > 0)
            {
                var oResponse = await AuthorizedClient.GetAsync($"AdminAPI/quizoption/{id}");
                if (oResponse.IsSuccessStatusCode)
                {
                    var json = await oResponse.Content.ReadAsStringAsync();
                    model.NewOption = JsonConvert.DeserializeObject<QuizOption>(json);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminQuizOption(QuizOptionViewModel model)
        {
            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/quizoption", model.NewOption);

            if (response.IsSuccessStatusCode)
            {
                _notyf.Success(model.NewOption.Id > 0 ? "Option updated" : "Option added");
                return RedirectToAction(nameof(AdminQuizOption), new { id = 0 });

            }

            _notyf.Error("Something went wrong");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuizOption(int id)
        {
            var response = await AuthorizedClient.DeleteAsync($"AdminAPI/quizoption/{id}");

            if (response.IsSuccessStatusCode)
            _notyf.Success("Option deleted", 5);
            else
            _notyf.Error("Delete failed",5);

            return RedirectToAction(nameof(AdminQuizOption));
        }
		public async Task<IActionResult> ReturnResponse()
		{
			// JSON options for enum handling
			var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
			jsonOptions.Converters.Add(new JsonStringEnumConverter());

			// Load return requests
			var list = await AuthorizedClient.GetFromJsonAsync<List<ReturnRequestViewDto>>("AdminAPI/ReturnRequest/getreturn",jsonOptions)?? new List<ReturnRequestViewDto>();
			// Load users (same as your other action)
			var userResponse = await AuthorizedClient.GetAsync("AdminAPI/users");

			var users = userResponse.IsSuccessStatusCode? await userResponse.Content.ReadFromJsonAsync<List<RegisterUser>>(jsonOptions): new List<RegisterUser>();
			// Replace UserId with FullName
			foreach (var request in list)
			{
				if (!string.IsNullOrEmpty(request.UserId))
				{
					var user = users.FirstOrDefault(u =>
						string.Equals(u.Id?.Trim(), request.UserId.Trim(), StringComparison.OrdinalIgnoreCase));

					request.UserId = user?.FullName ?? "Unknown User"; // <-- fill FullName
				}
				else
					request.UserId = "Unknown User";
			}

			return View(list);
		}
		// POST: /AdminReturn/UpdateStatus
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ReturnStatusUpdate(UpdateReturnStatusDto dto)
		{
			var response = await AuthorizedClient.PutAsJsonAsync($"AdminAPI/ReturnRequest/{dto.Id}/status", dto);
			if (response.IsSuccessStatusCode)
                _notyf.Success("Return status updated.", 5);
			else
			{
				var content = await response.Content.ReadAsStringAsync();
                _notyf.Error($"Failed to update status: {content}", 5);
			}
			return RedirectToAction("ReturnResponse");
		}
	

        [HttpGet]
        public async Task<IActionResult> AdminMyProfile()
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("SignIn", "Authentication");

            var vm = new AdminProfileViewModel
            {
                Id = userId
            };

            var userResponse = await AuthorizedClient.GetAsync($"AdminAPI/user/{userId}");
            if (userResponse.IsSuccessStatusCode)
            {
                var json = await userResponse.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<RegisterUser>(json);

                vm.FirstName = user.FirstName;
                vm.LastName = user.LastName;
                vm.Email = user.Email;
                vm.PhoneNumber = user.PhoneNumber;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminMyProfile(AdminProfileViewModel model)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("SignIn", "Authentication");

            if (!ModelState.IsValid)
                return View(model);

            var dto = new ProfileUpdateDto
            {
                Id = userId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };

            var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/profile", dto);

            if (response.IsSuccessStatusCode)
            {
                _notyf.Success("Profile updated successfully", 5);
                return RedirectToAction(nameof(AdminMyProfile));
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _notyf.Error(errorBody, 5);

            return View(model);
        }



    }
}
