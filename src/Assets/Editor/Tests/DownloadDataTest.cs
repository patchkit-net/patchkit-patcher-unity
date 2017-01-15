using System.IO;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Local;

public class DownloadDataTest
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
    public void Create()
    {
        var downloadData = new DownloadData(_dirPath);

        Assert.IsTrue(Directory.Exists(_dirPath));
    }

    [Test]
    public void PathsInsideDirectory()
    {
        var downloadData = new DownloadData(_dirPath);

        Assert.IsTrue(Path.GetDirectoryName(downloadData.GetFilePath("a")) == _dirPath);
        Assert.IsTrue(Path.GetDirectoryName(downloadData.GetContentPackagePath(1)) == _dirPath);
        Assert.IsTrue(Path.GetDirectoryName(downloadData.GetDiffPackagePath(1)) == _dirPath);
    }

    [Test]
    public void UniquePaths()
    {
        var downloadData = new DownloadData(_dirPath);

        string path1 = downloadData.GetFilePath("a");
        string path2 = downloadData.GetContentPackagePath(1);
        string path3 = downloadData.GetDiffPackagePath(1);

        Assert.AreNotEqual(path1, path2);
        Assert.AreNotEqual(path2, path3);
        Assert.AreNotEqual(path1, path3);
    }

    [Test]
    public void Clear()
    {
        var downloadData = new DownloadData(_dirPath);

        string filePath = downloadData.GetFilePath("a");
        File.WriteAllText(filePath, "a");

        Assert.IsTrue(File.Exists(filePath));

        downloadData.Clear();

        Assert.IsFalse(File.Exists(filePath));
    }
}