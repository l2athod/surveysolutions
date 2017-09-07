﻿using System;
using System.Linq;
using Machine.Specifications;
using WB.Core.BoundedContexts.Headquarters.Views.DataExport;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.Factories;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.InputModels;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.Views;
using WB.Tests.Abc;
using WB.Tests.Abc.Storage;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.Factories.SpeedReportFactoryTests
{
    internal class when_load_speed_report_for_interviewers_by_day_period : SpeedReportFactoryTestContext
    {
        Establish context = () =>
        {
            input = CreateSpeedByInterviewersReportInputModel(supervisorId: supervisorId, from: new DateTime(2010, 6, 10, 0, 0, 0, DateTimeKind.Utc), period:"d",  columnCount: 2);

            var user = Create.Entity.UserDocument(supervisorId: supervisorId);

            interviewStatuses = new TestInMemoryWriter<InterviewSummary>();
            interviewStatuses.Store(
                Create.Entity.InterviewSummary(questionnaireId: input.QuestionnaireId,
                    questionnaireVersion: input.QuestionnaireVersion,
                    statuses: new[]
                    {
                        Create.Entity.InterviewCommentedStatus(interviewerId: user.PublicKey,
                            supervisorId: supervisorId,
                            timestamp: input.From.Date.AddHours(3),
                            status: InterviewExportedAction.Completed,
                            timeSpanWithPreviousStatus: TimeSpan.FromMinutes(35)),
                        Create.Entity.InterviewCommentedStatus(interviewerId: Guid.NewGuid(),
                            supervisorId: Guid.NewGuid(),
                            timestamp: input.From.Date.AddHours(2),
                            status: InterviewExportedAction.Completed,
                            timeSpanWithPreviousStatus: TimeSpan.FromMinutes(35)),
                        Create.Entity.InterviewCommentedStatus(interviewerId: user.PublicKey,
                            supervisorId: supervisorId,
                            timestamp: input.From.Date.AddHours(-1),
                            status: InterviewExportedAction.Completed,
                            timeSpanWithPreviousStatus: TimeSpan.FromMinutes(15)),
                        Create.Entity.InterviewCommentedStatus(interviewerId: user.PublicKey,
                            supervisorId: supervisorId,
                            timestamp: input.From.Date.AddHours(-22), // 2010-6-9
                            status: InterviewExportedAction.Completed,
                            timeSpanWithPreviousStatus: TimeSpan.FromMinutes(28))
                    }), "2");

            speedReportFactory = CreateSpeedReportFactory(interviewStatuses: interviewStatuses);
        };
        
        Because of = () =>
            result = speedReportFactory.Load(input);

        It should_return_one_row = () =>
            result.Items.Count().ShouldEqual(1);

        It should_return_first_row_with_25_minutes_per_interview_at_first_period_and_28_minutes_per_interview_at_second = () =>
            result.Items.First().SpeedByPeriod.ShouldEqual(new double?[]{ 28, null });

        It should_return_first_row_with_Total = () =>
            result.Items.First().Total.ShouldEqual(28);

        It should_return_first_row_with_Average = () =>
           result.Items.First().Average.ShouldEqual(28);

        private static SpeedReportFactory speedReportFactory;
        private static SpeedByInterviewersReportInputModel input;
        private static SpeedByResponsibleReportView result;
        private static TestInMemoryWriter<InterviewSummary> interviewStatuses;
        private static Guid supervisorId = Guid.Parse("11111111111111111111111111111111");
    }
}
