using System.Collections.Generic;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Cancellation;
using JetBrains.Annotations;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public struct DataPacket
    {
        public byte[] Data;
        public int Length;
    }

    /// <summary>
    /// Base HTTP downloader.
    /// </summary>
    public interface IBaseHttpDownloader
    {
        void SetBytesRange(BytesRange? range);

        void Download(CancellationToken cancellationToken, [NotNull] DataAvailableHandler onDataAvailable);

        IEnumerable<DataPacket> ReadPackets(CancellationToken cancellationToken);
    }
}