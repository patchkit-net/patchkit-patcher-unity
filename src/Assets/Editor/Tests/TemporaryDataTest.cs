using System.IO;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Local;

class TemporaryDataTest
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
        var temporaryData = new TemporaryData(_dirPath);

        Assert.IsTrue(Directory.Exists(_dirPath));
    }

    [Test]
    public void Delete()
    {
        var temporaryData = new TemporaryData(_dirPath);

        File.WriteAllText(temporaryData.GetUniquePath(), "a");

        temporaryData.Dispose();
        Assert.IsFalse(Directory.Exists(_dirPath));
    }

    [Test]
    public void UniquePaths()
    {
        var temporaryData = new TemporaryData(_dirPath);

        for (int i = 0; i < 100; i++)
        {
            string path = temporaryData.GetUniquePath();

            Assert.IsFalse(File.Exists(path));
            Assert.IsFalse(Directory.Exists(path));

            if (i%2 == 0)
            {
                File.WriteAllText(temporaryData.GetUniquePath(), "a");
            }
            else
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}