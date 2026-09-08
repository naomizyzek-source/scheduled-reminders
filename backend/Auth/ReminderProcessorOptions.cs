namespace ScheduledReminders.Api.Auth;

public class ReminderProcessorOptions
{
    public const string SectionName = "ReminderProcessor";

    public int PollIntervalSeconds { get; set; } = 5;
}
