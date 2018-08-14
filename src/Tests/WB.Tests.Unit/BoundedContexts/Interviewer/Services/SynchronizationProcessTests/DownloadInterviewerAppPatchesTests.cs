﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Plugin.Permissions.Abstractions;
using WB.Core.BoundedContexts.Supervisor.Services;
using WB.Core.BoundedContexts.Supervisor.Synchronization;
using WB.Core.GenericSubdomains.Portable.Implementation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.Core.SharedKernels.Enumerator.Implementation.Services.Synchronization.Steps;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Synchronization;
using WB.Tests.Abc;

namespace WB.Tests.Unit.BoundedContexts.Interviewer.Services.SynchronizationProcessTests
{
    [TestOf(typeof(DownloadInterviewerAppPatches))]
    internal class DownloadInterviewerAppPatchesTests
    {
        [Test]
        public async Task when_ExecuteAsync_should_ask_storage_permissions()
        {
            // arrange
            var mockOfPermissionsService = new Mock<IPermissionsService>();
            var step = CreateDownloadInterviewerAppPatches(permissions: mockOfPermissionsService.Object);
            // act
            await step.ExecuteAsync();
            // assert
            mockOfPermissionsService.Verify(x=>x.AssureHasPermission(Permission.Storage), Times.Once);
        }

        [Test]
        public async Task when_ExecuteAsync_and_interviewer_app_delta_exists_then_should_not_download_delta_from_server_again()
        {
            // arrange
            var notExistingFile = "notExistingFile.delta";
            var existingFile = "existingFile.delta";
            var mockOfSupervisorSynchronization = new Mock<ISupervisorSynchronizationService>();
            mockOfSupervisorSynchronization
                .Setup(x => x.GetListOfInterviewerAppPatchesAsync(It.IsAny<CancellationToken>())).Returns(
                    Task.FromResult(new[]
                    {
                        new InterviewerApplicationPatchApiView {FileName = notExistingFile, Url = notExistingFile},
                        new InterviewerApplicationPatchApiView {FileName = existingFile, Url = existingFile},
                    }));
            var fileSystemAccessor = Mock.Of<IFileSystemAccessor>(x =>
                x.CombinePath(It.IsAny<string>(), notExistingFile) == notExistingFile &&
                x.CombinePath(It.IsAny<string>(), existingFile) == existingFile &&
                x.IsFileExists(notExistingFile) == false &&
                x.IsFileExists(existingFile) == true &&
                x.GetFileSize(existingFile) == 1);

            var step = CreateDownloadInterviewerAppPatches(
                synchronizationService: mockOfSupervisorSynchronization.Object,
                fileSystemAccessor: fileSystemAccessor);
            // act
            await step.ExecuteAsync();
            // assert
            mockOfSupervisorSynchronization.Verify(x => x.GetFileAsync(notExistingFile, It.IsAny<IProgress<TransferProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
            mockOfSupervisorSynchronization.Verify(x => x.GetFileAsync(existingFile, It.IsAny<IProgress<TransferProgress>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task when_ExecuteAsync_and_interviewer_app_delta_exists_with_zero_length_then_should_download_delta_from_server_again()
        {
            // arrange
            var zeroFileName = "existingFile.delta";
            var mockOfSupervisorSynchronization = new Mock<ISupervisorSynchronizationService>();
            mockOfSupervisorSynchronization
                .Setup(x => x.GetListOfInterviewerAppPatchesAsync(It.IsAny<CancellationToken>())).Returns(
                    Task.FromResult(new[]
                    {
                        new InterviewerApplicationPatchApiView {FileName = zeroFileName, Url = zeroFileName},
                    }));
            var fileSystemAccessor = Mock.Of<IFileSystemAccessor>(x =>
                x.CombinePath(It.IsAny<string>(), zeroFileName) == zeroFileName &&
                x.IsFileExists(zeroFileName) == true &&
                x.GetFileSize(zeroFileName) == 0);

            var step = CreateDownloadInterviewerAppPatches(
                synchronizationService: mockOfSupervisorSynchronization.Object,
                fileSystemAccessor: fileSystemAccessor);
            // act
            await step.ExecuteAsync();
            // assert
            mockOfSupervisorSynchronization.Verify(x => x.GetFileAsync(zeroFileName, It.IsAny<IProgress<TransferProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task when_ExecuteAsync_should_get_latest_interviewer_app_version_from_server_and_save_it_to_local_settings()
        {
            // arrange
            int? version = 12345;

            var mockOfSupervisorSynchronization = new Mock<ISupervisorSynchronizationService>();
            mockOfSupervisorSynchronization.Setup(x => x.GetLatestInterviewerAppVersionAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(version));

            var mockOfSupervisorSettings = new Mock<ISupervisorSettings>();

            var step = CreateDownloadInterviewerAppPatches(
                synchronizationService: mockOfSupervisorSynchronization.Object,
                supervisorSettings: mockOfSupervisorSettings.Object);

            // act
            await step.ExecuteAsync();
            // assert
            mockOfSupervisorSynchronization.Verify(x => x.GetLatestInterviewerAppVersionAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockOfSupervisorSettings.Verify(x => x.SetLatestInterviewerAppVersion(version), Times.Once);
        }

        private static DownloadInterviewerAppPatches CreateDownloadInterviewerAppPatches(
            ISupervisorSynchronizationService synchronizationService = null,
            ILogger logger = null,
            IFileSystemAccessor fileSystemAccessor = null,
            IPermissionsService permissions = null,
            ISupervisorSettings supervisorSettings = null)
        {
            return new DownloadInterviewerAppPatches(0,
                logger: logger ?? Mock.Of<ILogger>(),
                synchronizationService: synchronizationService ?? Mock.Of<ISupervisorSynchronizationService>(),
                fileSystemAccessor: fileSystemAccessor ?? Mock.Of<IFileSystemAccessor>(),
                permissions: permissions ?? Mock.Of<IPermissionsService>(), 
                supervisorSettings: supervisorSettings ?? Mock.Of<ISupervisorSettings>())
            {
                Context = Create.Entity.EnumeratorSynchonizationContext()
            };
        }
    }
}
