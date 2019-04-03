using System.Collections;
using PatchKit.Api;
using PatchKit_Patcher.Scripts;
using UnityEngine;

namespace PatchKit.Unity.UI
{
    public abstract class UIApiComponent : MonoBehaviour
    {
        private Coroutine _loadCoroutine;

        private bool _isDirty;

        private IApiConnection _mainApiConnection;

        public bool LoadOnStart = true;

        protected IApiConnection MainApiConnection
        {
            get { return _mainApiConnection; }
        }

        [ContextMenu("Reload")]
        public void SetDirty()
        {
            _isDirty = true;
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
            _mainApiConnection = LibPkAppsContainer.Resolve<IApiConnection>();
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