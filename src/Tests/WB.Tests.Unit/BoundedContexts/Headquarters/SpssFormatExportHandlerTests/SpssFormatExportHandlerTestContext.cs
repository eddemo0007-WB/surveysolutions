﻿using Machine.Specifications;
using Moq;
using WB.Core.BoundedContexts.Headquarters.DataExport.Accessors;
using WB.Core.BoundedContexts.Headquarters.DataExport.ExportProcessHandlers;
using WB.Core.BoundedContexts.Headquarters.DataExport.Services;
using WB.Core.BoundedContexts.Headquarters.Services.Export;
using WB.Core.BoundedContexts.Headquarters.Views.InterviewHistory;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.FileSystem;

namespace WB.Tests.Unit.BoundedContexts.Headquarters.SpssFormatExportHandlerTests
{
    [Subject(typeof(SpssFormatExportHandler))]
    internal class SpssFormatExportHandlerTestContext
    {
        protected static SpssFormatExportHandler CreateSpssFormatExportHandler(
            IFileSystemAccessor fileSystemAccessor = null,
            IArchiveUtils archiveUtils = null,
            ITabularFormatExportService tabularFormatExportService = null,
            IFilebasedExportedDataAccessor filebasedExportedDataAccessor = null,
            ITabularDataToExternalStatPackageExportService tabularDataToExternalStatPackageExportService = null)
        {
            return new SpssFormatExportHandler(
                fileSystemAccessor ?? Mock.Of<IFileSystemAccessor>(_=>_.GetFilesInDirectory(Moq.It.IsAny<string>(), Moq.It.IsAny<bool>()) ==new[] {"test.tab"}),
                archiveUtils ?? Mock.Of<IArchiveUtils>(),
                new InterviewDataExportSettings(),
                tabularFormatExportService ?? Mock.Of<ITabularFormatExportService>(),
                filebasedExportedDataAccessor ?? Mock.Of<IFilebasedExportedDataAccessor>(),
                tabularDataToExternalStatPackageExportService ??
                Mock.Of<ITabularDataToExternalStatPackageExportService>(),
                Mock.Of<IDataExportProcessesService>(),
                Mock.Of<ILogger>());
        }
    }
}