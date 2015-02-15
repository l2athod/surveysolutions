using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Chance.MvvmCross.Plugins.UserInteraction;
using Cirrious.MvvmCross.Plugins.WebBrowser;
using Cirrious.MvvmCross.ViewModels;
using WB.Core.BoundedContexts.QuestionnaireTester.Properties;
using WB.Core.GenericSubdomains.Utils.Implementation;
using WB.Core.GenericSubdomains.Utils.Services;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernel.Structures.Synchronization.Designer;
using WB.Core.SharedKernels.DataCollection;
using QuestionnaireVersion = WB.Core.SharedKernel.Structures.Synchronization.Designer.QuestionnaireVersion;

namespace WB.Core.BoundedContexts.QuestionnaireTester.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IRestService restService;
        private readonly IQueryablePlainStorageAccessor<QuestionnaireMetaInfo> questionnairesStorageAccessor;
        private readonly IMvxWebBrowserTask webBrowser;

        private readonly QuestionnaireVersion supportedQuestionnaireVersion;

        public DashboardViewModel(IPrincipal principal, IRestService restService, ILogger logger,
            IUserInteraction uiDialogs, IQueryablePlainStorageAccessor<QuestionnaireMetaInfo> questionnairesStorageAccessor, IMvxWebBrowserTask webBrowser)
            : base(logger, principal: principal, uiDialogs: uiDialogs)
        {
            this.restService = restService;
            this.questionnairesStorageAccessor = questionnairesStorageAccessor;
            this.webBrowser = webBrowser;

            var engineVersion = QuestionnaireVersionProvider.GetCurrentEngineVersion();
            this.supportedQuestionnaireVersion = new QuestionnaireVersion()
            {
                Major = engineVersion.Major,
                Minor = engineVersion.Minor,
                Patch = engineVersion.Patch
            };
        }

        public async void Init()
        {
            if (this.Principal.CurrentIdentity.IsAuthenticated)
            {
                await this.LoadQuestionnairesMetaInfoFromStorage();   
            }
            else
            {
                this.ShowViewModel<LoginViewModel>();
            }
        }

        private IList<QuestionnaireMetaInfo> allQuestionnaires;
        private IList<QuestionnaireMetaInfo> questionnaires;

        public IList<QuestionnaireMetaInfo> Questionnaires
        {
            get { return questionnaires; }
            set
            {
                questionnaires = value;
                RaisePropertyChanged(() => Questionnaires);
            }
        }

        private bool isInProgress = false;

        public bool IsInProgress
        {
            get { return isInProgress; }
            set
            {
                isInProgress = value;
                RaisePropertyChanged(() => IsInProgress);
            }
        }

        public string LoginName
        {
            get { return this.Principal.CurrentIdentity.Name; }
        }

        private IMvxCommand signOutCommand;
        public IMvxCommand SignOutCommand
        {
            get { return signOutCommand ?? (signOutCommand = new MvxCommand(this.SignOut)); }
        }

        private IMvxCommand showSettingsCommand;
        public IMvxCommand ShowSettingsCommand
        {
            get { return showSettingsCommand ?? (showSettingsCommand = new MvxCommand(() => this.ShowViewModel<SettingsViewModel>())); }
        }

        private IMvxCommand showHelpCommand;
        public IMvxCommand ShowHelpCommand
        {
            get { return showHelpCommand ?? (showHelpCommand = new MvxCommand(() => this.ShowViewModel<HelpViewModel>())); }
        }

        private IMvxCommand loadQuestionnaireCommand;
        public IMvxCommand LoadQuestionnaireCommand
        {
            get
            {
                return loadQuestionnaireCommand ?? (loadQuestionnaireCommand = new MvxCommand<QuestionnaireMetaInfo>(this.LoadQuestionnaire,
                        (questionnaire) => questionnaire.Version <= supportedQuestionnaireVersion));
            }
        }

        private IMvxCommand refreshQuestionnairesCommand;
        public IMvxCommand RefreshQuestionnairesCommand
        {
            get { return refreshQuestionnairesCommand ?? (refreshQuestionnairesCommand = new MvxCommand(async () => await this.RefreshQuestionnaires(), () => !this.IsInProgress)); }
        }

        private IMvxCommand findQuestionnairesCommand;
        public IMvxCommand FindQuestionnairesCommand
        {
            get { return findQuestionnairesCommand ?? (findQuestionnairesCommand = new MvxCommand<string>(this.FindQuestionnaires, (qyery) => !this.IsInProgress)); }
        }

        private IMvxCommand navigateToApplicationStorePageCommand;
        public IMvxCommand NavigateToApplicationStorePageCommand
        {
            get { return navigateToApplicationStorePageCommand ?? (navigateToApplicationStorePageCommand = new MvxCommand(() => this.webBrowser.ShowWebPage("market://details?id=org.worldbank.solutions.Vtester"))); }
        }


        private void LoadQuestionnaire(QuestionnaireMetaInfo questionnaire)
        {
            this.ShowViewModel<QuestionnairePrefilledQuestionsViewModel>(questionnaire);
        }

        private void FindQuestionnaires(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                if (this.allQuestionnaires == null) return;

                this.Questionnaires = this.allQuestionnaires;
                this.allQuestionnaires = null;
            }
            else
            {
                if (this.allQuestionnaires == null)
                {
                    this.allQuestionnaires = this.Questionnaires;
                }

                if (this.allQuestionnaires != null)
                {
                    this.Questionnaires = this.allQuestionnaires.Where(
                        item =>
                            item.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) > -1 ||
                            item.OwnerName.IndexOf(query, StringComparison.OrdinalIgnoreCase) > -1).ToList();
                }
            }
        }

        private Task LoadQuestionnairesMetaInfoFromStorage()
        {
            return Task.Run(() =>
            {
                this.Questionnaires = questionnairesStorageAccessor.LoadAll().ToList();
            });
        }

        private Task SaveQuestionnairesMetaInfoToStorage(IEnumerable<QuestionnaireMetaInfo> questionnaireListItems)
        {
            return Task.Run(() =>
            {
                this.questionnairesStorageAccessor.RemoveAll();
                this.questionnairesStorageAccessor.Store(questionnaireListItems.Select(qli => new Tuple<QuestionnaireMetaInfo, string>(qli, qli.Id)));
                this.Questionnaires = questionnaireListItems.ToList();
            });
        }

        public async Task RefreshQuestionnaires()
        {
            this.IsInProgress = true;
            try
            {
                var questionnaires = await this.restService.GetAsync<IEnumerable<QuestionnaireMetaInfo>>(
                    url: "questionnaires",
                    credentials:
                        new RestCredentials()
                        {
                            Login = this.Principal.CurrentIdentity.Name,
                            Password = this.Principal.CurrentIdentity.Password
                        });

                await this.SaveQuestionnairesMetaInfoToStorage(questionnaires);
            }
            catch (RestException ex)
            {
                switch (ex.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        this.SignOut();
                        break;
                    case HttpStatusCode.ServiceUnavailable:
                        this.UIDialogs.Alert(ex.Message.Contains("maintenance")
                            ? UIResources.Maintenance
                            : UIResources.ServiceUnavailable);
                        break;
                    case HttpStatusCode.RequestTimeout:
                        this.UIDialogs.Alert(UIResources.RequestTimeout);
                        break;
                    default:
                        throw;
                }
            }
            finally
            {
                this.IsInProgress = false;
            }
        }
    }
}