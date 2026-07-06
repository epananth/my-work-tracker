using System.Net;
using System.Net.Http.Json;
using MyWorkTracker.Web.Models;

namespace MyWorkTracker.Web.Services;

public sealed class WorkTrackerApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<TrackedRepoDto>> GetReposAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<TrackedRepoDto>>("api/repos", cancellationToken) ?? [];

    public async Task<TrackedRepoDto> AddRepoAsync(TrackedRepoDto repo, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/repos", repo, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TrackedRepoDto>(cancellationToken) ?? repo;
    }

    public async Task DeleteRepoAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"api/repos/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<PullRequestDto>> GetPullRequestsAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<PullRequestDto>>("api/prs", cancellationToken) ?? [];

    public async Task<IReadOnlyList<WorkItemDto>> GetWorkItemsAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<WorkItemDto>>("api/workitems", cancellationToken) ?? [];

    public async Task<Dictionary<string, List<WorkItemDto>>> GetWorkItemBoardAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<Dictionary<string, List<WorkItemDto>>>("api/workitems/board", cancellationToken) ?? [];

    public async Task<WorkItemDto?> UpdateWorkItemStatusAsync(int id, WorkItemStatus status, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsync($"api/workitems/{id}/status?status={status}", content: null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkItemDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<EpicDto>> GetEpicsAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<EpicDto>>("api/epics", cancellationToken) ?? [];

    public async Task<WorkItemDto?> GetEpicNextAsync(int epicId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/epics/{epicId}/next", cancellationToken);
        if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkItemDto>(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<WorkItemDto>> GetEpicPrioritizedAsync(int epicId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/epics/{epicId}/prioritized", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<WorkItemDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<TrackedAgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<TrackedAgentDto>>("api/agents", cancellationToken) ?? [];

    public async Task<IReadOnlyList<TrackedAgentDto>> GetAgentsByTypeAsync(string type, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<TrackedAgentDto>>($"api/agents/by-type/{Uri.EscapeDataString(type)}", cancellationToken) ?? [];
}
