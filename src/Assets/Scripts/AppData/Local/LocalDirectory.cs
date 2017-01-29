namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// Implementation of <see cref="ILocalDirectory"/>.
    /// </summary>
    /// <seealso cref="BaseWritableDirectory{LocalDirectory}" />
    /// <seealso cref="ILocalDirectory" />
    public class LocalDirectory : BaseWritableDirectory<LocalDirectory>, ILocalDirectory
    {
        public LocalDirectory(string path) : base(path)
        {
        }
    }
}