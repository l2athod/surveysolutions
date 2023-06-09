using System;
using System.Linq;
using FluentAssertions;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using Moq;
using NUnit.Framework;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.GenericSubdomains.Portable;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.QuestionnaireInfoFactoryTests
{
    internal class when_getting_question_edit_view : QuestionnaireInfoFactoryTestContext
    {
        [OneTimeSetUp] public void context () {
            questionDetailsReaderMock = new Mock<IDesignerQuestionnaireStorage>();
            questionnaireView = CreateQuestionnaireDocument();
            questionDetailsReaderMock
                .Setup(x => x.Get(questionnaireId))
                .Returns(questionnaireView);

            factory = CreateQuestionnaireInfoFactory(questionDetailsReaderMock.Object);
            BecauseOf();
        }

        private void BecauseOf() =>
            result = factory.GetQuestionEditView(questionnaireId, questionId);

        [Test] public void should_return_not_null_view () =>
            result.Should().NotBeNull();

        [Test] public void should_return_question_with_Id_equals_questionId () =>
            result.Id.Should().Be(questionId);

        [Test] public void should_return_question_equals_g3 () =>
            result.Title.Should().Be(GetQuestion(questionId).QuestionText);

        [Test] public void should_return_grouped_list_possible_linked_questions () =>
            result.SourceOfLinkedEntities.Count.Should().Be(10);

        [Test] public void should_replace_guids_in_condition_expressions_for_var_names () =>
            result.EnablementCondition.Should().Be("q1 > 25");

        [Test] public void should_return_grouped_list_of_multi_questions_with_one_pair_and_key_equals_ () =>
            result.SourceOfLinkedEntities.Select(x => x.Title).Should().Contain(linkedQuestionsKey1);

        [Test] public void should_return_integer_questions_in_group_with_key__linkedQuestionsKey1__with_ids_contains_only_q3Id () =>
            result.SourceOfLinkedEntities.Select(x => x.Id).Should().Contain(q3Id.FormatGuid());

        [Test] public void should_return_integer_questions_in_group_with_key__linkedQuestionsKey1__with_titles_contains_only_q3_title () =>
            result.SourceOfLinkedEntities.Select(x => x.Title).Should().Contain(GetQuestion(q3Id).QuestionText);

        [Test] public void should_return_integer_questions_in_group_with_key__linkedQuestionsKey2__with_ids_contains_only_q5Id () =>
            result.SourceOfLinkedEntities.Select(x => x.Id).Should().Contain(q5Id.FormatGuid());

        [Test] public void should_return_integer_questions_in_group_with_key__linkedQuestionsKey2__with_titles_contains_only_q5_title () =>
            result.SourceOfLinkedEntities.Select(x => x.Title).Should().Contain(GetQuestion(q5Id).QuestionText);

        [Test] public void should_return_roster_title_reference_for_first_roster () =>
            result.SourceOfLinkedEntities.Count(x => x.Title == "Roster: Roster 1.1" && !x.IsSectionPlaceHolder).Should().Be(1);

        [Test] public void should_return_roster_title_reference_for_second_roster () =>
            result.SourceOfLinkedEntities.Count(x => x.Title == "Roster: Roster 1.1.1" && !x.IsSectionPlaceHolder).Should().Be(1);

        [Test] public void should_return_roster_title_reference_for_third_roster () =>
            result.SourceOfLinkedEntities.Count(x => x.Title == "Roster: Roster 1.2" && !x.IsSectionPlaceHolder).Should().Be(1);

        private static IQuestion GetQuestion(Guid questionId)
        {
            return questionnaireView.Find<IQuestion>(questionId);
        }

        private static QuestionnaireInfoFactory factory;
        private static NewEditQuestionView result;
        private static QuestionnaireDocument questionnaireView;
        private static Mock<IDesignerQuestionnaireStorage> questionDetailsReaderMock;
        private static Guid questionId = q2Id;
        private static string linkedQuestionsKey1 = "Group 1 / Roster 1.1";
    }
}
