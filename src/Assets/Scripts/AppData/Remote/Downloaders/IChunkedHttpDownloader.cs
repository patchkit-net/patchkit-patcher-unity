using System;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public interface IChunkedHttpDownloader : IDisposable
    {
        void Download(CancellationToken cancellationToken);
    }
}