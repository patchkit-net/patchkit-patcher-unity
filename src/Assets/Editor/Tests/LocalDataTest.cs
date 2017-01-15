using System;
using System.IO;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Local;

class LocalDataTest
{
    private string _dirPath;

    private string _firstFileContent;
    private string _firstFilePath;

    private string _secondFileContent;
    private string _secondFilePath;

    [SetUp]
    public void Setup()
    {
        _dirPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _firstFileContent = Path.GetRandomFileName();
        _secondFileContent = Path.GetRandomFileName();
        _firstFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _secondFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(_firstFilePath, _firstFileContent);
        File.WriteAllText(_secondFilePath, _secondFileContent);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dirPath))
        {
            Directory.Delete(_dirPath, true);
        }
        if (File.Exists(_firstFilePath))
        {
            File.Delete(_firstFilePath);
        }
        if (File.Exists(_secondFileContent))
        {
            File.Delete(_secondFileContent);
        }
    }

    [Test]
    public void EnableWriteAccess()
    {
        var localData = new LocalData(_dirPath);
        localData.EnableWriteAccess();

        Assert.IsTrue(Directory.Exists(_dirPath));
    }

    [Test]
    public void CreateDirectory()
    {
        var localData = new LocalData(_dirPath);
        localData.EnableWriteAccess();

        localData.CreateDirectory("a");
        localData.CreateDirectory("a/a");

        Assert.IsTrue(localData.DirectoryExists("a"));
        Assert.IsTrue(localData.DirectoryExists("a/a"));
    }

    [Test]
    public void CreateDirectoryExistingDirectory()
    {
        var localData = new LocalData(_dirPath);
        localData.EnableWriteAccess();

        localData.CreateDirectory("a");
        localData.CreateDirectory("a");

        Assert.IsTrue(localData.DirectoryExists("a"));
    }

    [Test]
    public void DirectoryExists()
    {
        var localData = new LocalData(_dirPath);
        localData.EnableWriteAccess();

        localData.CreateDirectory("a");

        Assert.IsTrue(localData.DirectoryExists("a"));
        Assert.IsFalse(localData.DirectoryExists("b"));

        Assert.IsTrue(Directory.Exists(localData.GetDirectoryPath("a")));
        Assert.IsFalse(Directory.Exists(localData.GetDirectoryPath("b")));
    }

    [Test]
    public void CreateFile()
    {
        var localData = new LocalData(_dirPath);
        localData.EnableWriteAccess();

        localData.CreateOrUpdateFile("a", _firstFilePath);

        Assert.IsTrue(localData.FileExists("a"));
        Assert.AreEqual(_firstFileContent, File.ReadAllText(localData.GetFilePath("a")));
    }

    [Test]
    public void CreateFileInNotExistingDirectory()
    {
        var localData = new LocalData(_dirPath);
        localData.EnableWriteAccess();

        localData.CreateOrUpdateFile("a/a", _firstFilePath);

        Assert.IsTrue(localData.DirectoryExists("a"));
        Assert.IsTrue(localData.FileExists("a/a"));
    }

    [Test]
    public void UpdateFile()
    {
        var localData = new LocalData(_dirPath);
        localData.EnableWriteAccess();

        localData.CreateOrUpdateFile("a", _firstFilePath);
        localData.CreateOrUpdateFile("a", _secondFilePath);

        Assert.IsTrue(localData.FileExists("a"));
        Assert.AreEqual(_secondFileContent, File.ReadAllText(localData.GetFilePath("a")));
    }

    [Test]
    public void FileExists()
    {
        var localData = new LocalData(_dirPath);
        localData.EnableWriteAccess();

        localData.CreateOrUpdateFile("a", _firstFilePath);

        Assert.IsTrue(localData.FileExists("a"));
        Assert.IsFalse(localData.FileExists("b"));

        Assert.IsTrue(File.Exists(localData.GetFilePath("a")));
        Assert.IsFalse(File.Exists(localData.GetFilePath("b")));
    }

    [Test]
    public void DeleteFile()
    {
        var localData = new LocalData(_dirPath);
        localData.EnableWriteAccess();

        localData.CreateOrUpdateFile("a", _firstFilePath);

        Assert.IsTrue(localData.FileExists("a"));

        localData.DeleteFile("a");

        Assert.IsFalse(localData.FileExists("a"));
    }

    [Test]
    public void DeleteNotExistingFile()
    {
        var localData = new LocalData(_dirPath);
        localData.EnableWriteAccess();

        localData.DeleteFile("a");

        Assert.IsFalse(localData.FileExists("a"));
    }

    [Test]
    public void IsDirectoryEmpty()
    {
        var localData = new LocalData(_dirPath);
        localData.EnableWriteAccess();

        localData.CreateDirectory("a");
        localData.CreateDirectory("b");

        Assert.IsTrue(localData.IsDirectoryEmpty("a"));
        Assert.IsTrue(localData.IsDirectoryEmpty("b"));

        localData.CreateOrUpdateFile("a/a", _firstFilePath);

        Assert.IsFalse(localData.IsDirectoryEmpty("a"));
        Assert.IsTrue(localData.IsDirectoryEmpty("b"));

        localData.CreateOrUpdateFile("b/b", _firstFilePath);

        Assert.IsFalse(localData.IsDirectoryEmpty("a"));
        Assert.IsFalse(localData.IsDirectoryEmpty("b"));
    }

    [Test]
    public void UpdateDirectoryAsFile()
    {
        var localData = new LocalData(_dirPath);
        localData.EnableWriteAccess();

        localData.CreateDirectory("a");

        Assert.Catch<InvalidOperationException>(() => localData.CreateOrUpdateFile("a", _firstFilePath));
    }
}