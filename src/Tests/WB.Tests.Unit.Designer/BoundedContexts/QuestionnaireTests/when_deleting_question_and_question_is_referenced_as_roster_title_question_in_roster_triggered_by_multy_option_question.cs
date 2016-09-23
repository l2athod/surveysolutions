﻿using System;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.QuestionnaireDto;

namespace WB.Tests.Unit.Designer.BoundedContexts.QuestionnaireTests
{
    internal class when_deleting_question_and_question_is_referenced_as_roster_title_question_in_roster_triggered_by_multy_option_question : QuestionnaireTestsContext
    {
        Establish context = () =>
        {
            responsibleId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
            var chapterId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
            rosterSizeQuestionId = Guid.Parse("11111111111111111111111111111111");
            rosterTitleQuestionId = Guid.Parse("21111111111111111111111111111111");
            var rosterId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            rosterTitle = "Roster Title";

            questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            questionnaire.AddGroup(new NewGroupAdded { PublicKey = chapterId });
            questionnaire.AddQuestion(Create.Event.NewQuestionAdded(
                publicKey: rosterSizeQuestionId,
                groupPublicKey: chapterId,
                questionType: QuestionType.MultyOption
            ));
            questionnaire.AddGroup(new NewGroupAdded { PublicKey = rosterId, GroupText = rosterTitle });
            questionnaire.MarkGroupAsRoster(new GroupBecameARoster(responsibleId, rosterId));
            questionnaire.ChangeRoster(new RosterChanged(responsibleId, rosterId)
                {
                    RosterSizeQuestionId = null,
                    RosterSizeSource = RosterSizeSourceType.Question,
                    FixedRosterTitles = null,
                    RosterTitleQuestionId = rosterSizeQuestionId
                });
            questionnaire.AddQuestion(Create.Event.NewQuestionAdded(
                publicKey: rosterTitleQuestionId,
                groupPublicKey: rosterId,
                questionType: QuestionType.Text
            ));
        };


        Because of = () =>
                questionnaire.DeleteQuestion(rosterTitleQuestionId, responsibleId);

        It should_doesnt_contain_question = () =>
          questionnaire.QuestionnaireDocument.Find<IQuestion>(rosterTitleQuestionId).ShouldBeNull();

        private static string rosterTitle;
        private static Questionnaire questionnaire;
        private static Guid rosterSizeQuestionId;
        private static Guid rosterTitleQuestionId;
        private static Guid responsibleId;
    }
}
