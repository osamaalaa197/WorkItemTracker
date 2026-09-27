using FluentAssertions;
using WorkItemTracker.Domain.Entities;
using WorkItemTracker.Domain.Enums;
using WorkItemTracker.Domain.Exceptions;
using Xunit;

namespace WorkItemTracker.UnitTests;

public class WorkItemTests
{
    [Fact]
    public void New_item_starts_in_Todo()
    {
        var item = new WorkItem("Write tests", null);

        item.Status.Should().Be(WorkItemStatus.Todo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_rejects_missing_title(string? title)
    {
        var act = () => new WorkItem(title!, null);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Constructor_rejects_title_over_120_characters()
    {
        var tooLong = new string('a', 121);

        var act = () => new WorkItem(tooLong, null);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Constructor_accepts_title_at_exactly_120_characters()
    {
        var maxLength = new string('a', 120);

        var item = new WorkItem(maxLength, null);

        item.Title.Should().HaveLength(120);
    }

    [Theory]
    [InlineData(WorkItemStatus.Todo, WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Done)]
    public void Valid_forward_transitions_are_allowed(WorkItemStatus from, WorkItemStatus to)
    {
        var item = new WorkItem("Task", null);
        if (from == WorkItemStatus.InProgress)
        {
            item.TransitionTo(WorkItemStatus.InProgress);
        }

        item.TransitionTo(to);

        item.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(WorkItemStatus.Todo, WorkItemStatus.Done)]        // skips a step
    [InlineData(WorkItemStatus.Todo, WorkItemStatus.Todo)]        // no-op re-application
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Todo)]  // backwards
    [InlineData(WorkItemStatus.Done, WorkItemStatus.Todo)]        // backwards from terminal state
    [InlineData(WorkItemStatus.Done, WorkItemStatus.InProgress)]  // backwards from terminal state
    public void Invalid_transitions_throw(WorkItemStatus from, WorkItemStatus to)
    {
        var item = new WorkItem("Task", null);
        AdvanceTo(item, from);

        var act = () => item.TransitionTo(to);

        act.Should().Throw<InvalidTransitionException>();
        item.Status.Should().Be(from, "a rejected transition must not mutate state");
    }

    private static void AdvanceTo(WorkItem item, WorkItemStatus target)
    {
        if (target == WorkItemStatus.Todo) return;

        item.TransitionTo(WorkItemStatus.InProgress);
        if (target == WorkItemStatus.Done)
        {
            item.TransitionTo(WorkItemStatus.Done);
        }
    }
}
