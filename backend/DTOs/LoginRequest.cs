using System.ComponentModel.DataAnnotations;

namespace ScheduledReminders.Api.DTOs;

public class LoginRequest
{
    [Required]
    [MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}
