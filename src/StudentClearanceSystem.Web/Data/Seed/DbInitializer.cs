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
        var configuration = services.GetRequiredService<IConfiguration>();
        var environment = services.GetRequiredService<IHostEnvironment>();

        await context.Database.MigrateAsync();

        foreach (var role in new[] { Roles.Admin, Roles.Student })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = configuration["InitialAdmin:Email"] ?? "admin@university.edu";
        var adminPassword = configuration["InitialAdmin:Password"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Production deployment requires 'InitialAdmin:Password' configuration. " +
                    "Set the environment variable InitialAdmin_Password (or InitialAdmin:Password in appsettings.json).");
            }

            adminPassword = "Admin@12345";
        }

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

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException(
                    $"Failed to create the initial administrator account: {errors}");
            }

            var roleResult = await userManager.AddToRoleAsync(adminUser, Roles.Admin);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException(
                    $"Failed to assign the administrator role: {errors}");
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
