namespace ScheduledReminders.Api.Models;

public class ReminderExecution
{
    public Guid Id { get; set; }
    public Guid ReminderId { get; set; }
    public Reminder Reminder { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ReminderStatus Status { get; set; } = ReminderStatus.Running;
}
