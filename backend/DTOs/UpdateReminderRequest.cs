using System.ComponentModel.DataAnnotations;
using ScheduledReminders.Api.Models;

namespace ScheduledReminders.Api.DTOs;

public class UpdateReminderRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    public DateTime? ScheduledAt { get; set; }

    [Required]
    [EnumDataType(typeof(Frequency))]
    public Frequency Frequency { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, int.MaxValue)]
    public int FutureRunsCount { get; set; }
}
