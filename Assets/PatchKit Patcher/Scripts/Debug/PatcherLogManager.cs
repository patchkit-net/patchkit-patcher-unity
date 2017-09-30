using System;
using UnityEngine;
using UniRx;

namespace PatchKit.Unity.Patcher.Debug
{
    public class PatcherLogManager : MonoBehaviour
    {
        private PatcherLogStream _stream;

        private PatcherTemporaryLogFile _tempFile;

        private PatcherLogRegisterTriggers _registerTriggers;

        private PatcherLogStorage _storage;
        
        private PatcherLogSentryRegistry _sentryRegistry;

        public bool IgnoreEditorErrors = true;

        private bool _isEditor;
        
        private void Awake()
        {
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
                    if (Application.isEditor && IgnoreEditorErrors)
                    {
                        return;
                    }
                    
                    _tempFile.Flush();
                    StartCoroutine(_storage.SendLogFileCoroutine(_tempFile.FilePath));
                }).AddTo(this);
        }
    }
}