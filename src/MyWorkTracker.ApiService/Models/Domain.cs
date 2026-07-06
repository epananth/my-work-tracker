namespace MyWorkTracker.ApiService.Models;

public class TrackedRepo
{
    public int Id { get; set; }
    public required string Owner { get; set; }
    public required string Name { get; set; }
    public string? Platform { get; set; } = "GitHub"; // GitHub or AzureDevOps
    public DateTime OnboardedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class PullRequest
{
    public int Id { get; set; }
    public int RepoId { get; set; }
    public required string ExternalId { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
    public required string Author { get; set; }
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

public class WorkItem
{
    public int Id { get; set; }
    public int? RepoId { get; set; }
    public int? EpicId { get; set; }
    public required string ExternalId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string Url { get; set; }
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

public class Epic
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string ExternalId { get; set; }
    public required string Url { get; set; }
    public DateTime? TargetDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WorkItem> WorkItems { get; set; } = [];
}

public class TrackedAgent
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; } // Agent, Skill, Plugin
    public string? Description { get; set; }
    public string? RepoUrl { get; set; }
    public string? Version { get; set; }
    public AgentStatus Status { get; set; }
    public DateTime OnboardedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; set; } = [];
}

public enum AgentStatus
{
    Active,
    Development,
    Deprecated,
    Archived
}
