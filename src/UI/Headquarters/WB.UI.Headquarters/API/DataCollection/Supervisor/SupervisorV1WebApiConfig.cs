﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Web.Http;
using WB.Core.SharedKernel.Structures.Synchronization.SurveyManagement;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.Core.SharedKernels.SurveyManagement.Web.Filters;
using WB.UI.Headquarters.API.DataCollection.Enumerator.v1;
using WB.UI.Headquarters.API.DataCollection.Supervisor.v1;

namespace WB.UI.Headquarters.API.DataCollection.Supervisor
{
    [Localizable(false)]
    public class SupervisorV1WebApiConfig
    {
#pragma warning disable 4014

        public static void Register(HttpConfiguration config)
        {
            config.TypedRoute(@"api/supervisor/compatibility/{deviceid}/{deviceSyncProtocolVersion}",
                c => c.Action<SupervisorApiController>(x => x.CheckCompatibility(Param.Any<string>(), Param.Any<int>(), Param.Any<string>())));

            config.TypedRoute(@"api/supervisor/v1/devices/info", c => c.Action<DevicesApiV1Controller>(x => x.Info(Param.Any<DeviceInfoApiView>())));
            config.TypedRoute(@"api/supervisor/v1/devices/statistics", c => c.Action<DevicesApiV1Controller>(x => x.Statistics(Param.Any<SyncStatisticsApiView>())));
            config.TypedRoute(@"api/supervisor/v1/devices/exception", c => c.Action<DevicesApiV1Controller>(x => x.UnexpectedException(Param.Any<UnexpectedExceptionApiView>())));

            config.TypedRoute(@"api/supervisor/v1/extended", c => c.Action<SupervisorApiController>(x => x.GetSupervisor()));
            config.TypedRoute(@"api/supervisor/v1/apk/interviewer", c => c.Action<SupervisorApiController>(x => x.GetInterviewer()));
            config.TypedRoute(@"api/supervisor/v1/apk/interviewer-with-maps", c => c.Action<SupervisorApiController>(x => x.GetInterviewerWithMaps()));
            config.TypedRoute(@"api/supervisor/v1/extended/patch/{deviceVersion}", c => c.Action<SupervisorApiController>(x => x.Patch(Param.Any<int>())));
            config.TypedRoute("api/supervisor/v1/extended/latestversion", c => c.Action<SupervisorApiController>(x => x.GetLatestVersion()));

            config.TypedRoute("api/supervisor/v1/tabletInfo", c => c.Action<SupervisorApiController>(x => x.PostTabletInformation()));
            config.TypedRoute("api/supervisor/v1/devices/current/{id}/{version}",
                c => c.Action<DevicesApiV1Controller>(x => x.CanSynchronize(Param.Any<string>(), Param.Any<int>())));
            config.TypedRoute("api/supervisor/v1/devices/link/{id}/{version:int}",
                c => c.Action<DevicesApiV1Controller>(x => x.LinkCurrentResponsibleToDevice(Param.Any<string>(), Param.Any<int>())));
            config.TypedRoute("api/supervisor/v1/users/login", c => c.Action<UserApiController>(x => x.Login(Param.Any<LogonInfo>())));
            config.TypedRoute("api/supervisor/v1/users/current", c => c.Action<UserApiController>(x => x.Current()));
            config.TypedRoute("api/supervisor/v1/users/hasdevice", c => c.Action<UserApiController>(x => x.HasDevice()));

            config.TypedRoute("api/supervisor/compatibility/{deviceid}/{deviceSyncProtocolVersion}",
                c => c.Action<SupervisorApiController>(x => x.CheckCompatibility(Param.Any<string>(), Param.Any<int>(), Param.Any<string>())));
            config.TypedRoute("api/supervisor/v1/translations/{id}", c => c.Action<TranslationsApiV1Controller>(x => x.Get(Param.Any<string>())));
            config.TypedRoute("api/supervisor/v1/companyLogo", c => c.Action<SettingsV1Controller>(x => x.CompanyLogo()));
            config.TypedRoute("api/supervisor/v1/tenantId", c => c.Action<SettingsV1Controller>(x => x.TenantId()));
            config.TypedRoute("api/supervisor/v1/autoupdate", c => c.Action<SettingsV1Controller>(x => x.AutoUpdateEnabled()));
            config.TypedRoute("api/supervisor/v1/notifications", c => c.Action<SettingsV1Controller>(x => x.NotificationsEnabled()));
            config.TypedRoute("api/supervisor/v1/encryption-key", c => c.Action<SettingsV1Controller>(x => x.PublicKeyForEncryption()));

            config.TypedRoute("api/supervisor/v1/questionnaires/list",
                c => c.Action<QuestionnairesApiV1Controller>(x => x.List()));
            config.TypedRoute("api/supervisor/v1/questionnaires/{id:guid}/{version:int}/{contentVersion:long}",
                c => c.Action<QuestionnairesApiV1Controller>(x => x.Get(Param.Any<Guid>(), Param.Any<int>(), Param.Any<long>())));
            config.TypedRoute("api/supervisor/v1/questionnaires/{id:guid}/{version:int}/assembly",
                c => c.Action<QuestionnairesApiV1Controller>(x => x.GetAssembly(Param.Any<Guid>(), Param.Any<int>())));
            config.TypedRoute("api/supervisor/v1/questionnaires/{id:guid}/{version:int}/logstate",
                c =>
                    c.Action<QuestionnairesApiV1Controller>(
                        x => x.LogQuestionnaireAsSuccessfullyHandled(Param.Any<Guid>(), Param.Any<int>())));
            config.TypedRoute("api/supervisor/v1/questionnaires/{id:guid}/{version:int}/assembly/logstate",
                c =>
                    c.Action<QuestionnairesApiV1Controller>(
                        x => x.LogQuestionnaireAssemblyAsSuccessfullyHandled(Param.Any<Guid>(), Param.Any<int>())));
            config.TypedRoute("api/supervisor/v1/questionnaires/{id:guid}/{version:int}/attachments",
                c => c.Action<QuestionnairesApiV1Controller>(x => x.GetAttachments(Param.Any<Guid>(), Param.Any<int>())));
            
            config.TypedRoute("api/supervisor/v1/attachments/{id}",
                c => c.Action<AttachmentsApiV1Controller>(x => x.GetAttachmentContent(Param.Any<string>())));
            config.TypedRoute("api/supervisor/v1/assignments",
                c => c.Action<AssignmentsApiV1Controller>(x => x.GetAssignmentsAsync(Param.Any<CancellationToken>())));
            config.TypedRoute("api/supervisor/v1/assignments/{id}",
                c => c.Action<AssignmentsApiV1Controller>(x => x.GetAssignmentAsync(Param.Any<int>(), Param.Any<CancellationToken>())));
            config.TypedRoute("api/supervisor/v1/assignments/{id}/Received",
                c => c.Action<AssignmentsApiV1Controller>(x => x.Received(Param.Any<int>())));
            config.TypedRoute("api/supervisor/v1/maps", c => c.Action<MapsApiV1Controller>(x => x.GetMaps()));
            config.TypedRoute("api/supervisor/v1/maps/{id}",
                c => c.Action<MapsApiV1Controller>(x => x.GetMapContent((Param.Any<string>()))));
            config.TypedRoute("api/supervisor/v1/auditlog",
                c => c.Action<AuditLogApiV1Controller>(x => x.Post(Param.Any<AuditLogEntitiesApiView>())));

            config.TypedRoute("api/supervisor/v1/brokenInterviews",
                c => c.Action<BrokenInterviewPackageApiV1Controller>(x => x.Post(Param.Any<BrokenInterviewPackageApiView>())));

            config.TypedRoute("api/supervisor/v1/interviewerExceptions",
                c => c.Action<InterviewerExceptionsApiV1Controller>(x => x.Post(Param.Any<List<InterviewerExceptionInfo>>())));

            config.TypedRoute("api/supervisor/v1/interviewerStatistics",
                c => c.Action<InterviewerStatisticsApiV1Controller>(x => x.Post(Param.Any<InterviewerSyncStatisticsApiView>())));

            config.TypedRoute("api/supervisor/v1/deletedQuestionnairesList",
                c => c.Action<QuestionnairesApiV1Controller>(x => x.GetDeletedQuestionnaireList()));

            config.TypedRoute("api/supervisor/v1/interviewerTabletInfos",
                c => c.Action<InterviewerDeviceInfoApiV1Controller>(x => x.Post(Param.Any<DeviceInfoApiView>())));

            config.TypedRoute("api/supervisor/v1/interviews", c => c.Action<InterviewsApiV1Controller>(x => x.Get()));

            config.TypedRoute("api/supervisor/v1/interviews/{id:guid}",
                c => c.Action<InterviewsApiV1Controller>(x => x.Details(Param.Any<Guid>())));
            config.TypedRoute("api/supervisor/v1/interviews/{id:guid}/logstate",
                c => c.Action<InterviewsApiV1Controller>(x => x.LogInterviewAsSuccessfullyHandled(Param.Any<Guid>())));
            config.TypedRoute("api/supervisor/v1/interviews/{id:guid}",
                c => c.Action<InterviewsApiV1Controller>(x => x.Post(Param.Any<InterviewPackageApiView>())));
            config.TypedRoute("api/supervisor/v1/interviews/{id:guid}/image",
                c => c.Action<InterviewsApiV1Controller>(x => x.PostImage(Param.Any<PostFileRequest>())));
            config.TypedRoute("api/supervisor/v1/interviews/{id:guid}/audio",
                c => c.Action<InterviewsApiV1Controller>(x => x.PostAudio(Param.Any<PostFileRequest>())));
            config.TypedRoute("api/supervisor/v1/interviews/{id:guid}/audioaudit",
                c => c.Action<InterviewsApiV1Controller>(x => x.PostAudioAudit(Param.Any<PostFileRequest>())));
            
            config.TypedRoute("api/supervisor/v1/interviews/{id:guid}/getInterviewUploadState",
                c => c.Action<InterviewsApiV1Controller>(x => x.GetInterviewUploadState(Param.Any<Guid>(), Param.Any<EventStreamSignatureTag>())));

            // INTERVIEWERS
            config.TypedRoute("api/supervisor/v1/interviewers", c => c.Action<InterviewersApiController>(x => x.Get()));
        }

#pragma warning restore 4014

    }

}