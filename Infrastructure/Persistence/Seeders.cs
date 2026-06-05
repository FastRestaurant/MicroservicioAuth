using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence
{
    public static class Seeders
    {
     
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            await SeedRolesAsync(serviceProvider);
            await SeedAdminAsync(serviceProvider);
        }

        private static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles =
            {
                "Admin",
                "Waitress",
                "Kitchen",
                "Cashier"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            var admin = await userManager.FindByNameAsync("admin");

            if (admin == null)
            {
                admin = new AppUser
                {
                    UserName = "admin",
                    Email = "admin@admin.com",
                    FirstName = "System",
                    LastName = "Administrator",
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(admin, "Admin123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}
