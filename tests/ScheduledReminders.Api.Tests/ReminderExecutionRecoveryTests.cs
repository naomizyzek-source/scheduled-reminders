using Microsoft.EntityFrameworkCore;
using ScheduledReminders.Api.Data;
using ScheduledReminders.Api.Models;

namespace ScheduledReminders.Api.Tests;

public class ReminderExecutionRecoveryTests
{
    [Fact]
    public async Task Recover_marks_stale_running_execution_failed_and_returns_reminder_to_pending()
    {
        await using var db = TestHost.CreateDb();
        var scheduledAt = DateTime.UtcNow.AddMinutes(-5);
        var reminder = await SeedRunningAsync(db, Frequency.Daily, futureRuns: 3, scheduledAt, startedAt: DateTime.UtcNow.AddMinutes(-5));
        var service = TestHost.CreateExecutionService(db, simulationDelaySeconds: 1, staleRunningThresholdSeconds: 2);

        await service.RecoverStaleRunningAsync();
        await service.RecoverStaleRunningAsync();

        var storedReminder = await db.Reminders.AsNoTracking().SingleAsync();
        var storedExecution = await db.ReminderExecutions.AsNoTracking().SingleAsync();

        Assert.Equal(ReminderStatus.Failed, storedExecution.Status);
        Assert.NotNull(storedExecution.CompletedAt);
        Assert.Equal(ReminderStatus.Pending, storedReminder.Status);
        Assert.Equal(3, storedReminder.FutureRunsCount);
        Assert.Equal(scheduledAt, storedReminder.ScheduledAt);
        Assert.True(storedReminder.IsActive);
        Assert.Equal(reminder.Id, storedReminder.Id);
    }

    [Fact]
    public async Task Recover_does_not_touch_in_flight_running_executions()
    {
        await using var db = TestHost.CreateDb();
        await SeedRunningAsync(db, Frequency.Once, futureRuns: 1, scheduledAt: DateTime.UtcNow.AddMinutes(-1), startedAt: DateTime.UtcNow);
        var service = TestHost.CreateExecutionService(db, simulationDelaySeconds: 1, staleRunningThresholdSeconds: 30);

        await service.RecoverStaleRunningAsync();

        var storedReminder = await db.Reminders.AsNoTracking().SingleAsync();
        var storedExecution = await db.ReminderExecutions.AsNoTracking().SingleAsync();

        Assert.Equal(ReminderStatus.Running, storedReminder.Status);
        Assert.Equal(ReminderStatus.Running, storedExecution.Status);
        Assert.Null(storedExecution.CompletedAt);
        Assert.Equal(1, storedReminder.FutureRunsCount);
    }

    private static async Task<Reminder> SeedRunningAsync(
        AppDbContext db,
        Frequency frequency,
        int futureRuns,
        DateTime scheduledAt,
        DateTime startedAt)
    {
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            Name = "Stuck",
            Message = "test",
            ScheduledAt = DateTime.SpecifyKind(scheduledAt, DateTimeKind.Utc),
            Frequency = frequency,
            IsActive = true,
            FutureRunsCount = futureRuns,
            Status = ReminderStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.SpecifyKind(startedAt, DateTimeKind.Utc)
        };
        var execution = new ReminderExecution
        {
            Id = Guid.NewGuid(),
            ReminderId = reminder.Id,
            Reminder = reminder,
            StartedAt = DateTime.SpecifyKind(startedAt, DateTimeKind.Utc),
            CompletedAt = null,
            Status = ReminderStatus.Running
        };
        db.Reminders.Add(reminder);
        db.ReminderExecutions.Add(execution);
        await db.SaveChangesAsync();
        return reminder;
    }
}
