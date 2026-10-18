namespace PRN232.LMS.Services.Exceptions;

/// <summary>
/// Thrown when authentication fails or a token is missing/invalid.
/// The API layer translates it into HTTP 401 Unauthorized (or 403 for authorization failures).
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
