using System;

namespace PatchKit.Unity.Patcher.Diff
{
    internal class LibrsyncException : Exception
    {
        public LibrsyncException(int status) : base(string.Format("librsync failure - {0}", status))
        {
        }
    }
}