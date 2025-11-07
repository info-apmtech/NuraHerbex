using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Domain.Models
{
    public class NuraDbContext : IdentityDbContext<RegisterUser>
    {
        public NuraDbContext(DbContextOptions<NuraDbContext> options) : base(options) { }
		public DbSet<GST> GSTDetails { get; set; }
		public DbSet<Product> ProductDetails { get; set; }
		public DbSet<Blog> BlogDetails { get; set; }
		public DbSet<BlogCategory> BlogCategoryDetails { get; set; }
		public DbSet<ConsultationBooking> ConsultationBookingDetails { get; set; }
		public DbSet<Ingredient> Ingredients { get; set; }
		public DbSet<IngredientCategory> IngredientCategories { get; set; }
		public DbSet<AddressDetail> AddressDetails { get; set; }
		public DbSet<State> States { get; set; }
		public DbSet<Country> Countries { get; set; }
		public DbSet<PricingPlan> PricingPlans { get; set; }
		public DbSet<CartItem> CartItems { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }


    }
}
