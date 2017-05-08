using System.IO;

namespace PatchKit.Unity.Utilities
{
    public class Streams
    {
        public static void Copy(Stream source, Stream destination, int bufferSize = 131072)
        {
            var buffer = new byte[bufferSize];
            int count;
            while ((count = source.Read(buffer, 0, bufferSize)) != 0)
            {
                destination.Write(buffer, 0, count);
            }
        }
    }
}