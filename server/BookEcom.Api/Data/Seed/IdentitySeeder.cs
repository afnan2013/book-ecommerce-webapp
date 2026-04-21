using BookEcom.Api.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Api.Data.Seed;

public class IdentitySeeder(IServiceProvider services, ILogger<IdentitySeeder> logger) : IHostedService
{
    private const string DefaultAdminEmail = "admin@bookecom.local";
    private const string DefaultAdminPassword = "Admin!ChangeMe1";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            var hasAdmin = await db.Users.AnyAsync(u => u.UserType == UserType.Admin, cancellationToken);
            if (hasAdmin)
            {
                logger.LogInformation("Admin user already exists — skipping seed.");
                return;
            }

            var admin = new AppUser
            {
                UserName = DefaultAdminEmail,
                Email = DefaultAdminEmail,
                FullName = "Default SuperAdmin",
                UserType = UserType.Admin,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(admin, DefaultAdminPassword);
            if (result.Succeeded)
            {
                logger.LogWarning(
                    "Seeded default admin {Email} with placeholder password. CHANGE IT IMMEDIATELY in any non-throwaway environment.",
                    DefaultAdminEmail);
            }
            else
            {
                logger.LogError(
                    "Failed to seed default admin: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "IdentitySeeder failed. Migrations may not have been applied yet — run `dotnet ef database update`.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
