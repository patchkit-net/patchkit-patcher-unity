namespace PatchKit.Unity.Patcher
{
    public static class Version
    {
        public const int Major = 3;
        public const int Minor = 17;
        public const int Patch = 2;
        public const int Hotfix = 0;

        public static string Value
        {
#if PK_OFFICIAL
            get { return string.Format("v{0}.{1}.{2}.{3}-official", Major, Minor, Patch, Hotfix); }
#else
            get { return string.Format("v{0}.{1}.{2}.{3}", Major, Minor, Patch, Hotfix); }
#endif
        }
    }
}
