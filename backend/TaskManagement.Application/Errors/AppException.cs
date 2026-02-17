namespace TaskManagement.Application.Errors;

/// <summary>
/// Base exception for business rule violations. Caught by middleware and
/// converted to ProblemDetails with appropriate HTTP status codes.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, 404) { }
}

public class ValidationException : AppException
{
    public List<string> Errors { get; }

    public ValidationException(List<string> errors)
        : base(string.Join("; ", errors), 400)
    {
        Errors = errors;
    }

    public ValidationException(string error) : this(new List<string> { error }) { }
}
