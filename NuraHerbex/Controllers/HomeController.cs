using Domain.Extensions;
using Domain.Implementation;
using Domain.Models;
using Domain.ViewModel;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;
using NuraHerbex.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Claims;
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

            // Fetch blogs, ingredients, products as before
            var blogResponse = await _httpClient.GetAsync("AdminAPI/blogs");
            if (blogResponse.IsSuccessStatusCode)
                blogs = JsonConvert.DeserializeObject<List<Blog>>(await blogResponse.Content.ReadAsStringAsync()) ?? new List<Blog>();

            var ingredientResponse = await _httpClient.GetAsync("AdminAPI/ingredients");
            if (ingredientResponse.IsSuccessStatusCode)
                ingredients = JsonConvert.DeserializeObject<List<Ingredient>>(await ingredientResponse.Content.ReadAsStringAsync()) ?? new List<Ingredient>();

            var productsResponse = await _httpClient.GetAsync("AdminAPI/products");
            if (productsResponse.IsSuccessStatusCode)
                products = JsonConvert.DeserializeObject<List<Product>>(await productsResponse.Content.ReadAsStringAsync()) ?? new List<Product>();

            // Filter home ingredients, featured products, etc.
            var homeIngredients = ingredients.Where(i => i.IsActive && i.ShowHome).OrderByDescending(i => i.CreatedAt).Take(6).ToList();
            var latestBlogs = blogs.OrderByDescending(b => b.CreatedAt).Take(10).ToList();
            var featuredProducts = products.OrderBy(p => p.Id).ToList();

            // ? Fetch user's wishlist IDs if logged in
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

            var vm = new HomeViewModel
            {
                BlogList = latestBlogs,
                Ingredients = homeIngredients,
                PlanList = await GetPlansFromApi(),
                FeaturedProducts = featuredProducts,
                WishlistProductIds = wishlistIds // pass to view
            };

            return View(vm);
        }

        public IActionResult About()
        {
            return View();
        }
        public async Task<IActionResult> Blog()
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

            var vm = new BlogViewModel
            {
                BlogList = blogs,
                Categories = categories
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

        public async Task<IActionResult> Plan()
        {
            var plans = await GetPlansFromApi();
            return View(plans); 
        }
        public async Task<List<PricingPlan>> GetPlansFromApi()
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

        public async Task<IActionResult> Shop(int id = 0)
        {
            var products = new List<Product>();

            var productsResponse = await _httpClient.GetAsync("AdminAPI/products");
            if (productsResponse.IsSuccessStatusCode)
            {
                var json = await productsResponse.Content.ReadAsStringAsync();
                products = JsonConvert.DeserializeObject<List<Product>>(json) ?? new List<Product>();
            }

            // Optional: pre-select a specific product
            Product? selected = null;
            if (id > 0)
            {
                var oneResponse = await _httpClient.GetAsync($"AdminAPI/product/{id}");
                if (oneResponse.IsSuccessStatusCode)
                {
                    selected = JsonConvert.DeserializeObject<Product>(
                        await oneResponse.Content.ReadAsStringAsync()
                    );
                }
            }

            // ---- helpers to unpack "Heading | Description" ----
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
                NewProduct = selected ?? new Product()
            };

            // For the selected product
            ViewBag.SelectedBenefits = Extract(vm.NewProduct); // List<KeyValuePair<string,string>>

            // For product cards list � handle duplicate IDs safely
            ViewBag.BenefitsByProduct = products
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, g => Extract(g.First()));

            return View(vm);
        }

        public IActionResult Quiz()
        {
            return View();
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
        public IActionResult OrderSummary()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult TrackOrder()
        {
            return View();
        }
        public IActionResult MyOrders()
        {
            return View();
        }
        public IActionResult MyReturns()
        {
            return View();
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

        public IActionResult Invoice()
        {
            return View();
        }
        public IActionResult Payment()
        {
            return View();
        }
        public IActionResult MyConsultation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Consultation()
        {
            var response = await _httpClient.GetAsync("AdminAPI/users/Doctor");

            List<RegisterUser> doctors = new List<RegisterUser>();
            if (response.IsSuccessStatusCode)
            {
                doctors = await response.Content.ReadFromJsonAsync<List<RegisterUser>>();
            }

            var doctorListItems = doctors.Select(d => new SelectListItem
            {
                Value = d.Id,
                Text = d.UserName ?? d.Email
            }).ToList();

            ViewBag.DoctorList = doctorListItems;

            return View(new ConsultationBookingViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Consultation(ConsultationBookingViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ConsultationMessage"] = "Please login to book a consultation.";
                return RedirectToAction("Index");
            }

            var booking = new
            {
                model.FirstName,
                model.LastName,
                model.Email,
                model.Phone,
                model.ConsultationType,
                model.PreferredDoctorId,
                model.PreferredTimeSlot,
                model.Concerns,
                model.Medications,
                CreatedBy = userId,              
                SubmittedAt = DateTime.Now
            };

            var response = await _httpClient.PostAsJsonAsync("AdminAPI/consultationbooking", booking);

            if (response.IsSuccessStatusCode)
            {
                TempData["ConsultationMessage"] = "Consultation booked successfully.";
                return RedirectToAction("MyConsultation");
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, "Failed to book consultation: " + errorMsg);
            return View(model);
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



        public ActionResult _ShoppingCartPartial()
        {
            return PartialView("_ShoppingCartPartial");
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

            var addressesResponse = await _httpClient.GetAsync($"AdminAPI/addresses/{userId}");
            var addresses = addressesResponse.IsSuccessStatusCode
                ? await addressesResponse.Content.ReadFromJsonAsync<List<AddressDetail>>()
                : new List<AddressDetail>();

            var countriesResponse = await _httpClient.GetAsync("AdminAPI/countries");
            var countries = countriesResponse.IsSuccessStatusCode
                ? await countriesResponse.Content.ReadFromJsonAsync<List<Country>>()
                : new List<Country>();

            var statesResponse = await _httpClient.GetAsync("AdminAPI/states");
            var states = statesResponse.IsSuccessStatusCode
                ? await statesResponse.Content.ReadFromJsonAsync<List<State>>()
                : new List<State>();

            // Default empty address
            var selectedAddress = new AddressDetail { UserId = userId };

            if (id > 0)
            {
                var addressResponse = await _httpClient.GetAsync($"AdminAPI/address/{id}");
                if (addressResponse.IsSuccessStatusCode)
                    selectedAddress = await addressResponse.Content.ReadFromJsonAsync<AddressDetail>();
            }

            var vm = new UserProfileViewModel
            {
                AddressDetail = selectedAddress,
                Addresses = addresses,
                Countries = countries,
                States = states
            };

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
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var response = await _httpClient.DeleteAsync($"AdminAPI/address/{id}");
            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Address deleted successfully";
            else
                TempData["Error"] = "Failed to delete address";

            return RedirectToAction(nameof(MyProfile));
        }
    }
}
