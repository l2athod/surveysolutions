﻿using System;
using System.Collections.Generic;
using System.Linq;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform.Core;
using MvvmCross.Plugins.Messenger;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Utils;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Groups;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;


namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails
{
    public class EnumerationStageViewModel : MvxViewModel,
        IDisposable
    {
        private CompositeCollection<ICompositeEntity> items;
        public CompositeCollection<ICompositeEntity> Items
        {
            get { return this.items; }
            set
            {
                this.items = value;
                this.RaisePropertyChanged();
            }
        }

        private readonly IInterviewViewModelFactory interviewViewModelFactory;
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly ICompositeCollectionInflationService compositeCollectionInflationService;

        readonly IUserInterfaceStateService userInterfaceStateService;
        private readonly IMvxMainThreadDispatcher mvxMainThreadDispatcher;

        private NavigationState navigationState;

        IStatefulInterview interview;
        string interviewId;

        public DynamicTextViewModel Name { get; }

        public EnumerationStageViewModel(
            IInterviewViewModelFactory interviewViewModelFactory,
            IStatefulInterviewRepository interviewRepository,
            IUserInterfaceStateService userInterfaceStateService,
            IMvxMainThreadDispatcher mvxMainThreadDispatcher,
            DynamicTextViewModel dynamicTextViewModel, 
            ICompositeCollectionInflationService compositeCollectionInflationService)
        {
            this.interviewViewModelFactory = interviewViewModelFactory;
            this.interviewRepository = interviewRepository;
            this.userInterfaceStateService = userInterfaceStateService;
            this.mvxMainThreadDispatcher = mvxMainThreadDispatcher;

            this.Name = dynamicTextViewModel;
            this.compositeCollectionInflationService = compositeCollectionInflationService;
        }

        public void Init(string interviewId, NavigationState navigationState, Identity groupId, Identity anchoredElementIdentity)
        {
            if (navigationState == null) throw new ArgumentNullException(nameof(navigationState));
            if (this.navigationState != null) throw new InvalidOperationException("ViewModel already initialized");

            this.interviewId = interviewId;
            this.interview = this.interviewRepository.Get(interviewId);

            this.navigationState = navigationState;
            this.Items = new CompositeCollection<ICompositeEntity>();

            this.InitRegularGroupScreen(groupId, anchoredElementIdentity);
        }

        private void InitRegularGroupScreen(Identity groupIdentity, Identity anchoredElementIdentity)
        {
            this.Name.Init(this.interviewId, groupIdentity);

            this.LoadFromModel(groupIdentity);
            this.SetScrollTo(anchoredElementIdentity);
        }

        private void SetScrollTo(Identity scrollTo)
        {
            var anchorElementIndex = 0;

            if (scrollTo != null)
            {
                this.mvxMainThreadDispatcher.RequestMainThreadAction(() =>
                {
                    ICompositeEntity childItem = (this.Items.OfType<GroupViewModel>().FirstOrDefault(x => x.Identity.Equals(scrollTo)) ??
                                                  (ICompositeEntity) this.Items.OfType<QuestionHeaderViewModel>().FirstOrDefault(x => x.Identity.Equals(scrollTo))) ??
                                                 this.Items.OfType<StaticTextViewModel>().FirstOrDefault(x => x.Identity.Equals(scrollTo));

                    anchorElementIndex = childItem != null ? this.Items.ToList().IndexOf(childItem) : 0;
                });
            }
            this.ScrollToIndex = anchorElementIndex;
        }

        public int? ScrollToIndex { get; set; }

        private void LoadFromModel(Identity groupIdentity)
        {
            try
            {
                this.userInterfaceStateService.NotifyRefreshStarted();

                var previousGroupNavigationViewModel = this.interviewViewModelFactory.GetNew<GroupNavigationViewModel>();
                previousGroupNavigationViewModel.Init(this.interviewId, groupIdentity, this.navigationState);

                foreach (var interviewItemViewModel in this.Items.OfType<IDisposable>())
                {
                    interviewItemViewModel.Dispose();
                }

                var entities = this.interviewViewModelFactory.GetEntities(
                    interviewId: this.navigationState.InterviewId,
                    groupIdentity: groupIdentity,
                    navigationState: this.navigationState);

                var newGroupItems = entities.Concat(
                        previousGroupNavigationViewModel.ToEnumerable<IInterviewEntityViewModel>()).ToList();

                this.InterviewEntities = newGroupItems;
                
                this.Items = this.compositeCollectionInflationService.GetInflatedCompositeCollection(newGroupItems);
            }
            finally
            {
                this.userInterfaceStateService.NotifyRefreshFinished();
            }
        }

        private IList<IInterviewEntityViewModel> InterviewEntities { get; set; }
        
        public void Dispose()
        {
            this.InterviewEntities.ToArray().ForEach(ie => ie.DisposeIfDisposable());
            this.Items.ToArray().ForEach(ie => ie.DisposeIfDisposable());

            this.Name.Dispose();
        }
    }
}