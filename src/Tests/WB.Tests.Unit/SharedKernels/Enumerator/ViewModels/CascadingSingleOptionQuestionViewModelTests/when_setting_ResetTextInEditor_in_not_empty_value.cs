using System;
using System.Linq;
using Machine.Specifications;
using Moq;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.Core.SharedKernels.Enumerator.Aggregates;

using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.CascadingSingleOptionQuestionViewModelTests
{
    [Ignore("KP-8159")]
    internal class when_setting_ResetTextInEditor_in_not_empty_value: CascadingSingleOptionQuestionViewModelTestContext
    {
        Establish context = () =>
        {
            SetUp();

            var childAnswer = Mock.Of<InterviewTreeSingleOptionQuestion>(_ => _.IsAnswered == true && _.GetAnswer().SelectedValue == 3);
            var parentOptionAnswer = Mock.Of<InterviewTreeSingleOptionQuestion>(_ => _.IsAnswered == true && _.GetAnswer().SelectedValue == 1);

            var interview = new Mock<IStatefulInterview>();

            interview.Setup(x => x.QuestionnaireIdentity).Returns(questionnaireId);
            interview.Setup(x => x.GetSingleOptionQuestion(questionIdentity)).Returns(childAnswer);
            interview.Setup(x => x.GetSingleOptionQuestion(parentIdentity)).Returns(parentOptionAnswer);
            interview.Setup(x => x.GetOptionForQuestionWithoutFilter(questionIdentity, 3, 1)).Returns(new CategoricalOption() { Title = "3", Value = 3, ParentValue = 1 });
            interview.Setup(x => x.GetTopFilteredOptionsForQuestion(Moq.It.IsAny<Identity>(), Moq.It.IsAny<int?>(), Moq.It.IsAny<string>(), Moq.It.IsAny<int>()))
                .Returns((Identity identity, int? value, string filter, int count) => Options.Where(x => x.ParentValue == value && x.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList());


            var interviewRepository = Mock.Of<IStatefulInterviewRepository>(x => x.Get(interviewId) == interview.Object);

            var questionnaireRepository = SetupQuestionnaireRepositoryWithCascadingQuestion();

            var optionsRepository = SetupOptionsRepositoryForQuestionnaire(questionIdentity.Id);

            cascadingModel = CreateCascadingSingleOptionQuestionViewModel(
                interviewRepository: interviewRepository,
                questionnaireRepository: questionnaireRepository);

            cascadingModel.Init(interviewId, questionIdentity, navigationState);

            cascadingModel.FilterText = "a";
        };

        Because of = () =>
            cascadingModel.ResetTextInEditor = "hello";

        It should_not_set_selected_object = () =>
            cascadingModel.SelectedObject.ShouldNotBeNull();

        It should_not_set_filter_text = () =>
            cascadingModel.FilterText.ShouldNotBeNull();

        It should_set_empty_list_in_AutoCompleteSuggestions = () =>
            cascadingModel.AutoCompleteSuggestions.ShouldNotBeEmpty();

        private static CascadingSingleOptionQuestionViewModel cascadingModel;
    }
}