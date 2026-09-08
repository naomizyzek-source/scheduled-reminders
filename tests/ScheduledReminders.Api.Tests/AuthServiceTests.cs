using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ScheduledReminders.Api.Auth;
using ScheduledReminders.Api.Data;
using ScheduledReminders.Api.DTOs;
using ScheduledReminders.Api.Models;
using ScheduledReminders.Api.Services;

namespace ScheduledReminders.Api.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task Login_returns_token_and_role_for_seeded_users()
    {
        await using var db = TestHost.CreateDb();
        SeedUsers(db);
        var auth = CreateAuthService(db);

        var admin = await auth.LoginAsync(new LoginRequest { Username = "admin", Password = "Admin123!" });
        var viewer = await auth.LoginAsync(new LoginRequest { Username = "viewer", Password = "Viewer123!" });
        var rejected = await auth.LoginAsync(new LoginRequest { Username = "viewer", Password = "wrong" });

        Assert.NotNull(admin);
        Assert.Equal(Roles.Admin, admin!.Role);
        Assert.False(string.IsNullOrWhiteSpace(admin.Token));

        Assert.NotNull(viewer);
        Assert.Equal(Roles.Viewer, viewer!.Role);
        Assert.False(string.IsNullOrWhiteSpace(viewer.Token));

        Assert.Null(rejected);
    }

    private static AuthService CreateAuthService(AppDbContext db)
    {
        var jwt = Options.Create(new JwtOptions
        {
            Issuer = "ScheduledReminders",
            Audience = "ScheduledReminders.Api",
            ExpiryHours = 8,
            Key = "DEV-ONLY-do-not-use-in-production-scheduled-reminders-signing-key"
        });
        return new AuthService(db, jwt);
    }

    private static void SeedUsers(AppDbContext db)
    {
        var hasher = new PasswordHasher<AppUser>();
        var admin = new AppUser { Id = Guid.NewGuid(), Username = "admin", Role = Roles.Admin };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");
        var viewer = new AppUser { Id = Guid.NewGuid(), Username = "viewer", Role = Roles.Viewer };
        viewer.PasswordHash = hasher.HashPassword(viewer, "Viewer123!");
        db.Users.AddRange(admin, viewer);
        db.SaveChanges();
    }
}
