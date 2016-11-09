using Machine.Specifications;
using Moq;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.Core.SharedKernels.Enumerator.Aggregates;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.IntegerQuestionViewModelTests
{
    internal class when_answering_numeric_question_with_big_negative_value : IntegerQuestionViewModelTestContext
    {
        Establish context = () =>
        {
            SetUp();

            var integerNumericAnswer = Mock.Of<InterviewTreeIntegerQuestion>(_ => _.IsAnswered == true && _.GetAnswer() == 1);

            var interview = Mock.Of<IStatefulInterview>(_
                => _.QuestionnaireId == questionnaireId
                   && _.GetIntegerQuestion(questionIdentity) == integerNumericAnswer);

            var interviewRepository = Mock.Of<IStatefulInterviewRepository>(x => x.Get(interviewId) == interview);

            var questionnaireRepository = SetupQuestionnaireRepositoryWithNumericQuestion();

            integerModel = CreateIntegerQuestionViewModel(
                interviewRepository: interviewRepository,
                questionnaireRepository: questionnaireRepository);

            integerModel.Init(interviewId, questionIdentity, navigationState);
        };

        Because of = () =>
        {
            integerModel.Answer = decimal.MinValue;
            integerModel.ValueChangeCommand.Execute();
        };

        It should_mark_question_as_invalid_with_message = () =>
            ValidityModelMock.Verify(x => x.MarkAnswerAsNotSavedWithMessage("Entered value can not be parsed as integer value"), Times.Once);

        It should_not_send_answer_command = () =>
            AnsweringViewModelMock.Verify(x => x.SendAnswerQuestionCommandAsync(Moq.It.IsAny<AnswerNumericIntegerQuestionCommand>()), Times.Never);

        private static IntegerQuestionViewModel integerModel;
    }
}