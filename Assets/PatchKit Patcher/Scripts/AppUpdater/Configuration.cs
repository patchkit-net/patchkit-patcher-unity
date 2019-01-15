using System;

namespace PatchKit.Apps.Updating.AppUpdater
{
[Serializable]
public class Configuration
{
    public bool CheckConsistencyBeforeDiffUpdate;

    public long HashSizeThreshold = 1024 * 1024 * 1024; // in bytes
}
}