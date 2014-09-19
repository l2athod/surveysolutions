﻿using System;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Main.Core.Events.Questionnaire;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Events.Questionnaire;
using WB.Core.BoundedContexts.Designer.Exceptions;
using WB.Core.BoundedContexts.Designer.Tests.QuestionnaireTests;

namespace WB.Core.BoundedContexts.Designer.Tests.AddSingleOptionQuestionHandlerTests
{
    internal class when_adding_single_option_question_with_linkedQuestion_and_supervisor_scope : QuestionnaireTestsContext
    {
        Establish context = () =>
        {
            questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            questionnaire.Apply(new NewGroupAdded { PublicKey = chapterId });
            questionnaire.Apply(new NewGroupAdded { PublicKey = rosterId, ParentGroupPublicKey = chapterId });
            questionnaire.Apply(new GroupBecameARoster(responsibleId, rosterId));
            questionnaire.Apply(new NewGroupAdded { PublicKey = groupFromRosterId, ParentGroupPublicKey = rosterId });
            questionnaire.Apply(new NewQuestionAdded
            {
                PublicKey = linkedToQuestionId,
                GroupPublicKey = rosterId,
                QuestionType = QuestionType.Text,
                QuestionText = "text question",
                StataExportCaption = "source_of_linked_question"
            });
        };

        Because of = () =>
            exception = Catch.Exception(() =>
                questionnaire.AddSingleOptionQuestion(
                    questionId: questionId,
                    parentGroupId: groupFromRosterId,
                    title: title,
                    variableName: variableName,
                    variableLabel: null,
                    isMandatory: isMandatory,
                    isPreFilled: isPrefilled,
                    scope: QuestionScope.Supervisor,
                    enablementCondition: enablementCondition,
                    validationExpression: validationExpression,
                    validationMessage: validationMessage,
                    instructions: instructions,
                    responsibleId: responsibleId,
                    options: null,
                    linkedToQuestionId: linkedToQuestionId,
                    cascadeFromQuestionId: cascadeFromQuestionId,
                    isFilteredCombobox: isFilteredCombobox));

        It should_throw_QuestionnaireException = () =>
            exception.ShouldBeOfExactType<QuestionnaireException>();

        It should_throw_exception_with_message_containting_linked_categorical_questions_cannot_be_filled_by_supervisor_ = () =>
            new[] { "linked categorical questions cannot be filled by supervisor"}.ShouldEachConformTo(
                keyword => exception.Message.ToLower().Contains(keyword));

        private static Exception exception;
        private static Questionnaire questionnaire;
        private static Guid questionId = Guid.Parse("11111111111111111111111111111111");
        private static Guid rosterId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        private static Guid groupFromRosterId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
        private static Guid responsibleId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
        private static Guid chapterId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        private static bool isPrefilled = false;
        private static bool isMandatory = false;
        private static string variableName = "single_var";
        private static string title = "title";
        private static string instructions = "intructions";
        private static string enablementCondition = "";
        private static string validationExpression = "";
        private static string validationMessage = "";
        private static Guid linkedToQuestionId = Guid.Parse("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
        private static Guid? cascadeFromQuestionId = null;
        private static bool isFilteredCombobox = false;
    }
}
