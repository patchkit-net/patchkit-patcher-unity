using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Sentry;
using UnityEngine;

namespace Debugging
{
public class PatcherLogManager : MonoBehaviour
{
    [NotNull]
    public static PatcherLogManager Instance { get; private set; }

    private StreamWriter _logWriter;
    private readonly List<string> _logBuffer = new List<string>();

    private string _logPath;
    private string _logGuid;

    [NotNull]
    private readonly object _writeToLogLock = new object();

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

    private void OpenLogWriter()
    {
        lock (_writeToLogLock)
        {
            if (_logWriter != null)
            {
                return;
            }

            try
            {
                _logWriter = new StreamWriter(
                    path: _logPath,
                    append: true);

                foreach (var l in _logBuffer)
                {
                    _logWriter.WriteLine(l);
                }

                _logBuffer.Clear();
            }
            catch
            {
                // For safety ignore all exceptions
            }
        }
    }

    private void CloseLogWriter()
    {
        lock (_writeToLogLock)
        {
            if (_logWriter == null)
            {
                return;
            }

            try
            {
                _logWriter.Dispose();
                _logWriter = null;
            }
            catch
            {
                // For safety ignore all exceptions
            }
        }
    }

    private void WriteToLog([NotNull] string text)
    {
        lock (_writeToLogLock)
        {
            if (_logWriter != null)
            {
                _logWriter.WriteLine(text);
            }
            else
            {
                _logBuffer.Add(text);
            }
        }
    }

    private void Awake()
    {
        Instance = this;

        CreateLogFile();

        _logGuid = Guid.NewGuid().ToString();

        Application.logMessageReceivedThreaded += (
            condition,
            trace,
            type) =>
        {
            WriteToLog(text: $"[{type}] {condition}\n{trace}");
        };
    }

    [NotNull]
    private static SentryClient GetSentryClient()
    {
        return new SentryClient(
            options: new SentryOptions
            {
                Dsn = new Dsn(
                    dsn:
                    "https://4e111c71954d44f2a4decedc450c392d@sentry.io/1450096"),
                Environment = "development",
                Release = $"patchkit-patcher-unity@{Version.Text}"
            });
    }

    public async Task OnException(Exception exception)
    {
        CloseLogWriter();

        string logUrl = await LibPatchKitApps.SendLogAsync(
            path: _logPath,
            guid: _logGuid,
            appId: "patcher-unity",
            appVersion: Version.Text);

        try
        {
            using (var sentryClient = GetSentryClient())
            {
                var sentryEvent = new SentryEvent(exception: exception);

                if (logUrl != null)
                {
                    sentryEvent.SetExtra(
                        key: "log_url",
                        value: logUrl);
                }

                sentryClient.CaptureEvent(@event: sentryEvent);
            }
        }
        catch
        {
            // For safety ignore all exceptions
        }

        OpenLogWriter();
    }
}
}