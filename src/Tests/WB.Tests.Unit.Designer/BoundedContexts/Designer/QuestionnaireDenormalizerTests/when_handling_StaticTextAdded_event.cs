using System;
using FluentAssertions;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.StaticText;


namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.QuestionnaireDenormalizerTests
{
    internal class when_handling_StaticTextAdded_event : QuestionnaireDenormalizerTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {

            questionnaireView = CreateQuestionnaireDocument(new[]
            {
                CreateGroup(groupId: parentId)
            }, creatorId);

            questionnaire = CreateQuestionnaireDenormalizer(questionnaire: questionnaireView);
            BecauseOf();
        }

        private void BecauseOf() =>
            questionnaire.AddStaticTextAndMoveIfNeeded(new AddStaticText(questionnaireView.PublicKey, entityId, text, creatorId, parentId));

        [NUnit.Framework.Test] public void should__static_text_be_in_questionnaire_document_view () =>
            GetExpectedStaticText().Should().NotBeNull();

        [NUnit.Framework.Test] public void should_PublicKey_be_equal_to_entityId () =>
           GetExpectedStaticText().PublicKey.Should().Be(entityId);

        [NUnit.Framework.Test] public void should_parent_group_exists_in_questionnaire () =>
           questionnaireView.Find<IGroup>(parentId).Should().NotBeNull();

        [NUnit.Framework.Test] public void should_parent_group_contains_static_text () =>
           questionnaireView.Find<IGroup>(parentId).Children[0].PublicKey.Should().Be(entityId);

        [NUnit.Framework.Test] public void should_text_be_equal_specified_text () =>
            GetExpectedStaticText().Text.Should().Be(text);

        private static IStaticText GetExpectedStaticText()
        {
            return GetEntityById<IStaticText>(document: questionnaireView, entityId: entityId);
        }

        private static QuestionnaireDocument questionnaireView;
        private static Questionnaire questionnaire;
        private static Guid entityId = Guid.Parse("11111111111111111111111111111111");
        private static Guid parentId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");

        private static Guid creatorId = Guid.Parse("2AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        private static string text = "some text";
    }
}