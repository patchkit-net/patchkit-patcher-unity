using System;

namespace PatchKit.Unity.Patcher.Data.Local
{
    internal class LibrsyncException : Exception
    {
        public LibrsyncException(int status) : base(string.Format("librsync failure - {0}", status))
        {
        }
    }
}