using System;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    internal interface ITemporaryData : IDisposable
    {
        string GetUniquePath();
    }
}