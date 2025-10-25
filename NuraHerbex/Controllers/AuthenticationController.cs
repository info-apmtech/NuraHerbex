using Microsoft.AspNetCore.Mvc;

namespace NuraHerbex.Controllers
{
    public class AuthenticationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult SignIn()
        {
            return View();
        }
        public IActionResult ForgetPassword()
        {
            return View();
        }
        public IActionResult CreatePassword()
        {
            return View();
        }
    }
}
