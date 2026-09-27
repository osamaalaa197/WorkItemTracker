using Microsoft.EntityFrameworkCore;
using WorkItemTracker.Application.DTOs;
using WorkItemTracker.Application.Interfaces;
using WorkItemTracker.Domain.Entities;
using WorkItemTracker.Infrastructure.Data;

namespace WorkItemTracker.Infrastructure.Repositories;

public class WorkItemRepository : IWorkItemRepository
{
    private readonly AppDbContext _context;

    public WorkItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<WorkItem?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.WorkItems.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<(IReadOnlyList<WorkItem> Items, int TotalCount)> SearchAsync(WorkItemQuery query, CancellationToken ct = default)
    {
        var items = _context.WorkItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            items = items.Where(w => EF.Functions.Like(w.Title, $"%{term}%"));
        }

        if (query.Status.HasValue)
        {
            items = items.Where(w => w.Status == query.Status.Value);
        }

        var totalCount = await items.CountAsync(ct);

        var page = await items
            .OrderByDescending(w => w.CreatedAt)
            .ThenByDescending(w => w.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (page, totalCount);
    }

    public async Task AddAsync(WorkItem item, CancellationToken ct = default) =>
        await _context.WorkItems.AddAsync(item, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
