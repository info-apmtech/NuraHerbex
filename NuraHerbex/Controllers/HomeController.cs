using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NuraHerbex.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;

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

        public IActionResult Index()
        {
            return View();
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

        public IActionResult Plan()
        {
            return View();
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

            // Optional: if you want to pre-select a specific product (for details pane, etc.)
            Product? selected = null;
            if (id > 0)
            {
                var oneResponse = await _httpClient.GetAsync($"AdminAPI/product/{id}");
                if (oneResponse.IsSuccessStatusCode)
                {
                    selected = JsonConvert.DeserializeObject<Product>(await oneResponse.Content.ReadAsStringAsync());
                }
            }

            var vm = new ProductViewModel
            {
                ProductList = products,
                NewProduct = selected ?? new Product()
            };

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
        public IActionResult MyProfile()
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
    }
}
