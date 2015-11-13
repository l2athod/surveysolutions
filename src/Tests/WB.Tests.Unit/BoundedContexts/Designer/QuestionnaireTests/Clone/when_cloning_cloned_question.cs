﻿using System;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Main.Core.Events.Questionnaire;
using Ncqrs.Spec;
using WB.Core.BoundedContexts.Designer.Aggregates;

namespace WB.Tests.Unit.BoundedContexts.Designer.QuestionnaireTests.Clone
{
    internal class when_cloning_cloned_question : QuestionnaireTestsContext
    {
        Establish context = () =>
        {
            responsibleId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            questionnaire = CreateQuestionnaire(responsibleId);
            var groupId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
            questionnaire.Apply(new NewGroupAdded { PublicKey = groupId });
            sourceQuestionId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
            var grandQuestionId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCA");

            var questionCloned = CreateQuestionCloned
            (
                publicKey : sourceQuestionId,
                sourceQuestionId: grandQuestionId,
                targetIndex:0,
                questionText : "text",
                questionType : QuestionType.TextList,
                stataExportCaption : "varrr",
                variableLabel : "varlabel",
                maxAnswerCount : 5
            );
            questionnaire.Apply(questionCloned);

            eventContext = new EventContext();
        };

        Because of = () => questionnaire.CloneQuestionById(sourceQuestionId, responsibleId, targetId);

        It should_clone_MaxAnswerCount_value = () => eventContext.ShouldContainEvent<QuestionCloned>(x => x.PublicKey == targetId && x.MaxAnswerCount == 5);

        static Questionnaire questionnaire;
        static Guid sourceQuestionId;
        static EventContext eventContext;
        static Guid responsibleId;
        static Guid targetId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
    }
}

