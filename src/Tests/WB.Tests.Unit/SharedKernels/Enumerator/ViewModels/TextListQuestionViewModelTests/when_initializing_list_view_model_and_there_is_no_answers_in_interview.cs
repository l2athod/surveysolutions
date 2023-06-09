using System.Linq;
using FluentAssertions;
using Moq;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.ViewModels;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;
using WB.Tests.Abc;


namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.TextListQuestionViewModelTests
{
    internal class when_initializing_list_view_model_and_there_is_no_answers_in_interview : TextListQuestionViewModelTestContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            var textListAnswer = Mock.Of<InterviewTreeTextListQuestion>(_ => _.IsAnswered() == false);

            var interview = Mock.Of<IStatefulInterview>(_ => _.QuestionnaireId == questionnaireId
                && _.GetTextListQuestion(questionIdentity) == textListAnswer);

            var interviewRepository = Mock.Of<IStatefulInterviewRepository>(x => x.Get(interviewId) == interview);

            var questionnaireRepository = SetupQuestionnaireRepositoryWithListQuestion(isRosterSizeQuestion: true);

            listModel = CreateTextListQuestionViewModel(
                QuestionStateMock.Object, 
                AnsweringViewModelMock.Object,
                interviewRepository: interviewRepository,
                questionnaireRepository: questionnaireRepository);
            BecauseOf();
        }

        public void BecauseOf() =>
            listModel.Init(interviewId, questionIdentity, navigationState);

        [NUnit.Framework.Test] public void should_initialize_question_state () =>
            QuestionStateMock.Verify(x => x.Init(interviewId, questionIdentity, navigationState), Times.Once);

        [NUnit.Framework.Test] public void should_create_empty_list_of_answers () =>
            listModel.Answers.OfType<TextListItemViewModel>().Count().Should().Be(0);

        [NUnit.Framework.Test] public void should_contain_add_new_item_view_model () =>
            listModel.Answers.OfType<TextListAddNewItemViewModel>().Should().NotBeEmpty();

        private static TextListQuestionViewModel listModel;
        private static NavigationState navigationState = Create.Other.NavigationState();
        private static readonly Mock<QuestionStateViewModel<TextListQuestionAnswered>> QuestionStateMock = new Mock<QuestionStateViewModel<TextListQuestionAnswered>>();
        private static readonly Mock<AnsweringViewModel> AnsweringViewModelMock = new Mock<AnsweringViewModel>();

        private static readonly string interviewId = "Some interviewId";
        private static readonly string questionnaireId = "Questionnaire Id";
    }
}
