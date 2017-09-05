using System.Linq;
using Machine.Specifications;
using Moq;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions;
using WB.Tests.Abc;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.CascadingSingleOptionQuestionViewModelTests
{
    internal class when_initializing_cascading_view_model_and_child_and_parent_question_are_answered_and_question_2_level_roster : CascadingSingleOptionQuestionViewModelTestContext
    {
        Establish context = () =>
        {
            SetUp();
            var childAnswer = Mock.Of<InterviewTreeSingleOptionQuestion>(_ => _.IsAnswered() == true && _.GetAnswer() == Create.Entity.SingleOptionAnswer(answerOnChildQuestion));
            var parentOptionAnswer = Mock.Of<InterviewTreeSingleOptionQuestion>(_ => _.IsAnswered() == true && _.GetAnswer() == Create.Entity.SingleOptionAnswer(1));

            StatefulInterviewMock.Setup(x => x.QuestionnaireIdentity).Returns(questionnaireId);
            StatefulInterviewMock.Setup(x => x.GetSingleOptionQuestion(questionIdentity)).Returns(childAnswer);
            StatefulInterviewMock.Setup(x => x.GetSingleOptionQuestion(parentIdentity)).Returns(parentOptionAnswer);

            StatefulInterviewMock.Setup(x => x.GetOptionForQuestionWithoutFilter(questionIdentity, 3, 1))
                .Returns(new CategoricalOption() { Title = "3", Value = 3, ParentValue = 1 });

            StatefulInterviewMock.Setup(x => x.GetTopFilteredOptionsForQuestion(questionIdentity, 1, "3", Moq.It.IsAny<int>()))
                .Returns(Options.Where(x => x.Value == 3).ToList());

            var interviewRepository = Mock.Of<IStatefulInterviewRepository>(x => x.Get(interviewId) == StatefulInterviewMock.Object);

            var questionnaireRepository = SetupQuestionnaireRepositoryWithCascadingQuestion();

            cascadingModel = CreateCascadingSingleOptionQuestionViewModel(
                interviewRepository: interviewRepository,
                questionnaireRepository: questionnaireRepository);
        };

        Because of = () => cascadingModel.Init(interviewId, questionIdentity, navigationState);

        It should_get_answer_for_parent_question_once = () =>
            StatefulInterviewMock.Verify(x => x.GetSingleOptionQuestion(parentIdentity), Times.Once);

        It should_get_answer_for_question_once = () =>
            StatefulInterviewMock.Verify(x => x.GetSingleOptionQuestion(questionIdentity), Times.Once);

        It should_initialize_question_state = () =>
            QuestionStateMock.Verify(x => x.Init(interviewId, questionIdentity, navigationState), Times.Once);

        It should_subscribe_for_events = () =>
            EventRegistry.Verify(x => x.Subscribe(cascadingModel, Moq.It.IsAny<string>()), Times.Once);
        
        It should_set_filter_text = () =>
            cascadingModel.FilterText.ShouldEqual("3");

        It should_set_1_item_list_in_AutoCompleteSuggestions = () =>
            cascadingModel.AutoCompleteSuggestions.Count.ShouldEqual(1);

        It should_format_first_option_in_AutoCompleteSuggestions = () =>
            cascadingModel.AutoCompleteSuggestions.ElementAt(0).ShouldContain("title klo <b>3</b>");

        private static CascadingSingleOptionQuestionViewModel cascadingModel;
        private static readonly Mock<IStatefulInterview> StatefulInterviewMock = new Mock<IStatefulInterview>();
        private const int answerOnChildQuestion = 3;
    }
}