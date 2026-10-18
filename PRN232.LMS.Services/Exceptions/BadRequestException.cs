namespace PRN232.LMS.Services.Exceptions;

/// <summary>
/// Thrown by the Service layer when client input violates a rule.
/// The API layer translates it into HTTP 400 Bad Request.
/// </summary>
public class BadRequestException : Exception
{
    /// <summary>Optional per-field error messages.</summary>
    public IReadOnlyList<string> Errors { get; }

    public BadRequestException(string message, IEnumerable<string>? errors = null) : base(message)
    {
        Errors = errors?.ToList() ?? new List<string>();
    }
}
