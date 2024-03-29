﻿using System;
using PatchKit.Unity.Patcher.AppData.FileSystem;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using UniRx;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    internal class ProgressBytesFilesStatus : IReadOnlyOperationStatus
    {
        public ReactiveProperty<long> Bytes { get; private set; }
        public ReactiveProperty<long> BytesCurrentFile { get; private set; }
        public ReactiveProperty<long> TotalBytes { get; private set; }
        public ReactiveProperty<double> Progress { get; private set; }
        public ReactiveProperty<double> Weight { get; private set; }
        public ReactiveProperty<bool> IsActive { get; private set; }
        public ReactiveProperty<string> Description { get; private set; }
        public ReactiveProperty<bool> IsIdle { get; private set; }

        private string FilePath { get; set; }

        public ProgressBytesFilesStatus(string description)
        {
            Progress = new ReactiveProperty<double>();
            Weight = new ReactiveProperty<double>();
            IsActive = new ReactiveProperty<bool>();
            Description = new ReactiveProperty<string>();
            IsIdle = new ReactiveProperty<bool>();
            Bytes = new ReactiveProperty<long>();
            TotalBytes = new ReactiveProperty<long>();
            BytesCurrentFile = new ReactiveProperty<long>();

            Progress = Bytes.CombineLatest(BytesCurrentFile, TotalBytes,
                (bytes, currentBytes, totalBytes) =>
                    totalBytes > 0 ? (double) (bytes + currentBytes) / (double) totalBytes : 0.0).ToReactiveProperty();
            Description = Bytes.CombineLatest(BytesCurrentFile, TotalBytes,
                (bytes, currentBytes, totalBytes) => string.Format("{0}: {1:0.0} MB of {2:0.0} MB", description,
                    (bytes + currentBytes) / 1024.0 / 1024.0,
                    totalBytes / 1024.0 / 1024.0)).ToReactiveProperty();

            IDisposable updateBytesObservable = Observable
                .Interval(TimeSpan.FromSeconds(1), Scheduler.MainThread)
                .ObserveOn(Scheduler.MainThread)
                .Subscribe(_ =>
                {
                    if (IsActive.Value)
                    {
                        UpdateBytes();
                    }
                });

            IsActive.Subscribe(isActive =>
            {
                if (!isActive && Math.Abs(Progress.Value - 1) < 0.8)
                {
                    updateBytesObservable.Dispose();
                }
            });
        }

        public void ObserveFile(string pathFile)
        {
            UpdateBytes();
            Bytes.Value += BytesCurrentFile.Value;
            BytesCurrentFile.Value = 0;
            FilePath = pathFile;
        }
        
        
        public void EndObserveFile()
        {
            UpdateBytes();
            Bytes.Value += BytesCurrentFile.Value;
            BytesCurrentFile.Value = 0;
            FilePath = null;
        }

        private void UpdateBytes()
        {
            BytesCurrentFile.Value = FileOperations.GetSizeFile(FilePath);
        }

        IReadOnlyReactiveProperty<double> IReadOnlyOperationStatus.Progress
        {
            get { return Progress; }
        }

        IReadOnlyReactiveProperty<double> IReadOnlyOperationStatus.Weight
        {
            get { return Weight; }
        }

        IReadOnlyReactiveProperty<bool> IReadOnlyOperationStatus.IsActive
        {
            get { return IsActive; }
        }

        IReadOnlyReactiveProperty<string> IReadOnlyOperationStatus.Description
        {
            get { return Description; }
        }

        IReadOnlyReactiveProperty<bool> IReadOnlyOperationStatus.IsIdle
        {
            get { return IsIdle; }
        }
    }
}