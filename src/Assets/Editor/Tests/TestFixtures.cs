using UnityEngine;

class TestFixtures
{
    private static string BasePath
    {
        get { return Application.dataPath.Replace("/Assets", "/TestFixtures"); }
    }

    public static string GetFilePath(string fileName)
    {
        return System.IO.Path.Combine(BasePath, fileName);
    }

    public static string GetDirectoryPath(string dirName)
    {
        return System.IO.Path.Combine(BasePath, dirName);
    }
}