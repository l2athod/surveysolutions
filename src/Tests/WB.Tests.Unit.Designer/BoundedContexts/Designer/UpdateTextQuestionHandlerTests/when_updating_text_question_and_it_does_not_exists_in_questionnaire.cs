using System;
using FluentAssertions;
using Main.Core.Entities.SubEntities;
using NUnit.Framework;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Base;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Question;
using WB.Tests.Unit.Designer.BoundedContexts.QuestionnaireTests;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.UpdateTextQuestionHandlerTests
{
    internal class when_updating_text_question_and_it_does_not_exists_in_questionnaire : QuestionnaireTestsContext
    {
        [NUnit.Framework.Test] public void should_throw_QuestionnaireException () {
            questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            questionnaire.AddGroup(chapterId, responsibleId:responsibleId);
            questionnaire.AddQRBarcodeQuestion(questionId,
                    chapterId,
                        responsibleId,
                        title: "old title",
                        variableName: "old_variable_name",
                        instructions: "old instructions",
                        enablementCondition: "old condition");
            BecauseOf();

            exception.Message.ToLower().ToSeparateWords().Should().Contain(new[] { "question", "can't", "found" });
        }

        private void BecauseOf() =>
             exception = Assert.Throws<QuestionnaireException>(() =>
                 questionnaire.UpdateTextQuestion(
                     new UpdateTextQuestion(
                         questionnaire.Id,
                         notExistingQuestionId,
                         responsibleId,
                         new CommonQuestionParameters() { Title = title, VariableName = variableName },
                         null, scope, false,
                         new System.Collections.Generic.List<WB.Core.SharedKernels.QuestionnaireEntities.ValidationCondition>())));

        private static Questionnaire questionnaire;
        private static Exception exception;
        private static Guid questionId = Guid.Parse("11111111111111111111111111111111");
        private static Guid notExistingQuestionId = Guid.Parse("22222222222222222222222222222222");
        private static Guid chapterId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        private static Guid responsibleId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
        private static string variableName = "qr_barcode_question";
        private static string title = "title";
        private static QuestionScope scope = QuestionScope.Interviewer;
        
    }
}
