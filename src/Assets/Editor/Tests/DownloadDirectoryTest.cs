using System.IO;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Local;

public class DownloadDirectoryTest
{
    private string _dirPath;

    [SetUp]
    public void Setup()
    {
        _dirPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dirPath))
        {
            Directory.Delete(_dirPath, true);
        }
    }

    [Test]
    public void PrepareForWriting_CreatesDirectory()
    {
        var downloadDirectory = new DownloadDirectory(_dirPath);
        downloadDirectory.PrepareForWriting();

        Assert.IsTrue(Directory.Exists(_dirPath));
    }

    [Test]
    public void GetContentPackagePath_ForDifferentVersions_ReturnsUniquePaths()
    {
        var downloadDirectory = new DownloadDirectory(_dirPath);

        string path1 = downloadDirectory.GetContentPackagePath(1);
        string path2 = downloadDirectory.GetContentPackagePath(2);

        Assert.AreNotEqual(path1, path2);
    }

    [Test]
    public void GetDiffPackagePath_ForDifferentVersions_ReturnsUniquePaths()
    {
        var downloadDirectory = new DownloadDirectory(_dirPath);

        string path1 = downloadDirectory.GetDiffPackagePath(1);
        string path2 = downloadDirectory.GetDiffPackagePath(2);

        Assert.AreNotEqual(path1, path2);
    }

    [Test]
    public void GetContentPackagePath_And_GetDiffPackagePath_ForSameVersion_ReturnsUniquePaths()
    {
        var downloadDirectory = new DownloadDirectory(_dirPath);

        string path1 = downloadDirectory.GetContentPackagePath(1);
        string path2 = downloadDirectory.GetDiffPackagePath(1);

        Assert.AreNotEqual(path1, path2);
    }

    [Test]
    public void Clear()
    {
        var downloadDirectory = new DownloadDirectory(_dirPath);
        downloadDirectory.PrepareForWriting();

        string filePath = downloadDirectory.GetContentPackagePath(1);
        File.WriteAllText(filePath, "a");

        downloadDirectory.Clear();

        Assert.IsFalse(File.Exists(filePath));
    }
}