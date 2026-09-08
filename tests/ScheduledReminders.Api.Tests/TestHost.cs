using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ScheduledReminders.Api.Auth;
using ScheduledReminders.Api.Data;
using ScheduledReminders.Api.Services;

namespace ScheduledReminders.Api.Tests;

internal static class TestHost
{
    public static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static ReminderService CreateReminderService(AppDbContext db) => new(db);

    public static ReminderExecutionService CreateExecutionService(AppDbContext db, int simulationDelaySeconds = 1)
    {
        var options = Options.Create(new ReminderProcessorOptions
        {
            PollIntervalSeconds = 1,
            SimulationDelaySeconds = simulationDelaySeconds
        });
        return new ReminderExecutionService(db, options, NullLogger<ReminderExecutionService>.Instance);
    }
}
