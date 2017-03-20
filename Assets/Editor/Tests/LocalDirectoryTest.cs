using System.IO;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Local;

class LocalDirectoryTest
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
        var localDirectory = new LocalDirectory(_dirPath);
        localDirectory.PrepareForWriting();

        Assert.IsTrue(Directory.Exists(_dirPath));
    }
}