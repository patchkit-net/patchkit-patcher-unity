#if UNITY_2018
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using PatchKit.Unity.Patcher;
using PatchKit.Unity.Utilities;
using UnityEngine;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Api.Models.Main;
using PatchKit.Network;
using PatchKit.Unity.Patcher.AppData.Remote;

public class ChunkedHttpDownloaderTest
{
    const long DataSize = 1103;
    const long ChunkSize = 64;
    const long PartSize = 128;

    private ResourceUrl _url;
    private ChunksData _chunksData;

    [Test]
    public void ByteRangesTests()
    {
        var fullRange = BytesRangeUtils.Full();
        var outer = BytesRangeUtils.Make(0, 100);
        var inner = BytesRangeUtils.Make(10, 90);
        var innerInside = BytesRangeUtils.Make(50, 60);
        var innerRightSideOverlap = BytesRangeUtils.Make(50, 95);
        var innerLeftSideOverlap = BytesRangeUtils.Make(5, 15);

        Assert.That(outer.Contains(inner));

        var localizedRightOverlap = innerRightSideOverlap.LocalizeTo(inner);
        var localizedLeftOverlap = innerLeftSideOverlap.LocalizeTo(inner);

        Assert.That(localizedRightOverlap, Is.EqualTo(BytesRangeUtils.Make(40, -1)));
        Assert.That(localizedLeftOverlap, Is.EqualTo(BytesRangeUtils.Make(0, 5)));

        var localizedFull = fullRange.LocalizeTo(inner);
        Assert.That(localizedFull, Is.EqualTo(BytesRangeUtils.Full()));

        var localizedInner = innerInside.LocalizeTo(inner);
        Assert.That(localizedInner, Is.EqualTo(BytesRangeUtils.Make(40, 50)));
    }

    [SetUp]
    public void SetUp()
    {
        _url = new ResourceUrl
        {
            Url = "test.com/someData", 
            MetaUrl = "test.com/someMetaData", 
            Country = "GB", 
            PartSize = PartSize
        };

        List<Chunk> chunks = new List<Chunk>();

        for (int i = 0; i < (DataSize / ChunkSize); i++)
        {
            chunks.Add(new Chunk
            {
                Hash=new byte[] {0x20}  // TODO: Generate random bytes here
            });
        }

        _chunksData = new ChunksData
        {
            ChunkSize = ChunkSize,
            Chunks = chunks.ToArray()
        };
    }

    [Test]
    public void ChunksCalculations_WithSpecifiedRange_1()
    {
        BytesRange range = BytesRangeUtils.Make(9, 315);

        var chunksRange =range.Chunkify(_chunksData);

        Assert.That(chunksRange.Start, Is.EqualTo(0));
        Assert.That(chunksRange.End, Is.EqualTo(ChunkSize * 5 - 1));

        Assert.That(chunksRange.Contains(range));
    }

    [Test]
    public void ChunksCalculations_WithSpecifiedRange_2()
    {
        BytesRange range = BytesRangeUtils.Make(450, 830);

        var chunksRange = range.Chunkify(_chunksData);

        Assert.That(chunksRange.Start, Is.EqualTo(ChunkSize * 7));
        Assert.That(chunksRange.End, Is.EqualTo(ChunkSize * 13 - 1));

        Assert.That(chunksRange.Contains(range));
    }

    [Test]
    public void ChunksCalculations_WithFullRange()
    {
        BytesRange range = BytesRangeUtils.Full();

        var chunksRange = range.Chunkify(_chunksData);

        Assert.That(chunksRange.Start, Is.EqualTo(0));
        Assert.That(chunksRange.End, Is.EqualTo(-1));

        Assert.That(chunksRange.Contains(range));
    }

    [Test]
    public void ChunksCalculations_RangeIsExactWithChunks()
    {
        BytesRange range = BytesRangeUtils.Make(ChunkSize, (ChunkSize * 3) - 1); // Exactly 3 chunks

        var chunksRange = range.Chunkify(_chunksData);

        Assert.That(chunksRange, Is.EqualTo(range));
    }

    [Test]
    public void ChunksCalculations_WithRangeExactlyAsDataSize()
    {
        BytesRange range = BytesRangeUtils.Make(0, DataSize);

        var chunksRange = range.Chunkify(_chunksData);

        Assert.That(chunksRange.Start, Is.EqualTo(0));
        Assert.That(chunksRange.End, Is.EqualTo(DataSize));
    }

    [Test]
    public void JobQueuing_WithFullRange()
    {
        BytesRange range = BytesRangeUtils.Full();

        var jobs = ChunkedHttpDownloader.BuildDownloadJobQueue(_url, 0, range, DataSize, _chunksData).ToList();
        int expectedJobCount = (int) (DataSize / PartSize) + 1;

        Assert.That(jobs.Count, Is.EqualTo(expectedJobCount));

        var firstJob = jobs[0];

        Assert.That(firstJob.Range.Start, Is.EqualTo(0));
        Assert.That(firstJob.Range.End, Is.EqualTo(-1));

        var lastJob = jobs[expectedJobCount - 1];

        Assert.That(lastJob.Range.Start, Is.EqualTo(0));
        Assert.That(lastJob.Range.End, Is.EqualTo(-1));
    }

    [Test]
    public void JobQueuing_WithSpecifiedRange()
    {
        BytesRange range = BytesRangeUtils.Make(450, 830);

        var jobs = ChunkedHttpDownloader.BuildDownloadJobQueue(_url, 0,range, DataSize, _chunksData).ToList();

        Assert.That(jobs.Count, Is.EqualTo(4));
        
        var firstJob = jobs[0];

        Assert.That(firstJob.Url, Is.EqualTo("test.com/someData.3"));
        Assert.That(firstJob.Range.Start, Is.EqualTo(64));
        Assert.That(firstJob.Range.End, Is.EqualTo(-1));

        var middleJob = jobs[1];

        Assert.That(middleJob.Url, Is.EqualTo("test.com/someData.4"));
        Assert.That(middleJob.Range.Start, Is.EqualTo(0));
        Assert.That(middleJob.Range.End, Is.EqualTo(-1));

        var lastJob = jobs[3];

        Assert.That(lastJob.Url, Is.EqualTo("test.com/someData.6"));

        Assert.That(lastJob.Range.Start, Is.EqualTo(0));
        Assert.That(lastJob.Range.End, Is.EqualTo(63));
    }

    [Test]
    public void JobQueuing_WithSpecifiedRangeAndOffset()
    {
        BytesRange range = BytesRangeUtils.Make(450, 830);

        long offset = 512;

        var jobs = ChunkedHttpDownloader.BuildDownloadJobQueue(_url, offset,range, DataSize, _chunksData).ToList();

        Assert.That(jobs.Count, Is.EqualTo(3));
        
        var firstJob = jobs[0];

        Assert.That(firstJob.Url, Is.EqualTo("test.com/someData.4"));
        Assert.That(firstJob.Range.Start, Is.EqualTo(0));
        Assert.That(firstJob.Range.End, Is.EqualTo(-1));

        var middleJob = jobs[1];

        Assert.That(middleJob.Url, Is.EqualTo("test.com/someData.5"));
        Assert.That(middleJob.Range.Start, Is.EqualTo(0));
        Assert.That(middleJob.Range.End, Is.EqualTo(-1));

        var lastJob = jobs[2];

        Assert.That(lastJob.Url, Is.EqualTo("test.com/someData.6"));

        Assert.That(lastJob.Range.Start, Is.EqualTo(0));
        Assert.That(lastJob.Range.End, Is.EqualTo(63));
    }

    [Test]
    public void JobQueuing_SinglePartScenario()
    {
        BytesRange range = BytesRangeUtils.Make(315, 380);

        var jobs = ChunkedHttpDownloader.BuildDownloadJobQueue(_url, 0,range, DataSize, _chunksData).ToList();

        Assert.That(jobs.Count, Is.EqualTo(1));
        
        var job = jobs[0];

        Assert.That(job.Url, Is.EqualTo("test.com/someData.2"));
        Assert.That(job.Range.Start, Is.EqualTo(0));
        Assert.That(job.Range.End, Is.EqualTo(-1).Or.EqualTo(127));
    }
}
#endif