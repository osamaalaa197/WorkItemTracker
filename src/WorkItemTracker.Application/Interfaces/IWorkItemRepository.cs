using WorkItemTracker.Application.DTOs;
using WorkItemTracker.Domain.Entities;

namespace WorkItemTracker.Application.Interfaces;

public interface IWorkItemRepository
{
    Task<WorkItem?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<(IReadOnlyList<WorkItem> Items, int TotalCount)> SearchAsync(WorkItemQuery query, CancellationToken ct = default);

    Task AddAsync(WorkItem item, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
