using System.Collections.Generic;
using System.Data.HashFunction;
using System.IO;
using System.Linq;
using System.Text;

namespace PatchKit.Unity.Patcher.Data
{
    internal static class HashUtilities
    {
        public static string ComputeStringHash(string str)
        {
            return string.Concat(new xxHash((ulong)42).ComputeHash(Encoding.UTF8.GetBytes(str)).Select(b => b.ToString("X2")));
        }

        public static string ComputeFileHash(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IEnumerable<string> enumerable = new xxHash((ulong)42).ComputeHash(fileStream).Select(b => b.ToString("X2")).Reverse();
                return string.Concat(string.Join("", enumerable.ToArray()).ToLower().TrimStart('0'));
            }
        }
    }
}
