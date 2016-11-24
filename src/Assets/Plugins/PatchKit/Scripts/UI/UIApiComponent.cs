using System.Collections;
using PatchKit.Api;
using UnityEngine;

namespace PatchKit.Unity.UI
{
    public abstract class UIApiComponent : MonoBehaviour
    {
        private Coroutine _loadCoroutine;

        private bool _isDirty;

        private ApiConnection _apiConnection;

        public bool LoadOnAwake = true;

        protected ApiConnection ApiConnection
        {
            get { return _apiConnection; }
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
            _apiConnection = new ApiConnection(Settings.GetApiConnectionSettings());

            if (LoadOnAwake)
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