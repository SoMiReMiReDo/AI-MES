namespace MESagent.Domain;

public static class QualityRules
{
    public static bool ExactNumber(decimal value) => value > -100000000000000m
        && value < 100000000000000m && decimal.Round(value, 6) == value;

    public static bool ValidSpecification(string type, decimal? min, decimal? max,
        bool? boolean, string? text) => type switch
        {
            "decimal" or "integer" => boolean == null && text == null && (min != null || max != null)
                && (min == null || ExactNumber(min.Value)) && (max == null || ExactNumber(max.Value))
                && (min == null || max == null || min <= max)
                && (type != "integer" || (min == null || decimal.Truncate(min.Value) == min)
                    && (max == null || decimal.Truncate(max.Value) == max)),
            "boolean" => min == null && max == null && boolean != null && text == null,
            "string" => min == null && max == null && boolean == null
                && !string.IsNullOrWhiteSpace(text) && text.Length <= 2000,
            _ => false
        };

    public static bool ValidValue(string type, decimal? number, bool? boolean, string? text)
    {
        if ((number != null ? 1 : 0) + (boolean != null ? 1 : 0) + (text != null ? 1 : 0) != 1)
            return false;
        return type switch
        {
            "decimal" => number != null && ExactNumber(number.Value),
            "integer" => number != null && ExactNumber(number.Value) && decimal.Truncate(number.Value) == number,
            "boolean" => boolean != null,
            "string" => !string.IsNullOrWhiteSpace(text) && text.Length <= 2000,
            _ => false
        };
    }

    public static bool Passed(string type, decimal? min, decimal? max, bool? expectedBoolean,
        string? expectedText, decimal? number, bool? boolean, string? text) => type switch
        {
            "decimal" or "integer" => number != null && (min == null || number >= min)
                && (max == null || number <= max),
            "boolean" => boolean == expectedBoolean,
            "string" => string.Equals(text, expectedText, StringComparison.Ordinal),
            _ => false
        };
}
