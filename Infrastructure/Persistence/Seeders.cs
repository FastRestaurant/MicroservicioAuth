using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Constants;
using Infrastructure.Identity;
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

            foreach (var role in ApplicationRoles.All)
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
                    FirstName = "Michael",
                    LastName = "London",
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(admin, "Admin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, ApplicationRoles.Admin);
                }
            }
            var firstUser = new AppUser
            {
                UserName = "Ncamarero",
                Email = "nicolas@gmail.com",
                FirstName = "Nicolas",
                LastName = "Salas",
                CreatedAt = DateTime.UtcNow
            };
            var resultCreateOne = await userManager.CreateAsync(firstUser, "Camarero123!");
            if (resultCreateOne.Succeeded)
            {
                await userManager.AddToRoleAsync(firstUser, ApplicationRoles.Waitress);
            }

            var secondUser = new AppUser
            {
                UserName = "Jcocinero",
                Email = "jonylapaz@gmail.com",
                FirstName = "Jonathan",
                LastName = "La Paz",
                CreatedAt = DateTime.UtcNow
            };
            var resultCreateTwo = await userManager.CreateAsync(secondUser, "Cocinero123!");
            if (resultCreateTwo.Succeeded)
            {
                await userManager.AddToRoleAsync(secondUser, ApplicationRoles.Kitchen);
            }

            var thirdUser = new AppUser
            {
                UserName = "Gcajero",
                Email = "gaby@gmail.com",
                FirstName = "Gabriel",
                LastName = "Navarro",
                CreatedAt = DateTime.UtcNow
            };
            var resultCreateThree = await userManager.CreateAsync(thirdUser, "Cajero123!");
            if (resultCreateThree.Succeeded)
            {
                await userManager.AddToRoleAsync(thirdUser, ApplicationRoles.Cashier);
            }

            var fourthUser = new AppUser
            {
                UserName = "Fcajero",
                Email = "fede@gmail.com",
                FirstName = "Federico",
                LastName = "Fradera",
                CreatedAt = DateTime.UtcNow
            };
            var resultCreateFour = await userManager.CreateAsync(fourthUser, "Cajero123!");
            if (resultCreateFour.Succeeded)
            {
                await userManager.AddToRoleAsync(fourthUser, ApplicationRoles.Cashier);
            }

            var fifthUser = new AppUser
            {
                UserName = "Tcamarero",
                Email = "tobias@gmail.com",
                FirstName = "Tobías",
                LastName = "Masksymowicz",
                CreatedAt = DateTime.UtcNow
            };
            var resultCreateFive = await userManager.CreateAsync(fifthUser, "Camarero123!");
            if (resultCreateFive.Succeeded)
            {
                await userManager.AddToRoleAsync(fifthUser, ApplicationRoles.Waitress);
            }

            var sixthUser = new AppUser
            {
                UserName = "Gcocinero",
                Email = "germanV@gmail.com",
                FirstName = "Germán",
                LastName = "Vallejos",
                CreatedAt = DateTime.UtcNow
            };
            var resultCreateSix = await userManager.CreateAsync(sixthUser, "Cocinero123!");
            if (resultCreateSix.Succeeded)
            {
                await userManager.AddToRoleAsync(sixthUser, ApplicationRoles.Kitchen);
            }

            var seventhUser = new AppUser
            {
                UserName = "Ncajero",
                Email = "nahuc@gmail.com",
                FirstName = "Nahuel",
                LastName = "Coronel",
                CreatedAt = DateTime.UtcNow
            };
            var resultCreateSeven = await userManager.CreateAsync(seventhUser, "Cajero123!");
            if (resultCreateSeven.Succeeded)
            {
                await userManager.AddToRoleAsync(seventhUser, ApplicationRoles.Cashier);
            }

            var eighthUser = new AppUser
            {
                UserName = "Jcamarero",
                Email = "rodriguesJ@gmail.com",
                FirstName = "Juan",
                LastName = "Rodrigues",
                CreatedAt = DateTime.UtcNow
            };
            var resultCreateEight = await userManager.CreateAsync(eighthUser, "Camarero123!");
            if (resultCreateEight.Succeeded)
            {
                await userManager.AddToRoleAsync(eighthUser, ApplicationRoles.Waitress);
            }

            var ninethUser = new AppUser
            {
                UserName = "Lcocinero",
                Email = "leonelp@gmail.com",
                FirstName = "Leonel",
                LastName = "Paco",
                CreatedAt = DateTime.UtcNow
            };
            var resultCreateNine = await userManager.CreateAsync(ninethUser, "Cocinero123!");
            if (resultCreateNine.Succeeded)
            {
                await userManager.AddToRoleAsync(ninethUser, ApplicationRoles.Kitchen);
            }

            var newUser = new AppUser
            {
                UserName = "Fcajero",
                Email = "gomezf@gmail.com",
                FirstName = "Franco",
                LastName = "Gomez",
                CreatedAt = DateTime.UtcNow
            };
            var newResultCreate = await userManager.CreateAsync(newUser, "Cajero123!");
            if (newResultCreate.Succeeded)
            {
                await userManager.AddToRoleAsync(newUser, ApplicationRoles.Cashier);
            }
        }
    }
}
