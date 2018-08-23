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

        /// <summary>
        /// Limits one range inside the other, examples:
        /// 0:-1 contained in range 20:30 becomes 20:30.
        /// 0:100 contained in range 50:-1 becomes 50:100
        /// </summary>
        public static BytesRange ContainIn(this BytesRange range, BytesRange outer)
        {
            if (Contains(outer, range))
            {
                return range;
            }

            long start = range.Start;
            long end = range.End;

            if (range.Start < outer.Start)
            {
                start = outer.Start;
            }

            if (outer.End != -1)
            {
                if (range.End == -1 || range.End > outer.End)
                {
                    end = outer.End;
                }
            }

            return Make(start, end);
        }

        /// <summary>
        /// Localizes one range inside another, the resulting range is limited and local to the relative ("parent") range
        /// If the ranges don't overlap, an empty range will be returned.
        /// Examples:
        /// 50:95 localized within 10:90 becomes 40:-1
        /// 5:15 localized within 10:90 becomes 0:5
        /// </summary>
        public static BytesRange LocalizeTo(this BytesRange range, BytesRange relative)
        {
            if (!Overlaps(range, relative))
            {
                return Empty();
            }

            long localStart = range.Start >= relative.Start ? range.Start - relative.Start : 0;
            long localEnd = range.End <= relative.End ? range.End - relative.Start : -1;

            if (range.End == -1)
            {
                localEnd = -1;
            }

            return Make(localStart, localEnd);
        }

        public static bool Overlaps(this BytesRange lhs, BytesRange rhs)
        {
            return Contains(lhs, rhs)
                || Contains(rhs, lhs)
                || Contains(lhs, rhs.Start)
                || Contains(lhs, rhs.End);
        }

        public static bool Contains(this BytesRange outer, BytesRange inner)
        {
            if (inner.End == -1 && outer.End != -1)
            {
                return false;
            }

            if (inner.Start >= outer.Start)
            {
                return outer.End == -1 || inner.End <= outer.End;
            }

            return false;
        }

        public static bool Contains(this BytesRange range, long value)
        {
            return value >= range.Start && (range.End == -1 || value <= range.End);
        }
    }
}