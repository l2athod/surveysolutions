﻿using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Moq;
using WB.Core.SharedKernels.DataCollection.ValueObjects;
using WB.Core.SharedKernels.DataCollection.Views;
using WB.Core.SharedKernels.SurveyManagement.Views;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.Merger
{
    internal class when_merging_questionnaire_and_interview_with_question_linked_on_question : InterviewDataAndQuestionnaireMergerTestContext
    {
        Establish context = () =>
        {
            linkedQuestionId = Guid.Parse("10000000000000000000000000000000");
            linkedOnNestedRosterQuestionId = Guid.Parse("33333333333333333333333333333333");

            rosterId = Guid.Parse("44444444444444444444444444444444");
            nestedRosterId = Guid.Parse("20000000000000000000000000000000");

            linkedQuestionSourceId = Guid.NewGuid();
            linkedOnNestedRosterQuestionSourceId = Guid.NewGuid();

            interviewId = Guid.Parse("43333333333333333333333333333333");

            questionnaire =
                Create.Other.QuestionnaireDocumentWithOneChapter(children:
                    new IComposite[]
                    {
                        Create.Other.SingleQuestion(id: linkedQuestionId, linkedToQuestionId: linkedQuestionSourceId,
                            variable: "link"),
                        Create.Other.SingleQuestion(id: linkedOnNestedRosterQuestionId,
                            linkedToQuestionId: linkedOnNestedRosterQuestionSourceId,
                            variable: "nestedlink"),
                        Create.Other.Roster(rosterId: rosterId, variable: "ros",
                            children:
                                new IComposite[]
                                {
                                    Create.Other.Roster(rosterId: nestedRosterId, variable: "ros1", children:
                                        new[]
                                        {
                                            Create.Other.TextQuestion(questionId: linkedOnNestedRosterQuestionSourceId,
                                                variable: "txt_nested_source")
                                        }),
                                    Create.Other.TextQuestion(questionId: linkedQuestionSourceId, variable: "txt_source")
                                })
                    });

            interview = CreateInterviewData(interviewId);

            AddInterviewLevel(interview, new ValueVector<Guid> { rosterId }, Create.Other.RosterVector(0).ToArray(),
                rosterTitles: new Dictionary<Guid, string>() { { rosterId, "roster0" } },
                answeredQuestions: new Dictionary<Guid, object>() { { linkedQuestionSourceId, "ros0_txt" } });

            AddInterviewLevel(interview, new ValueVector<Guid> { rosterId, nestedRosterId }, Create.Other.RosterVector(0, 0).ToArray(), rosterTitles: new Dictionary<Guid, string>() { { nestedRosterId, "roster01" } },
                answeredQuestions: new Dictionary<Guid, object>() { { linkedOnNestedRosterQuestionSourceId, "ros00_txt" } });

            AddInterviewLevel(interview, new ValueVector<Guid> { rosterId, nestedRosterId }, Create.Other.RosterVector(0, 1).ToArray(), rosterTitles: new Dictionary<Guid, string>() { { nestedRosterId, "roster02" } },
                answeredQuestions: new Dictionary<Guid, object>() { { linkedOnNestedRosterQuestionSourceId, "ros01_txt" } });

            AddInterviewLevel(interview, new ValueVector<Guid> { rosterId }, Create.Other.RosterVector(1).ToArray(), rosterTitles: new Dictionary<Guid, string>() { { rosterId, "roster1" } }, answeredQuestions: new Dictionary<Guid, object>() { { linkedQuestionSourceId, "ros1_txt" } });

            AddInterviewLevel(interview, new ValueVector<Guid> { rosterId, nestedRosterId }, Create.Other.RosterVector(1, 0).ToArray(), rosterTitles: new Dictionary<Guid, string>() { { nestedRosterId, "roster11" } },
                answeredQuestions: new Dictionary<Guid, object>() { { linkedOnNestedRosterQuestionSourceId, "ros10_txt" } });
            AddInterviewLevel(interview, new ValueVector<Guid> { rosterId, nestedRosterId }, Create.Other.RosterVector(1, 1).ToArray(), rosterTitles: new Dictionary<Guid, string>() { { nestedRosterId, "roster12" } },
                answeredQuestions: new Dictionary<Guid, object>() { { linkedOnNestedRosterQuestionSourceId, "ros11_txt" } });

            interview.Levels["#"].QuestionsSearchCache.Add(linkedQuestionId, Create.Other.InterviewQuestion(linkedQuestionId, Create.Other.RosterVector(1).ToArray()));

            user = Mock.Of<UserDocument>();

            merger = CreateMerger(questionnaire);
        };

        Because of = () =>
            mergeResult = merger.Merge(interview, questionnaire, user.GetUseLight(), null, null);


        It should_question_has_2_options = () =>
            GetQuestion(mergeResult, linkedQuestionId, new decimal[0]).Options.Select(x => x.Label).ShouldContainOnly("ros0_txt", "ros1_txt");

        It should_first_option_of_linked_question_be_checked = () =>
            GetQuestion(mergeResult, linkedQuestionId, new decimal[0]).AnswerString.ShouldEqual("1");

        It should_answer_on_linked_question_be_roster_vector = () =>
             GetQuestion(mergeResult, linkedQuestionId, new decimal[0]).Answer.ShouldEqual(Create.Other.RosterVector(1));

        It should_question_linked_on_nested_roster_has_4_options = () =>
            GetQuestion(mergeResult, linkedOnNestedRosterQuestionId, new decimal[0]).Options.Select(x => x.Label).ShouldContainOnly("roster0: ros00_txt", "roster0: ros01_txt", "roster1: ros10_txt", "roster1: ros11_txt");


        private static InterviewDataAndQuestionnaireMerger merger;
        private static InterviewDetailsView mergeResult;
        private static InterviewData interview;
        private static QuestionnaireDocument questionnaire;
        private static UserDocument user;

        private static Guid linkedQuestionId;
        private static Guid linkedOnNestedRosterQuestionId;
        private static Guid rosterId;
        private static Guid nestedRosterId;
        private static Guid interviewId;

        private static Guid linkedQuestionSourceId;
        private static Guid linkedOnNestedRosterQuestionSourceId;
    }
}