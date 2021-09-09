using System;
using System.Collections;
using System.Threading;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.Animations
{
    public class AnimationManager
    {
        private const float Duration = 0.1f;
        private static EventWaitHandle _scrolling;
        private bool _stop;
        private const float Constant = 5f;

        public void DoScrolling(Transform transform, Vector3 newPosition, Action nextAction)
        {
            StopScrolling();
            _scrolling = UnityDispatcher.InvokeCoroutine(Scrolling(transform, newPosition, nextAction));
        }

        public void StopScrolling()
        {
            if (_scrolling != null)
            {
                _stop = true;
                _scrolling = null;
            }
        }

        IEnumerator Scrolling(Transform transform, Vector3 newPosition, Action nextAction)
        {
            float difference = Math.Abs(transform.localPosition.y - newPosition.y);
            float split = Duration / 0.008f;
            if (transform.localPosition.y < newPosition.y)
            {
                while (transform.localPosition.y < newPosition.y && !_stop)
                {
                    yield return new WaitForSeconds(0.008f);
                    difference = Math.Abs(transform.localPosition.y - newPosition.y);
                    transform.localPosition += Vector3.up * ((difference + Constant) / split);
                }
            }
            else
            {
                while (transform.localPosition.y > newPosition.y && !_stop)
                {
                    yield return new WaitForSeconds(0.008f);
                    difference = Math.Abs(transform.localPosition.y - newPosition.y);
                    transform.localPosition += Vector3.down * ((difference + Constant) / split);
                }
            }
            
            _scrolling = null;
            _stop = false;
            nextAction();
        }
    }
}