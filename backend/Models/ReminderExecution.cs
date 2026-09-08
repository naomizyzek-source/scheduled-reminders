namespace ScheduledReminders.Api.Models;

public class ReminderExecution
{
    public Guid Id { get; set; }
    public Guid ReminderId { get; set; }
    public DateTime ExecutedAt { get; set; }
    public ReminderStatus Status { get; set; }
    public string Detail { get; set; } = string.Empty;
}
