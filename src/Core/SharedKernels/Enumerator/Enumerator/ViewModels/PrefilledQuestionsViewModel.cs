using System;
using System.Collections.ObjectModel;
using System.Linq;
using Cirrious.MvvmCross.ViewModels;
using WB.Core.BoundedContexts.Tester.Implementation.Entities;
using WB.Core.BoundedContexts.Tester.Repositories;
using WB.Core.BoundedContexts.Tester.Services;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.PlainStorage;

namespace WB.Core.BoundedContexts.Tester.ViewModels
{
    public class PrefilledQuestionsViewModel : BaseViewModel
    {
        private readonly IInterviewViewModelFactory interviewViewModelFactory;
        private readonly IPlainKeyValueStorage<QuestionnaireModel> plainQuestionnaireRepository;
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly IViewModelNavigationService viewModelNavigationService;
        private string interviewId;

        public PrefilledQuestionsViewModel(
            IInterviewViewModelFactory interviewViewModelFactory,
            IPlainKeyValueStorage<QuestionnaireModel> plainQuestionnaireRepository,
            IStatefulInterviewRepository interviewRepository,
            IViewModelNavigationService viewModelNavigationService)
        {
            if (interviewViewModelFactory == null) throw new ArgumentNullException("interviewViewModelFactory");
            if (plainQuestionnaireRepository == null) throw new ArgumentNullException("plainQuestionnaireRepository");
            if (interviewRepository == null) throw new ArgumentNullException("interviewRepository");
            if (viewModelNavigationService == null) throw new ArgumentNullException("viewModelNavigationService");

            this.interviewViewModelFactory = interviewViewModelFactory;
            this.plainQuestionnaireRepository = plainQuestionnaireRepository;
            this.interviewRepository = interviewRepository;
            this.viewModelNavigationService = viewModelNavigationService;
        }

        public string QuestionnaireTitle { get; set; }

        private ObservableCollection<dynamic> prefilledQuestions;
        public ObservableCollection<dynamic> PrefilledQuestions
        {
            get { return prefilledQuestions; }
            set { prefilledQuestions = value; RaisePropertyChanged(); }
        }

        public void Init(string interviewId)
        {
            if (interviewId == null) throw new ArgumentNullException("interviewId");

            this.interviewId = interviewId;

            var interview = this.interviewRepository.Get(this.interviewId);
            if (interview == null) throw new Exception("Interview is null.");

            var questionnaire = this.plainQuestionnaireRepository.GetById(interview.QuestionnaireId);
            if (questionnaire == null) throw new Exception("questionnaire is null");

            if (questionnaire.PrefilledQuestionsIds.Count == 0)
            {
                this.viewModelNavigationService.NavigateTo<InterviewViewModel>(new { interviewId = this.interviewId });
                return;
            }

            this.QuestionnaireTitle = questionnaire.Title;
            this.PrefilledQuestions = new ObservableCollection<dynamic>();

            this.interviewViewModelFactory.GetPrefilledQuestions(this.interviewId)
                .ForEach(x => this.PrefilledQuestions.Add(x));

            var startButton = this.interviewViewModelFactory.GetNew<StartInterviewViewModel>();
            startButton.Init(interviewId, null, null);
            this.PrefilledQuestions.Add(startButton);
        }
        
        public override void NavigateToPreviousViewModel()
        {
//            TODO: CAPI-Interview-Details
//            this.viewModelNavigationService.NavigateTo<DashboardViewModel>();
        }
    }
}