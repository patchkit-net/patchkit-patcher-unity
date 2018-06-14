using System.Collections;
using PatchKit.Api;
using PatchKit.Apps.Updating;
using PatchKit.Network;
using UnityEngine;

namespace PatchKit.Patching.Unity.UI
{
    public abstract class UIApiComponent : MonoBehaviour
    {
        private Coroutine _loadCoroutine;

        private bool _isDirty;

        private IApiConnection _apiConnection;

        public bool LoadOnStart = true;

        protected IApiConnection ApiConnection
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
            _apiConnection = DependencyResolver.Resolve<IApiConnection>();
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