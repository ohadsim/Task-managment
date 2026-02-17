using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Strategies;

public class ProcurementTaskStrategy : TaskStrategyBase
{
    public override string TaskType => "Procurement";
    public override int MaxStatus => 3;

    public override IReadOnlyList<StatusDefinition> GetStatusDefinitions() => new List<StatusDefinition>
    {
        new() { Status = 1, Label = "Created" },
        new() { Status = 2, Label = "Supplier offers received" },
        new() { Status = 3, Label = "Purchase completed" }
    };

    public override IReadOnlyList<FieldDefinition> GetRequiredFields(int targetStatus) => targetStatus switch
    {
        2 => new List<FieldDefinition>
        {
            new() { FieldName = "priceQuote1", Label = "Price Quote 1", FieldType = "string", Required = true },
            new() { FieldName = "priceQuote2", Label = "Price Quote 2", FieldType = "string", Required = true }
        },
        3 => new List<FieldDefinition>
        {
            new() { FieldName = "receipt", Label = "Receipt", FieldType = "string", Required = true }
        },
        _ => new List<FieldDefinition>()
    };

    public override List<string> ValidateStatusData(int targetStatus, Dictionary<string, object> customData)
    {
        var errors = new List<string>();

        switch (targetStatus)
        {
            case 2:
                ValidateRequiredString(customData, "priceQuote1", "Price Quote 1", errors);
                ValidateRequiredString(customData, "priceQuote2", "Price Quote 2", errors);
                break;
            case 3:
                ValidateRequiredString(customData, "receipt", "Receipt", errors);
                break;
        }

        return errors;
    }
}
