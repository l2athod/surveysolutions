﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Main.Core.Entities.SubEntities;
using Main.Core.View;
using Ncqrs.Commanding.ServiceModel;
using Questionnaire.Core.Web.Helpers;
using WB.Core.GenericSubdomains.Logging;
using WB.Core.SharedKernels.DataCollection.Views.Questionnaire;
using WB.Core.SharedKernels.DataCollection.Views.Questionnaire.BrowseItem;
using WB.Core.SharedKernels.SurveyManagement.Implementation.SampleRecordsAccessors;
using WB.Core.SharedKernels.SurveyManagement.Services;
using WB.Core.SharedKernels.SurveyManagement.Views.Reposts.Views;
using WB.Core.SharedKernels.SurveyManagement.Views.SampleImport;
using WB.Core.SharedKernels.SurveyManagement.Views.Survey;
using WB.Core.SharedKernels.SurveyManagement.Views.TakeNew;
using WB.Core.SharedKernels.SurveyManagement.Views.User;
using WB.Core.SharedKernels.SurveyManagement.Views.UsersAndQuestionnaires;
using WB.UI.Headquarters.Models;

namespace WB.UI.Headquarters.Controllers
{
    [Authorize(Roles = "Headquarter")]
    public class HQController : BaseController
    {
        private readonly IViewFactory<AllUsersAndQuestionnairesInputModel, AllUsersAndQuestionnairesView> allUsersAndQuestionnairesFactory;
        private readonly IViewFactory<QuestionnaireBrowseInputModel, QuestionnaireBrowseView> questionnaireBrowseViewFactory;
        private readonly IViewFactory<QuestionnaireItemInputModel, QuestionnaireBrowseItem> questionnaireItemFactory;
        private readonly ISampleImportService sampleImportService;
        private readonly IViewFactory<UserListViewInputModel, UserListView> supervisorsFactory;
        private readonly IViewFactory<SurveyUsersViewInputModel, SurveyUsersView> surveyUsersViewFactory;
        private readonly IViewFactory<TakeNewInterviewInputModel, TakeNewInterviewView> takeNewInterviewViewFactory;
        private readonly IViewFactory<UserListViewInputModel, UserListView> userListViewFactory;

        public HQController(ICommandService commandService, IGlobalInfoProvider provider, ILogger logger,
                            IViewFactory<QuestionnaireBrowseInputModel, QuestionnaireBrowseView> questionnaireBrowseViewFactory,
                            IViewFactory<QuestionnaireItemInputModel, QuestionnaireBrowseItem> questionnaireItemFactory,
                            IViewFactory<UserListViewInputModel, UserListView> userListViewFactory,
                            IViewFactory<SurveyUsersViewInputModel, SurveyUsersView> surveyUsersViewFactory,
                            IViewFactory<TakeNewInterviewInputModel, TakeNewInterviewView> takeNewInterviewViewFactory,
                            IViewFactory<UserListViewInputModel, UserListView> supervisorsFactory,
                            ISampleImportService sampleImportService,
                            IViewFactory<AllUsersAndQuestionnairesInputModel, AllUsersAndQuestionnairesView>
                                allUsersAndQuestionnairesFactory)
            : base(commandService, provider, logger)
        {
            this.questionnaireBrowseViewFactory = questionnaireBrowseViewFactory;
            this.questionnaireItemFactory = questionnaireItemFactory;
            this.userListViewFactory = userListViewFactory;
            this.surveyUsersViewFactory = surveyUsersViewFactory;
            this.takeNewInterviewViewFactory = takeNewInterviewViewFactory;
            this.sampleImportService = sampleImportService;
            this.allUsersAndQuestionnairesFactory = allUsersAndQuestionnairesFactory;
            this.supervisorsFactory = supervisorsFactory;
        }

        public ActionResult Index()
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            var model = new HQDashboardModel
                {
                    Questionnaires = this.questionnaireBrowseViewFactory.Load(
                            new QuestionnaireBrowseInputModel() { PageSize = 1024 })
                };
            return this.View(model);
        }

        public ActionResult Interviews(Guid? questionnaireId)
        {
            if (questionnaireId.HasValue)
            {
                this.Success(
                    string.Format(
                        @"Interview was successfully created. <a class=""btn btn-success"" href=""{0}""><i class=""icon-plus""></i> Create one more?</a>",
                        this.Url.Action("TakeNew", "HQ", new { id = questionnaireId.Value })));
            }
            this.ViewBag.ActivePage = MenuItem.Docs;
            UserLight currentUser = this.GlobalInfo.GetCurrentUser();
            this.ViewBag.CurrentUser = new UsersViewItem { UserId = currentUser.Id, UserName = currentUser.Name };
            return this.View(this.Filters());
        }

        public ActionResult BatchUpload(Guid id)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            var questionnaireBrowseItem = this.questionnaireItemFactory.Load(new QuestionnaireItemInputModel(id));

            var viewModel = new BatchUploadModel()
            {
                FeaturedQuestions =  questionnaireBrowseItem.FeaturedQuestions,
                QuestionnaireId = questionnaireBrowseItem.QuestionnaireId,
                QuestionnaireTitle = questionnaireBrowseItem.Title
            };

            return this.View(viewModel);
        }

        public ActionResult SampleCreationResult(Guid id)
        {
            SampleCreationStatus result = this.sampleImportService.GetSampleCreationStatus(id);
            return this.View(result);
        }

        public JsonResult GetSampleCreationStatus(Guid id)
        {
            return this.Json(this.sampleImportService.GetSampleCreationStatus(id));
        }

        public ActionResult TakeNew(Guid id)
        {
            Guid key = id;
            UserLight user = this.GlobalInfo.GetCurrentUser();
            TakeNewInterviewView model = this.takeNewInterviewViewFactory.Load(new TakeNewInterviewInputModel(key, user.Id));
            return this.View(model);
        }

        public ActionResult SurveysAndStatuses()
        {
            this.ViewBag.ActivePage = MenuItem.Surveys;

            AllUsersAndQuestionnairesView usersAndQuestionnaires =
                this.allUsersAndQuestionnairesFactory.Load(new AllUsersAndQuestionnairesInputModel());

            return this.View(usersAndQuestionnaires.Users);
        }

        public ActionResult SupervisorsAndStatuses()
        {
            this.ViewBag.ActivePage = MenuItem.Summary;

            AllUsersAndQuestionnairesView usersAndQuestionnaires =
                this.allUsersAndQuestionnairesFactory.Load(new AllUsersAndQuestionnairesInputModel());

            return this.View(usersAndQuestionnaires.Questionnaires);
        }

        public ActionResult MapReport()
        {
            this.ViewBag.ActivePage = MenuItem.MapReport;

            return this.View();
        }

        public ActionResult Status()
        {
            this.ViewBag.ActivePage = MenuItem.Statuses;
            return this.View(StatusHelper.GetOnlyActualSurveyStatusViewItems());
        }

        private DocumentFilter Filters()
        {
            IEnumerable<SurveyStatusViewItem> statuses = StatusHelper.GetOnlyActualSurveyStatusViewItems();

            AllUsersAndQuestionnairesView usersAndQuestionnaires =
                this.allUsersAndQuestionnairesFactory.Load(new AllUsersAndQuestionnairesInputModel());

            return new DocumentFilter
                {
                    Users =
                        this.supervisorsFactory.Load(new UserListViewInputModel { PageSize = int.MaxValue })
                            .Items.Where(u => !u.IsLocked)
                            .Select(u => new UsersViewItem
                                {
                                    UserId = u.UserId,
                                    UserName = u.UserName
                                }),
                    Responsibles = usersAndQuestionnaires.Users,
                    Templates = usersAndQuestionnaires.Questionnaires,
                    Statuses = statuses
                };
        }
    }
}