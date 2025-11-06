using Domain.Extensions;
using Domain.Models;
using Domain.ViewModel;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;
using NuraHerbex.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using MimeKit;
using MailKit.Security;
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
                Ingredients = homeIngredients
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
        //        [HttpPost]
        //        [ValidateAntiForgeryToken]
        //        public async Task<IActionResult> Subscribe(string email)
        //        {
        //            var fromAddress = _emailSettings.FromAddress;
        //            var fromPassword = _emailSettings.Password;
        //            var smtpHost = _emailSettings.Host;
        //            var smtpPort = _emailSettings.Port;
        //            var useSsl = _emailSettings.UseSSL;

        //            if (string.IsNullOrWhiteSpace(email) || !MailboxAddress.TryParse(email, out var userMailbox))
        //            {
        //                TempData["Message"] = "Invalid email address.";
        //                return RedirectToAction("Index");
        //            }

        //            // 1?? Notify your internal team
        //            var toCompany = new MimeMessage();
        //            toCompany.From.Add(MailboxAddress.Parse(fromAddress));
        //            toCompany.To.Add(MailboxAddress.Parse(fromAddress));
        //            toCompany.Subject = "New Newsletter Subscription – Nura Herbex";
        //            toCompany.Body = new TextPart("plain")
        //            {
        //                Text = $"A new user has subscribed to the Nura Herbex newsletter.\n\n" +
        //                       $"Email: {email}\n" +
        //                       $"Subscribed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
        //            };

        //            // 2?? Auto-reply to subscriber
        //            var toUser = new MimeMessage();
        //            toUser.From.Add(MailboxAddress.Parse(fromAddress));
        //            toUser.To.Add(userMailbox);
        //            toUser.Subject = "Welcome to Nura Herbex!";

        //            var htmlBody = $@"
        //<html>
        //  <body style=""font-family: Arial, Helvetica, sans-serif; font-size: 14px; color: #222;"">
        //    <p>Dear Subscriber,</p>
        //    <p>Thank you for subscribing to <strong>Nura Herbex</strong> — your partner in natural wellness.</p>
        //    <p>You'll be among the first to know about our latest herbal innovations, exclusive offers, and wellness insights.</p>
        //    <p style=""margin-top:16px;"">Warm regards,<br/>The Nura Herbex Team</p>
        //    <hr style=""margin-top:20px;margin-bottom:10px;border:0;border-top:1px solid #ddd;"">
        //    <p style=""font-size:12px;color:#666;"">You’re receiving this email because you subscribed at <strong>nuraherbex.com</strong>.</p>
        //  </body>
        //</html>";

        //            toUser.Body = new TextPart("html") { Text = htmlBody };

        //            try
        //            {
        //                using var smtp = new MailKit.Net.Smtp.SmtpClient();
        //                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

        //                var secure = useSsl
        //                    ? MailKit.Security.SecureSocketOptions.StartTls
        //                    : MailKit.Security.SecureSocketOptions.None;

        //                await smtp.ConnectAsync(smtpHost, smtpPort, secure);
        //                await smtp.AuthenticateAsync(fromAddress, fromPassword);

        //                await smtp.SendAsync(toCompany);  // notify admin
        //                await smtp.SendAsync(toUser);     // thank subscriber
        //                await smtp.DisconnectAsync(true);

        //                TempData["Message"] = "Thank you for subscribing! Please check your inbox for confirmation.";
        //            }
        //            catch (Exception ex)
        //            {
        //                TempData["Message"] = $"Subscription failed: {ex.Message}";
        //            }

        //            return RedirectToAction("Index");
        //        }
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
            var res = await AuthorizedClient.PostAsJsonAsync("AdminAPI/newsletter/subscription", payload);

            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                TempData["Message"] = $"Subscription failed: {err}";
                return RedirectToAction("Index");
            }

            var dto = await res.Content.ReadFromJsonAsync<NewsletterSubscriptionResult>();
            if (dto is null || !dto.Succeeded)
            {
                TempData["Message"] = "Subscription failed: unexpected response.";
                return RedirectToAction("Index");
            }

            // Send using content from Service
            try
            {
                using var smtp = new SmtpClient();
                var secure = _emailSettings.UseSSL
                    ? (_emailSettings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
                    : SecureSocketOptions.None;

                await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, secure);
                await smtp.AuthenticateAsync(_emailSettings.FromAddress, _emailSettings.Password);

                // Admin
                var toCompany = new MimeMessage();
                toCompany.From.Add(MailboxAddress.Parse(_emailSettings.FromAddress));
                toCompany.To.Add(MailboxAddress.Parse(_emailSettings.FromAddress));
                toCompany.Subject = dto.AdminSubject;
                toCompany.Body = new TextPart("plain") { Text = dto.AdminBodyText };

                // User
                var toUser = new MimeMessage();
                toUser.From.Add(MailboxAddress.Parse(_emailSettings.FromAddress));
                toUser.To.Add(MailboxAddress.Parse(dto.Email));
                toUser.Subject = dto.UserSubject;
                toUser.Body = new TextPart("html") { Text = dto.UserBodyHtml };

                await smtp.SendAsync(toCompany);
                await smtp.SendAsync(toUser);
                await smtp.DisconnectAsync(true);

                TempData["Message"] = "Thank you for subscribing! Please check your inbox.";
            }
            catch
            {
                TempData["Message"] = "Saved successfully, but sending email failed.";
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
    }
}
