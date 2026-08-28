namespace Keepwise.Application.Common;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

public sealed class AppValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public AppValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]> { ["general"] = [message] };
    }

    public AppValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
