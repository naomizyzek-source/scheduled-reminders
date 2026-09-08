namespace ScheduledReminders.Api.Auth;

public class ReminderProcessorOptions
{
    public const string SectionName = "ReminderProcessor";

    public int PollIntervalSeconds { get; set; } = 1;
    public int SimulationDelaySeconds { get; set; } = 10;
    public int StaleRunningThresholdSeconds { get; set; } = 30;
}
