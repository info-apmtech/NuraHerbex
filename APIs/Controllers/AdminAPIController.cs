using Domain.Implementation;
using Domain.Interface;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIs.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class AdminAPIController : ControllerBase
	{
		private readonly IAdmin _adminservice;

		public AdminAPIController(IAdmin adminservice)
		{
			_adminservice = adminservice;
		}

		[HttpGet("users/{role}")]
		public async Task<IActionResult> GetUsers(UserRole role)
		{
			var users = await _adminservice.GetUsersByRoleAsync(role);
			return Ok(users);
		}

		[HttpGet("user/{id}")]
		public async Task<IActionResult> GetUser(string id)
		{
			var user = await _adminservice.GetUserByIdAsync(id);
			if (user == null)
				return NotFound();

			return Ok(user);
		}

		[AllowAnonymous]
		[HttpPost("register")]
		public async Task<IActionResult> AddOrUpdateUser([FromBody] RegisterUser user)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _adminservice.AddOrUpdateUserAsync(user);

			if (result.Succeeded)
				return Ok(new { success = true, message = "User saved successfully" });

			return BadRequest(result.Errors);
		}


        //BlogCategory

        [AllowAnonymous]
        [HttpGet("blogcategory/{id}")]
        public async Task<IActionResult> GetBlogCategory(int id)
        {
            var category = await _adminservice.GetBlogCategoryByIdAsync(id);
            if (category == null)
                return NotFound();

            return Ok(category);
        }

        [AllowAnonymous] 
        [HttpGet("blogcategories")]
        public async Task<IActionResult> GetBlogCategories()
        {
            var categories = await _adminservice.GetBlogCategoriesAsync();
            return Ok(categories);
        }

        [AllowAnonymous] 
        [HttpPost("blogcategory")]
        public async Task<IActionResult> AddOrUpdateBlogCategory([FromBody] BlogCategory category)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddOrUpdateBlogCategoryAsync(category);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Blog category saved successfully" });

            return BadRequest(result.Errors);
        }

        [AllowAnonymous]
        [HttpDelete("blogcategory/{id}")]
        public async Task<IActionResult> DeleteBlogCategory(int id)
        {
            var result = await _adminservice.DeleteBlogCategoryAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Category deleted successfully" });

            return BadRequest(result.Errors);
        }


        //Blog
        [AllowAnonymous]
        [HttpGet("blogs")]
        public async Task<IActionResult> GetBlogs()
        {
            var blogs = await _adminservice.GetBlogsAsync();
            return Ok(blogs);
        }

        [AllowAnonymous]
        [HttpGet("blog/{id}")]
        public async Task<IActionResult> GetBlog(int id)
        {
            var blog = await _adminservice.GetBlogByIdAsync(id);
            if (blog == null)
                return NotFound();

            return Ok(blog);
        }

        [AllowAnonymous]
        [HttpPost("blog")]
        public async Task<IActionResult> AddOrUpdateBlog([FromBody] Blog blog)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddOrUpdateBlogAsync(blog);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Blog saved successfully" });

            return BadRequest(result.Errors);
        }

        [AllowAnonymous]
        [HttpDelete("blog/{id}")]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            var result = await _adminservice.DeleteBlogAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Blog deleted successfully" });

            return BadRequest(result.Errors);
        }


    }
}
