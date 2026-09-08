using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScheduledReminders.Api.Auth;
using ScheduledReminders.Api.Data;
using ScheduledReminders.Api.Models;

namespace ScheduledReminders.Api.Services;

public interface IReminderExecutionService
{
    Task RecoverStaleRunningAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClaimedExecution>> ClaimDueRemindersAsync(CancellationToken cancellationToken = default);
    Task RunSimulatedExecutionAsync(Guid reminderId, Guid executionId, CancellationToken cancellationToken = default);
}

public class ReminderExecutionService : IReminderExecutionService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReminderExecutionService> _logger;
    private readonly TimeSpan _simulationDelay;
    private readonly TimeSpan _staleThreshold;

    public ReminderExecutionService(
        AppDbContext db,
        IOptions<ReminderProcessorOptions> options,
        ILogger<ReminderExecutionService> logger)
    {
        _db = db;
        _logger = logger;
        var delaySeconds = Math.Max(1, options.Value.SimulationDelaySeconds);
        _simulationDelay = TimeSpan.FromSeconds(delaySeconds);

        var configuredStaleSeconds = options.Value.StaleRunningThresholdSeconds > 0
            ? options.Value.StaleRunningThresholdSeconds
            : delaySeconds + 20;
        // Never treat an in-flight simulated send as stale.
        _staleThreshold = TimeSpan.FromSeconds(Math.Max(configuredStaleSeconds, delaySeconds + 1));
    }

    public async Task RecoverStaleRunningAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - _staleThreshold;

        var staleExecutions = await _db.ReminderExecutions
            .Include(e => e.Reminder)
            .Where(e =>
                e.Status == ReminderStatus.Running &&
                e.CompletedAt == null &&
                e.StartedAt <= cutoff)
            .ToListAsync(cancellationToken);

        if (staleExecutions.Count == 0)
        {
            return;
        }

        foreach (var execution in staleExecutions)
        {
            execution.Status = ReminderStatus.Failed;
            execution.CompletedAt = now;

            if (execution.Reminder.Status == ReminderStatus.Running)
            {
                execution.Reminder.Status = ReminderStatus.Pending;
                execution.Reminder.UpdatedAt = now;
            }

            _logger.LogWarning(
                "Recovered stale running execution {ExecutionId} for reminder {ReminderId}. Reminder returned to Pending without changing FutureRunsCount.",
                execution.Id,
                execution.ReminderId);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClaimedExecution>> ClaimDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dueReminders = await _db.Reminders
            .Where(r =>
                r.IsActive &&
                r.Status == ReminderStatus.Pending &&
                r.ScheduledAt <= now &&
                r.FutureRunsCount > 0)
            .OrderBy(r => r.ScheduledAt)
            .ToListAsync(cancellationToken);

        var claimed = new List<ClaimedExecution>();

        foreach (var reminder in dueReminders)
        {
            try
            {
                var startedAt = DateTime.UtcNow;
                reminder.Status = ReminderStatus.Running;
                reminder.UpdatedAt = startedAt;

                var execution = new ReminderExecution
                {
                    Id = Guid.NewGuid(),
                    ReminderId = reminder.Id,
                    StartedAt = startedAt,
                    CompletedAt = null,
                    Status = ReminderStatus.Running
                };

                _db.ReminderExecutions.Add(execution);
                await _db.SaveChangesAsync(cancellationToken);
                claimed.Add(new ClaimedExecution(reminder.Id, execution.Id));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to claim reminder {ReminderId}. Continuing with remaining reminders.", reminder.Id);
            }
        }

        return claimed;
    }

    public async Task RunSimulatedExecutionAsync(
        Guid reminderId,
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(_simulationDelay, cancellationToken);

        var completedAt = DateTime.UtcNow;
        var result = Random.Shared.Next(2) == 0
            ? ReminderStatus.Success
            : ReminderStatus.Failed;

        var reminder = await _db.Reminders.SingleOrDefaultAsync(r => r.Id == reminderId, cancellationToken);
        var execution = await _db.ReminderExecutions.SingleOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (reminder is null || execution is null)
        {
            _logger.LogWarning(
                "Skipping completion for reminder {ReminderId} execution {ExecutionId}; record was not found.",
                reminderId,
                executionId);
            return;
        }

        execution.CompletedAt = completedAt;
        execution.Status = result;
        ApplyReminderOutcome(reminder, result, completedAt);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Reminder {ReminderId} execution {ExecutionId} completed as {Status}. Remaining runs: {Remaining}. Next: {Next}.",
            reminder.Id,
            execution.Id,
            result,
            reminder.FutureRunsCount,
            reminder.Status == ReminderStatus.Pending ? reminder.ScheduledAt : (DateTime?)null);
    }

    private static void ApplyReminderOutcome(Reminder reminder, ReminderStatus result, DateTime completedAt)
    {
        if (reminder.Frequency == Frequency.Once)
        {
            reminder.FutureRunsCount = 0;
            reminder.IsActive = false;
            reminder.Status = result;
            reminder.UpdatedAt = completedAt;
            return;
        }

        reminder.FutureRunsCount = Math.Max(0, reminder.FutureRunsCount - 1);

        if (reminder.FutureRunsCount > 0)
        {
            reminder.ScheduledAt = NextRunUtc(reminder.ScheduledAt, reminder.Frequency);
            reminder.Status = ReminderStatus.Pending;
            reminder.IsActive = true;
        }
        else
        {
            reminder.IsActive = false;
            reminder.Status = result;
        }

        reminder.UpdatedAt = completedAt;
    }

    private static DateTime NextRunUtc(DateTime scheduledAt, Frequency frequency)
    {
        var utc = scheduledAt.Kind == DateTimeKind.Utc
            ? scheduledAt
            : DateTime.SpecifyKind(scheduledAt, DateTimeKind.Utc);

        return frequency switch
        {
            Frequency.Daily => utc.AddDays(1),
            Frequency.Weekly => utc.AddDays(7),
            Frequency.Monthly => utc.AddMonths(1),
            _ => utc
        };
    }
}
