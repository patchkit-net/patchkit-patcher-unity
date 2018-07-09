namespace PatchKit.Patching.Unity
{
    public static class Version
    {
        public const int Major = 4;
        public const int Minor = 0;
        public const int Patch = 0;

#if PK_OFFICIAL
        public const string Suffix = "";
#else
        public const string Suffix = "-official";
#endif

        public static string Value => $"v{Major}.{Minor}.{Patch}{Suffix}";
    }
}