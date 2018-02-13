namespace PatchKit.Unity.Patcher
{
    public static class Version
    {
        public const int major = 3;
        public const int minor = 9;
        public const int release = 0;

        public static string Value
        {
            get 
            {
                return string.Format("v{0}.{1}.{2}", major, minor, release);
            }
        }
    }
}