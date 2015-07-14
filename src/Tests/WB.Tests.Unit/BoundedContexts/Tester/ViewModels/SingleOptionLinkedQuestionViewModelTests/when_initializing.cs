using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Moq;
using WB.Core.BoundedContexts.Tester.Implementation.Aggregates;
using WB.Core.BoundedContexts.Tester.Implementation.Entities;
using WB.Core.BoundedContexts.Tester.ViewModels;
using WB.Core.BoundedContexts.Tester.ViewModels.Questions.State;
using WB.Core.BoundedContexts.Tester.ViewModels.Questions;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.BoundedContexts.QuestionnaireTester.ViewModels.SingleOptionLinkedQuestionViewModelTests
{
    public class when_initializing : SingleOptionLinkedQuestionViewModelTestsContext
    {
        Establish context = () =>
        {
            var questionnaire = SetupQuestionnaireModelWithSingleOptionQuestionLinkedToTextQuestion(questionId, linkedToQuestionId);

            var interview = Mock.Of<IStatefulInterview>(_
                => _.FindAnswersOfReferencedQuestionForLinkedQuestion(Moq.It.IsAny<Guid>(), Moq.It.IsAny<Identity>()) == new[]
                    {
                        Create.TextAnswer("answer1"),
                        Create.TextAnswer(null),
                        Create.TextAnswer("answer2"),
                    }
                && _.Answers == new Dictionary<string, BaseInterviewAnswer>());

            viewModel = Create.SingleOptionLinkedQuestionViewModel(
                eventRegistry: eventRegistryMock.Object,
                questionState: questionStateMock.Object,
                questionnaireModel: questionnaire,
                interview: interview);
        };

        Because of = () =>
            viewModel.Init(interviewId, questionIdentity, navigationState);

        It should_initialize_question_state = () =>
            questionStateMock.Verify(state => state.Init(interviewId, questionIdentity, navigationState), Times.Once);

        It should_subsribe_self_to_event_registry = () =>
            eventRegistryMock.Verify(registry => registry.Subscribe(viewModel), Times.Once);

        It should_fill_options_with_answers_from_linked_to_question = () =>
            viewModel.Options.Select(option => option.Title).ShouldContainOnly("answer1", "answer2");

        private static Mock<ILiteEventRegistry> eventRegistryMock = new Mock<ILiteEventRegistry>();
        private static SingleOptionLinkedQuestionViewModel viewModel;
        private static string interviewId = "11111111111111111111111111111111";
        private static Guid questionId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        private static Identity questionIdentity = Create.Identity(questionId, Empty.RosterVector);
        private static Guid linkedToQuestionId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
        private static NavigationState navigationState = Create.NavigationState();
        private static Mock<QuestionStateViewModel<SingleOptionLinkedQuestionAnswered>> questionStateMock = new Mock<QuestionStateViewModel<SingleOptionLinkedQuestionAnswered>>();
    }
}