using Microsoft.EntityFrameworkCore;
using MyWorkTracker.ApiService.Data;
using MyWorkTracker.ApiService.Models;
using MyWorkTracker.ApiService.Services;

namespace MyWorkTracker.ApiService.Endpoints;

public static class RepoEndpoints
{
    public static void MapRepoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/repos");

        group.MapGet("/", async (TrackerDbContext db) =>
            await db.Repos.Where(r => r.IsActive).ToListAsync());

        group.MapPost("/", async (TrackedRepo repo, TrackerDbContext db) =>
        {
            db.Repos.Add(repo);
            await db.SaveChangesAsync();
            return Results.Created($"/api/repos/{repo.Id}", repo);
        });

        group.MapDelete("/{id:int}", async (int id, TrackerDbContext db) =>
        {
            var repo = await db.Repos.FindAsync(id);
            if (repo is null) return Results.NotFound();
            repo.IsActive = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

public static class PrEndpoints
{
    public static void MapPrEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/prs");

        group.MapGet("/", async (TrackerDbContext db) =>
            await db.PullRequests
                .Where(p => p.Status == PrStatus.Open || p.Status == PrStatus.ReviewRequested)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync());

        group.MapGet("/by-repo/{repoId:int}", async (int repoId, TrackerDbContext db) =>
            await db.PullRequests.Where(p => p.RepoId == repoId).ToListAsync());
    }
}

public static class WorkItemEndpoints
{
    public static void MapWorkItemEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/workitems");

        group.MapGet("/", async (TrackerDbContext db) =>
            await db.WorkItems.OrderByDescending(w => w.UpdatedAt).ToListAsync());

        group.MapGet("/board", async (TrackerDbContext db) =>
        {
            var items = await db.WorkItems
                .Where(w => w.Status != WorkItemStatus.Done && w.Status != WorkItemStatus.Cancelled)
                .ToListAsync();

            return items.GroupBy(w => w.Status)
                .ToDictionary(g => g.Key.ToString(), g => g.ToList());
        });

        group.MapPost("/", async (CreateWorkItemRequest request, TrackerDbContext db, IConfiguration config) =>
        {
            // Create locally
            var item = new WorkItem
            {
                Title = request.Title,
                Description = request.Description,
                ExternalId = request.ExternalId ?? $"local-{Guid.NewGuid():N}",
                Url = request.Url ?? "",
                Status = request.Status ?? WorkItemStatus.Todo,
                Priority = request.Priority ?? WorkItemPriority.Medium,
                DueDate = request.DueDate,
                EpicId = request.EpicId,
                RepoId = request.RepoId,
                Tags = request.Tags ?? [],
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow
            };

            // If a repo is specified, create a GitHub issue and assign to self
            if (request.RepoId.HasValue && request.CreateInRemote)
            {
                var repo = await db.Repos.FindAsync(request.RepoId.Value);
                if (repo is not null && repo.Platform == "GitHub")
                {
                    var ghToken = config["GitHub:Token"];
                    if (!string.IsNullOrEmpty(ghToken))
                    {
                        using var http = new HttpClient();
                        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {ghToken}");
                        http.DefaultRequestHeaders.Add("User-Agent", "MyWorkTracker");
                        http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

                        var issuePayload = new
                        {
                            title = request.Title,
                            body = request.Description ?? "",
                            assignees = new[] { config["GitHub:Username"] ?? "" },
                            labels = request.Tags ?? []
                        };

                        var response = await http.PostAsJsonAsync(
                            $"https://api.github.com/repos/{repo.Owner}/{repo.Name}/issues",
                            issuePayload);

                        if (response.IsSuccessStatusCode)
                        {
                            var created = await response.Content.ReadFromJsonAsync<GitHubIssueResponse>();
                            if (created is not null)
                            {
                                item.ExternalId = created.Number.ToString();
                                item.Url = created.HtmlUrl;
                            }
                        }
                    }
                }
            }

            db.WorkItems.Add(item);
            await db.SaveChangesAsync();
            return Results.Created($"/api/workitems/{item.Id}", item);
        });

        group.MapPut("/{id:int}/status", async (int id, WorkItemStatus status, TrackerDbContext db) =>
        {
            var item = await db.WorkItems.FindAsync(id);
            if (item is null) return Results.NotFound();
            item.Status = status;
            item.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(item);
        });
    }
}

public static class EpicEndpoints
{
    public static void MapEpicEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/epics");

        group.MapGet("/", async (TrackerDbContext db) =>
            await db.Epics.Include(e => e.WorkItems).ToListAsync());

        group.MapPost("/", async (Epic epic, TrackerDbContext db, AzureDevOpsSyncService azDoSync) =>
        {
            db.Epics.Add(epic);
            await db.SaveChangesAsync();

            // Auto-sync child work items from AzDO if URL is provided
            if (!string.IsNullOrWhiteSpace(epic.Url) && epic.Url.Contains("dev.azure.com"))
            {
                await azDoSync.SyncEpicWorkItemsAsync(epic, db);
            }

            // Reload with work items
            var result = await db.Epics.Include(e => e.WorkItems).FirstOrDefaultAsync(e => e.Id == epic.Id);
            return Results.Created($"/api/epics/{epic.Id}", result);
        });

        group.MapPost("/{id:int}/sync", async (int id, TrackerDbContext db, AzureDevOpsSyncService azDoSync) =>
        {
            var epic = await db.Epics.Include(e => e.WorkItems).FirstOrDefaultAsync(e => e.Id == id);
            if (epic is null) return Results.NotFound();

            var synced = await azDoSync.SyncEpicWorkItemsAsync(epic, db);
            return Results.Ok(new { syncedCount = synced.Count, workItems = synced });
        });

        group.MapGet("/{id:int}/next", async (int id, TrackerDbContext db, PrioritizationService prioritizer) =>
        {
            var epic = await db.Epics.Include(e => e.WorkItems).FirstOrDefaultAsync(e => e.Id == id);
            if (epic is null) return Results.NotFound();

            var next = prioritizer.GetNextItem(epic.WorkItems);
            return next is null ? Results.NoContent() : Results.Ok(next);
        });

        group.MapGet("/{id:int}/prioritized", async (int id, TrackerDbContext db, PrioritizationService prioritizer) =>
        {
            var epic = await db.Epics.Include(e => e.WorkItems).FirstOrDefaultAsync(e => e.Id == id);
            if (epic is null) return Results.NotFound();

            return Results.Ok(prioritizer.GetPrioritizedItems(epic.WorkItems));
        });
    }
}

public static class AgentEndpoints
{
    public static void MapAgentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/agents");

        group.MapGet("/", async (TrackerDbContext db) =>
            await db.Agents.ToListAsync());

        group.MapPost("/", async (TrackedAgent agent, TrackerDbContext db) =>
        {
            db.Agents.Add(agent);
            await db.SaveChangesAsync();
            return Results.Created($"/api/agents/{agent.Id}", agent);
        });

        group.MapGet("/by-type/{type}", async (string type, TrackerDbContext db) =>
            await db.Agents.Where(a => a.Type == type).ToListAsync());

        group.MapPut("/{id:int}", async (int id, TrackedAgent updated, TrackerDbContext db) =>
        {
            var agent = await db.Agents.FindAsync(id);
            if (agent is null) return Results.NotFound();
            agent.Name = updated.Name;
            agent.Description = updated.Description;
            agent.Version = updated.Version;
            agent.Status = updated.Status;
            agent.Metadata = updated.Metadata;
            await db.SaveChangesAsync();
            return Results.Ok(agent);
        });
    }
}
