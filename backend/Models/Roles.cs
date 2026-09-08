namespace ScheduledReminders.Api.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Viewer = "Viewer";

    public const string AdminOrViewer = $"{Admin},{Viewer}";
}
