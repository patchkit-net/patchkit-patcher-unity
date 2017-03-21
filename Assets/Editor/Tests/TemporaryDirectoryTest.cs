using System.IO;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Local;

class TemporaryDirectoryTest
{
    private string _dirPath;

    [SetUp]
    public void Setup()
    {
        _dirPath = TestHelpers.CreateTemporaryDirectory();
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.DeleteTemporaryDirectory(_dirPath);
    }

    [Test]
    public void PrepareForWriting_CreatesDirectory()
    {
        using (var temporaryDirectory = new TemporaryDirectory(_dirPath))
        {
            temporaryDirectory.PrepareForWriting();

            Assert.IsTrue(Directory.Exists(_dirPath));
        }
    }

    [Test]
    public void Dispose_DeletesDirectory()
    {
        using (var temporaryDirectory = new TemporaryDirectory(_dirPath))
        {
            temporaryDirectory.PrepareForWriting();
        }

        Assert.IsFalse(Directory.Exists(_dirPath));
    }

    [Test]
    public void Dispose_DeletesDirectoryWithContent()
    {
        using (var temporaryDirectory = new TemporaryDirectory(_dirPath))
        {
            temporaryDirectory.PrepareForWriting();
            File.WriteAllText(temporaryDirectory.GetUniquePath(), "a");
        }

        Assert.IsFalse(Directory.Exists(_dirPath));
    }

    [Test]
    public void GetUniquePath_ReturnsUniquePaths()
    {
        using (var temporaryData = new TemporaryDirectory(_dirPath))
        {
            temporaryData.PrepareForWriting();

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
}