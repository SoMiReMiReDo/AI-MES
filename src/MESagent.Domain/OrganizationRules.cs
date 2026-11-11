using System.Text.RegularExpressions;

namespace MESagent.Domain;

public static partial class OrganizationRules
{
    [GeneratedRegex("^[A-Z0-9][A-Z0-9_.-]{0,63}$")]
    private static partial Regex CodePattern();

    public static string Code(string value)
    {
        var result = value.Trim().ToUpperInvariant();
        if (!CodePattern().IsMatch(result))
            throw new ArgumentException("编码须为 1～64 位字母、数字、-_.，以字母或数字开头");

        return result;
    }

    public static string Text(string value, int max = 200)
    {
        var result = value.Trim();
        if (result.Length == 0 || result.Length > max)
            throw new ArgumentException($"内容长度须为 1～{max}");

        return result;
    }

    public static string Timezone(string value)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(value, out _) || !value.Contains('/'))
            throw new ArgumentException("请输入有效 IANA 时区，如 Asia/Shanghai");

        return value;
    }
}
