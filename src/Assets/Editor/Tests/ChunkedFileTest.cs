using System;
using System.IO;
using Assets.Scripts.Patcher;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;

public class ChunkedFileTest {
    private string _fileName;
    private byte[][] _bytes;
    private byte[] _invalidHash;

    [SetUp]
    public void Setup()
    {
        _fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _bytes = new[] { new byte[] { 1 }, new byte[] { 2 } };
        _invalidHash = new byte[] { 1, 2, 3, 4, 5 };
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_fileName))
        {
            File.Delete(_fileName);
        }
    }

    [Test]
    public void CreateChunkedFile()
    {
        Assert.False(File.Exists(_fileName));
        var chunkedFile = new ChunkedFile(_fileName, 2, 3, _bytes, (buffer, offset, length) => null);
        Assert.True(File.Exists(_fileName));
        chunkedFile.Dispose();
    }

    [Test]
    public void SaveValidFileSinglePass()
    {
        int chunk = 0;

        ChunkedFile.HashFunction hashFunction = (buffer, offset, length) => _bytes[chunk++];
        
        using (var chunkedFile = new ChunkedFile(_fileName, 2, 3, _bytes, hashFunction))
        {

            Assert.AreEqual(0, chunkedFile.VerifiedLength);
            Assert.AreEqual(3, chunkedFile.RemainingLength);

            Assert.True(chunkedFile.Write(new byte[] {1, 2, 3}, 0, 3));

            Assert.AreEqual(3, chunkedFile.VerifiedLength);
            Assert.AreEqual(0, chunkedFile.RemainingLength);
        }
    }

    [Test]
    public void SaveValidFileTwoPasses()
    {
        int sequence = 0;

        ChunkedFile.HashFunction hashFunction = (buffer, offset, length) => _bytes[sequence++];
        
        using (var chunkedFile = new ChunkedFile(_fileName, 2, 3, _bytes, hashFunction))
        {

            Assert.AreEqual(0, chunkedFile.VerifiedLength);
            Assert.AreEqual(3, chunkedFile.RemainingLength);

            Assert.True(chunkedFile.Write(new byte[] {1, 2}, 0, 2));

            Assert.AreEqual(2, chunkedFile.VerifiedLength);
            Assert.AreEqual(1, chunkedFile.RemainingLength);

            Assert.True(chunkedFile.Write(new byte[] {3}, 0, 1));

            Assert.AreEqual(3, chunkedFile.VerifiedLength);
            Assert.AreEqual(0, chunkedFile.RemainingLength);
        }

        Assert.AreEqual(3, new FileInfo(_fileName).Length);
    }

    [Test]
    public void SaveInvalidFirstPass()
    {
        int sequence = 0;

        ChunkedFile.HashFunction hashFunction = (buffer, offset, length) =>
        {
            switch (sequence++)
            {
                case 0:
                    return _invalidHash;
                case 1:
                    return _bytes[0];
                case 2:
                    return _bytes[1];
                default:
                    throw new IndexOutOfRangeException(sequence.ToString());
            }
        };
        
        using (var chunkedFile = new ChunkedFile(_fileName, 2, 3, _bytes, hashFunction))
        {

            Assert.AreEqual(0, chunkedFile.VerifiedLength);
            Assert.AreEqual(3, chunkedFile.RemainingLength);

            Assert.False(chunkedFile.Write(new byte[] {1, 2}, 0, 2), "Should reject those bytes");

            Assert.AreEqual(0, chunkedFile.VerifiedLength);
            Assert.AreEqual(3, chunkedFile.RemainingLength);

            Assert.True(chunkedFile.Write(new byte[] {1, 2}, 0, 2));

            Assert.AreEqual(2, chunkedFile.VerifiedLength);
            Assert.AreEqual(1, chunkedFile.RemainingLength);

            Assert.True(chunkedFile.Write(new byte[] {3}, 0, 1));

            Assert.AreEqual(3, chunkedFile.VerifiedLength);
            Assert.AreEqual(0, chunkedFile.RemainingLength);
        }
    }

    [Test]
    public void SaveInvalidSecondPass()
    {
        int sequence = 0;

        ChunkedFile.HashFunction hashFunction = (buffer, offset, length) =>
        {
            switch (sequence++)
            {
                case 0:
                    return _bytes[0];
                case 1:
                    return _invalidHash;
                case 2:
                    return _bytes[1];
                default:
                    throw new IndexOutOfRangeException(sequence.ToString());
            }
        };
        
        using (var chunkedFile = new ChunkedFile(_fileName, 2, 3, _bytes, hashFunction))
        {

            Assert.AreEqual(0, chunkedFile.VerifiedLength);
            Assert.AreEqual(3, chunkedFile.RemainingLength);

            Assert.True(chunkedFile.Write(new byte[] {1, 2}, 0, 2));

            Assert.AreEqual(2, chunkedFile.VerifiedLength);
            Assert.AreEqual(1, chunkedFile.RemainingLength);

            Assert.False(chunkedFile.Write(new byte[] {3}, 0, 1), "Should reject those bytes");

            Assert.AreEqual(2, chunkedFile.VerifiedLength);
            Assert.AreEqual(1, chunkedFile.RemainingLength);

            Assert.True(chunkedFile.Write(new byte[] {3}, 0, 1));

            Assert.AreEqual(3, chunkedFile.VerifiedLength);
            Assert.AreEqual(0, chunkedFile.RemainingLength);
        }
    }
}
