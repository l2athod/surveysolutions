using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Moq;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.PlainStorage;


namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.QuestionnaireInfoFactoryTests
{
    internal class when_getting_questions_eligible_for_numeric_roster_title_and_requested_size_question_is_unsaved : QuestionnaireInfoFactoryTestContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            questionnaireView = Create.QuestionnaireDocument(Guid.NewGuid(),
                Create.Roster(roster1Id, rosterSizeQuestionId: rosterSizeQuestionId, rosterType: RosterSizeSourceType.Question, children: new List<IComposite>()
                {
                    Create.TextQuestion(rosterTitleQuestionId),
                }),
                Create.Roster(roster2Id, rosterSizeQuestionId: otherRosterSizeQuestionId, rosterType: RosterSizeSourceType.Question, children: new List<IComposite>()
                {
                    Create.TextQuestion(childTitleQuestionId),
                }),
                Create.NumericIntegerQuestion(rosterSizeQuestionId)
            );

            questionDetailsReaderMock
                .Setup(x => x.Get(questionnaireId))
                .Returns(questionnaireView);

            factory = CreateQuestionnaireInfoFactory(questionDetailsReaderMock.Object);
            BecauseOf();
        } 

        private void BecauseOf() =>
            result = factory.GetQuestionsEligibleForNumericRosterTitle(questionnaireId, roster2Id, rosterSizeQuestionId);

        [NUnit.Framework.Test] public void should_return_3_elements_to_show_in_dropdown () =>
            result.Count.Should().Be(3);

        [NUnit.Framework.Test] public void should_return_roster_title_questions_as_the_2nd_element () =>
            result.ElementAt(1).Id.Should().Be(rosterTitleQuestionId.FormatGuid());

        [NUnit.Framework.Test] public void should_return_child_title_questions_as_the_3rd_element () =>
            result.ElementAt(2).Id.Should().Be(childTitleQuestionId.FormatGuid());

        private static QuestionnaireInfoFactory factory;
        private static List<DropdownEntityView> result;
        private static QuestionnaireDocument questionnaireView;
        private static readonly Mock<IDesignerQuestionnaireStorage> questionDetailsReaderMock = new Mock<IDesignerQuestionnaireStorage>();
        private static readonly Guid roster1Id = Guid.Parse("11111111111111111111111111111111");
        private static readonly Guid roster2Id = Guid.Parse("22222222222222222222222222222222");
        private static readonly Guid rosterSizeQuestionId = Guid.Parse("33333333333333333333333333333333");
        private static readonly Guid otherRosterSizeQuestionId = Guid.Parse("99999999999999999999999999999999");
        private static readonly Guid rosterTitleQuestionId = Guid.Parse("44444444444444444444444444444444");
        private static readonly Guid childTitleQuestionId =  Guid.Parse("55555555555555555555555555555555");
    }
}
