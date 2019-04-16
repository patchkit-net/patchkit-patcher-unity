public static class Version
{
    public const int Major = 4;
    public const int Minor = 0;
    public const int Release = 0;

#if PK_OFFICIAL
    public const string Suffix = "-rc1.official";
#else
    public const string Suffix = "-rc1";
#endif

    public static string Text => $"v{Major}.{Minor}.{Release}{Suffix}";
}