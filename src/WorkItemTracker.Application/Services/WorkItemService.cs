using WorkItemTracker.Application.DTOs;
using WorkItemTracker.Application.Interfaces;
using WorkItemTracker.Domain.Entities;
using WorkItemTracker.Domain.Exceptions;

namespace WorkItemTracker.Application.Services;
public class WorkItemService : IWorkItemService
{
    private readonly IWorkItemRepository _repository;

    public WorkItemService(IWorkItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<WorkItemResponse> CreateAsync(CreateWorkItemRequest request, CancellationToken ct = default)
    {
        var item = new WorkItem(request.Title, request.Description);

        await _repository.AddAsync(item, ct);
        await _repository.SaveChangesAsync(ct);

        return ToResponse(item);
    }

    public async Task<WorkItemResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        return item is null ? null : ToResponse(item);
    }

    public async Task<PagedResult<WorkItemResponse>> SearchAsync(WorkItemQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.SearchAsync(query, ct);

        return new PagedResult<WorkItemResponse>(
            items.Select(ToResponse).ToList(),
            totalCount,
            query.Page,
            query.PageSize);
    }

    public async Task<WorkItemResponse> ChangeStatusAsync(int id, UpdateStatusRequest request, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(WorkItem), id);
        item.TransitionTo(request.Status);
        await _repository.SaveChangesAsync(ct);

        return ToResponse(item);
    }

    private static WorkItemResponse ToResponse(WorkItem item) =>
        new(item.Id, item.Title, item.Description, item.Status, item.CreatedAt);
}
