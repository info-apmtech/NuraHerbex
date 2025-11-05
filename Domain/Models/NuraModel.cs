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
        public Specialities? Specialties { get; set; } // time slot need to discuss
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
        //public string? MobileNo { get; set; }
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
        [NotMapped]
        public ICollection<IFormFile>? ProductFiles { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }

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
        public RegisterUser PreferredDoctor { get; set; }

        [Required]
        public TimeSlot PreferredTimeSlot { get; set; }

        public string? Concerns { get; set; }
        public string? Medications { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
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


}
