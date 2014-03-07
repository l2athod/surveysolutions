﻿using System;
using System.Linq;
using Machine.Specifications;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using It = Machine.Specifications.It;

namespace WB.Core.SharedKernels.DataCollection.Tests.InterviewTests
{
    internal class when_hqreject_interview : InterviewTestsContext
    {
        private Establish context = () =>
        {
            userId = Guid.Parse("AAAA0000AAAA00000000AAAA0000AAAA");
            supervisorId = Guid.Parse("BBAA0000AAAA00000000AAAA0000AAAA");
            questionnaireId = Guid.Parse("33333333333333333333333333333333");

            interview = CreateInterview(questionnaireId: questionnaireId);

            interview.AssignInterviewer(supervisorId, userId);
            interview.Complete(userId, string.Empty);
            interview.Approve(userId, string.Empty);

            eventContext = new EventContext();
        };

        private Because of = () =>
            interview.HqReject(userId, string.Empty);

        private It should_raise_two_events = () =>
            eventContext.Events.Count().ShouldEqual(2);

        It should_raise_InterviewApprovedByHQ_event = () =>
            eventContext.ShouldContainEvent<InterviewRejectedByHQ>(@event => @event.UserId == userId);

        It should_raise_InterviewStatusChanged_event = () =>
            eventContext.ShouldContainEvent<InterviewStatusChanged>(@event => @event.Status == InterviewStatus.RejectedByHeadquarters);
        
        Cleanup stuff = () =>
        {
            eventContext.Dispose();
            eventContext = null;
        };

        private static Guid userId;
        private static Guid supervisorId;

        private static Guid questionnaireId;
        private static EventContext eventContext;
        private static Interview interview;
    }
}
