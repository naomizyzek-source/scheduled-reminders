using Microsoft.AspNetCore.Http.HttpResults;
using ScheduledReminders.Api.DTOs;
using ScheduledReminders.Api.Models;
using ScheduledReminders.Api.Services;
using ScheduledReminders.Api.Validation;

namespace ScheduledReminders.Api.Endpoints;

public static class ReminderEndpoints
{
    public static IEndpointRouteBuilder MapReminderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reminders")
            .WithTags("Reminders")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetReminders")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Viewer))
            .Produces<IReadOnlyList<ReminderResponse>>();

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetReminderById")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Viewer))
            .Produces<ReminderResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateReminder")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin))
            .Produces<ReminderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateReminder")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin))
            .Produces<ReminderResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ReminderResponse>>> GetAll(
        IReminderService reminderService,
        CancellationToken cancellationToken)
    {
        var reminders = await reminderService.GetAllAsync(cancellationToken);
        return TypedResults.Ok(reminders);
    }

    private static async Task<Results<Ok<ReminderResponse>, NotFound>> GetById(
        Guid id,
        IReminderService reminderService,
        CancellationToken cancellationToken)
    {
        var reminder = await reminderService.GetByIdAsync(id, cancellationToken);
        return reminder is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(reminder);
    }

    private static async Task<Results<Created<ReminderResponse>, ValidationProblem>> Create(
        CreateReminderRequest request,
        IReminderService reminderService,
        CancellationToken cancellationToken)
    {
        var errors = RequestValidator.Validate(request);
        if (errors is not null)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var created = await reminderService.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/reminders/{created.Id}", created);
    }

    private static async Task<Results<Ok<ReminderResponse>, ValidationProblem, NotFound>> Update(
        Guid id,
        UpdateReminderRequest request,
        IReminderService reminderService,
        CancellationToken cancellationToken)
    {
        var errors = RequestValidator.Validate(request);
        if (errors is not null)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var updated = await reminderService.UpdateAsync(id, request, cancellationToken);
        return updated is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(updated);
    }
}
