﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Logging;
using Plugin.Permissions.Abstractions;
using WB.Core.BoundedContexts.Supervisor.Properties;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.Enumerator.OfflineSync.Entities;
using WB.Core.SharedKernels.Enumerator.OfflineSync.Services;
using WB.Core.SharedKernels.Enumerator.OfflineSync.ViewModels;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;

namespace WB.Core.BoundedContexts.Supervisor.ViewModel
{
    [ExcludeFromCodeCoverage()] // TODO: remove attribute when UI binding completed
    public class OfflineSupervisorSyncViewModel : BaseOfflineSyncViewModel, IOfflineSyncViewModel
    {
        private readonly IInterviewViewModelFactory viewModelFactory;

        public OfflineSupervisorSyncViewModel(IPrincipal principal,
            IViewModelNavigationService viewModelNavigationService,
            IPermissionsService permissions,
            IEnumeratorSettings settings,
            INearbyCommunicator communicator,
            INearbyConnection nearbyConnection,
            IInterviewViewModelFactory viewModelFactory)
            : base(principal, viewModelNavigationService, permissions, nearbyConnection, settings)
        {

            SetStatus(ConnectionStatus.WaitingForGoogleApi);
            communicator.IncomingInfo.Subscribe(OnIncomingData);
            this.viewModelFactory = viewModelFactory;

        }

        private string title;
        public string Title
        {
            get => this.title;
            set => this.SetProperty(ref this.title, value);
        }

        private bool hasConnectedDevices = true;
        public bool HasConnectedDevices
        {
            get => this.hasConnectedDevices;
            set => this.SetProperty(ref this.hasConnectedDevices, value);
        }

        private bool allSynchronizationsFinished;
        public bool AllSynchronizationsFinished
        {
            get => this.allSynchronizationsFinished;
            set => this.SetProperty(ref this.allSynchronizationsFinished, value);
        }

        private string progressTitle;
        public string ProgressTitle
        {
            get => this.progressTitle;
            set => this.SetProperty(ref this.progressTitle, value);
        }

        private ObservableCollection<ConnectedDeviceViewModel> connectedDevices;
        public ObservableCollection<ConnectedDeviceViewModel> ConnectedDevices
        {
            get => this.connectedDevices;
            set => this.SetProperty(ref this.connectedDevices, value);
        }

        public override async Task Initialize()
        {
            this.Title = SupervisorUIResources.OfflineSync_ReceivingInterviewsFromDevices;
            this.ProgressTitle = string.Format(SupervisorUIResources.OfflineSync_NoDevicesDetectedFormat,
                this.principal.CurrentUserIdentity.Name);

            this.ConnectedDevices = new ObservableCollection<ConnectedDeviceViewModel>(new[]
            {
                this.viewModelFactory.GetNew<ConnectedDeviceViewModel>(),
                this.viewModelFactory.GetNew<ConnectedDeviceViewModel>()
            });

            await this.ConnectedDevices[0].Initialize();
            await this.ConnectedDevices[1].Initialize();
        }

        private void SetStatus(ConnectionStatus connectionStatus, string details = null) 
            => this.ProgressTitle = $"{this.GetServiceName()}\r\n{connectionStatus.ToString()}\r\n{details ?? String.Empty}";

        protected override void OnDeviceConnected(string name) => SetStatus(ConnectionStatus.Connected, "Connected to " + name);
        protected override void OnDeviceDisconnected(string name) => SetStatus(ConnectionStatus.Disconnected, "Disconnected from " + name);

        protected override void OnDeviceFound(string name) => SetStatus(ConnectionStatus.Connecting, $"Found {name}. Requesting conection");

        protected override void OnDeviceConnectionRequested(string name) => SetStatus(ConnectionStatus.Connecting, $"Requested conection to {name}");

        public IMvxAsyncCommand StartSynchronization => new MvxAsyncCommand(() =>
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            return this.StartAdvertising();
        });

        protected async Task StartAdvertising()
        {
            await this.permissions.AssureHasPermission(Permission.Location);

            Log.Trace("StartAdvertising");

            SetStatus(ConnectionStatus.StartAdvertising, $"Starting advertising");
            var serviceName = this.GetServiceName();
            try
            {
                await this.nearbyConnection.StartAdvertisingAsync(serviceName, this.principal.CurrentUserIdentity.Name, cancellationTokenSource.Token);
                SetStatus(ConnectionStatus.Advertising, "Waiting for interviewers connections");
            }
            catch (NearbyConnectionException nce)
            {
                SetStatus(ConnectionStatus.Error, nce.Message);
            }
        }

        private void OnIncomingData(IncomingDataInfo dataInfo)
        {
            SetStatus(ConnectionStatus.Sync, dataInfo.ToString());
        }
        
        protected override string GetDeviceIdentification() => this.principal.CurrentUserIdentity.UserId.FormatGuid();

        public IMvxCommand CancelCommand => new MvxCommand(() => {});
    }
}
