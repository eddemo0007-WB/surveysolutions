﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using PCLStorage;
using WB.Core.BoundedContexts.Interviewer.Services;
using WB.Core.BoundedContexts.Interviewer.Views;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Infrastructure.Shared.Enumerator;

namespace WB.UI.Interviewer.Settings
{
    internal class InterviewerSettings : IInterviewerSettings
    {
        private readonly IAsyncPlainStorage<ApplicationSettingsView> settingsStorage;
        private readonly IAsyncPlainStorage<InterviewerIdentity> interviewersPlainStorage;
        private readonly IAsyncPlainStorage<InterviewView> interviewViewRepository;
        private readonly IAsyncPlainStorage<QuestionnaireView> questionnaireViewRepository;
        private readonly ISyncProtocolVersionProvider syncProtocolVersionProvider;
        private readonly IFileSystemAccessor fileSystemAccessor;
        public InterviewerSettings(
            IAsyncPlainStorage<ApplicationSettingsView> settingsStorage, 
            ISyncProtocolVersionProvider syncProtocolVersionProvider, 
            IAsyncPlainStorage<InterviewerIdentity> interviewersPlainStorage, 
            IAsyncPlainStorage<InterviewView> interviewViewRepository, 
            IAsyncPlainStorage<QuestionnaireView> questionnaireViewRepository, IFileSystemAccessor fileSystemAccessor)
        {
            this.settingsStorage = settingsStorage;
            this.syncProtocolVersionProvider = syncProtocolVersionProvider;
            this.interviewersPlainStorage = interviewersPlainStorage;
            this.interviewViewRepository = interviewViewRepository;
            this.questionnaireViewRepository = questionnaireViewRepository;
            this.fileSystemAccessor = fileSystemAccessor;
        }

        private ApplicationSettingsView CurrentSettings => this.settingsStorage.Query(settings => settings.FirstOrDefault()) ?? new ApplicationSettingsView
        {
            Id = "settings",
            Endpoint = string.Empty,
            HttpResponseTimeoutInSec = Application.Context.Resources.GetInteger(Resource.Integer.HttpResponseTimeout),
            CommunicationBufferSize = Application.Context.Resources.GetInteger(Resource.Integer.BufferSize),
            GpsResponseTimeoutInSec = Application.Context.Resources.GetInteger(Resource.Integer.GpsReceiveTimeoutSec)
        };

        public string Endpoint => this.CurrentSettings.Endpoint;
        public TimeSpan Timeout => new TimeSpan(0, 0, this.CurrentSettings.HttpResponseTimeoutInSec);
        public int BufferSize => this.CurrentSettings.CommunicationBufferSize;
        public bool AcceptUnsignedSslCertificate => false;
        public int GpsReceiveTimeoutSec => this.CurrentSettings.GpsResponseTimeoutInSec;

        public string GetDeviceId()
        {
            return Android.Provider.Settings.Secure.GetString(Application.Context.ContentResolver,
                Android.Provider.Settings.Secure.AndroidId);
        }

        public string GetApplicationVersionName()
        {
            return Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, 0).VersionName;
        }

        public string GetDeviceTechnicalInformation()
        {
            var interviewIds = string.Join(","+ Environment.NewLine, this.interviewViewRepository.Query(_ => _.Select(i => i.InterviewId)));
            var questionnaireIds = string.Join(","+ Environment.NewLine, this.questionnaireViewRepository.Query(_ => _.Select(i => i.Identity)));

            return $"Version:{this.GetApplicationVersionName()} {Environment.NewLine}" +
                   $"SyncProtocolVersion:{this.syncProtocolVersionProvider.GetProtocolVersion()} {Environment.NewLine}" +
                   $"User:{GetUserInformation()} {Environment.NewLine}" +
                   $"DeviceId:{this.GetDeviceId()} {Environment.NewLine}" +
                   $"RAM:{GetRAMInformation()}% {Environment.NewLine}" +
                   $"DBSize:{GetDataBaseSize()} {Environment.NewLine}" +
                   $"Endpoint:{this.Endpoint}{Environment.NewLine}" +
                   $"AcceptUnsignedSslCertificate:{this.AcceptUnsignedSslCertificate} {Environment.NewLine}" +
                   $"BufferSize:{this.BufferSize} {Environment.NewLine}" +
                   $"Timeout:{this.Timeout} {Environment.NewLine}" +
                   $"CurrentDataTime:{DateTime.Now} {Environment.NewLine}" +
                   $"QuestionnairesList:{questionnaireIds} {Environment.NewLine}" +
                   $"InterviewsList:{interviewIds}";
        }

        public int GetApplicationVersionCode()
        {
            return Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, 0).VersionCode;
        }

        public async Task SetEndpointAsync(string endpoint)
        {
            await this.SaveCurrentSettings(settings =>
            {
                settings.Endpoint = endpoint;
            });
        }

        public async Task SetHttpResponseTimeoutAsync(int timeout)
        {
            await this.SaveCurrentSettings(settings =>
            {
                settings.HttpResponseTimeoutInSec = timeout;
            });
        }

        public async Task SetGpsResponseTimeoutAsync(int timeout)
        {
            await this.SaveCurrentSettings(settings =>
            {
                settings.GpsResponseTimeoutInSec = timeout;
            });
        }

        public async Task SetCommunicationBufferSize(int bufferSize)
        {
            await this.SaveCurrentSettings(settings =>
            {
                settings.CommunicationBufferSize = bufferSize;
            });
        }

        public string ExternalStorageDirectory
        {
            get
            {
                var externalFolder =
                    this.fileSystemAccessor.CombinePath(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath,
                        "Interviewer");

                this.CreateFolderIfNeeded(externalFolder);

                return externalFolder;
            }
        }

        public string BackupFolder
        {
            get
            {
                var backupFolder= this.fileSystemAccessor.CombinePath(this.ExternalStorageDirectory, "Backup");
                CreateFolderIfNeeded(backupFolder);
                return backupFolder;
            }
        }

        public string RestoreFolder
        {
            get
            {
                var restoreFolder = this.fileSystemAccessor.CombinePath(this.ExternalStorageDirectory, "Restore");
                CreateFolderIfNeeded(restoreFolder);
                return restoreFolder;
            }
        }

        public string CrushFolder
        {
            get
            {
                var crushFolder = this.fileSystemAccessor.CombinePath(this.ExternalStorageDirectory, "Logs");
                CreateFolderIfNeeded(crushFolder);
                return crushFolder;
            }
        }

        private async Task SaveCurrentSettings(Action<ApplicationSettingsView> onChanging)
        {
            var settings = this.CurrentSettings;
            onChanging(settings);
            await this.settingsStorage.StoreAsync(settings);
        }

        private string GetUserInformation()
        {
            var currentStoredUser = this.interviewersPlainStorage.Query(_ => _.FirstOrDefault());
            if (currentStoredUser != null)
            {
                return $"{currentStoredUser.Name}: {currentStoredUser.Id}";
            }
            return "NONE";
        }

        private string GetRAMInformation()
        {
            ActivityManager activityManager = Application.Context.GetSystemService(Context.ActivityService) as ActivityManager;
            if (activityManager == null)
                return "UNKNOWN";

            ActivityManager.MemoryInfo mi = new ActivityManager.MemoryInfo();
            activityManager.GetMemoryInfo(mi);
            return
                $"{FileSizeUtils.SizeSuffix(mi.TotalMem)} total, avaliable {(int) (((double) (100*mi.AvailMem))/mi.TotalMem)}";
        }

        private string GetDataBaseSize()
        {
            return
                FileSizeUtils.SizeSuffix(this.fileSystemAccessor.GetDirectorySize(FileSystem.Current.LocalStorage.Path));
        }

        private void CreateFolderIfNeeded(string folder)
        {
            if (!this.fileSystemAccessor.IsDirectoryExists(folder))
                this.fileSystemAccessor.CreateDirectory(folder);
        }
    }
}
