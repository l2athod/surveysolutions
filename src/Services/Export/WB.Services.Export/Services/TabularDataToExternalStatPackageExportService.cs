﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StatData.Core;
using WB.Services.Export.CsvExport.Implementation.DoFiles;
using WB.Services.Export.Interview;
using WB.Services.Export.Questionnaire;
using WB.Services.Export.Services.Processing;
using WB.Services.Infrastructure.Tenant;

namespace WB.Services.Export.Services
{
    public class TabularDataToExternalStatPackageExportService : ITabularDataToExternalStatPackageExportService
    {
        private readonly ILogger<TabularDataToExternalStatPackageExportService> logger;

        private readonly IQuestionnaireExportStructureFactory questionnaireExportStructureStorage;
        private readonly ITabFileReader tabReader;
        private readonly IDatasetWriterFactory datasetWriterFactory;
        private readonly IDataQueryFactory dataQueryFactory;

        private readonly IQuestionnaireLabelFactory questionnaireLabelFactory;

        private readonly IExportServiceDataProvider exportSeviceDataProvider;

        public TabularDataToExternalStatPackageExportService(ILogger<TabularDataToExternalStatPackageExportService> logger,
            ITabFileReader tabReader,
            IDataQueryFactory dataQueryFactory,
            IDatasetWriterFactory datasetWriterFactory,
            IQuestionnaireLabelFactory questionnaireLabelFactory,
            IQuestionnaireExportStructureFactory questionnaireExportStructureStorage,
            IExportServiceDataProvider exportSeviceDataProvider)
        {
            this.logger = logger;

            this.tabReader = tabReader;
            this.datasetWriterFactory = datasetWriterFactory;
            this.questionnaireLabelFactory = questionnaireLabelFactory;
            this.questionnaireExportStructureStorage = questionnaireExportStructureStorage;
            this.dataQueryFactory = dataQueryFactory;
            this.exportSeviceDataProvider = exportSeviceDataProvider;

        }

        private const string StataFileNameExtension = ExportFileSettings.StataDataFileExtension;
        private const string SpssFileNameExtension = ExportFileSettings.SpssDataFileExtension;

        public async Task<string[]> CreateAndGetStataDataFilesForQuestionnaireAsync(TenantInfo tenant, 
            QuestionnaireId questionnaireId,
            Guid? translationId,
            string[] tabularDataFiles,
            ExportProgress progress,
            CancellationToken cancellationToken)
        {
            return await this.CreateAndGetExportDataFiles(tenant, 
                questionnaireId,
                translationId,
                DataExportFormat.STATA,
                tabularDataFiles, 
                progress,
                cancellationToken);
        }

        public async Task<string[]> CreateAndGetSpssDataFilesForQuestionnaireAsync(TenantInfo tenant,
            QuestionnaireId questionnaireId,
            Guid? translationId,
            string[] tabularDataFiles,
            ExportProgress progress,
            CancellationToken cancellationToken)
        {
            return await this.CreateAndGetExportDataFiles(tenant, questionnaireId, translationId, DataExportFormat.SPSS, tabularDataFiles, progress, cancellationToken);
        }

        private async Task<string[]> CreateAndGetExportDataFiles(TenantInfo tenant, QuestionnaireId questionnaireId,
            Guid? translationId, DataExportFormat format,
            string[] dataFiles, ExportProgress progress, CancellationToken cancellationToken)
        {
            string currentDataInfo = string.Empty;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var questionnaireExportStructure = await
                        this.questionnaireExportStructureStorage
                            .GetQuestionnaireExportStructureAsync(tenant, questionnaireId, translationId);

                if (questionnaireExportStructure == null)
                    return Array.Empty<string>();

                var labelsForQuestionnaire =  this.questionnaireLabelFactory.CreateLabelsForQuestionnaire(questionnaireExportStructure);

                var serviceDataLabels = this.exportSeviceDataProvider.GetServiceDataLabels();

                var result = new List<string>();
                string fileExtension = format == DataExportFormat.STATA
                    ? StataFileNameExtension
                    : SpssFileNameExtension;

                var writer = this.datasetWriterFactory.CreateDatasetWriter(format);
                long processedFiles = 0;

                foreach (var tabFile in dataFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    currentDataInfo = $"filename: {tabFile}";
                    string dataFilePath = Path.ChangeExtension(tabFile, fileExtension);

                    var meta = this.tabReader.GetMetaFromTabFile(tabFile);

                    var fileName = Path.GetFileNameWithoutExtension(tabFile);

                    var questionnaireLevelLabels =
                        labelsForQuestionnaire.FirstOrDefault(
                            x => string.Equals(x.LevelName, fileName, StringComparison.InvariantCultureIgnoreCase));

                    if (questionnaireLevelLabels != null)
                        UpdateMetaWithLabels(meta, questionnaireLevelLabels);
                    else if (serviceDataLabels.ContainsKey(fileName))
                        UpdateMetaWithLabels(meta, serviceDataLabels[fileName]);

                    meta.ExtendedMissings.Add(ExportFormatSettings.MissingNumericQuestionValue, "missing");
                    meta.ExtendedStrMissings.Add(ExportFormatSettings.MissingStringQuestionValue, "missing");

                    using (IDataQuery tabStreamDataQuery = dataQueryFactory.CreateDataQuery(tabFile))
                    {
                        logger.LogTrace("Tab file data written: {tabFile} {filePath}", tabFile, dataFilePath);
                        writer.WriteToFile(dataFilePath, meta, tabStreamDataQuery);
                    }

                    result.Add(dataFilePath);

                    processedFiles++;
                    progress.Report(processedFiles.PercentOf(dataFiles.Length));

                }
                return result.ToArray();
            }
            catch (Exception exc)
            {
                this.logger.Log(LogLevel.Error, exc, $"Error on data export (questionnaireId:{questionnaireId}): ");
                this.logger.LogError(currentDataInfo);

                throw;
            }
        }

        private static void UpdateMetaWithLabels(IDatasetMeta meta, QuestionnaireLevelLabels questionnaireLevelLabels)
        {
            for (int index = 0; index < meta.Variables.Length; index++)
            {
                var variableName = meta.Variables[index].VarName;

                if (!questionnaireLevelLabels.ContainsVariable(variableName))
                    continue;

                var variableLabels = questionnaireLevelLabels[variableName];

                meta.Variables[index] = new DatasetVariable(variableName)
                {
                    Storage = GetStorageType(variableLabels.ValueType), VarLabel = variableLabels.VariableLabel
                };

                var valueSet = new ValueSet();

                if (variableLabels.Value.IsReference)
                {
                    var labels =
                        questionnaireLevelLabels.PredefinedLabels.FirstOrDefault(x =>
                            x.Name == variableLabels.Value.Name);
                    if (labels != null)
                    {
                        foreach (var variableValueLabel in labels.VariableValues)
                        {
                            if (double.TryParse(variableValueLabel.Value, NumberStyles.Any, CultureInfo.InvariantCulture,
                                out double value))
                                valueSet.Add(value, variableValueLabel.Label);
                        }
                    }
                }
                else
                {
                    foreach (var variableValueLabel in variableLabels.Value.VariableValues)
                    {
                        if (double.TryParse(variableValueLabel.Value, NumberStyles.Any, CultureInfo.InvariantCulture,
                            out var value))
                            valueSet.Add(value, variableValueLabel.Label);
                    }
                }

                meta.AssociateValueSet(variableName, valueSet);
            }
        }

        private static void UpdateMetaWithLabels(IDatasetMeta meta, Dictionary<string, HeaderItemDescription> serviceLabels)
        {
            for (int index = 0; index < meta.Variables.Length; index++)
            {
                if (!serviceLabels.ContainsKey(meta.Variables[index].VarName))
                    continue;

                var variableLabels = serviceLabels[meta.Variables[index].VarName];

                meta.Variables[index] = new DatasetVariable(meta.Variables[index].VarName)
                {
                    Storage = GetStorageType(variableLabels.ValueType)
                };

                meta.Variables[index].VarLabel = variableLabels.Label;


                if (variableLabels.VariableValueLabels.Any())
                {
                    var valueSet = new ValueSet();

                    foreach (var variableValueLabel in variableLabels.VariableValueLabels)
                    {
                        double value;
                        if (double.TryParse(variableValueLabel.Value, NumberStyles.Any, CultureInfo.InvariantCulture,
                            out value))
                            valueSet.Add(value, variableValueLabel.Label);
                    }

                    meta.AssociateValueSet(meta.Variables[index].VarName, valueSet);
                }
            }
        }

        private static VariableStorage GetStorageType(ExportValueType variableLabelsValueType)
        {
            switch (variableLabelsValueType)
            {
                case ExportValueType.String:
                    return VariableStorage.StringStorage;
                case ExportValueType.NumericInt:
                    return VariableStorage.NumericIntegerStorage;
                case ExportValueType.Numeric:
                    return VariableStorage.NumericStorage;
                case ExportValueType.Date:
                    return VariableStorage.DateStorage;
                case ExportValueType.DateTime:
                    return VariableStorage.DateTimeStorage;
                case ExportValueType.Boolean:
                    return VariableStorage.NumericIntegerStorage;

                case ExportValueType.Unknown:
                default:
                    return VariableStorage.UnknownStorage;
            }
        }
    }
}
