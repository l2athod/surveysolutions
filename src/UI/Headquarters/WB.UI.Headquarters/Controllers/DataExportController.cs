﻿using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ASP;
using WB.Core.BoundedContexts.Headquarters.Views.DataExport;
using WB.Core.BoundedContexts.Headquarters.Views.InterviewHistory;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.Views;
using WB.Core.BoundedContexts.Headquarters.Views.UsersAndQuestionnaires;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.SurveyManagement.Web.Filters;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.Core.SharedKernels.SurveyManagement.Web.Utils;
using WB.UI.Headquarters.Code;
using WB.UI.Headquarters.Filters;
using WB.UI.Headquarters.Models;

namespace WB.UI.Headquarters.Controllers
{
    [LimitsFilter]
    [AuthorizeOr403(Roles = "Administrator, Headquarter")]
    public class DataExportController: BaseController
    {
        private readonly IAllUsersAndQuestionnairesFactory allUsersAndQuestionnairesFactory;
        private readonly InterviewDataExportSettings interviewDataExportSettings;
        private readonly ExternalStoragesSettings externalStoragesSettings;

        public DataExportController(ICommandService commandService, ILogger logger,
            IAllUsersAndQuestionnairesFactory allUsersAndQuestionnairesFactory, 
            InterviewDataExportSettings interviewDataExportSettings,
            ExternalStoragesSettings externalStoragesSettings)
            : base(commandService, logger)
        {
            this.allUsersAndQuestionnairesFactory = allUsersAndQuestionnairesFactory;
            this.interviewDataExportSettings = interviewDataExportSettings;
            this.externalStoragesSettings = externalStoragesSettings;
        }

        [ObserverNotAllowed]
        public ActionResult New()
        {
            this.ViewBag.ActivePage = MenuItem.DataExport;

            var statuses = new List<InterviewStatus?>
                {
                    InterviewStatus.InterviewerAssigned,
                    InterviewStatus.Completed,
                    InterviewStatus.ApprovedBySupervisor,
                    InterviewStatus.ApprovedByHeadquarters
                }
                .Select(item => new ComboboxViewItem{ Key = ((int)item.Value).ToString(), Value = item.ToUiString() })
                .ToArray();


            var export = new NewExportModel
            {
                Statuses = statuses,
                ExternalStoragesSettings = this.externalStoragesSettings is FakeExternalStoragesSettings
                    ? null
                    : this.externalStoragesSettings,
                Api = new
                {
                    HistoryUrl = Url.RouteUrl("DefaultApiWithAction", new {httproute = "", controller = "DataExportApi", action = "Paradata"}),
                    DDIUrl = Url.RouteUrl("DefaultApiWithAction", new {httproute = "", controller = "DataExportApi", action = "DDIMetadata"}),
                    ExportedDataReferencesForQuestionnaireUrl = Url.RouteUrl("DefaultApiWithAction", new {httproute = "", controller = "DataExportApi", action = "GetExportStatus"}),

                    
                    UpdateSurveyDataUrl = Url.RouteUrl("DefaultApiWithAction", new {httproute = "", controller = "DataExportApi", action = "RequestUpdate"}),
                    RegenerateSurveyDataUrl = Url.RouteUrl("DefaultApiWithAction", new {httproute = "", controller = "DataExportApi", action = "Regenerate"}),
                    QuestionnairesUrl = Url.RouteUrl("DefaultApiWithAction", new {httproute = "", controller = "QuestionnairesApi", action = "QuestionnairesWithVersions"}),
                    StatusUrl = Url.RouteUrl("DefaultApiWithAction", new {httproute = "", controller = "DataExportApi", action = "Status"}),
                    DataAvailabilityUrl = Url.RouteUrl("DefaultApiWithAction", new {httproute = "", controller = "DataExportApi", action = "DataAvailability"}),
                    WasExportFileRecreatedUrl = Url.RouteUrl("DefaultApiWithAction", new {httproute = "", controller = "DataExportApi", action = "WasExportFileRecreated"}),
                    DownloadDataUrl = Url.RouteUrl("DefaultApiWithAction", new {httproute = "", controller = "DataExportApi", action = "DownloadData"}),
                    ExportToExternalStorageUrl = Url.RouteUrl("DefaultApiWithAction", new {httproute = "", controller = "DataExportApi", action = "ExportToExternalStorage"}, Request.UrlScheme()),
                    CancelExportProcessUrl = Url.RouteUrl("DefaultApiWithAction", new { httproute = "", controller = "DataExportApi", action = "DeleteDataExportProcess" }),
                }
            };

            return this.View(export);
        }

        [ObserverNotAllowed]
        public ActionResult Index()
        {
            this.ViewBag.ActivePage = MenuItem.DataExport;
            this.ViewBag.EnableInterviewHistory = this.interviewDataExportSettings.EnableInterviewHistory;

            AllUsersAndQuestionnairesView usersAndQuestionnaires =
                this.allUsersAndQuestionnairesFactory.Load();

            ExportModel export = new ExportModel
            {
                Questionnaires = usersAndQuestionnaires.Questionnaires,
                ExportStatuses = new List<InterviewStatus>
                {
                    InterviewStatus.InterviewerAssigned,
                    InterviewStatus.Completed,
                    InterviewStatus.ApprovedBySupervisor,
                    InterviewStatus.ApprovedByHeadquarters
                },
                ExternalStoragesSettings = this.externalStoragesSettings is FakeExternalStoragesSettings
                    ? null
                    : this.externalStoragesSettings
            };

            return this.View(export);
        }
    }
}
