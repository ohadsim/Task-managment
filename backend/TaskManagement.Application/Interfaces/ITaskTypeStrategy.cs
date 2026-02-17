namespace TaskManagement.Application.Interfaces;

public interface ITaskTypeStrategy
{
    /// <summary>
    /// The unique type name this strategy handles (e.g., "Procurement", "Development").
    /// </summary>
    string TaskType { get; }

    /// <summary>
    /// The total number of statuses (excluding "Closed"). Procurement=3, Development=4.
    /// </summary>
    int MaxStatus { get; }

    /// <summary>
    /// Returns ordered list of status definitions: { StatusNumber, Label }.
    /// </summary>
    IReadOnlyList<StatusDefinition> GetStatusDefinitions();

    /// <summary>
    /// Validates the custom data for a forward move TO the given target status.
    /// Returns a list of validation errors (empty = valid).
    /// </summary>
    List<string> ValidateStatusData(int targetStatus, Dictionary<string, object> customData);

    /// <summary>
    /// Returns the field definitions required when moving forward TO the given target status.
    /// Used by both backend validation and frontend form rendering.
    /// </summary>
    IReadOnlyList<FieldDefinition> GetRequiredFields(int targetStatus);
}

public class StatusDefinition
{
    public int Status { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class FieldDefinition
{
    public string FieldName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
    public int? ArrayLength { get; set; }
}
