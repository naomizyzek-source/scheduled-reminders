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

        await ProcessOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(_pollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessOnceAsync(stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IReminderProcessor>();
            await processor.ProcessDueRemindersAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reminder processor cycle failed.");
        }
    }
}
