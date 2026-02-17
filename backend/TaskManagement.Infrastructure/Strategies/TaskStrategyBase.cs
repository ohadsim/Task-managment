using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Strategies;

/// <summary>
/// Shared base class for all task type strategies.
/// Contains common validation helpers used across strategy implementations.
/// </summary>
public abstract class TaskStrategyBase : ITaskTypeStrategy
{
    public abstract string TaskType { get; }
    public abstract int MaxStatus { get; }
    public abstract IReadOnlyList<StatusDefinition> GetStatusDefinitions();
    public abstract IReadOnlyList<FieldDefinition> GetRequiredFields(int targetStatus);
    public abstract List<string> ValidateStatusData(int targetStatus, Dictionary<string, object> customData);

    protected static void ValidateRequiredString(
        Dictionary<string, object> data, string key, string label, List<string> errors)
    {
        if (!data.TryGetValue(key, out var value) || value is null)
        {
            errors.Add($"{label} is required.");
            return;
        }
        var str = value.ToString();
        if (string.IsNullOrWhiteSpace(str))
        {
            errors.Add($"{label} cannot be empty.");
        }
    }
}
