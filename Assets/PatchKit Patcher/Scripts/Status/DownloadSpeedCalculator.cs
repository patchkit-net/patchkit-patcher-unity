using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchKit.Unity.Patcher.Status
{
    public class DownloadSpeedCalculator
    {
        private struct Sample
        {
            public long Bytes;

            public long Duration;

            public DateTime AddTime;
        }

        private const long SampleLifeTime = 10000;

        private const long MinimumDelayBetweenSamples = 1000;

        private readonly List<Sample> _samples = new List<Sample>();

        private long _lastBytes;

        private DateTime _lastTime;

        private void CleanOldSamples(DateTime time)
        {
            _samples.RemoveAll(s => (time - s.AddTime).TotalMilliseconds > SampleLifeTime);
        }

        public void Restart(DateTime time)
        {
            _lastBytes = 0;
            _lastTime = time;
            _samples.Clear();
        }

        public void AddSample(long bytes, DateTime time)
        {
            long duration = (long) (time - _lastTime).TotalMilliseconds;

            if (duration < MinimumDelayBetweenSamples)
            {
                return;
            }

            CleanOldSamples(time);

            _samples.Add(new Sample
            {
                Bytes = bytes - _lastBytes,
                Duration = duration,
                AddTime = time
            });

            _lastBytes = bytes;
            _lastTime = time;
        }

        public double BytesPerSecond
        {
            get
            {
                long bytes = _samples.Sum(s => s.Bytes);
                long duration = _samples.Sum(s => s.Duration);

                if (bytes == 0)
                {
                    return 0.0;
                }

                return bytes / (duration / 1000.0);
            }
        }
    }
}