using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Moq;
using WB.Core.BoundedContexts.QuestionnaireTester.Implementation.Aggregates;
using WB.Core.BoundedContexts.QuestionnaireTester.Implementation.Entities;
using WB.Core.BoundedContexts.QuestionnaireTester.Implementation.Entities.QuestionModels;
using WB.Core.BoundedContexts.QuestionnaireTester.Infrastructure;
using WB.Core.BoundedContexts.QuestionnaireTester.Repositories;
using WB.Core.BoundedContexts.QuestionnaireTester.ViewModels;
using WB.Core.BoundedContexts.QuestionnaireTester.ViewModels.QuestionsViewModels;
using WB.Core.BoundedContexts.QuestionnaireTester.ViewModels.QuestionStateViewModels;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;

using It = Machine.Specifications.It;

namespace WB.Tests.Unit.BoundedContexts.QuestionnaireTester.ViewModels.CascadingSingleOptionQuestionViewModelTests
{
    public class when_setting_FilterText_after_handling_SingleOptionQuestionAnswered_for_parent_question : CascadingSingleOptionQuestionViewModelTestContext
    {
        Establish context = () =>
        {
            var childAnswer = Mock.Of<SingleOptionAnswer>(_ => _.IsAnswered == true && _.Answer == answerOnChildQuestion);
            var parentOptionAnswer = Mock.Of<SingleOptionAnswer>(_ => _.IsAnswered == true && _.Answer == 1);
            var secondParentOptionAnswer = Mock.Of<SingleOptionAnswer>(_ => _.IsAnswered == true && _.Answer == 2);

            var userIdentity = Mock.Of<IUserIdentity>(_ => _.UserId == userId);
            var principal = Mock.Of<IPrincipal>(_ => _.CurrentUserIdentity == userIdentity);

            StatefulInterviewMock.Setup(x => x.Id).Returns(interviewId);
            StatefulInterviewMock.Setup(x => x.QuestionnaireId).Returns(questionnaireId);
            StatefulInterviewMock.Setup(x => x.GetSingleOptionAnswer(questionIdentity)).Returns(childAnswer);
            StatefulInterviewMock.Setup(x => x.GetSingleOptionAnswer(parentIdentity)).Returns(parentOptionAnswer);


            var interviewRepository = Mock.Of<IStatefulInterviewRepository>(x => x.Get(interviewId.FormatGuid()) == StatefulInterviewMock.Object);

            var cascadingQuestionModel = Mock.Of<CascadingSingleOptionQuestionModel>(_
                => _.Id == questionIdentity.Id
                   && _.Options == Options
                   && _.CascadeFromQuestionId == parentIdentity.Id
                   && _.RosterLevelDeepOfParentQuestion == 1);

            var questionnaireModel = Mock.Of<QuestionnaireModel>(_ => _.Questions == new Dictionary<Guid, BaseQuestionModel> { { questionIdentity.Id, cascadingQuestionModel } });

            var questionnaireRepository = Mock.Of<IPlainKeyValueStorage<QuestionnaireModel>>(x => x.GetById(questionnaireId) == questionnaireModel);

            cascadingModel = CreateCascadingSingleOptionQuestionViewModel(
                QuestionStateMock.Object,
                AnsweringViewModelMock.Object,
                interviewRepository: interviewRepository,
                questionnaireRepository: questionnaireRepository,
                eventRegistry: EventRegistry.Object,
                principal: principal);

            cascadingModel.Init(interviewId.FormatGuid(), questionIdentity, navigationState);

            StatefulInterviewMock.Setup(x => x.GetSingleOptionAnswer(parentIdentity)).Returns(secondParentOptionAnswer);

            cascadingModel.Handle(Create.Event.SingleOptionQuestionAnswered(parentIdentity.Id, parentIdentity.RosterVector, 2));
        };

        Because of = () =>
            cascadingModel.FilterText = "c";

        It should_set_ShouldClearText_in_true = () =>
            cascadingModel.ShouldClearText.ShouldBeTrue();

        It should_set_not_empty_list_in_AutoCompleteSuggestions = () =>
            cascadingModel.AutoCompleteSuggestions.ShouldNotBeEmpty();

        It should_set_2_options_in_AutoCompleteSuggestions = () =>
            cascadingModel.AutoCompleteSuggestions.Count.ShouldEqual(2);

        It should_format_first_option_in_AutoCompleteSuggestions = () =>
        {
            var firstOption = cascadingModel.AutoCompleteSuggestions.ElementAt(0);
            firstOption.Text.ShouldEqual("title <b>c</b>cc 5");
            firstOption.Value.ShouldEqual(5);
            firstOption.ParentValue.ShouldEqual(2);
            firstOption.OriginalText.ShouldEqual("title ccc 5");
        };

        It should_format_second_option_in_AutoCompleteSuggestions = () =>
        {
            var firstOption = cascadingModel.AutoCompleteSuggestions.ElementAt(1);
            firstOption.Text.ShouldEqual("title b<b>c</b>w 6");
            firstOption.Value.ShouldEqual(6);
            firstOption.ParentValue.ShouldEqual(2);
            firstOption.OriginalText.ShouldEqual("title bcw 6");
        };

        private static CascadingSingleOptionQuestionViewModel cascadingModel;
        private static Identity questionIdentity = Create.Identity(Guid.Parse("11111111111111111111111111111111"), new decimal[] { 1, 2 });
        private static Identity parentIdentity = Create.Identity(Guid.Parse("22222222222222222222222222222222"), new decimal[] { 1 });
        private static NavigationState navigationState = Create.NavigationState();

        private static readonly Mock<QuestionStateViewModel<SingleOptionQuestionAnswered>> QuestionStateMock =
            new Mock<QuestionStateViewModel<SingleOptionQuestionAnswered>> { DefaultValue = DefaultValue.Mock };

        private static readonly Mock<IStatefulInterview> StatefulInterviewMock = new Mock<IStatefulInterview>();

        private static readonly Mock<AnsweringViewModel> AnsweringViewModelMock =
            new Mock<AnsweringViewModel> { DefaultValue = DefaultValue.Mock };

        private static readonly Mock<ILiteEventRegistry> EventRegistry = new Mock<ILiteEventRegistry>();

        private static readonly List<CascadingOptionModel> Options = new List<CascadingOptionModel>
                                                                     {
                                                                         Create.CascadingOptionModel(1, "title abc 1", 1),
                                                                         Create.CascadingOptionModel(2, "title def 2", 1),
                                                                         Create.CascadingOptionModel(3, "title klo 3", 1),
                                                                         Create.CascadingOptionModel(4, "title gha 4", 2),
                                                                         Create.CascadingOptionModel(5, "title ccc 5", 2),
                                                                         Create.CascadingOptionModel(6, "title bcw 6", 2)
                                                                     };

        private static readonly string questionnaireId = "Questionnaire Id";
        private static readonly Guid userId = Guid.Parse("ffffffffffffffffffffffffffffffff");
        private static readonly Guid interviewId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        private static readonly int answerOnChildQuestion = 3;
    }
}