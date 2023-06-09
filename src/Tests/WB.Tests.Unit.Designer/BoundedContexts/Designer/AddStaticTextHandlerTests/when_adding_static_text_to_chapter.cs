using System;
using FluentAssertions;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.StaticText;
using WB.Tests.Unit.Designer.BoundedContexts.QuestionnaireTests;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.AddStaticTextHandlerTests
{
    internal class when_adding_static_text_to_chapter : QuestionnaireTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            questionnaire.AddGroup(chapterId, responsibleId: responsibleId);
            BecauseOf();
        }

        private void BecauseOf() =>
                questionnaire.AddStaticTextAndMoveIfNeeded(
                    new AddStaticText(questionnaire.Id, entityId, text, responsibleId, chapterId, index));


        [NUnit.Framework.Test] public void should_raise_StaticTextAdded_event_with_EntityId_specified () =>
            questionnaire.QuestionnaireDocument.Find<IStaticText>(entityId).Should().NotBeNull();

        [NUnit.Framework.Test] public void should_raise_StaticTextAdded_event_with_ParentId_specified () =>
            questionnaire.QuestionnaireDocument.Find<IStaticText>(entityId).GetParent().PublicKey.Should().Be(chapterId);

        [NUnit.Framework.Test] public void should_raise_StaticTextAdded_event_with_Text_specified () =>
            questionnaire.QuestionnaireDocument.Find<IStaticText>(entityId).Text.Should().Be(text);


        private static Questionnaire questionnaire;
        private static Guid entityId = Guid.Parse("11111111111111111111111111111112");
        private static Guid chapterId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        private static Guid responsibleId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
        private static string text = "some text";
        private static int index = 5;
    }
}