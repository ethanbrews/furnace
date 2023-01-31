namespace Furnace.Utility.Extension;

public static class StringExtensions
{
    public static string Truncate(this string value, int maxChars)
    {
        return value.Length <= maxChars ? value : value[..maxChars] + "...";
    }
}