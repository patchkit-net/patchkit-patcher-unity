using System;

namespace PatchKit.Unity.Patcher.Data.Local
{
    internal interface ITemporaryData : IDisposable
    {
        string GetUniquePath();
    }
}
