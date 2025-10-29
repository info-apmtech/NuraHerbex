using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
        public bool isActive { get; set; } = true; //need to discuss
        
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
        public string UpdatedBy { get; set; }
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
        public bool isIndianGST { get; set; } = true;
        public string? Remarks { get; set; }
        [NotMapped]
        public string? NMUpdatedBy { get; set; }
    }
    public class Product
    {
        [Key]
        public int Id { get; set; }
        public string ProductName { get; set; }
        public ProductLevels Level { get; set; }
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
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Image1Path { get; set; }
        public string? Image2Path { get; set; }
        public string? ReadTime { get; set; }
        public string? WrittenBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        [Display(Name = "Blog Image Main")]
        public IFormFile? BlogImage1 { get; set; }

        [NotMapped]
        [Display(Name = "Blog Image Side")]
        public IFormFile? BlogImage2 { get; set; }

        public string? BlogCategoryIds { get; set; }

        [NotMapped]
        [Display(Name = "Blog Category")]
        public string[] BlogCategories { get; set; }

        [NotMapped]
        public int PreviousBlogId { get; set; }
        [NotMapped]
        public int NextBlogId { get; set; }
    }

    public class BlogCategory
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
    public class Quiz
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property for related questions
        public List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    }

    public class QuizQuestion
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string QuestionText { get; set; }

        // Navigation property for question's options
        public List<QuizOption> Options { get; set; } = new List<QuizOption>();

        public Quiz Quiz { get; set; }
    }

    public class QuizOption
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string OptionText { get; set; }
        public bool IsCorrect { get; set; } // optional, if you track correct answers

        public QuizQuestion Question { get; set; }
    }

}
