using System;
using JetBrains.Annotations;
using PatchKit.IssueReporting;
using PatchKit.Logging;
using PatchKit.Unity.Utilities;
using UnityEngine;
using UniRx;

namespace PatchKit.Unity.Patcher.Debug
{
    public class PatcherLogManager : MonoBehaviour, IIssueReporter
    {
        private static DefaultLogger _defaultLogger;

        [NotNull]
        public static DefaultLogger DefaultLogger
        {
            get { return _defaultLogger ?? (_defaultLogger = new DefaultLogger(new DefaultLogStackFrameLocator())); }
        }

        private static PatcherLogManager _instance;
        
        [NotNull]
        public static PatcherLogManager Instance
        {
            get
            {
                UnityDispatcher.Invoke(() =>
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<PatcherLogManager>();
                    }
                }).WaitOne();
                return _instance;
            }
        }

        public PatcherLogSentryRegistry SentryRegistry
        {
            get 
            {
                return _sentryRegistry;
            }
        }

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(PatcherLogManager));

        private PatcherLogStream _stream;

        private PatcherTemporaryLogFile _tempFile;

        private PatcherLogRegisterTriggers _registerTriggers;

        private PatcherLogStorage _storage;

        public PatcherLogStorage Storage { get { return _storage; } }
        
        private PatcherLogSentryRegistry _sentryRegistry;

        public bool IgnoreEditorErrors = true;

        private bool _isEditor;

        private void Awake()
        {
            DefaultLogger.AddWriter(new UnityMessageWriter(new SimpleMessageFormatter()));

            _isEditor = Application.isEditor;
            _stream = new PatcherLogStream();
            _tempFile = new PatcherTemporaryLogFile();
            _registerTriggers = new PatcherLogRegisterTriggers();
            _storage = new PatcherLogStorage();
            _sentryRegistry = new PatcherLogSentryRegistry();

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

            DebugLogger.Log("Cancelling application quit because log is being sent or is about to be sent.");
            _storage.AbortSending();
            Application.CancelQuit();
        }

        public void Report(Issue issue)
        {
            if (_isEditor && IgnoreEditorErrors)
            {
                return;
            }
            
            _sentryRegistry.RegisterWithException(issue, _storage.Guid.ToString());
        }
    }
}