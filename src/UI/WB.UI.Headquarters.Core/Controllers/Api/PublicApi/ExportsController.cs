﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using WB.Core.BoundedContexts.Headquarters.DataExport;
using WB.Core.BoundedContexts.Headquarters.DataExport.Dtos;
using WB.Core.BoundedContexts.Headquarters.DataExport.Security;
using WB.Core.BoundedContexts.Headquarters.DataExport.Views;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.UI.Shared.Web.Services;

namespace WB.UI.Headquarters.Controllers.Api.PublicApi
{
    /// <summary>
    /// Provides a methods for managing export related actions
    /// </summary>
    [Authorize(Roles = "ApiUser, Administrator")]
    [Route(@"api/v2/exports")]
    public class ExportsController : ControllerBase
    {
        private readonly IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory;
        private readonly IDataExportStatusReader dataExportStatusReader;
        private readonly IExportServiceApi exportServiceApi;
        private readonly IExportSettings exportSettings;
        private readonly ISystemLog auditLog;
        private readonly IExportFileNameService exportFileNameService;
        private readonly IVirtualPathService env;

        public ExportsController(IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory,
            IDataExportStatusReader dataExportStatusReader,
            IExportServiceApi exportServiceApi,
            IExportSettings exportSettings,
            ISystemLog auditLog,
            IExportFileNameService exportFileNameService,
            IVirtualPathService env)
        {
            this.questionnaireBrowseViewFactory = questionnaireBrowseViewFactory;
            this.dataExportStatusReader = dataExportStatusReader;
            this.exportServiceApi = exportServiceApi;
            this.exportSettings = exportSettings;
            this.auditLog = auditLog;
            this.exportFileNameService = exportFileNameService;
            this.env = env;
        }

        /// <summary>
        /// Start export file creation
        /// </summary>
        /// <param name="id">Questionnaire id in format [QuestionnaireGuid$Version]</param>
        /// <param name="exportType">Format of export data to download</param>
        /// <param name="status">Status of exported interviews</param>
        /// <param name="from">Started date for timeframe of exported interviews (when change was done to an interview). Should be in UTC date</param>
        /// <param name="to">Finished date for timeframe of exported interviews (when change was done to an interview). Should be in UTC date</param>
        /// 
        /// <response code="200">Export started</response>
        /// <response code="400">Questionnaire id is malformed</response>
        /// <response code="404">Questionnaire was not found</response>
        [HttpPost]
        public async Task<ActionResult<ExportProcess>> PostExports(CreateExportProcess request)
        {
            if (!QuestionnaireIdentity.TryParse(request.QuestionnaireId, out var questionnaireIdentity))
                return StatusCode(StatusCodes.Status400BadRequest, @"Invalid questionnaire identity");

            var questionnaireBrowseItem = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);
            if (questionnaireBrowseItem == null)
                return StatusCode(StatusCodes.Status404NotFound, @"Questionnaire not found");

            var password = this.exportSettings.EncryptionEnforced()
                ? this.exportSettings.GetPassword()
                : null;

            var interviewStatus = request.InterviewStatus == ExportInterviewType.All
                ? (InterviewStatus?) null
                : (InterviewStatus) request.InterviewStatus;

            var result = await this.exportServiceApi.RequestUpdate(questionnaireIdentity.ToString(),
                (DataExportFormat) request.ExportType, interviewStatus, request.From, request.To, password,
                request.AccessToken, (WB.Core.BoundedContexts.Headquarters.DataExport.Dtos.ExternalStorageType?) request.StorageType);

            this.auditLog.ExportStared(
                $@"{questionnaireBrowseItem.Title} v{questionnaireBrowseItem.Version} {request.InterviewStatus.ToString() ?? ""}",
                (DataExportFormat) request.ExportType);

            return CreatedAtAction(nameof(GetExports), new {id = result.JobId}, request);
        }

        /// <summary>
        /// Get detailed information about export process
        /// </summary>
        /// <param name="id">Export process id</param>
        /// 
        /// <response code="200"></response>
        /// <response code="404">Export process was not found</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ExportProcess>> GetExports(long id)
        {
            var exportProcess = await this.dataExportStatusReader.GetProcessStatus(id);
            if (exportProcess == null) return NotFound();

            return this.Ok(ToExportProcess(exportProcess));
        }

        /// <summary>
        /// Get list of export processes
        /// </summary>
        /// <param name="exportType">Format of export data to download</param>
        /// <param name="interviewStatus">Status of exported interviews</param>
        /// <param name="questionnaireIdentity">Questionnaire id</param>
        /// <param name="exportStatus">Status of export process</param>
        /// <param name="hasFile">Has export process file to download</param>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExportProcess>>> GetExports(ExportType? exportType,
            ExportInterviewType? interviewStatus, string questionnaireIdentity, ExportStatus? exportStatus,
            bool? hasFile)
        {
            var status = interviewStatus == ExportInterviewType.All ? null : (InterviewStatus?) interviewStatus;

            var filteredProcesses = await this.exportServiceApi.GetJobsByQuery((DataExportFormat?) exportType,
                status, questionnaireIdentity, (DataExportJobStatus?) exportStatus, hasFile);

            return this.Ok(filteredProcesses.Select(ToExportProcess));
        }

        /// <summary>
        /// Cancel export process
        /// </summary>
        ///
        /// <response code="200">Export deleted</response>
        /// <response code="404">Export process was not found</response>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ExportProcess>> CancelExports(long id)
        {
            var exportProcess = await this.dataExportStatusReader.GetProcessStatus(id);
            if (exportProcess == null) return NotFound();

            await this.exportServiceApi.DeleteProcess(id);

            exportProcess = await this.dataExportStatusReader.GetProcessStatus(id);

            return this.Ok(ToExportProcess(exportProcess));
        }

        /// <summary>
        /// Downloads export file. It will return either 200 status code with export file content or 302 redirect to export location.
        /// </summary>
        /// <param name="id">Export process id</param>
        /// 
        /// <response code="200">Returns content of the export file as zip archive</response>
        /// <response code="302">Location header contains location of export file for download</response>
        /// <response code="404">Export process was not found</response>
        /// <response code="400">Export file was not generated yet</response>
        [HttpGet]
        [Route(@"{id}/file")]
        public async Task<ActionResult> GetExportFile(long id)
        {
            var exportProcess = await this.dataExportStatusReader.GetProcessStatus(id);
            if (exportProcess == null) return NotFound();

            if (exportProcess.Format == DataExportFormat.DDI)
            {
                var archivePassword =
                    this.exportSettings.EncryptionEnforced() ? this.exportSettings.GetPassword() : null;
                var ddiArchiveResponse = await exportServiceApi.GetDdiArchive(
                    exportProcess.QuestionnaireIdentity.ToString(),
                    archivePassword);

                var fileNameForDdiByQuestionnaire =
                    this.exportFileNameService.GetFileNameForDdiByQuestionnaire(exportProcess.QuestionnaireIdentity);

                var content = await ddiArchiveResponse.ReadAsStreamAsync();

                return File(content, "application/zip", fileNameForDdiByQuestionnaire);
            }

            var result = await this.dataExportStatusReader.GetDataArchive(
                exportProcess.QuestionnaireIdentity, exportProcess.Format, exportProcess.InterviewStatus,
                exportProcess.FromDate, exportProcess.ToDate);

            return result == null
                ? BadRequest()
                : result.Redirect != null
                    ? (ActionResult) Redirect(result.Redirect)
                    : File(result.Data, "application/zip", result.FileName);
        }

        private ExportProcess ToExportProcess(DataExportProcessView exportProcess)
        {
            var process = new ExportProcess
            {
                JobId = exportProcess.Id,
                QuestionnaireId = exportProcess.QuestionnaireIdentity.ToString(),
                From = exportProcess.FromDate,
                To = exportProcess.ToDate,
                StartDate = exportProcess.BeginDate,
                CompleteDate = exportProcess.EndDate,
                ExportStatus = (ExportStatus) exportProcess.JobStatus,
                Progress = exportProcess.Progress,
                ExportType = (ExportType) exportProcess.Format,
                ETA = exportProcess.TimeEstimation,
                Error = exportProcess.Error?.Message,
                HasExportFile = exportProcess.HasFile,
                InterviewStatus = exportProcess.InterviewStatus == null
                    ? ExportInterviewType.All
                    : (ExportInterviewType) exportProcess.InterviewStatus
            };

            if (exportProcess.IsRunning || exportProcess.HasFile)
                process.Links = new ExportJobLinks();
            if (exportProcess.IsRunning)
                process.Links.Cancel = $"{this.env.GetBaseUrl()}{Url.Action("CancelExports", new {id = exportProcess.Id})}";
            if (exportProcess.HasFile)
                process.Links.Download = $"{this.env.GetBaseUrl()}{Url.Action("GetExportFile", new {id = exportProcess.Id})}";

            return process;
        }

        public class CreateExportProcess
        {
            public ExportType ExportType { get; set; }
            public string QuestionnaireId { get; set; }
            public ExportInterviewType InterviewStatus { get; set; }
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
            public string AccessToken { get; set; }
            public ExternalStorageType? StorageType { get; set; }
        }

        public class ExportProcess : CreateExportProcess
        {
            public long JobId { get; set; }
            public ExportStatus ExportStatus { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? CompleteDate { get; set; }
            public int Progress { get; set; }
            public TimeSpan? ETA { get; set; }
            public string Error { get; set; }
            public ExportJobLinks Links { get; set; }
            public bool HasExportFile { get; set; }
        }

        public class ExportJobLinks
        {
            public string Cancel { get; set; }
            public string Download { get; set; }
        }

        public enum ExportType
        {
            Tabular = 1,
            STATA = 2,
            SPSS = 3,
            Binary = 4,
            DDI = 5,
            Paradata = 6
        }

        public enum ExportStatus
        {
            Created = 0,
            Running = 1,
            Completed = 2,
            Fail = 3,
            Canceled = 4
        }

        public enum ExportInterviewType
        {
            All,
            SupervisorAssigned = 40,
            InterviewerAssigned = 60,
            Completed = 100,

            RejectedBySupervisor = 65,
            ApprovedBySupervisor = 120,

            RejectedByHeadquarters = 125,
            ApprovedByHeadquarters = 130
        }

        public enum ExternalStorageType
        {
            Dropbox = 1,
            OneDrive = 2,
            GoogleDrive = 3
        }
    }
}
