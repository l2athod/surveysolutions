using System;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Main.Core.Events.Questionnaire;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Exceptions;
using WB.Tests.Unit.BoundedContexts.Designer.QuestionnaireTests;

namespace WB.Tests.Unit.BoundedContexts.Designer.UpdateCascadingComboboxOptionsHandlerTests
{
    internal class when_updating_cascading_combobox_options_and_user_dont_have_permission_to_execute_command : QuestionnaireTestsContext
    {
        Establish context = () =>
        {
            questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            questionnaire.Apply(new NewGroupAdded { PublicKey = chapterId });
            questionnaire.Apply(CreateNewQuestionAdded
            (
                publicKey : parentQuestionId,
                groupPublicKey : chapterId,
                questionType : QuestionType.SingleOption,
                questionText : "text",
                stataExportCaption : "var",
                responsibleId : responsibleId,
                answers : new Answer[]
                {
                    new Answer { AnswerText = "Option 1", AnswerValue = "1" },
                    new Answer { AnswerText = "Option 2", AnswerValue = "2" }
                }
            ));
            questionnaire.Apply(CreateNewQuestionAdded(
                publicKey : questionId,
                groupPublicKey : chapterId,
                questionType : QuestionType.SingleOption,
                questionText : "text",
                stataExportCaption : "var",
                isFilteredCombobox : false,
                responsibleId : responsibleId,
                cascadeFromQuestionId : parentQuestionId
           ));
        };

        Because of = () =>
            exception = Catch.Exception(() =>
                questionnaire.UpdateCascadingComboboxOptions(questionId: questionId, responsibleId: notExistingResponsibleId, options: options));

        It should_throw_QuestionnaireException = () =>
            exception.ShouldBeOfExactType<QuestionnaireException>();

        It should_throw_exception_with_message_containting__dont__have__permissions__ = () =>
             new[] { "don't", "have", "permissions" }.ShouldEachConformTo(
                    keyword => exception.Message.ToLower().Contains(keyword));


        private static Questionnaire questionnaire;
        private static Exception exception;
        private static Guid questionId = Guid.Parse("11111111111111111111111111111111");
        private static Guid parentQuestionId = Guid.Parse("22222222222222222222222222222222");
        private static Guid chapterId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        private static Guid responsibleId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
        private static Guid notExistingResponsibleId = Guid.Parse("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
        private static Option[] options = new[] { new Option(Guid.NewGuid(), "1", "Option 1"), new Option(Guid.NewGuid(), "2", "Option 2") };
    }
}