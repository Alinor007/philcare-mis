namespace philcare.Api.Features.Programs.Domain;

/// <summary>
/// Shared lifecycle for Project.ImplementationStatus and Activity.ImplementationStatus — both
/// use the same implementation_status lookup vocabulary and the same workflow shape, so both
/// ChangeProjectStatus and ChangeActivityStatus enforce transitions from this one table rather
/// than each hand-rolling their own (mirrors the pattern in ChangeSponsorshipStatusHandler's
/// AllowedTransitions dictionary, generalized from an enum key to the lookup's string codes).
/// </summary>
public static class ImplementationStatusTransitions
{
    public static readonly IReadOnlyDictionary<string, string[]> Allowed = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["PLANNED"] = ["ONGOING", "CANCELLED"],
        ["ONGOING"] = ["COMPLETED", "DELAYED", "ON_HOLD", "CANCELLED"],
        ["DELAYED"] = ["ONGOING", "CANCELLED"],
        ["ON_HOLD"] = ["ONGOING", "CANCELLED"],
        ["COMPLETED"] = [],
        ["CANCELLED"] = []
    };

    public static bool CanTransition(string currentStatus, string requestedStatus) =>
        Allowed.TryGetValue(currentStatus, out var allowedTargets)
        && allowedTargets.Contains(requestedStatus, StringComparer.OrdinalIgnoreCase);
}
