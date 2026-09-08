namespace ScheduledReminders.Api.Models;

public class Reminder
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public Frequency Frequency { get; set; }
    public bool IsActive { get; set; } = true;
    public int FutureRunsCount { get; set; }
    public ReminderStatus Status { get; set; } = ReminderStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ReminderExecution> Executions { get; set; } = new List<ReminderExecution>();
}
