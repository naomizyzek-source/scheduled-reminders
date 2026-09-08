using Microsoft.EntityFrameworkCore;
using ScheduledReminders.Api.Data;
using ScheduledReminders.Api.Models;

namespace ScheduledReminders.Api.Tests;

public class ReminderExecutionServiceTests
{
    [Fact]
    public async Task Claim_sets_due_pending_reminder_to_running_and_creates_execution()
    {
        await using var db = TestHost.CreateDb();
        var reminder = await SeedReminderAsync(db, Frequency.Once, futureRuns: 1, scheduledAt: DateTime.UtcNow.AddMinutes(-1));
        var service = TestHost.CreateExecutionService(db);

        var claimed = await service.ClaimDueRemindersAsync();

        Assert.Single(claimed);
        Assert.Equal(reminder.Id, claimed[0].ReminderId);

        var stored = await db.Reminders.AsNoTracking().SingleAsync(r => r.Id == reminder.Id);
        Assert.Equal(ReminderStatus.Running, stored.Status);

        var execution = await db.ReminderExecutions.AsNoTracking().SingleAsync();
        Assert.Equal(reminder.Id, execution.ReminderId);
        Assert.Equal(ReminderStatus.Running, execution.Status);
        Assert.Null(execution.CompletedAt);
        Assert.Equal(claimed[0].ExecutionId, execution.Id);
    }

    [Fact]
    public async Task Claim_skips_reminders_that_are_not_due_or_not_pending()
    {
        await using var db = TestHost.CreateDb();
        await SeedReminderAsync(db, Frequency.Once, futureRuns: 1, scheduledAt: DateTime.UtcNow.AddHours(1));
        await SeedReminderAsync(db, Frequency.Daily, futureRuns: 0, scheduledAt: DateTime.UtcNow.AddMinutes(-1));
        var inactive = await SeedReminderAsync(db, Frequency.Daily, futureRuns: 2, scheduledAt: DateTime.UtcNow.AddMinutes(-1));
        inactive.IsActive = false;
        await db.SaveChangesAsync();
        var running = await SeedReminderAsync(db, Frequency.Once, futureRuns: 1, scheduledAt: DateTime.UtcNow.AddMinutes(-1));
        running.Status = ReminderStatus.Running;
        await db.SaveChangesAsync();

        var service = TestHost.CreateExecutionService(db);
        var claimed = await service.ClaimDueRemindersAsync();

        Assert.Empty(claimed);
    }

    [Fact]
    public async Task Completed_execution_is_success_or_failed()
    {
        await using var db = TestHost.CreateDb();
        var reminder = await SeedReminderAsync(db, Frequency.Once, futureRuns: 1, scheduledAt: DateTime.UtcNow.AddMinutes(-1));
        var service = TestHost.CreateExecutionService(db, simulationDelaySeconds: 1);
        var claimed = await service.ClaimDueRemindersAsync();

        await service.RunSimulatedExecutionAsync(claimed[0].ReminderId, claimed[0].ExecutionId);

        var execution = await db.ReminderExecutions.AsNoTracking().SingleAsync();
        Assert.True(execution.Status is ReminderStatus.Success or ReminderStatus.Failed);
        Assert.NotNull(execution.CompletedAt);
    }

    [Fact]
    public async Task Once_reminder_is_exhausted_after_execution()
    {
        await using var db = TestHost.CreateDb();
        var reminder = await SeedReminderAsync(db, Frequency.Once, futureRuns: 1, scheduledAt: DateTime.UtcNow.AddMinutes(-1));
        var originalSchedule = reminder.ScheduledAt;
        var service = TestHost.CreateExecutionService(db, simulationDelaySeconds: 1);
        var claimed = await service.ClaimDueRemindersAsync();

        await service.RunSimulatedExecutionAsync(claimed[0].ReminderId, claimed[0].ExecutionId);

        var stored = await db.Reminders.AsNoTracking().SingleAsync(r => r.Id == reminder.Id);
        Assert.Equal(0, stored.FutureRunsCount);
        Assert.False(stored.IsActive);
        Assert.True(stored.Status is ReminderStatus.Success or ReminderStatus.Failed);
        Assert.Equal(originalSchedule, stored.ScheduledAt);
    }

    [Fact]
    public async Task Daily_reminder_decrements_count_and_reschedules_when_runs_remain()
    {
        await using var db = TestHost.CreateDb();
        var reminder = await SeedReminderAsync(db, Frequency.Daily, futureRuns: 2, scheduledAt: DateTime.UtcNow.AddMinutes(-1));
        var originalSchedule = reminder.ScheduledAt;
        var service = TestHost.CreateExecutionService(db, simulationDelaySeconds: 1);
        var claimed = await service.ClaimDueRemindersAsync();

        await service.RunSimulatedExecutionAsync(claimed[0].ReminderId, claimed[0].ExecutionId);

        var stored = await db.Reminders.AsNoTracking().SingleAsync(r => r.Id == reminder.Id);
        Assert.Equal(1, stored.FutureRunsCount);
        Assert.True(stored.IsActive);
        Assert.Equal(ReminderStatus.Pending, stored.Status);
        Assert.Equal(originalSchedule.AddDays(1), stored.ScheduledAt);
    }

    [Fact]
    public async Task Weekly_and_monthly_advance_scheduled_at_by_existing_rules()
    {
        await using var weeklyDb = TestHost.CreateDb();
        var weekly = await SeedReminderAsync(weeklyDb, Frequency.Weekly, futureRuns: 2, scheduledAt: DateTime.UtcNow.AddMinutes(-1));
        var weeklyStart = weekly.ScheduledAt;
        var weeklyService = TestHost.CreateExecutionService(weeklyDb, simulationDelaySeconds: 1);
        var weeklyClaim = await weeklyService.ClaimDueRemindersAsync();
        await weeklyService.RunSimulatedExecutionAsync(weeklyClaim[0].ReminderId, weeklyClaim[0].ExecutionId);
        var weeklyStored = await weeklyDb.Reminders.AsNoTracking().SingleAsync();
        Assert.Equal(weeklyStart.AddDays(7), weeklyStored.ScheduledAt);
        Assert.Equal(ReminderStatus.Pending, weeklyStored.Status);
        Assert.Equal(1, weeklyStored.FutureRunsCount);

        await using var monthlyDb = TestHost.CreateDb();
        var monthly = await SeedReminderAsync(monthlyDb, Frequency.Monthly, futureRuns: 2, scheduledAt: DateTime.UtcNow.AddMinutes(-1));
        var monthlyStart = monthly.ScheduledAt;
        var monthlyService = TestHost.CreateExecutionService(monthlyDb, simulationDelaySeconds: 1);
        var monthlyClaim = await monthlyService.ClaimDueRemindersAsync();
        await monthlyService.RunSimulatedExecutionAsync(monthlyClaim[0].ReminderId, monthlyClaim[0].ExecutionId);
        var monthlyStored = await monthlyDb.Reminders.AsNoTracking().SingleAsync();
        Assert.Equal(monthlyStart.AddMonths(1), monthlyStored.ScheduledAt);
        Assert.Equal(ReminderStatus.Pending, monthlyStored.Status);
    }

    private static async Task<Reminder> SeedReminderAsync(
        AppDbContext db,
        Frequency frequency,
        int futureRuns,
        DateTime scheduledAt)
    {
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            Name = $"{frequency} reminder",
            Message = "test",
            ScheduledAt = DateTime.SpecifyKind(scheduledAt, DateTimeKind.Utc),
            Frequency = frequency,
            IsActive = true,
            FutureRunsCount = futureRuns,
            Status = ReminderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync();
        return reminder;
    }
}
