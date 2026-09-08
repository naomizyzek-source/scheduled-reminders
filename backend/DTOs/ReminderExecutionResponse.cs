using ScheduledReminders.Api.Models;

namespace ScheduledReminders.Api.DTOs;

public class ReminderExecutionResponse
{
    public Guid Id { get; set; }
    public Guid ReminderId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ReminderStatus Status { get; set; }

    public static ReminderExecutionResponse FromEntity(ReminderExecution execution) => new()
    {
        Id = execution.Id,
        ReminderId = execution.ReminderId,
        StartedAt = execution.StartedAt,
        CompletedAt = execution.CompletedAt,
        Status = execution.Status
    };
}
