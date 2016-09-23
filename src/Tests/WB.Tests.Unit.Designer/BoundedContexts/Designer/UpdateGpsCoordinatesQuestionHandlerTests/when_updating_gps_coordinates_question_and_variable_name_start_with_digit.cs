using System;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Exceptions;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.QuestionnaireDto;
using WB.Core.SharedKernels.QuestionnaireEntities;
using WB.Tests.Unit.Designer.BoundedContexts.QuestionnaireTests;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.UpdateGpsCoordinatesQuestionHandlerTests
{
    internal class when_updating_gps_coordinates_question_and_variable_name_start_with_digit : QuestionnaireTestsContext
    {
        Establish context = () =>
        {
            questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            questionnaire.AddGroup(new NewGroupAdded { PublicKey = chapterId });
            questionnaire.AddQuestion(Create.Event.NewQuestionAdded(
                    publicKey: questionId,
                    groupPublicKey: chapterId,
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
                questionnaire.UpdateGpsCoordinatesQuestion(
                    questionId: questionId,
                    title: title,
                    variableName: variableNameStartsWithDigit,
                    variableLabel: null,
                    isPreFilled: false,
                    scope: scope,
                    enablementCondition: enablementCondition,
                    hideIfDisabled: false,
                    instructions: instructions,
                    responsibleId: responsibleId, validationConditions: new System.Collections.Generic.List<WB.Core.SharedKernels.QuestionnaireEntities.ValidationCondition>(), properties: Create.QuestionProperties()));

        It should_throw_QuestionnaireException = () =>
            exception.ShouldBeOfExactType<QuestionnaireException>();

        It should_throw_exception_with_message_containting__variable__start__digit__ = () =>
            new[] { "variable", "start", "digit" }.ShouldEachConformTo(
                keyword => exception.Message.ToLower().Contains(keyword));


        private static Questionnaire questionnaire;
        private static Exception exception;
        private static Guid questionId = Guid.Parse("11111111111111111111111111111111");
        private static Guid chapterId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        private static Guid responsibleId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
        private static string variableNameStartsWithDigit = "1var";
        private static string title = "title";
        private static string instructions = "intructions";
        private static QuestionScope scope = QuestionScope.Interviewer;
        private static string enablementCondition = null;
    }
}