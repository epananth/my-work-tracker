using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyWorkTracker.ApiService.Data;
using MyWorkTracker.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace MyWorkTracker.ApiService.Services;

/// <summary>
/// Fetches child work items from an Azure DevOps epic using the WIQL API.
/// Requires a PAT with "Work Items (Read)" scope.
/// </summary>
public class AzureDevOpsSyncService(IConfiguration config, ILogger<AzureDevOpsSyncService> logger)
{
    /// <summary>
    /// Fetches all child work items for a given epic and upserts them into the database.
    /// Epic URL format: https://dev.azure.com/{org}/{project}/_workitems/edit/{id}
    /// </summary>
    public async Task<List<WorkItem>> SyncEpicWorkItemsAsync(Epic epic, TrackerDbContext db, CancellationToken ct = default)
    {
        var pat = config["AzureDevOps:Pat"];
        if (string.IsNullOrEmpty(pat))
        {
            logger.LogWarning("AzureDevOps:Pat not configured. Cannot sync epic work items.");
            return [];
        }

        var parsed = ParseEpicUrl(epic.Url);
        if (parsed is null)
        {
            logger.LogWarning("Could not parse AzDO epic URL: {Url}", epic.Url);
            return [];
        }

        var (org, project, epicId) = parsed.Value;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{pat}")));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Use WIQL to find all children of this epic
        var wiql = new
        {
            query = $@"SELECT [System.Id], [System.Title], [System.State], [Microsoft.VSTS.Common.Priority], [System.AssignedTo], [Microsoft.VSTS.Scheduling.DueDate]
                       FROM WorkItemLinks
                       WHERE ([Source].[System.Id] = {epicId})
                       AND ([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward')
                       AND ([Target].[System.WorkItemType] <> '')
                       MODE (Recursive)"
        };

        var wiqlResponse = await http.PostAsJsonAsync(
            $"https://dev.azure.com/{org}/{project}/_apis/wit/wiql?api-version=7.1",
            wiql, ct);

        if (!wiqlResponse.IsSuccessStatusCode)
        {
            var body = await wiqlResponse.Content.ReadAsStringAsync(ct);
            logger.LogError("WIQL query failed ({Status}): {Body}", wiqlResponse.StatusCode, body);
            return [];
        }

        var wiqlResult = await wiqlResponse.Content.ReadFromJsonAsync<WiqlResponse>(ct);
        if (wiqlResult?.WorkItemRelations is null || wiqlResult.WorkItemRelations.Count == 0)
        {
            logger.LogInformation("No child work items found for epic {EpicId}", epicId);
            return [];
        }

        // Get all target IDs (skip the source which is the epic itself)
        var childIds = wiqlResult.WorkItemRelations
            .Where(r => r.Target is not null && r.Target.Id.ToString() != epicId)
            .Select(r => r.Target!.Id)
            .Distinct()
            .ToList();

        if (childIds.Count == 0) return [];

        // Fetch work item details in batches of 200
        var results = new List<WorkItem>();
        foreach (var batch in childIds.Chunk(200))
        {
            var ids = string.Join(",", batch);
            var detailsResponse = await http.GetAsync(
                $"https://dev.azure.com/{org}/{project}/_apis/wit/workitems?ids={ids}&$expand=none&api-version=7.1", ct);

            if (!detailsResponse.IsSuccessStatusCode) continue;

            var details = await detailsResponse.Content.ReadFromJsonAsync<WorkItemListResponse>(ct);
            if (details?.Value is null) continue;

            foreach (var wi in details.Value)
            {
                var externalId = wi.Id.ToString();
                var existing = await db.WorkItems
                    .FirstOrDefaultAsync(w => w.ExternalId == externalId && w.EpicId == epic.Id, ct);

                var state = MapState(wi.Fields.State);
                var priority = MapPriority(wi.Fields.Priority);

                if (existing is not null)
                {
                    existing.Title = wi.Fields.Title;
                    existing.Status = state;
                    existing.Priority = priority;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.LastSyncedAt = DateTime.UtcNow;
                    results.Add(existing);
                }
                else
                {
                    var newItem = new WorkItem
                    {
                        ExternalId = externalId,
                        Title = wi.Fields.Title,
                        Description = wi.Fields.Description ?? "",
                        Url = $"https://dev.azure.com/{org}/{project}/_workitems/edit/{wi.Id}",
                        Status = state,
                        Priority = priority,
                        EpicId = epic.Id,
                        DueDate = wi.Fields.DueDate,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        LastSyncedAt = DateTime.UtcNow,
                        Tags = []
                    };
                    db.WorkItems.Add(newItem);
                    results.Add(newItem);
                }
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Synced {Count} work items for epic '{Title}'", results.Count, epic.Title);
        return results;
    }

    private static (string org, string project, string epicId)? ParseEpicUrl(string url)
    {
        // Handles: https://dev.azure.com/{org}/{project}/_workitems/edit/{id}
        // Also: https://{org}.visualstudio.com/{project}/_workitems/edit/{id}
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Trim('/').Split('/');

            if (uri.Host == "dev.azure.com" && segments.Length >= 4)
            {
                return (segments[0], segments[1], segments[3]);
            }

            if (uri.Host.EndsWith(".visualstudio.com") && segments.Length >= 3)
            {
                var org = uri.Host.Replace(".visualstudio.com", "");
                return (org, segments[0], segments[2]);
            }
        }
        catch { }

        return null;
    }

    private static WorkItemStatus MapState(string? state) => state?.ToLowerInvariant() switch
    {
        "new" or "proposed" => WorkItemStatus.Backlog,
        "active" or "committed" => WorkItemStatus.InProgress,
        "resolved" => WorkItemStatus.InReview,
        "closed" or "done" or "completed" => WorkItemStatus.Done,
        "removed" or "cut" => WorkItemStatus.Cancelled,
        _ => WorkItemStatus.Todo
    };

    private static WorkItemPriority MapPriority(int? priority) => priority switch
    {
        1 => WorkItemPriority.Critical,
        2 => WorkItemPriority.High,
        3 => WorkItemPriority.Medium,
        _ => WorkItemPriority.Low
    };
}

// AzDO API response models
file class WiqlResponse
{
    [JsonPropertyName("workItemRelations")]
    public List<WorkItemRelation>? WorkItemRelations { get; set; }
}

file class WorkItemRelation
{
    [JsonPropertyName("target")]
    public WorkItemRef? Target { get; set; }
}

file class WorkItemRef
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

file class WorkItemListResponse
{
    [JsonPropertyName("value")]
    public List<AzDoWorkItem>? Value { get; set; }
}

file class AzDoWorkItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("fields")]
    public AzDoFields Fields { get; set; } = new();
}

file class AzDoFields
{
    [JsonPropertyName("System.Title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("System.State")]
    public string? State { get; set; }

    [JsonPropertyName("System.Description")]
    public string? Description { get; set; }

    [JsonPropertyName("Microsoft.VSTS.Common.Priority")]
    public int? Priority { get; set; }

    [JsonPropertyName("Microsoft.VSTS.Scheduling.DueDate")]
    public DateTime? DueDate { get; set; }
}
