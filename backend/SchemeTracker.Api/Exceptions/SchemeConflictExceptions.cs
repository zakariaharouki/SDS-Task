namespace SchemeTracker.Api.Exceptions;

/// <summary>Another scheme already has this name and address.</summary>
public sealed class DuplicateSchemeException : Exception
{
    public DuplicateSchemeException(Exception? innerException = null)
        : base("A scheme with this name already exists at this address.", innerException)
    {
    }
}

/// <summary>The scheme changed after the client loaded it.</summary>
public sealed class SchemeVersionConflictException : Exception
{
    public SchemeVersionConflictException()
        : base("This scheme was changed by someone else. Reload to see the latest version.")
    {
    }
}
