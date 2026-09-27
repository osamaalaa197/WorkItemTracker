using System.Linq;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WorkItemTracker.Application.DTOs;
using WorkItemTracker.Domain.Enums;
using Xunit;

namespace WorkItemTracker.IntegrationTests;

public class WorkItemsApiTests : IClassFixture<WorkItemApiFactory>
{
    private readonly WorkItemApiFactory _factory;

    public WorkItemsApiTests(WorkItemApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_creates_item_in_Todo_and_returns_201_with_Location()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/work-items",
            new CreateWorkItemRequest("Write integration tests", "Cover the happy paths"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var body = await response.Content.ReadFromJsonAsync<WorkItemResponse>();
        body!.Status.Should().Be(WorkItemStatus.Todo);
        body.Title.Should().Be("Write integration tests");
    }

    [Fact]
    public async Task Post_with_blank_title_returns_400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/work-items",
            new CreateWorkItemRequest("", null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_with_title_over_120_chars_returns_400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/work-items",
            new CreateWorkItemRequest(new string('x', 121), null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_status_on_unknown_id_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync("/api/work-items/999999/status",
            new UpdateStatusRequest(WorkItemStatus.InProgress));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_status_skipping_InProgress_returns_409()
    {
        var client = _factory.CreateClient();
        var created = await CreateItem(client, "Skip-step task");

        var response = await client.PatchAsJsonAsync($"/api/work-items/{created.Id}/status",
            new UpdateStatusRequest(WorkItemStatus.Done));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Patch_status_valid_transition_returns_200_with_new_status()
    {
        var client = _factory.CreateClient();
        var created = await CreateItem(client, "Valid transition task");

        var response = await client.PatchAsJsonAsync($"/api/work-items/{created.Id}/status",
            new UpdateStatusRequest(WorkItemStatus.InProgress));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<WorkItemResponse>();
        body!.Status.Should().Be(WorkItemStatus.InProgress);
    }
    [Fact]
    public async Task Created_item_is_persisted_and_visible_to_a_new_client_instance()
    {
        var firstClient = _factory.CreateClient();
        var created = await CreateItem(firstClient, "Survives a restart");

        var secondClient = _factory.CreateClient(); // fresh HttpClient, same backing store
        var getResponse = await secondClient.GetAsync($"/api/work-items/{created.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<WorkItemResponse>();
        fetched!.Id.Should().Be(created.Id);
        fetched.Title.Should().Be("Survives a restart");
    }

    [Fact]
    public async Task Get_unknown_id_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/work-items/987654");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_supports_title_filter_status_filter_and_pagination()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        await CreateItem(client, $"{unique} alpha");
        await CreateItem(client, $"{unique} beta");
        var inProgress = await CreateItem(client, $"{unique} gamma");
        await client.PatchAsJsonAsync($"/api/work-items/{inProgress.Id}/status",
            new UpdateStatusRequest(WorkItemStatus.InProgress));

        var byTitle = await client.GetFromJsonAsync<PagedResult<WorkItemResponse>>(
            $"/api/work-items?search={unique}&pageSize=50");
        var byStatus = await client.GetFromJsonAsync<PagedResult<WorkItemResponse>>(
            $"/api/work-items?search={unique}&status=InProgress&pageSize=50");
        var firstPage = await client.GetFromJsonAsync<PagedResult<WorkItemResponse>>(
            $"/api/work-items?search={unique}&pageSize=2&page=1");

        byTitle!.TotalCount.Should().Be(3);
        byStatus!.TotalCount.Should().Be(1);
        byStatus.Items.Single().Title.Should().Be($"{unique} gamma");
        firstPage!.Items.Should().HaveCount(2);
        firstPage.TotalCount.Should().Be(3);
    }

    private static async Task<WorkItemResponse> CreateItem(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/work-items", new CreateWorkItemRequest(title, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkItemResponse>())!;
    }
}
