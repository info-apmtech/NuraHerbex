using System.Diagnostics;
using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NuraHerbex.Models;

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

        public IActionResult Plan()
        {
            return View();
        }
        public IActionResult Shop()
        {
            return View();
        }
        public IActionResult Quiz()
        {
            return View();
        }
		public IActionResult Ingredients()
		{
			return View();
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
        public IActionResult MyConsultation()
        {
            return View();
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
