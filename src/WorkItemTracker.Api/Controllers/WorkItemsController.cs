using Microsoft.AspNetCore.Mvc;
using WorkItemTracker.Application.DTOs;
using WorkItemTracker.Application.Interfaces;

namespace WorkItemTracker.Api.Controllers;

[ApiController]
[Route("api/work-items")]
public class WorkItemsController : ControllerBase
{
    private readonly IWorkItemService _service;

    public WorkItemsController(IWorkItemService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkItemResponse>> Create(
        [FromBody] CreateWorkItemRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(WorkItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkItemResponse>> GetById(int id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WorkItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WorkItemResponse>>> Search(
        [FromQuery] WorkItemQuery query, CancellationToken ct)
    {
        return Ok(await _service.SearchAsync(query, ct));
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(WorkItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkItemResponse>> ChangeStatus(
        int id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var updated = await _service.ChangeStatusAsync(id, request, ct);
        return Ok(updated);
    }
}
