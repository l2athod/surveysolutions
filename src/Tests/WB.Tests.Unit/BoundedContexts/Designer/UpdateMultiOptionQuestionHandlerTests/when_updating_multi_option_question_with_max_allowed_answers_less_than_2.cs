using System;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Main.Core.Events.Questionnaire;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Events.Questionnaire;
using WB.Core.BoundedContexts.Designer.Exceptions;
using WB.Tests.Unit.BoundedContexts.Designer.QuestionnaireTests;

namespace WB.Tests.Unit.BoundedContexts.Designer.UpdateMultiOptionQuestionHandlerTests
{
    internal class when_updating_multi_option_question_with_max_allowed_answers_less_than_2 : QuestionnaireTestsContext
    {
        Establish context = () =>
        {
            questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            questionnaire.Apply(new NewGroupAdded { PublicKey = parentGroupId });
            questionnaire.Apply(Create.Event.NewQuestionAdded(
                publicKey: questionId,
                groupPublicKey: parentGroupId,
                questionText: "old title",
                stataExportCaption: "old_variable_name",
                instructions: "old instructions",
                conditionExpression: "old condition",
                responsibleId: responsibleId,
                questionType: QuestionType.QRBarcode
                ));
        };

        Because of = () =>
            exception = Catch.Exception(() =>
                questionnaire.UpdateMultiOptionQuestion(
                    questionId: questionId,
                    title: "title",
                    variableName: "var",
                    variableLabel: null,
                    scope: QuestionScope.Interviewer,
                    enablementCondition: null,
                    hideIfDisabled: false,
                    instructions: null,
                    responsibleId: responsibleId,
                    options: new Option[]
                    {
                        new Option(Guid.NewGuid(), "1", "opt1Title"),
                        new Option(Guid.NewGuid(), "2", "opt2Title")
                    },
                    linkedToEntityId: null,
                    areAnswersOrdered: false,
                    maxAllowedAnswers: 1,
                    yesNoView: yesNoView, validationConditions: new System.Collections.Generic.List<WB.Core.SharedKernels.QuestionnaireEntities.ValidationCondition>()));


        It should_throw_QuestionnaireException = () =>
            exception.ShouldBeOfExactType<QuestionnaireException>();

        It should_throw_exception_with_message_containting__maximum_allowed_answers_should_be_more_than_one__ = () =>
            new[] { "maximum allowed answers for question should be more than one" }.ShouldEachConformTo(
                keyword => exception.Message.ToLower().Contains(keyword));

        private static Exception exception;
        private static Questionnaire questionnaire;
        private static Guid questionId = Guid.Parse("11111111111111111111111111111111");
        private static Guid responsibleId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
        private static Guid parentGroupId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        private static bool yesNoView = false;
    }
}