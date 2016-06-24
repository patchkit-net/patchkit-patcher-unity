using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace PatchKit.Unity.Common
{
    [AddComponentMenu("")]
    public class Dispatcher : MonoBehaviour
    {
        private static Dispatcher _instance;

        private static Thread _mainThread;

        private static void ValidateInstance()
        {
            if(_instance == null)
            {
                throw new InvalidOperationException("Dispatcher has to be initialized before any usage.");
            }
        }

        public static void Initialize()
        {
            if (_instance == null)
            {
                var gameObject = new GameObject("_CoroutineDispatcher");

                DontDestroyOnLoad(gameObject);

                _instance = gameObject.AddComponent<Dispatcher>();

                _mainThread = Thread.CurrentThread;
            }
        }

        private static EventWaitHandle BaseInvoke(Action<ManualResetEvent> actionStarter)
        {
            ValidateInstance();

            ManualResetEvent manualResetEvent = new ManualResetEvent(false);

            if (_mainThread == Thread.CurrentThread)
            {
                actionStarter(manualResetEvent);
            }
            else
            {
                lock (_instance._pendingActions)
                {
                    _instance._pendingActions.Enqueue(() => actionStarter(manualResetEvent));
                }
            }

            return manualResetEvent;
        }

        public static EventWaitHandle Invoke(Action action)
        {
            return BaseInvoke(manualResetEvent => ActionWithEventWaitHandle(action, manualResetEvent));
        }

        private static void ActionWithEventWaitHandle(Action action, ManualResetEvent manualResetEvent)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                manualResetEvent.Set();
            }
        }

        public static EventWaitHandle InvokeCoroutine(IEnumerator coroutine)
        {
            return BaseInvoke(manualResetEvent => _instance.StartCoroutine(CoroutineWithEventWaitHandle(coroutine, manualResetEvent)));
        }

        private static IEnumerator CoroutineWithEventWaitHandle(IEnumerator coroutine, ManualResetEvent manualResetEvent)
        {
            while (true)
            {
                try
                {
                    if (!coroutine.MoveNext())
                    {
                        break;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }

                yield return coroutine.Current;
            }

            manualResetEvent.Set();
        }

        private readonly Queue<Action> _pendingActions = new Queue<Action>();

        private void Update()
        {
            lock (_pendingActions)
            {
                while (_pendingActions.Count > 0)
                {
                    Action action = _pendingActions.Dequeue();
                    action();
                }
            }
        }
    }
}
