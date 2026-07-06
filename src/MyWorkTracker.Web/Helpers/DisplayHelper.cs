using System.Text.RegularExpressions;
using MyWorkTracker.Web.Models;

namespace MyWorkTracker.Web.Helpers;

public static partial class DisplayHelper
{
    public static string ToDisplayText<TEnum>(TEnum value) where TEnum : struct, Enum => SplitWords(value.ToString());

    public static string ToDisplayText(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : SplitWords(value);

    public static string PullRequestStatusBadge(PrStatus status) => status switch
    {
        PrStatus.Open => "bg-primary-subtle text-primary-emphasis",
        PrStatus.ReviewRequested => "bg-warning-subtle text-warning-emphasis",
        PrStatus.ChangesRequested => "bg-danger-subtle text-danger-emphasis",
        PrStatus.Approved => "bg-success-subtle text-success-emphasis",
        PrStatus.Merged => "bg-info-subtle text-info-emphasis",
        _ => "bg-secondary-subtle text-secondary-emphasis"
    };

    public static string WorkItemStatusBadge(WorkItemStatus status) => status switch
    {
        WorkItemStatus.Backlog => "bg-secondary-subtle text-secondary-emphasis",
        WorkItemStatus.Todo => "bg-primary-subtle text-primary-emphasis",
        WorkItemStatus.InProgress => "bg-warning-subtle text-warning-emphasis",
        WorkItemStatus.InReview => "bg-info-subtle text-info-emphasis",
        WorkItemStatus.Done => "bg-success-subtle text-success-emphasis",
        _ => "bg-dark-subtle text-dark-emphasis"
    };

    public static string WorkItemPriorityBadge(WorkItemPriority priority) => priority switch
    {
        WorkItemPriority.Critical => "bg-danger text-white",
        WorkItemPriority.High => "bg-warning text-dark",
        WorkItemPriority.Medium => "bg-info text-dark",
        _ => "bg-light text-dark border"
    };

    public static string AgentStatusBadge(AgentStatus status) => status switch
    {
        AgentStatus.Active => "bg-success-subtle text-success-emphasis",
        AgentStatus.Development => "bg-warning-subtle text-warning-emphasis",
        AgentStatus.Deprecated => "bg-danger-subtle text-danger-emphasis",
        _ => "bg-secondary-subtle text-secondary-emphasis"
    };

    public static string AgentTypeBadge(string? type) => type?.ToLowerInvariant() switch
    {
        "agent" => "bg-primary-subtle text-primary-emphasis",
        "skill" => "bg-info-subtle text-info-emphasis",
        "plugin" => "bg-dark-subtle text-dark-emphasis",
        _ => "bg-secondary-subtle text-secondary-emphasis"
    };

    public static int PriorityWeight(WorkItemPriority priority) => priority switch
    {
        WorkItemPriority.Critical => 4,
        WorkItemPriority.High => 3,
        WorkItemPriority.Medium => 2,
        _ => 1
    };

    private static string SplitWords(string value) => CamelCaseRegex().Replace(value, "$1 $2");

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex CamelCaseRegex();
}
