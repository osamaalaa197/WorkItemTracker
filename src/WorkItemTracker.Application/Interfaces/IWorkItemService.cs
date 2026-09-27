using WorkItemTracker.Application.DTOs;

namespace WorkItemTracker.Application.Interfaces;

public interface IWorkItemService
{
    Task<WorkItemResponse> CreateAsync(CreateWorkItemRequest request, CancellationToken ct = default);

    Task<WorkItemResponse?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<PagedResult<WorkItemResponse>> SearchAsync(WorkItemQuery query, CancellationToken ct = default);

    Task<WorkItemResponse> ChangeStatusAsync(int id, UpdateStatusRequest request, CancellationToken ct = default);
}
