using UnityEngine;
using System.Collections.Generic;

namespace PatchKit.Unity.Patcher
{
    public class PatcherLocalJobs : MonoBehaviour
    {
        public interface ILocalJob
        {
            bool isDone { get; }
            void OnStart();
            void OnFinished();
            void Update();
        }

        private List<ILocalJob> _scheduledJobs = new List<ILocalJob>();
        private List<ILocalJob> _processingJobs = new List<ILocalJob>();

        private void Awake()
        {
            instance = this;
        }

        public static PatcherLocalJobs instance { get; private set; }

        public void ScheduleJob(ILocalJob job)
        {
            lock (_scheduledJobs)
            {
                _scheduledJobs.Add(job);
            }
        }

        private void Update()
        {
            var startingJobs = new List<ILocalJob>();
            var finishedJobs = new List<ILocalJob>();

            lock (_scheduledJobs)
            {
                foreach (var job in _scheduledJobs)
                {
                    job.OnStart();
                    startingJobs.Add(job);
                }

                foreach (var job in startingJobs)
                {
                    _scheduledJobs.Remove(job);
                    _processingJobs.Add(job);
                }
            }

            foreach (var job in _processingJobs)
            {
                if (job.isDone)
                {
                    finishedJobs.Add(job);
                }
                else
                {
                    job.Update();
                }
            }

            foreach (var job in finishedJobs)
            {
                job.OnFinished();
                _processingJobs.Remove(job);
            }
        }
    }
}