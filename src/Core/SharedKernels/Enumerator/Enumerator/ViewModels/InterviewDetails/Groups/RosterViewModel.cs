﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform.Core;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Utils;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Utils;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Groups
{
    public class RosterViewModel : MvxNotifyPropertyChanged,
        ILiteEventHandler<RosterInstancesAdded>,
        ILiteEventHandler<RosterInstancesRemoved>,
        IDisposable,
        IInterviewEntityViewModel
    {
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly IInterviewViewModelFactory interviewViewModelFactory;
        private readonly ILiteEventRegistry eventRegistry;
        private readonly IMvxMainThreadDispatcher mainThreadDispatcher;
        private string interviewId;
        private NavigationState navigationState;
        private CovariantObservableCollection<ICompositeEntity> rosterInstances;

        public IObservableCollection<ICompositeEntity> RosterInstances => this.rosterInstances;

        public Identity Identity { get; private set; }

        public RosterViewModel(IStatefulInterviewRepository interviewRepository,
            IInterviewViewModelFactory interviewViewModelFactory,
            ILiteEventRegistry eventRegistry,
            IMvxMainThreadDispatcher mainThreadDispatcher)
        {
            this.interviewRepository = interviewRepository;
            this.interviewViewModelFactory = interviewViewModelFactory;
            this.eventRegistry = eventRegistry;
            this.mainThreadDispatcher = mainThreadDispatcher;
            this.rosterInstances = new CovariantObservableCollection<ICompositeEntity>();
        }

        public void Init(string interviewId, Identity entityId, NavigationState navigationState)
        {
            this.interviewId = interviewId;
            this.Identity = entityId;
            this.navigationState = navigationState;

            this.eventRegistry.Subscribe(this, interviewId);
            var statefulInterview = this.interviewRepository.Get(interviewId);

            var viewModels = statefulInterview.GetRosterInstances(navigationState.CurrentGroup, entityId.Id)
                                              .Select(this.GetGroupViewModel);
            this.rosterInstances = new CovariantObservableCollection<ICompositeEntity>(viewModels);
        }

        public void Handle(RosterInstancesAdded @event)
        {
            var typedRosterInstances = this.RosterInstances.Cast<GroupViewModel>().ToArray();

            foreach (AddedRosterInstance newRosterInstance in @event.Instances.Where(x => x.GroupId == this.Identity.Id))
            {
                var groupViewModel = this.GetGroupViewModel(newRosterInstance.GetIdentity());

                int index = Array.FindLastIndex(typedRosterInstances, t => t.SortIndex < groupViewModel.SortIndex) + 1;
                this.mainThreadDispatcher.RequestMainThreadAction(() => this.rosterInstances.Insert(index, groupViewModel));
            }
        }

        public void Handle(RosterInstancesRemoved @event)
        {
            var typedRosterInstances = this.RosterInstances.Cast<GroupViewModel>().ToList();

            foreach (var rosterInstance in @event.Instances)
            {
                var instancesToRemove = typedRosterInstances.Where(x => x.Identity.Equals(rosterInstance.GetIdentity())).ToList();

                this.mainThreadDispatcher.RequestMainThreadAction(() => instancesToRemove.ForEach(x => this.rosterInstances.Remove(x)));
                instancesToRemove.ForEach(x => x.DisposeIfDisposable());
            }
        }

        private GroupViewModel GetGroupViewModel(Identity identity)
        {
            var groupViewModel = this.interviewViewModelFactory.GetNew<GroupViewModel>();
            groupViewModel.Init(this.interviewId, identity, this.navigationState);
            return groupViewModel;
        }

        public void Dispose()
        {
            foreach (var rosterInstance in this.RosterInstances)
            {
                rosterInstance.DisposeIfDisposable();
            }

            this.eventRegistry.Unsubscribe(this);
        }
    }
}