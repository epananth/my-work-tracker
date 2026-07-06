using MyWorkTracker.ApiService.Models;

namespace MyWorkTracker.ApiService.Services;

/// <summary>
/// Determines the next most important work item based on priority scoring.
/// Factors: deadline proximity, blocking others, priority level, staleness.
/// </summary>
public class PrioritizationService
{
    public IReadOnlyList<WorkItem> GetPrioritizedItems(IEnumerable<WorkItem> items)
    {
        return items
            .Where(w => w.Status is WorkItemStatus.Todo or WorkItemStatus.InProgress or WorkItemStatus.Backlog)
            .OrderByDescending(ScoreItem)
            .ToList();
    }

    public WorkItem? GetNextItem(IEnumerable<WorkItem> items)
    {
        return GetPrioritizedItems(items).FirstOrDefault();
    }

    private double ScoreItem(WorkItem item)
    {
        double score = 0;

        // Priority weight
        score += item.Priority switch
        {
            WorkItemPriority.Critical => 100,
            WorkItemPriority.High => 70,
            WorkItemPriority.Medium => 40,
            WorkItemPriority.Low => 10,
            _ => 0
        };

        // Deadline urgency (higher score for closer deadlines)
        if (item.DueDate.HasValue)
        {
            var daysUntilDue = (item.DueDate.Value - DateTime.UtcNow).TotalDays;
            if (daysUntilDue <= 0) score += 200; // overdue
            else if (daysUntilDue <= 3) score += 150;
            else if (daysUntilDue <= 7) score += 80;
            else if (daysUntilDue <= 14) score += 40;
        }

        // Already in progress gets a boost (finish what you started)
        if (item.Status == WorkItemStatus.InProgress)
            score += 30;

        // Blocking others multiplier
        score += item.BlockedByIds.Count == 0 ? 20 : -10; // unblocked items preferred

        // Staleness (items not updated in a while need attention)
        var daysSinceUpdate = (DateTime.UtcNow - item.UpdatedAt).TotalDays;
        if (daysSinceUpdate > 7) score += 15;

        return score;
    }
}
