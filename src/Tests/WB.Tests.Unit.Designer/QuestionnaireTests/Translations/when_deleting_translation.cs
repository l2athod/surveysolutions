using System;
using FluentAssertions;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Translations;


namespace WB.Tests.Unit.Designer.BoundedContexts.QuestionnaireTests.Translations
{
    internal class when_deleting_translation : QuestionnaireTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            questionnaire = CreateQuestionnaire(questionnaireId: questionnaireId, responsibleId: responsibleId);
            questionnaire.AddOrUpdateTranslation(Create.Command.AddOrUpdateTranslation(questionnaireId, translationId, "", responsibleId));

            deleteTranslation = Create.Command.DeleteTranslation(questionnaireId, translationId, responsibleId);
            BecauseOf();
        }

        private void BecauseOf() => questionnaire.DeleteTranslation(deleteTranslation);

        [NUnit.Framework.Test] public void should_doesnt_contain_Translation_with_EntityId_specified () =>
            questionnaire.QuestionnaireDocument.Translations.Should().NotContain(t => t.Id == translationId);

        private static DeleteTranslation deleteTranslation;
        private static Questionnaire questionnaire;
        private static readonly Guid responsibleId = Guid.Parse("DDDD0000000000000000000000000000");
        private static readonly Guid questionnaireId = Guid.Parse("11111111111111111111111111111111");
        private static readonly Guid translationId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
    }
}
