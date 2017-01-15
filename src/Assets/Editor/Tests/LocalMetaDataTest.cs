using System.IO;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Local;

public class LocalMetaDataTest
{
    private string _filePath;

    [SetUp]
    public void Setup()
    {
        _filePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    [Test]
    public void CreateMetaDataFile()
    {
        Assert.False(File.Exists(_filePath));

        var localMetaData = new LocalMetaData(_filePath);

        localMetaData.AddOrUpdateFile("test", 1);

        Assert.True(File.Exists(_filePath));
    }

    [Test]
    public void SaveValidFileSinglePass()
    {
        var localMetaData = new LocalMetaData(_filePath);

        localMetaData.AddOrUpdateFile("a", 1);
        localMetaData.AddOrUpdateFile("b", 2);

        var localMetaData2 = new LocalMetaData(_filePath);

        Assert.IsTrue(localMetaData2.FileExists("a"));
        Assert.IsTrue(localMetaData2.FileExists("b"));

        Assert.AreEqual(1, localMetaData2.GetFileVersion("a"));
        Assert.AreEqual(2, localMetaData2.GetFileVersion("b"));
    }
}