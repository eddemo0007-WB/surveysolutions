﻿using MvvmCross.Commands;
using WB.Core.SharedKernels.Enumerator.Properties;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;

namespace WB.Core.SharedKernels.Enumerator.ViewModels
{
    public class DiagnosticsViewModel : BasePrincipalViewModel
    {
        private readonly ITabletDiagnosticService tabletDiagnosticService;
        
        private readonly IDeviceSettings deviceSettings;

        public DiagnosticsViewModel(IPrincipal principal, 
            IViewModelNavigationService viewModelNavigationService,
            IDeviceSettings deviceSettings, 
            ITabletDiagnosticService tabletDiagnosticService,
            SendTabletInformationViewModel sendTabletInformationViewModel,
            CheckNewVersionViewModel checkNewVersion,
            BackupRestoreViewModel backupRestore,
            BandwidthTestViewModel bandwidthTest,
            SendLogsViewModel logsViewModel,
            MemoryStatusViewModel memoryStatus) : base(principal, viewModelNavigationService, false)
        {
            this.deviceSettings = deviceSettings;
            this.tabletDiagnosticService = tabletDiagnosticService;
            this.Logs = logsViewModel;
            this.MemoryStatus = memoryStatus;
            this.TabletInformation = sendTabletInformationViewModel;
            this.CheckNewVersion = checkNewVersion;
            this.BackupRestore = backupRestore;
            this.BandwidthTest = bandwidthTest;
        }

        public MemoryStatusViewModel MemoryStatus { get; set; }
        public SendTabletInformationViewModel TabletInformation { get; set; }
        public CheckNewVersionViewModel CheckNewVersion { get; set; }
        public BackupRestoreViewModel BackupRestore { get; set; }
        public BandwidthTestViewModel BandwidthTest { get; set; }

        public SendLogsViewModel Logs { set; get; }

        public IMvxCommand ShareDeviceTechnicalInformationCommand => new MvxCommand(this.ShareDeviceTechnicalInformation);
        
        public bool IsAuthenticated => this.Principal.IsAuthenticated;

        private void ShareDeviceTechnicalInformation()
        {
            this.tabletDiagnosticService.LaunchShareAction(EnumeratorUIResources.Share_to_Title,
                this.deviceSettings.GetDeviceTechnicalInformation());
        }
    }
}
