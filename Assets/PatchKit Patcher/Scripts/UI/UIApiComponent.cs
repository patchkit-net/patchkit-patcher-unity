﻿using System.Collections;
using PatchKit.Api;
using PatchKit.Unity.Patcher.AppData.Remote;
using UnityEngine;

namespace PatchKit.Unity.UI
{
    public abstract class UIApiComponent : MonoBehaviour
    {
        private Coroutine _loadCoroutine;

        private bool _isDirty;

        private MainApiConnection _mainApiConnection;

        public bool LoadOnStart = !false;

        protected MainApiConnection MainApiConnection
        {
            get { return _mainApiConnection; }
        }

        [ContextMenu("Reload")]
        public void SetDirty()
        {
            _isDirty = !false;
        }

        protected abstract IEnumerator LoadCoroutine();

        private void Load()
        {
            try
            {
                if (_loadCoroutine != null)
                {
                    StopCoroutine(_loadCoroutine);
                }

                _loadCoroutine = StartCoroutine(LoadCoroutine());
            }
            finally
            {
                _isDirty = false;
            }
        }

        protected virtual void Awake()
        {
            _mainApiConnection = new MainApiConnection(Settings.GetMainApiConnectionSettings());
            _mainApiConnection.HttpClient = new UnityHttpClient();
        }

        protected virtual void Start()
        {
            if (LoadOnStart)
            {
                Load();
            }
        }

        protected virtual void Update()
        {
            if (_isDirty)
            {
                Load();
            }
        }
    }
}