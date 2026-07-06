namespace MyWorkTracker.Web.Models;

public class TrackedRepoDto
{
    public int Id { get; set; }
    public string Owner { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Platform { get; set; } = "GitHub";
    public DateTime OnboardedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PullRequestDto
{
    public int Id { get; set; }
    public int RepoId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public PrStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public enum PrStatus
{
    Open,
    ReviewRequested,
    ChangesRequested,
    Approved,
    Merged,
    Closed
}

public class WorkItemDto
{
    public int Id { get; set; }
    public int? RepoId { get; set; }
    public int? EpicId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public WorkItemStatus Status { get; set; }
    public WorkItemPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<int> BlockedByIds { get; set; } = [];
}

public enum WorkItemStatus
{
    Backlog,
    Todo,
    InProgress,
    InReview,
    Done,
    Cancelled
}

public enum WorkItemPriority
{
    Critical,
    High,
    Medium,
    Low
}

public class EpicDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WorkItemDto> WorkItems { get; set; } = [];
}

public class TrackedAgentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RepoUrl { get; set; }
    public string? Version { get; set; }
    public AgentStatus Status { get; set; }
    public DateTime OnboardedAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = [];
}

public enum AgentStatus
{
    Active,
    Development,
    Deprecated,
    Archived
}
