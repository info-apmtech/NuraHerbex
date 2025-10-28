using Microsoft.AspNetCore.Mvc;

namespace NuraHerbex.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
