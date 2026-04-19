using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeClock.Models;

namespace TimeClock.Data;

public static class DbSeeder
{
    public static readonly string[] Roles = new[]
    {
        "Admin",
        "Manager",
        "Supervisor",
        "BasicUser"
    };

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.EnsureCreatedAsync();

        // Create roles
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Default pay codes (configurable later in the UI)
        if (!await db.PayCodes.AnyAsync())
        {
            db.PayCodes.AddRange(
                new PayCode { Code = "C", Description = "Hr Reg (Regular Hours)", Multiplier = 1.0m },
                new PayCode { Code = "D", Description = "Hr OT (Overtime)", Multiplier = 1.5m },
                new PayCode { Code = "E", Description = "PTO", Multiplier = 1.0m },
                new PayCode { Code = "H", Description = "Holiday", Multiplier = 1.0m },
                new PayCode { Code = "S", Description = "Sick", Multiplier = 1.0m }
            );
            await db.SaveChangesAsync();
        }

        // Default pay periods
        if (!await db.PayPeriods.AnyAsync())
        {
            db.PayPeriods.AddRange(
                new PayPeriod
                {
                    Name = "Weekly (Sun-Sat)",
                    PeriodType = PayPeriodType.Weekly,
                    AnchorDate = new DateTime(2024, 1, 7), // Sunday
                    OvertimeThresholdHours = 40m,
                    OvertimeMultiplier = 1.5m,
                    IsActive = true
                },
                new PayPeriod
                {
                    Name = "Bi-Weekly",
                    PeriodType = PayPeriodType.BiWeekly,
                    AnchorDate = new DateTime(2024, 1, 7),
                    OvertimeThresholdHours = 40m,
                    OvertimeMultiplier = 1.5m,
                    IsActive = true
                }
            );
            await db.SaveChangesAsync();
        }

        // Default admin account
        const string adminEmail = "admin@timeclock.local";
        const string adminPassword = "Admin123!";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Admin",
                IsSalaried = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Demo employee account
        const string employeeEmail = "employee@timeclock.local";
        var employee = await userManager.FindByEmailAsync(employeeEmail);
        if (employee == null)
        {
            var weeklyPeriod = await db.PayPeriods.FirstOrDefaultAsync(p => p.PeriodType == PayPeriodType.Weekly);
            employee = new ApplicationUser
            {
                UserName = employeeEmail,
                Email = employeeEmail,
                EmailConfirmed = true,
                FirstName = "Demo",
                LastName = "Employee",
                IsSalaried = false,
                IsActive = true,
                PayPeriodId = weeklyPeriod?.Id,
                PtoBalanceHours = 40m,
                PtoAccrualRatePerPeriod = 1.54m, // ~80 hours/year at weekly accrual
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(employee, "Employee123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(employee, "BasicUser");
        }
    }
}
