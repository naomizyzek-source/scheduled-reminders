using ScheduledReminders.Api.DTOs;
using ScheduledReminders.Api.Services;
using ScheduledReminders.Api.Validation;

namespace ScheduledReminders.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", Login)
            .WithName("Login")
            .WithTags("Auth")
            .AllowAnonymous()
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> Login(LoginRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        var errors = RequestValidator.Validate(request);
        if (errors is not null)
        {
            return Results.ValidationProblem(errors);
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        return result is null
            ? Results.Unauthorized()
            : Results.Ok(result);
    }
}
