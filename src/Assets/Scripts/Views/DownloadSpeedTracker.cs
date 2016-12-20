using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchKit.Unity.Patcher.Views
{
    internal class DownloadSpeedTracker
    {
        private struct Sample
        {
            public long Bytes;

            public long Duration;

            public DateTime AddTime;
        }

        private readonly List<Sample> _samples = new List<Sample>();

        private void CleanOldSamples()
        {
            _samples.RemoveAll(s => (DateTime.Now - s.AddTime).TotalMilliseconds > SampleLifeTime);
        }

        public long SampleLifeTime = 10000;

        public void AddSample(long bytes, long duration)
        {
            _samples.Add(new Sample
            {
                Bytes = bytes,
                Duration = duration,
                AddTime = DateTime.Now
            });
        }

        public double DownloadSpeed
        {
            get
            {
                CleanOldSamples();

                long bytes = _samples.Sum(s => s.Bytes);
                long duration = _samples.Sum(s => s.Duration);

                if (bytes == 0)
                {
                    return 0.0;
                }

                return bytes / 1024.0 / duration;
            }
        }
    }
}