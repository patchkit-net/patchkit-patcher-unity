using JetBrains.Annotations;

public static class StringExtensions
{
    public static string SurroundWithQuotes([NotNull] this string @this)
    {
        return $"\"{@this}\"";
    }
}