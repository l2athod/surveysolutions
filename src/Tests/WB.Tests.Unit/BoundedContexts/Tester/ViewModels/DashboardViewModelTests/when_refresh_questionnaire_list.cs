using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Machine.Specifications;

using Moq;
using Nito.AsyncEx.Synchronous;
using WB.Core.BoundedContexts.Tester.Implementation.Services;
using WB.Core.BoundedContexts.Tester.ViewModels;
using WB.Core.BoundedContexts.Tester.Views;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Tests.Unit.SharedKernels.SurveyManagement;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.BoundedContexts.Tester.ViewModels.DashboardViewModelTests
{
    internal class when_refresh_questionnaire_list : DashboardViewModelTestContext
    {
        Establish context = () =>
        {
            var designerApiService = Mock.Of<IDesignerApiService>(
                    _ => _.GetQuestionnairesAsync(Moq.It.IsAny<CancellationToken>()) ==
                        Task.FromResult((IReadOnlyCollection<QuestionnaireListItem>)MyQuestionnaires.Union(PublicQuestionnaires).ToReadOnlyCollection()));

            var storageAccessor = new TestAsyncPlainStorage<QuestionnaireListItem>(Array.Empty<QuestionnaireListItem>());

            viewModel = CreateDashboardViewModel(questionnaireListStorage: storageAccessor,
                designerApiService: designerApiService);
            viewModel.Init().WaitAndUnwrapException();
            viewModel.ShowPublicQuestionnairesCommand.Execute();
        };

        Because of = () => viewModel.RefreshQuestionnairesCommand.Execute();

        It should_stay_on_same_tab_with_public_questionnaires = () => viewModel.IsPublicShowed.ShouldBeTrue();
        It should_Questionnaires_have_3_public_questionnaires = () => viewModel.Questionnaires.Count.ShouldEqual(3);
        It should_contains_only_public_questionnaires = () => viewModel.Questionnaires.All(_ => _.IsPublic).ShouldBeTrue();
        It should_set_MyQuestionnairesCount_to_2 = () => viewModel.MyQuestionnairesCount.ShouldEqual(2);
        It should_set_PublicQuestionnairesCount_to_3 = () => viewModel.PublicQuestionnairesCount.ShouldEqual(3);

        private static DashboardViewModel viewModel;
        private static readonly IReadOnlyCollection<QuestionnaireListItem> MyQuestionnaires = new List<QuestionnaireListItem>
        {
            new QuestionnaireListItem(){IsPublic = false, OwnerName = userName},
            new QuestionnaireListItem(){IsPublic = false, OwnerName = userName}
        };

        private static readonly IReadOnlyCollection<QuestionnaireListItem> PublicQuestionnaires = new List<QuestionnaireListItem>
        {
            new QuestionnaireListItem(){IsPublic = true},
            new QuestionnaireListItem(){IsPublic = true},
            new QuestionnaireListItem(){IsPublic = true}
        };
    }
}