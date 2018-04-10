namespace PatchKit.Unity.Patcher
{
    public static class Version
    {
        public const int Major = 3;
        public const int Minor = 10;
        public const int Release = 0;

        public static string Value
        {
            get { return string.Format("v{0}.{1}.{2}", Major, Minor, Release); }
        }
    }
}