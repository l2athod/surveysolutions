﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Main.Core.Documents;
using Polly;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport;
using WB.Core.BoundedContexts.Headquarters.Commands;
using WB.Core.BoundedContexts.Headquarters.Designer;
using WB.Core.BoundedContexts.Headquarters.Resources;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Implementation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernel.Structures.Synchronization.Designer;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.Questionnaire.Synchronization.Designer;
using WB.Enumerator.Native.Questionnaire;
using WB.Enumerator.Native.WebInterview;
using WB.Infrastructure.Native.Questionnaire;
using WB.Infrastructure.Native.Storage.Postgre;

namespace WB.Core.BoundedContexts.Headquarters.Implementation.Services
{
    internal class QuestionnaireImportService : IQuestionnaireImportService
    {
        private readonly IStringCompressor zipUtils;
        private readonly IAssignmentsUpgradeService upgradeService;
        private readonly IDesignerUserCredentials designerUserCredentials;
        private readonly IPlainKeyValueStorage<QuestionnairePdf> pdfStorage;
        private readonly IQuestionnaireVersionProvider questionnaireVersionProvider;
        private readonly ILogger logger;
        private readonly ISystemLog auditLog;
        private readonly IUnitOfWork unitOfWork;
        private readonly IAuthorizedUser authorizedUser;
        private readonly IDesignerApi designerApi;

        public QuestionnaireImportService(
            IStringCompressor zipUtils,
            IQuestionnaireVersionProvider questionnaireVersionProvider,
            ILogger logger,
            ISystemLog auditLog,
            IUnitOfWork unitOfWork,
            IAuthorizedUser authorizedUser,
            IDesignerApi designerApi,
            IPlainKeyValueStorage<QuestionnairePdf> pdfStorage,
            IAssignmentsUpgradeService upgradeService,
            IDesignerUserCredentials designerUserCredentials)
        {
            this.zipUtils = zipUtils;
            this.questionnaireVersionProvider = questionnaireVersionProvider;
            this.logger = logger;
            this.auditLog = auditLog;
            this.unitOfWork = unitOfWork;
            this.authorizedUser = authorizedUser;
            this.designerApi = designerApi;
            this.pdfStorage = pdfStorage;
            this.upgradeService = upgradeService;
            this.designerUserCredentials = designerUserCredentials;
        }

        private static Dictionary<QuestionnaireIdentity, QuestionnaireImportResult> statuses = new Dictionary<QuestionnaireIdentity, QuestionnaireImportResult>();

        public QuestionnaireImportResult GetStatus(QuestionnaireIdentity questionnaireId)
        {
            if (statuses.TryGetValue(questionnaireId, out QuestionnaireImportResult status))
                return status;
            return null;
        }

        public Task<QuestionnaireImportResult> Import(Guid questionnaireId, string name, bool isCensusMode,
            string comment, string requestUrl, bool includePdf = true)
        {
            return ImportAndMigrateAssignments(questionnaireId, name, isCensusMode, comment, requestUrl, includePdf,
                false, null);
        }


        public async Task<QuestionnaireImportResult> ImportAndMigrateAssignments(Guid questionnaireId, string name,
            bool isCensusMode,
            string comment, string requestUrl, bool includePdf, bool shouldMigrateAssignments, QuestionnaireIdentity migrateFrom)
        {
            // prevent 2 concurrent requests from importing
            var query = this.unitOfWork.Session.CreateSQLQuery("select pg_advisory_xact_lock(51658156)");
            await query.ExecuteUpdateAsync();

            try
            {
                await this.designerApi.IsLoggedIn();
            }
            catch
            {
                return new QuestionnaireImportResult
                {
                    ImportError = ErrorMessages.IncorrectUserNameOrPassword
                };
            }

            var questionnaireVersion = this.questionnaireVersionProvider.GetNextVersion(questionnaireId);
            var questionnaireIdentity = new QuestionnaireIdentity(questionnaireId, questionnaireVersion);
            var questionnaireImportResult = new QuestionnaireImportResult
            {
                Identity = questionnaireIdentity,
                Percent = 0,
                Status = QuestionnaireImportStatus.Progress
            };
            statuses[questionnaireIdentity] = questionnaireImportResult;
            var designerCredentials = designerUserCredentials.Get();

            _ = Task.Run(async () =>
            {
                var result = await ImportImpl(designerCredentials, questionnaireImportResult, name, isCensusMode, comment, requestUrl, includePdf);
                if (shouldMigrateAssignments && migrateFrom != null)
                {
                    var sourceQuestionnaireId = migrateFrom;

                    var processId = Guid.NewGuid();
                    this.upgradeService.EnqueueUpgrade(processId, authorizedUser.Id, sourceQuestionnaireId, result.Identity);
                    result.MigrateAssignmentProcessId = processId;
                    result.Status = QuestionnaireImportStatus.MigrateAssignments;
                }
            });

            return questionnaireImportResult;
        }

        private Task<QuestionnaireImportResult> ImportImpl(RestCredentials designerCredentials, QuestionnaireImportResult questionnaireImportResult, string name, bool isCensusMode,
            string comment, string requestUrl, bool includePdf = true)
        {
            return InScopeExecutor.Current.ExecuteAsync(async (serviceLocatorLocal) =>
            {
                var userCredentials = serviceLocatorLocal.GetInstance<IDesignerUserCredentials>();
                userCredentials.Set(designerCredentials);

                var questionnaireIdentity = questionnaireImportResult.Identity;
                var questionnaireId = questionnaireIdentity.QuestionnaireId;

                bool shouldRollback = true;
                try
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    this.logger.Trace($"IMPORT!!! Trigger pdf render {stopwatch.Elapsed}");

                    await TriggerPdfRendering(questionnaireImportResult.Identity.QuestionnaireId, includePdf);
                    questionnaireImportResult.Percent = 5;

                    var supportedVersionProvider = serviceLocatorLocal.GetInstance<ISupportedVersionProvider>();
                    var minSupported = supportedVersionProvider.GetMinVerstionSupportedByInterviewer();
                    var supportedVersion = supportedVersionProvider.GetSupportedQuestionnaireVersion();
                    var questionnairePackage = await this.designerApi.GetQuestionnaire(questionnaireImportResult.Identity.QuestionnaireId, supportedVersion, minSupported);
                    QuestionnaireDocument questionnaire = this.zipUtils.DecompressString<QuestionnaireDocument>(questionnairePackage.Questionnaire);

                    this.logger.Debug($"IMPORT!!! Downloaded questionnaire {stopwatch.Elapsed}");

                    questionnaireImportResult.Percent = 20;

                    await TriggerPdfTranslationsRendering(questionnaire, includePdf);
                    questionnaireImportResult.Percent = 25;
                    this.logger.Trace($"IMPORT!!! TriggerPdfTranslationsRendering {stopwatch.Elapsed}");

                    if (questionnaire.Attachments != null)
                    {
                        var attachmentContentService = serviceLocatorLocal.GetInstance<IAttachmentContentService>();

                        foreach (var questionnaireAttachment in questionnaire.Attachments)
                        {
                            if (attachmentContentService.HasAttachmentContent(questionnaireAttachment.ContentId))
                                continue;

                            var attachmentContent = await this.designerApi.DownloadQuestionnaireAttachment(
                                questionnaireAttachment.ContentId, questionnaireAttachment.AttachmentId);

                            attachmentContentService.SaveAttachmentContent(
                                questionnaireAttachment.ContentId,
                                attachmentContent.ContentType,
                                attachmentContent.FileName,
                                attachmentContent.Content);
                        }
                    }
                    questionnaireImportResult.Percent = 35;

                    this.logger.Trace($"IMPORT!!! Attachments {stopwatch.Elapsed}");

                    this.logger.Debug($"checking translations questionnaire {questionnaireId}");
                    if (questionnaire.Translations?.Count > 0)
                    {
                        var translationManagementService = serviceLocatorLocal.GetInstance<ITranslationManagementService>();
                        translationManagementService.Delete(questionnaireIdentity);

                        this.logger.Debug($"loading translations {questionnaireId}");
                        var translationContent = await this.designerApi.GetTranslations(questionnaire.PublicKey);

                        translationManagementService.Store(translationContent.Select(x => new TranslationInstance
                        {
                            QuestionnaireId = questionnaireIdentity,
                            Value = x.Value,
                            QuestionnaireEntityId = x.QuestionnaireEntityId,
                            Type = x.Type,
                            TranslationIndex = x.TranslationIndex,
                            TranslationId = x.TranslationId
                        }));
                    }
                    questionnaireImportResult.Percent = 45;
                    this.logger.Trace($"IMPORT!!! translations {stopwatch.Elapsed}");

                    this.logger.Debug($"checking lookup tables questionnaire {questionnaireId}");
                    if (questionnaire.LookupTables.Any())
                    {
                        var lookupTablesStorage = serviceLocatorLocal.GetInstance<IPlainKeyValueStorage<QuestionnaireLookupTable>>();
                        foreach (var lookupId in questionnaire.LookupTables.Keys)
                        {
                            this.logger.Debug($"Loading lookup table questionnaire {questionnaireId}. Lookup id {lookupId}");
                            var lookupTable = await this.designerApi.GetLookupTables(questionnaire.PublicKey, lookupId);

                            lookupTablesStorage.Store(lookupTable, questionnaireIdentity, lookupId);
                        }
                    }
                    questionnaireImportResult.Percent = 55;
                    this.logger.Trace($"IMPORT!!! LookupTables {stopwatch.Elapsed}");


                    this.logger.Debug($"checking reusable categories for questionnaire {questionnaireId}");
                    if (questionnaire.Categories.Any())
                    {
                        var reusableCategoriesStorage = serviceLocatorLocal.GetInstance<IReusableCategoriesStorage>();

                        foreach (var category in questionnaire.Categories)
                        {
                            this.logger.Debug($"Loading reusable category for questionnaire {questionnaireId}. Category id {category.Id}");
                            var reusableCategories = await this.designerApi.GetReusableCategories(questionnaire.PublicKey, category.Id);
                            reusableCategoriesStorage.Store(questionnaireIdentity, category.Id, reusableCategories);
                        }
                    }
                    questionnaireImportResult.Percent = 65;
                    this.logger.Trace($"IMPORT!!! Categories {stopwatch.Elapsed}");


                    logger.Verbose($"commandService.Execute.new ImportFromDesigner: {questionnaire.Title}({questionnaire.PublicKey} rev.{questionnaire.Revision})");

                    var questionnaireContentVersion = questionnairePackage.QuestionnaireContentVersion;
                    var questionnaireAssembly = questionnairePackage.QuestionnaireAssembly;

                    var commandService = serviceLocatorLocal.GetInstance<ICommandService>();

                    commandService.Execute(new ImportFromDesigner(
                        this.authorizedUser.Id,
                        questionnaire,
                        isCensusMode,
                        questionnaireAssembly,
                        questionnaireContentVersion,
                        questionnaireIdentity.Version,
                        comment));
                    this.logger.Trace($"IMPORT!!! commandService {stopwatch.Elapsed}");
                    questionnaireImportResult.Percent = 75;


                    logger.Verbose($"UpdateRevisionMetadata: {questionnaire.Title}({questionnaire.PublicKey} rev.{questionnaire.Revision})");
                    await designerApi.UpdateRevisionMetadata(questionnaire.PublicKey, questionnaire.Revision,
                        new QuestionnaireRevisionMetadataModel
                        {
                            HqHost = GetDomainFromUri(requestUrl),
                            HqTimeZoneMinutesOffset =
                                (int)TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes,
                            HqImporterLogin = this.authorizedUser.UserName,
                            HqQuestionnaireVersion = questionnaireIdentity.Version,
                            Comment = comment,
                        });
                    questionnaireImportResult.Percent = 80;
                    this.logger.Trace($"IMPORT!!! UpdateRevisionMetadata {stopwatch.Elapsed}");


                    logger.Verbose($"DownloadAndStorePdf: {questionnaire.Title}({questionnaire.PublicKey} rev.{questionnaire.Revision})");

                    await DownloadAndStorePdf(questionnaireIdentity, questionnaire, includePdf);
                    questionnaireImportResult.Percent = 95;
                    this.logger.Trace($"IMPORT!!! DownloadAndStorePdf {stopwatch.Elapsed}");

                    this.auditLog.QuestionnaireImported(questionnaire.Title, questionnaireIdentity);
                    this.logger.Trace($"IMPORT!!! QuestionnaireImported {stopwatch.Elapsed}");
                    stopwatch.Stop();
                    questionnaireImportResult.Percent = 100;
                    questionnaireImportResult.Status = QuestionnaireImportStatus.Finished;

                    shouldRollback = false;
                    return questionnaireImportResult;
                }
                catch (RestException ex)
                {
                    questionnaireImportResult.Status = QuestionnaireImportStatus.Error;

                    switch (ex.StatusCode)
                    {
                        case HttpStatusCode.Unauthorized:
                        case HttpStatusCode.Forbidden:
                        case HttpStatusCode.UpgradeRequired:
                        case HttpStatusCode.ExpectationFailed:
                            questionnaireImportResult.ImportError = ex.Message;
                            break;
                        case HttpStatusCode.PreconditionFailed:
                            questionnaireImportResult.ImportError =
                                string.Format(ErrorMessages.Questionnaire_verification_failed, name);
                            break;
                        case HttpStatusCode.NotFound:
                            questionnaireImportResult.ImportError = string.Format(ErrorMessages.TemplateNotFound, name);
                            break;
                        case HttpStatusCode.ServiceUnavailable:
                            questionnaireImportResult.ImportError = ErrorMessages.ServiceUnavailable;
                            break;
                        case HttpStatusCode.RequestTimeout:
                            questionnaireImportResult.ImportError = ErrorMessages.RequestTimeout;
                            break;
                        default:
                            questionnaireImportResult.ImportError = ErrorMessages.ServerError;
                            break;
                    }

                    return questionnaireImportResult;
                }
                catch (QuestionnaireAssemblyAlreadyExistsException ex)
                {
                    questionnaireImportResult.Status = QuestionnaireImportStatus.Error;
                    questionnaireImportResult.ImportError = ex.Message;
                    this.logger.Error("Failed to import questionnaire from designer", ex);

                    return questionnaireImportResult;
                }
                catch (Exception ex)
                {
                    var domainEx = ex.GetSelfOrInnerAs<QuestionnaireException>();
                    if (domainEx != null)
                    {
                        questionnaireImportResult.Status = QuestionnaireImportStatus.Error;
                        questionnaireImportResult.ImportError = domainEx.Message;
                        return questionnaireImportResult;
                    }

                    this.logger.Error($"Designer: error when importing template #{questionnaireId}", ex);

                    throw;
                }
                finally
                {
                    if (shouldRollback)
                    {
                        questionnaireImportResult.Status = QuestionnaireImportStatus.Error;
                        this.unitOfWork.DiscardChanges();
                    }
                }
            });
        }

        private async Task TriggerPdfRendering(Guid questionnaireId, bool includePdf)
        {
            if (includePdf)
                await this.designerApi.GetPdfStatus(questionnaireId);
        }

        private async Task TriggerPdfTranslationsRendering(QuestionnaireDocument questionnaire, bool includePdf)
        {
            if (!includePdf)
                return;

            this.logger.Error($"Requesting pdf generator to start working for questionnaire {questionnaire.PublicKey}");
                        
            foreach (var questionnaireTranslation in questionnaire.Translations)
            {
                await this.designerApi.GetPdfStatus(questionnaire.PublicKey, questionnaireTranslation.Id);
            }
        }

        private async Task DownloadAndStorePdf(QuestionnaireIdentity questionnaireIdentity,
            QuestionnaireDocument questionnaire, bool includePdf)
        {
            if (!includePdf)
                return;

            var pdfRetry = Policy
                .HandleResult<PdfStatus>(x => x.ReadyForDownload == false && x.CanRetry != true)
                .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));

            await pdfRetry.ExecuteAsync(async () =>
            {
                this.logger.Trace($"Waiting for pdf to be ready {questionnaireIdentity}");
                return await this.designerApi.GetPdfStatus(questionnaireIdentity.QuestionnaireId);     
            });

            this.logger.Error("Loading pdf for default language");

            var pdfFile = await this.designerApi.DownloadPdf(questionnaireIdentity.QuestionnaireId);

            this.pdfStorage.Store(new QuestionnairePdf { Content = pdfFile.Content }, questionnaireIdentity.ToString());

            this.logger.Error($"PDF for questionnaire stored {questionnaireIdentity}");

            foreach (var translation in questionnaire.Translations)
            {
                this.logger.Error($"loading pdf for translation {translation}");

                await pdfRetry.ExecuteAsync(async () =>
                {
                    this.logger.Trace($"Waiting for pdf to be ready {questionnaireIdentity}");

                    return await this.designerApi.GetPdfStatus(questionnaireIdentity.QuestionnaireId, translation.Id);
                });

                var pdfTranslated = await this.designerApi.DownloadPdf(questionnaireIdentity.QuestionnaireId, translation.Id);

                this.pdfStorage.Store(new QuestionnairePdf { Content = pdfTranslated.Content },
                    $"{translation.Id.FormatGuid()}_{questionnaireIdentity}");

                this.logger.Error($"PDF for questionnaire stored {questionnaireIdentity} translation {translation.Id}, {translation.Name}");
            }
        }

        private string GetDomainFromUri(string requestUrl)
        {
            if (requestUrl == null) return null;
            var uri = new Uri(requestUrl);
            return uri.Host + (!uri.IsDefaultPort ? ":" + uri.Port : "");
        }
    }
}
