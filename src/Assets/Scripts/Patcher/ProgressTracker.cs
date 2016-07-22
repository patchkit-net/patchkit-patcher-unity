using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchKit.Unity.Patcher
{
    internal class ProgressTracker
    {
        public delegate void OnProgressHandler(float progress);

        public class Task
        {
            private readonly ProgressTracker _progressTracker;

            private float _progress;

            public readonly float Weight;

            internal Task(float weight, ProgressTracker progressTracker)
            {
                if (weight <= 0.0f)
                {
                    throw new ArgumentOutOfRangeException("weight");
                }

                _progressTracker = progressTracker;
                Weight = weight;
            }

            public event OnProgressHandler OnProgress;

            internal void InvokeOnProgress()
            {
                if (OnProgress != null)
                {
                    OnProgress.Invoke(Progress);
                }

                _progressTracker.InvokeOnProgress();
            }

            public float Progress
            {
                set
                {
                    if (_progress < 0.0f || _progress > 1.0f)
                    {
                        throw new ArgumentOutOfRangeException("value");
                    }

                    _progress = value;

                    InvokeOnProgress();
                }
                get
                {
                    return _progress;
                }
            }
        }

        private void InvokeOnProgress()
        {
            if (OnProgress != null)
            {
                OnProgress.Invoke(Progress);
            }
        }

        private readonly HashSet<Task> _tasks = new HashSet<Task>();

        public event OnProgressHandler OnProgress;

        public float Progress
        {
            get
            {
                if (_tasks.Count == 0)
                {
                    return 0.0f;
                }

                return _tasks.Sum(task => task.Progress * task.Weight)/_tasks.Sum(task => task.Weight);
            }
        }

        public Task AddNewTask(float weight)
        {
            var task = new Task(weight, this);

            _tasks.Add(task);

            InvokeOnProgress();

            return task;
        }
    }
}
