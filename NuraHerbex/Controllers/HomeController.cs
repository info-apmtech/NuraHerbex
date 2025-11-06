using Domain.Implementation;
using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using NuraHerbex.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
//using static ServiceStack.Diagnostics.Events;

namespace NuraHerbex.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;


        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("NuraHerbexApi");
        }

        public async Task<IActionResult> Index()
        {
            var blogs = new List<Blog>();
            var ingredients = new List<Ingredient>();

            // ? Get Blogs
            var blogResponse = await _httpClient.GetAsync("AdminAPI/blogs");
            if (blogResponse.IsSuccessStatusCode)
            {
                var json = await blogResponse.Content.ReadAsStringAsync();
                blogs = JsonConvert.DeserializeObject<List<Blog>>(json) ?? new List<Blog>();
            }

            // ? Get Ingredients
            var ingredientResponse = await _httpClient.GetAsync("AdminAPI/ingredients");
            if (ingredientResponse.IsSuccessStatusCode)
            {
                var json = await ingredientResponse.Content.ReadAsStringAsync();
                ingredients = JsonConvert.DeserializeObject<List<Ingredient>>(json) ?? new List<Ingredient>();
            }

            // ? Filter only active ingredients for homepage
            var homeIngredients = ingredients
                .Where(i => i.IsActive && i.ShowHome)
                .OrderByDescending(i => i.CreatedAt)
                .Take(6) // optional: show first 6 for layout balance
                .ToList();

            // ? Get top 10 blogs
            var latestBlogs = blogs
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .ToList();

            var vm = new HomeViewModel
            {
                BlogList = latestBlogs,
                Ingredients = homeIngredients,
                PlanList = await GetPlansFromApi()
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

            // For product cards list — handle duplicate IDs safely
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


        public IActionResult Consultation()
        {
            return View();
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
        //public IActionResult MyProfile()
        //{
        //    return View();
        //}
        public IActionResult MyOrders()
        {
            return View();
        }
        public IActionResult MyReturns()
        {
            return View();
        }
        public IActionResult Wishlist()
        {
            return View();
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
        [HttpPost]
        public async Task<IActionResult> Subscribe(string Email)
        {
            if (string.IsNullOrEmpty(Email))
                return BadRequest("Email is required.");

            try
            {
                // Configure mail message
                var mail = new MailMessage();
                mail.From = new MailAddress("yourcompanyemail@example.com", "Nura Herbex");
                mail.To.Add(Email);
                mail.Subject = "Thanks for Subscribing!";
                mail.Body = "Thank you for subscribing to Nura Herbex! Our team will contact you soon.";
                mail.IsBodyHtml = false;

                // Configure SMTP client
                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential("yourcompanyemail@example.com", "your-app-password");
                    smtp.EnableSsl = true;
                    await smtp.SendMailAsync(mail);
                }

                // Optionally send internal notification
                // e.g., send to your admin email also
                // mail.To.Clear();
                // mail.To.Add("support@nuraherbex.com");

                TempData["Message"] = "Subscription successful! Please check your email.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log error here
                TempData["Message"] = "Error: " + ex.Message;
                return RedirectToAction("Index");
            }
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
