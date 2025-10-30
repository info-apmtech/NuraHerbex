using Microsoft.AspNetCore.Mvc;

namespace NuraHerbex.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult UserCreation()
        {
            return View();
        }
        public IActionResult AdminBlog()
        {
            return View();
        }
        public IActionResult AdminBlogCategory()
        {
            return View();
        } 
        public IActionResult AdminIncredient()
        {
            return View();
        }
    }
}
