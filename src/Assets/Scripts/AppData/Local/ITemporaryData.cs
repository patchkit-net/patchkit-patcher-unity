using System;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public interface ITemporaryData : IDisposable
    {
        string GetUniquePath();
    }
}