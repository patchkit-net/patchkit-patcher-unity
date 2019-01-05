using System;
using JetBrains.Annotations;
using PatchKit.Apps.Updating;
using PatchKit.Apps.Updating.Debug;
using PatchKit.Logging;
using UniRx;
using UnityEngine;

namespace PatchKit.Patching.Unity.Debug
{
    public class LogManager : MonoBehaviour
    {
        private static LogManager _instance;
        
        [NotNull]
        public static LogManager Instance
        {
            get
            {
                UnityDispatcher.Invoke(() =>
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<LogManager>();
                    }
                }).WaitOne();
                return _instance;
            }
        }

        private DebugLogger _debugLogger;

        private LogStream _stream;

        private TemporaryLogFile _tempFile;

        private LogRegisterTriggers _registerTriggers;

        private LogStorage _storage;
        
        private LogSentryRegistry _sentryRegistry;

        public bool IgnoreEditorErrors = true;

        private bool _isEditor;

        private void Awake()
        {
            _debugLogger = new DebugLogger(typeof(LogManager));
            //var messagesStream = DependencyResolver.Resolve<IMessagesStream>();
            //messagesStream.Subscribe(new UnityMessageWriter(new SimpleMessageFormatter()));

            _isEditor = Application.isEditor;
            _stream = new LogStream();
            _tempFile = new TemporaryLogFile();
            _registerTriggers = new LogRegisterTriggers();
            _storage = new LogStorage();
            _sentryRegistry = new LogSentryRegistry();

            // Automatically call dispose on those objects when OnDestroy is called.
            _stream.AddTo(this);
            _tempFile.AddTo(this);
            _registerTriggers.AddTo(this);

            _stream.Messages.Subscribe(_tempFile.WriteLine).AddTo(this);

            _registerTriggers.ExceptionTrigger.Subscribe(e =>
                {
                    if (_isEditor && IgnoreEditorErrors)
                    {
                        return;
                    }
                    
                    _sentryRegistry.RegisterWithException(e, _storage.Guid.ToString());
                })
                .AddTo(this);

            _registerTriggers.ExceptionTrigger.Throttle(TimeSpan.FromSeconds(5))
                .Subscribe(e =>
                {
                    if (_isEditor && IgnoreEditorErrors)
                    {
                        return;
                    }
                    
                    _tempFile.Flush();
                    StartCoroutine(_storage.SendLogFileCoroutine(_tempFile.FilePath));
                }).AddTo(this);
        }

        private void OnApplicationQuit()
        {
            if (!_storage.IsLogBeingSent)
            {
                return;
            }

            _debugLogger.Log("Cancelling application quit because log is being sent or is about to be sent.");
            _storage.AbortSending();
            Application.CancelQuit();
        }
    }
}