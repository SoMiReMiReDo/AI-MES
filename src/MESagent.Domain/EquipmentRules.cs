namespace MESagent.Domain;

public static class EquipmentRules
{
    public static bool WithinLife(long count, long maximum, DateTime? until, DateTime now) =>
        maximum > 0 && count >= 0 && count < maximum && (until == null || until > now);

    public static bool Qualified(string? status, DateTime from, DateTime? until, DateTime now) =>
        status == "Qualified" && from <= now && (until == null || until > now);
}
