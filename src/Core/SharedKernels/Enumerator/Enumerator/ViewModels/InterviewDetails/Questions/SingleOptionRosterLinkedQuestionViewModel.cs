﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using MvvmCross;
using MvvmCross.Base;
using MvvmCross.ViewModels;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Utils;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions
{
    public class SingleOptionRosterLinkedQuestionViewModel : MvxNotifyPropertyChanged,
        IInterviewEntityViewModel,
        IViewModelEventHandler<AnswersRemoved>,
        IAsyncViewModelEventHandler<RosterInstancesTitleChanged>,
        IAsyncViewModelEventHandler<LinkedOptionsChanged>,
        ICompositeQuestionWithChildren,
        IDisposable
    {
        private readonly Guid userId;
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly IQuestionnaireStorage questionnaireRepository;
        private readonly IViewModelEventRegistry eventRegistry;
        private readonly IMvxMainThreadAsyncDispatcher mainThreadDispatcher;
        private readonly ThrottlingViewModel throttlingModel;

        public SingleOptionRosterLinkedQuestionViewModel(
            IPrincipal principal,
            IQuestionnaireStorage questionnaireRepository,
            IStatefulInterviewRepository interviewRepository,
            IViewModelEventRegistry eventRegistry,
            QuestionStateViewModel<SingleOptionLinkedQuestionAnswered> questionStateViewModel,
            QuestionInstructionViewModel instructionViewModel,
            AnsweringViewModel answering, 
            ThrottlingViewModel throttlingModel)
        {
            if (principal == null) throw new ArgumentNullException(nameof(principal));
            this.questionnaireRepository = questionnaireRepository ?? throw new ArgumentNullException(nameof(questionnaireRepository));
            this.interviewRepository = interviewRepository ?? throw new ArgumentNullException(nameof(interviewRepository));
            this.eventRegistry = eventRegistry ?? throw new ArgumentNullException(nameof(eventRegistry));
            
            this.userId = principal.CurrentUserIdentity.UserId;
            this.mainThreadDispatcher = Mvx.IoCProvider.Resolve<IMvxMainThreadAsyncDispatcher>();

            this.questionState = questionStateViewModel;
            this.InstructionViewModel = instructionViewModel;
            this.Answering = answering;
            this.throttlingModel = throttlingModel;
            this.throttlingModel.Init(SaveAnswer);
        }

        private Identity questionIdentity;
        private string interviewId;
        private Guid linkedToRosterId;
        private CovariantObservableCollection<SingleOptionLinkedQuestionOptionViewModel> options;
        private HashSet<Guid> parentRosters;
        private readonly QuestionStateViewModel<SingleOptionLinkedQuestionAnswered> questionState;
        private OptionBorderViewModel optionsTopBorderViewModel;
        private OptionBorderViewModel optionsBottomBorderViewModel;

        public CovariantObservableCollection<SingleOptionLinkedQuestionOptionViewModel> Options
        {
            get => this.options;
            private set
            {
                this.options = value;
                this.RaisePropertyChanged(() => this.HasOptions);
            }
        }

        public bool HasOptions => this.Options.Any();

        public IQuestionStateViewModel QuestionState => this.questionState;

        public IObservableCollection<ICompositeEntity> Children
        {
            get
            {
                var result = new CompositeCollection<ICompositeEntity>();
                this.optionsTopBorderViewModel = new OptionBorderViewModel(this.questionState, true)
                {
                    HasOptions = HasOptions
                };
                result.Add(this.optionsTopBorderViewModel);
                result.AddCollection(this.Options);
                this.optionsBottomBorderViewModel = new OptionBorderViewModel(this.questionState, false)
                {
                    HasOptions = HasOptions
                };
                result.Add(this.optionsBottomBorderViewModel);
                return result;
            }
        }

        public QuestionInstructionViewModel InstructionViewModel { get; set; }
        public AnsweringViewModel Answering { get; private set; }

        public Identity Identity => this.questionIdentity;

        public void Init(string interviewId, Identity questionIdentity, NavigationState navigationState)
        {
            if (interviewId == null) throw new ArgumentNullException(nameof(interviewId));
            if (questionIdentity == null) throw new ArgumentNullException(nameof(questionIdentity));

            this.questionState.Init(interviewId, questionIdentity, navigationState);
            this.InstructionViewModel.Init(interviewId, questionIdentity, navigationState);

            var interview = this.interviewRepository.Get(interviewId);

            this.questionIdentity = questionIdentity;
            this.interviewId = interviewId;

            var questionnaire =
                this.questionnaireRepository.GetQuestionnaire(interview.QuestionnaireIdentity,
                    interview.Language);
            this.linkedToRosterId = questionnaire.GetRosterReferencedByLinkedQuestion(questionIdentity.Id);
            this.parentRosters = questionnaire.GetRostersFromTopToSpecifiedEntity(this.linkedToRosterId).ToHashSet();
            this.Options =
                new CovariantObservableCollection<SingleOptionLinkedQuestionOptionViewModel>(this.CreateOptions());

            var question = interview.GetLinkedSingleOptionQuestion(this.Identity);
            this.previousOptionToReset = question.IsAnswered() ? (decimal[])question.GetAnswer()?.SelectedValue : (decimal[])null;

            this.Options.CollectionChanged += CollectionChanged;
            this.eventRegistry.Subscribe(this, interviewId);
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.optionsTopBorderViewModel != null)
            {
                this.optionsTopBorderViewModel.HasOptions = HasOptions;
            }
            if (this.optionsBottomBorderViewModel != null)
            {
                this.optionsBottomBorderViewModel.HasOptions = this.HasOptions;
            }
        }

        public void Dispose()
        {
            this.Options.CollectionChanged -= CollectionChanged;
            this.eventRegistry.Unsubscribe(this);
            this.QuestionState.Dispose();
            this.InstructionViewModel.Dispose();

            foreach (var option in Options)
            {
                option.BeforeSelected -= this.OptionSelected;
                option.AnswerRemoved -= this.RemoveAnswer;
            }
            this.throttlingModel.Dispose();
        }

        private IEnumerable<SingleOptionLinkedQuestionOptionViewModel> CreateOptions()
        {
            var interview = this.interviewRepository.GetOrThrow(interviewId);
            var linkedQuestion = interview.GetLinkedSingleOptionQuestion(this.Identity);

            foreach (var linkedOption in linkedQuestion.Options)
                yield return this.CreateOptionViewModel(linkedOption, linkedQuestion.GetAnswer()?.SelectedValue,
                    interview);
        }

        private async void OptionSelected(object sender, EventArgs eventArgs)
        {
            await this.OptionSelectedAsync(sender);
        }

        private decimal[] previousOptionToReset = null;
        private decimal[] selectedOptionToSave = null;

        private async Task SaveAnswer()
        {
            if (this.previousOptionToReset != null && this.selectedOptionToSave.SequenceEqual(this.previousOptionToReset))
                return;

            var selectedOption = this.GetOptionByValue(this.selectedOptionToSave);
            var previousOption = this.GetOptionByValue(this.previousOptionToReset);

            if (selectedOption == null)
                return;

            var command = new AnswerSingleOptionLinkedQuestionCommand(
                Guid.Parse(this.interviewId),
                this.userId,
                this.questionIdentity.Id,
                this.questionIdentity.RosterVector,
                selectedOption.RosterVector);

            try
            {
                if (previousOption != null)
                {
                    previousOption.Selected = false;
                }

                await this.Answering.SendQuestionCommandAsync(command);

                this.previousOptionToReset = this.selectedOptionToSave;

                await this.QuestionState.Validity.ExecutedWithoutExceptions();
            }
            catch (InterviewException ex)
            {
                selectedOption.Selected = false;

                if (previousOption != null)
                {
                    previousOption.Selected = true;
                }

                await this.QuestionState.Validity.ProcessException(ex);
            }
        }

        private SingleOptionLinkedQuestionOptionViewModel GetOptionByValue(decimal[] value)
        {
            return value != null
                ? this.Options.FirstOrDefault(x => Enumerable.SequenceEqual(x.RosterVector, value))
                : null;
        }

        internal async Task OptionSelectedAsync(object sender)
        {
            var selectedOption = (SingleOptionLinkedQuestionOptionViewModel) sender;
            this.selectedOptionToSave = selectedOption.RosterVector;

            this.Options.Where(x => x.Selected && x != selectedOption).ForEach(x => x.Selected = false);

            await this.throttlingModel.ExecuteActionIfNeeded();
        }

        private async void RemoveAnswer(object sender, EventArgs e)
        {
            try
            {
                this.throttlingModel.CancelPendingAction();
                await this.Answering.SendQuestionCommandAsync(
                    new RemoveAnswerCommand(Guid.Parse(this.interviewId),
                        this.userId,
                        this.questionIdentity));
                await this.QuestionState.Validity.ExecutedWithoutExceptions();

                foreach (var option in this.Options.Where(option => option.Selected).ToList())
                {
                    option.Selected = false;
                }

                this.previousOptionToReset = null;
            }
            catch (InterviewException exception)
            {
                await this.QuestionState.Validity.ProcessException(exception);
            }
        }

        public void Handle(AnswersRemoved @event)
        {
            foreach (var question in @event.Questions)
            {
                if (this.questionIdentity.Equals(question.Id, question.RosterVector))
                {
                    foreach (var option in this.Options.Where(option => option.Selected))
                    {
                        option.Selected = false;
                    }

                    this.previousOptionToReset = null;
                }
            }
        }

        private SingleOptionLinkedQuestionOptionViewModel CreateOptionViewModel(RosterVector linkedOption,
            RosterVector answeredOption, IStatefulInterview interview)
        {
            var optionViewModel = new SingleOptionLinkedQuestionOptionViewModel
            {
                RosterVector = linkedOption,
                Title = interview.GetLinkedOptionTitle(this.Identity, linkedOption),
                Selected = linkedOption.Equals(answeredOption),
                QuestionState = this.questionState
            };

            optionViewModel.BeforeSelected += this.OptionSelected;
            optionViewModel.AnswerRemoved += this.RemoveAnswer;

            return optionViewModel;
        }

        public async Task HandleAsync(RosterInstancesTitleChanged @event)
        {
            var optionListShouldBeUpdated = @event.ChangedInstances.Any(x =>
                x.RosterInstance.GroupId == this.linkedToRosterId ||
                this.parentRosters.Contains(x.RosterInstance.GroupId));
            if (optionListShouldBeUpdated)
            {
                await this.RefreshOptionsListFromModelAsync();
            }
        }

        public async Task HandleAsync(LinkedOptionsChanged @event)
        {
            var optionListShouldBeUpdated = @event.ChangedLinkedQuestions.Any(x => x.QuestionId.Id == this.Identity.Id);
            if (optionListShouldBeUpdated)
            {
                await this.RefreshOptionsListFromModelAsync();
            }
        }

        private async Task RefreshOptionsListFromModelAsync()
        {
            var optionsToUpdate = this.CreateOptions().ToArray();

            await this.mainThreadDispatcher.ExecuteOnMainThreadAsync(() =>
            {
                this.Options.ReplaceWith(optionsToUpdate);
                this.RaisePropertyChanged(() => this.HasOptions);
            });
        }
    }
}
