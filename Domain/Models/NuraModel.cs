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
}
