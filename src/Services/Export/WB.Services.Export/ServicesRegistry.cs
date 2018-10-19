﻿using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WB.Services.Export.CsvExport;
using WB.Services.Export.CsvExport.Exporters;
using WB.Services.Export.CsvExport.Implementation.DoFiles;
using WB.Services.Export.Ddi;
using WB.Services.Export.Ddi.Implementation;
using WB.Services.Export.DescriptionGenerator;
using WB.Services.Export.ExportProcessHandlers;
using WB.Services.Export.ExportProcessHandlers.Externals;
using WB.Services.Export.ExportProcessHandlers.Implementation;
using WB.Services.Export.Infrastructure;
using WB.Services.Export.Infrastructure.Implementation;
using WB.Services.Export.Interview;
using WB.Services.Export.Jobs;
using WB.Services.Export.Questionnaire;
using WB.Services.Export.Questionnaire.Services;
using WB.Services.Export.Questionnaire.Services.Implementation;
using WB.Services.Export.Services;
using WB.Services.Export.Services.Implementation;
using WB.Services.Export.Services.Processing;
using WB.Services.Export.Storage;
using WB.Services.Export.Utils;
using WB.Services.Infrastructure.FileSystem;

[assembly:InternalsVisibleTo("WB.Services.Export.Tests")]

namespace WB.Services.Export
{
    public class ServicesRegistry
    {
        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();

            // Transients
            services.AddTransient<IFileSystemAccessor, FileSystemAccessor>();
            services.AddTransient<ICsvWriter, CsvWriter>();
            services.AddTransient<ITabularFormatExportService, CsvExport.Implementation.TabularFormatExportService>();
            services.AddTransient<IProductVersion, ProductVersion>();
            services.AddTransient<ICommentsExporter, CommentsExporter>();
            services.AddTransient<IQuestionnaireExportStructureFactory, QuestionnaireExportStructureFactory>();
            services.AddTransient<IDiagnosticsExporter, DiagnosticsExporter>();
            services.AddTransient<IQuestionnaireStorage, QuestionnaireStorage>();
            services.AddTransient<IInterviewActionsExporter, InterviewActionsExporter>();
            services.AddTransient<IInterviewsExporter, InterviewsExporter>();
            services.AddTransient<IInterviewFactory, InterviewFactory>();
            services.AddTransient<IInterviewErrorsExporter, InterviewErrorsExporter>();
            services.AddTransient<IExportQuestionService, ExportQuestionService>();
            services.AddTransient<IDescriptionGenerator, DescriptionGenerator.DescriptionGenerator>();
            services.AddTransient<IEnvironmentContentService, StataEnvironmentContentService>();
            services.AddTransient<IFileBasedExportedDataAccessor, FileBasedExportedDataAccessor>();
            services.AddTransient<IDataExportFileAccessor, DataExportFileAccessor>();
            services.AddTransient<IQuestionnaireLabelFactory, QuestionnaireLabelFactory>();
            services.AddTransient<IExportFileNameService, ExportExportFileNameService>();
            services.AddTransient<IArchiveUtils, ZipArchiveUtils>();
            services.AddTransient<IExternalFileStorage, S3FileStorage>();
            services
                .AddTransient<ITabularDataToExternalStatPackageExportService,
                    TabularDataToExternalStatPackageExportService>();
            services.AddTransient<ITabFileReader, TabFileReader>();
            services.AddTransient<IDatasetWriterFactory, DatasetWriterFactory>();
            services.AddTransient<IDataQueryFactory, DataQueryFactory>();
            services.AddTransient<IExportServiceDataProvider, ExportServiceDataProvider>();
            services.AddTransient<IBinaryDataSource, BinaryDataSource>();
            services.AddTransient<IJobsStatusReporting, JobsStatusReporting>();
            services.AddTransient<IExportArchiveHandleService, ExportArchiveHandleService>();
            services.AddTransient<IDdiMetadataAccessor, DdiMetadataAccessor>();
            services.AddTransient<IDdiMetadataFactory, DdiMetadataFactory>();
            services.AddTransient<IMetadataWriter, MetadataWriter>();
            services.AddTransient<IMetaDescriptionFactory, MetaDescriptionFactory>();
            services.AddTransient<IExportJob, ExportJob>();

            // Singletons
            RegisterHandlers(services);

            
            FileStorageModule.Register(services, configuration);

            // options
            services.Configure<InterviewDataExportSettings>(configuration.GetSection("ExportSettings"));
        }

        private static void RegisterHandlers(IServiceCollection services)
        {
            services.AddTransient<BinaryFormatDataExportHandler>();
            services.AddTransient<TabularFormatParaDataExportProcessHandler>();
            services.AddTransient<TabularFormatDataExportHandler>();
            services.AddTransient<SpssFormatExportHandler>();
            services.AddTransient<StataFormatExportHandler>();
            services.AddTransient<OnedriveBinaryDataExportHandler>();
            services.AddTransient<DropboxBinaryDataExportHandler>();
            services.AddTransient<GoogleDriveBinaryDataExportHandler>();
        }
    }
}
