﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WB.Services.Export.CsvExport.Exporters;
using WB.Services.Export.DescriptionGenerator;
using WB.Services.Export.Infrastructure;
using WB.Services.Export.Interview;
using WB.Services.Export.Questionnaire;
using WB.Services.Export.Questionnaire.Services;
using WB.Services.Export.Services;
using WB.Services.Export.Tenant;

namespace WB.Services.Export.CsvExport.Implementation
{
    internal class CsvExport : ICsvExport
    {
        private readonly string dataFileExtension = "tab";

        private readonly ILogger<CsvExport> logger;
        private readonly ITenantApi<IHeadquartersApi> tenantApi;

        private readonly ICommentsExporter commentsExporter;
        private readonly IInterviewActionsExporter interviewActionsExporter;
        private readonly IDiagnosticsExporter diagnosticsExporter;
        private readonly IQuestionnaireExportStructureFactory exportStructureFactory;
        private readonly IQuestionnaireStorage questionnaireStorage;
        private readonly IDescriptionGenerator descriptionGenerator;
        private readonly IEnvironmentContentService environmentContentService;

        private readonly IInterviewsExporter interviewsExporter;

        public CsvExport(ILogger<CsvExport> logger,
            ITenantApi<IHeadquartersApi> tenantApi,
            IInterviewsExporter interviewsExporter,
            ICommentsExporter commentsExporter,
            IDiagnosticsExporter diagnosticsExporter,
            IInterviewActionsExporter interviewActionsExporter,
            IQuestionnaireExportStructureFactory exportStructureFactory,
            IQuestionnaireStorage questionnaireStorage,
            IDescriptionGenerator descriptionGenerator,
            IEnvironmentContentService environmentContentService)
        {
            this.logger = logger;
            this.tenantApi = tenantApi;
            this.interviewsExporter = interviewsExporter;
            this.commentsExporter = commentsExporter;
            this.diagnosticsExporter = diagnosticsExporter;
            this.interviewActionsExporter = interviewActionsExporter;
            this.exportStructureFactory = exportStructureFactory;
            this.questionnaireStorage = questionnaireStorage;
            this.descriptionGenerator = descriptionGenerator;
            this.environmentContentService = environmentContentService;
        }

        public async Task ExportInterviewsInTabularFormat(
            TenantInfo tenant,
            QuestionnaireId questionnaireIdentity,
            InterviewStatus? status,
            string basePath,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var questionnaire = await this.questionnaireStorage.GetQuestionnaireAsync(tenant, questionnaireIdentity);

            QuestionnaireExportStructure questionnaireExportStructure = this.exportStructureFactory.GetQuestionnaireExportStructure(questionnaire, tenant);

            var exportInterviewsProgress = new Progress<int>();
            var exportCommentsProgress = new Progress<int>();
            var exportInterviewActionsProgress = new Progress<int>();

            //ProgressAggregator proggressAggregator = new ProgressAggregator();
            //proggressAggregator.Add(exportInterviewsProgress, 0.8);
            //proggressAggregator.Add(exportCommentsProgress, 0.1);
            //proggressAggregator.Add(exportInterviewActionsProgress, 0.1);

            //proggressAggregator.ProgressChanged += (sender, overallProgress) => progress.Report(overallProgress);

            var cancellationToken = CancellationToken.None;

            var api = this.tenantApi.For(tenant);
            var interviewsToExport = await api.GetInterviewsToExportAsync(questionnaireIdentity, status, fromDate, toDate);
            var interviewIdsToExport = interviewsToExport.Select(x => x.Id).ToList();

            Stopwatch exportWatch = Stopwatch.StartNew();
            basePath = Path.Combine(basePath, ".export", tenant.Id.ToString());
            Directory.CreateDirectory(basePath);

            await Task.WhenAll(
            this.commentsExporter.ExportAsync(questionnaireExportStructure, interviewIdsToExport, basePath, tenant, exportCommentsProgress, cancellationToken),
            this.interviewActionsExporter.ExportAsync(tenant, questionnaireIdentity, interviewIdsToExport, basePath, exportInterviewActionsProgress),
            this.interviewsExporter.ExportAsync(tenant, questionnaireExportStructure, questionnaire, interviewsToExport, basePath, exportInterviewsProgress, cancellationToken),
            this.diagnosticsExporter.ExportAsync(interviewIdsToExport, basePath, tenant, exportInterviewsProgress, cancellationToken)
         );

            exportWatch.Stop();


            this.logger.Log(LogLevel.Information, $"Export with all steps finished for questionnaire {questionnaireIdentity}. " +
                             $"Took {exportWatch.Elapsed:c} to export {interviewIdsToExport.Count} interviews");

            this.descriptionGenerator.GenerateDescriptionFile(questionnaireExportStructure, basePath, ExportFileSettings.TabDataFileExtension);

            this.environmentContentService.CreateEnvironmentFiles(questionnaireExportStructure, basePath, cancellationToken);
        }
    }
}
