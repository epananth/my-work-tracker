namespace MyWorkTracker.Worker;

/// <summary>
/// Background worker that periodically syncs PRs and work items from onboarded repos.
/// Uses GitHub REST API (Octokit) and Azure DevOps API to pull assigned items.
/// </summary>
public class GitHubSyncWorker(ILogger<GitHubSyncWorker> logger) : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Starting sync cycle at: {time}", DateTimeOffset.Now);

            try
            {
                await SyncPullRequestsAsync(stoppingToken);
                await SyncWorkItemsAsync(stoppingToken);
                logger.LogInformation("Sync cycle completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during sync cycle");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }

    private Task SyncPullRequestsAsync(CancellationToken ct)
    {
        // TODO: Implement GitHub PR sync using Octokit
        // 1. Load all active repos from DB
        // 2. For each repo, fetch PRs assigned to the user
        // 3. Upsert into PullRequests table
        logger.LogInformation("Syncing pull requests...");
        return Task.CompletedTask;
    }

    private Task SyncWorkItemsAsync(CancellationToken ct)
    {
        // TODO: Implement work item sync
        // 1. Sync GitHub Issues assigned to user
        // 2. Sync Azure DevOps work items assigned to user
        logger.LogInformation("Syncing work items...");
        return Task.CompletedTask;
    }
}
