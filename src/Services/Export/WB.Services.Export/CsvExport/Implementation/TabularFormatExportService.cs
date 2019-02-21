﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WB.Services.Export.CsvExport.Exporters;
using WB.Services.Export.ExportProcessHandlers;
using WB.Services.Export.Infrastructure;
using WB.Services.Export.Models;
using WB.Services.Export.Questionnaire;
using WB.Services.Export.Questionnaire.Services;
using WB.Services.Export.Services;
using WB.Services.Export.Utils;
using WB.Services.Infrastructure.Tenant;

namespace WB.Services.Export.CsvExport.Implementation
{
    public class TabularFormatExportService : ITabularFormatExportService
    {
        private readonly ILogger<TabularFormatExportService> logger;
        private readonly ITenantApi<IHeadquartersApi> tenantApi;

        private readonly ICommentsExporter commentsExporter;
        private readonly IInterviewActionsExporter interviewActionsExporter;
        private readonly IDiagnosticsExporter diagnosticsExporter;
        private readonly IQuestionnaireExportStructureFactory exportStructureFactory;
        private readonly IQuestionnaireStorage questionnaireStorage;

        private readonly IProductVersion productVersion;
        private readonly IFileSystemAccessor fileSystemAccessor;
        private readonly IInterviewsExporter interviewsExporter;

        public TabularFormatExportService(
            ILogger<TabularFormatExportService> logger,
            ITenantApi<IHeadquartersApi> tenantApi,
            IInterviewsExporter interviewsExporter,
            ICommentsExporter commentsExporter,
            IDiagnosticsExporter diagnosticsExporter,
            IInterviewActionsExporter interviewActionsExporter,
            IQuestionnaireExportStructureFactory exportStructureFactory,
            IQuestionnaireStorage questionnaireStorage,
            IProductVersion productVersion, 
            IFileSystemAccessor fileSystemAccessor)
        {
            this.logger = logger;
            this.tenantApi = tenantApi;
            this.interviewsExporter = interviewsExporter;
            this.commentsExporter = commentsExporter;
            this.diagnosticsExporter = diagnosticsExporter;
            this.interviewActionsExporter = interviewActionsExporter;
            this.exportStructureFactory = exportStructureFactory;
            this.questionnaireStorage = questionnaireStorage;
            this.productVersion = productVersion;
            this.fileSystemAccessor = fileSystemAccessor;
        }

        public async Task ExportInterviewsInTabularFormatAsync(
            ExportSettings settings,
            string tempPath,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            var tenant = settings.Tenant;
            var questionnaireIdentity = settings.QuestionnaireId;
            var status = settings.Status;
            var fromDate = settings.FromDate;
            var toDate = settings.ToDate;

            var questionnaire = await this.questionnaireStorage.GetQuestionnaireAsync(tenant, questionnaireIdentity);

            QuestionnaireExportStructure questionnaireExportStructure = this.exportStructureFactory.CreateQuestionnaireExportStructure(questionnaire);

            var exportInterviewsProgress = new Progress<int>();
            var exportCommentsProgress = new Progress<int>();
            var exportInterviewActionsProgress = new Progress<int>();
            var exportDiagnosticsProgress = new Progress<int>();

            ProgressAggregator progressAggregator = new ProgressAggregator();
            progressAggregator.Add(exportInterviewsProgress, 0.4);
            progressAggregator.Add(exportCommentsProgress, 0.2);
            progressAggregator.Add(exportInterviewActionsProgress, 0.2);
            progressAggregator.Add(exportDiagnosticsProgress, 0.2);

            progressAggregator.ProgressChanged += (sender, overallProgress) => progress.Report(overallProgress);

            var api = this.tenantApi.For(tenant);
            var interviewsToExport = await api.GetInterviewsToExportAsync(questionnaireIdentity, status, fromDate, toDate);
            var interviewIdsToExport = interviewsToExport.Select(x => x.Id).ToList();

            Stopwatch exportWatch = Stopwatch.StartNew();

            await Task.WhenAll(
                this.commentsExporter.ExportAsync(questionnaireExportStructure, interviewIdsToExport, tempPath, tenant, exportCommentsProgress, cancellationToken),
                this.interviewActionsExporter.ExportAsync(tenant, questionnaireIdentity, interviewIdsToExport, tempPath, exportInterviewActionsProgress, cancellationToken),
                this.interviewsExporter.ExportAsync(tenant, questionnaireExportStructure, questionnaire, interviewsToExport, tempPath, exportInterviewsProgress, cancellationToken),
                this.diagnosticsExporter.ExportAsync(interviewIdsToExport, tempPath, tenant, exportDiagnosticsProgress, cancellationToken)
            );

            exportWatch.Stop();

            this.logger.LogInformation("Export with all steps finished for questionnaire {questionnaireIdentity}. " +
                                       "Took {elapsed:c} to export {interviewIds} interviews",
                questionnaireIdentity, exportWatch.Elapsed, interviewIdsToExport.Count
                );
        }
        
        public async Task GenerateDescriptionFileAsync(TenantInfo tenant, QuestionnaireId questionnaireId, string basePath, string dataFilesExtension)
        {
            QuestionnaireExportStructure questionnaireExportStructure = await this.exportStructureFactory.GetQuestionnaireExportStructureAsync(tenant, questionnaireId);

            var descriptionBuilder = new StringBuilder();
            descriptionBuilder.AppendLine($"Generated by Survey Solutions export module {this.productVersion} on {DateTime.Today:D}");

            foreach (var level in questionnaireExportStructure.HeaderToLevelMap.Values)
            {
                string fileName = $"{level.LevelName}{dataFilesExtension}";
                var variables = level.HeaderItems.Values.Select(question => question.VariableName);

                descriptionBuilder.AppendLine();
                descriptionBuilder.AppendLine(fileName);
                descriptionBuilder.AppendLine(string.Join(", ", variables));
            }

            this.fileSystemAccessor.WriteAllText(
                Path.Combine(basePath, "export__readme.txt"),
                descriptionBuilder.ToString());
        }
    }
}
