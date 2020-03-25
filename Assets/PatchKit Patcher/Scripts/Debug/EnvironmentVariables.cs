namespace PatchKit.Unity.Patcher.Debug
{
    public static class EnvironmentVariables
    {
        public const string ForceSecretEnvironmentVariable = "PK_PATCHER_FORCE_SECRET";
        public const string ForceVersionEnvironmentVariable = "PK_PATCHER_FORCE_VERSION";
        public const string ApiUrlEnvironmentVariable = "PK_PATCHER_API_URL";
        public const string ApiCacheUrlEnvironmentVariable = "PK_PATCHER_API_CACHE_URL";
        public const string KeysUrlEnvironmentVariable = "PK_PATCHER_KEYS_URL";
        public const string KeepFilesOnErrorEnvironmentVariable = "PK_PATCHER_KEEP_FILES_ON_ERROR";

        // will corrupt every 10th file
        public const string CorruptFilesOnUnpack10 = "PK_PATCHER_CORRUPT_UNPACK_10";

        // will corrupt every 50th file
        public const string CorruptFilesOnUnpack50 = "PK_PATCHER_CORRUPT_UNPACK_50";

        // will corrupt every 300th file
        public const string CorruptFilesOnUnpack300 = "PK_PATCHER_CORRUPT_UNPACK_300";
    }
}