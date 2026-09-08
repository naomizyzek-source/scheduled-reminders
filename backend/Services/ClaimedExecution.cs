namespace ScheduledReminders.Api.Services;

public sealed record ClaimedExecution(Guid ReminderId, Guid ExecutionId);
