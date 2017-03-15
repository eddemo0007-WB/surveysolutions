using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MvvmCross.Platform;
using MvvmCross.Platform.Exceptions;
using WB.Core.BoundedContexts.Interviewer.Implementation.Services;
using WB.Core.BoundedContexts.Interviewer.Properties;
using WB.Core.BoundedContexts.Interviewer.Services.Infrastructure;
using WB.Core.BoundedContexts.Interviewer.Services.Synchronization;
using WB.Core.BoundedContexts.Interviewer.Views;
using WB.Core.BoundedContexts.Interviewer.Views.Dashboard;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Implementation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.Core.SharedKernels.Enumerator.Properties;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Core.SharedKernels.Questionnaire.Translations;

namespace WB.Core.BoundedContexts.Interviewer.Services
{
    public class SynchronizationProcess : ISynchronizationProcess
    {
        private readonly ISynchronizationService synchronizationService;
        private bool shouldUpdatePasswordOfInterviewer;
        private readonly IPlainStorage<InterviewerIdentity> interviewersPlainStorage;
        private readonly IPlainStorage<InterviewView> interviewViewRepository;
        private readonly IInterviewerInterviewAccessor interviewFactory;

        private readonly IPrincipal principal;
        private readonly ILogger logger;
        private readonly IUserInteractionService userInteractionService;
        private readonly IInterviewerQuestionnaireAccessor questionnairesAccessor;
        private readonly IAttachmentContentStorage attachmentContentStorage;
        private readonly IPlainStorage<InterviewMultimediaView> interviewMultimediaViewStorage;
        private readonly IPlainStorage<InterviewFileView> interviewFileViewStorage;
        private readonly CompanyLogoSynchronizer logoSynchronizer;
        private readonly AttachmentsCleanupService cleanupService;
        private readonly IPasswordHasher passwordHasher;
        private readonly IInterviewerSettings interviewerSettings;

        private RestCredentials restCredentials;

        public SynchronizationProcess(ISynchronizationService synchronizationService, 
            IPlainStorage<InterviewerIdentity> interviewersPlainStorage, 
            IPlainStorage<InterviewView> interviewViewRepository,
            IPrincipal principal,
            ILogger logger, 
            IUserInteractionService userInteractionService, 
            IInterviewerQuestionnaireAccessor questionnairesAccessor, 
            IAttachmentContentStorage attachmentContentStorage, 
            IInterviewerInterviewAccessor interviewFactory, 
            IPlainStorage<InterviewMultimediaView> interviewMultimediaViewStorage, 
            IPlainStorage<InterviewFileView> interviewFileViewStorage,
            CompanyLogoSynchronizer logoSynchronizer, 
            AttachmentsCleanupService cleanupService,
            IPasswordHasher passwordHasher,
            IInterviewerSettings interviewerSettings)
        {
            this.synchronizationService = synchronizationService;
            this.interviewersPlainStorage = interviewersPlainStorage;
            this.interviewViewRepository = interviewViewRepository;
            this.principal = principal;
            this.logger = logger;
            this.userInteractionService = userInteractionService;
            this.questionnairesAccessor = questionnairesAccessor;
            this.attachmentContentStorage = attachmentContentStorage;
            this.interviewFactory = interviewFactory;
            this.interviewMultimediaViewStorage = interviewMultimediaViewStorage;
            this.interviewFileViewStorage = interviewFileViewStorage;
            this.logoSynchronizer = logoSynchronizer;
            this.cleanupService = cleanupService;
            this.passwordHasher = passwordHasher;
            this.interviewerSettings = interviewerSettings;
        }

        public async Task SyncronizeAsync(IProgress<SyncProgressInfo> progress, CancellationToken cancellationToken)
        {
            SychronizationStatistics statistics = new SychronizationStatistics();
            try
            {
                progress.Report(new SyncProgressInfo
                {
                    Title = InterviewerUIResources.Synchronization_UserAuthentication_Title,
                    Description = InterviewerUIResources.Synchronization_UserAuthentication_Description,
                    Status = SynchronizationStatus.Started,
                    Statistics = statistics
                });

                this.restCredentials = this.restCredentials ?? new RestCredentials
                {
                    Login = this.principal.CurrentUserIdentity.Name,
                    Password = this.principal.CurrentUserIdentity.Password
                };

                await this.synchronizationService.CanSynchronizeAsync(token: cancellationToken, credentials: this.restCredentials);

                if (this.shouldUpdatePasswordOfInterviewer)
                {
                    this.shouldUpdatePasswordOfInterviewer = false;
                    this.UpdatePasswordOfInterviewer(this.restCredentials.Password);
                }

                cancellationToken.ThrowIfCancellationRequested();
                var deviceInfo = await this.interviewerSettings.GetDeviceInfoAsync();
                await this.synchronizationService.SendDeviceInfoAsync(this.ToDeviceInfoApiView(deviceInfo), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await this.UploadCompletedInterviewsAsync(statistics, progress, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await this.SyncronizeQuestionnairesAsync(progress, statistics, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await this.DownloadInterviewsAsync(statistics, progress, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await this.logoSynchronizer.DownloadCompanyLogo(progress, cancellationToken);

                progress.Report(new SyncProgressInfo
                {
                    Title = InterviewerUIResources.Synchronization_Success_Title,
                    Description = InterviewerUIResources.Synchronization_Success_Description,
                    Status = SynchronizationStatus.Success,
                    Statistics = statistics
                });
            }
            catch (OperationCanceledException)
            {
                progress.Report(new SyncProgressInfo
                {
                    Status = SynchronizationStatus.Stopped,
                    Statistics = statistics
                });

                return;
            }
            catch (SynchronizationException ex)
            {
                var errorTitle = InterviewerUIResources.Synchronization_Fail_Title;
                var errorDescription = ex.Message;

                switch (ex.Type)
                {
                    case SynchronizationExceptionType.RequestCanceledByUser:
                        progress.Report(new SyncProgressInfo {
                            Title = errorTitle,
                            Description = errorDescription,
                            Status = SynchronizationStatus.Canceled,
                            Statistics = statistics
                        });
                        break;
                    case SynchronizationExceptionType.Unauthorized:
                        this.shouldUpdatePasswordOfInterviewer = true;
                        break;
                    case SynchronizationExceptionType.UserLinkedToAnotherDevice:
                        progress.Report(new SyncProgressInfo
                        {
                            Title = InterviewerUIResources.Synchronization_UserLinkedToAnotherDevice_Status,
                            Description = InterviewerUIResources.Synchronization_UserLinkedToAnotherDevice_Title,
                            UserIsLinkedToAnotherDevice = true,
                            Status = SynchronizationStatus.Fail,
                            Statistics = statistics
                        });
                        break;
                    case SynchronizationExceptionType.UnacceptableSSLCertificate:
                        progress.Report(new SyncProgressInfo
                        {
                            Title = InterviewerUIResources.UnexpectedException,
                            Description = InterviewerUIResources.UnacceptableSSLCertificate,
                            Status = SynchronizationStatus.Fail,
                            Statistics = statistics
                        });
                        break;
                    default:
                        progress.Report(new SyncProgressInfo
                        {
                            Title = errorTitle,
                            Description = errorDescription,
                            Status = SynchronizationStatus.Fail,
                            Statistics = statistics
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                Mvx.Trace(ex.ToLongString());
                progress.Report(new SyncProgressInfo
                {
                    Title = InterviewerUIResources.Synchronization_Fail_Title,
                    Description = InterviewerUIResources.Synchronization_Fail_UnexpectedException,
                    Status = SynchronizationStatus.Fail,
                    Statistics = statistics
                });

                this.logger.Error("Synchronization. Unexpected exception", ex);
            }

            if (!cancellationToken.IsCancellationRequested && this.shouldUpdatePasswordOfInterviewer)
            {
                var newPassword = await this.GetNewPasswordAsync();
                if (newPassword == null)
                {
                    this.shouldUpdatePasswordOfInterviewer = false;
                    progress.Report(new SyncProgressInfo
                    {
                        Title = InterviewerUIResources.Synchronization_Fail_Title,
                        Description = InterviewerUIResources.Unauthorized,
                        Status = SynchronizationStatus.Fail,
                        Statistics = statistics
                    });
                }
                else
                {
                    this.restCredentials.Password = this.passwordHasher.Hash(newPassword);
                    await this.SyncronizeAsync(progress, cancellationToken);
                }
            }
        }

        private async Task<string> GetNewPasswordAsync()
        {
            var message = InterviewerUIResources.Synchronization_UserPassword_Update_Format.FormatString(this.principal.CurrentUserIdentity.Name);
            return await this.userInteractionService.ConfirmWithTextInputAsync(
                message: message,
                okButton: UIResources.LoginText,
                cancelButton: InterviewerUIResources.Synchronization_Cancel,
                isTextInputPassword: true);
        }

        private async Task SyncronizeQuestionnairesAsync(IProgress<SyncProgressInfo> progress, SychronizationStatistics staciStatistics, CancellationToken cancellationToken)
        {
            var remoteCensusQuestionnaireIdentities = await this.synchronizationService.GetCensusQuestionnairesAsync(cancellationToken);
            var localCensusQuestionnaireIdentities = this.questionnairesAccessor.GetCensusQuestionnaireIdentities();

            var notExistingRemoteCensusQuestionnaireIdentities = localCensusQuestionnaireIdentities.Where(
                questionnaireIdentity => !remoteCensusQuestionnaireIdentities.Contains(questionnaireIdentity));

            foreach (var censusQuestionnaireIdentity in notExistingRemoteCensusQuestionnaireIdentities)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.questionnairesAccessor.RemoveQuestionnaire(censusQuestionnaireIdentity);
            }

            var processedQuestionnaires = 0;
            var notExistingLocalCensusQuestionnaireIdentities = remoteCensusQuestionnaireIdentities.Except(localCensusQuestionnaireIdentities).ToList();
            foreach (var censusQuestionnaireIdentity in notExistingLocalCensusQuestionnaireIdentities)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.Report(new SyncProgressInfo
                {
                    Title = InterviewerUIResources.Synchronization_Download_Title,
                    Description = string.Format(InterviewerUIResources.Synchronization_Download_Description_Format,
                        processedQuestionnaires,
                        notExistingLocalCensusQuestionnaireIdentities.Count,
                        InterviewerUIResources.Synchronization_Questionnaires)
                });
                await this.DownloadQuestionnaireAsync(censusQuestionnaireIdentity, cancellationToken);

                processedQuestionnaires++;
            }

            progress.Report(new SyncProgressInfo
            {
                Title = InterviewerUIResources.Synchronization_Check_Obsolete_Questionnaires,
                Statistics = staciStatistics,
                Status = SynchronizationStatus.Download
            });

            var serverQuestionnaires = await this.synchronizationService.GetServerQuestionnairesAsync(cancellationToken);
            var localQuestionnaires = this.questionnairesAccessor.GetAllQuestionnaireIdentities();
            var questionnairesToRemove = localQuestionnaires.Except(serverQuestionnaires).ToList();
            
            int removedQuestionnairesCounter = 0;
            foreach (var questionnaireIdentity in questionnairesToRemove)
            {
                cancellationToken.ThrowIfCancellationRequested();
                removedQuestionnairesCounter++;
                progress.Report(new SyncProgressInfo
                {
                    Title = InterviewerUIResources.Synchronization_Check_Obsolete_Questionnaires,
                    Description = string.Format(InterviewerUIResources.Synchronization_Check_Obsolete_Questionnaires_Description, removedQuestionnairesCounter, questionnairesToRemove.Count),
                    Statistics = staciStatistics,
                    Status = SynchronizationStatus.Download
                });

                this.questionnairesAccessor.RemoveQuestionnaire(questionnaireIdentity);
            }
            if (questionnairesToRemove.Count > 0)
            {
                progress.Report(new SyncProgressInfo
                {
                    Title = InterviewerUIResources.Synchronization_Download_AttachmentsCleanup
                });
                this.cleanupService.RemovedOrphanedAttachments();
            }
        }

        private async Task DownloadQuestionnaireAsync(QuestionnaireIdentity questionnaireIdentity, CancellationToken cancellationToken)
        {
            if (!this.questionnairesAccessor.IsQuestionnaireAssemblyExists(questionnaireIdentity))
            {
                var questionnaireAssembly = await this.synchronizationService.GetQuestionnaireAssemblyAsync(
                    questionnaire: questionnaireIdentity,
                    onDownloadProgressChanged: (progressPercentage, bytesReceived, totalBytesToReceive) => { },
                    token: cancellationToken).ConfigureAwait(false);

                await this.questionnairesAccessor.StoreQuestionnaireAssemblyAsync(questionnaireIdentity, questionnaireAssembly).ConfigureAwait(false);
                await this.synchronizationService.LogQuestionnaireAssemblyAsSuccessfullyHandledAsync(questionnaireIdentity).ConfigureAwait(false);
            }

            if (!this.questionnairesAccessor.IsQuestionnaireExists(questionnaireIdentity))
            {
                var contentIds = await this.synchronizationService.GetAttachmentContentsAsync(questionnaire: questionnaireIdentity,
                    onDownloadProgressChanged: (progressPercentage, bytesReceived, totalBytesToReceive) => { },
                    token: cancellationToken).ConfigureAwait(false);

                foreach (var contentId in contentIds)
                {
                    var isExistContent = attachmentContentStorage.Exists(contentId);
                    if (!isExistContent)
                    {
                        var attachmentContent = await this.synchronizationService.GetAttachmentContentAsync(contentId: contentId,
                            onDownloadProgressChanged: (progressPercentage, bytesReceived, totalBytesToReceive) => { },
                            token: cancellationToken).ConfigureAwait(false); 

                        this.attachmentContentStorage.Store(attachmentContent);
                    }
                }

                List<TranslationDto> translationDtos = 
                    await this.synchronizationService.GetQuestionnaireTranslationAsync(questionnaireIdentity, cancellationToken)
                                                              .ConfigureAwait(false);
                
                var questionnaireApiView = await this.synchronizationService.GetQuestionnaireAsync(
                    questionnaire: questionnaireIdentity,
                    onDownloadProgressChanged: (progressPercentage, bytesReceived, totalBytesToReceive) => { },
                    token: cancellationToken).ConfigureAwait(false);

                this.questionnairesAccessor.StoreQuestionnaire(questionnaireIdentity, questionnaireApiView.QuestionnaireDocument, 
                    questionnaireApiView.AllowCensus, translationDtos);

                await this.synchronizationService.LogQuestionnaireAsSuccessfullyHandledAsync(questionnaireIdentity).ConfigureAwait(false);
            }
        }

        private async Task DownloadInterviewsAsync(SychronizationStatistics statistics, IProgress<SyncProgressInfo> progress, CancellationToken cancellationToken)
        {
            var localCensusQuestionnaireIds =
                this.questionnairesAccessor.GetCensusQuestionnaireIdentities()
                    .Select(questionnaireIdentity => questionnaireIdentity.ToString());

            var remoteInterviews = await this.synchronizationService.GetInterviewsAsync(cancellationToken);

            var remoteInterviewIds = remoteInterviews.Select(interview => interview.Id);

            var localInterviews = this.interviewViewRepository.LoadAll();

            var localInterviewIds = localInterviews.Select(interview => interview.InterviewId).ToList();

            var localInterviewsToRemove = localInterviews.Where(interview =>
                !remoteInterviewIds.Contains(interview.InterviewId) &&
                (!interview.CanBeDeleted || (interview.CanBeDeleted && !localCensusQuestionnaireIds.Contains(interview.QuestionnaireId))));

            var localInterviewIdsToRemove = localInterviewsToRemove.Select(interview => interview.InterviewId).ToList();

            var remoteInterviewsToCreate = remoteInterviews.Where(interview => !localInterviewIds.Contains(interview.Id)).ToList();

            this.RemoveInterviews(localInterviewIdsToRemove, statistics, progress);

            await this.CreateInterviewsAsync(remoteInterviewsToCreate, statistics, progress, cancellationToken);
        }

        private void RemoveInterviews(List<Guid> interviewIds, SychronizationStatistics statistics, IProgress<SyncProgressInfo> progress)
        {
            statistics.TotalDeletedInterviewsCount = interviewIds.Count;

            foreach (var interviewId in interviewIds)
            {
                progress.Report(new SyncProgressInfo
                {
                    Title = InterviewerUIResources.Synchronization_Download_Title,
                    Description = string.Format(InterviewerUIResources.Synchronization_Download_Description_Format,
                        statistics.DeletedInterviewsCount + 1,
                        interviewIds.Count,
                        InterviewerUIResources.Synchronization_Interviews)
                });

                this.interviewFactory.RemoveInterview(interviewId);

                statistics.DeletedInterviewsCount++;
            }
        }

        private async Task CreateInterviewsAsync(List<InterviewApiView> interviews, 
            SychronizationStatistics statistics,
            IProgress<SyncProgressInfo> progress, 
            CancellationToken cancellationToken)
        {
            statistics.TotalNewInterviewsCount = interviews.Count(interview => !interview.IsRejected);
            statistics.TotalRejectedInterviewsCount = interviews.Count(interview => interview.IsRejected);

            foreach (var interview in interviews)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress.Report(new SyncProgressInfo
                    {
                        Title = InterviewerUIResources.Synchronization_Download_Title,
                        Description = string.Format(InterviewerUIResources.Synchronization_Download_Description_Format,
                            statistics.RejectedInterviewsCount + statistics.NewInterviewsCount + 1, interviews.Count,
                            InterviewerUIResources.Synchronization_Interviews)
                    });

                    await this.DownloadQuestionnaireAsync(interview.QuestionnaireIdentity, cancellationToken);

                    var interviewDetails = await this.synchronizationService.GetInterviewDetailsAsync(
                        interviewId: interview.Id,
                        onDownloadProgressChanged: (progressPercentage, bytesReceived, totalBytesToReceive) => { },
                        token: cancellationToken);

                    if (interviewDetails == null)
                    {
                        statistics.NewInterviewsCount ++;
                        continue;
                    }

                    await this.interviewFactory.CreateInterviewAsync(interview, interviewDetails);
                    await this.synchronizationService.LogInterviewAsSuccessfullyHandledAsync(interview.Id);

                    if (interview.IsRejected)
                        statistics.RejectedInterviewsCount++;
                    else
                        statistics.NewInterviewsCount++;
                }
                catch (Exception exception)
                {
                    statistics.FailedToCreateInterviewsCount++;
                    this.logger.Error($"Failed to create interview {interview.Id}, interviewer {this.principal.CurrentUserIdentity.Name}", exception);
                }
            }
        }

        private async Task UploadCompletedInterviewsAsync(SychronizationStatistics statistics, IProgress<SyncProgressInfo> progress, CancellationToken cancellationToken)
        {
            var completedInterviews = this.interviewViewRepository.Where(interview => interview.Status == InterviewStatus.Completed);

            statistics.TotalCompletedInterviewsCount = completedInterviews.Count;

            foreach (var completedInterview in completedInterviews)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var interviewPackage = this.interviewFactory.GetInteviewEventsPackageOrNull(completedInterview.InterviewId);

                    progress.Report(new SyncProgressInfo
                    {
                        Title = string.Format(InterviewerUIResources.Synchronization_Upload_Title_Format, InterviewerUIResources.Synchronization_Upload_CompletedAssignments_Text),
                        Description = string.Format(InterviewerUIResources.Synchronization_Upload_Description_Format,
                            statistics.SuccessfullyUploadedInterviewsCount, statistics.TotalCompletedInterviewsCount,
                            InterviewerUIResources.Synchronization_Upload_Interviews_Text),
                        Status = SynchronizationStatus.Upload
                    });

                    await this.UploadImagesByCompletedInterview(completedInterview.InterviewId, progress, cancellationToken);

                    if (interviewPackage != null)
                    {
                        await this.synchronizationService.UploadInterviewAsync(
                            interviewId: completedInterview.InterviewId,
                            completedInterview: interviewPackage,
                            onDownloadProgressChanged: (progressPercentage, bytesReceived, totalBytesToReceive) => { },
                            token: cancellationToken);
                    }
                    else
                    {
                        this.logger.Warn($"Interview event stream is missing. No package was sent to server. Interview ID: {completedInterview.InterviewId}");
                    }

                    this.interviewFactory.RemoveInterview(completedInterview.InterviewId);

                    statistics.SuccessfullyUploadedInterviewsCount++;
                }
                catch (Exception syncException)
                {
                    statistics.FailedToUploadInterviwesCount++;
                    this.logger.Error($"Failed to sync interview {completedInterview.Id}. Interviewer login {this.principal.CurrentUserIdentity.Name}", syncException);
                }
            }
        }

        private async Task UploadImagesByCompletedInterview(Guid interviewId, IProgress<SyncProgressInfo> progress, CancellationToken cancellationToken)
        {
            var imageViews = this.interviewMultimediaViewStorage.Where(image => image.InterviewId == interviewId);

            foreach (var imageView in imageViews)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileView = this.interviewFileViewStorage.GetById(imageView.FileId);
                await this.synchronizationService.UploadInterviewImageAsync(
                    interviewId: imageView.InterviewId,
                    fileName: imageView.FileName,
                    fileData: fileView.File,
                    onDownloadProgressChanged: (progressPercentage, bytesReceived, totalBytesToReceive) => { },
                    token: cancellationToken);
                this.interviewMultimediaViewStorage.Remove(imageView.Id);
                this.interviewFileViewStorage.Remove(fileView.Id);
            }
        }

        private void UpdatePasswordOfInterviewer(string password)
        {
            var localInterviewer = this.interviewersPlainStorage.FirstOrDefault();
            localInterviewer.Password = password;

            this.interviewersPlainStorage.Store(localInterviewer);
            this.principal.SignIn(localInterviewer.Name, localInterviewer.Password, true);
        }

        private DeviceInfoApiView ToDeviceInfoApiView(DeviceInfo info) => new DeviceInfoApiView
        {
            DeviceId = info.DeviceId,
            DeviceModel = info.DeviceModel,
            DeviceType = info.DeviceType,
            DeviceDate = info.DeviceDate,
            DeviceLanguage = info.DeviceLanguage,
            DeviceLocation = info.DeviceLocation != null ? ToLocationAddressApiView(info.DeviceLocation) : null,

            AndroidVersion = info.AndroidVersion,
            AndroidSdkVersion = info.AndroidSdkVersion,

            AppVersion = info.AppVersion,
            AppOrientation = info.AppOrientation,
            LastAppUpdatedDate = info.LastAppUpdatedDate,

            BatteryChargePercent = info.BatteryChargePercent,
            BatteryPowerSource = info.BatteryPowerSource,
            
            MobileOperator = info.MobileOperator,
            MobileSignalStrength = info.MobileSignalStrength,
            NetworkType = info.NetworkType,
            NetworkSubType = info.NetworkSubType,

            NumberOfStartedInterviews = this.interviewViewRepository.Count(interview=>interview.StartedDateTime != null && interview.CompletedDateTime == null),
            
            DBSizeInfo = info.DBSizeInfo,
            StorageInfo = ToStorageInfoApiView(info.StorageInfo),
            RAMInfo = ToRAMInfoApiView(info.RAMInfo)
        };

        private LocationAddressApiView ToLocationAddressApiView(LocationAddress locationAddress)
            => new LocationAddressApiView
            {
                Longitude = locationAddress.Longitude,
                Latitude = locationAddress.Latitude
            };

        private RAMInfoApiView ToRAMInfoApiView(RAMInfo ramInfo) => new RAMInfoApiView
        {
            Free = ramInfo.Free,
            Total = ramInfo.Total
        };

        private StorageInfoApiView ToStorageInfoApiView(StorageInfo storageInfo) => new StorageInfoApiView
        {
            Free = storageInfo.Free,
            Total = storageInfo.Total
        };
    }
}