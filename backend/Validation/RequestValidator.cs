using System.ComponentModel.DataAnnotations;

namespace ScheduledReminders.Api.Validation;

public static class RequestValidator
{
    public static IDictionary<string, string[]>? Validate<T>(T instance)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(instance!);
        var isValid = Validator.TryValidateObject(instance!, context, results, validateAllProperties: true);

        if (isValid)
        {
            return null;
        }

        return results
            .SelectMany(result =>
            {
                var names = result.MemberNames.Any() ? result.MemberNames : new[] { string.Empty };
                return names.Select(name => (Name: name, Message: result.ErrorMessage ?? "Invalid value."));
            })
            .GroupBy(x => x.Name)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());
    }
}
