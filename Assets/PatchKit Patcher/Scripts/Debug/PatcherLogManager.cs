using System;
using System.IO;
using JetBrains.Annotations;
using PatchKit.Logging;
using PatchKit.Unity.Utilities;
using PatchKit_Patcher.Scripts;
using UnityEngine;
using UniRx;

namespace PatchKit.Unity.Patcher.Debug
{
    public class PatcherLogManager : MonoBehaviour
    {
        private StreamWriter _logWriter;

        private string _logPath;
        private string _logGuid;
        private string _logUrl;

        private void CreateLogFile()
        {
            try
            {
                _logPath = Path.GetTempFileName();

                if (File.Exists(_logPath))
                {
                    File.Delete(_logPath);
                }

                _logWriter = new StreamWriter(
                    path: _logPath,
                    append: false);
            }
            catch
            {
                // For safety ignore all exceptions
            }
        }

        private void Awake()
        {
            CreateLogFile();

            _logGuid = Guid.NewGuid().ToString();

            _logUrl = LibPatchKitApps.GetLogUrlPrediction(
                guid: _logGuid,
                appId: "patcher-unity");
        }
    }
}