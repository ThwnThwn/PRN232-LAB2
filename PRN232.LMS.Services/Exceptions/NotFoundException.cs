namespace PRN232.LMS.Services.Exceptions;

/// <summary>
/// Thrown by the Service layer when a resource could not be found.
/// The API layer translates it into HTTP 404 Not Found.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
