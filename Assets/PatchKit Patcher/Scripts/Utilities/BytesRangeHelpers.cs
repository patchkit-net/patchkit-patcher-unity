using PatchKit.Network;
using ChunksData = PatchKit.Unity.Patcher.AppData.Remote.ChunksData;

namespace PatchKit.Unity.Utilities
{
    public static class BytesRangeUtils
    {
        public static BytesRange Make(long start, long end = -1, long topBound = -1)
        {
            long s = start;
            long e = end;

            if (end == -1 && topBound != -1)
            {
                end = topBound;
            }

            return new BytesRange
            {
                Start = s,
                End = e
            };
        }

        public static BytesRange Full()
        {
            return Make(0, -1);
        }

        public static BytesRange Empty()
        {
            return Make(0, 0);
        }

        /// <summary>
        /// Makes the Start and End values of a given range correspond to provided chunk sizes.
        /// </summary>
        public static BytesRange Chunkify(this BytesRange range, ChunksData chunksData)
        {
            long chunkSize = chunksData.ChunkSize;
            long bottom = (range.Start / chunkSize) * chunkSize;

            if (range.End == -1)
            {
                return new BytesRange(bottom, -1);
            }

            long top = ((range.End / chunkSize) + 1) * (chunkSize) - 1;

            if (top > chunksData.Chunks.Length * chunkSize)
            {
                top = range.End;
            }

            return new BytesRange(bottom, top);
        }
    }
}