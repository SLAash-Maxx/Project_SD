using Microsoft.EntityFrameworkCore;
using BlindMatchPAS.Models;
using Microsoft.AspNetCore.Identity;

namespace BlindMatchPAS.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db          = services.GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync();

        string[] roles = ["Student", "Supervisor", "ModuleLeader", "SystemAdmin"];
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // Create default system admin
        const string adminEmail = "admin@blindmatch.ac.lk";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email    = adminEmail,
                FullName = "System Administrator",
                Role     = "SystemAdmin",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@1234!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "SystemAdmin");
        }

        // Create default module leader
        const string mlEmail = "moduleleader@blindmatch.ac.lk";
        if (await userManager.FindByEmailAsync(mlEmail) is null)
        {
            var ml = new ApplicationUser
            {
                UserName = mlEmail,
                Email    = mlEmail,
                FullName = "Module Leader",
                Role     = "ModuleLeader",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(ml, "Leader@1234!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(ml, "ModuleLeader");
        }
    }
}
