using System;
using System.Net;
using System.Reflection;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    // http://stackoverflow.com/questions/6576397/how-to-specify-range-2gb-for-httpwebrequest-in-net-3-5
    public static class HttpWebRequestExt {
        static readonly MethodInfo HttpWebRequestAddRangeHelper = typeof(WebHeaderCollection).GetMethod
                                        ("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Adds a byte range header to a request for a specific range from the beginning or end of the requested data.
        /// </summary>
        /// <param name="request">The <see cref="HttpWebRequest"/> to add the range specifier to.</param>
        /// <param name="start">The starting or ending point of the range.</param>
        public static void AddRange(this HttpWebRequest request, long start) { request.AddRange(start, -1L); }

        /// <summary>Adds a byte range header to the request for a specified range.</summary>
        /// <param name="request">The <see cref="HttpWebRequest"/> to add the range specifier to.</param>
        /// <param name="start">The position at which to start sending data.</param>
        /// <param name="end">The position at which to stop sending data.</param>
        public static void AddRange(this HttpWebRequest request, long start, long end)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (start < 0) throw new ArgumentOutOfRangeException("start", "Starting byte cannot be less than 0.");
            if (end < start) end = -1;

            string key = "Range";
            string val = string.Format("bytes={0}-{1}", start, end == -1 ? "" : end.ToString());

            HttpWebRequestAddRangeHelper.Invoke(request.Headers, new object[] { key, val });
        }
    }
}
