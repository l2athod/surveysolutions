﻿using System;
using System.Linq;
using Machine.Specifications;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.Factories;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.InputModels;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.Views;
using WB.Tests.Abc;
using WB.Tests.Abc.Storage;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.Factories.SpeedReportFactoryTests
{
    internal class when_load_speed_report_for_supervisors_by_week_period_and_interview_completed_earlier_then_assigned : SpeedReportFactoryTestContext
    {
        Establish context = () =>
        {
            input = CreateSpeedBetweenStatusesBySupervisorsReportInputModel(period: "w");
            var timestamp = input.From.Date.AddHours(-1);

            var interviewStatuses = new TestInMemoryWriter<InterviewSummary>();
            interviewStatuses.Store(Create.Entity.InterviewSummary(questionnaireId: input.QuestionnaireId,
                questionnaireVersion: input.QuestionnaireVersion, statuses: new[]
                {
                    Create.Entity.InterviewCommentedStatus(timestamp: timestamp)
                }), "1");

            var user = Create.Entity.UserDocument(supervisorId: supervisorId);

            interviewStatusTimeSpans = new TestInMemoryWriter<InterviewStatusTimeSpans>();
            interviewStatusTimeSpans.Store(
                Create.Entity.InterviewStatusTimeSpans(questionnaireId: input.QuestionnaireId,
                    questionnaireVersion: input.QuestionnaireVersion,
                    timeSpans: new[]
                    {
                        Create.Entity.TimeSpanBetweenStatuses(interviewerId: user.PublicKey,
                            timestamp: timestamp,
                            timeSpanWithPreviousStatus: TimeSpan.FromMinutes(-35))
                    }), "2");

            quantityReportFactory = CreateSpeedReportFactory(interviewStatusTimeSpans: interviewStatusTimeSpans,
                interviewStatuses: interviewStatuses);
        };

        Because of = () =>
            result = quantityReportFactory.Load(input);

        It should_return_one_row = () =>
            result.Items.Count().ShouldEqual(1);

        It should_return_first_row_with_positive_35_minutes_per_interview_at_first_period_and_null_minutes_per_interview_at_second = () =>
            result.Items.First().SpeedByPeriod.ShouldEqual(new double?[] { 35 });

        It should_return_first_row_with_positive_35_minutes_in_Total = () =>
            result.Items.First().Total.ShouldEqual(35);

        It should_return_first_row_with_positive_35_minutesin_Average = () =>
           result.Items.First().Average.ShouldEqual(35);

        private static SpeedReportFactory quantityReportFactory;
        private static SpeedBetweenStatusesBySupervisorsReportInputModel input;
        private static SpeedByResponsibleReportView result;
        private static TestInMemoryWriter<InterviewStatusTimeSpans> interviewStatusTimeSpans;
        private static Guid supervisorId = Guid.Parse("11111111111111111111111111111111");
    }
}
