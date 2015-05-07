﻿using Machine.Specifications;
using Moq;
using WB.Core.SharedKernels.SurveyManagement.Implementation.Services.HealthCheck;
using WB.Core.SharedKernels.SurveyManagement.Implementation.Services.HealthCheck.Checks;
using WB.Core.SharedKernels.SurveyManagement.Synchronization;
using WB.Core.SharedKernels.SurveyManagement.ValueObjects.HealthCheck;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.HealthCheckTests
{
    internal class when_healthcheck_service_check_when_there_are_any_errors : HealthCheckTestContext
    {
        private Establish context = () =>
        {
            var eventStoreHealthCheck = Mock.Of<IAtomicHealthCheck<EventStoreHealthCheckResult>>(m => m.Check() == EventStoreHealthCheckResult.Down(eventStoreErrorMessage));
            var brokenSyncPackagesStorage = Mock.Of<IAtomicHealthCheck<NumberOfUnhandledPackagesHealthCheckResult>>(m => m.Check() == NumberOfUnhandledPackagesHealthCheckResult.Warning(numberOfunhandledPackages,numberOfUnhandledPackagesErrorMessage));
            var chunkReader = Mock.Of<IAtomicHealthCheck<NumberOfSyncPackagesWithBigSizeCheckResult>>(m => m.Check() == NumberOfSyncPackagesWithBigSizeCheckResult.Warning(numberOfSyncPackagesWithBigSize, numberOfSyncPackagesWithBigSizeErrorMessage));
            var folderPermissionChecker = Mock.Of<IAtomicHealthCheck<FolderPermissionCheckResult>>(m => m.Check() == new FolderPermissionCheckResult(HealthCheckStatus.Down, currentUserName, allowedFoldersList, denidedFoldersList));

            service = CreateHealthCheckService(
                eventStoreHealthCheck,
                brokenSyncPackagesStorage,
                chunkReader,
                folderPermissionChecker);
        };

        Because of = () =>
        {
            result = service.Check();
        };

        It should_return_HealthCheckStatus = () =>
            result.ShouldBeOfExactType<HealthCheckResults>();

        It should_return_Down_status = () =>
            result.Status.ShouldEqual(HealthCheckStatus.Down);

        It should_return_Down_status_for_EventStore_check = () =>
            result.EventstoreConnectionStatus.Status.ShouldEqual(HealthCheckStatus.Down);

        It should_return_error_message_for_EventStore_check = () =>
            result.EventstoreConnectionStatus.ErrorMessage.ShouldEqual(eventStoreErrorMessage);

        It should_return_Warning_status_for_NumberOfUnhandledPackages_check = () =>
            result.NumberOfUnhandledPackages.Status.ShouldEqual(HealthCheckStatus.Warning);

        It should_return_error_message_for_NumberOfUnhandledPackages_check = () =>
            result.NumberOfUnhandledPackages.ErrorMessage.ShouldEqual(numberOfUnhandledPackagesErrorMessage);

        It should_return_4_packages_for_NumberOfUnhandledPackages_check = () =>
            result.NumberOfUnhandledPackages.Value.ShouldEqual(numberOfunhandledPackages);

      It should_return_Warning_status_for_NumberOfSyncPackagesWithBigSize_check = () =>
            result.NumberOfSyncPackagesWithBigSize.Status.ShouldEqual(HealthCheckStatus.Warning);

        It should_return_error_message_for_NumberOfSyncPackagesWithBigSize_check = () =>
            result.NumberOfSyncPackagesWithBigSize.ErrorMessage.ShouldEqual(numberOfSyncPackagesWithBigSizeErrorMessage);

        It should_return_Down_status_for_FolderPermissionCheckResult_check = () =>
            result.FolderPermissionCheckResult.Status.ShouldEqual(HealthCheckStatus.Down);

        It should_return_user_name_for_FolderPermissionCheckResult_check = () =>
            result.FolderPermissionCheckResult.ProcessRunedUnder.ShouldEqual(currentUserName);

        It should_return_allowed_folders_for_FolderPermissionCheckResult_check = () =>
            result.FolderPermissionCheckResult.AllowedFolders.ShouldEqual(allowedFoldersList);

        It should_return_denided_folders_for_FolderPermissionCheckResult_check = () =>
            result.FolderPermissionCheckResult.DeniedFolders.ShouldEqual(denidedFoldersList);


        private static string   eventStoreErrorMessage = "eventStore error message";
        private static int      numberOfSyncPackagesWithBigSize = 5;
        private static int numberOfunhandledPackages = 3;
        private static string numberOfUnhandledPackagesErrorMessage = "numberOfUnhandledPackagesErrorMessage error message";
        private static string numberOfSyncPackagesWithBigSizeErrorMessage = "numberOfUnhandledPackagesErrorMessage error message";
        private static string[] unhandledPackagesList = new[] { "package name" };
        private static string   currentUserName = "user name";
        private static string[] allowedFoldersList = new[] { "allow folder" };
        private static string[] denidedFoldersList = new[] { "deny folder" };

        private static HealthCheckResults result;
        private static HealthCheckService service;
        
    }
}
