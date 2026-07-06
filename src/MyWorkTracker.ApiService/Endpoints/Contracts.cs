using MyWorkTracker.ApiService.Models;

namespace MyWorkTracker.ApiService.Endpoints;

public record CreateWorkItemRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? ExternalId { get; init; }
    public string? Url { get; init; }
    public WorkItemStatus? Status { get; init; }
    public WorkItemPriority? Priority { get; init; }
    public DateTime? DueDate { get; init; }
    public int? EpicId { get; init; }
    public int? RepoId { get; init; }
    public List<string>? Tags { get; init; }
    /// <summary>
    /// When true and a RepoId is provided, creates a GitHub Issue and assigns it to you.
    /// </summary>
    public bool CreateInRemote { get; init; } = false;
}

public record GitHubIssueResponse
{
    public int Number { get; init; }
    public string HtmlUrl { get; init; } = "";
}
