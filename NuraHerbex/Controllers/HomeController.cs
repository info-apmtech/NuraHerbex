using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NuraHerbex.Models;

namespace NuraHerbex.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Blog()
        {
            return View();
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
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
