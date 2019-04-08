using System;
using System.Collections.Generic;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public struct DataPacket
    {
        public byte[] Data;
        public int Length;
    }

    public interface IBaseHttpStream
    {
        void SetBytesRange(BytesRange? range);
        IEnumerable<DataPacket> Download(CancellationToken cancellationToken);
    }
}