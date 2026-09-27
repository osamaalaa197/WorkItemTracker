using FluentAssertions;
using WorkItemTracker.Application.DTOs;
using WorkItemTracker.Application.Services;
using WorkItemTracker.Domain.Enums;
using WorkItemTracker.Domain.Exceptions;
using Xunit;

namespace WorkItemTracker.UnitTests;

public class WorkItemServiceTests
{
    private static WorkItemService CreateSut(out FakeWorkItemRepository repo)
    {
        repo = new FakeWorkItemRepository();
        return new WorkItemService(repo);
    }

    [Fact]
    public async Task CreateAsync_persists_item_in_Todo_status()
    {
        var sut = CreateSut(out _);

        var result = await sut.CreateAsync(new CreateWorkItemRequest("Buy milk", "2%"), default);

        result.Status.Should().Be(WorkItemStatus.Todo);
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_with_blank_title_throws_validation_exception()
    {
        var sut = CreateSut(out _);

        var act = () => sut.CreateAsync(new CreateWorkItemRequest("   ", null), default);

        await act.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task ChangeStatusAsync_with_unknown_id_throws_not_found()
    {
        var sut = CreateSut(out _);

        var act = () => sut.ChangeStatusAsync(999, new UpdateStatusRequest(WorkItemStatus.InProgress), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ChangeStatusAsync_allows_Todo_to_InProgress()
    {
        var sut = CreateSut(out _);
        var created = await sut.CreateAsync(new CreateWorkItemRequest("Task", null), default);

        var updated = await sut.ChangeStatusAsync(created.Id, new UpdateStatusRequest(WorkItemStatus.InProgress), default);

        updated.Status.Should().Be(WorkItemStatus.InProgress);
    }

    [Fact]
    public async Task ChangeStatusAsync_rejects_skipping_InProgress()
    {
        var sut = CreateSut(out _);
        var created = await sut.CreateAsync(new CreateWorkItemRequest("Task", null), default);

        var act = () => sut.ChangeStatusAsync(created.Id, new UpdateStatusRequest(WorkItemStatus.Done), default);

        await act.Should().ThrowAsync<InvalidTransitionException>();
    }

    [Fact]
    public async Task SearchAsync_filters_by_title_and_status_and_paginates()
    {
        var sut = CreateSut(out _);
        await sut.CreateAsync(new CreateWorkItemRequest("Fix login bug", null), default);
        await sut.CreateAsync(new CreateWorkItemRequest("Fix logout bug", null), default);
        var third = await sut.CreateAsync(new CreateWorkItemRequest("Write docs", null), default);
        await sut.ChangeStatusAsync(third.Id, new UpdateStatusRequest(WorkItemStatus.InProgress), default);

        var byTitle = await sut.SearchAsync(new WorkItemQuery { Search = "fix" }, default);
        var byStatus = await sut.SearchAsync(new WorkItemQuery { Status = WorkItemStatus.InProgress }, default);
        var page1 = await sut.SearchAsync(new WorkItemQuery { PageSize = 2, Page = 1 }, default);

        byTitle.TotalCount.Should().Be(2);
        byStatus.TotalCount.Should().Be(1);
        byStatus.Items.Single().Title.Should().Be("Write docs");
        page1.Items.Should().HaveCount(2);
        page1.TotalCount.Should().Be(3);
        page1.TotalPages.Should().Be(2);
    }
}
