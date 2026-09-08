using ScheduledReminders.Api.Models;

namespace ScheduledReminders.Api.DTOs;

public class ReminderResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public Frequency Frequency { get; set; }
    public bool IsActive { get; set; }
    public int FutureRunsCount { get; set; }
    public ReminderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ReminderResponse FromEntity(Reminder reminder) => new()
    {
        Id = reminder.Id,
        Name = reminder.Name,
        Message = reminder.Message,
        ScheduledAt = reminder.ScheduledAt,
        Frequency = reminder.Frequency,
        IsActive = reminder.IsActive,
        FutureRunsCount = reminder.FutureRunsCount,
        Status = reminder.Status,
        CreatedAt = reminder.CreatedAt,
        UpdatedAt = reminder.UpdatedAt
    };
}
