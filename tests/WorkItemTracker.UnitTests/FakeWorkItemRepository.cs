using WorkItemTracker.Application.DTOs;
using WorkItemTracker.Application.Interfaces;
using WorkItemTracker.Domain.Entities;

namespace WorkItemTracker.UnitTests;

public class FakeWorkItemRepository : IWorkItemRepository
{
    private readonly List<WorkItem> _items = new();
    private int _nextId = 1;

    public Task<WorkItem?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(w => w.Id == id));

    public Task<(IReadOnlyList<WorkItem> Items, int TotalCount)> SearchAsync(WorkItemQuery query, CancellationToken ct = default)
    {
        var results = _items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            results = results.Where(w => w.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Status.HasValue)
        {
            results = results.Where(w => w.Status == query.Status.Value);
        }

        var materialized = results.OrderByDescending(w => w.CreatedAt).ToList();
        var total = materialized.Count;
        var page = materialized.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();

        return Task.FromResult<(IReadOnlyList<WorkItem>, int)>((page, total));
    }

    public Task AddAsync(WorkItem item, CancellationToken ct = default)
    {
        typeof(WorkItem).GetProperty(nameof(WorkItem.Id))!
            .SetValue(item, _nextId++); // simulate DB-assigned identity
        _items.Add(item);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
