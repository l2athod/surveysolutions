using System;
using FluentAssertions;
using Main.Core.Entities.SubEntities;
using NUnit.Framework;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Tests.Unit.Designer.BoundedContexts.QuestionnaireTests;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.ReplaceTextHanderTests
{
    internal class when_replacing_texts_for_different_user : QuestionnaireTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            var responsibleId = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
            questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: chapterId);

            questionnaire.AddMultiOptionQuestion(questionId, chapterId, responsibleId,
                title: $"filter with {searchFor}");
            BecauseOf();
        }

        private void BecauseOf() => exception = Assert.Throws<QuestionnaireException>(() => 
                questionnaire.ReplaceTexts(Create.Command.ReplaceTextsCommand(searchFor, replaceWith, userId: Guid.NewGuid())));

        [NUnit.Framework.Test] public void should_not_allow_to_edit_questionnaire () => exception.ErrorType.Should().Be(DomainExceptionType.DoesNotHavePermissionsForEdit);

        [NUnit.Framework.Test] public void should_not_change_questionnaire () => 
            questionnaire.QuestionnaireDocument.Find<IQuestion>(questionId).QuestionText.Should().Be($"filter with {searchFor}");

        static Questionnaire questionnaire;

        const string searchFor = "to_search";
        const string replaceWith = "replaced";
        static readonly Guid chapterId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        static readonly Guid questionId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        static QuestionnaireException exception;
    }
}
