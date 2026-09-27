using WorkItemTracker.Domain.Enums;

namespace WorkItemTracker.Application.DTOs;

public record CreateWorkItemRequest(string Title, string? Description);

public record UpdateStatusRequest(WorkItemStatus Status);

public record WorkItemResponse(
    int Id,
    string Title,
    string? Description,
    WorkItemStatus Status,
    DateTimeOffset CreatedAt);

public class WorkItemQuery
{
    private const int MaxPageSize = 100;
    private int _page = 1;
    private int _pageSize = 20;

    public string? Search { get; set; }

    public WorkItemStatus? Status { get; set; }

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 1,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
