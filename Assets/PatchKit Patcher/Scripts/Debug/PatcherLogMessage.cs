using UnityEngine;

namespace PatchKit.Unity.Patcher.Debug
{
    public struct PatcherLogMessage
    {
        public string Message { get; set; }

        public string StackTrace { get; set; }
        
        public LogType LogType { get; set; }
    }
}