using Domain.Implementation;
using Domain.Interface;
using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        [AllowAnonymous]
        [HttpGet("pincodes")]
        public async Task<IActionResult> GetPincodes()
        {
            var pincodes = await _adminservice.GetPincodesAsync();
            return Ok(pincodes);
        }

        [AllowAnonymous]
        [HttpGet("pincode/{id}")]
        public async Task<IActionResult> GetPincode(int id)
        {
            var pincode = await _adminservice.GetPincodeByIdAsync(id);
            if (pincode == null)
                return NotFound();

            return Ok(pincode);
        }

        [AllowAnonymous]
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

        [AllowAnonymous]
        [HttpDelete("pincode/{id}")]
        public async Task<IActionResult> DeletePincode(int id)
        {
            var result = await _adminservice.DeletePincodeAsync(id);
            if (result.Succeeded)
                return Ok(new { success = true, message = "Pincode deleted successfully" });

            return BadRequest(result.Errors);
        }
        [AllowAnonymous]
        [HttpPost("orders")]
        public async Task<IActionResult> Create([FromBody] OrderSummaryViewModel vm)
        {
            if (vm == null || vm.Order == null || vm.Details == null || vm.Details.Count == 0)
                return BadRequest("Invalid data.");
            var orderId = await _adminservice.CreateAsync(vm.Order, vm.Details);
            return Ok(new { id = orderId });
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
		// GET /AdminAPI/pincodes/600001
		[HttpGet("{code:length(6)}")]
		public async Task<ActionResult<Pincode>> GetByCode(string code)
		{
			if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^\d{6}$"))
				return BadRequest("PIN must be exactly 6 digits.");

			var pin = await _adminservice.GetPincodeByCodeAsync(code);
			return pin is null ? NotFound() : Ok(pin);
		}
	}
}
