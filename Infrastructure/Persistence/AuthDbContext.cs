using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;


namespace Infrastructure.Persistence
{
    public class AuthDbContext
        : IdentityDbContext<AppUser>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options)
            : base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}
