using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Strategies;

public class DevelopmentTaskStrategy : TaskStrategyBase
{
    public override string TaskType => "Development";
    public override int MaxStatus => 4;

    public override IReadOnlyList<StatusDefinition> GetStatusDefinitions() => new List<StatusDefinition>
    {
        new() { Status = 1, Label = "Created" },
        new() { Status = 2, Label = "Specification completed" },
        new() { Status = 3, Label = "Development completed" },
        new() { Status = 4, Label = "Distribution completed" }
    };

    public override IReadOnlyList<FieldDefinition> GetRequiredFields(int targetStatus) => targetStatus switch
    {
        2 => new List<FieldDefinition>
        {
            new() { FieldName = "specificationText", Label = "Specification Text", FieldType = "string", Required = true }
        },
        3 => new List<FieldDefinition>
        {
            new() { FieldName = "branchName", Label = "Branch Name", FieldType = "string", Required = true }
        },
        4 => new List<FieldDefinition>
        {
            new() { FieldName = "versionNumber", Label = "Version Number", FieldType = "string", Required = true }
        },
        _ => new List<FieldDefinition>()
    };

    public override List<string> ValidateStatusData(int targetStatus, Dictionary<string, object> customData)
    {
        var errors = new List<string>();

        switch (targetStatus)
        {
            case 2:
                ValidateRequiredString(customData, "specificationText", "Specification Text", errors);
                break;
            case 3:
                ValidateRequiredString(customData, "branchName", "Branch Name", errors);
                break;
            case 4:
                ValidateRequiredString(customData, "versionNumber", "Version Number", errors);
                break;
        }

        return errors;
    }
}
