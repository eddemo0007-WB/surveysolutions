﻿using System.IO;
using Machine.Specifications;
using Moq;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.SurveyManagement.Implementation.Synchronization;
using WB.Core.Synchronization;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.IncomingPackagesQueueTests
{
    [Subject(typeof(IncomingSyncPackagesQueue))]
    internal class IncomingPackagesQueueTestContext
    {
        protected static IncomingSyncPackagesQueue CreateIncomingPackagesQueue(IFileSystemAccessor fileSystemAccessor = null)
        {
            return new IncomingSyncPackagesQueue(fileSystemAccessor ?? Mock.Of<IFileSystemAccessor>(), new SyncSettings(AppDataDirectory, IncomingCapiPackagesWithErrorsDirectoryName, IncomingCapiPackageFileNameExtension, IncomingCapiPackagesDirectoryName, ""));
        }

        protected static Mock<IFileSystemAccessor> CreateDefaultFileSystemAccessorMock()
        {
            var fileSystemAccessorMock = new Mock<IFileSystemAccessor>();
            fileSystemAccessorMock.Setup(x => x.CombinePath(Moq.It.IsAny<string>(), Moq.It.IsAny<string>()))
                .Returns<string, string>(Path.Combine);
            return fileSystemAccessorMock;
        }

        const string AppDataDirectory = "App_Data";
        const string IncomingCapiPackagesDirectoryName = "IncomingData";
        const string IncomingCapiPackagesWithErrorsDirectoryName = "IncomingDataWithErrors";
        const string IncomingCapiPackageFileNameExtension = "sync";
    }
}
