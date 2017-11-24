﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WB.Core.BoundedContexts.Headquarters.DataExport.Factories;
using WB.Core.BoundedContexts.Headquarters.DataExport.Services.Exporters;
using WB.Core.BoundedContexts.Headquarters.Repositories;
using WB.Core.BoundedContexts.Headquarters.Services.Export;
using WB.Core.BoundedContexts.Headquarters.ValueObjects.Export;
using WB.Core.BoundedContexts.Headquarters.Views.DataExport;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.InterviewHistory;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.GenericSubdomains.Portable.Tasks;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.Infrastructure.Transactions;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.GenericSubdomains.Portable.Implementation.ServiceVariables;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.Infrastructure.Versions;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;

namespace WB.Core.BoundedContexts.Headquarters.DataExport.Services
{
    internal class ReadSideToTabularFormatExportService : ITabularFormatExportService
    {
        private readonly string dataFileExtension = "tab";

        private readonly IFileSystemAccessor fileSystemAccessor;
        private readonly ICsvWriter csvWriter;
        private readonly ILogger logger;

        private readonly ITransactionManagerProvider transactionManager;
        private readonly CommentsExporter commentsExporter;
        private readonly InterviewActionsExporter interviewActionsExporter;
        private readonly InterviewsExporter interviewsExporter;

        private readonly IQuestionnaireExportStructureStorage questionnaireExportStructureStorage;
        private readonly IQueryableReadSideRepositoryReader<InterviewSummary> interviewSummaries;
        private readonly InterviewDataExportSettings exportSettings;
        private readonly IProductVersion productVersion;
        private InterviewErrorsExporter errorsExporter;

        public ReadSideToTabularFormatExportService(IFileSystemAccessor fileSystemAccessor,
            ICsvWriter csvWriter, 
            ILogger logger,
            ITransactionManagerProvider transactionManager, 
            IQueryableReadSideRepositoryReader<InterviewSummary> interviewSummaries,
            InterviewDataExportSettings exportSettings, 
            IQuestionnaireExportStructureStorage questionnaireExportStructureStorage,
            IProductVersion productVersion)
        {
            this.fileSystemAccessor = fileSystemAccessor;
            this.csvWriter = csvWriter;
            this.logger = logger;
            this.transactionManager = transactionManager;
            this.interviewSummaries = interviewSummaries;
            this.exportSettings = exportSettings;
            this.questionnaireExportStructureStorage = questionnaireExportStructureStorage;
            this.productVersion = productVersion;

            this.interviewsExporter = ServiceLocator.Current.GetInstance<InterviewsExporter>();

            this.commentsExporter = ServiceLocator.Current.GetInstance<CommentsExporter>();
            this.errorsExporter = ServiceLocator.Current.GetInstance<InterviewErrorsExporter>();

            this.interviewActionsExporter = ServiceLocator.Current.GetInstance<InterviewActionsExporter>();
        }

        public void GenerateDescriptionFile(QuestionnaireIdentity questionnaireIdentity, string basePath, string dataFilesExtension)
        {
            QuestionnaireExportStructure questionnaireExportStructure = this.GetQuestionnaireExportStructure(questionnaireIdentity.QuestionnaireId, questionnaireIdentity.Version);

            var descriptionBuilder = new StringBuilder();
            descriptionBuilder.AppendLine(
                $"Exported from Survey Solutions Headquarters {this.productVersion} on {DateTime.Today:D}");

            foreach (var level in questionnaireExportStructure.HeaderToLevelMap.Values)
            {
                string fileName = $"{level.LevelName}{dataFilesExtension}";
                var variables = level.HeaderItems.Values.Select(question => question.VariableName);

                descriptionBuilder.AppendLine();
                descriptionBuilder.AppendLine(fileName);
                descriptionBuilder.AppendLine(string.Join(", ", variables));
            }

            this.fileSystemAccessor.WriteAllText(
                this.fileSystemAccessor.CombinePath(basePath, "description.txt"),
                descriptionBuilder.ToString());
        }

        public void ExportInterviewsInTabularFormat(QuestionnaireIdentity questionnaireIdentity,
            InterviewStatus? status, 
            string basePath, 
            IProgress<int> progress, 
            CancellationToken cancellationToken)
        {
            QuestionnaireExportStructure questionnaireExportStructure = this.GetQuestionnaireExportStructure(questionnaireIdentity.QuestionnaireId, questionnaireIdentity.Version);

            var exportInterviewsProgress = new Progress<int>();
            var exportCommentsProgress = new Progress<int>();
            var exportInterviewActionsProgress = new Progress<int>();
            var exportErrorsProgress = new Progress<int>();

            ProggressAggregator proggressAggregator = new ProggressAggregator();
            proggressAggregator.Add(exportInterviewsProgress, 0.7);
            proggressAggregator.Add(exportCommentsProgress, 0.1);
            proggressAggregator.Add(exportInterviewActionsProgress, 0.1);
            proggressAggregator.Add(exportErrorsProgress, 0.1);

            proggressAggregator.ProgressChanged += (sender, overallProgress) => progress.Report(overallProgress);


            var interviewsToExport = GetInterviewsToExport(questionnaireIdentity, status, cancellationToken);
            var interviewIdsToExport = interviewsToExport.Select(x => x.Id).ToList();

            Stopwatch exportWatch = new Stopwatch();
            exportWatch.Start(); 

            Task.WaitAll(new[] {
                Task.Run(() => this.interviewsExporter.Export(questionnaireExportStructure, interviewsToExport, basePath, exportInterviewsProgress, cancellationToken), cancellationToken),
                Task.Run(() => this.commentsExporter.Export(questionnaireExportStructure, interviewIdsToExport, basePath, exportCommentsProgress), cancellationToken),
                Task.Run(() => this.interviewActionsExporter.Export(questionnaireIdentity, interviewIdsToExport, basePath, exportInterviewActionsProgress), cancellationToken),
                Task.Run(() => this.errorsExporter.Export(questionnaireExportStructure, interviewIdsToExport, basePath, exportErrorsProgress, cancellationToken), cancellationToken),
            }, cancellationToken);
            exportWatch.Stop();

            this.logger.Info($"Export with all steps finished for questionnaire {questionnaireIdentity}. Took {exportWatch.Elapsed:c}");
        }

        public List<InterviewToExport> GetInterviewsToExport(QuestionnaireIdentity questionnaireIdentity, InterviewStatus? status, CancellationToken cancellationToken)
        {
            Expression<Func<InterviewSummary, bool>> expression;
            if (status.HasValue)
            {
                InterviewStatus requiredStatus = status.Value;
                expression = x => x.QuestionnaireId == questionnaireIdentity.QuestionnaireId &&
                                  x.QuestionnaireVersion == questionnaireIdentity.Version &&
                                  x.Status == requiredStatus;
            }
            else
            {
                expression = x => x.QuestionnaireId == questionnaireIdentity.QuestionnaireId &&
                                  x.QuestionnaireVersion == questionnaireIdentity.Version;
            }

            List<InterviewToExport> interviewIdsToExport = new List<InterviewToExport>();

            var stopwatch = Stopwatch.StartNew();
           
            string lastRecivedId = null;
            while (true)
            {
                var interviews = this.transactionManager.GetTransactionManager().ExecuteInQueryTransaction(() =>
                    this.interviewSummaries.Query(_ => _
                        .Where(expression)
                        .OrderBy(x => x.InterviewId)
                        .Where(x => lastRecivedId == null || x.SummaryId.CompareTo(lastRecivedId) > 0)
                        .Select(x => new InterviewToExport(x.InterviewId, x.Key, x.HasErrors, x.Status))
                        .Take(this.exportSettings.InterviewIdsQueryBatchSize)
                        .ToList()));

                if (interviews.Count == 0) break;

                cancellationToken.ThrowIfCancellationRequested();
                interviewIdsToExport.AddRange(interviews);
                lastRecivedId = interviews.Last().Id.FormatGuid();
                this.logger.Debug($"Received {interviewIdsToExport.Count:n0} interview interview ids.");
            }
            stopwatch.Stop();
            this.logger.Info($"Received {interviewIdsToExport.Count:N0} interviewIds to start export. Took {stopwatch.Elapsed:g} to complete.");
            return interviewIdsToExport;
        }

        public void CreateHeaderStructureForPreloadingForQuestionnaire(QuestionnaireIdentity questionnaireIdentity, string basePath)
        {
            QuestionnaireExportStructure questionnaireExportStructure =
                this.questionnaireExportStructureStorage.GetQuestionnaireExportStructure(questionnaireIdentity);

            if (questionnaireExportStructure == null)
                return;

            this.CreateDataSchemaForInterviewsInTabular(questionnaireExportStructure, basePath);
        }

        public string[] GetTabularDataFilesFromFolder(string basePath)
        {
            var filesInDirectory = this.fileSystemAccessor.GetFilesInDirectory(basePath).Where(fileName => fileName.EndsWith("." + this.dataFileExtension)).ToArray();
            return filesInDirectory;
        }

        private void CreateDataSchemaForInterviewsInTabular(QuestionnaireExportStructure questionnaireExportStructure, string basePath)
        {
            foreach (var level in questionnaireExportStructure.HeaderToLevelMap.Values)
            {
                var dataByTheLevelFilePath =
                    this.fileSystemAccessor.CombinePath(basePath, Path.ChangeExtension(level.LevelName, this.dataFileExtension));

                var interviewLevelHeader = new List<string> {level.LevelIdColumnName};


                if (level.IsTextListScope)
                {
                    interviewLevelHeader.AddRange(level.ReferencedNames);
                }

                foreach (IExportedHeaderItem headerItem in level.HeaderItems.Values)
                {
                    interviewLevelHeader.AddRange(headerItem.ColumnNames);
                }

                if (level.LevelScopeVector.Length == 0)
                {
                    interviewLevelHeader.AddRange(ServiceColumns.SystemVariables.Values.Select(systemVariable => systemVariable.VariableExportColumnName));
                }

                interviewLevelHeader.AddRange(questionnaireExportStructure.GetAllParentColumnNamesForLevel(level.LevelScopeVector));

                this.csvWriter.WriteData(dataByTheLevelFilePath, new[] { interviewLevelHeader.ToArray() }, ExportFileSettings.DataFileSeparator.ToString());
            }
        }

        private QuestionnaireExportStructure GetQuestionnaireExportStructure(Guid questionnaireId, long questionnaireVersion)
            => this.questionnaireExportStructureStorage.GetQuestionnaireExportStructure(
                new QuestionnaireIdentity(questionnaireId, questionnaireVersion));
    }
}