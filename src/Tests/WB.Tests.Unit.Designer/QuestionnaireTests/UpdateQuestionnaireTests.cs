﻿using System;
using NUnit.Framework;
using WB.Core.BoundedContexts.Designer.Aggregates;

namespace WB.Tests.Unit.Designer.BoundedContexts.QuestionnaireTests
{
    [TestFixture]
    internal class UpdateQuestionnaireTests : QuestionnaireTestsContext
    {
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t")]
        public void UpdateQuestionnaire_When_questionnaire_title_is_empty_or_contains_whitespaces_only_Then_throws_DomainException_with_type_QuestionnaireTitleRequired(string emptyTitle)
        {
            // arrange
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaire(responsibleId: responsibleId);

            // act
            TestDelegate act = () => questionnaire.UpdateQuestionnaire(Create.UpdateQuestionnaire(emptyTitle, false, responsibleId));

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.QuestionnaireTitleRequired));
        }

        [Test]
        public void UpdateQuestionnaire_When_questionnaire_title_is_not_empty_Then_raised_QuestionnaireUpdated_event_contains_questionnaire_title()
        {
            // arrange
            var nonEmptyTitle = "Title";
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaire(responsibleId: responsibleId);

            // act
            questionnaire.UpdateQuestionnaire(Create.UpdateQuestionnaire(nonEmptyTitle, false, responsibleId));

            // assert
            Assert.That(questionnaire.QuestionnaireDocument.Title, Is.EqualTo(nonEmptyTitle));
        }

        [Test]
        public void UpdateQuestionnaire_When_User_Doesnot_Have_Permissions_For_Edit_Questionnaire_Then_DomainException_should_be_thrown()
        {
            // arrange
            Questionnaire questionnaire = CreateQuestionnaire(responsibleId: Guid.NewGuid());

            // act
            TestDelegate act = () => questionnaire.UpdateQuestionnaire(Create.UpdateQuestionnaire("title", false, responsibleId: Guid.NewGuid()));
            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.DoesNotHavePermissionsForEdit));
        }

        [Test]
        public void UpdateQuestionnaire_When_User_is_admin_Then_DomainException_should_not_be_thrown()
        {
            // arrange
            Questionnaire questionnaire = CreateQuestionnaire(responsibleId: Guid.NewGuid());

            // act
            TestDelegate act = () => questionnaire.UpdateQuestionnaire(Create.UpdateQuestionnaire("title", false,
                responsibleId: Guid.NewGuid(), isResponsibleAdmin: true));
            // assert
            Assert.DoesNotThrow(act);
        }
    }
}
