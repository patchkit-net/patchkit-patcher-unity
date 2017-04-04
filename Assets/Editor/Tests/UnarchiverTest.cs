using System;
using System.IO;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;

class UnarchiverTest
{
    private string _dirPath;

    [SetUp]
    public void Setup()
    {
        _dirPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_dirPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dirPath))
        {
            Directory.Delete(_dirPath, true);
        }
    }

    private void CheckFileConsistency(string sourceFilePath, string filePath)
    {
        var sourceFileInfo = new FileInfo(sourceFilePath);
        var fileInfo = new FileInfo(filePath);

        Assert.AreEqual(sourceFileInfo.Length, fileInfo.Length,
            string.Format("File size is different for {0}", sourceFilePath));
    }

    private void CheckConsistency(string sourceDirPath, string dirPath)
    {
        var sourceDirInfo = new DirectoryInfo(sourceDirPath);

        foreach (var file in sourceDirInfo.GetFiles("*", SearchOption.TopDirectoryOnly))
        {
            Assert.IsTrue(File.Exists(Path.Combine(dirPath, file.Name)),
                string.Format("Missing file {0} in extracted directory.", file.FullName));
            CheckFileConsistency(Path.Combine(sourceDirPath, file.Name), Path.Combine(dirPath, file.Name));
        }

        foreach (var dir in sourceDirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly))
        {
            Assert.IsTrue(Directory.Exists(Path.Combine(dirPath, dir.Name)),
                string.Format("Missing directory {0} in extracted directory.", dir.FullName));
            CheckConsistency(Path.Combine(sourceDirPath, dir.Name), Path.Combine(dirPath, dir.Name));
        }
    }

    [Test]
    public void Unarchive()
    {
        var unarchiver = new ZipUnarchiver(TestFixtures.GetFilePath("unarchiver-test/zip.zip"), _dirPath);

        unarchiver.Unarchive(CancellationToken.Empty);

        CheckConsistency(TestFixtures.GetDirectoryPath("unarchiver-test/zip"), _dirPath);
    }

    [Test]
    public void CancelUnarchive()
    {
        var unarchiver = new ZipUnarchiver(TestFixtures.GetFilePath("unarchiver-test/zip.zip"), _dirPath);

        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Catch<OperationCanceledException>(() => unarchiver.Unarchive(source));
    }

    [Test]
    public void UnarchiveCorruptedArchive()
    {
        var unarchiver = new ZipUnarchiver(TestFixtures.GetFilePath("unarchiver-test/corrupted-zip.zip"), _dirPath);

        Assert.Catch<Exception>(() => unarchiver.Unarchive(CancellationToken.Empty));
    }

    [Test]
    public void UnarchiveWithPassword()
    {
        string password = "\x08\x07\x18\x24" + "123==";

        var unarchiver = new ZipUnarchiver(TestFixtures.GetFilePath("unarchiver-test/password-zip.zip"), _dirPath, password);

        unarchiver.Unarchive(CancellationToken.Empty);

        CheckConsistency(TestFixtures.GetDirectoryPath("unarchiver-test/password-zip"), _dirPath);
    }

    [Test]
    public void ProgressReporting()
    {
        var unarchiver = new ZipUnarchiver(TestFixtures.GetFilePath("unarchiver-test/zip.zip"), _dirPath);

        int? lastAmount = null;
        int? lastEntry = null;

        unarchiver.UnarchiveProgressChanged += (name, isFile, entry, amount) =>
        {
            if (!lastAmount.HasValue)
            {
                lastAmount = amount;
            }
            Assert.AreEqual(lastAmount, amount, "Amount of extracted files cannot change during the operation.");

            if (lastEntry.HasValue)
            {
                Assert.AreEqual(lastEntry + 1, entry, "Entries are not following each other.");
            }

            lastEntry = entry;

            if (entry == 0)
            {
                Assert.IsNull(name);
            }
            else if (isFile)
            {
                string filePath = Path.Combine(_dirPath, name);
                Assert.IsTrue(File.Exists(filePath), string.Format("File doesn't exist - {0}", filePath));
            }
            else
            {
                string dirPath = Path.Combine(_dirPath, name);
                Assert.IsTrue(Directory.Exists(dirPath), string.Format("Directory doesn't exist - {0}", dirPath));
            }
        };

        unarchiver.Unarchive(CancellationToken.Empty);

        Assert.IsNotNull(lastAmount);
        Assert.IsNotNull(lastEntry);
        Assert.AreEqual(lastAmount, lastEntry, "Last entry must be equal to amount.");
    }
}