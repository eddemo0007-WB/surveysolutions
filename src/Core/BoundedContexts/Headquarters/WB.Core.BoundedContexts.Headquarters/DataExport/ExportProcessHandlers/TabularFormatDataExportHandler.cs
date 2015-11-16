using System;
using System.Collections.Generic;
using Ncqrs.Eventing;
using Ncqrs.Eventing.Storage;
using WB.Core.BoundedContexts.Headquarters.DataExport.Accessors;
using WB.Core.BoundedContexts.Headquarters.DataExport.Dtos;
using WB.Core.BoundedContexts.Headquarters.DataExport.Services;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Tasks;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.Infrastructure.Transactions;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Views;
using WB.Core.SharedKernels.SurveyManagement.EventHandler;
using WB.Core.SharedKernels.SurveyManagement.Services.Export;
using WB.Core.SharedKernels.SurveyManagement.Views.DataExport;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;
using WB.Core.SharedKernels.SurveyManagement.Views.InterviewHistory;
using IFilebasedExportedDataAccessor = WB.Core.BoundedContexts.Headquarters.DataExport.Accessors.IFilebasedExportedDataAccessor;

namespace WB.Core.BoundedContexts.Headquarters.DataExport.ExportProcessHandlers
{
    internal class TabularFormatDataExportHandler : AbstractDataExportHandler
    {
        private readonly IReadSideKeyValueStorage<QuestionnaireExportStructure> questionnaireReader;
        private readonly ITransactionManagerProvider transactionManagerProvider;
        private readonly ITabularFormatExportService tabularFormatExportService;
        private readonly IEnvironmentContentService environmentContentService;

        public TabularFormatDataExportHandler(
            IFileSystemAccessor fileSystemAccessor, 
            IArchiveUtils archiveUtils, 
            InterviewDataExportSettings interviewDataExportSettings, 
            IReadSideKeyValueStorage<QuestionnaireExportStructure> questionnaireReader, 
            ITransactionManagerProvider transactionManagerProvider, 
            ITabularFormatExportService tabularFormatExportService,
            IEnvironmentContentService environmentContentService, 
            IFilebasedExportedDataAccessor filebasedExportedDataAccessor) : base(fileSystemAccessor, archiveUtils, filebasedExportedDataAccessor, interviewDataExportSettings)
        {
            this.questionnaireReader = questionnaireReader;
            this.transactionManagerProvider = transactionManagerProvider;
            this.tabularFormatExportService = tabularFormatExportService;
            this.environmentContentService = environmentContentService;
        }

        protected override DataExportFormat Format => DataExportFormat.Tabular;

        protected override void ExportAllDataIntoDirectory(QuestionnaireIdentity questionnaireIdentity, string directoryPath, IProgress<int> progress)
        {
            this.tabularFormatExportService.ExportInterviewsInTabularFormat(questionnaireIdentity,
                directoryPath, progress);

            CreateDoFilesForQuestionnaire(questionnaireIdentity, directoryPath);
        }

        protected override void ExportApprovedDataIntoDirectory(QuestionnaireIdentity questionnaireIdentity, string directoryPath, IProgress<int> progress)
        {
            this.tabularFormatExportService.ExportApprovedInterviewsInTabularFormat(questionnaireIdentity,
                directoryPath, progress);

            CreateDoFilesForQuestionnaire(questionnaireIdentity, directoryPath);
        }

        private void CreateDoFilesForQuestionnaire(QuestionnaireIdentity questionnaireIdentity, string directoryPath)
        {
            this.transactionManagerProvider.GetTransactionManager().ExecuteInQueryTransaction(() =>
                this.environmentContentService.CreateEnvironmentFiles(
                    this.questionnaireReader.AsVersioned()
                        .Get(questionnaireIdentity.QuestionnaireId.FormatGuid(), questionnaireIdentity.Version),
                    directoryPath));
        }
    }
}