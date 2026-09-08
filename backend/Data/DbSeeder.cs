using Microsoft.AspNetCore.Identity;
using ScheduledReminders.Api.Models;

namespace ScheduledReminders.Api.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Users.Any())
        {
            return;
        }

        var hasher = new PasswordHasher<AppUser>();

        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Role = Roles.Admin
        };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");

        var viewer = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "viewer",
            Role = Roles.Viewer
        };
        viewer.PasswordHash = hasher.HashPassword(viewer, "Viewer123!");

        db.Users.AddRange(admin, viewer);
        db.SaveChanges();
    }
}
