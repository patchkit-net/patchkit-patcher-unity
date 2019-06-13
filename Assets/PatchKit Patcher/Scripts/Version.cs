public static class Version
{
    public const int Major = 4;
    public const int Minor = 0;
    public const int Patch = 0;
    public const int Hotfix = 0;

#if PK_OFFICIAL
    public const string Suffix = "-rc3.official";
#else
    public const string Suffix = "-rc3";
#endif

    public static string Text => $"v{Major}.{Minor}.{Patch}.{Hotfix}{Suffix}";
}