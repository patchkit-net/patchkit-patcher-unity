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

        localMetaData.RegisterEntry("test", 1);

        Assert.True(File.Exists(_filePath));
    }

    [Test]
    public void SaveValidFileSinglePass()
    {
        var localMetaData = new LocalMetaData(_filePath);

        localMetaData.RegisterEntry("a", 1);
        localMetaData.RegisterEntry("b", 2);

        var localMetaData2 = new LocalMetaData(_filePath);

        Assert.IsTrue(localMetaData2.IsEntryRegistered("a"));
        Assert.IsTrue(localMetaData2.IsEntryRegistered("b"));

        Assert.AreEqual(1, localMetaData2.GetEntryVersionId("a"));
        Assert.AreEqual(2, localMetaData2.GetEntryVersionId("b"));
    }
}