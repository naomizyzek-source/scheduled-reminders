using Microsoft.EntityFrameworkCore;
using ScheduledReminders.Api.Models;

namespace ScheduledReminders.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<ReminderExecution> ReminderExecutions => Set<ReminderExecution>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(64).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Message).HasMaxLength(2000).IsRequired();
            entity.Property(r => r.Frequency).HasConversion<string>().HasMaxLength(32);
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasMany(r => r.Executions)
                .WithOne(e => e.Reminder)
                .HasForeignKey(e => e.ReminderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReminderExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(e => e.ReminderId);
            entity.HasIndex(e => e.StartedAt);
        });
    }
}
