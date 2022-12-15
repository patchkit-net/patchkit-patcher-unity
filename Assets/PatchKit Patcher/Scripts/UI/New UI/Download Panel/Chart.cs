using System.Collections.Generic;
using System.Linq;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Utilities;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class Chart : MonoBehaviour
    {
        public int NumberOfSamples = 100;
        public float WaitTime = 1;
        public Transform DownloadSpeedTransform;

        private float _maxSpeed = 30;
        private int _id = 0;
        private int _smaNumber = 1;
        private readonly float _lastSamplesSma = 50;
        private Queue<double> _heights = new Queue<double>();
        private Image _image;
        private float _timer;
        private float _chartWidth;
        private DownloadData _downloadData;

        private struct DownloadData
        {
            public double Speed;
            public double Bytes;
            public long StartBytes;
            public long TotalBytes;
        }

        private void Start()
        {
            _image = GetComponent<Image>();
            Rect rect = ((RectTransform) transform).rect;
            _chartWidth = rect.width;
            _image.material.SetInt("_NumberOfSamples", NumberOfSamples);

            IObservable<IReadOnlyDownloadStatus> downloadStatus = Patcher.Instance.UpdaterStatus
                .SelectSwitchOrNull(u => u.LatestActiveOperation)
                .Select(s => s as IReadOnlyDownloadStatus);

            IObservable<double> speed = downloadStatus.SelectSwitchOrDefault(d => d.BytesPerSecond, 0);
            IObservable<long> startBytes = downloadStatus.SelectSwitchOrDefault(d => d.StartBytes, 0);
            IObservable<long> totalBytes = downloadStatus.SelectSwitchOrDefault(d => d.TotalBytes, 0);
            IObservable<long> bytes = downloadStatus.SelectSwitchOrDefault(d => d.Bytes, 0);


            Patcher.Instance.State
                .CombineLatest(bytes, speed, startBytes, totalBytes,
                    (state, bytesValue, speedValue, startBytesValue, totalBytesValue) => new DownloadData
                    {
                        Bytes = bytesValue,
                        Speed = speedValue,
                        StartBytes = startBytesValue,
                        TotalBytes = totalBytesValue
                    })
                .ObserveOnMainThread()
                .Subscribe(downloadData => _downloadData = downloadData)
                .AddTo(this);

            Patcher.Instance.State.ObserveOnMainThread().Where(s => s != PatcherState.UpdatingApp).Subscribe(state =>
            {
                _image.material.SetFloat("_StepMax", 0);
            });
        }

        private void Update()
        {
            if (!(_downloadData.Bytes > _downloadData.StartBytes) ||
                !(_downloadData.Bytes < _downloadData.TotalBytes))
            {
                return;
            }

            _timer += Time.deltaTime;

            if (_timer > WaitTime)
            {
                OnUpdate();
                _timer -= WaitTime;
            }
        }

        private void OnUpdate()
        {
            double[] heights = _heights.Reverse().Take(_smaNumber - 1).ToArray();
            double averageHeights = _downloadData.Speed;
            foreach (var height in heights)
            {
                averageHeights += height;
            }

            averageHeights /= _smaNumber;

            _heights.Enqueue(averageHeights);
            if (_heights.Count > NumberOfSamples)
            {
                _heights.Dequeue();
            }
            else
            {
                _id++;
                if (_id < _lastSamplesSma)
                    _smaNumber++;
            }

            DownloadSpeedTransform.localPosition = new Vector3((float) _id / NumberOfSamples * _chartWidth + 7.8f,
                DownloadSpeedTransform.localPosition.y);

            _maxSpeed = (float) _heights.Max();
            var array = _heights.Select(h => (float) h / _maxSpeed).Concat(new float[NumberOfSamples - _id]).ToArray();

            _image.material.SetInt("_NumberOfSamples", NumberOfSamples);
            _image.material.SetFloatArray("_StepHeight", array);
            _image.material.SetFloat("_StepMax", (float) _id / NumberOfSamples);
        }
    }
}