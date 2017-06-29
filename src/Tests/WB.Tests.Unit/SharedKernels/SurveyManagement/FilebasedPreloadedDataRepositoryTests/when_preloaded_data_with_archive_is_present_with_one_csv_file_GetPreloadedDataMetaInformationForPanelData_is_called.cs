﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Moq;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Parser;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Implementation.Repositories;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.FileSystem;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.FilebasedPreloadedDataRepositoryTests
{
    internal class when_preloaded_data_with_archive_is_present_with_one_csv_file_GetPreloadedDataMetaInformationForPanelData_is_called : FilebasedPreloadedDataRepositoryTestContext
    {
        private Establish context = () =>
        {
            fileSystemAccessor = CreateIFileSystemAccessorMock();
            fileSystemAccessor.Setup(x => x.IsDirectoryExists("PreLoadedData\\" + archiveId)).Returns(true);
            fileSystemAccessor.Setup(x => x.GetFileExtension(tabFileName)).Returns(".tab");
            fileSystemAccessor.Setup(x => x.GetFileExtension(fileNameWithoutExtension)).Returns("");
            fileSystemAccessor.Setup(x => x.GetFilesInDirectory(preLoadedData + "\\" + archiveId, Moq.It.IsAny<bool>())).Returns(new string[] { archiveName + ".zip" });
            fileSystemAccessor.Setup(x => x.GetFilesInDirectory(archiveName, Moq.It.IsAny<bool>()))
                .Returns(new string[0]);

            archiveUtils = new Mock<IArchiveUtils>();
            archiveUtils.Setup(x => x.IsZipFile(Moq.It.IsAny<string>())).Returns(true);
            archiveUtils.Setup(x => x.GetArchivedFileNamesAndSize(Moq.It.IsAny<string>()))
                .Returns(new Dictionary<string, long>() { { tabFileName, 20 },{fileNameWithoutExtension,1} });
            recordsAccessorFactory = new Mock<IRecordsAccessorFactory>();
            filebasedPreloadedDataRepository = CreateFilebasedPreloadedDataRepository(fileSystemAccessor.Object, archiveUtils.Object, recordsAccessorFactory.Object);
        };

        Because of = () => result = filebasedPreloadedDataRepository.GetPreloadedDataMetaInformationForPanelData(archiveId);

        It should_result_has_info_about_2_elements = () =>
            result.FilesMetaInformation.Length.ShouldEqual(2);

        It should_result_has_info_about_first_element_with_name_1_tab = () =>
          result.FilesMetaInformation[0].FileName.ShouldEqual(tabFileName);

        It should_first_element_be_marked_and_CanBeHandled = () =>
         result.FilesMetaInformation[0].CanBeHandled.ShouldEqual(true);

        It should_result_has_info_about_second_element_with_name_nastya = () =>
         result.FilesMetaInformation[1].FileName.ShouldEqual(fileNameWithoutExtension);

        It should_second_element_be_marked_and_CanBeHandled = () =>
        result.FilesMetaInformation[1].CanBeHandled.ShouldEqual(false);

        private static Mock<IFileSystemAccessor> fileSystemAccessor;
        private static FilebasedPreloadedDataRepository filebasedPreloadedDataRepository;
        private static PreloadedContentMetaData result;
        private static Mock<IArchiveUtils> archiveUtils;
        private static Mock<IRecordsAccessorFactory> recordsAccessorFactory;
        private static string archiveName = "test";
        private static string preLoadedData = "PreLoadedData";
        private static string archiveId = Guid.NewGuid().FormatGuid();
        private static string tabFileName = "1.tab";
        private static string fileNameWithoutExtension = "nastya";
    }
}
