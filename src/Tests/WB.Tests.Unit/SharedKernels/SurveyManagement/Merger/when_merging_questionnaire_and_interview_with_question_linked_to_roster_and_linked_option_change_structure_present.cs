﻿using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Moq;
using WB.Core.BoundedContexts.Headquarters.Views;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.SharedKernels.DataCollection.ValueObjects;
using WB.Core.SharedKernels.DataCollection.Views;
using WB.Tests.Abc;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.Merger
{
    internal class when_merging_questionnaire_and_interview_with_question_linked_to_roster_and_linked_option_change_structure_present : InterviewDataAndQuestionnaireMergerTestContext
    {
        Establish context = () =>
        {
            linkedQuestionId = Guid.Parse("10000000000000000000000000000000");
            linkedOnNestedRosterQuestionId = Guid.Parse("33333333333333333333333333333333");

            rosterId = Guid.Parse("44444444444444444444444444444444");
            nestedRosterId = Guid.Parse("20000000000000000000000000000000");

            interviewId = Guid.Parse("43333333333333333333333333333333");

            questionnaire =
                Create.Entity.QuestionnaireDocumentWithOneChapter(children:
                    new IComposite[]
                    {
                        Create.Entity.SingleQuestion(id: linkedQuestionId, linkedToRosterId: rosterId, variable: "link"),
                        Create.Entity.SingleQuestion(id: linkedOnNestedRosterQuestionId, linkedToRosterId: nestedRosterId,
                            variable: "nestedlink"),
                        Create.Entity.Roster(rosterId: rosterId, variable: "ros",
                            children: new[] {Create.Entity.Roster(rosterId: nestedRosterId, variable: "ros1")})
                    });

            interview = CreateInterviewData(interviewId);

            AddInterviewLevel(interview, new ValueVector<Guid> { rosterId }, Create.Entity.RosterVector(0).CoordinatesAsDecimals.ToArray(), 
                rosterTitles: new Dictionary<Guid, string>() { { rosterId, "roster0" } });

            AddInterviewLevel(interview, new ValueVector<Guid> { rosterId, nestedRosterId }, Create.Entity.RosterVector(0, 0).CoordinatesAsDecimals.ToArray(), 
                rosterTitles: new Dictionary<Guid, string>() { { nestedRosterId, "roster01" } });

            AddInterviewLevel(interview, new ValueVector<Guid> { rosterId, nestedRosterId }, Create.Entity.RosterVector(0, 1).CoordinatesAsDecimals.ToArray(),
                rosterTitles: new Dictionary<Guid, string>() { { nestedRosterId, "roster02" } });

            AddInterviewLevel(interview, new ValueVector<Guid> { rosterId }, Create.Entity.RosterVector(1).CoordinatesAsDecimals.ToArray(), 
                rosterTitles: new Dictionary<Guid, string>() { { rosterId, "roster1" } });

            AddInterviewLevel(interview, new ValueVector<Guid> { rosterId, nestedRosterId }, Create.Entity.RosterVector(1, 0).CoordinatesAsDecimals.ToArray(), 
                rosterTitles: new Dictionary<Guid, string>() { { nestedRosterId, "roster11" } });
            AddInterviewLevel(interview, new ValueVector<Guid> { rosterId, nestedRosterId }, Create.Entity.RosterVector(1, 1).CoordinatesAsDecimals.ToArray(),
                rosterTitles: new Dictionary<Guid, string>() { { nestedRosterId, "roster12" } });

            interview.Levels["#"].QuestionsSearchCache.Add(linkedQuestionId, Create.Entity.InterviewQuestion(linkedQuestionId, Create.Entity.RosterVector(1).ToArray()));

            user = Mock.Of<UserView>();
            interviewLinkedQuestionOptions =
                Create.Entity.InterviewLinkedQuestionOptions(Create.Entity.ChangedLinkedOptions(linkedQuestionId,
                    options:
                        new[]
                        {
                            Create.Entity.RosterVector(0), Create.Entity.RosterVector(1)
                        }),
                    Create.Entity.ChangedLinkedOptions(linkedOnNestedRosterQuestionId,
                        options:
                            new[]
                            {
                                Create.Entity.RosterVector(0, 0), Create.Entity.RosterVector(0, 1), Create.Entity.RosterVector(1, 0),
                                Create.Entity.RosterVector(1, 1)
                            }));

            merger = CreateMerger(questionnaire);
        };

        Because of = () =>
            mergeResult = merger.Merge(interview, questionnaire, user.GetUseLight(), interviewLinkedQuestionOptions, null);


        It should_question_has_2_options = () =>
            GetQuestion(mergeResult, linkedQuestionId, new decimal[0]).Options.Select(x => x.Label).ShouldContainOnly("roster0", "roster1");

        It should_first_option_of_linked_question_be_checked = () =>
            GetQuestion(mergeResult, linkedQuestionId, new decimal[0]).AnswerString.ShouldEqual("1");

        It should_answer_on_linked_question_be_roster_vector = () =>
             GetQuestion(mergeResult, linkedQuestionId, new decimal[0]).Answer.ShouldEqual(Create.Entity.RosterVector(1));

        It should_question_linked_on_nested_roster_has_4_options = () =>
            GetQuestion(mergeResult, linkedOnNestedRosterQuestionId, new decimal[0]).Options.Select(x => x.Label).ShouldContainOnly("roster0: roster01", "roster0: roster02", "roster1: roster11", "roster1: roster12");


        private static InterviewDataAndQuestionnaireMerger merger;
        private static InterviewDetailsView mergeResult;
        private static InterviewData interview;
        private static QuestionnaireDocument questionnaire;
        private static InterviewLinkedQuestionOptions interviewLinkedQuestionOptions;
        private static UserView user;

        private static Guid linkedQuestionId;
        private static Guid linkedOnNestedRosterQuestionId;
        private static Guid rosterId;
        private static Guid nestedRosterId;
        private static Guid interviewId;
    }
}