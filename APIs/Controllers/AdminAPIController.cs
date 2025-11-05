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
        [AllowAnonymous]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _adminservice.GetAllUsersAsync();
            return Ok(users);
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
        //[AllowAnonymous]
        //[HttpDelete("delete/{id}")]
        //public async Task<IActionResult> DeleteUser(string id)
        //{
        //    var result = await _adminservice.DeleteUserAsync(id);
        //    if (result.Succeeded)
        //        return Ok(new { success = true, message = "User deleted successfully" });

        //    return BadRequest(result.Errors);
        //}


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
        [AllowAnonymous]
        [HttpPost("blog/incrementreadcount/{id}")]
        public async Task<IActionResult> IncrementReadCount(int id)
        {
            var blog = await _adminservice.GetBlogByIdAsync(id);
            if (blog == null)
                return NotFound();

            blog.ReadCount++;
            var result = await _adminservice.AddOrUpdateBlogAsync(blog);

            if (result.Succeeded)
                return Ok(new { success = true });

            return BadRequest(result.Errors);
        }

        //Incredients
        // GET: api/adminapi/ingredients
        [AllowAnonymous]
        [HttpGet("ingredients")]
        public async Task<IActionResult> GetIngredients()
        {
            var ingredients = await _adminservice.GetIngredientsAsync();
            return Ok(ingredients);
        }

        // GET: api/adminapi/ingredient/{id}
        [AllowAnonymous]
        [HttpGet("ingredient/{id}")]
        public async Task<IActionResult> GetIngredient(int id)
        {
            var ingredient = await _adminservice.GetIngredientByIdAsync(id);
            if (ingredient == null)
                return NotFound();

            return Ok(ingredient);
        }

        // POST: api/adminapi/ingredient
        [AllowAnonymous]
        [HttpPost("ingredient")]
        public async Task<IActionResult> AddOrUpdateIngredient([FromBody] Ingredient ingredient)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddOrUpdateIngredientAsync(ingredient);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Ingredient saved successfully" });

            return BadRequest(result.Errors);
        }

        // DELETE: api/adminapi/ingredient/{id}
        [AllowAnonymous]
        [HttpDelete("ingredient/{id}")]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            var result = await _adminservice.DeleteIngredientAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Ingredient deleted successfully" });

            return BadRequest(result.Errors);
        }

        //Ingredients Category
        // IngredientCategory APIs
        [AllowAnonymous]
        [HttpGet("ingredientcategories")]
        public async Task<IActionResult> GetIngredientCategories()
        {
            var categories = await _adminservice.GetIngredientCategoriesAsync();
            return Ok(categories);
        }

        [AllowAnonymous]
        [HttpGet("ingredientcategory/{id}")]
        public async Task<IActionResult> GetIngredientCategory(int id)
        {
            var category = await _adminservice.GetIngredientCategoryByIdAsync(id);
            if (category == null)
                return NotFound();

            return Ok(category);
        }

        [AllowAnonymous]
        [HttpPost("ingredientcategory")]
        public async Task<IActionResult> AddOrUpdateIngredientCategory([FromBody] IngredientCategory category)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddOrUpdateIngredientCategoryAsync(category);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Ingredient category saved successfully" });

            return BadRequest(result.Errors);
        }

        [AllowAnonymous]
        [HttpDelete("ingredientcategory/{id}")]
        public async Task<IActionResult> DeleteIngredientCategory(int id)
        {
            var result = await _adminservice.DeleteIngredientCategoryAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Ingredient category deleted successfully" });

            return BadRequest(result.Errors);
        }
        // Products
        // GET: api/adminapi/products
        [AllowAnonymous]
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _adminservice.GetProductsAsync();
            return Ok(products);
        }

        // GET: api/adminapi/product/{id}
        [AllowAnonymous]
        [HttpGet("product/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _adminservice.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            return Ok(product);
        }

        // -------------------------------------------
        // OPTION A: JSON body (no file uploads)
        // POST: api/adminapi/product
        [AllowAnonymous]
        [HttpPost("product")]
        public async Task<IActionResult> AddOrUpdateProduct([FromBody] Product product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddOrUpdateProductAsync(product);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Product saved successfully" });

            return BadRequest(result.Errors);
        }
        // -------------------------------------------

        // -------------------------------------------
        // OPTION B: multipart/form-data with files
        // (Remove OPTION A if you use this.)
        // POST: api/adminapi/product-form
        [AllowAnonymous]
        [HttpPost("product-form")]
        [RequestSizeLimit(50_000_000)] // optional: 50 MB
        public async Task<IActionResult> AddOrUpdateProductForm([FromForm] Product product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddOrUpdateProductAsync(product);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Product saved successfully" });

            return BadRequest(result.Errors);
        }
        // -------------------------------------------

        // DELETE: api/adminapi/product/{id}
        [AllowAnonymous]
        [HttpDelete("product/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _adminservice.DeleteProductAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Product deleted successfully" });

            return BadRequest(result.Errors);
        }


    }
}
