﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.BoundedContexts.Supervisor.Views.DataExport;
using WB.Core.BoundedContexts.Supervisor.Views.Interview;

namespace WB.Core.BoundedContexts.Supervisor.Tests.EventHandlers.InterviewExportedDataEventHandlerTests
{
    internal class when_InterviewApproved_recived_by_interview_with_nested_roster_with_2_rows_each : InterviewExportedDataEventHandlerTestContext
    {
        Establish context = () =>
        {
            questionInsideRosterGroupId = Guid.Parse("12222222222222222222222222222222");
            rosterId = Guid.Parse("11111111111111111111111111111111");
            nestedRosterId = Guid.Parse("13333333333333333333333333333333");
            
            questionnarie = CreateQuestionnaireDocumentWithOneChapter(new Group()
            {
                PublicKey = rosterId,
                IsRoster = true,
                RosterSizeSource = RosterSizeSourceType.FixedTitles,
                RosterFixedTitles = new[] { "t1", "t2" },
                Children = new List<IComposite>
                {
                    new Group()
                    {
                        PublicKey = nestedRosterId,
                        IsRoster = true,
                        RosterSizeSource = RosterSizeSourceType.FixedTitles,
                        RosterFixedTitles = new []{"t1","t2"},
                        Children = new List<IComposite>
                        {
                            new NumericQuestion()
                            {
                                PublicKey = questionInsideRosterGroupId,
                                QuestionType = QuestionType.Numeric,
                                StataExportCaption = "q1"
                            }
                        }
                    }
                }
            });

            interviewExportedDataDenormalizer = CreateInterviewExportedDataEventHandlerForQuestionnarieCreatedByMethod(
                () => questionnarie,
                CreateInterviewDataWith2PropagatedLevels, r => result = r);
        };

        Because of = () =>
            interviewExportedDataDenormalizer.Handle(CreatePublishableEvent());

        It should_records_count_equals_4 = () =>
           GetLevel(result, nestedRosterId).Records.Length.ShouldEqual(4);

        It should_first_record_id_equals_0 = () =>
           GetLevel(result, nestedRosterId).Records[0].RecordId.ShouldEqual("0");

        It should_second_record_id_equals_1 = () =>
           GetLevel(result, nestedRosterId).Records[1].RecordId.ShouldEqual("1");

        It should_third_record_id_equals_1 = () =>
           GetLevel(result, nestedRosterId).Records[2].RecordId.ShouldEqual("0");

        It should_fourth_record_id_equals_1 = () =>
           GetLevel(result, nestedRosterId).Records[3].RecordId.ShouldEqual("1");

        It should_first_rosters_record_parent_id_equals_to_main_level_record_id = () =>
          GetLevel(result, nestedRosterId).Records[0].ParentRecordId.ShouldEqual("0");

        It should_second_rosters_record_parent_id_equals_to_main_level_record_id = () =>
           GetLevel(result, nestedRosterId).Records[1].ParentRecordId.ShouldEqual("0");

        It should_third_rosters_record_parent_id_equals_to_main_level_record_id = () =>
          GetLevel(result, nestedRosterId).Records[2].ParentRecordId.ShouldEqual("1");

        It should_fourth_rosters_record_parent_id_equals_to_main_level_record_id = () =>
           GetLevel(result, nestedRosterId).Records[3].ParentRecordId.ShouldEqual("1");

        private static InterviewData CreateInterviewDataWith2PropagatedLevels()
        {
            InterviewData interview = CreateInterviewData();
            for (int i = 0; i < levelCount; i++)
            {
                var vector = new decimal[1] { i };
                var newLevel = new InterviewLevel(rosterId, null, vector);
                interview.Levels.Add(string.Join(",", vector), newLevel);
                for (int j = 0; j < levelCount; j++)
                {
                    var nestedVector = new decimal[] { i, j };
                    var nestedLevel = new InterviewLevel(nestedRosterId, null, nestedVector);
                    interview.Levels.Add(string.Join(",", nestedVector), nestedLevel);
                    var question = nestedLevel.GetOrCreateQuestion(questionInsideRosterGroupId);
                    question.Answer = "some answer";
                }

            }
            return interview;
        }

        private static EventHandler.InterviewExportedDataDenormalizer interviewExportedDataDenormalizer;
        private static InterviewDataExportView result;
        private static Guid nestedRosterId;
        private static Guid rosterId;
        private static Guid questionInsideRosterGroupId;
        private static int levelCount=2;
        private static QuestionnaireDocument questionnarie;
    }
}
