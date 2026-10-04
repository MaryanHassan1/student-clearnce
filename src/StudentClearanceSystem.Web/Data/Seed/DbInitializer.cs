using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Models;

namespace StudentClearanceSystem.Web.Data.Seed;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.MigrateAsync();

        foreach (var role in new[] { Roles.Admin, Roles.Student })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        const string adminEmail = "admin@university.edu";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                MustChangePassword = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
            }
        }

        if (!context.ClearanceDepartments.Any())
        {
            var names = new[]
            {
                "Library", "Finance", "Academic Department", "Student Affairs",
                "Examination Office", "ICT Department", "Hostel"
            };

            foreach (var name in names)
            {
                context.ClearanceDepartments.Add(new ClearanceDepartment
                {
                    Name = name,
                    IsActive = true
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
