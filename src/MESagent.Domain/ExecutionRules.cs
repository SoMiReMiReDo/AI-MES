using System.Text.Json;

namespace MESagent.Domain;

public static class ExecutionRules
{
    public static bool ValidResults(IEnumerable<Guid> participants, IEnumerable<(Guid Id, string Result)> results)
    {
        var rows = results.ToArray();
        return rows.Length > 0 && rows.Select(x => x.Id).Distinct().Count() == rows.Length
            && participants.ToHashSet().SetEquals(rows.Select(x => x.Id))
            && rows.All(x => x.Result is "Passed" or "Failed");
    }

    public static bool HasUnimplementedPolicy(string? parameters) =>
        !string.IsNullOrWhiteSpace(parameters) && parameters.Trim() != "{}";

    public static bool ValidOutput(
        string type,
        string? options,
        string? text,
        decimal? number,
        bool? boolean,
        DateTime? date)
    {
        var count = (text != null ? 1 : 0) + (number != null ? 1 : 0)
            + (boolean != null ? 1 : 0) + (date != null ? 1 : 0);
        if (count != 1)
            return false;
        return type switch
        {
            "decimal" => number is > -100000000000000m and < 100000000000000m
                && decimal.Round(number.Value, 6) == number.Value,
            "integer" => number is > -100000000000000m and < 100000000000000m
                && decimal.Truncate(number.Value) == number.Value,
            "string" => !string.IsNullOrWhiteSpace(text) && text.Length <= 4000,
            "boolean" => boolean != null,
            "datetime" => date is { Kind: DateTimeKind.Utc },
            "enum" => ValidSelection(options, text),
            _ => false
        };
    }

    private static bool ValidSelection(string? options, string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 4000 || string.IsNullOrWhiteSpace(options))
            return false;
        try
        {
            using var json = JsonDocument.Parse(options);
            return json.RootElement.ValueKind == JsonValueKind.Array
                && json.RootElement.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String
                    && x.GetString() == text);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
