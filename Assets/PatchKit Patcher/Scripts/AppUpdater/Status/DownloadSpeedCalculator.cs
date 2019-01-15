﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchKit.Apps.Updating.AppUpdater.Status
{
    public class DownloadSpeedCalculator
    {
        private struct Sample
        {
            public long Bytes;

            public TimeSpan Duration;

            public DateTime AddTime;
        }

        private static readonly TimeSpan SampleLifeTime = TimeSpan.FromSeconds(10.0);

        private static readonly TimeSpan MinimumDelayBetweenSamples = TimeSpan.FromSeconds(1.0);

        private readonly List<Sample> _samples = new List<Sample>();

        private long _lastBytes;

        private DateTime _lastTime;

        private void CleanOldSamples(DateTime time)
        {
            _samples.RemoveAll(s => time - s.AddTime > SampleLifeTime);
        }

        public void Restart(DateTime time)
        {
            _lastBytes = 0;
            _lastTime = time;
            _samples.Clear();
        }

        public void AddSample(long bytes, DateTime time)
        {
            if (_lastBytes > bytes)
            {
                Restart(time);
            }

            var duration = time - _lastTime;

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
                double duration = _samples.Sum(s => s.Duration.TotalSeconds);

                if (bytes > 0 && duration > 0.0)
                {
                    return bytes / duration;
                }

                return 0.0;
            }
        }
    }
}