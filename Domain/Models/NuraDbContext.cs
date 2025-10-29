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

    }
}
