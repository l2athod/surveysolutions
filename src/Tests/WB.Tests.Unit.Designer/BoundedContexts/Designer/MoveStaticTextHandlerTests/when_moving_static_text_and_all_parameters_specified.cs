using System;
using FluentAssertions;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.StaticText;

using WB.Tests.Unit.Designer.BoundedContexts.QuestionnaireTests;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.MoveStaticTextHandlerTests
{
    internal class when_moving_static_text_and_all_parameters_specified : QuestionnaireTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            questionnaire.AddGroup(chapterId, responsibleId:responsibleId);
            questionnaire.AddStaticTextAndMoveIfNeeded(new AddStaticText(questionnaire.Id, entityId, "title", responsibleId, chapterId));
            questionnaire.AddGroup(targetEntityId, responsibleId: responsibleId);
            BecauseOf();
        }

        private void BecauseOf() =>            
                questionnaire.MoveStaticText(entityId: entityId, responsibleId: responsibleId, targetEntityId: targetEntityId, targetIndex: targetIndex);


        [NUnit.Framework.Test] public void should_moved_statictext_to_new_group_with_PublicKey_specified () =>
            questionnaire.QuestionnaireDocument.Find<IStaticText>(entityId).GetParent().PublicKey.Should().Be(targetEntityId);

        [NUnit.Framework.Test] public void should_moved_statictext_to_new_group_with_TargetIndex_specified () =>
            questionnaire.QuestionnaireDocument.Find<IStaticText>(entityId).GetParent().Children[targetIndex].PublicKey.Should().Be(entityId);
        
        private static Questionnaire questionnaire;
        private static Guid entityId = Guid.Parse("11111111111111111111111111111112");
        private static Guid targetEntityId = Guid.Parse("22222222222222222222222222222222");
        private static Guid chapterId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        private static Guid responsibleId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
        private static int targetIndex = 0;
        
    }
}