using Microsoft.Extensions.Options;
using ScheduledReminders.Api.Auth;

namespace ScheduledReminders.Api.Services;

public class ReminderProcessorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderProcessorHostedService> _logger;
    private readonly TimeSpan _pollInterval;

    public ReminderProcessorHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<ReminderProcessorOptions> options,
        ILogger<ReminderProcessorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var seconds = Math.Max(1, options.Value.PollIntervalSeconds);
        _pollInterval = TimeSpan.FromSeconds(seconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reminder processor started. Poll interval: {Interval}.", _pollInterval);

        using var timer = new PeriodicTimer(_pollInterval);

        await RunCycleAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCycleAsync(stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        IReadOnlyList<ClaimedExecution> claimed;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var executions = scope.ServiceProvider.GetRequiredService<IReminderExecutionService>();
            await executions.RecoverStaleRunningAsync(stoppingToken);
            claimed = await executions.ClaimDueRemindersAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reminder processor claim cycle failed. Polling will continue.");
            return;
        }

        if (claimed.Count == 0)
        {
            return;
        }

        var tasks = claimed.Select(item => RunClaimedExecutionAsync(item, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task RunClaimedExecutionAsync(ClaimedExecution claimed, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var executions = scope.ServiceProvider.GetRequiredService<IReminderExecutionService>();
            await executions.RunSimulatedExecutionAsync(claimed.ReminderId, claimed.ExecutionId, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Reminder {ReminderId} execution {ExecutionId} cancelled because the application is stopping.",
                claimed.ReminderId,
                claimed.ExecutionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Reminder {ReminderId} execution {ExecutionId} failed unexpectedly. Other reminders will continue.",
                claimed.ReminderId,
                claimed.ExecutionId);
        }
    }
}
