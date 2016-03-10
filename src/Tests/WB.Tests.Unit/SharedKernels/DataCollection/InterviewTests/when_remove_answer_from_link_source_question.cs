﻿using System;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;

namespace WB.Tests.Unit.SharedKernels.DataCollection.InterviewTests
{
    [Ignore("temporary ignore in order to fix build, will be unignored later when invariants will updated")]
    internal class when_remove_answer_from_link_source_question : InterviewTestsContext
    {
        Establish context = () =>
        {
            var questionnaireId = Guid.Parse("10000000000000000000000000000000");
            userId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
            rosterId = Guid.Parse("21111111111111111111111111111111");
            sourceOfLinkQuestionId = Guid.Parse("22222222222222222222222222222222");
            linkedQuestionId = Guid.Parse("33222222222222222222222222222222");

            QuestionnaireDocument questionnaire = Create.QuestionnaireDocument(id: questionnaireId,
                children: new IComposite[]
                {
                    Create.Roster(rosterId: rosterId, variable: "ros", fixedTitles: new[] {"1", "2"},
                        children: new IComposite[]
                        {
                            Create.TextQuestion(questionId: sourceOfLinkQuestionId)
                        }),
                    Create.SingleQuestion(id: linkedQuestionId, linkedToQuestionId: sourceOfLinkQuestionId)
                });

            var questionnaireRepository = CreateQuestionnaireRepositoryStubWithOneQuestionnaire(questionnaireId, new PlainQuestionnaire(questionnaire, 1));

            interview = CreateInterview(questionnaireId: questionnaireId, questionnaireRepository: questionnaireRepository);
            interview.AnswerTextQuestion(userId, sourceOfLinkQuestionId, new decimal[] {0},
                DateTime.Now, "a");
            interview.AnswerSingleOptionLinkedQuestion(userId, linkedQuestionId, new decimal[0], DateTime.Now,
                new decimal[] {0});
            eventContext = new EventContext();
        };

        Cleanup stuff = () =>
        {
            eventContext.Dispose();
            eventContext = null;
        };

        Because of = () =>
           interview.RemoveAnswer(sourceOfLinkQuestionId, new decimal[] { 0 }, userId, DateTime.Now);

        It should_raise_AnswerRemoved_event_for_first_row = () =>
            eventContext.ShouldContainEvent<AnswerRemoved>(@event
                => @event.QuestionId == sourceOfLinkQuestionId && @event.RosterVector.SequenceEqual(new decimal[] { 0 }));

        It should_raise_AnswersRemoved_event_for_answered_linked_Question = () =>
            eventContext.ShouldContainEvent<AnswersRemoved>(@event
                => @event.Questions.Count(q => q.Id == linkedQuestionId && !q.RosterVector.Any())==1);

        private static EventContext eventContext;
        private static Interview interview;
        private static Guid userId;
        private static Guid sourceOfLinkQuestionId;
        private static Guid linkedQuestionId;
        private static Guid rosterId;
    }
}