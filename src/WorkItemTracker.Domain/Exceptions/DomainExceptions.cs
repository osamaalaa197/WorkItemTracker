using WorkItemTracker.Domain.Enums;

namespace WorkItemTracker.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.") { }
}

public class InvalidTransitionException : DomainException
{
    public WorkItemStatus From { get; }
    public WorkItemStatus To { get; }

    public InvalidTransitionException(WorkItemStatus from, WorkItemStatus to)
        : base($"Cannot transition from '{from}' to '{to}'. Allowed path is Todo -> InProgress -> Done.")
    {
        From = from;
        To = to;
    }
}
