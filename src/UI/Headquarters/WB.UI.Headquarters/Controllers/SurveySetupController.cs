﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Parser;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Repositories;
using WB.Core.BoundedContexts.Headquarters.Resources;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Services.Preloading;
using WB.Core.BoundedContexts.Headquarters.UserPreloading.Services;
using WB.Core.BoundedContexts.Headquarters.UserPreloading.Tasks;
using WB.Core.BoundedContexts.Headquarters.Views.InterviewHistory;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.SurveyManagement.Web.Filters;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.UI.Headquarters.Code;
using WB.UI.Headquarters.Filters;
using WB.UI.Headquarters.Models;
using WB.UI.Headquarters.Resources;

namespace WB.UI.Headquarters.Controllers
{
    [LimitsFilter]
    [AuthorizeOr403(Roles = "Administrator, Headquarter")]
    public class SurveySetupController : BaseController
    {
        private readonly IPreloadingTemplateService preloadingTemplateService;
        private readonly ISampleUploadViewFactory sampleUploadViewFactory;
        private readonly InterviewDataExportSettings interviewDataExportSettings;
        private readonly IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory;
        private readonly IFileSystemAccessor fileSystemAccessor;
        private readonly IPreloadedDataServiceFactory preloadedDataServiceFactory;
        private readonly IQuestionnaireExportStructureStorage questionnaireExportStructureStorage;
        private readonly IRosterStructureService rosterStructureService;
        private readonly IQuestionnaireStorage questionnaireStorage;
        private readonly IAssignmentsImportService assignmentsImportService;
        private readonly AssignmentsImportTask assignmentsImportTask;
        private readonly AssignmentsVerificationTask assignmentsVerificationTask;
        private readonly IAssignmentsImportReader assignmentsImportReader;

        public SurveySetupController(
            ICommandService commandService,
            ILogger logger,
            IPreloadingTemplateService preloadingTemplateService,
            IPreloadedDataRepository preloadedDataRepository,
            ISampleUploadViewFactory sampleUploadViewFactory,
            InterviewDataExportSettings interviewDataExportSettings,
            IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory,
            IInterviewImportService interviewImportService,
            IFileSystemAccessor fileSystemAccessor,
            IPreloadedDataServiceFactory preloadedDataServiceFactory,
            IQuestionnaireExportStructureStorage questionnaireExportStructureStorage,
            IRosterStructureService rosterStructureService,
            IQuestionnaireStorage questionnaireStorage,
            IAssignmentsImportService assignmentsImportService,
            AssignmentsImportTask assignmentsImportTask,
            AssignmentsVerificationTask assignmentsVerificationTask,
            IAssignmentsImportReader assignmentsImportReader)
            : base(commandService, logger)
        {
            this.preloadingTemplateService = preloadingTemplateService;
            this.interviewDataExportSettings = interviewDataExportSettings;
            this.questionnaireBrowseViewFactory = questionnaireBrowseViewFactory;
            this.fileSystemAccessor = fileSystemAccessor;
            this.preloadedDataServiceFactory = preloadedDataServiceFactory;
            this.questionnaireExportStructureStorage = questionnaireExportStructureStorage;
            this.rosterStructureService = rosterStructureService;
            this.questionnaireStorage = questionnaireStorage;
            this.assignmentsImportService = assignmentsImportService;
            this.assignmentsImportTask = assignmentsImportTask;
            this.assignmentsVerificationTask = assignmentsVerificationTask;
            this.assignmentsImportReader = assignmentsImportReader;
            this.sampleUploadViewFactory = sampleUploadViewFactory;
        }

        public ActionResult Index()
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;
            this.ViewBag.EnableInterviewHistory = this.interviewDataExportSettings.EnableInterviewHistory;
            return this.View();
        }

        public ActionResult BatchUpload(Guid id, long version)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            var status = this.assignmentsImportService.GetImportStatus();

            var assignmentsPageToRedirect = this.GetImportAssignmentsPageToRedirect(status, nameof(BatchUpload));
            if (assignmentsPageToRedirect != null) return assignmentsPageToRedirect;

            var featuredQuestionItems = this.sampleUploadViewFactory.Load(new SampleUploadViewInputModel(id, version)).ColumnListToPreload;
            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(new QuestionnaireIdentity(id, version));

            var viewModel = new BatchUploadModel
            {
                QuestionnaireId = id,
                QuestionnaireVersion = version,
                QuestionnaireTitle = questionnaireInfo?.Title,
                FeaturedQuestions = featuredQuestionItems
            };

            return this.View(viewModel);
        }

        public ActionResult TemplateDownload(Guid id, long version)
        {
            var pathToFile = this.preloadingTemplateService.GetFilePathToPreloadingTemplate(id, version);
            return this.File(this.fileSystemAccessor.ReadFile(pathToFile), "application/zip", fileDownloadName: this.fileSystemAccessor.GetFileName(pathToFile));
        }

        public ActionResult SimpleTemplateDownload(Guid id, long version)
        {
            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(new QuestionnaireIdentity(id, version));
            if (questionnaireInfo == null || questionnaireInfo.IsDeleted)
                return this.HttpNotFound();

            string fileName = this.fileSystemAccessor.MakeValidFileName(questionnaireInfo.Title + ".tab");
            byte[] templateFile = this.preloadingTemplateService.GetPrefilledPreloadingTemplateFile(id, version);
            return this.File(templateFile, "text/tab-separated-values", fileDownloadName: fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ObserverNotAllowed]
        public ActionResult PanelBatchUploadAndVerify(BatchUploadModel model)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            if (!this.ModelState.IsValid)
                return this.RedirectToAction(nameof(BatchUpload), new { id = model.QuestionnaireId, version = model.QuestionnaireVersion });

            var status = this.assignmentsImportService.GetImportStatus();

            var assignmentsPageToRedirect = this.GetImportAssignmentsPageToRedirect(status, nameof(PanelBatchUploadAndVerify));
            if (assignmentsPageToRedirect != null) return assignmentsPageToRedirect;

            var questionnaireIdentity = new QuestionnaireIdentity(model.QuestionnaireId, model.QuestionnaireVersion);
            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);

            if (questionnaireInfo.IsDeleted)
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        global::Resources.BatchUpload.Prerequisite_Questionnaire,
                        AssignmentImportType.Panel,
                        model.File.FileName));
            }

            if (@".zip" != this.fileSystemAccessor.GetFileExtension(model.File.FileName).ToLower())
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        global::Resources.BatchUpload.Prerequisite_ZipFile,
                        AssignmentImportType.Panel,
                        model.File.FileName));
            }

            PreloadedFile[] allImportedFiles = null;
            try
            {
                allImportedFiles = this.assignmentsImportReader.ReadZipFile(model.File.InputStream).ToArray();
            }
            catch (ZipException)
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        PreloadingVerificationMessages.ArchiveWithPasswordNotSupported,
                        AssignmentImportType.Panel,
                        model.File.FileName));
            }

            if (allImportedFiles == null || !allImportedFiles.Any())
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        PreloadingVerificationMessages.PL0024_DataWasNotFound,
                        AssignmentImportType.Panel,
                        model.File.FileName));
            }

            try
            {
                var questionnaire = this.questionnaireStorage.GetQuestionnaire(questionnaireIdentity, null);
                var errors = this.assignmentsImportService
                    .VerifyPanel(model.File.FileName, allImportedFiles, questionnaire).Take(10).ToArray();

                if (errors.Any())
                {
                    return this.View("InterviewImportVerificationErrors",
                        new ImportDataParsingErrorsView(
                            model.QuestionnaireId,
                            model.QuestionnaireVersion,
                            questionnaireInfo?.Title,
                            errors,
                            new InterviewImportError[0],
                            false,
                            AssignmentImportType.Panel,
                            model.File.FileName));
                }

                this.assignmentsVerificationTask.Run(3);
            }
            catch (Exception e)
            {
                this.Logger.Error(@"Import panel assignments error", e);

                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        Pages.GlobalSettings_UnhandledExceptionMessage,
                        AssignmentImportType.Panel,
                        model.File.FileName));
            }

            return this.RedirectToAction("InterviewVerificationProgress");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ObserverNotAllowed]
        public ActionResult AssignmentsBatchUploadAndVerify(BatchUploadModel model)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            if (!this.ModelState.IsValid)
                return this.RedirectToAction(nameof(BatchUpload), new { id = model.QuestionnaireId, version = model.QuestionnaireVersion });

            var status = this.assignmentsImportService.GetImportStatus();

            var assignmentsPageToRedirect = this.GetImportAssignmentsPageToRedirect(status, nameof(AssignmentsBatchUploadAndVerify));
            if (assignmentsPageToRedirect != null) return assignmentsPageToRedirect;

            var questionnaireIdentity = new QuestionnaireIdentity(model.QuestionnaireId, model.QuestionnaireVersion);
            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);
            if (questionnaireInfo.IsDeleted)
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        global::Resources.BatchUpload.Prerequisite_Questionnaire,
                        AssignmentImportType.Assignments,
                        model.File.FileName));
            }

            var extension = this.fileSystemAccessor.GetFileExtension(model.File.FileName).ToLower();
            if (!new[] {@".tab", @".txt", @".zip"}.Contains(extension))
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        global::Resources.BatchUpload.Prerequisite_TabOrTxtFile,
                        AssignmentImportType.Assignments,
                        model.File.FileName));
            }
            
            PreloadedFile preloadedSample = null;
            
            if (new[] { @".tab", @".txt" }.Contains(extension))
                preloadedSample = this.assignmentsImportReader.ReadTextFile(model.File.InputStream, model.File.FileName);

            if (@".zip" == extension)
            {
                var questionnaireFileName = this.fileSystemAccessor.MakeStataCompatibleFileName(questionnaireInfo.Title);

                try
                {
                    var preloadedFiles = this.assignmentsImportReader.ReadZipFile(model.File.InputStream);

                    preloadedSample =
                        preloadedFiles.FirstOrDefault(x => x.FileInfo.QuestionnaireOrRosterName == questionnaireFileName) ??
                        preloadedFiles.FirstOrDefault();
                }
                catch (ZipException)
                {
                    return this.View("InterviewImportVerificationErrors",
                        ImportDataParsingErrorsView.CreatePrerequisiteError(
                            model.QuestionnaireId,
                            model.QuestionnaireVersion,
                            questionnaireInfo?.Title,
                            PreloadingVerificationMessages.ArchiveWithPasswordNotSupported,
                            AssignmentImportType.Assignments,
                            model.File.FileName));
                }
            }

            if (preloadedSample == null || preloadedSample.Rows.Length == 0)
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        PreloadingVerificationMessages.PL0024_DataWasNotFound,
                        AssignmentImportType.Assignments,
                        model.File.FileName));
            }
            
            try
            {
                var questionnaire = this.questionnaireStorage.GetQuestionnaire(questionnaireIdentity, null);
                var errors = this.assignmentsImportService.VerifySimple(preloadedSample, questionnaire).Take(10).ToArray();
                if (errors.Any())
                {
                    return this.View("InterviewImportVerificationErrors",
                        new ImportDataParsingErrorsView(
                            model.QuestionnaireId,
                            model.QuestionnaireVersion,
                            questionnaireInfo?.Title,
                            errors,
                            new InterviewImportError[0],
                            false,
                            AssignmentImportType.Panel,
                            model.File.FileName));
                }
            }
            catch (Exception e)
            {
                this.Logger.Error(@"Import assignments error", e);

                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        Pages.GlobalSettings_UnhandledExceptionMessage,
                        AssignmentImportType.Assignments,
                        model.File.FileName));
            }

            return this.RedirectToAction("InterviewImportConfirmation");
        }

        private IPreloadedDataService CreatePreloadedDataService(QuestionnaireIdentity questionnaireIdentity)
        {
            var questionnaire = this.questionnaireStorage.GetQuestionnaireDocument(questionnaireIdentity);
            var questionnaireExportStructure = this.questionnaireExportStructureStorage.GetQuestionnaireExportStructure(questionnaireIdentity);
            var questionnaireRosterStructure = this.rosterStructureService.GetRosterScopes(questionnaire);

            return questionnaireExportStructure == null || questionnaireRosterStructure == null || questionnaire == null
                ? null
                : this.preloadedDataServiceFactory.CreatePreloadedDataService(questionnaireExportStructure, questionnaireRosterStructure, questionnaire);
        }

        [HttpGet]
        public ActionResult InterviewImportConfirmation()
        {
            var status = this.assignmentsImportService.GetImportStatus();
            if (status == null) return RedirectToAction("Index");

            var assignmentsPageToRedirect = this.GetImportAssignmentsPageToRedirect(status, nameof(InterviewImportConfirmation));
            if (assignmentsPageToRedirect != null) return assignmentsPageToRedirect;

            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(status.QuestionnaireIdentity);

            return this.View(new PreloadedDataConfirmationModel
            {
                QuestionnaireId = status.QuestionnaireIdentity.QuestionnaireId,
                Version = status.QuestionnaireIdentity.Version,
                QuestionnaireTitle = questionnaireInfo?.Title,
                FileName = status.FileName,
                EntitiesCount = status.TotalAssignments,
                AssignmentImportType = AssignmentImportType.Assignments,
                WasResponsibleProvided = status.AssignedToInterviewersCount + status.AssignedToSupervisorsCount > 0,
                EnumeratorsCount = status.AssignedToInterviewersCount,
                SupervisorsCount = status.AssignedToSupervisorsCount
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ObserverNotAllowed]
        public ActionResult InterviewImportConfirmation(PreloadedDataConfirmationModel model)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            var status = this.assignmentsImportService.GetImportStatus();
            if (status == null) return RedirectToAction("Index");

            var assignmentsPageToRedirect = this.GetImportAssignmentsPageToRedirect(status, nameof(InterviewImportConfirmation));
            if (assignmentsPageToRedirect != null) return assignmentsPageToRedirect;

            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(status.QuestionnaireIdentity);
            if (questionnaireInfo.IsDeleted)
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.Version,
                        questionnaireInfo?.Title,
                        global::Resources.BatchUpload.Prerequisite_Questionnaire,
                        model.AssignmentImportType,
                        null));
            }

            if (!this.ModelState.IsValid)
            {
                model.QuestionnaireTitle = questionnaireInfo.Title;
                return this.View(model);
            }

            if (!model.WasResponsibleProvided && model.ResponsibleId.HasValue)
                this.assignmentsImportService.SetResponsibleToAllImportedAssignments(model.ResponsibleId.Value);

            assignmentsImportTask.Run();

            return this.RedirectToAction("InterviewImportProgress");
        }

        [ObserverNotAllowed]
        public ActionResult InterviewVerificationProgress()
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            var status = this.assignmentsImportService.GetImportStatus();
            if (status == null) return RedirectToAction("Index");

            var assignmentsPageToRedirect = this.GetImportAssignmentsPageToRedirect(status, nameof(InterviewVerificationProgress));
            if (assignmentsPageToRedirect != null) return assignmentsPageToRedirect;

            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(status.QuestionnaireIdentity);

            return this.View(new PreloadedDataInterviewProgressModel
            {
                Status = status,
                QuestionnaireId = questionnaireInfo.QuestionnaireId,
                Version = questionnaireInfo.Version,
                QuestionnaireTitle = questionnaireInfo.Title,
            });
        }

        [ObserverNotAllowed]
        public ActionResult InterviewImportProgress()
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;
            
            var status = this.assignmentsImportService.GetImportStatus();
            if (status == null) return RedirectToAction("Index");

            var assignmentsPageToRedirect = this.GetImportAssignmentsPageToRedirect(status, nameof(InterviewImportProgress));
            if (assignmentsPageToRedirect != null) return assignmentsPageToRedirect;

            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(status.QuestionnaireIdentity);

            return this.View(new PreloadedDataInterviewProgressModel
            {
                Status = status,
                QuestionnaireId = questionnaireInfo.QuestionnaireId,
                Version = questionnaireInfo.Version,
                QuestionnaireTitle = questionnaireInfo.Title,
            });
        }

        [ObserverNotAllowed]
        public ActionResult CancelImportAssignments(Guid id, long version)
        {
            this.assignmentsImportService.RemoveAllAssignmentsToImport();
            return this.RedirectToAction(nameof(BatchUpload), new {id, version});
        }

        [Localizable(false)]
        private ActionResult GetImportAssignmentsPageToRedirect(AssignmentsImportStatus status, string actionName)
        {
            if (status == null) return null;

            var importAssignmentsPages = new (string actionName, Func<bool> shouldRedirect)[]
            {
                (nameof(InterviewImportProgress), () => IsAssignmentsImportIsInProgress(status)),
                (nameof(InterviewVerificationProgress), () => IsAssignmentsVerifying(status)),
                (nameof(InterviewImportConfirmation), () => NoResponsibleForVerifiedAssignments(status)),
            };

            foreach (var importAssignmentsPage in importAssignmentsPages)
            {
                if (importAssignmentsPage.shouldRedirect.Invoke() && importAssignmentsPage.actionName != actionName)
                    return RedirectToAction(importAssignmentsPage.actionName);
            }

            return null;
        }

        private static bool NoResponsibleForVerifiedAssignments(AssignmentsImportStatus status)
            => status.InQueueCount == status.TotalAssignments &&
               status.VerifiedAssignments == status.TotalAssignments &&
               status.AssignedToInterviewersCount + status.AssignedToSupervisorsCount == 0;

        private static bool IsAssignmentsVerifying(AssignmentsImportStatus status)
            => status.InQueueCount == status.TotalAssignments && status.VerifiedAssignments < status.TotalAssignments;

        private static bool IsAssignmentsImportIsInProgress(AssignmentsImportStatus status)
            => status.InQueueCount < status.TotalAssignments && status.InQueueCount > status.AssingmentsWithErrors;
    }
}
