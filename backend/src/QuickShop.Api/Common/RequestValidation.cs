using System.ComponentModel.DataAnnotations;

namespace QuickShop.Api.Common;

/// <summary>DataAnnotations-based request validation for minimal API endpoints.</summary>
public static class RequestValidation
{
    /// <summary>
    /// Validates a request DTO with DataAnnotations. Returns false and fills
    /// <paramref name="errors"/> (field → messages) when the DTO is invalid.
    /// </summary>
    public static bool Validate<T>(this T request, out Dictionary<string, string[]> errors)
        where T : class
    {
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        errors = results
            .Where(r => r.MemberNames.Any())
            .ToDictionary(
                r => r.MemberNames.First(),
                r => r.ErrorMessage is null ? new[] { "Invalid value." } : new[] { r.ErrorMessage },
                StringComparer.OrdinalIgnoreCase);

        if (results.Count > 0 && errors.Count == 0)
        {
            errors["request"] = results.Select(r => r.ErrorMessage ?? "Invalid value.").ToArray();
            valid = false;
        }

        return valid;
    }
}

/// <summary>Small string helpers shared across the API.</summary>
public static class Text
{
    /// <summary>Builds a URL-safe slug from arbitrary text.</summary>
    public static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim().ToLowerInvariant();

        var chars = value.Select(c =>
            char.IsLetterOrDigit(c) ? c : (c is ' ' or '-' or '_' ? '-' : '\0')).ToArray();

        // Drop everything that is neither a letter/digit nor a separator — otherwise
        // punctuation would embed NUL characters in the slug and break URLs.
        value = new string(chars).Replace("\0", string.Empty);

        while (value.Contains("--"))
            value = value.Replace("--", "-");

        return value.Trim('-');
    }
}
