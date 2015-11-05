﻿using System;
using WB.Core.BoundedContexts.Headquarters.DataExport.Dtos;
using WB.Core.BoundedContexts.Headquarters.DataExport.Factories;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.Transactions;

namespace WB.Core.BoundedContexts.Headquarters.DataExport.Services
{
    internal class DataExporter : IDataExporter
    {
        private bool IsWorking = false; //please use singleton injection

        private readonly IDataExportQueue dataExportQueue;

        private readonly IQuestionnaireDataExportServiceFactory questionnaireDataExportServiceFactory;

        private readonly ILogger logger;

        public DataExporter(IDataExportQueue dataExportQueue,
            ILogger logger,
            IQuestionnaireDataExportServiceFactory questionnaireDataExportServiceFactory)
        {
            this.dataExportQueue = dataExportQueue;
            this.logger = logger;
            this.questionnaireDataExportServiceFactory = questionnaireDataExportServiceFactory;
        }

        public void StartDataExport()
        {
            if (IsWorking)
                return;

            IsWorking = true;
            try
            {
                while (IsWorking)
                {
                    string dataExportProcessId = this.dataExportQueue.DeQueueDataExportProcessId();

                    if (string.IsNullOrEmpty(dataExportProcessId))
                        return;


                    var dataExportProcess = this.dataExportQueue.GetDataExportProcess(dataExportProcessId);

                    if (dataExportProcess == null)
                        return;

                    var questionnaireDataExportService =
                        questionnaireDataExportServiceFactory.CreateQuestionnaireDataExportService(
                            dataExportProcess.DataExportFormat);

                    if (questionnaireDataExportService == null)
                        return;

                    try
                    {
                        questionnaireDataExportService.ExportData(dataExportProcess);
                        this.dataExportQueue.FinishDataExportProcess(dataExportProcessId);
                    }
                    catch (Exception e)
                    {
                        logger.Error(
                            string.Format("data export process with id {0} finished with error", dataExportProcessId),
                            e);

                        this.dataExportQueue.FinishDataExportProcessWithError(dataExportProcessId, e);
                    }
                }
            }
            finally
            {
                IsWorking = false;
            }
        }
    }
}