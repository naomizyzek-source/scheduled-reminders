using ScheduledReminders.Api.DTOs;

namespace ScheduledReminders.Api.Services;

public interface IReminderService
{
    Task<IReadOnlyList<ReminderResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ReminderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReminderResponse> CreateAsync(CreateReminderRequest request, CancellationToken cancellationToken = default);
    Task<ReminderResponse?> UpdateAsync(Guid id, UpdateReminderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReminderExecutionResponse>?> GetExecutionsAsync(Guid reminderId, CancellationToken cancellationToken = default);
}
