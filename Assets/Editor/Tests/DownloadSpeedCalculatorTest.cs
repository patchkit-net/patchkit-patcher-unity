using System;
using NUnit.Framework;
using PatchKit.Unity.Patcher.Status;

class DownloadSpeedCalculatorTest
{
    [Test]
    public void DownloadSpeed_For3Samples_ReturnsAverage()
    {
        var downloadSpeedCalculator = new DownloadSpeedCalculator();
        downloadSpeedCalculator.Restart(new DateTime(2000, 1, 1, 1, 1, 0));
        downloadSpeedCalculator.AddSample(1000, new DateTime(2000, 1, 1, 1, 1, 1));
        downloadSpeedCalculator.AddSample(2500, new DateTime(2000, 1, 1, 1, 1, 2));
        downloadSpeedCalculator.AddSample(3000, new DateTime(2000, 1, 1, 1, 1, 3));

        Assert.AreEqual(1000.0, downloadSpeedCalculator.BytesPerSecond, 0.1, "Download speed is not correct.");
    }
}