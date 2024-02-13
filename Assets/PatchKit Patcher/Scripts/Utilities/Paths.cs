namespace PatchKit.Unity.Utilities
{
    // This utility is to make sure that all paths fixes are applied
    
    public class Paths
    {
        // Long path fix on Windows needs to ensure that:
        // - All paths are using back slashes
        // - All path are absolute
        // - All paths start with \\?\ prefix, e.g. \\?\C:\path\to\file
        public static string Fix(string path)
        {
            if (Platform.IsWindows() && Patcher.Instance.FixLongPathsOnWindows)
            {
                path = path.ReplaceAll('/', '\\');
                path = System.IO.Path.GetFullPath(path);
                if (!path.StartsWith("\\\\?\\"))
                {
                    path = "\\\\?\\" + path;
                }

                return path;
            }
            else
            {
                return path;

            }
             
        }
    }
}