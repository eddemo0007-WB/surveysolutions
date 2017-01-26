﻿using WB.Core.BoundedContexts.Headquarters.DataExport.Dtos;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.Transactions;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;

namespace WB.Core.BoundedContexts.Headquarters.Implementation.Services
{
    internal class ExportExportFileNameService : IExportFileNameService
    {
        private readonly IPlainStorageAccessor<QuestionnaireBrowseItem> questionnaires;
        private readonly IPlainTransactionManagerProvider plainTransactionManagerProvider;
        private readonly IFileSystemAccessor fileSystemAccessor;

        private IPlainTransactionManager transactionManager => this.plainTransactionManagerProvider.GetPlainTransactionManager();


        public ExportExportFileNameService(
            IPlainTransactionManagerProvider plainTransactionManagerProvider, 
            IPlainStorageAccessor<QuestionnaireBrowseItem> questionnaires, 
            IFileSystemAccessor fileSystemAccessor)
        {
            this.plainTransactionManagerProvider = plainTransactionManagerProvider;
            this.questionnaires = questionnaires;
            this.fileSystemAccessor = fileSystemAccessor;
        }

        public string GetFileNameForBatchUploadByQuestionnaire(QuestionnaireIdentity identity)
        {
            var questionnaireTitle = GetQuestionnaireTitle(identity);
            return $"template_{questionnaireTitle}_v{identity.Version}.zip";
        }

        public string GetFolderNameForParaDataByQuestionnaire(QuestionnaireIdentity identity, string pathToHistoryFiles)
        {
            var questionnaireTitle = GetQuestionnaireTitle(identity);
            return this.fileSystemAccessor.CombinePath(pathToHistoryFiles, $"{questionnaireTitle}_{identity.Version}");
        }

        public string GetFileNameForDdiByQuestionnaire(QuestionnaireIdentity identity, string pathToDdiMetadata)
        {
            var questionnaireTitle = GetQuestionnaireTitle(identity);
            return this.fileSystemAccessor.CombinePath(pathToDdiMetadata, $"{questionnaireTitle}_{identity.Version}_ddi.zip");
        }

        public string GetFileNameForTabByQuestionnaire(QuestionnaireIdentity identity, string pathToExportedData, DataExportFormat format, string statusSuffix)
        {
            var questionnaireTitle = GetQuestionnaireTitle(identity);
            var archiveName = $"{questionnaireTitle}_{identity.Version}_{format}_{statusSuffix}.zip";
            return this.fileSystemAccessor.CombinePath(pathToExportedData, archiveName);
        }

        private string GetQuestionnaireTitle(QuestionnaireIdentity identity)
        {
            var questionnaireTitle = this.transactionManager.ExecuteInQueryTransaction(() => this.questionnaires.GetById(identity.ToString())?.Title);

            questionnaireTitle = this.fileSystemAccessor.MakeValidFileName(questionnaireTitle);
            
            questionnaireTitle = string.IsNullOrWhiteSpace(questionnaireTitle) 
                ? identity.QuestionnaireId.FormatGuid() 
                : questionnaireTitle;

            return questionnaireTitle;
        }
    }
}
