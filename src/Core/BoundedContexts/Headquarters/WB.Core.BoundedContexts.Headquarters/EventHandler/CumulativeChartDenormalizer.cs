﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.BoundedContexts.Headquarters.DataExport.Accessors;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.EventBus;
using WB.Core.Infrastructure.EventHandlers;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Infrastructure.Native.Storage;

namespace WB.Core.BoundedContexts.Headquarters.EventHandler
{
    internal class CumulativeChartDenormalizer : IFunctionalEventHandler, IEventHandler
    {
        internal class CumulativeState
        {
            public List<CumulativeReportStatusChange> Added { get; set; } = new List<CumulativeReportStatusChange>();
            public InterviewStatus? LastInterviewStatus { get; set; }
            public QuestionnaireIdentity QuestionnaireIdentity { get; set; }
            public bool IsDirty => Added.Count > 0;
        }

        private readonly INativeReadSideStorage<CumulativeReportStatusChange> cumulativeReportReader;
        private readonly IMemoryCache memoryCache;
        private readonly IReadSideRepositoryWriter<CumulativeReportStatusChange> cumulativeReportStatusChangeStorage;
        private readonly IQueryableReadSideRepositoryReader<InterviewSummary> interviewReferencesStorage;

        public CumulativeChartDenormalizer(
            IReadSideRepositoryWriter<CumulativeReportStatusChange> cumulativeReportStatusChangeStorage,
            IQueryableReadSideRepositoryReader<InterviewSummary> interviewReferencesStorage,
            INativeReadSideStorage<CumulativeReportStatusChange> cumulativeReportReader,
            IMemoryCache memoryCache)
        {
            this.cumulativeReportStatusChangeStorage = cumulativeReportStatusChangeStorage;
            this.interviewReferencesStorage = interviewReferencesStorage;
            this.cumulativeReportReader = cumulativeReportReader;
            this.memoryCache = memoryCache;
        }

        public void Handle(IEnumerable<IPublishableEvent> publishableEvents)
        {
            var statusChangeEvents = publishableEvents.Where(x => x.Payload is InterviewStatusChanged).ToList();
            
            if (!statusChangeEvents.Any())
                return;

            Dictionary<Guid, CumulativeState> states = new Dictionary<Guid, CumulativeState>();

            foreach (var statusChangeEvent in statusChangeEvents)
            {
                var interviewStatusChanged = statusChangeEvent.Payload as InterviewStatusChanged;

                if(interviewStatusChanged == null) continue;

                var state = states.GetOrAdd(statusChangeEvent.EventSourceId, () => new CumulativeState
                {
                    QuestionnaireIdentity = this.memoryCache.GetQuestionnaireIdentity(
                        this.interviewReferencesStorage, 
                        statusChangeEvent.EventSourceId)
                });

                state.LastInterviewStatus = interviewStatusChanged.PreviousStatus
                                            ?? state.LastInterviewStatus
                                            ?? cumulativeReportReader.Query(_ => _
                                                .Where(x => x.InterviewId == statusChangeEvent.EventSourceId && x.ChangeValue > 0)
                                                .OrderByDescending(x => x.EventSequence)
                                                .FirstOrDefault())?.Status;

                this.Update(state, interviewStatusChanged, statusChangeEvent);

                state.LastInterviewStatus = interviewStatusChanged.Status;
            }

            foreach (var state in states.Values)
            {
                if (state.IsDirty)
                    this.SaveState(state);
            }
        }

        private void Update(CumulativeState state, InterviewStatusChanged statusChanged, IPublishableEvent publishableEvent)
        {
            InterviewStatus? lastInterviewStatus = state.LastInterviewStatus;

            InterviewStatus newStatus = statusChanged.Status;
            if (newStatus == InterviewStatus.Deleted)
                return;

            if (lastInterviewStatus != null)
            {
                var minusChange = new CumulativeReportStatusChange(
                    $"{publishableEvent.EventIdentifier.FormatGuid()}-minus",
                    state.QuestionnaireIdentity.QuestionnaireId,
                    state.QuestionnaireIdentity.Version,
                    publishableEvent.EventTimeStamp.Date, // time of synchronization
                    lastInterviewStatus.Value,
                    -1,
                    publishableEvent.EventSourceId,
                    publishableEvent.EventSequence);

                state.Added.Add(minusChange);
            }

            var plusChange = new CumulativeReportStatusChange(
                $"{publishableEvent.EventIdentifier.FormatGuid()}-plus",
                state.QuestionnaireIdentity.QuestionnaireId,
                state.QuestionnaireIdentity.Version,
                publishableEvent.EventTimeStamp.Date, // time of synchronization
                newStatus,
                +1,
                publishableEvent.EventSourceId,
                publishableEvent.EventSequence);

            state.Added.Add(plusChange);
        }

        protected void SaveState(CumulativeState state)
        {
            cumulativeReportStatusChangeStorage.BulkStore(state.Added.Select(x => new Tuple<CumulativeReportStatusChange, string>(x, x.EntryId)).ToList());
        }

        public string Name => "Cumulative Chart Functional Denormalizer";
    }
}
