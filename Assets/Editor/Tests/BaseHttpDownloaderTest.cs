#if UNITY_2018 && PK_TESTS_FIX_TODO_2019_04_18
using System.IO;
using System.Net;
using NUnit.Framework;
using NSubstitute;
using PatchKit.Network;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Unity.Patcher.Cancellation;
using ILogger = PatchKit.Logging.ILogger;
using Random = System.Random;

public class BaseHttpDownloaderTest
{
    private static byte[] CreateRandomData(int length)
    {
        var data = new byte[length];
        new Random().NextBytes(data);
        return data;
    }

    private static IHttpClient MockHttpClient(Stream responseStream, HttpStatusCode statusCode)
    {
        var httpClient = Substitute.For<IHttpClient>();
        httpClient.Get(Arg.Any<HttpGetRequest>()).Returns(info =>
        {
            var request = info.Arg<HttpGetRequest>();
            var response = Substitute.For<IHttpResponse>();

            if (request.Range.HasValue)
            {
                responseStream.Seek(request.Range.Value.Start, SeekOrigin.Begin);
            }

            response.ContentStream.Returns(responseStream);
            response.StatusCode.Returns(statusCode);

            return response;
        });

        return httpClient;
    }

    private static void ValidateOutput(byte[] inputData, MemoryStream outputDataStream, BytesRange bytesRange)
    {
        byte[] buffer = new byte[1];
        
        for (long i = bytesRange.Start; i < bytesRange.End; i++)
        {
            Assert.AreEqual(1, outputDataStream.Read(buffer, 0, 1),
                string.Format("Cannot read output data stream at byte {0}.", i));
            Assert.AreEqual(inputData[i], buffer[0], string.Format("Output data is different at byte {0}.", i));
        }
    }

    [Test]
    public void DownloadStreamsAllData_For_NotSpecifiedRange()
    {
        var inputData = CreateRandomData(1024);
        var inputDataStream = new MemoryStream(inputData, false);

        var baseHttpDownloader = new BaseHttpDownloader("http://test_url.com", 10000,
            MockHttpClient(inputDataStream, HttpStatusCode.OK), Substitute.For<ILogger>());

        var outputDataStream = new MemoryStream(inputData.Length);

        baseHttpDownloader.DataAvailable += (data, length) =>
        {
            Assert.IsTrue(length > 0, "Data length passed in DataAvailable event is not more than zero.");

            outputDataStream.Write(data, 0, length);
        };

        baseHttpDownloader.Download(CancellationToken.Empty);

        outputDataStream.Seek(0, SeekOrigin.Begin);

        ValidateOutput(inputData, outputDataStream, new BytesRange(0, inputData.Length));
    }

    [Test]
    public void DownloadStreamsCertainData_For_SpecifiedRange()
    {
        var bytesRange = new BytesRange
        {
            Start = 100,
            End = 200
        };

        var inputData = CreateRandomData(1024);
        var inputDataStream = new MemoryStream(inputData, false);

        var baseHttpDownloader = new BaseHttpDownloader("http://test_url.com", 10000,
            MockHttpClient(inputDataStream, HttpStatusCode.OK), Substitute.For<ILogger>());


        var outputDataStream = new MemoryStream(inputData.Length);

        baseHttpDownloader.DataAvailable += (data, length) =>
        {
            Assert.IsTrue(length > 0, "Data length passed in DataAvailable event is not more than zero.");

            outputDataStream.Write(data, 0, length);
        };

        baseHttpDownloader.SetBytesRange(bytesRange);

        baseHttpDownloader.Download(CancellationToken.Empty);

        outputDataStream.Seek(0, SeekOrigin.Begin);

        ValidateOutput(inputData, outputDataStream, bytesRange);
    }

    [Test]
    public void DownloadThrowsException_For_Status404()
    {
        var inputData = CreateRandomData(1024);
        var inputDataStream = new MemoryStream(inputData, false);

        var baseHttpDownloader = new BaseHttpDownloader("http://test_url.com", 10000,
            MockHttpClient(inputDataStream, HttpStatusCode.NotFound), Substitute.For<ILogger>());
        
        Assert.Catch<DataNotAvailableException>(() => baseHttpDownloader.Download(CancellationToken.Empty));
    }
    
    [Test]
    public void DownloadThrowsException_For_Status500()
    {
        var inputData = CreateRandomData(1024);
        var inputDataStream = new MemoryStream(inputData, false);

        var baseHttpDownloader = new BaseHttpDownloader("http://test_url.com", 10000,
            MockHttpClient(inputDataStream, HttpStatusCode.InternalServerError), Substitute.For<ILogger>());
        
        Assert.Catch<ServerErrorException>(() => baseHttpDownloader.Download(CancellationToken.Empty));
    }
    
    [Test]
    public void DownloadThrowsException_For_WebException()
    {
        var httpClient = Substitute.For<IHttpClient>();
        httpClient.Get(Arg.Any<HttpGetRequest>()).Returns(_ =>
        {
            throw new WebException();
        });

        var baseHttpDownloader = new BaseHttpDownloader("http://test_url.com", 10000, httpClient, Substitute.For<ILogger>());
        
        Assert.Catch<ConnectionFailureException>(() => baseHttpDownloader.Download(CancellationToken.Empty));
    }
}
#endif