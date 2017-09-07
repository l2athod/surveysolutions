﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.BoundedContexts.Headquarters.EventHandler;
using WB.Core.BoundedContexts.Headquarters.Views.DataExport;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.GenericSubdomains.Utils;
using WB.Tests.Abc;
using WB.Tests.Abc.Storage;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.EventHandlers.StatusChangeHistoryDenormalizerFunctionalTests
{
    internal class when_interview_first_answer_given : StatusChangeHistoryDenormalizerFunctionalTestContext
    {
        Establish context = () =>
        {
            interviewStatusesStorage = new TestInMemoryWriter<InterviewSummary>();
            interviewStatuses = Create.Entity.InterviewSummary(statuses: new [] { Create.Entity.InterviewCommentedStatus(status: InterviewExportedAction.InterviewerAssigned, statusId: interviewId) } );
            denormalizer = CreateDenormalizer(interviewStatuses: interviewStatusesStorage);
        };

        Because of = () => result = denormalizer.Update(interviewStatuses, Create.PublishedEvent.TextQuestionAnswered(interviewId: Guid.NewGuid()));

        It should_record_first_answer_status =
            () => result.InterviewCommentedStatuses.Last().Status.ShouldEqual(InterviewExportedAction.FirstAnswerSet);

        private static StatusChangeHistoryDenormalizerFunctional denormalizer;
        private static TestInMemoryWriter<InterviewSummary> interviewStatusesStorage;
        private static Guid interviewId = Guid.Parse("11111111111111111111111111111111");
        private static InterviewSummary interviewStatuses;
        private static InterviewSummary result;
    }
}
