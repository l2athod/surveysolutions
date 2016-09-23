﻿using System;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.QuestionnaireDto;

namespace WB.Tests.Unit.Designer.BoundedContexts.QuestionnaireTests
{
    internal class when_moving_group_from_roster_with_roster_title_question_to_group_in_roster_by_roster_size_question : QuestionnaireTestsContext
    {
        Establish context = () =>
        {
            questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            questionnaire.AddGroup(new NewGroupAdded { PublicKey = chapterId });
            questionnaire.AddQuestion(Create.Event.NumericQuestionAdded(
                publicKey : rosterSizeQuestionId,
                groupPublicKey : chapterId,
                isInteger : true
            ));
            questionnaire.AddGroup(new NewGroupAdded { PublicKey = targetRosterId, ParentGroupPublicKey = chapterId});
            questionnaire.MarkGroupAsRoster(new GroupBecameARoster(responsibleId, targetRosterId));
            questionnaire.ChangeRoster(new RosterChanged(responsibleId: responsibleId, groupId: targetRosterId)
                {
                    RosterSizeQuestionId = rosterSizeQuestionId,
                    RosterSizeSource = RosterSizeSourceType.Question,
                    FixedRosterTitles = null,
                    RosterTitleQuestionId = null
                });
            questionnaire.AddGroup(new NewGroupAdded { PublicKey = groupFromTargetRosterId, ParentGroupPublicKey = targetRosterId });

            questionnaire.AddGroup(new NewGroupAdded { PublicKey = sourceRosterId, ParentGroupPublicKey = chapterId });
            questionnaire.MarkGroupAsRoster(new GroupBecameARoster(responsibleId, sourceRosterId));
            questionnaire.ChangeRoster(new RosterChanged(responsibleId: responsibleId, groupId: sourceRosterId){
                    RosterSizeQuestionId = rosterSizeQuestionId,
                    RosterSizeSource = RosterSizeSourceType.Question,
                    FixedRosterTitles = null,
                    RosterTitleQuestionId = rosterTitleQuestionId
                });

            questionnaire.AddGroup(new NewGroupAdded { PublicKey = groupFromSourceRosterId, ParentGroupPublicKey = sourceRosterId });
            questionnaire.AddQuestion(Create.Event.NumericQuestionAdded(
                publicKey : rosterTitleQuestionId,
                groupPublicKey : groupFromSourceRosterId
            ));
        };

        Because of = () => questionnaire.MoveGroup(groupFromSourceRosterId, groupFromTargetRosterId, 0, responsibleId);


        It should_contains_group = () =>
            questionnaire.QuestionnaireDocument.Find<IGroup>(groupFromSourceRosterId).ShouldNotBeNull();

        It should_contains_group_with_GroupId_specified = () =>
            questionnaire.QuestionnaireDocument.Find<IGroup>(groupFromSourceRosterId)
                .PublicKey.ShouldEqual(groupFromSourceRosterId);

        It should_contains_group_with_ParentGroupId_specified = () =>
            questionnaire.QuestionnaireDocument.Find<IGroup>(groupFromSourceRosterId)
                .GetParent().PublicKey.ShouldEqual(groupFromTargetRosterId);

        private static Questionnaire questionnaire;
        private static Guid responsibleId = Guid.Parse("DDDD0000000000000000000000000000");
        private static Guid targetRosterId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        private static Guid sourceRosterId = Guid.Parse("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
        private static Guid groupFromSourceRosterId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
        private static Guid groupFromTargetRosterId = Guid.Parse("22222222222222222222222222222222");
        private static Guid rosterTitleQuestionId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
        private static Guid rosterSizeQuestionId = Guid.Parse("11111111111111111111111111111111");
        private static Guid chapterId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
    }
}