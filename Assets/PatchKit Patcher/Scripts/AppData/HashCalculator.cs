using System.Collections.Generic;
using System.Data.HashFunction;
using System.IO;
using System.Linq;
using System.Text;

namespace PatchKit.Unity.Patcher.AppData
{
    public static class HashCalculator
    {
        private const ulong Seed = 42;

        public static byte[] ComputeHash(byte[] buffer, int offset, int length)
        {
            var xxHash = new xxHash(Seed);
            using (var memoryStream = new MemoryStream(buffer, offset, length))
            {
                return xxHash.ComputeHash(memoryStream);
            }
        }

        public static string ComputeHashString(byte[] buffer, int offset, int length)
        {
            byte[] hash = ComputeHash(buffer, offset, length);
            return string.Join("", hash.Select(b => b.ToString()).Reverse().ToArray());
        }

        public static string ComputeStringHash(string str)
        {
            return string.Concat(new xxHash(Seed).ComputeHash(Encoding.UTF8.GetBytes(str)).Select(b => b.ToString("X2")));
        }

        public static string ComputeFileHash(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IEnumerable<string> enumerable = new xxHash(Seed).ComputeHash(fileStream).Select(b => b.ToString("X2")).Reverse();
                return string.Join("", enumerable.ToArray()).ToLower().TrimStart('0');
            }
        }
    }
}
