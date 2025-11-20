using Domain.Implementation;
using Domain.Interface;
using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static Domain.ViewModel.CartItemViewModel;

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
        [AllowAnonymous]
        [HttpGet("users/{role}")]
        public async Task<IActionResult> GetUsers(UserRole role)
        {
            var users = await _adminservice.GetUsersByRoleAsync(role);
            return Ok(users);
        }
        [AllowAnonymous]
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
			var isNew = string.IsNullOrWhiteSpace(user.Id);

			// For new users, ignore “Id is required” validation
			if (isNew)
			{
				ModelState.Remove("Id");
				ModelState.Remove("user.Id");
			}

			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _adminservice.AddOrUpdateUserAsync(user);

			if (result.Succeeded)
				return Ok(new { success = true, message = "User saved successfully" });

			return BadRequest(result.Errors);
		}


        [AllowAnonymous]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var result = await _adminservice.DeleteUserAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "User deleted successfully" });

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

        [AllowAnonymous]
        [HttpGet("gstentries")]
        public async Task<IActionResult> GetGSTEntries()
        {
            var gstEntries = await _adminservice.GetGSTEntriesAsync();
            return Ok(gstEntries);
        }

        [AllowAnonymous]
        [HttpGet("gstentry/{id}")]
        public async Task<IActionResult> GetGSTEntry(int id)
        {
            var gst = await _adminservice.GetGSTEntryByIdAsync(id);
            if (gst == null) return NotFound();
            return Ok(gst);
        }

        //[AllowAnonymous]
        [Authorize]
        [HttpPost("gstentry")]
        public async Task<IActionResult> AddOrUpdateGSTEntry([FromBody] GST gst)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ✅ Extract the logged-in username or user ID from claims
            var updatedBy = User?.Identity?.Name ?? "System";
            gst.UpdatedBy = updatedBy;

            var result = await _adminservice.AddOrUpdateGSTEntryAsync(gst);
            if (result.Succeeded)
                return Ok(new { success = true, message = "GST entry saved successfully", updatedBy });

            return BadRequest(result.Errors);
        }

        [AllowAnonymous]
        [HttpDelete("gstentry/{id}")]
        public async Task<IActionResult> DeleteGSTEntry(int id)
        {
            var result = await _adminservice.DeleteGSTEntryAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "GST entry deleted successfully" });

            return BadRequest(result.Errors);
        }

        //Plans
        [AllowAnonymous]
        [HttpGet("pricingplans")]
        public async Task<IActionResult> GetPricingPlans()
        {
            var plans = await _adminservice.GetPricingPlansAsync();
            return Ok(plans);
        }

        [AllowAnonymous]
        [HttpGet("pricingplan/{id}")]
        public async Task<IActionResult> GetPricingPlan(int id)
        {
            var plan = await _adminservice.GetPricingPlanByIdAsync(id);
            if (plan == null) return NotFound();
            return Ok(plan);
        }

        [AllowAnonymous]
        [Authorize]
        [HttpPost("pricingplan")]
        public async Task<IActionResult> AddOrUpdatePricingPlan([FromBody] PricingPlan plan)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddOrUpdatePricingPlanAsync(plan);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Pricing plan saved successfully" });

            return BadRequest(result.Errors);
        }

        [AllowAnonymous]
        [HttpDelete("pricingplan/{id}")]
        public async Task<IActionResult> DeletePricingPlan(int id)
        {
            var result = await _adminservice.DeletePricingPlanAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Pricing plan deleted successfully" });

            return BadRequest(result.Errors);
        }
        //Address
        [AllowAnonymous]
        [HttpGet("addresses/{userId}")]
        public async Task<IActionResult> GetUserAddresses(string userId)
        {
            var addresses = await _adminservice.GetAddressesByUserAsync(userId);
            return Ok(addresses);
        }
        [AllowAnonymous]
        [HttpGet("address/{id}")]
        public async Task<IActionResult> GetAddress(int id)
        {
            var address = await _adminservice.GetAddressByIdAsync(id);
            if (address == null)
                return NotFound();

            return Ok(address);
        }
        [AllowAnonymous]
        [HttpPost("address")]
        public async Task<IActionResult> AddOrUpdateAddress([FromBody] AddressDetail address)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddOrUpdateAddressAsync(address);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Address saved successfully" });

            return BadRequest(result.Errors);
        }
        [AllowAnonymous]
        [HttpDelete("address/{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var result = await _adminservice.DeleteAddressAsync(id);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Address deleted successfully" });

            return BadRequest(result.Errors);
        }
        [AllowAnonymous]
        [HttpGet("countries")]
        public async Task<IActionResult> GetCountries()
        {
            var countries = await _adminservice.GetCountriesAsync();
            return Ok(countries);
        }
        [AllowAnonymous]
        [HttpGet("states/{countryId}")]
        public async Task<IActionResult> GetStates(int countryId)
        {
            var states = await _adminservice.GetStatesByCountryAsync(countryId);
            return Ok(states);
        }
        [AllowAnonymous]
        [HttpGet("states")]
        public async Task<IActionResult> GetAllStates()
        {
            var states = await _adminservice.GetAllStatesAsync();
            return Ok(states);
        }


        [AllowAnonymous]
        [HttpPost("newsletter/subscription")]
        public async Task<IActionResult> AddOrUpdateNewsletterSubscription([FromBody] SubscribeRequest request)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { success = false, error = "Email is required" });

            var result = await _adminservice.SaveNewsletterSubscriptionAsync(request.Email);
            if (!result.Succeeded)
                return BadRequest(new { success = false, error = result.Error });

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("subscriptions")]
        public async Task<IActionResult> GetSubscriptions()
        {
            var subs = await _adminservice.GetAllSubscription();
            return Ok(subs);
        }

        // Wishlist
        [AllowAnonymous]
        [HttpGet("wishlist/{userId}")]
        public async Task<IActionResult> GetWishlist(string userId)
        {
            var items = await _adminservice.GetWishlistByUserAsync(userId);
            return Ok(items);
        }
        [AllowAnonymous]
        [HttpPost("wishlist")]
        public async Task<IActionResult> AddOrUpdateWishlist([FromBody] WishlistItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.UserId) || item.ProductId == 0)
                return BadRequest("Invalid wishlist item payload.");

            var result = await _adminservice.AddOrUpdateWishlistAsync(item);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Wishlist updated" });

            return BadRequest(result.Errors);
        }
        [AllowAnonymous]
        [HttpDelete("wishlist/{id}")]
        public async Task<IActionResult> DeleteWishlist(int id)
        {
            var result = await _adminservice.DeleteWishlistAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true });

            return BadRequest(result.Errors);
        }
        // Cart
        [AllowAnonymous]
        [HttpGet("Cart/{userId}")]
        public async Task<IActionResult> Cartlist(string userId)
        {
            var items = await _adminservice.GetCartByUserAsync(userId);
            return Ok(items);
        }

        [AllowAnonymous]
        [HttpPost("Cart")]
        public async Task<IActionResult> AddOrUpdateCart([FromBody] CartItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.UserId) || item.ProductId <= 0)
                return BadRequest("Invalid Cart item.");

            // Default quantity when caller didn't set it
            if (item.Quantity <= 0) item.Quantity = 1;

            var result = await _adminservice.AddOrUpdateCartAsync(item);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Cart updated" });

            return BadRequest(result.Errors);
        }

        [AllowAnonymous]
        [HttpDelete("Cart/{id:int}")]
        public async Task<IActionResult> DeleteCart(int id)
        {
            var result = await _adminservice.DeleteCartAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true });

            return BadRequest(result.Errors);
        }

        // NEW: change qty by delta (vm.Quantity is used as delta)
        [AllowAnonymous]
        [HttpPost("Cart/quantity/change")]
        public async Task<IActionResult> ChangeQuantity([FromBody] CartItemViewModel vm)
        {
            if (vm == null || vm.CartItemId <= 0 || vm.Quantity == 0)
                return BadRequest("Invalid payload.");

            var result = await _adminservice.ChangeCartQuantityAsync(vm.CartItemId, vm.Quantity);
            if (result.Succeeded) return Ok(new { success = true });
            return BadRequest(new { success = false, message = "Failed to change quantity." });
        }

        // NEW: set absolute qty (vm.Quantity is the new value)
        [AllowAnonymous]
        [HttpPut("Cart/quantity/set")]
        public async Task<IActionResult> SetQuantity([FromBody] CartItemViewModel vm)
        {
            if (vm == null || vm.CartItemId <= 0)
                return BadRequest("Invalid payload.");

            var result = await _adminservice.SetCartQuantityAsync(vm.CartItemId, vm.Quantity);
            if (result.Succeeded) return Ok(new { success = true });
            return BadRequest(new { success = false, message = "Failed to set quantity." });
        }

        [AllowAnonymous]
        [HttpPost("consultationbooking")]
        public async Task<IActionResult> BookConsultation([FromBody] ConsultationBooking consultation)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.SaveConsultationAsync(consultation);
            if (result.Succeeded)
            {
                return Ok(new { success = true, message = "Consultation booked" });
            }

            return BadRequest(result.Errors);
        }

        // ✅ Get all consultations for a specific doctor
        [AllowAnonymous]
        [HttpGet("consultationbooking/{userId}")]
        public async Task<IActionResult> GetConsultations(string userId)
        {
            var consultations = await _adminservice.GetConsultationsByUserAsync(userId);
            return Ok(consultations);
        }

        // ✅ Get all consultations created by a specific user (patient)
        [AllowAnonymous]
        [HttpGet("consultationbooking/user/{userId}")]
        public async Task<IActionResult> GetConsultationsByCreator(string userId)
        {
            var consultations = await _adminservice.GetConsultationsByCreatorAsync(userId);
            return Ok(consultations);
        }
        [AllowAnonymous]
        [HttpGet("consultationbooking/all")]
        public async Task<IActionResult> GetAllConsultations()
        {
            var consultations = await _adminservice.GetAllConsultationsAsync();
            return Ok(consultations);
        }
        [AllowAnonymous]
        [HttpPost("consultationbooking/{consultationId}/status")]
        public async Task<IActionResult> UpdateConsultationStatus(int consultationId, [FromBody] ConsultationStatusUpdateModel updateModel)
        {
            bool success = await _adminservice.UpdateConsultationStatusAsync(consultationId, updateModel);
            if (!success)
                return NotFound();

            return Ok();
        }


        //Specialities
        [AllowAnonymous]
        [HttpGet("doctorspecialities")]
        public async Task<IActionResult> GetDoctorSpecialities()
        {
            var specialities = await _adminservice.GetDoctorSpecialitiesAsync();
            return Ok(specialities);
        }

        [AllowAnonymous]
        [HttpGet("doctorspeciality/{id}")]
        public async Task<IActionResult> GetDoctorSpeciality(int id)
        {
            var speciality = await _adminservice.GetDoctorSpecialityByIdAsync(id);
            if (speciality == null)
                return NotFound();

            return Ok(speciality);
        }

        [AllowAnonymous]
        [HttpPost("doctorspeciality")]
        public async Task<IActionResult> AddOrUpdateDoctorSpeciality([FromBody] DoctorSpeciality speciality)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddOrUpdateDoctorSpecialityAsync(speciality);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Doctor speciality saved successfully" });

            return BadRequest(result.Errors);
        }

        [AllowAnonymous]
        [HttpDelete("doctorspeciality/{id}")]
        public async Task<IActionResult> DeleteDoctorSpeciality(int id)
        {
            var result = await _adminservice.DeleteDoctorSpecialityAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Doctor speciality deleted successfully" });

            return BadRequest(result.Errors);
        }

        //DoctorDetails
        [AllowAnonymous]
        [HttpGet("doctordetails")]
        public async Task<IActionResult> GetDoctorDetails()
        {
            var list = await _adminservice.GetDoctorDetailsAsync();
            return Ok(list);
        }

        [AllowAnonymous]
        [HttpGet("doctordetail/{id}")]
        public async Task<IActionResult> GetDoctorDetail(int id)
        {
            var detail = await _adminservice.GetDoctorDetailByIdAsync(id);
            if (detail == null)
                return NotFound();

            return Ok(detail);
        }

        [AllowAnonymous]
        [HttpPost("doctordetail")]
        public async Task<IActionResult> AddOrUpdateDoctorDetail([FromBody] DoctorDetail detail)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddOrUpdateDoctorDetailAsync(detail);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Doctor detail saved successfully" });

            return BadRequest(result.Errors);
        }

        [AllowAnonymous]
        [HttpDelete("doctordetail/{id}")]
        public async Task<IActionResult> DeleteDoctorDetail(int id)
        {
            var result = await _adminservice.DeleteDoctorDetailAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Doctor detail deleted successfully" });

            return BadRequest(result.Errors);
        }

        [AllowAnonymous]
        [HttpGet("feedbacks")]
        public async Task<IActionResult> GetAllFeedbacks(CancellationToken ct)
        {
            var feedbacks = await _adminservice.GetAllFeedbacksAsync(ct);
            return Ok(feedbacks);
        }
        [AllowAnonymous]
        [HttpGet("feedbacks/user/{userId}")]
        public async Task<IActionResult> GetFeedbacksByUser(string userId, CancellationToken ct)
        {
            var allFeedbacks = await _adminservice.GetAllFeedbacksAsync(ct);

            var userFeedbacks = allFeedbacks
                .Where(f => string.Equals(f.CustomerID, userId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Ok(userFeedbacks);
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost("submitfeedback")]
        public async Task<IActionResult> Submit([FromBody] SubmitFeedbackRequest req, CancellationToken ct)
        {
            if (req is null) return BadRequest("Invalid payload.");
            if (req.Rating < 1 || req.Rating > 5) return BadRequest("Rating must be 1..5.");

            var saved = await _adminservice.SaveAsync(req, ct);
            return Ok(new { saved.Id, saved.OrderID, saved.CustomerID, saved.RatingCount, saved.Message, saved.SubmittedAt });
        }

        //Pincode
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("pincodes")]
        public async Task<IActionResult> GetPincodes()
        {
            var pincodes = await _adminservice.GetPincodesAsync();
            return Ok(pincodes);
        }
		[Authorize(Roles = "Admin,Employee")]
        [HttpGet("pincode/{id}")]
        public async Task<IActionResult> GetPincode(int id)
        {
            var pincode = await _adminservice.GetPincodeByIdAsync(id);
            if (pincode == null)
                return NotFound();

            return Ok(pincode);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("pincode")]
        public async Task<IActionResult> AddOrUpdatePincode([FromBody] Pincode pincode)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddOrUpdatePincodeAsync(pincode);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Pincode saved successfully" });

            return BadRequest(result.Errors);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("pincode/{id}")]
        public async Task<IActionResult> DeletePincode(int id)
        {
            var result = await _adminservice.DeletePincodeAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Pincode deleted successfully" });

            return BadRequest(result.Errors);
        }

		//Orders
		[AllowAnonymous]
		[HttpPost("orders")]
		public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
		{
			if (!ModelState.IsValid)
				return ValidationProblem(ModelState);

			if (dto == null || dto.Details == null || dto.Details.Count == 0)
				return BadRequest("Invalid data.");

			// Map DTO -> EF Order entity
			var order = new Order
			{
				UserId        = dto.UserId,
				AddressId     = dto.AddressId,
				DoorNo        = dto.DoorNo,
				PhoneNo       = dto.PhoneNo,
				Address       = dto.Address,
				State         = dto.State,
				PinCode       = dto.PinCode,
				Country       = dto.Country,
				OrderDate     = dto.OrderDate,
				Status        = dto.Status,
				Subtotal      = dto.Subtotal,
				Tax           = dto.Tax,
				Shipping      = dto.Shipping,
				TotalDiscount = dto.TotalDiscount,
				Total         = dto.Total
			};

			// Map DTO details -> EF OrderDetail entities
			var details = dto.Details.Select(d => new OrderDetail
			{
				ProductId       = d.ProductId,
				Quantity        = d.Quantity,
				UnitPrice       = d.UnitPrice,
				ProductDiscount = 0m // or compute if needed
									 // OrderId will be set inside CreateAsync when order is saved
			}).ToList();

			var orderId = await _adminservice.CreateAsync(order, details);

			return Ok(new { id = orderId });
		}


		[AllowAnonymous]
        [HttpPost("updateStatus")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusRequest request)
        {
            var success = await _adminservice.UpdateOrderStatusAsync(request.OrderId, request.Status);
            if (success)
                return Ok(new { message = "Order status updated successfully." });
            else
                return NotFound(new { message = "Order not found." });
        }
        [AllowAnonymous]
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _adminservice.GetAllOrdersAsync();
            return Ok(orders);
        }
        //[AllowAnonymous]
        //[HttpGet("orderdetails")]
        //public async Task<IActionResult> GetOrderDetails([FromQuery] int orderId)
        //{
        //    var details = await _adminservice.GetOrderDetailsAsync(orderId);
        //    if (details == null || !details.Any())
        //        return NotFound();
        //    return Ok(details);
        //}
        [AllowAnonymous]
        [HttpGet("orderdetails")]
        public async Task<IActionResult> GetOrderDetails([FromQuery] int orderId)
        {
            var details = await _adminservice.GetOrderDetailsAsync(orderId);
            return Ok(details ?? new List<OrderDetail>()); 
        }

        [AllowAnonymous]
        [HttpGet("user/orders/{userId}")]
        public async Task<IActionResult> GetOrdersByUserId(string userId)
        {
            var allOrders = await _adminservice.GetAllOrdersAsync();
            var userOrders = allOrders.Where(o => o.UserId == userId).ToList();
            return Ok(userOrders);
        }
        [AllowAnonymous]
        [HttpGet("orderdetails/user/{userId}")]
        public async Task<IActionResult> GetOrderDetailsByUserId(string userId)
        {
            var allOrders = await _adminservice.GetAllOrdersAsync();
            var userOrders = allOrders.Where(o => o.UserId == userId).Select(o => o.Id).ToList();

            var details = new List<OrderDetail>();
            foreach (var orderId in userOrders)
                details.AddRange(await _adminservice.GetOrderDetailsAsync(orderId));

            if (!details.Any())
                return NotFound();

            return Ok(details);
        }
        [AllowAnonymous]
        [HttpGet("orders/{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var order = await _adminservice.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();
            return Ok(order);
        }
        [AllowAnonymous]
        //[HttpGet("{orderId:int}")]
        [HttpGet("orders/{orderId:int}")]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            var order = await _adminservice.GetOrderAsync(orderId);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [AllowAnonymous]
        //[HttpGet("{orderId:int}")]
        [HttpGet("orders/full/{orderId:int}")]
        public async Task<IActionResult> GetOrderWithDetails(int orderId)
        {
            var order = await _adminservice.GetOrderAsync(orderId);
            if (order == null) return NotFound();
            return Ok(order);
        }
        //payment
        [AllowAnonymous]
        [HttpPost("PaymentDetails")]
        public async Task<IActionResult> AddPaymentDetails([FromBody] PaymentGatewayDetails payment)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddPaymentGatewayDetails(payment);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Pincode saved successfully" });

            return BadRequest(result.Errors);
        }

        [AllowAnonymous]
        [HttpGet("paymentlist")]
        public async Task<IActionResult> GetPayments()
        {
            var payments = await _adminservice.GetPaymentGatewayDetailsAsync();
            return Ok(payments);

            
        }
		// GET /api/AdminAPI/pincodes/600001
		[AllowAnonymous]                                      // <-- override [Authorize]
		[HttpGet("pincodes/{code:length(6)}")]
		public async Task<ActionResult<Pincode>> GetByCode(string code)
		{
			if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^\d{6}$"))
				return BadRequest("PIN must be exactly 6 digits.");

			var pin = await _adminservice.GetPincodeByCodeAsync(code);
			return pin is null ? NotFound() : Ok(pin);
		}
        [HttpGet("dashboard-stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
        {
            var stats = await _adminservice.GetDashboardStatsAsync();
            return Ok(stats);
        }
		[AllowAnonymous]
		[HttpPost("profile")]
		public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var result = await _adminservice.UpdateUserProfileAsync(dto);

			if (result.Succeeded)
				return Ok(new { success = true, message = "Profile updated successfully" });

			return BadRequest(result.Errors);
		}
		[AllowAnonymous]  // ⬅️ important
		[HttpPost("user/{id}/toggle-active")]
		public async Task<IActionResult> ToggleUserActive(string id)
		{
			var result = await _adminservice.ToggleUserActiveAsync(id);

			if (result.Succeeded)
				return Ok(new { success = true, message = "User status updated successfully" });

			return BadRequest(result.Errors);
		}
        // ====================== QUIZ CATEGORY API ========================= //

        [AllowAnonymous]
        [HttpGet("quizcategory/{id}")]
        public async Task<IActionResult> GetQuizCategory(int id)
        {
            var category = await _adminservice.GetQuizCategoryByIdAsync(id);
            if (category == null)
                return NotFound();

            return Ok(category);
        }

        [AllowAnonymous]
        [HttpGet("quizcategories")]
        public async Task<IActionResult> GetQuizCategories()
        {
            var categories = await _adminservice.GetQuizCategoriesAsync();
            return Ok(categories);
        }

        [AllowAnonymous]
        [HttpPost("quizcategory")]
        public async Task<IActionResult> AddOrUpdateQuizCategory([FromBody] QuizCategory category)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddOrUpdateQuizCategoryAsync(category);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Quiz category saved successfully" });

            return BadRequest(result.Errors);
        }

        [AllowAnonymous]
        [HttpDelete("quizcategory/{id}")]
        public async Task<IActionResult> DeleteQuizCategory(int id)
        {
            var result = await _adminservice.DeleteQuizCategoryAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Quiz category deleted successfully" });

            return BadRequest(result.Errors);
        }
        // ====================== QUIZ QUESTION API ========================= //

        [AllowAnonymous]
        [HttpGet("quizquestion/{id}")]
        public async Task<IActionResult> GetQuizQuestion(int id)
        {
            var question = await _adminservice.GetQuizQuestionByIdAsync(id);
            if (question == null)
                return NotFound();

            return Ok(question);
        }

        [AllowAnonymous]
        [HttpGet("quizquestions")]
        public async Task<IActionResult> GetQuizQuestions()
        {
            var questions = await _adminservice.GetQuizQuestionsAsync();
            return Ok(questions);
        }

        [AllowAnonymous]
        [HttpPost("quizquestion")]
        public async Task<IActionResult> AddOrUpdateQuizQuestion([FromBody] QuizQuestion question)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminservice.AddOrUpdateQuizQuestionAsync(question);

            if (result.Succeeded)
                return Ok(new { success = true, message = "Quiz question saved successfully" });

            return BadRequest(result.Errors);
        }

        [AllowAnonymous]
        [HttpDelete("quizquestion/{id}")]
        public async Task<IActionResult> DeleteQuizQuestion(int id)
        {
            var result = await _adminservice.DeleteQuizQuestionAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Quiz question deleted successfully" });

            return BadRequest(result.Errors);
        }

        // ====================== QUIZ OPTION API ========================= //

        [AllowAnonymous]
        [HttpGet("quizoptions")]
        public async Task<IActionResult> GetQuizOptions()
        {
            var options = await _adminservice.GetQuizOptionsAsync();
            return Ok(options);
        }

        [AllowAnonymous]
        [HttpGet("quizoption/{id}")]
        public async Task<IActionResult> GetQuizOption(int id)
        {
            var option = await _adminservice.GetQuizOptionByIdAsync(id);
            if (option == null)
                return NotFound();

            return Ok(option);
        }

        [AllowAnonymous]
        [HttpPost("quizoption")]
        public async Task<IActionResult> AddOrUpdateQuizOption([FromBody] QuizOption option)
        {
            var result = await _adminservice.AddOrUpdateQuizOptionAsync(option);

            if (result.Succeeded)
                return Ok(new { success = true });

            return BadRequest(result.Errors);
        }

        [AllowAnonymous]
        [HttpDelete("quizoption/{id}")]
        public async Task<IActionResult> DeleteQuizOption(int id)
        {
            var result = await _adminservice.DeleteQuizOptionAsync(id);

            if (result.Succeeded)
                return Ok(new { success = true });

            return BadRequest(result.Errors);
        }
		// User creates Return request
		//[HttpPost("request")]
		//public async Task<IActionResult> CreateReturn([FromBody] CreateReturnRequestDto dto)
		//{
		//	var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		//	if (string.IsNullOrEmpty(userId))
		//		return Unauthorized();

		//	try
		//	{
		//		var entity = await _adminservice.CreateReturnRequestAsync(userId, dto.OrderId, dto.Reason);
		//		return Ok(entity.Id);
		//	}
		//	catch (Exception ex)
		//	{
		//		return BadRequest(new { error = ex.Message });
		//	}
		//}

		//// User: get own returns
		//[HttpGet("me")]
		//public async Task<IActionResult> GetMyReturns([FromQuery] ReturnStatus? status)
		//{
		//	var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		//	if (string.IsNullOrEmpty(userId))
		//		return Unauthorized();

		//	var list = await _adminservice.GetUserReturnsAsync(userId, status);
		//	return Ok(list);
		//}

		//// Admin: get all
		//[HttpGet("admin")]
		//public async Task<IActionResult> GetAll([FromQuery] ReturnStatus? status)
		//{
		//	var list = await _adminservice.GetAllReturnsAsync(status);
		//	return Ok(list);
		//}

		//[HttpPost("{id}/approve")]
		//public async Task<IActionResult> Approve(int id)
		//{
		//	var result = await _adminservice.ApproveReturnAsync(id);
		//	return result.Succeeded ? Ok() : BadRequest(result.Errors);
		//}

		//[HttpPost("{id}/cancel")]
		//public async Task<IActionResult> Cancel(int id)
		//{
		//	var result = await _adminservice.CancelReturnAsync(id);
		//	return result.Succeeded ? Ok() : BadRequest(result.Errors);
		//}
		//// GET api/admin/returns?status=Pending
		//[HttpGet]
		//public async Task<IActionResult> GetAllReturns([FromQuery] ReturnStatus? status)
		//{
		//	var list = await _adminservice.GetAllReturnsAsync(status);
		//	return Ok(list);
		//}

		//// POST api/admin/returns/{id}/approve
		//[HttpPost("return/{id}/approve")]
		//public async Task<IActionResult> ApproveReturn(int id)
		//{
		//	var result = await _adminservice.ApproveReturnAsync(id);
		//	if (result.Succeeded)
		//		return Ok(new { success = true });

		//	return BadRequest(result.Errors);
		//}

		//[HttpPost("return/{id}/cancel")]
		//public async Task<IActionResult> CancelReturn(int id)
		//{
		//	var result = await _adminservice.CancelReturnAsync(id);
		//	if (result.Succeeded)
		//		return Ok(new { success = true });

		//	return BadRequest(result.Errors);
		//}
		// POST: api/AdminAPI/returnrequest
		//[HttpPost("returnrequest")]
		//public async Task<IActionResult> CreateReturnRequest([FromBody] ReturnRequestDto dto)
		//{
		//	if (!ModelState.IsValid)
		//		return BadRequest(ModelState);

		//	var created = await _adminservice.CreateReturnRequestAsync(dto);

		//	// Return 201 with location
		//	return CreatedAtAction(
		//		nameof(GetReturnRequestById),
		//		new { id = created.Id },
		//		created);
		//}

		// GET: api/AdminAPI/return/{id}
		//[HttpGet("return/{id}")]
		//public async Task<IActionResult> GetReturnRequestById(int id)
		//{
		//	// optional helper endpoint
		//	var entity = await _adminservice
		//		.GetReturnByIdAsync(id); // if you add this method
		//	if (entity == null) return NotFound();
		//	return Ok(entity);
		//}

		// POST: api/AdminAPI/return/{id}/approve
		//[HttpPost("return/{id}/approve")]
		//public async Task<IActionResult> ApproveReturn(int id, [FromBody] ReturnApproveDto dto)
		//{
		//	if (!ModelState.IsValid)
		//		return BadRequest(ModelState);

		//	// you can get admin name from User.Identity if using auth:
		//	var approvedBy = User?.Identity?.Name ?? "system";

		//	var updated = await _adminservice.ApproveReturnAsync(id, dto, approvedBy);
		//	if (updated == null)
		//		return NotFound(new { message = $"Return request {id} not found" });

		//	return Ok(updated);
		//}
		// ========== USER ENDPOINTS ==========

		// POST: AdminAPI/ReturnRequest
		[HttpPost("ReturnRequest")]
		[AllowAnonymous] // or nothing
		public async Task<IActionResult> CreateReturn([FromBody] ReturnRequestDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			//var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			//if (userId == null)
			//	return Unauthorized();

			try
			{
				var result = await _adminservice.CreateReturnAsync(dto.UserId, dto);
				return Ok(result);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		// GET: AdminAPI/ReturnRequest/my
		[HttpGet("ReturnRequest/my")]
		[AllowAnonymous] // ok, because you will validate in MVC & pass userId
		public async Task<IActionResult> MyReturns([FromQuery] string userId)
		    {
			if (string.IsNullOrWhiteSpace(userId))
				return BadRequest("UserId is required.");

			var list = await _adminservice.GetUserReturnsAsync(userId);
			return Ok(list);
		}


		// ========== ADMIN ENDPOINTS ==========

		// GET: AdminAPI/ReturnRequest/pending
		[HttpGet("ReturnRequest/getreturn")]
		[AllowAnonymous] // or nothing // ideally [Authorize(Roles = "Admin")]
		public async Task<IActionResult> GetReturnOrder()
		{
			var list = await _adminservice.GetOrderReturnsAsync();
			return Ok(list);
		}

		// GET: AdminAPI/ReturnRequest/5
		[HttpGet("ReturnRequest/{id:int}")]
		[AllowAnonymous] // or nothing // or Roles = "Admin"
		public async Task<IActionResult> GetById(int id)
		{
			var rr = await _adminservice.GetByIdAsync(id);
			if (rr == null) return NotFound();

			return Ok(rr);
		}

		// PUT: AdminAPI/ReturnRequest/5/status
		[HttpPut("ReturnRequest/{id:int}/status")]
		[AllowAnonymous] // or nothing// or Roles = "Admin"
		public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateReturnStatusDto dto)
		{
			if (id != dto.Id)
				return BadRequest(new { message = "Id mismatch." });

			var updated = await _adminservice.UpdateStatusAsync(dto);
			if (updated == null) return NotFound();

			return Ok(updated);
		}

		[HttpDelete("DeleteReturn/{id:int}")]
		// [Authorize(Roles = "Admin")] // when you’re ready
		public async Task<IActionResult> DeleteReturn(int id)
		{
			var success = await _adminservice.DeleteReturnAsync(id);

			if (!success)
				return NotFound();

			return NoContent(); // 204
		}

	}
}
