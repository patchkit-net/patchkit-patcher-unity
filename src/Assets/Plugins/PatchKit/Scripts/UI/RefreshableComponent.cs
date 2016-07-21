using System.Collections;
using UnityEngine;

namespace PatchKit.Unity.UI
{
    public abstract class RefreshableComponent : MonoBehaviour
    {
        private bool _refresh;

        private Coroutine _refreshCoroutine;

        public void Refresh()
        {
            _refresh = true;
        }

        protected virtual void Awake()
        {
            _refresh = false;
        }

        protected virtual void Update()
        {
            if (_refresh)
            {
                if (_refreshCoroutine != null)
                {
                    StopCoroutine(_refreshCoroutine);
                }

                _refreshCoroutine = StartCoroutine(RefreshCoroutine());

                _refresh = false;
            }
        }

        protected abstract IEnumerator RefreshCoroutine();
    }
}
