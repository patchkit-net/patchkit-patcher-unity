using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace PatchKit.Unity.Utilities
{
    [AddComponentMenu("")]
    public class UnityDispatcher : MonoBehaviour
    {
        private static UnityDispatcher _instance;

        private static Thread _mainThread;

        private readonly Queue<Action> _pendingActions = new Queue<Action>();

        /// <summary>Validates that <see cref="UnityDispatcher"/> has been created. Otherwise throws exception.</summary>
        /// <exception cref="InvalidOperationException">Dispatcher hasn't been created.</exception>
        private static void ValidateInstance()
        {
            if(_instance == null)
            {
                throw new InvalidOperationException("Dispatcher has to be initialized before any usage.");
            }
        }

        /// <summary>
        /// Initializes instance of <see cref="UnityDispatcher"/>.
        /// </summary>
        public static void Initialize()
        {
            if (_instance != null)
            {
                return;
            }

            var gameObject = new GameObject("_CoroutineDispatcher")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            DontDestroyOnLoad(gameObject);

            _instance = gameObject.AddComponent<UnityDispatcher>();

            _mainThread = Thread.CurrentThread;
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
