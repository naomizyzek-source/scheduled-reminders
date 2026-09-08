using ScheduledReminders.Api.DTOs;
using ScheduledReminders.Api.Models;

namespace ScheduledReminders.Api.Tests;

public class ReminderServiceTests
{
    [Fact]
    public async Task Create_sets_status_to_pending()
    {
        await using var db = TestHost.CreateDb();
        var service = TestHost.CreateReminderService(db);

        var created = await service.CreateAsync(new CreateReminderRequest
        {
            Name = "Standup",
            Message = "Join standup",
            ScheduledAt = DateTime.UtcNow.AddMinutes(5),
            Frequency = Frequency.Once,
            IsActive = true,
            FutureRunsCount = 1
        });

        Assert.Equal(ReminderStatus.Pending, created.Status);
        Assert.Equal(1, created.FutureRunsCount);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task GetExecutions_returns_null_when_reminder_does_not_exist()
    {
        await using var db = TestHost.CreateDb();
        var service = TestHost.CreateReminderService(db);

        var executions = await service.GetExecutionsAsync(Guid.NewGuid());

        Assert.Null(executions);
    }
}
