using Microsoft.EntityFrameworkCore;
using ScheduledReminders.Api.Data;
using ScheduledReminders.Api.Models;

namespace ScheduledReminders.Api.Services;

public interface IReminderProcessor
{
    Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default);
}

public class ReminderProcessor : IReminderProcessor
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReminderProcessor> _logger;

    public ReminderProcessor(AppDbContext db, ILogger<ReminderProcessor> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var dueReminders = await _db.Reminders
            .Where(r =>
                r.IsActive &&
                r.FutureRunsCount > 0 &&
                r.Status != ReminderStatus.Running &&
                r.ScheduledAt <= now)
            .OrderBy(r => r.ScheduledAt)
            .ToListAsync(cancellationToken);

        foreach (var reminder in dueReminders)
        {
            await ExecuteReminderAsync(reminder, now, cancellationToken);
        }
    }

    private async Task ExecuteReminderAsync(Reminder reminder, DateTime now, CancellationToken cancellationToken)
    {
        reminder.Status = ReminderStatus.Running;
        reminder.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        // Simulated send only: no email, SMS, or external provider.
        var execution = new ReminderExecution
        {
            Id = Guid.NewGuid(),
            ReminderId = reminder.Id,
            ExecutedAt = now,
            Status = ReminderStatus.Success,
            Detail = $"Simulated send of \"{reminder.Name}\": {reminder.Message}"
        };

        _db.ReminderExecutions.Add(execution);

        reminder.FutureRunsCount = Math.Max(0, reminder.FutureRunsCount - 1);

        var shouldContinue =
            reminder.Frequency != Frequency.Once &&
            reminder.FutureRunsCount > 0;

        if (shouldContinue)
        {
            reminder.ScheduledAt = NextRun(reminder.ScheduledAt, reminder.Frequency);
            reminder.Status = ReminderStatus.Pending;
            reminder.IsActive = true;
        }
        else
        {
            reminder.FutureRunsCount = 0;
            reminder.IsActive = false;
            reminder.Status = ReminderStatus.Success;
        }

        reminder.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Simulated reminder {ReminderId} ({Name}). Remaining runs: {Remaining}. Next: {Next}",
            reminder.Id,
            reminder.Name,
            reminder.FutureRunsCount,
            reminder.IsActive ? reminder.ScheduledAt : (DateTime?)null);
    }

    private static DateTime NextRun(DateTime scheduledAt, Frequency frequency) => frequency switch
    {
        Frequency.Daily => scheduledAt.AddDays(1),
        Frequency.Weekly => scheduledAt.AddDays(7),
        Frequency.Monthly => scheduledAt.AddMonths(1),
        _ => scheduledAt
    };
}
