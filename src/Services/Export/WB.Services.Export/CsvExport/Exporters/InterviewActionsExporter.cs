﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WB.Services.Export.Infrastructure;
using WB.Services.Export.Interview;
using WB.Services.Export.Interview.Entities;
using WB.Services.Export.Questionnaire;
using WB.Services.Export.Services;
using WB.Services.Infrastructure;
using WB.Services.Infrastructure.Tenant;

namespace WB.Services.Export.CsvExport.Exporters
{
    public class InterviewActionsExporter: IInterviewActionsExporter
    {
        private readonly IOptions<ExportServiceSettings> interviewDataExportSettings;
        public string InterviewActionsFileName => "interview__actions";

        public DoExportFileHeader[] ActionFileColumns => new []
        {
            CommonHeaderItems.InterviewKey,
            CommonHeaderItems.InterviewId,
            new DoExportFileHeader("date", "Date when the action was taken", ExportValueType.String),
            new DoExportFileHeader("time", "Time when the action was taken", ExportValueType.String),
            new DoExportFileHeader("action", "Type of action taken", ExportValueType.NumericInt, 
                Enum.GetValues(typeof(InterviewExportedAction))
                    .Cast<InterviewExportedAction>()
                    .Select(x => new VariableValueLabel(((int)x).ToString(), x.ToString())).ToArray()),
            new DoExportFileHeader("originator", "Login name of the person performing the action", ExportValueType.String),
            new DoExportFileHeader("role", "System role of the person performing the action", ExportValueType.NumericInt,
                ExportHelper.RolesMap
                    .Select(x => new VariableValueLabel(x.Key.ToString(CultureInfo.InvariantCulture), x.Value)).ToArray()),
            new DoExportFileHeader("responsible__name", "Login name of the person now responsible for the interview", ExportValueType.String),
            new DoExportFileHeader("responsible__role", "System role of the person now responsible for the interview", ExportValueType.NumericInt,
                ExportHelper.RolesMap
                    .Select(x => new VariableValueLabel(x.Key.ToString(CultureInfo.InvariantCulture), x.Value)).ToArray())
        };

        private readonly string dataFileExtension = "tab";
        private readonly ICsvWriter csvWriter;
        private readonly ITenantApi<IHeadquartersApi> tenantApi;
        public InterviewActionsExporter(IOptions<ExportServiceSettings> interviewDataExportSettings,
            ICsvWriter csvWriter,
            ITenantApi<IHeadquartersApi> tenantApi)
        {
            this.interviewDataExportSettings = interviewDataExportSettings;
            this.csvWriter = csvWriter;
            this.tenantApi = tenantApi;
        }

        public async Task ExportAsync(TenantInfo tenant, QuestionnaireId questionnaireIdentity, List<Guid> interviewIdsToExport,
            string basePath, ExportProgress progress, CancellationToken cancellationToken = default)
        {
            var actionFilePath = Path.Combine(basePath, Path.ChangeExtension(this.InterviewActionsFileName, this.dataFileExtension));
            var batchSize = this.interviewDataExportSettings.Value.MaxRecordsCountPerOneExportQuery;

            var fileColumns = ActionFileColumns.Select(a => a.Title).ToArray();
            this.csvWriter.WriteData(actionFilePath, new[] { fileColumns }, ExportFileSettings.DataFileSeparator.ToString());

            long totalProcessedCount = 0;
            var api = this.tenantApi.For(tenant);

            var batchOptions = new BatchOptions { Max = batchSize };

            foreach (var interviewsBatch in interviewIdsToExport.BatchInTime(batchOptions))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var interviewIdsStrings = interviewsBatch.ToArray();
                var actionsChunk = await this.QueryActionsChunkFromReadSide(api, interviewIdsStrings);
                cancellationToken.ThrowIfCancellationRequested();
                this.csvWriter.WriteData(actionFilePath, actionsChunk, ExportFileSettings.DataFileSeparator.ToString());

                totalProcessedCount += interviewIdsStrings.Length;
                progress.Report(totalProcessedCount.PercentOf(interviewIdsToExport.Count));
                cancellationToken.ThrowIfCancellationRequested();
            }

            progress.Report(100);
        }

        public void ExportDoFile(QuestionnaireExportStructure questionnaireExportStructure, string basePath)
        {
            var doContent = new DoFile();

            doContent.BuildInsheet(Path.ChangeExtension(this.InterviewActionsFileName, this.dataFileExtension));
            doContent.AppendLine();

            foreach (var actionFileColumn in ActionFileColumns)
            {
                if (actionFileColumn.VariableValueLabels.Any())
                {
                    doContent.DefineLabel(actionFileColumn.Title, actionFileColumn.VariableValueLabels);
                    doContent.AssignValuesToVariable(actionFileColumn.Title, actionFileColumn.Title);
                }

                doContent.AppendLabelToVariableMatching(actionFileColumn.Title, actionFileColumn.Description);
            }

            var fileName = $"{InterviewActionsFileName}.{DoFile.ContentFileNameExtension}";
            var contentFilePath = Path.Combine(basePath, fileName);

            File.WriteAllText(contentFilePath, doContent.ToString());
        }

        private async Task<List<string[]>> QueryActionsChunkFromReadSide(IHeadquartersApi api, Guid[] interviewIds)
        {
            var interviews = await api.GetInterviewSummariesBatchAsync(interviewIds);
            var result = new List<string[]>();

            foreach (var interview in interviews)
            {
                var resultRow = new List<string>
                {
                    interview.Key,
                    interview.InterviewId.FormatGuid(),
                    interview.Timestamp.ToString(ExportFormatSettings.ExportDateFormat, CultureInfo.InvariantCulture),
                    interview.Timestamp.ToString("T", CultureInfo.InvariantCulture),
                    ((int)interview.Status).ToString(CultureInfo.InvariantCulture),
                    interview.StatusChangeOriginatorName,
                    ExportHelper.GetUserRoleDisplayValue(interview.StatusChangeOriginatorRole),
                    this.GetResponsibleName(interview.Status, interview.InterviewerName, interview.SupervisorName, interview.StatusChangeOriginatorName),
                    this.GetResponsibleRole(interview.Status, interview.StatusChangeOriginatorRole, interview.InterviewerName)
                };
                result.Add(resultRow.ToArray());
            }
            return result;
        }

        private string GetResponsibleName(InterviewExportedAction status, string interviewerName, string supervisorName, string statusChangeOriginatorName)
        {
            switch (status)
            {
                case InterviewExportedAction.Created:
                    return statusChangeOriginatorName;
                case InterviewExportedAction.SupervisorAssigned:
                case InterviewExportedAction.Completed:
                case InterviewExportedAction.RejectedByHeadquarter:
                case InterviewExportedAction.UnapprovedByHeadquarter:
                    return supervisorName;
                case InterviewExportedAction.ApprovedBySupervisor:
                    return "any headquarters";
                case InterviewExportedAction.ApprovedByHeadquarter:
                    return string.Empty;
            }

            return interviewerName;
        }

        private string GetResponsibleRole(InterviewExportedAction status, UserRoles statusChangeOriginatorRole, string interviewerName)
        {
            switch (status)
            {
                case InterviewExportedAction.Created:
                case InterviewExportedAction.TranslationSwitched:
                    return ExportHelper.GetUserRoleDisplayValue(statusChangeOriginatorRole);
                case InterviewExportedAction.SupervisorAssigned:
                case InterviewExportedAction.Completed:
                case InterviewExportedAction.RejectedByHeadquarter:
                case InterviewExportedAction.UnapprovedByHeadquarter:
                case InterviewExportedAction.OpenedBySupervisor:
                case InterviewExportedAction.ClosedBySupervisor:
                    return ExportHelper.Supervisor.ToString(CultureInfo.InvariantCulture);
                case InterviewExportedAction.ApprovedBySupervisor:
                    return ExportHelper.Headquarter.ToString(CultureInfo.InvariantCulture);
                case InterviewExportedAction.ApprovedByHeadquarter:
                    return ExportHelper.NoRole;
                case InterviewExportedAction.InterviewerAssigned:
                    if (string.IsNullOrWhiteSpace(interviewerName))
                        return ExportHelper.NoRole;
                    break;
            }

            return ExportHelper.Interviewer.ToString(CultureInfo.InvariantCulture);
        }
    }
}
