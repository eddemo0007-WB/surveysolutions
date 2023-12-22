﻿#nullable enable
using System;
using System.Linq;
using Android.App;
using WB.Core.BoundedContexts.Interviewer.Services;
using WB.Core.BoundedContexts.Interviewer.Services.Infrastructure;
using WB.Core.BoundedContexts.Interviewer.Views;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Core.SharedKernels.Enumerator.Services.Workspace;
using WB.Core.SharedKernels.Enumerator.Views;
using WB.UI.Shared.Enumerator.Services;
using Environment = System.Environment;

namespace WB.UI.Interviewer.Settings
{
    internal class InterviewerSettings : EnumeratorSettings, IInterviewerSettings
    {
        private readonly IPlainStorage<ApplicationSettingsView> settingsStorage;
        private readonly IPlainStorage<ApplicationWorkspaceSettingsView> workspaceSettingsStorage;
        private readonly IInterviewerPrincipal principal;
        private readonly IPlainStorage<InterviewView> interviewViewRepository;
        private readonly IPlainStorage<QuestionnaireView> questionnaireViewRepository;
        private readonly IWorkspaceAccessor workspaceAccessor;

        public InterviewerSettings(IPlainStorage<ApplicationSettingsView> settingsStorage,
            IPlainStorage<ApplicationWorkspaceSettingsView> workspaceSettingsStorage,
            IInterviewerSyncProtocolVersionProvider syncProtocolVersionProvider,
            IQuestionnaireContentVersionProvider questionnaireContentVersionProvider,
            IInterviewerPrincipal principal,
            IPlainStorage<InterviewView> interviewViewRepository,
            IPlainStorage<QuestionnaireView> questionnaireViewRepository, 
            IFileSystemAccessor fileSystemAccessor,
            string backupFolder, 
            string restoreFolder,
            IWorkspaceAccessor workspaceAccessor,
            IEnvironmentInformationUtils environmentInformationUtils) : base(syncProtocolVersionProvider,
            questionnaireContentVersionProvider, fileSystemAccessor, backupFolder, restoreFolder, environmentInformationUtils)
        {
            this.settingsStorage = settingsStorage;
            this.workspaceSettingsStorage = workspaceSettingsStorage;
            this.principal = principal;
            this.interviewViewRepository = interviewViewRepository;
            this.questionnaireViewRepository = questionnaireViewRepository;
            this.workspaceAccessor = workspaceAccessor;
        }
        
        private string GetUserInformation()
        {
            var currentStoredUser = this.principal.CurrentUserIdentity;
            if (currentStoredUser != null)
            {
                return $"{currentStoredUser.Name}: {currentStoredUser.Id}";
            }
            return "NONE";
        }

        protected override string GetExternalInformation()
        {
            var interviewIds = string.Join("," + Environment.NewLine, this.interviewViewRepository.LoadAll().Select(i => i.InterviewId));
            var questionnaireIds = string.Join("," + Environment.NewLine, this.questionnaireViewRepository.LoadAll().Select(i => i.Id));

            return $"User: {GetUserInformation()} {Environment.NewLine}" +
                   $"VibrateOnError: {this.VibrateOnError} {Environment.NewLine}" +
                   $"QuestionnairesList: {questionnaireIds} {Environment.NewLine}" +
                   $"InterviewsList: {interviewIds}";
        }

        private ApplicationSettingsView currentSettings => this.settingsStorage.FirstOrDefault() ?? new ApplicationSettingsView
        {
            Id = "settings",
            Endpoint = string.Empty,
            HttpResponseTimeoutInSec = Application.Context.Resources?.GetInteger(Resource.Integer.HttpResponseTimeout) ?? 1200,
            EventChunkSize = Application.Context.Resources? .GetInteger(Resource.Integer.EventChunkSize) ?? 1000,
            CommunicationBufferSize = Application.Context.Resources?.GetInteger(Resource.Integer.BufferSize) ?? 4096,
            GpsResponseTimeoutInSec = Application.Context.Resources?.GetInteger(Resource.Integer.GpsReceiveTimeoutSec) ?? 30,
            GpsDesiredAccuracy = Application.Context.Resources?.GetInteger(Resource.Integer.GpsDesiredAccuracy) ?? 50,
            VibrateOnError = Application.Context.Resources?.GetBoolean(Resource.Boolean.VibrateOnError) ?? true
        };
        
        private ApplicationWorkspaceSettingsView? currentWorkspaceSettings
        {
            get
            {
                var workspace = workspaceAccessor.GetCurrentWorkspaceName();
                if (workspace == null)
                    return null;
                
                return this.workspaceSettingsStorage.GetById(workspace) ?? new ApplicationWorkspaceSettingsView()
                {
                    Id = workspace,
                    AllowSyncWithHq = Application.Context.Resources?.GetBoolean(Resource.Boolean.AllowSyncWithHq)
                };
            }
        }


        protected override EnumeratorSettingsView CurrentSettings => this.currentSettings;
        protected override EnumeratorWorkspaceSettingsView? CurrentWorkspaceSettings => this.currentWorkspaceSettings;

        public override bool VibrateOnError => this.currentSettings.VibrateOnError ?? Application.Context.Resources?.GetBoolean(Resource.Boolean.VibrateOnError) ?? true;

        public override double GpsDesiredAccuracy => this.currentSettings.GpsDesiredAccuracy.GetValueOrDefault(Application.Context.Resources?.GetInteger(Resource.Integer.GpsDesiredAccuracy) ?? 50);

        public override bool ShowLocationOnMap => this.currentSettings.ShowLocationOnMap.GetValueOrDefault(true);

        public override int GpsReceiveTimeoutSec => this.currentSettings.GpsResponseTimeoutInSec;

        public override int EventChunkSize => this.CurrentSettings.EventChunkSize.GetValueOrDefault(Application.Context.Resources?.GetInteger(Resource.Integer.EventChunkSize) ?? 1000);

        public bool AllowSyncWithHq => this.currentWorkspaceSettings?.AllowSyncWithHq ?? true;
        public bool IsOfflineSynchronizationDone => this.currentWorkspaceSettings?.IsOfflineSynchronizationDone ?? false;

        public void SetOfflineSynchronizationCompleted()
        {
            this.SaveCurrentSettings(settings =>
            {
               settings.IsOfflineSynchronizationDone = true;
            });
        }
        
        public void SetGpsResponseTimeout(int timeout)
        {
            this.SaveCurrentSettings(settings =>
            {
                settings.GpsResponseTimeoutInSec = timeout;
            });
        }
        
        public void SetGpsDesiredAccuracy(double value)
        {
            this.SaveCurrentSettings(settings =>
            {
                settings.GpsDesiredAccuracy = value;
            });
        }

        public void SetVibrateOnError(bool vibrateOnError)
        {
            this.SaveCurrentSettings(settings =>
            {
                settings.VibrateOnError = vibrateOnError;
            });
        }

        public void SetShowLocationOnMap(bool showLocationOnMap)
        {
            this.SaveCurrentSettings(settings => settings.ShowLocationOnMap = showLocationOnMap);
        }

        public void SetAllowSyncWithHq(bool allowSyncWithHq)
        {
            this.SaveCurrentSettings(settings =>
            {
                settings.AllowSyncWithHq = allowSyncWithHq;
            });
        }

        private void SaveCurrentSettings(Action<ApplicationSettingsView> onChanging)
        {
            var settings = this.currentSettings;
            onChanging(settings);
            SaveSettings(settings);
        }

        protected override void SaveSettings(EnumeratorSettingsView settings)
            => this.settingsStorage.Store((ApplicationSettingsView)settings);

        private void SaveCurrentSettings(Action<ApplicationWorkspaceSettingsView> onChanging)
        {
            var settings = this.currentWorkspaceSettings;
            if (settings == null)
                throw new InvalidOperationException("Saving workspace settings outside a workspace is not valid.");
            
            onChanging(settings);
            SaveSettings(settings);
        }

        protected override void SaveSettings(EnumeratorWorkspaceSettingsView settings)
            => this.workspaceSettingsStorage.Store((ApplicationWorkspaceSettingsView)settings);
    }
}
