using System.Collections.Generic;
using System.Linq;

namespace PatchKit.Unity.Patcher.Statistics
{
    internal class ComplexProgressReporter : IProgressReporter
    {
        private class Child
        {
            public readonly IProgressReporter ProgressReporter;

            public readonly double Weight;

            public Child(IProgressReporter progressReporter, double weight)
            {
                ProgressReporter = progressReporter;
                Weight = weight;
            }

            public double Progress
            {
                get { return ProgressReporter.Progress*Weight; }
            }
        }

        private readonly HashSet<Child> _children = new HashSet<Child>();

        public event ProgressHandler OnProgress;

        public double Progress
        {
            get
            {
                if (_children.Count == 0)
                {
                    return 0.0;
                }
                return _children.Sum(c => c.Progress)/_children.Sum(c => c.Weight);
            }
        }

        public void AddChild(IProgressReporter progressReporter, double weight)
        {
            _children.Add(new Child(progressReporter, weight));
            progressReporter.OnProgress += progress => InvokeOnProgress();
            InvokeOnProgress();
        }

        public void RemoveChild(IProgressReporter progressReporter)
        {
            _children.RemoveWhere(child => child.ProgressReporter == progressReporter);
            InvokeOnProgress();
        }

        private void InvokeOnProgress()
        {
            if (OnProgress != null)
            {
                OnProgress(Progress);
            }
        }
    }
}