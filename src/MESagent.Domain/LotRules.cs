namespace MESagent.Domain;

public static class LotRules
{
    public static bool IsTerminal(string status) =>
        status is "Completed" or "Merged" or "Cancelled" or "Closed";

    public static bool IsProperSubset(IEnumerable<Guid> source, IEnumerable<Guid> selected)
    {
        var all = source.ToHashSet();
        var picked = selected.ToArray();
        return picked.Length > 0 && picked.Length < all.Count
            && picked.Distinct().Count() == picked.Length && picked.All(all.Contains);
    }

    public static DateTime NextEventTime(DateTime now, IEnumerable<DateTime> starts)
    {
        var minimum = starts.DefaultIfEmpty(DateTime.MinValue).Max();
        // PostgreSQL stores microseconds; ensure every closed interval has positive length.
        var ticks = Math.Max(now.Ticks / 10 * 10, minimum.Ticks / 10 * 10 + 10);
        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
