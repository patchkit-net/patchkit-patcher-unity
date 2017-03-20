using System;
using System.IO;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;

public class ChunkedFileStreamTest {
    private string _fileName;
    private byte[] _invalidHash;
    private ChunksData _chunksData;

    [SetUp]
    public void Setup()
    {
        _fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _invalidHash = new byte[] { 1, 2, 3, 4, 5 };

        _chunksData = new ChunksData
        {
            ChunkSize = 2,
            Chunks = new []
            {
                new Chunk
                {
                    Hash = new byte[]{ 1 }
                },
                new Chunk
                {
                    Hash = new byte[]{ 2 }
                }
            }
        };
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
        
        var chunkedFile = new ChunkedFileStream(_fileName, 3, _chunksData, (buffer, offset, length) => null);
        Assert.True(File.Exists(_fileName));
        chunkedFile.Dispose();
    }

    [Test]
    public void SaveValidFileSinglePass()
    {
        int chunk = 0;

        ChunkedFileStream.HashFunction hashFunction = (buffer, offset, length) => _chunksData.Chunks[chunk++].Hash;
        
        using (var chunkedFile = new ChunkedFileStream(_fileName, 3, _chunksData, hashFunction))
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

        ChunkedFileStream.HashFunction hashFunction = (buffer, offset, length) => _chunksData.Chunks[sequence++].Hash;
        
        using (var chunkedFile = new ChunkedFileStream(_fileName, 3, _chunksData, hashFunction))
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

        ChunkedFileStream.HashFunction hashFunction = (buffer, offset, length) =>
        {
            switch (sequence++)
            {
                case 0:
                    return _invalidHash;
                case 1:
                    return _chunksData.Chunks[0].Hash;
                case 2:
                    return _chunksData.Chunks[1].Hash;
                default:
                    throw new IndexOutOfRangeException(sequence.ToString());
            }
        };
        
        using (var chunkedFile = new ChunkedFileStream(_fileName, 3, _chunksData, hashFunction))
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

        ChunkedFileStream.HashFunction hashFunction = (buffer, offset, length) =>
        {
            switch (sequence++)
            {
                case 0:
                    return _chunksData.Chunks[0].Hash;
                case 1:
                    return _invalidHash;
                case 2:
                    return _chunksData.Chunks[1].Hash;
                default:
                    throw new IndexOutOfRangeException(sequence.ToString());
            }
        };
        
        using (var chunkedFile = new ChunkedFileStream(_fileName, 3, _chunksData, hashFunction))
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
