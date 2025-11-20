using Domain.Extensions;
using Domain.Implementation;
using Domain.Models;
using Domain.ViewModel;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;
using NuraHerbex.Models;
using Org.BouncyCastle.Ocsp;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
//using static ServiceStack.Diagnostics.Events;
using JsonSerializer = System.Text.Json.JsonSerializer;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;
//using static ServiceStack.Diagnostics.Events;

namespace NuraHerbex.Controllers
{
    public class HomeController : Controller
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ILogger<HomeController> _logger;
		private readonly EmailSettings _emailSettings;
		private readonly IHttpContextAccessor _httpContextAccessor;

		private readonly HttpClient _httpClient;
        private JsonSerializerOptions? _jsonOptions;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IOptions<EmailSettings> emailSettings, IHttpContextAccessor httpContextAccessor)
		{
			_logger = logger;
			_emailSettings = emailSettings.Value;
			_httpClientFactory = httpClientFactory;
			_httpContextAccessor = httpContextAccessor;
			_httpClient = httpClientFactory.CreateClient("NuraHerbexApi");

		}
		private System.Net.Http.HttpClient AuthorizedClient => _httpClientFactory.CreateAuthorizedClient(_httpContextAccessor);

		public async Task<IActionResult> Index()
		{
			var blogs = new List<Blog>();
			var ingredients = new List<Ingredient>();
			var products = new List<Product>();
			var feedbacks = new List<FeedbackViewModel>();

			// Fetch blogs
			var blogResponse = await _httpClient.GetAsync("AdminAPI/blogs");
			if (blogResponse.IsSuccessStatusCode)
				blogs = JsonConvert.DeserializeObject<List<Blog>>(await blogResponse.Content.ReadAsStringAsync()) ?? new List<Blog>();

			// Fetch ingredients
			var ingredientResponse = await _httpClient.GetAsync("AdminAPI/ingredients");
			if (ingredientResponse.IsSuccessStatusCode)
				ingredients = JsonConvert.DeserializeObject<List<Ingredient>>(await ingredientResponse.Content.ReadAsStringAsync()) ?? new List<Ingredient>();

			// Fetch products
			var productsResponse = await _httpClient.GetAsync("AdminAPI/products");
			if (productsResponse.IsSuccessStatusCode)
				products = JsonConvert.DeserializeObject<List<Product>>(await productsResponse.Content.ReadAsStringAsync()) ?? new List<Product>();

			// Fetch feedbacks via API
			var feedbackResponse = await _httpClient.GetAsync("AdminAPI/feedbacks");
			if (feedbackResponse.IsSuccessStatusCode)
				feedbacks = JsonConvert.DeserializeObject<List<FeedbackViewModel>>(await feedbackResponse.Content.ReadAsStringAsync())
							?? new List<FeedbackViewModel>();

			// Filter home ingredients, featured products, etc.
			var homeIngredients = ingredients
				.Where(i => i.IsActive && i.ShowHome)
				.OrderByDescending(i => i.CreatedAt)
				.Take(6)
				.ToList();

			var latestBlogs = blogs
				.OrderByDescending(b => b.CreatedAt)
				.Take(10)
				.ToList();

			var featuredProducts = products.OrderBy(p => p.Id).ToList();

			// Fetch user's wishlist IDs if logged in
			var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			List<int> wishlistIds = new List<int>();
			if (!string.IsNullOrEmpty(userId))
			{
				var wishlistResponse = await _httpClient.GetAsync($"AdminAPI/wishlist/{userId}");
				if (wishlistResponse.IsSuccessStatusCode)
				{
					var wishlistItems = await wishlistResponse.Content.ReadFromJsonAsync<List<WishlistItem>>();
					wishlistIds = wishlistItems?.Select(x => x.ProductId).ToList() ?? new List<int>();
				}
			}

			// Build ViewModel
			var vm = new HomeViewModel
			{
				BlogList = latestBlogs,
				Ingredients = homeIngredients,
				PlanList = await GetPlansFromApi(),
				FeaturedProducts = featuredProducts,
				WishlistProductIds = wishlistIds,
				FeedbackList = feedbacks
			};

			return View(vm);
		}

		public IActionResult About()
		{
			return View();
		}
		public async Task<IActionResult> Blog()
		{
			// 1️⃣ Get blogs
			var blogsResponse = await _httpClient.GetAsync("AdminAPI/blogs");
			var categoriesResponse = await _httpClient.GetAsync("AdminAPI/blogcategories");

			var blogs = new List<Blog>();
			var categories = new List<BlogCategory>();

			if (blogsResponse.IsSuccessStatusCode)
			{
				var json = await blogsResponse.Content.ReadAsStringAsync();
				blogs = JsonConvert.DeserializeObject<List<Blog>>(json);
			}

			if (categoriesResponse.IsSuccessStatusCode)
			{
				var json = await categoriesResponse.Content.ReadAsStringAsync();
				categories = JsonConvert.DeserializeObject<List<BlogCategory>>(json);
			}

			// 2️⃣ Get doctors
			var doctorResponse = await _httpClient.GetAsync("AdminAPI/users/doctor");
			var doctors = new List<RegisterUser>();
			if (doctorResponse.IsSuccessStatusCode)
			{
				var json = await doctorResponse.Content.ReadAsStringAsync();
				doctors = JsonConvert.DeserializeObject<List<RegisterUser>>(json);
			}

			// 3️⃣ Get doctor details
			var doctorDetailResponse = await _httpClient.GetAsync("AdminAPI/doctordetails");
			var doctorDetails = new List<DoctorDetail>();
			if (doctorDetailResponse.IsSuccessStatusCode)
			{
				var json = await doctorDetailResponse.Content.ReadAsStringAsync();
				doctorDetails = JsonConvert.DeserializeObject<List<DoctorDetail>>(json);
			}

			// 4️⃣ Merge doctors with details and filter only active
			var doctorViewModels = doctors
				.Select(d =>
				{
					var detail = doctorDetails.FirstOrDefault(dd => dd.DoctorId == d.Id && dd.IsWorking);
					if (detail == null) return null;

					return new DoctorViewModel
					{
						Id = d.Id,
						FullName = $"{d.FirstName} {d.LastName}",
						PhotoPath = detail.PhotoPath,
						PrimarySpeciality = detail.PrimarySpecality ?? "General"
					};
				})
				.Where(d => d != null)
				.ToList()!;

			// 5️⃣ Create ViewModel
			var vm = new BlogViewModel
			{
				BlogList = blogs,
				Categories = categories,
				Doctors = doctorViewModels
			};

			return View(vm);
		}

		public async Task<IActionResult> BlogDetail(int id)
		{
			// Increment read count first
			var incrementResponse = await _httpClient.PostAsync($"AdminAPI/blog/incrementreadcount/{id}", null);
			if (!incrementResponse.IsSuccessStatusCode)
			{
				// Optionally log error but continue
			}

			// Get blog details
			var blogResponse = await _httpClient.GetAsync($"AdminAPI/blog/{id}");
			var categoriesResponse = await _httpClient.GetAsync("AdminAPI/blogcategories");

			if (!blogResponse.IsSuccessStatusCode)
				return NotFound();

			var blogJson = await blogResponse.Content.ReadAsStringAsync();
			var blog = JsonConvert.DeserializeObject<Blog>(blogJson);

			var categories = new List<BlogCategory>();
			if (categoriesResponse.IsSuccessStatusCode)
			{
				var catJson = await categoriesResponse.Content.ReadAsStringAsync();
				categories = JsonConvert.DeserializeObject<List<BlogCategory>>(catJson);
			}

			var vm = new BlogViewModel
			{
				NewBlog = blog,
				Categories = categories
			};

			return View(vm);
		}
		public async Task<IActionResult> BlogsByCategory(int categoryId)
		{
			var blogsResponse = await _httpClient.GetAsync("AdminAPI/blogs");
			var categoriesResponse = await _httpClient.GetAsync("AdminAPI/blogcategories");

			var blogs = new List<Blog>();
			var categories = new List<BlogCategory>();

			if (blogsResponse.IsSuccessStatusCode)
			{
				var json = await blogsResponse.Content.ReadAsStringAsync();
				blogs = JsonConvert.DeserializeObject<List<Blog>>(json);
			}

			if (categoriesResponse.IsSuccessStatusCode)
			{
				var json = await categoriesResponse.Content.ReadAsStringAsync();
				categories = JsonConvert.DeserializeObject<List<BlogCategory>>(json);
			}

			// Filter blogs by category
			var filteredBlogs = blogs.Where(b => !b.IsFeatured &&
				b.BlogCategoryIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
								 .Contains(categoryId.ToString()))
				.OrderByDescending(b => b.CreatedAt)
				.ToList();

			var vm = new BlogViewModel
			{
				BlogList = filteredBlogs,
				Categories = categories,
				CategoryId = categoryId
			};

			return View("BlogsByCategory", vm);
		}
        [HttpGet]
        public async Task<IActionResult> Plan(int? score)
        {
            var plans = await GetPlansFromApi();

            // If score is null → user came directly → show all plans
            if (score == null)
            {
                ViewBag.IsFromQuiz = false;
                return View(plans);
            }

            // User came via quiz
            ViewBag.IsFromQuiz = true;
            ViewBag.Score = score.Value;

            List<PricingPlan> filteredPlans;

            if (score <= 10)
            {
                filteredPlans = plans.Where(p => p.PlanName == "Elite Pack").ToList();
            }
            else if (score > 10 && score <= 15)
            {
                filteredPlans = plans.Where(p => p.PlanName == "Performance Pack").ToList();
            }
            else 
            {
                filteredPlans = plans.Where(p => p.PlanName == "Essential Pack").ToList();
            }

            return View(filteredPlans);
        }



        private async Task<List<PricingPlan>> GetPlansFromApi()
        {
            var plans = new List<PricingPlan>();
            var response = await _httpClient.GetAsync("AdminAPI/pricingplans");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                plans = JsonConvert.DeserializeObject<List<PricingPlan>>(json) ?? new List<PricingPlan>();
            }
            return plans;
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlanSubscribe(SubscriptionPaymentViewModel model)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var emailAddress = !string.IsNullOrWhiteSpace(model.EmailAddress)
                ? model.EmailAddress
                : User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";

            var paymentMethod = !string.IsNullOrWhiteSpace(model.PaymentMethod)
                ? model.PaymentMethod
                : "UPI";

            // ✅ CASE 1: PAYMENT FAILED / CANCELLED → DO NOT SAVE ANYTHING
            if (!model.HasPaid || string.IsNullOrEmpty(model.RazorpayPaymentId))
            {
                return RedirectToAction("SubscriptionConfirmation", new
                {
                    isSuccess = false,
                    message = string.IsNullOrWhiteSpace(model.PaymentError)
                        ? "Payment was cancelled or failed."
                        : model.PaymentError,
                    amount = model.Amount,
                    paymentMethod = paymentMethod,
                    email = emailAddress
                });
            }

            // ✅ CASE 2: PAYMENT SUCCESS → SAVE PAYMENT + SUBSCRIPTION, THEN CONFIRMATION

            var contactNo = !string.IsNullOrWhiteSpace(model.ContactNo)
                ? model.ContactNo
                : User?.FindFirst("phone_number")?.Value ?? "";

            var paymentDetails = !string.IsNullOrWhiteSpace(model.PaymentDetails)
                ? model.PaymentDetails
                : $"Subscription for PlanId: {model.PlanId}";

            var payment = new PaymentGatewayDetails
            {
                PaymentId = model.RazorpayPaymentId,
                BankRRn = model.BankRRn ?? "",
                OrderId = model.RazorpayOrderId,
                PaymentMethod = paymentMethod,
                PaymentDetails = paymentDetails,
                TotalAmount = model.Amount,
                ContactNo = contactNo,
                EmailAddress = emailAddress
            };

            var paymentResponse = await AuthorizedClient.PostAsJsonAsync("AdminAPI/PaymentDetails", payment);

            if (!paymentResponse.IsSuccessStatusCode)
            {
                var body = await paymentResponse.Content.ReadAsStringAsync();
                // log body if needed

                return RedirectToAction("SubscriptionConfirmation", new
                {
                    isSuccess = false,
                    message = "Payment succeeded but saving payment details failed.",
                    amount = model.Amount,
                    paymentMethod = paymentMethod,
                    email = emailAddress
                });
            }

            var subscription = new Subscription
            {
                UserId = userId,
                PlanId = model.PlanId,
                UpdatedAt = DateTime.UtcNow
            };

            var subscriptionResponse = await AuthorizedClient.PostAsJsonAsync("AdminAPI/subscription", subscription);

            if (subscriptionResponse.IsSuccessStatusCode)
            {
                return RedirectToAction("SubscriptionConfirmation", new
                {
                    isSuccess = true,
                    message = "Your subscription has been activated successfully.",
                    amount = model.Amount,
                    paymentMethod = paymentMethod,
                    email = emailAddress
                });
            }

            return RedirectToAction("SubscriptionConfirmation", new
            {
                isSuccess = false,
                message = "Payment succeeded but subscription could not be saved. Please contact support.",
                amount = model.Amount,
                paymentMethod = paymentMethod,
                email = emailAddress
            });
        }


        [HttpGet]
        public IActionResult SubscriptionConfirmation(
    bool isSuccess,
    string message,
    decimal amount,
    string paymentMethod,
    string email)
        {
            var vm = new SubscriptionConfirmationViewModel
            {
                IsSuccess = isSuccess,
                Message = message,
                Amount = amount,
                PaymentMethod = paymentMethod,
                Email = email
            };

            return View(vm); // Views/Home/SubscriptionConfirmation.cshtml
        }


        //[HttpPost]
        //public async Task<IActionResult> PlanSubscribe(int planId)
        //{
        //    var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized();

        //    var subscription = new Subscription
        //    {
        //        UserId = userId,
        //        PlanId = planId,
        //        UpdatedAt = DateTime.UtcNow
        //    };

        //    var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/subscription", subscription);

        //    if (response.IsSuccessStatusCode)
        //    {
        //        TempData["Success"] = "Subscription successful!";
        //        return RedirectToAction("Plan");
        //    }

        //    TempData["Error"] = "Unable to subscribe. Try again.";
        //    return RedirectToAction("Plan");
        //}



        public async Task<IActionResult> Shop(int id = 0)
		{
			var products = new List<Product>();
			var feedbacks = new List<FeedbackViewModel>();

			// Fetch products
			var productsResponse = await _httpClient.GetAsync("AdminAPI/products");
			if (productsResponse.IsSuccessStatusCode)
				products = JsonConvert.DeserializeObject<List<Product>>(await productsResponse.Content.ReadAsStringAsync()) ?? new List<Product>();

			// Fetch feedbacks
			var feedbackResponse = await _httpClient.GetAsync("AdminAPI/feedbacks");
			if (feedbackResponse.IsSuccessStatusCode)
				feedbacks = JsonConvert.DeserializeObject<List<FeedbackViewModel>>(await feedbackResponse.Content.ReadAsStringAsync())
							?? new List<FeedbackViewModel>();

			// Optional: pre-select a product
			Product? selected = null;
			if (id > 0)
			{
				var oneResponse = await _httpClient.GetAsync($"AdminAPI/product/{id}");
				if (oneResponse.IsSuccessStatusCode)
					selected = JsonConvert.DeserializeObject<Product>(await oneResponse.Content.ReadAsStringAsync());
			}

			// Helper functions for benefits
			static (string Heading, string Desc) Unpack(string? s)
			{
				if (string.IsNullOrWhiteSpace(s)) return ("", "");
				var parts = s.Split('|', 2);
				return (parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : "");
			}

			static List<KeyValuePair<string, string>> Extract(Product? p)
			{
				var list = new List<KeyValuePair<string, string>>();
				if (p == null) return list;
				foreach (var raw in new[] { p.KeyBenefits1, p.KeyBenefits2, p.KeyBenefits3, p.KeyBenefits4 })
				{
					var (h, d) = Unpack(raw);
					if (!string.IsNullOrWhiteSpace(h) || !string.IsNullOrWhiteSpace(d))
						list.Add(new KeyValuePair<string, string>(h, d));
				}
				return list;
			}

			var vm = new ProductViewModel
			{
				ProductList = products,
				NewProduct = selected ?? new Product(),
				FeedbackList = feedbacks.Take(10).ToList()
			};

			ViewBag.SelectedBenefits = Extract(vm.NewProduct);
			ViewBag.BenefitsByProduct = products.GroupBy(p => p.Id)
												.ToDictionary(g => g.Key, g => Extract(g.First()));

			return View(vm);
		}
		[HttpGet]
		public async Task<IActionResult> GetProduct(int id)
		{
			// call the AdminAPI endpoint from MVC
			var res = await _httpClient.GetAsync($"AdminAPI/product/{id}");
			if (!res.IsSuccessStatusCode)
				return NotFound();

			var json = await res.Content.ReadAsStringAsync();
			var p = JsonConvert.DeserializeObject<Product>(json);
			if (p == null)
				return NotFound();

			// split images and prepare front-end fields
			var imgs = (p.ProductImages ?? string.Empty)
				.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToList();

			var inr = new System.Globalization.CultureInfo("en-IN");
			var amount = p.Amount;
			var disc = p.DiscountPercentage;
			var final = amount - (amount * (disc / 100m));



			return Json(new
			{
				id = p.Id,
				productName = p.ProductName,
				description = !string.IsNullOrWhiteSpace(p.Description) ? p.Description : p.SubTitle,
				amount = p.Amount,
				discountPercentage = p.DiscountPercentage,
				productImages = p.ProductImages,
				keyBenefits1 = p.KeyBenefits1,
				keyBenefits2 = p.KeyBenefits2,
				keyBenefits3 = p.KeyBenefits3,
				keyBenefits4 = p.KeyBenefits4,
				forThis1 = p.ForThis1,
				forThis2 = p.ForThis2,
				forThis3 = p.ForThis3,
				forThis4 = p.ForThis4
			});
		}
       
        [HttpGet]
        public async Task<IActionResult> Quiz()
        {
            var categoriesResponse = await _httpClient.GetAsync("AdminAPI/quizcategories");
            categoriesResponse.EnsureSuccessStatusCode();
            var categoriesJson = await categoriesResponse.Content.ReadAsStringAsync();
            var categories = JsonConvert.DeserializeObject<List<QuizCategory>>(categoriesJson);
            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestion(int categoryId = 0, int questionIndex = 0, int totalPoints = 0)
        {
            // Get all questions for category
            var questionsResponse = await _httpClient.GetAsync("AdminAPI/quizquestions");
            questionsResponse.EnsureSuccessStatusCode();
            var questionsJson = await questionsResponse.Content.ReadAsStringAsync();
            var allQuestions = JsonConvert.DeserializeObject<List<QuizQuestion>>(questionsJson);
            var questions = allQuestions.Where(q => q.CategoryId == categoryId).ToList();

            if (questionIndex >= questions.Count)
            {
                // Quiz finished -> redirect to Plan page with totalPoints
                return Json(new
                {
                    quizFinished = true,
                    totalPoints = totalPoints,
                    totalQuestions = questions.Count,
                    redirectUrl = Url.Action("Plan", "Home", new { score = totalPoints })
                });
            }

            var currentQuestion = questions[questionIndex];

            var optionsResponse = await _httpClient.GetAsync("AdminAPI/quizoptions");
            optionsResponse.EnsureSuccessStatusCode();
            var optionsJson = await optionsResponse.Content.ReadAsStringAsync();
            var allOptions = JsonConvert.DeserializeObject<List<QuizOption>>(optionsJson);
            var options = allOptions.Where(o => o.QuestionId == currentQuestion.Id).ToList();

            return Json(new
            {
                quizFinished = false,
                currentQuestionIndex = questionIndex,
                totalPoints,
                currentQuestion = currentQuestion,
                options
            });
        }

        public async Task<IActionResult> Ingredients()
		{
			var ingredientsResponse = await _httpClient.GetAsync("AdminAPI/ingredients");
			var categoriesResponse = await _httpClient.GetAsync("AdminAPI/ingredientcategories");

			var ingredients = new List<Ingredient>();
			var categories = new List<IngredientCategory>();

			if (ingredientsResponse.IsSuccessStatusCode)
			{
				var json = await ingredientsResponse.Content.ReadAsStringAsync();
				ingredients = JsonConvert.DeserializeObject<List<Ingredient>>(json) ?? new List<Ingredient>();
			}

			if (categoriesResponse.IsSuccessStatusCode)
			{
				var json = await categoriesResponse.Content.ReadAsStringAsync();
				categories = JsonConvert.DeserializeObject<List<IngredientCategory>>(json) ?? new List<IngredientCategory>();
			}

			var vm = new IngredientViewModel
			{
				IngredientList = ingredients,
				IngredientCategories = categories
			};

			return View(vm);
		}
		// DRY helper
		private async Task<CartViewModel> BuildCartViewModelAsync()
		{
			var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
						 ?? User?.Identity?.Name;

			if (string.IsNullOrEmpty(userId))
				return new CartViewModel();

			try
			{
				var client = AuthorizedClient ?? _httpClient;

				var cartResponse = await client.GetAsync($"AdminAPI/Cart/{userId}");
				if (!cartResponse.IsSuccessStatusCode) return new CartViewModel();
				var cartItems = await cartResponse.Content.ReadFromJsonAsync<List<CartItem>>() ?? new();

				var productResponse = await client.GetAsync("AdminAPI/products");
				var products = productResponse.IsSuccessStatusCode
					? await productResponse.Content.ReadFromJsonAsync<List<Product>>()
					: new List<Product>();

				return new CartViewModel
				{
                    Items = cartItems
        .GroupJoin(
            products,
            c => c.ProductId,
            p => p.Id,
            (c, prodJoin) => new { c, prodJoin }
        )
        .SelectMany(
            x => x.prodJoin.DefaultIfEmpty(),
            (x, p) => new CartItemViewModel
            {
                CartItemId = x.c.Id,
                ProductId = x.c.ProductId,
                ProductName = p?.ProductName ?? $"Product #{x.c.ProductId}",
                ProductImages = p?.ProductImages,
                Quantity = x.c.Quantity,
                UnitPrice = x.c.Price
            }
        )
        .ToList()
                };
			}
			catch
			{
				return new CartViewModel();
			}
		}
		//public async Task<IActionResult> OrderSummary()
		//{
		//	var vm = await BuildCartViewModelAsync(); // reuse logic
		//	return View(vm); // strongly-typed view: @model CartViewModel
		//}
		[HttpGet]
		public async Task<IActionResult> OrderSummary(int? addressId = null)
		{
			var cart = await BuildCartViewModelAsync();

			var vm = new OrderSummaryViewModel
			{
				Cart = cart,
				Items = cart.Items?.ToList() ?? new(),
				Addresses = new List<AddressDetail>()
			};

			var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
			if (!string.IsNullOrEmpty(userId))
			{
				var resp = await _httpClient.GetAsync($"AdminAPI/addresses/{userId}");
				if (resp.IsSuccessStatusCode)
					vm.Addresses = await resp.Content.ReadFromJsonAsync<List<AddressDetail>>() ?? new();
			}

			vm.SelectedAddressId =
				addressId ??
				vm.Addresses.FirstOrDefault(a => (bool?)a.IsDefault == true)?.Id ??
				vm.Addresses.FirstOrDefault()?.Id;

			vm.AddressItems = vm.Addresses.Select(a => new SelectListItem
			{
				Value = a.Id.ToString(),
				Text  = string.Join(", ", new string?[]
				{
			a.Name, a.Location, a.DoorNo, a.Address,
			a.State > 0 ? a.State.ToString() : null,
			a.Pincode,
			a.Country > 0 ? a.Country.ToString() : null,
			a.PhoneNumber
				}.Where(s => !string.IsNullOrWhiteSpace(s))),
				Selected = (vm.SelectedAddressId == a.Id)
			}).ToList();

			/// -------- Load pincode FOR THE SELECTED ADDRESS only --------
			if (vm.SelectedAddressId.HasValue)
			{
				var selectedAddress = vm.Addresses
					.FirstOrDefault(a => a.Id == vm.SelectedAddressId.Value);

				var pin = selectedAddress?.Pincode?.Trim();

				if (!string.IsNullOrEmpty(pin))
				{
					try
					{
						// call your existing GET /AdminAPI/pincodes/{code}
						//var pinResp = await _httpClient.GetAsync($"AdminAPI/pincodes/{pin}");
						var client = AuthorizedClient ?? _httpClient;   // be consistent with cart/products
						var pinResp = await client.GetAsync($"AdminAPI/pincodes/{pin}");

						//if (!pinResp.IsSuccessStatusCode)
						//{
						//	ModelState.AddModelError("", "Invalid username or password.");
						//	return View();
						//}
						if (pinResp.IsSuccessStatusCode)
						{
							var rate = await pinResp.Content.ReadFromJsonAsync<Pincode>();

							if (rate != null)
							{
								vm.Pincodes.Add(rate);                    // so JS still sees it
								vm.SelectedStandard = rate.StandardDeliveryAmount;
								vm.SelectedExpress  = rate.ExpressDeliveryAmount;
								vm.Shipping         = vm.SelectedStandard; // default
							}
						}
						else
						{
							_logger.LogWarning("Pincode {Pin} not found. Status {Status}",
											   pin, pinResp.StatusCode);
						}

					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Failed to load pincode {Pin} from AdminAPI.", pin);
					}
				}
			}
			return View(vm);
		}
        [HttpGet]
        public async Task<IActionResult> Confirmation(int orderId)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

            var resp = await _httpClient.GetAsync($"AdminAPI/orders/full/{orderId}");

            if (!resp.IsSuccessStatusCode)
                return NotFound("Order not found");

            var json = await resp.Content.ReadAsStringAsync();

            var summary = JsonSerializer.Deserialize<OrderSummaryViewModel>(json, new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            });

            // --- Ensure order belongs to logged-in user ---
            if (summary == null || summary.Order.UserId != userId)
                return Unauthorized("Order does not belong to this user");

            // --- Fetch user email ---
            var userResp = await _httpClient.GetAsync($"AdminAPI/user/{userId}");
            var user = await userResp.Content.ReadFromJsonAsync<RegisterUser>();

            var vm = new OrderConfirmationViewModel
            {
                Order = summary.Order,
                Details = summary.Details,
                Email = user?.Email ?? "--"
            };

            return View(vm);
        }


        //public IActionResult Confirmation()
        //{
        //    return View();
        //}
        public IActionResult Privacy()
		{
			return View();
		}
        public async Task<IActionResult> TrackOrder(int? orderId)
        {
            if (!orderId.HasValue)
            {
                TempData["ErrorMessage"] = "Please select an order to track.";
                return RedirectToAction("MyOrders");
            }

            var client = _httpClient;
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            // ✅ Fetch order + details (same logic as AdminOrderDetails)
            var orderResponse = await client.GetAsync($"AdminAPI/orders/full/{orderId.Value}");
            if (!orderResponse.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction("MyOrders");
            }

            var orderSummary = await orderResponse.Content.ReadFromJsonAsync<OrderSummaryViewModel>(jsonOptions);
            if (orderSummary == null || orderSummary.Order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction("MyOrders");
            }

            var order = orderSummary.Order;
            var orderDetails = orderSummary.Details ?? new List<OrderDetail>();

            // ✅ Fetch product info for each detail
            var products = new List<Product>();
            foreach (var detail in orderDetails)
            {
                if (detail.ProductId.HasValue)
                {
                    var prodResponse = await client.GetAsync($"AdminAPI/product/{detail.ProductId.Value}");
                    if (prodResponse.IsSuccessStatusCode)
                    {
                        var product = await prodResponse.Content.ReadFromJsonAsync<Product>(jsonOptions);
                        if (product != null && !products.Any(p => p.Id == product.Id))
                        {
                            products.Add(product);
                        }
                    }
                }
            }

            var model = new TrackOrderViewModel
            {
                Order = order,
                UserRole = User.FindFirstValue(ClaimTypes.Role) ?? "User",
                OrderDetails = orderDetails,
                Products = products
            };

            return View(model);
        }
        //public async Task<IActionResult> TrackOrder(int? orderId)
        //{
        //    if (!orderId.HasValue)
        //    {
        //        TempData["ErrorMessage"] = "Please select an order to track.";
        //        return RedirectToAction("MyOrders");
        //    }

        //    var client = _httpClient;
        //    var jsonOptions = new JsonSerializerOptions
        //    {
        //        PropertyNameCaseInsensitive = true,
        //        Converters = { new JsonStringEnumConverter() }
        //    };

        //    // Fetch the order
        //    var orderResponse = await client.GetAsync($"AdminAPI/orders/{orderId.Value}");
        //    if (!orderResponse.IsSuccessStatusCode)
        //    {
        //        TempData["ErrorMessage"] = "Order not found.";
        //        return RedirectToAction("MyOrders");
        //    }

        //    var order = await orderResponse.Content.ReadFromJsonAsync<Order>(jsonOptions);

        //    if (order == null)
        //    {
        //        TempData["ErrorMessage"] = "Order not found.";
        //        return RedirectToAction("MyOrders");
        //    }

        //    // Fetch order details
        //    var detailsResponse = await client.GetAsync($"AdminAPI/orderdetails?orderId={order.Id}");
        //    var orderDetails = detailsResponse.IsSuccessStatusCode
        //        ? await detailsResponse.Content.ReadFromJsonAsync<List<OrderDetail>>(jsonOptions) ?? new List<OrderDetail>()
        //        : new List<OrderDetail>();

        //    // Fetch product info
        //    var products = new List<Product>();
        //    foreach (var detail in orderDetails)
        //    {
        //        if (detail.ProductId.HasValue)
        //        {
        //            var prodResponse = await client.GetAsync($"AdminAPI/product/{detail.ProductId.Value}");
        //            if (prodResponse.IsSuccessStatusCode)
        //            {
        //                var product = await prodResponse.Content.ReadFromJsonAsync<Product>(jsonOptions);
        //                if (product != null)
        //                    products.Add(product);
        //            }
        //        }
        //    }

        //    var model = new TrackOrderViewModel
        //    {
        //        Order = order,
        //        UserRole = User.FindFirstValue(ClaimTypes.Role) ?? "User",
        //        OrderDetails = orderDetails,
        //        Products = products
        //    };

        //    return View(model);
        //}
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["OrderMessage"] = "Please login to view your orders.";
                return RedirectToAction("Index");
            }

            var jsonOptions = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            // Fetch orders for logged-in user
            var orderResponse = await _httpClient.GetAsync($"AdminAPI/user/orders/{userId}");
            List<Order> orders = new();
            if (orderResponse.IsSuccessStatusCode)
                orders = await orderResponse.Content.ReadFromJsonAsync<List<Order>>(jsonOptions);

            // Fetch order details
            var orderDetailsResponse = await _httpClient.GetAsync($"AdminAPI/orderdetails/user/{userId}");
            var orderDetails = orderDetailsResponse.IsSuccessStatusCode
                ? await orderDetailsResponse.Content.ReadFromJsonAsync<List<OrderDetail>>(jsonOptions)
                : new List<OrderDetail>();

            // Fetch products
            var productResponse = await _httpClient.GetAsync("AdminAPI/products");
            var products = productResponse.IsSuccessStatusCode
                ? await productResponse.Content.ReadFromJsonAsync<List<Product>>(jsonOptions)
                : new List<Product>();

            // Fetch all feedbacks 
            var feedbackResponse = await _httpClient.GetAsync("AdminAPI/feedbacks");
            List<FeedbackViewModel> feedbacks = new();
            if (feedbackResponse.IsSuccessStatusCode)
                feedbacks = await feedbackResponse.Content.ReadFromJsonAsync<List<FeedbackViewModel>>(jsonOptions);

            // Normalize userId for comparison
            var normalizedUserId = userId.Trim().ToLowerInvariant();

            // Filter feedbacks by user (case-insensitive)
            var userFeedbacks = feedbacks
                .Where(f => !string.IsNullOrWhiteSpace(f.CustomerID)
                            && f.CustomerID.Trim().ToLowerInvariant() == normalizedUserId)
                .ToList();

            // Build dictionary keyed by OrderID
            var feedbackByOrder = userFeedbacks.ToDictionary(f => f.OrderID, f => f);
			//  Fetch return requests for this user using AuthorizedClient
			var returnUrl = $"AdminAPI/ReturnRequest/my?userId={Uri.EscapeDataString(userId)}";

			var returnRequests = await AuthorizedClient.GetFromJsonAsync<List<ReturnRequestViewDto>>(returnUrl, jsonOptions)?? new List<ReturnRequestViewDto>();
			var returnedOrderIds = returnRequests.Select(r => r.OrderId).Distinct().ToList();
			var model = new OrderListViewModel
            {
                UserRole = "User",
                Orders = orders
            };

            ViewBag.OrderDetails = orderDetails;
            ViewBag.Products = products;
            ViewBag.UserId = userId;
            ViewBag.FeedbackByOrder = feedbackByOrder;
			ViewBag.ReturnedOrderIds = returnedOrderIds;  
			return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback([FromBody] SubmitFeedbackRequest req, CancellationToken ct)
        {
            if (req is null) return BadRequest("Invalid payload.");
            if (req.Rating < 1 || req.Rating > 5) return BadRequest("Rating must be 1..5.");

            if (req.OrderId <= 0) req.OrderId = 1; // fallback so API doesn’t get 0

            var apiRes = await _httpClient.PostAsJsonAsync("AdminAPI/submitfeedback", req, ct);
            var payload = await apiRes.Content.ReadAsStringAsync(ct);

            return new ContentResult
            {
                Content = payload,
                ContentType = "application/json",
                StatusCode = (int)apiRes.StatusCode
            };
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["OrderError"] = "Please login to cancel orders.";
                return RedirectToAction("Index");
            }

            var payload = new UpdateOrderStatusRequest
            {
                OrderId = orderId,
                Status = OrderStatus.Cancelled
            };

            // Call the API endpoint internally using _httpClient (just like MyOrders)
            var jsonOptions = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            var res = await _httpClient.PostAsJsonAsync("AdminAPI/updateStatus", payload);
            if (res.IsSuccessStatusCode)
            {
                TempData["OrderMessage"] = "Order cancelled successfully.";
            }
            else
            {
                var errMsg = await res.Content.ReadAsStringAsync();
                TempData["OrderError"] = "Failed to cancel order: " + errMsg;
            }

            return RedirectToAction("MyOrders");
        }


		// GET: /Home/MyReturns (uses API to get user's returns)
		// Show user's return requests
		[HttpGet]
		public async Task<IActionResult> MyReturns()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return RedirectToAction("SignIn", "Authentication");
			}

			// 🔥 FIX: enable enum string conversion ONLY here
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
			options.Converters.Add(new JsonStringEnumConverter());

			var url = $"AdminAPI/ReturnRequest/my?userId={Uri.EscapeDataString(userId)}";

			// 🔥 FIX APPLIED ONLY TO THIS CALL
			var list = await AuthorizedClient
				.GetFromJsonAsync<List<ReturnRequestViewDto>>(url, options)
				?? new List<ReturnRequestViewDto>();

			return View(list);
		}

		[HttpPost]
		public async Task<IActionResult> ToggleWishlist(int productId)
		{
			var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			if (string.IsNullOrEmpty(userId))
			{
				TempData["WishlistMessage"] = "Please login to modify wishlist.";
				return RedirectToAction("Index");
			}

			try
			{
				// 1?? Get current wishlist
				var wishlistResponse = await _httpClient.GetAsync($"AdminAPI/wishlist/{userId}");
				var wishlistItems = wishlistResponse.IsSuccessStatusCode
					? await wishlistResponse.Content.ReadFromJsonAsync<List<WishlistItem>>()
					: new List<WishlistItem>();

				// 2?? Check if product already in wishlist
				var existingItem = wishlistItems.FirstOrDefault(x => x.ProductId == productId);

				if (existingItem != null)
				{
					// ? Remove from wishlist
					var deleteResponse = await _httpClient.DeleteAsync($"AdminAPI/wishlist/{existingItem.Id}");
					if (deleteResponse.IsSuccessStatusCode)
						TempData["WishlistMessage"] = "Product removed from wishlist.";
					else
						TempData["WishlistMessage"] = "Failed to remove product from wishlist.";
				}
				else
				{
					// ? Add to wishlist
					var postData = new { UserId = userId, ProductId = productId };
					var postResponse = await _httpClient.PostAsJsonAsync("AdminAPI/wishlist", postData);
					if (postResponse.IsSuccessStatusCode)
						TempData["WishlistMessage"] = "Product added to wishlist.";
					else
						TempData["WishlistMessage"] = "Failed to add product to wishlist.";
				}
			}
			catch (Exception ex)
			{
				TempData["WishlistMessage"] = $"Unexpected error: {ex.Message}";
			}

			return RedirectToAction("Index");
		}


		public async Task<IActionResult> Wishlist()
		{
			var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userId))
				return RedirectToAction("Login", "Account");

			var client = AuthorizedClient;

			var wishlistResponse = await client.GetAsync($"AdminAPI/wishlist/{userId}");
			var wishlistItems = wishlistResponse.IsSuccessStatusCode
				? await wishlistResponse.Content.ReadFromJsonAsync<List<WishlistItem>>()
				: new List<WishlistItem>();

			var productsResponse = await client.GetAsync("AdminAPI/products");
			var products = productsResponse.IsSuccessStatusCode
				? await productsResponse.Content.ReadFromJsonAsync<List<Product>>()
				: new List<Product>();

			var wishlistProducts = from wish in wishlistItems
								   join prod in products on wish.ProductId equals prod.Id
								   select prod;

			return View(wishlistProducts.ToList());
		}

        [HttpGet("Home/Invoice/{orderId:int}")]
        public async Task<IActionResult> Invoice(int orderId)
        {
            var response = await _httpClient.GetAsync($"AdminAPI/orders/{orderId}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<InvoiceOrderSummaryDto>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (dto == null || dto.Order == null)
                return NotFound();

            // Map order
            var order = new Order
            {
                Id = dto.Order.Id,
                UserId = dto.Order.UserId,
                AddressId = dto.Order.AddressId,
                DoorNo = dto.Order.DoorNo,
                PhoneNo = dto.Order.PhoneNo,
                Address = dto.Order.Address,
                State = dto.Order.State,
                PinCode = dto.Order.PinCode,
                Country = dto.Order.Country,
                OrderDate = dto.Order.OrderDate,
                Status = ParseOrderStatus(dto.Order.Status),
                Subtotal = dto.Order.Subtotal,
                Tax = dto.Order.Tax,
                Shipping = dto.Order.Shipping,
                TotalDiscount = dto.Order.TotalDiscount,
                Total = dto.Order.Total
            };

            // Map details (base fields)
            var details = dto.Details?.Select(d => new OrderDetail
            {
                Id = d.Id,
                OrderId = d.OrderId,
                ProductId = d.ProductId,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                productName = d.productName
            }).ToList() ?? new List<OrderDetail>();

            // Fetch list of countries
            var countriesResponse = await _httpClient.GetAsync("AdminAPI/countries");
            List<Country> countries = new();
            if (countriesResponse.IsSuccessStatusCode)
            {
                countries = await countriesResponse.Content.ReadFromJsonAsync<List<Country>>()
                            ?? new List<Country>();
            }

            // Fetch list of states
            var statesResponse = await _httpClient.GetAsync("AdminAPI/states");
            List<State> states = new();
            if (statesResponse.IsSuccessStatusCode)
            {
                states = await statesResponse.Content.ReadFromJsonAsync<List<State>>()
                         ?? new List<State>();
            }

            // Fetch user (for FullName)
            string fullName;
            var userResponse = await _httpClient.GetAsync($"AdminAPI/user/{order.UserId}");
            if (userResponse.IsSuccessStatusCode)
            {
                var user = await userResponse.Content.ReadFromJsonAsync<RegisterUser>();
                if (user != null)
                {
                    fullName = $"{user.FirstName} {user.LastName}".Trim();
                }
                else
                {
                    fullName = "Unknown User";
                }
            }
            else
            {
                fullName = "Unknown User";
            }

            // Resolve country/state names from order.Country & order.State
            string countryName = countries
                .FirstOrDefault(c => c.Id == order.Country)?.CountryName
                ?? "Unknown Country";

            string stateName = states
                .FirstOrDefault(s => s.Id == order.State)?.StateName
                ?? "Unknown State";

            // For each detail: set FullName / CountryName / StateName and productName
            foreach (var item in details)
            {
                // Set the three NotMapped fields
                item.FullName = fullName;
                item.CountryName = countryName;
                item.StateName = stateName;

                // Fetch product name
                var productResponse = await _httpClient.GetAsync($"AdminAPI/product/{item.ProductId}");
                if (productResponse.IsSuccessStatusCode)
                {
                    var product = await productResponse.Content.ReadFromJsonAsync<Product>();
                    if (product != null)
                    {
                        item.productName = product.ProductName;
                    }
                }
            }

            var vm = new OrderSummaryViewModel
            {
                Order = order,
                Details = details
            };

            return View(vm);
        }


      
        private OrderStatus ParseOrderStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return OrderStatus.OrderPlaced; // or whatever default you want

            // try enum name first (case-insensitive)
            if (Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed))
                return parsed;

            // try if API sent number as string, like "1"
            if (int.TryParse(status, out var number) && Enum.IsDefined(typeof(OrderStatus), number))
                return (OrderStatus)number;

            // final fallback
            return OrderStatus.OrderPlaced;
        }


        //public async Task<IActionResult> Payment(int orderId)
        //{
        //    var resp = await _httpClient.GetAsync($"AdminAPI/orders/{orderId}");
        //    if (!resp.IsSuccessStatusCode) return NotFound();

        //    var order = await resp.Content.ReadFromJsonAsync<Order>();
        //    if (order == null) return NotFound();

        //    var amountRupees = order.Total;
        //    var amountPaise = (int)Math.Round(amountRupees * 100, MidpointRounding.AwayFromZero);

        //    var vm = new PaymentViewModel
        //    {
        //        OrderId = order.Id,
        //        CustomerId = order.UserId,   // ← your customerId
        //        AmountRupees = amountRupees,
        //        AmountPaise = amountPaise
        //    };

        //    return View(vm);
        //}
        public async Task<IActionResult> MyConsultation()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (string.IsNullOrEmpty(userId))
			{
				TempData["ConsultationMessage"] = "Please login to view consultations.";
				return RedirectToAction("Index");
			}

			var jsonOptions = new JsonSerializerOptions
			{
				Converters = { new JsonStringEnumConverter() }
			};

			// ✅ Get consultations created by this user (patient)
			var consultationResponse = await _httpClient.GetAsync($"AdminAPI/consultationbooking/user/{userId}");
			List<ConsultationBooking> consultations = new List<ConsultationBooking>();
			if (consultationResponse.IsSuccessStatusCode)
				consultations = await consultationResponse.Content.ReadFromJsonAsync<List<ConsultationBooking>>(jsonOptions);

			// ✅ Get all doctors
			var doctorResponse = await _httpClient.GetAsync("AdminAPI/users");
			List<RegisterUser> doctors = new List<RegisterUser>();
			if (doctorResponse.IsSuccessStatusCode)
				doctors = await doctorResponse.Content.ReadFromJsonAsync<List<RegisterUser>>(jsonOptions);

			// ✅ Get doctor details
			var doctorDetailResponse = await _httpClient.GetAsync("AdminAPI/doctordetails");
			List<DoctorDetail> doctorDetails = new List<DoctorDetail>();
			if (doctorDetailResponse.IsSuccessStatusCode)
				doctorDetails = await doctorDetailResponse.Content.ReadFromJsonAsync<List<DoctorDetail>>(jsonOptions);

			// ✅ Build ViewModel
			var model = new MyConsultationViewModel();

			foreach (var c in consultations)
			{
				var doctor = doctors.FirstOrDefault(d => d.Id == c.PreferredDoctorId);
				var detail = doctorDetails.FirstOrDefault(dd => dd.DoctorId == c.PreferredDoctorId);

				model.Consultations.Add(new ConsultationWithDoctorViewModel
				{
					Consultation = c,
					Doctor = doctor,
					DoctorDetail = detail
				});
			}

			return View(model);
		}

		[HttpGet]
		public async Task<IActionResult> Consultation(DateOnly? date = null)
		{
			var doctorsResponse = await _httpClient.GetAsync("AdminAPI/users/Doctor");
			var doctorDetailsResponse = await _httpClient.GetAsync("AdminAPI/doctordetails");
			var specialitiesResponse = await _httpClient.GetAsync("AdminAPI/doctorspecialities");

			var doctors = doctorsResponse.IsSuccessStatusCode ?
				await doctorsResponse.Content.ReadFromJsonAsync<List<RegisterUser>>() : new List<RegisterUser>();

			var doctorDetails = doctorDetailsResponse.IsSuccessStatusCode ?
				await doctorDetailsResponse.Content.ReadFromJsonAsync<List<DoctorDetail>>() : new List<DoctorDetail>();

			var specialities = specialitiesResponse.IsSuccessStatusCode ?
				await specialitiesResponse.Content.ReadFromJsonAsync<List<DoctorSpeciality>>() : new List<DoctorSpeciality>();

			// Convert CSV speciality IDs
			foreach (var detail in doctorDetails)
			{
				if (!string.IsNullOrEmpty(detail.SpecalityIds))
				{
					detail.SelectedSpecialityIds = detail.SpecalityIds
						.Split(',', StringSplitOptions.RemoveEmptyEntries)
						.Select(int.Parse)
						.ToList();
				}
			}

			// Only working doctors
			doctorDetails = doctorDetails.Where(d => d.IsWorking).ToList();

			// Determine available doctors based on the selected date
			List<string> availableDoctorIds;

			if (date.HasValue)
			{
				string dayName = date.Value.DayOfWeek.ToString(); // e.g. Monday, Tuesday
				availableDoctorIds = doctorDetails
					.Where(d => IsDoctorAvailableOnDay(d, dayName))
					.Select(d => d.DoctorId)
					.Distinct()
					.ToList();
			}
			else
			{
				// Default: show all working doctors
				availableDoctorIds = doctorDetails.Select(d => d.DoctorId).Distinct().ToList();
			}

			var filteredDoctors = doctors.Where(d => availableDoctorIds.Contains(d.Id)).ToList();

			var viewModel = new ConsultationPageViewModel
			{
				BookingModel = new ConsultationBookingViewModel
				{
					PreferredDate = date ?? DateOnly.FromDateTime(DateTime.Today)
				},
				DoctorDetailsModel = new DoctorDetailViewModel
				{
					Doctors = filteredDoctors,
					DoctorDetailList = doctorDetails,
					Specialities = specialities
				}
			};

			// ✅ Populate dropdown
			ViewBag.DoctorList = new SelectList(
				filteredDoctors.Select(d => new
				{
					Id = d.Id,
					Name = d.FullName ?? $"{d.FirstName} {d.LastName}"
				}),
				"Id",
				"Name"
			);

			return View(viewModel);
		}

		// helper function
		private bool IsDoctorAvailableOnDay(DoctorDetail detail, string day)
		{
			return day switch
			{
				"Monday" => detail.MondayStartTime.HasValue && detail.MondayEndTime.HasValue,
				"Tuesday" => detail.TuesdayStartTime.HasValue && detail.TuesdayEndTime.HasValue,
				"Wednesday" => detail.WednesdayStartTime.HasValue && detail.WednesdayEndTime.HasValue,
				"Thursday" => detail.ThursdayStartTime.HasValue && detail.ThursdayEndTime.HasValue,
				"Friday" => detail.FridayStartTime.HasValue && detail.FridayEndTime.HasValue,
				"Saturday" => detail.SaturdayStartTime.HasValue && detail.SaturdayEndTime.HasValue,
				"Sunday" => detail.SundayStartTime.HasValue && detail.SundayEndTime.HasValue,
				_ => false
			};
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Consultation(ConsultationPageViewModel model)
		{
			// explicitly clear validation for DoctorDetailsModel
			ModelState.ClearValidationState(nameof(model.DoctorDetailsModel));

			var booking = model.BookingModel;

			if (!TryValidateModel(booking, nameof(model.BookingModel)))
			{
				// reload doctor list for view
				var doctorsResponse = await _httpClient.GetAsync("AdminAPI/users/Doctor");
				var doctors = doctorsResponse.IsSuccessStatusCode
					? await doctorsResponse.Content.ReadFromJsonAsync<List<RegisterUser>>()
					: new List<RegisterUser>();

				ViewBag.DoctorList = new SelectList(
					doctors.Select(d => new
					{
						Id = d.Id,
						Name = d.FullName ?? $"{d.FirstName} {d.LastName}"
					}),
					"Id",
					"Name"
				);

				return View(model);
			}

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				TempData["ConsultationMessage"] = "Please login to book a consultation.";
				return RedirectToAction("Index");
			}

			var bookingEntity = new ConsultationBooking
			{
				FirstName = booking.FirstName,
				LastName = booking.LastName,
				Email = booking.Email,
				Phone = booking.Phone,
				ConsultationType = (ConsultationType)booking.ConsultationType,
				PreferredDoctorId = booking.PreferredDoctorId,
				PreferredTimeSlot = (TimeSlot)booking.PreferredTimeSlot,
				PreferredDate = booking.PreferredDate,
				Concerns = booking.Concerns,
				Medications = booking.Medications,
				CreatedBy = userId,
				SubmittedAt = DateTime.Now
			};

			var response = await _httpClient.PostAsJsonAsync("AdminAPI/consultationbooking", bookingEntity);

			if (response.IsSuccessStatusCode)
			{
				TempData["ConsultationMessage"] = "Consultation booked successfully.";
				return RedirectToAction("MyConsultation");
			}

			var errorMsg = await response.Content.ReadAsStringAsync();
			ModelState.AddModelError(string.Empty, "Failed to book consultation: " + errorMsg);

			return View(model);
		}

		[HttpGet]
		public async Task<IActionResult> GetAvailableDoctors(DateOnly date)
		{
			var doctorsResponse = await _httpClient.GetAsync("AdminAPI/users/Doctor");
			var doctorDetailsResponse = await _httpClient.GetAsync("AdminAPI/doctordetails");

			var doctors = doctorsResponse.IsSuccessStatusCode
				? await doctorsResponse.Content.ReadFromJsonAsync<List<RegisterUser>>()
				: new List<RegisterUser>();

			var doctorDetails = doctorDetailsResponse.IsSuccessStatusCode
				? await doctorDetailsResponse.Content.ReadFromJsonAsync<List<DoctorDetail>>()
				: new List<DoctorDetail>();

			// Convert CSV speciality IDs
			foreach (var detail in doctorDetails)
			{
				if (!string.IsNullOrEmpty(detail.SpecalityIds))
				{
					detail.SelectedSpecialityIds = detail.SpecalityIds
						.Split(',', StringSplitOptions.RemoveEmptyEntries)
						.Select(int.Parse)
						.ToList();
				}
			}

			doctorDetails = doctorDetails.Where(d => d.IsWorking).ToList();

			string dayName = date.DayOfWeek.ToString();
			var availableDoctorIds = doctorDetails
				.Where(d => IsDoctorAvailableOnDay(d, dayName))
				.Select(d => d.DoctorId)
				.Distinct()
				.ToList();

			var filteredDoctors = doctors
				.Where(d => availableDoctorIds.Contains(d.Id))
				.Select(d => new
				{
					id = d.Id,
					name = d.FullName ?? $"{d.FirstName} {d.LastName}"
				})
				.ToList();

			return Json(filteredDoctors);
		}


		//        [HttpPost]
		//        [ValidateAntiForgeryToken]
		//        public async Task<IActionResult> Subscribe(string email)
		//        {
		//            var fromAddress = _emailSettings.FromAddress;
		//            var fromPassword = _emailSettings.Password;
		//            var smtpHost = _emailSettings.Host;
		//            var smtpPort = _emailSettings.Port;
		//            var useSsl = _emailSettings.UseSSL;

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Subscribe([FromForm] string email)
		{
			if (string.IsNullOrWhiteSpace(email) || !MailboxAddress.TryParse(email, out var parsed))
			{
				TempData["Message"] = "Invalid email address.";
				return RedirectToAction("Index");
			}

			var payload = new { Email = parsed.Address.Trim().ToLowerInvariant() };
			var apiResponse = await AuthorizedClient.PostAsJsonAsync("AdminAPI/newsletter/subscription", payload);

			if (!apiResponse.IsSuccessStatusCode)
			{
				var err = await apiResponse.Content.ReadAsStringAsync();
				TempData["Message"] = $"Subscription failed: {err}";
				return RedirectToAction("Index");
			}

			var dto = await apiResponse.Content.ReadFromJsonAsync<NewsletterSubscriptionResult>();
			if (dto is null || !dto.Succeeded)
			{
				TempData["Message"] = "Subscription failed: unexpected response.";
				return RedirectToAction("Index");
			}

			try
			{
				using var logger = new ProtocolLogger("smtp.log");
				using var smtp = new SmtpClient(logger);

				smtp.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;

				var host = _emailSettings.Host;
				var port = _emailSettings.Port;
				var secure = port == 465
					? SecureSocketOptions.SslOnConnect
					: port == 587 ? SecureSocketOptions.StartTls
					: SecureSocketOptions.Auto;

				await smtp.ConnectAsync(host, port, secure);

				smtp.AuthenticationMechanisms.Remove("XOAUTH2");

				if (!string.IsNullOrWhiteSpace(_emailSettings.Password))
				{
					await smtp.AuthenticateAsync(_emailSettings.FromAddress, _emailSettings.Password);
				}

				var adminMsg = new MimeMessage();
				adminMsg.From.Add(MailboxAddress.Parse(_emailSettings.FromAddress));
				adminMsg.To.Add(MailboxAddress.Parse(_emailSettings.FromAddress));
				adminMsg.Subject = dto.AdminSubject;
				adminMsg.Body = new TextPart("plain") { Text = dto.AdminBodyText };

				var userMsg = new MimeMessage();
				userMsg.From.Add(MailboxAddress.Parse(_emailSettings.FromAddress));
				userMsg.To.Add(MailboxAddress.Parse(dto.Email));
				userMsg.Subject = dto.UserSubject;
				userMsg.Body = new BodyBuilder
				{
					TextBody = "Thank you for subscribing to Nura Herbex!",
					HtmlBody = dto.UserBodyHtml
				}.ToMessageBody();

				// 5) Send emails
				await smtp.SendAsync(userMsg);
				await smtp.SendAsync(adminMsg);
				await smtp.DisconnectAsync(true);

				TempData["Message"] = "Thank you for subscribing! Please check your inbox.";
			}
			catch (SmtpCommandException ex)
			{
				TempData["Message"] = $"Email send failed ({ex.StatusCode}): {ex.Message}";
			}
			catch (SmtpProtocolException ex)
			{
				TempData["Message"] = $"Email send failed (protocol): {ex.Message}";
			}
			catch (SslHandshakeException ex)
			{
				TempData["Message"] =
					$"TLS handshake failed: {ex.Message}. Make sure the SMTP certificate includes '{_emailSettings.Host}'.";
			}
			catch (Exception ex)
			{
				TempData["Message"] = $"Saved successfully, but sending email failed: {ex.Message}";
			}

			return RedirectToAction("Index");
		}
		[AllowAnonymous]
		public async Task<IActionResult> _ShoppingCartPartial()
		{
			var vm = await BuildCartViewModelAsync();
			return PartialView("_ShoppingCartPartial", vm);
		}


		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
		[HttpGet]
		public async Task<IActionResult> MyProfile(int id = 0)
		{
			var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
			if (string.IsNullOrEmpty(userId))
				return RedirectToAction("SignIn", "Authentication");

			var vm = new UserProfileViewModel
			{
				Id = userId
			};

			// 1️⃣ Get user details
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

			// 2️⃣ Addresses
			var addressesResponse = await _httpClient.GetAsync($"AdminAPI/addresses/{userId}");
			vm.Addresses = addressesResponse.IsSuccessStatusCode
				? await addressesResponse.Content.ReadFromJsonAsync<List<AddressDetail>>()
				: new List<AddressDetail>();

			// 3️⃣ Countries
			var countriesResponse = await _httpClient.GetAsync("AdminAPI/countries");
			vm.Countries = countriesResponse.IsSuccessStatusCode
				? await countriesResponse.Content.ReadFromJsonAsync<List<Country>>()
				: new List<Country>();

			// 4️⃣ States
			var statesResponse = await _httpClient.GetAsync("AdminAPI/states");
			vm.States = statesResponse.IsSuccessStatusCode
				? await statesResponse.Content.ReadFromJsonAsync<List<State>>()
				: new List<State>();

			// 5️⃣ Selected address (for add/edit popup)
			vm.AddressDetail = new AddressDetail { UserId = userId };
			if (id > 0)
			{
				var addressResponse = await _httpClient.GetAsync($"AdminAPI/address/{id}");
				if (addressResponse.IsSuccessStatusCode)
					vm.AddressDetail = await addressResponse.Content.ReadFromJsonAsync<AddressDetail>();
			}

			return View(vm);
		}



		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> MyProfile(UserProfileViewModel model)
		{
			if (!ModelState.IsValid)
			{
				var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

				var addressesResponse = await _httpClient.GetAsync($"AdminAPI/addresses/{userId}");
				var addresses = addressesResponse.IsSuccessStatusCode
					? await addressesResponse.Content.ReadFromJsonAsync<List<AddressDetail>>()
					: new List<AddressDetail>();

				var countriesResponse = await _httpClient.GetAsync("AdminAPI/countries");
				var countries = countriesResponse.IsSuccessStatusCode
					? await countriesResponse.Content.ReadFromJsonAsync<List<Country>>()
					: new List<Country>();

				List<State> states = new List<State>();

				if (countries.Count > 0)
				{
					var statesResponse = await _httpClient.GetAsync($"AdminAPI/states/{countries.First().Id}");
					if (statesResponse.IsSuccessStatusCode)
						states = await statesResponse.Content.ReadFromJsonAsync<List<State>>();
				}

				model.Addresses = addresses ?? new List<AddressDetail>();
				model.Countries = countries ?? new List<Country>();
				model.States = states ?? new List<State>();

				return View(model);
			}

			var postResponse = await _httpClient.PostAsJsonAsync("AdminAPI/address", model.AddressDetail);
			if (postResponse.IsSuccessStatusCode)
			{
				TempData["Success"] = model.AddressDetail.Id > 0 ? "Address updated successfully" : "Address added successfully";
				return RedirectToAction(nameof(MyProfile));
			}

			TempData["Error"] = "Failed to save address";
			// reload dropdown data after failure
			return await MyProfile(model.AddressDetail.Id);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateProfile(UserProfileViewModel model)
		{
			var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
			if (string.IsNullOrEmpty(userId))
				return RedirectToAction("SignIn", "Authentication");

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
				TempData["Success"] = "Profile updated successfully";
				return RedirectToAction(nameof(MyProfile));
			}

			var errorBody = await response.Content.ReadAsStringAsync();
			ModelState.AddModelError(string.Empty, errorBody);

			// re-load addresses / dropdown data (same as GET MyProfile)
			var addressesResponse = await _httpClient.GetAsync($"AdminAPI/addresses/{userId}");
			model.Addresses = addressesResponse.IsSuccessStatusCode
				? await addressesResponse.Content.ReadFromJsonAsync<List<AddressDetail>>()
				: new List<AddressDetail>();

			var countriesResponse = await _httpClient.GetAsync("AdminAPI/countries");
			model.Countries = countriesResponse.IsSuccessStatusCode
				? await countriesResponse.Content.ReadFromJsonAsync<List<Country>>()
        : new List<Country>();

			var statesResponse = await _httpClient.GetAsync("AdminAPI/states");
			model.States = statesResponse.IsSuccessStatusCode
				? await statesResponse.Content.ReadFromJsonAsync<List<State>>()
				: new List<State>();

			return View("MyProfile", model);
		}



		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteAddress(int id)
		{
			var response = await _httpClient.DeleteAsync($"AdminAPI/address/{id}");
			if (response.IsSuccessStatusCode)
				TempData["Success"] = "Address deleted successfully";
			else
				TempData["Error"] = "Failed to delete address";

			return RedirectToAction(nameof(MyProfile));
		}
		[HttpPost]
		[IgnoreAntiforgeryToken]
		public async Task<IActionResult> Cartlist(int productId)
		{
			var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			// ✅ If not logged in, return 401 for JS
			if (string.IsNullOrEmpty(userId))
				return Unauthorized(new { message = "Please log in to add to cart." });

			try
			{
				var postData = new { UserId = userId, ProductId = productId, Quantity = 1 };
				var postResponse = await _httpClient.PostAsJsonAsync("AdminAPI/Cart", postData);

				if (postResponse.IsSuccessStatusCode)
					return Ok(new { success = true, message = "Cart updated." });

				var err = await postResponse.Content.ReadAsStringAsync();
				return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(err) ? "Failed to add product to Cart." : err });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = $"Unexpected error: {ex.Message}" });
			}
		}


		//public async Task<IActionResult> Cart()
		//{
		//	var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		//	if (string.IsNullOrEmpty(userId))
		//		return RedirectToAction("SignIn", "Authentication");

		//	var client = AuthorizedClient;

		//	var cartResponse = await client.GetAsync($"AdminAPI/Cart/{userId}");
		//	var cartItems = cartResponse.IsSuccessStatusCode
		//		? await cartResponse.Content.ReadFromJsonAsync<List<CartItem>>()
		//		: new List<CartItem>();

		//	var productsResponse = await client.GetAsync("AdminAPI/products");
		//	var products = productsResponse.IsSuccessStatusCode
		//		? await productsResponse.Content.ReadFromJsonAsync<List<Product>>()
		//		: new List<Product>();

		//	var cartProducts = from wish in cartItems
		//						   join prod in products on wish.ProductId equals prod.Id
		//						   select prod;

		//	return View(cartProducts.ToList());
		//}
		[HttpPost]
		[IgnoreAntiforgeryToken]
		public async Task<IActionResult> ChangeQuantity(int cartId, int delta)
		{
			var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userId))
				return Unauthorized(new { message = "Please login first." });

			try
			{
				var postData = new { CartItemId = cartId, Quantity = delta };
				var response = await _httpClient.PostAsJsonAsync("AdminAPI/Cart/quantity/change", postData);

				if (response.IsSuccessStatusCode)
					return Ok(new { success = true });

				var err = await response.Content.ReadAsStringAsync();
				return BadRequest(new { success = false, message = err });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpDelete]
		[IgnoreAntiforgeryToken]
		public async Task<IActionResult> DeleteCartItem(int cartId)
		{
			var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userId))
				return Unauthorized(new { message = "Please login first." });

			try
			{
				var response = await _httpClient.DeleteAsync($"AdminAPI/Cart/{cartId}");
				if (response.IsSuccessStatusCode)
					return Ok(new { success = true });

				var err = await response.Content.ReadAsStringAsync();
				return BadRequest(new { success = false, message = err });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}
		[HttpGet]
		public async Task<IActionResult> _AddressSummary(int id)
		{
			var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
			if (string.IsNullOrEmpty(userId)) return Unauthorized();

			var addressResponse = await _httpClient.GetAsync($"AdminAPI/address/{id}");
			if (!addressResponse.IsSuccessStatusCode) return BadRequest();

			var addr = await addressResponse.Content.ReadFromJsonAsync<AddressDetail>();
			if (addr is null || addr.UserId != userId) return Forbid();

			return View("OrderSummary", addr);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ProceedToPaymentPost([FromForm] OrderSummaryViewModel input)
		{
			var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();
			// 1) Load cart
			var cart = await BuildCartViewModelAsync();
			var items = cart.Items?.ToList() ?? new();
			if (items.Count == 0)
				return BadRequest("Cart is empty.");
			AddressDetail address;
			// -------------------------------------------------------
			// CASE 1: USER CHOSE A CUSTOM ADDRESS
			// -------------------------------------------------------
			if (input.UseCustomAddress)
			{
                if (string.IsNullOrWhiteSpace(input.StreetAddress) ||
       string.IsNullOrWhiteSpace(input.City) ||
       string.IsNullOrWhiteSpace(input.ZipCode) ||
       string.IsNullOrWhiteSpace(input.Phone))
                {
                    ModelState.AddModelError("", "Please fill all required address fields for custom address.");
                    return await OrderSummary(input.SelectedAddressId);
                }
                //  Keep your original mapping so address API keeps working
                var custom = new AddressDetail
				{
					UserId = userId,
					Name = $"{input.FirstName} {input.LastName}".Trim(),
					DoorNo = input.StreetAddress,        // as you had
					Address = input.StreetAddress,       // as you had
					Location = input.City,
					State = int.TryParse(input.State, out var s) ? s : 1,
					Pincode = input.ZipCode,
					Country = 1,                         // your default
					PhoneNumber = input.Phone,
					IsDefault = false
				};

				var createAddr = await _httpClient.PostAsJsonAsync("AdminAPI/address", custom);

				if (!createAddr.IsSuccessStatusCode)
				{
					var body = await createAddr.Content.ReadAsStringAsync();
					ModelState.AddModelError("", $"Failed to save custom address: {body}");
					return await OrderSummary(null);
				}

				// 🔹 Now re-load addresses for this user
				var resp = await _httpClient.GetAsync($"AdminAPI/addresses/{userId}");
				var addrList = resp.IsSuccessStatusCode
					? await resp.Content.ReadFromJsonAsync<List<AddressDetail>>() ?? new()
					: new List<AddressDetail>();

				// Pick the newest address for this user (by Id or CreatedAt)
				address = addrList.OrderByDescending(a => a.Id).FirstOrDefault();
				if (address == null)
				{
					ModelState.AddModelError("", "Failed to load saved address.");
					return await OrderSummary(null);
				}
				input.SelectedAddressId = address.Id;
			}
			else
			{
				// -------------------------------------------------------
				// CASE 2: EXISTING ADDRESS CHOSEN
				// -------------------------------------------------------
				var resp = await _httpClient.GetAsync($"AdminAPI/addresses/{userId}");
				var addrList = resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<List<AddressDetail>>() ?? new() : new();
				address = addrList.FirstOrDefault(a => a.Id == input.SelectedAddressId);
				if (address == null)
					return BadRequest("Address not found.");
			}

            // -------------------------------------------------------
            // SHIPPING CALCULATION
            // -------------------------------------------------------
            decimal subtotal = items.Sum(i => i.LineTotal);

            Pincode? pin = null;
            if (!string.IsNullOrWhiteSpace(address.Pincode))
            {
                var pinResponse = await _httpClient.GetAsync($"AdminAPI/pincodes/{address.Pincode}");

                if (pinResponse.IsSuccessStatusCode)
                {
                    // 200 OK → we have config for this pincode
                    pin = await pinResponse.Content.ReadFromJsonAsync<Pincode>();
                }
                else
                {
                    // 404 or 400 etc → no pincode config, just treat as "no special shipping"
                    pin = null;
                    // Optionally log:
                    // _logger.LogWarning("No pincode config for {Pin}. Status {StatusCode}", address.Pincode, pinResponse.StatusCode);
                }
            }

            // if pin is null, both amounts default to 0
            decimal shipping = input.DeliveryOption == "express"
                ? (pin?.ExpressDeliveryAmount ?? 0m)
                : (pin?.StandardDeliveryAmount ?? 0m);

            decimal tax = 0m;
            decimal discount = 0m;
            decimal total = subtotal + shipping;


            // -------------------------------------------------------
            // BUILD ORDER PAYLOAD
            // -------------------------------------------------------
            var dto = new CreateOrderDto
			{
				UserId = userId,
				AddressId = address.Id,
				DoorNo = address.DoorNo,
				PhoneNo = address.PhoneNumber,
				Address = string.Join(", ", new[]
				{
			address.DoorNo,
			address.Address,
			address.Location
		}.Where(x => !string.IsNullOrWhiteSpace(x))),
				State = address.State,
				PinCode = address.Pincode,
				Country = address.Country,
				OrderDate = DateTime.Now,
				Status = OrderStatus.OrderPlaced,
				Subtotal = subtotal,
				Tax = tax,
				Shipping = shipping,
				TotalDiscount = discount,
				Total = total,
				Details = items.Select(i => new CreateOrderDetailDto
				{
					ProductId = i.ProductId,
					Quantity = i.Quantity,
					UnitPrice = i.UnitPrice
				}).ToList()
			};

			// -------------------------------------------------------
			// SAVE ORDER (Order + OrderDetails) via AdminAPI
			// -------------------------------------------------------
			var response = await _httpClient.PostAsJsonAsync("AdminAPI/orders", dto);

			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync();
				ModelState.AddModelError("", $"Order creation failed: {body}");
				// Better to re-show summary instead of redirect blindly:
				return await OrderSummary(input.SelectedAddressId);
			}

			var result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
			var orderId = result?["id"] ?? 0;

			return RedirectToAction("Confirmation", "Home", new { orderId });
		}


		[HttpGet]
		public async Task<IActionResult> GetZipInfo(string zip)
		{
			if (string.IsNullOrWhiteSpace(zip))
				return BadRequest("ZIP is required.");

			zip = zip.Trim();

			// ✅ same validation as AdminAPI GetByCode (fix the regex: only ONE backslash)
			if (!Regex.IsMatch(zip, @"^\d{6}$"))
				return BadRequest("PIN must be exactly 6 digits.");

			try
			{
				// ✅ be consistent with OrderSummary / BuildCartViewModel
				var client = AuthorizedClient ?? _httpClient;

				// reuse your existing AdminAPI endpoint
				var resp = await client.GetAsync($"AdminAPI/pincodes/{zip}");

				if (resp.StatusCode == HttpStatusCode.NotFound)
					return NotFound(); // 404 – pincode not configured

				if (!resp.IsSuccessStatusCode)
					return StatusCode((int)resp.StatusCode);

				var pin = await resp.Content.ReadFromJsonAsync<Pincode>();
				if (pin == null)
					return NotFound();

				// Return only what the UI needs
				return Json(new
				{
					code = pin.Code,
					standard = pin.StandardDeliveryAmount,
					express = pin.ExpressDeliveryAmount,
					desc = pin.Description
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load pincode {Zip} via GetZipInfo.", zip);
				return StatusCode(500, "Error while fetching pincode.");
			}
		}

        
        // helper method in HomeController
        private OrderStatus ParseOrderStatuss(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return OrderStatus.OrderPlaced; // or whatever default you want

            // try enum name first (case-insensitive)
            if (Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed))
                return parsed;

            // try if API sent number as string, like "1"
            if (int.TryParse(status, out var number) && Enum.IsDefined(typeof(OrderStatus), number))
                return (OrderStatus)number;

            // final fallback
            return OrderStatus.OrderPlaced;
        }

		// POST: /Home/RequestReturn  (called by your popup form)
		// Called by your popup form
		// Called by your popup form
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RequestReturn(ReturnRequestDto dto)
		{
			
			dto.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			//if (!ModelState.IsValid)
			//{
			//	TempData["ReturnError"] = "Please provide a reason.";
			//	return RedirectToAction("MyOrders", "Home");
			//}
			var response = await AuthorizedClient.PostAsJsonAsync("AdminAPI/ReturnRequest", dto);

			if (response.IsSuccessStatusCode)
			{
				TempData["ReturnSuccess"] = "Return request submitted.";
			}
			else
			{
				var content = await response.Content.ReadAsStringAsync();
				TempData["ReturnError"] = $"Failed to submit return: {content}";
			}

			return RedirectToAction("MyOrders", "Home");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CancelReturn(int id)
		{
			// Call AdminAPI DELETE endpoint
			var response = await AuthorizedClient.DeleteAsync($"AdminAPI/DeleteReturn/{id}");

			if (response.IsSuccessStatusCode)
			{
				TempData["Success"] = "Return request cancelled.";
			}
			else
			{
				TempData["Error"] = "Unable to cancel return request.";
			}

			// Redirect back to whatever page shows returns
			return RedirectToAction("MyOrders", "Home"); 
		}



	}
}
