using System;
using System.IO;
using System.Net;
using NUnit.Framework;
using NSubstitute;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Unity.Patcher.Cancellation;

public class BaseHttpDownloaderTest
{
    private byte[] CreateRandomData(int length)
    {
        byte[] data = new byte[length];
        new Random().NextBytes(data);
        return data;
    }

    private IHttpWebRequestAdapter MockRequest(Stream responseStream, HttpStatusCode statusCode)
    {
        var request = Substitute.For<IHttpWebRequestAdapter>();
        var response = Substitute.For<IHttpWebResponseAdapter>();
        response.GetResponseStream().Returns(responseStream);
        response.StatusCode.Returns(statusCode);
        request.GetResponse().Returns(response);
        request.WhenForAnyArgs(adapter => adapter.AddRange(0, 0))
            .Do(info =>
            {
                if (responseStream != null)
                {
                    responseStream.Seek((long) info[0], SeekOrigin.Begin);
                }
            });

        return request;
    }

    private void ValidateOutput(byte[] inputData, MemoryStream outputDataStream, int bytesRangeStart, int bytesRangeEnd)
    {
        byte[] buffer = new byte[1];

        for (int i = bytesRangeStart; i < bytesRangeEnd; i++)
        {
            Assert.AreEqual(1, outputDataStream.Read(buffer, 0, 1),
                string.Format("Cannot read output data stream at byte {0}.", i));
            Assert.AreEqual(inputData[i], buffer[0], string.Format("Output data is different at byte {0}.", i));
        }
    }

    [Test]
    public void Download()
    {
        var inputData = CreateRandomData(1024);
        var inputDataStream = new MemoryStream(inputData, false);

        var baseHttpDownloader = new BaseHttpDownloader("someurl", 10000, 64,
            url => MockRequest(inputDataStream, HttpStatusCode.OK));

        var outputDataStream = new MemoryStream(inputData.Length);

        baseHttpDownloader.DataAvailable += (data, length) =>
        {
            Assert.IsTrue(length > 0, "Data length passed in DataAvailable event is not more than zero.");

            outputDataStream.Write(data, 0, length);
        };

        baseHttpDownloader.Download(CancellationToken.Empty);

        outputDataStream.Seek(0, SeekOrigin.Begin);

        ValidateOutput(inputData, outputDataStream, 0, inputData.Length);
    }

    [Test]
    public void DownloadRange()
    {
        const int bytesStartRange = 100;
        const int bytesEndRange = 200;

        var inputData = CreateRandomData(1024);
        var inputDataStream = new MemoryStream(inputData, false);

        var baseHttpDownloader = new BaseHttpDownloader("someurl", 10000, 64,
            url => MockRequest(inputDataStream, HttpStatusCode.OK));

        var outputDataStream = new MemoryStream(inputData.Length);

        baseHttpDownloader.DataAvailable += (data, length) =>
        {
            Assert.IsTrue(length > 0, "Data length passed in DataAvailable event is not more than zero.");

            outputDataStream.Write(data, 0, length);
        };

        baseHttpDownloader.SetBytesRange(bytesStartRange, bytesEndRange);

        baseHttpDownloader.Download(CancellationToken.Empty);

        outputDataStream.Seek(0, SeekOrigin.Begin);

        ValidateOutput(inputData, outputDataStream, bytesStartRange, bytesEndRange);
    }

    [Test]
    public void InvalidResponse()
    {
        var inputData = CreateRandomData(1024);
        var inputDataStream = new MemoryStream(inputData, false);

        var baseHttpDownloader = new BaseHttpDownloader("someurl", 10000, 64,
            url => MockRequest(inputDataStream, HttpStatusCode.BadRequest));

        Assert.Catch<DownloaderException>(() => baseHttpDownloader.Download(CancellationToken.Empty));
    }

    [Test]
    public void EmptyDataStream()
    {
        var baseHttpDownloader = new BaseHttpDownloader("someurl", 10000, 64,
            url => MockRequest(null, HttpStatusCode.OK));

        Assert.Catch<DownloaderException>(() => baseHttpDownloader.Download(CancellationToken.Empty));
    }
}