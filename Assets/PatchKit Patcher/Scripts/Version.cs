namespace PatchKit.Patching.Unity
{
    public static class Version
    {
        public const int Major = 4;
        public const int Minor = 0;
        public const int Patch = 0;
        public const string Suffix = "";

        public static string Value => $"v{Major}.{Minor}.{Patch}{Suffix}";
    }
}