using System;
using System.Collections.Generic;

using Machine.Specifications;

using Moq;

using WB.Core.BoundedContexts.QuestionnaireTester.Implementation.Aggregates;
using WB.Core.BoundedContexts.QuestionnaireTester.Implementation.Entities;
using WB.Core.BoundedContexts.QuestionnaireTester.Implementation.Entities.QuestionModels;
using WB.Core.BoundedContexts.QuestionnaireTester.Repositories;
using WB.Core.BoundedContexts.QuestionnaireTester.ViewModels.QuestionsViewModels;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;

using It = Machine.Specifications.It;

namespace WB.Tests.Unit.BoundedContexts.QuestionnaireTester.ViewModels.IntegerQuestionViewModelTests
{
    public class when_answering_roster_size_numeric_question_with_big_value_and_question_was_not_answered_before : IntegerQuestionViewModelTestContext
    {
        Establish context = () =>
        {
            SetUp();

            var integerNumericAnswer = Mock.Of<IntegerNumericAnswer>(_ => _.IsAnswered == false);

            var interview = Mock.Of<IStatefulInterview>(_
                => _.QuestionnaireId == questionnaireId
                   && _.GetIntegerNumericAnswer(questionIdentity) == integerNumericAnswer);

            var interviewRepository = Mock.Of<IStatefulInterviewRepository>(x => x.Get(interviewId) == interview);

            var cascadingQuestionModel = Mock.Of<IntegerNumericQuestionModel>(_
                => _.Id == questionIdentity.Id
                   && _.IsRosterSizeQuestion == true
                   && _.MaxValue == null);

            var questionnaireModel = Mock.Of<QuestionnaireModel>(_ => _.Questions == new Dictionary<Guid, BaseQuestionModel> { { questionIdentity.Id, cascadingQuestionModel } });

            var questionnaireRepository = Mock.Of<IPlainKeyValueStorage<QuestionnaireModel>>(x => x.GetById(questionnaireId) == questionnaireModel);

            integerModel = CreateIntegerQuestionViewModel(
                interviewRepository: interviewRepository,
                questionnaireRepository: questionnaireRepository);

            integerModel.Init(interviewId, questionIdentity, navigationState);
        };

        Because of = () =>
        {
            integerModel.AnswerAsString = "50";
            integerModel.ValueChangeCommand.Execute();
        };

        It should_mark_question_as_invalid_with_message = () =>
            ValidityModelMock.Verify(x => x.MarkAnswerAsInvalidWithMessage("Answer '50' is incorrect because answer is greater than Roster upper bound '40'."), Times.Once);

        It should_not_send_answer_command = () =>
            AnsweringViewModelMock.Verify(x => x.SendAnswerQuestionCommand(Moq.It.IsAny<AnswerNumericIntegerQuestionCommand>()), Times.Never);

        It should_reset_AnswerAsString_to_null = () =>
            integerModel.AnswerAsString.ShouldBeNull();

        private static IntegerQuestionViewModel integerModel;
    }
}