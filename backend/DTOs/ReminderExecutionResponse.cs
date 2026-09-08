using ScheduledReminders.Api.Models;

namespace ScheduledReminders.Api.DTOs;

public class ReminderExecutionResponse
{
    public Guid Id { get; set; }
    public Guid ReminderId { get; set; }
    public DateTime ExecutedAt { get; set; }
    public ReminderStatus Status { get; set; }
    public string Detail { get; set; } = string.Empty;

    public static ReminderExecutionResponse FromEntity(ReminderExecution execution) => new()
    {
        Id = execution.Id,
        ReminderId = execution.ReminderId,
        ExecutedAt = execution.ExecutedAt,
        Status = execution.Status,
        Detail = execution.Detail
    };
}
