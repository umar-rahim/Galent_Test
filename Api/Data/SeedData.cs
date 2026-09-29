using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Api.Models;

namespace Api.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
            var config = provider.GetRequiredService<IConfiguration>();

            string[] roles = new[] { "Admin", "Preparer", "Reviewer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            await EnsureUserAsync(userManager, config, "ADMIN_EMAIL", "ADMIN_PASSWORD", "Admin");
            await EnsureUserAsync(userManager, config, "PREPARER_EMAIL", "PREPARER_PASSWORD", "Preparer");
            await EnsureUserAsync(userManager, config, "REVIEWER_EMAIL", "REVIEWER_PASSWORD", "Reviewer");
        }

        private static async Task EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            string emailKey,
            string passwordKey,
            string role)
        {
            var email = config[emailKey];
            var password = config[passwordKey];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException($"{emailKey} and {passwordKey} must be configured before startup.");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Unable to seed {role}: {string.Join(", ", result.Errors.Select(error => error.Code))}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var result = await userManager.AddToRoleAsync(user, role);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Unable to assign {role} role: {string.Join(", ", result.Errors.Select(error => error.Code))}");
                }
            }
        }
    }
}
