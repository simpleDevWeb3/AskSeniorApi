namespace AskSeniorApi.Helpers;

public static class StringExtensions
{
    public static string? Clean(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        value = value.Trim().ToLower();
        return value is "null" or "undefined" ? null : value;
    }
}
