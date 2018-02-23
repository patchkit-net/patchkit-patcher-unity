namespace PatchKit.Patching.Unity
{
    public static class Version
    {
        public const int Major = 4;
        public const int Minor = 0;
        public const int Patch = 0;
        public const string Suffix = "";

        public static string Value
        {
            get { return string.Format("v{0}.{1}.{2}{3}", Major, Minor, Patch, Suffix); }
        }
    }
}