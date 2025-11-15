using Domain.Models;
using Domain.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModel
{
    //public class LoginModel
    //{
    //    [Required]
    //    public string Username { get; set; } = null!;
    //    [Required]
    //    [DataType(DataType.Password)]
    //    public string Password { get; set; } = null!;
    //}
    public class LoginResponseModel
    {
        public string Token { get; set; }
        public RegisterUser User { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public DateTime Expiration { get; set; }
        public int AccountId { get; set; }
        public string BasketId { get; set; }
        public string BaseUrl { get; set; }
    }
    public class RegisterUserViewModel
    {
        [Required]
        public string Username { get; set; } = null!;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        public bool IsActive { get; set; } = true;   // "Remember me" / persistent cookie
                                                     //public UserRole? role { get; set; }

        [BindNever][ValidateNever] public RegisterUser? RegisteredUser { get; set; }
        [BindNever][ValidateNever] public List<RegisterUser>? UserList { get; set; }
        [BindNever][ValidateNever] public DateTime? FromDate { get; set; }
        [BindNever][ValidateNever] public DateTime? ToDate { get; set; }
    }
    public enum ForgotFlowStep { Request = 0, Verify = 1 }

    public class ForgotPasswordViewModel : IValidatableObject
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Only required when Step == Verify
        [StringLength(6, MinimumLength = 6)]
        public string? Otp { get; set; }

        public ForgotFlowStep Step { get; set; } = ForgotFlowStep.Request;

        public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
        {
            if (Step == ForgotFlowStep.Verify && string.IsNullOrWhiteSpace(Otp))
            {
                yield return new ValidationResult("OTP is required.", new[] { nameof(Otp) });
            }
        }
    }

    public class ResetPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Keep OTP on the Create Password page too (server will re-verify atomically)
        [Required, StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }


    //public class VerifyOtpViewModel
    //{
    //	[Required, EmailAddress]
    //	public string Email { get; set; }

    //	[Required, StringLength(6, MinimumLength = 6)]
    //	public string Otp { get; set; }
    //}

    //public class ResetPasswordViewModel
    //{
    //	//[Required, EmailAddress]
    //	public string Email { get; set; }
    //	public string Otp { get; set; } = string.Empty; // Used to validate before reset

    //	//[Required]
    //	[StringLength(100, MinimumLength = 6)]
    //	public string NewPassword { get; set; }

    //	//[Required]
    //	[Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    //	public string ConfirmPassword { get; set; }
    //}
    //public class ForgotPasswordViewModel
    //{
    //	// Step 1: Request OTP
    //	[Required(ErrorMessage = "Email is required.")]
    //	[EmailAddress(ErrorMessage = "Invalid email address.")]
    //	public string Email { get; set; } = string.Empty;

    //	// Step 2: Verify OTP
    //	[StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits.")]
    //	public string Otp { get; set; } = string.Empty;

    //	// Step 3: Reset Password
    //	[StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    //	public string NewPassword { get; set; } = string.Empty;

    //	[Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    //	public string ConfirmPassword { get; set; } = string.Empty;

    //}
    public class HomeViewModel
    {
        public List<Blog> BlogList { get; set; } = new List<Blog>();
        public List<Ingredient> Ingredients { get; set; } = new();
        public List<PricingPlan> PlanList { get; set; } = new();
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
        public List<FeedbackViewModel> FeedbackList { get; set; } = new();
        public List<int> WishlistProductIds { get; set; } = new List<int>();
    }
    public class BlogCategoryViewModel
    {
        public List<BlogCategory> CategoryList { get; set; } = new();
        public BlogCategory NewCategory { get; set; } = new();
    }
    public class BlogViewModel
    {
        public Blog NewBlog { get; set; } = new Blog();
        public List<Blog> BlogList { get; set; } = new List<Blog>();
        public List<BlogCategory> Categories { get; set; } = new List<BlogCategory>();
        public IFormFile ImageFile { get; set; }
        public List<int> SelectedCategoryIds { get; set; } = new List<int>();
        public int CategoryId { get; set; }
        public List<DoctorViewModel> Doctors { get; set; }
    }
    public class IngredientViewModel
    {
        public List<IngredientCategory> IngredientCategories { get; set; } = new List<IngredientCategory>();
        public List<Ingredient> IngredientList { get; set; } = new List<Ingredient>();
        public Ingredient NewIngredient { get; set; } = new Ingredient();
        public IFormFile ImageFile { get; set; }
    }
    public class IngredientCategoryViewModel
    {
        public List<IngredientCategory> CategoryList { get; set; } = new List<IngredientCategory>();
        public IngredientCategory NewCategory { get; set; } = new IngredientCategory();
    }
    public class ProductViewModel
    {
        public List<Product> ProductList { get; set; } = new List<Product>();
        public List<FeedbackViewModel> FeedbackList { get; set; } 
        public Product NewProduct { get; set; } = new Product();
        public List<IFormFile> ProductFiles { get; set; } = new();
        public List<GST> GSTDetails { get; set; } = new List<GST>();

    }
    public class GSTViewModel
    {
        public List<GST> GSTList { get; set; } = new();
        public GST NewGST { get; set; } = new();
    }
    public class SubscriptionViewModel
    {
        public List<NewsletterSubscription> SubscriptionList { get; set; } = new List<NewsletterSubscription>();
    }
    public class PaymentGatewayViewModel
    {
        public List<PaymentGatewayDetails> PaymentGatewayList { get; set; } = new List<PaymentGatewayDetails>();
    }
    public sealed class SubscribeRequest
    {
        public string Email { get; set; } = "";
    }

    public class EmailSettings
    {
        public string FromAddress { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool UseSSL { get; set; }
    }


    public class NewsletterSubscriptionResult
    {
        public bool Succeeded { get; set; }
        public string? Error { get; set; }

        public string Email { get; set; } = string.Empty;
        public DateTime SubscribedAtUtc { get; set; }

        // Admin notification
        public string AdminSubject { get; set; } = string.Empty;
        public string AdminBodyText { get; set; } = string.Empty;
        public bool IsNew { get; set; }
        // User auto-reply
        public string UserSubject { get; set; } = string.Empty;
        public string UserBodyHtml { get; set; } = string.Empty;
    }
    public class PricingPlanViewModel
    {
        public List<PricingPlan> PlanList { get; set; } = new List<PricingPlan>();
        public PricingPlan NewPlan { get; set; } = new PricingPlan();
    }
    public class SmsDataSet
    {
        public string UNIQUE_ID { get; set; }
        public string MESSAGE { get; set; }
        public string OA { get; set; }
        public string MSISDN { get; set; }
        public string CHANNEL { get; set; }
        public string CAMPAIGN_NAME { get; set; }
        public string DLT_CT_ID { get; set; }
        public string DLT_PE_ID { get; set; }
        public string DLT_TM_ID { get; set; }
        public string CIRCLE_NAME { get; set; }
        public string USER_NAME { get; set; }
    }
    public class SmsJson
    {
        public string keyword { get; set; }
        public string timestamp { get; set; }
        public List<SmsDataSet> dataSet { get; set; }
    }

    public class UserProfileViewModel
    {
        public AddressDetail AddressDetail { get; set; } = new AddressDetail();
        public List<AddressDetail> Addresses { get; set; } = new List<AddressDetail>();
        public List<Country> Countries { get; set; } = new List<Country>();
        public List<State> States { get; set; } = new List<State>();
    }
    public class ConsultationBookingViewModel
    {
        [Required, MaxLength(100)] public string FirstName { get; set; }
        [Required, MaxLength(100)] public string LastName { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
        [Required, MaxLength(20)] public string Phone { get; set; }
        [Required] public int ConsultationType { get; set; }
        public string PreferredDoctorId { get; set; }
        [Required] public int PreferredTimeSlot { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateOnly PreferredDate { get; set; }
        public string Concerns { get; set; }
        public string Medications { get; set; }
    }
    public class DoctorSpecialityViewModel
    {
        public List<DoctorSpeciality> SpecialityList { get; set; } = new List<DoctorSpeciality>();
        public DoctorSpeciality NewSpeciality { get; set; } = new DoctorSpeciality();
    }
    public class DoctorDetailViewModel
    {
        public DoctorDetail DoctorDetail { get; set; } = new DoctorDetail();
        public List<DoctorDetail> DoctorDetailList { get; set; } = new List<DoctorDetail>();
        public List<RegisterUser> Doctors { get; set; } = new List<RegisterUser>();
        public List<DoctorSpeciality> Specialities { get; set; } = new List<DoctorSpeciality>();
        public SelectList DoctorSelectList => new SelectList(Doctors, "Id", "FullName", DoctorDetail?.DoctorId);
        public IFormFile? PhotoFile { get; set; }

    }
    public class ConsultationPageViewModel
    {
        public ConsultationBookingViewModel BookingModel { get; set; } = new ConsultationBookingViewModel();
        [ValidateNever]
        public DoctorDetailViewModel DoctorDetailsModel { get; set; } = new DoctorDetailViewModel();
    }
    public class MyConsultationViewModel
    {
        public List<ConsultationWithDoctorViewModel> Consultations { get; set; } = new();
    }

    public class ConsultationWithDoctorViewModel
    {
        public ConsultationBooking Consultation { get; set; }
        public RegisterUser Doctor { get; set; }
        public DoctorDetail DoctorDetail { get; set; }
    }
    public class ConsultationWithAssignedDoctorViewModel
    {
        public ConsultationBooking Consultation { get; set; }
        public RegisterUser? Doctor { get; set; }
        public bool CanJoin { get; set; } = false;
    }

    public class ConsultationListViewModel
    {
        public List<ConsultationWithAssignedDoctorViewModel> Consultations { get; set; } = new();
        public string UserRole { get; set; }
    }


    public class DoctorViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string PrimarySpeciality { get; set; }
        public string? PhotoPath { get; set; }
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();

        public decimal SubTotal => Items.Sum(i => i.LineTotal);
    }
    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public int? ProductId { get; set; }

        // product display
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImages { get; set; }

        // cart math
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
    }
    public class ConsultationStatusUpdateModel
    {
        public ConsultationStatus Status { get; set; }
        public string? MeetingLink { get; set; }
    }
    public class FeedbackViewModel
    {
        public int Id { get; set; }
        public int OrderID { get; set; }
        public string CustomerID { get; set; }
        public string CustomerName { get; set; } = "Anonymous"; 
        public int RatingCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string CustomerImage { get; set; } = "/images/default-user.png"; // default image
    }
    public sealed class SubmitFeedbackRequest
    {
        public int OrderId { get; set; }                       // default 1 now
        public string CustomerId { get; set; } = default!;     // default your GUID now
        public int Rating { get; set; }                        // 1..5
        public string? Message { get; set; }
    }
    public class MyOrdersViewModel
    {
        public int OrderId { get; set; }
        public string CustomerId { get; set; }
    }
    public class OrderSummaryViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal SubTotal => Items.Sum(i => i.LineTotal);

        public CartViewModel Cart { get; set; } = new();
        public List<AddressDetail> Addresses { get; set; } = new();
        public int? SelectedAddressId { get; set; }
        // For the dropdown
        public IEnumerable<SelectListItem> AddressItems { get; set; } = Enumerable.Empty<SelectListItem>();
		public Order Order { get; set; } = new();                 // your domain header
		public List<OrderDetail> Details { get; set; } = new();   // domain line items
																  // Optional UI inputs
		public string? DeliveryOption { get; set; }  
		//public string? CouponCode { get; set; }

		// UI-only (server will recompute on POST)
		public decimal? Shipping { get; set; } = 0m;
		public decimal Tax { get; set; } = 0m;
		public decimal TotalDiscount { get; set; } = 0m;
		public decimal? GrandTotal => SubTotal + Shipping + Tax - TotalDiscount;
		// ✅ Use Pincode directly
		public List<Pincode> Pincodes { get; set; } = new();

		// For initial render (selected address’s pincode)
		public decimal? SelectedStandard { get; set; } = 0m;
		public decimal? SelectedExpress { get; set; } = 0m;
		// ---------- NEW: custom address ----------
		public bool UseCustomAddress { get; set; } = false;

		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? StreetAddress { get; set; }
		public string? City { get; set; }
		public string? State { get; set; }
		public string? ZipCode { get; set; }
		public string? Phone { get; set; }
      
        

    }
    public class PincodeViewModel
    {
        public List<Pincode> PincodeList { get; set; } = new();
        public Pincode NewPincode { get; set; } = new();
    }
    public class UpdateOrderStatusRequest
    {
        public int OrderId { get; set; }
        public OrderStatus Status { get; set; }
    }
    public class OrderListViewModel
    {
        public List<Order> Orders { get; set; } = new List<Order>();
        public string UserRole { get; set; } 
    }
    public class PaymentViewModel
    {
        public int OrderId { get; set; }
        public string CustomerId { get; set; }
        public decimal AmountPaise { get;set; }
        public decimal AmountRupees { get; set; }
    }
    public class ProceedToPaymentInput
    {
        public int? SelectedAddressId { get; set; }
        public string? DeliveryOption { get; set; }

        public bool HasPaid { get; set; }
        public string? RazorpayPaymentId { get; set; }
        public string? RazorpayOrderId { get; set; }
        public string? RazorpaySignature { get; set; }
    }

    public class InvoiceOrderSummaryDto
    {
        public InvoiceOrderDto Order { get; set; }
        public List<InvoiceOrderDetailDto> Details { get; set; }
    }

    public class InvoiceOrderDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        // IMPORTANT: Status as string, so any JSON value is accepted
        public string Status { get; set; }

        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Shipping { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal Total { get; set; }

        public int AddressId { get; set; }
        public string DoorNo { get; set; }
        public string PhoneNo { get; set; }
        public string Address { get; set; }
        public int State { get; set; }
        public string PinCode { get; set; }
        public int Country { get; set; }

        public DateTime OrderDate { get; set; }
    }

    public class InvoiceOrderDetailDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // match your API JSON name exactly
        public string productName { get; set; }
    }

    public class TrackOrderViewModel
    {
        public Order? Order { get; set; }
        public string UserRole { get; set; } = "User";
        public List<OrderDetail>? OrderDetails { get; set; } = new List<OrderDetail>();
        public List<Product>? Products { get; set; }


    }


}

