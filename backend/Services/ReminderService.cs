using Microsoft.EntityFrameworkCore;
using ScheduledReminders.Api.Data;
using ScheduledReminders.Api.DTOs;
using ScheduledReminders.Api.Models;

namespace ScheduledReminders.Api.Services;

public class ReminderService : IReminderService
{
    private readonly AppDbContext _db;

    public ReminderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ReminderResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var reminders = await _db.Reminders
            .AsNoTracking()
            .OrderBy(r => r.ScheduledAt)
            .ToListAsync(cancellationToken);

        return reminders.Select(ReminderResponse.FromEntity).ToList();
    }

    public async Task<ReminderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reminder = await _db.Reminders
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

        return reminder is null ? null : ReminderResponse.FromEntity(reminder);
    }

    public async Task<ReminderResponse> CreateAsync(CreateReminderRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Message = request.Message.Trim(),
            ScheduledAt = ToUtc(request.ScheduledAt!.Value),
            Frequency = request.Frequency,
            IsActive = request.IsActive,
            FutureRunsCount = request.FutureRunsCount,
            Status = ReminderStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Reminders.Add(reminder);
        await _db.SaveChangesAsync(cancellationToken);

        return ReminderResponse.FromEntity(reminder);
    }

    public async Task<ReminderResponse?> UpdateAsync(Guid id, UpdateReminderRequest request, CancellationToken cancellationToken = default)
    {
        var reminder = await _db.Reminders.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (reminder is null)
        {
            return null;
        }

        reminder.Name = request.Name.Trim();
        reminder.Message = request.Message.Trim();
        reminder.ScheduledAt = ToUtc(request.ScheduledAt!.Value);
        reminder.Frequency = request.Frequency;
        reminder.IsActive = request.IsActive;
        reminder.FutureRunsCount = request.FutureRunsCount;
        reminder.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ReminderResponse.FromEntity(reminder);
    }

    public async Task<IReadOnlyList<ReminderExecutionResponse>?> GetExecutionsAsync(
        Guid reminderId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Reminders.AnyAsync(r => r.Id == reminderId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var executions = await _db.ReminderExecutions
            .AsNoTracking()
            .Where(e => e.ReminderId == reminderId)
            .OrderByDescending(e => e.ExecutedAt)
            .ToListAsync(cancellationToken);

        return executions.Select(ReminderExecutionResponse.FromEntity).ToList();
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
