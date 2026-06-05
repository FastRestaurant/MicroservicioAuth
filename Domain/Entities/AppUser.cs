using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
