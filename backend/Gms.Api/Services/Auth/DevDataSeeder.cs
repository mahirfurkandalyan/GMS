using Gms.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services.Auth;

/// <summary>
/// Development-only seeder that sets password hashes for the seeded users. Passwords
/// are never stored in migration source; hashes (non-deterministic salt) are computed
/// here at startup. Idempotent: only fills users whose PasswordHash is empty.
/// Roles/permissions/grants are seeded deterministically via EF HasData.
/// </summary>
public static class DevDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var enabled = config.GetValue("DevSeed:Enabled", true);
        if (!enabled) return;

        var password = config["DevSeed:DefaultPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("DevSeed:DefaultPassword ayarlı değil; kullanıcı parolaları atlanıyor.");
            return;
        }

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        var users = await db.Users.Where(u => u.PasswordHash == string.Empty).ToListAsync();
        if (users.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var user in users)
        {
            user.PasswordHash = passwordService.Hash(user, password);
            user.UpdatedAt = now;
        }
        await db.SaveChangesAsync();
        logger.LogInformation("DevDataSeeder: {Count} kullanıcı için geliştirme parolası ayarlandı.", users.Count);
    }
}
