using Domain.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class RegisterUser : IdentityUser
    {
        public string Password { get; set; }
        [NotMapped]
        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and Confirm Password must match.")]
        [Display(Name = "Confirm Password")]
        public string? NMConfirmPassword { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole Role { get; set; }
        //public Specialities? Specialties { get; set; } // time slot need to discuss
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        [NotMapped]
        public string? NMAdminName { get; set; }
        [NotMapped]
        public string? NMDoctorName { get; set; }
        [NotMapped]
        public string? NMPatient { get; set; }
        [NotMapped]
        public string? NMCountry { get; set; }
        [NotMapped]
        public string? NMState { get; set; }
        [Display(Name = "Country")]
        public int Country { get; set; }
        [Display(Name = "State")]
        public int? State { get; set; }
        [Display(Name = "Address")]
        public string? Address { get; set; }
        [Display(Name = "Pincode")]
        public string? Pincode { get; set; }
        public string? Experience { get; set; }// for doctor
        public bool isActive { get; set; } = true; //need to check user active
        public bool? isWorking { get; set; } = true; // need to check doctor availability
        public string FirstName { get; set; }
        public string? LastName { get; set; }
        [NotMapped]
        public string FullName => string.IsNullOrEmpty(LastName) ? FirstName : $"{FirstName} {LastName}";

        //public string? MobileNo { get; set; }
    }
    public class DoctorSpeciality
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
    public class DoctorDetail
    {
        public int Id { get; set; }
        public string DoctorId { get; set; }
        public string PrimarySpecality { get; set; }
        public string? Remark { get; set; }
        [NotMapped]
        public List<int> SelectedSpecialityIds { get; set; } = new();
        public string? SpecalityIds { get; set; }
        public int YearsOfExperience { get; set; }
        public string? PhotoPath { get; set; }
        public bool IsWorking { get; set; } = true;
        public TimeOnly? MondayStartTime { get; set; }
        public TimeOnly? MondayEndTime { get; set; }
        public TimeOnly? TuesdayStartTime { get; set; }
        public TimeOnly? TuesdayEndTime { get; set; }
        public TimeOnly? WednesdayStartTime { get; set; }
        public TimeOnly? WednesdayEndTime { get; set; }
        public TimeOnly? ThursdayStartTime { get; set; }
        public TimeOnly? ThursdayEndTime { get; set; }
        public TimeOnly? FridayStartTime { get; set; }
        public TimeOnly? FridayEndTime { get; set; }
        public TimeOnly? SaturdayStartTime { get; set; }
        public TimeOnly? SaturdayEndTime { get; set; }
        public TimeOnly? SundayStartTime { get; set; }
        public TimeOnly? SundayEndTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
    public class ConsultationBooking
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; }

        [Required, MaxLength(100)]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MaxLength(20)]
        public string Phone { get; set; }

        [Required]
        public ConsultationType ConsultationType { get; set; }

        public string? PreferredDoctorId { get; set; }
        [Required]
        public TimeSlot PreferredTimeSlot { get; set; }
        public DateOnly PreferredDate { get; set; }

        public string? Concerns { get; set; }
        public string? Medications { get; set; }
        public string CreatedBy { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConsultationStatus Status { get; set; } = ConsultationStatus.Pending;
        public string? MeetingLink { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }
    public class State
    {
        public int Id { get; set; }
        public string StateName { get; set; }
        public string? StateCode { get; set; }
        public int CountryId { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
    public class Country
    {
        public int Id { get; set; }
        public string CountryName { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
    public class AddressDetail
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string DoorNo { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public int State { get; set; }
        public string Pincode { get; set; }
        public int Country { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

    }
    public class PricingPlan
    {
        public int Id { get; set; }
        public string PlanName { get; set; }
        public string PlanSubtitle { get; set; }
        public string PlanDescription { get; set; }
        public decimal PriceAmount { get; set; }
        public string Duration { get; set; }
        public string PlanFeatures { get; set; }
        public bool IsMostPopular { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
    public class CartItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string UserId { get; set; }

    }
    public class WishlistItem
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ProductId { get; set; }
    }
    public class GST
    {
        [Key]
        public int Id { get; set; }
        public string TaxName { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal SGSTPercentage { get; set; }
        public decimal CGSTPercentage { get; set; }
        public decimal IGSTPercentage { get; set; }
        [BindNever]
        public string UpdatedBy { get; set; } = "System";
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
        public string? Remarks { get; set; }
    }
    public class Product
    {
        [Key]
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string? SubTitle { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; } = 0; //For Gst need to discuss
        public int GSTId { get; set; }
        public decimal DiscountPercentage { get; set; } = 0;
        public string? ProductImages { get; set; }
        public string? KeyBenefits1 { get; set; }
        public string? KeyBenefits2 { get; set; }
        public string? KeyBenefits3 { get; set; }
        public string? KeyBenefits4 { get; set; }
        public string? ForThis1 { get; set; }
        public string? ForThis2 { get; set; }
        public string? ForThis3 { get; set; }
        public string? ForThis4 { get; set; }
        [NotMapped]
        public ICollection<IFormFile>? ProductFiles { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public bool ForIndex { get; set; } = false;
        public int? DisplayOrder { get; set; }

    }
    public class Blog
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }
        public string ImagePath { get; set; }
        public string ReadTime { get; set; }
        public string WrittenBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string BlogCategoryIds { get; set; }
        public bool IsFeatured { get; set; }
        public int ReadCount { get; set; } = 0;

    }

    public class BlogCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Ingredient
    {
        public int Id { get; set; }
        public string ImagePath { get; set; }
        public string IngredientName { get; set; }
        public int IngredientCategoryId { get; set; }
        public string? ScientificName { get; set; }
        public string? Benefits { get; set; }
        public string? Evidence { get; set; }
        public string? KnownFor { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public bool ShowHome { get; set; } = true;
    }
    public class IngredientCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class NewsletterSubscription
    {
        public int Id { get; set; }

        public string Email { get; set; } = default!;
        public DateTime SubscribedAt { get; set; }

    }
    public class FeedBack
    {
        public int Id { get; set; }
        public int OrderID { get; set; }
        public string CustomerID { get; set; }
        public int RatingCount { get; set; }
        public string Message { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }
    public class Pincode
    {
        public int Id { get; set; }

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "PIN must be exactly 6 digits.")]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = default!;

        [StringLength(200)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
