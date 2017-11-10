using System;
using System.Threading;
using MvvmCross.Core.ViewModels;
using WB.Core.BoundedContexts.Interviewer.Services;

namespace WB.Core.BoundedContexts.Interviewer.Views
{
    public class MapSynchronizationViewModel : MvxNotifyPropertyChanged
    {

        public IMapSyncBackgroundService MapSyncBackgroundService { get; set; }
        public event EventHandler SyncCompleted;


        private bool synchronizationErrorOccured;
        public bool SynchronizationErrorOccured
        {
            get => this.synchronizationErrorOccured;
            set
            {
                this.synchronizationErrorOccured = value;
                this.RaisePropertyChanged();
            }
        }

        

        private bool isSynchronizationInfoShowed;
        public bool IsSynchronizationInfoShowed
        {
            get => this.isSynchronizationInfoShowed;
            set
            {
                this.isSynchronizationInfoShowed = value;
                this.RaisePropertyChanged();
            }
        }

        private string processOperation;
        public string ProcessOperation
        {
            get => this.processOperation;
            set
            {
                if (this.processOperation == value) return;

                this.processOperation = value;
                this.RaisePropertyChanged();
            }
        }

        private string processOperationDescription;
        public string ProcessOperationDescription
        {
            get => this.processOperationDescription;
            set
            {
                this.processOperationDescription = value;
                this.RaisePropertyChanged();
            }
        }

        private bool isSynchronizationInProgress;
        public bool IsSynchronizationInProgress
        {
            get => this.isSynchronizationInProgress;
            set
            {
                this.isSynchronizationInProgress = value;
                this.RaisePropertyChanged();
            }
        }
        private CancellationTokenSource synchronizationCancellationTokenSource;

        public IMvxCommand CancelSynchronizationCommand => new MvxCommand(this.CancelSynchronizaion);
        public IMvxCommand HideSynchronizationCommand => new MvxCommand(this.HideSynchronizaion);

        public void HideSynchronizaion()
        {
            this.IsSynchronizationInfoShowed = false;
        }

        private void CancelSynchronizaion()
        {
            if (this.synchronizationCancellationTokenSource != null && !this.synchronizationCancellationTokenSource.IsCancellationRequested)
                this.synchronizationCancellationTokenSource.Cancel();
        }

        public void Init()
        {
            var mapSyncProgressStatus = this.MapSyncBackgroundService?.CurrentProgress;
            if (mapSyncProgressStatus != null)
            {
                mapSyncProgressStatus.Progress.ProgressChanged += ProgressOnProgressChanged;
                this.synchronizationCancellationTokenSource = mapSyncProgressStatus.CancellationTokenSource;
            }
        }

        public void Synchronize()
        {
            this.IsSynchronizationInProgress = true;
            this.synchronizationCancellationTokenSource = new CancellationTokenSource();
            IsSynchronizationInfoShowed = true;

            MapSyncBackgroundService.SyncMaps();

            var mapSyncProgressStatus = this.MapSyncBackgroundService.CurrentProgress;
            if (mapSyncProgressStatus != null)
            {
                mapSyncProgressStatus.Progress.ProgressChanged += ProgressOnProgressChanged;
                this.synchronizationCancellationTokenSource = mapSyncProgressStatus.CancellationTokenSource;
            }
        }

        private void ProgressOnProgressChanged(object sender, MapSyncProgress syncProgressInfo)
        {
            this.InvokeOnMainThread(() =>
            {
                this.IsSynchronizationInProgress = syncProgressInfo.IsRunning;
                this.ProcessOperation = syncProgressInfo.Title;
                this.ProcessOperationDescription = syncProgressInfo.Description;
                this.IsSynchronizationInProgress = syncProgressInfo.IsRunning;
                
                if (!syncProgressInfo.IsRunning)
                {
                    this.OnSyncCompleted();
                }
            });
        }

        protected virtual void OnSyncCompleted()
        {
            this.SyncCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}