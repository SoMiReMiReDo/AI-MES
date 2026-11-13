namespace MESagent.Domain;

public static class ManufacturingRules
{
    public static bool IsEffective(string state, DateTime? from, DateTime? to, DateTime now) =>
        state == "Released" && (from is null || from <= now) && (to is null || to > now);

    public static bool CanPublish(string state) => state == "Draft";

    public static bool CanObsolete(string state) => state == "Released";

    public static bool ValidWorkOrderPlan(int quantity, int priority, DateTime? start, DateTime? end) =>
        quantity > 0 && priority is >= 0 and <= 100 && (start is null || end is null || end >= start);
}
