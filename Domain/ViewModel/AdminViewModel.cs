using Domain.Models;
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
		[Required]
		[DataType(DataType.Password)]
		public string Password { get; set; } = null!;
		public bool IsActive { get; set; } = true;
		public UserRole role { get; set; }
		public RegisterUser? RegisteredUser { get; set; }
		public List<RegisterUser>? UserList { get; set; }
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
	}
}
