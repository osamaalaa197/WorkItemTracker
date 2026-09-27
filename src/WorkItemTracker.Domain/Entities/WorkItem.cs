using WorkItemTracker.Domain.Enums;
using WorkItemTracker.Domain.Exceptions;

namespace WorkItemTracker.Domain.Entities;
public class WorkItem
{
    public const int TitleMaxLength = 120;

    public int Id { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public WorkItemStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WorkItem() { }

    public WorkItem(string title, string? description)
    {
        SetTitle(title);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Status = WorkItemStatus.Todo;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("Title is required.");
        }

        title = title.Trim();

        if (title.Length > TitleMaxLength)
        {
            throw new DomainValidationException($"Title must be at most {TitleMaxLength} characters.");
        }

        Title = title;
    }
    public void TransitionTo(WorkItemStatus newStatus)
    {
        var allowed =
            (Status == WorkItemStatus.Todo && newStatus == WorkItemStatus.InProgress) ||
            (Status == WorkItemStatus.InProgress && newStatus == WorkItemStatus.Done);

        if (!allowed)
        {
            throw new InvalidTransitionException(Status, newStatus);
        }

        Status = newStatus;
    }
}
