namespace PatchKit.Unity.Patcher
{
    public static class Version
    {
        public const int Major = 3;
        public const int Minor = 12;
        public const int Release = 1;

        public static string Value
        {
#if PK_OFFICIAL
            get { return string.Format("v{0}.{1}.{2}-official", Major, Minor, Release); }
#else
            get { return string.Format("v{0}.{1}.{2}", Major, Minor, Release); }
#endif
        }
    }
}
