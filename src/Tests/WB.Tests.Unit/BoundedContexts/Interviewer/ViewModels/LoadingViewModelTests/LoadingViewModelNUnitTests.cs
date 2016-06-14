﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MvvmCross.Plugins.Messenger;
using Ncqrs.Eventing.Storage;
using NSubstitute;
using NUnit.Framework;
using WB.Core.BoundedContexts.Interviewer.Services.Infrastructure;
using WB.Core.BoundedContexts.Interviewer.Views;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.Enumerator.Aggregates;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;

namespace WB.Tests.Unit.BoundedContexts.Interviewer.ViewModels.LoadingViewModelTests
{
    [TestFixture]
    internal class LoadingViewModelNUnitTests
    {
        [Test]
        public async Task LoadingViewModel_when_interview_is_created_on_client_should_open_prefilled_questions_section()
        {
            var interview = Substitute.For<IStatefulInterview>();
            interview.CreatedOnClient.Returns(true);

            var statefulInterviewRepository = Substitute.For<IStatefulInterviewRepository>();
            statefulInterviewRepository.GetAsync(null, Moq.It.IsAny<IProgress<EventReadingProgress>>(),Moq.It.IsAny<CancellationToken>()).ReturnsForAnyArgs(Task.FromResult(interview));

            var navigationServiceMock = Substitute.For<IViewModelNavigationService>();

            var loadingViewModel = CreateLoadingViewModel(viewModelNavigationService: navigationServiceMock,
                interviewRepository: statefulInterviewRepository);

            await loadingViewModel.RestoreInterviewAndNavigateThere();

            await navigationServiceMock.ReceivedWithAnyArgs().NavigateToPrefilledQuestionsAsync(null);
        }

        [Test]
        public async Task LoadingViewModel_when_interview_is_not_created_on_client_should_open_interview_section()
        {
            var interview = Substitute.For<IStatefulInterview>();
            interview.CreatedOnClient.Returns(false);

            var statefulInterviewRepository = Substitute.For<IStatefulInterviewRepository>();
            statefulInterviewRepository.GetAsync(null, Moq.It.IsAny<IProgress<EventReadingProgress>>(), Moq.It.IsAny<CancellationToken>()).ReturnsForAnyArgs(Task.FromResult(interview));

            var navigationServiceMock = Substitute.For<IViewModelNavigationService>();

            var loadingViewModel = CreateLoadingViewModel(viewModelNavigationService: navigationServiceMock,
                interviewRepository: statefulInterviewRepository);

            await loadingViewModel.RestoreInterviewAndNavigateThere();

            await navigationServiceMock.ReceivedWithAnyArgs().NavigateToInterviewAsync(null);
        }

        [Test]
        public async Task LoadingViewModel_when_interview_is_completed_should_restart_interview_and_open_interview_section()
        {
            var interview = Substitute.For<IStatefulInterview>();
            interview.CreatedOnClient.Returns(false);
            interview.Status.Returns(InterviewStatus.Completed);

            var statefulInterviewRepository = Substitute.For<IStatefulInterviewRepository>();
            statefulInterviewRepository.GetAsync(null, Moq.It.IsAny<IProgress<EventReadingProgress>>(), Moq.It.IsAny<CancellationToken>()).ReturnsForAnyArgs(Task.FromResult(interview));

            var navigationServiceMock = Substitute.For<IViewModelNavigationService>();
            var commandService = Substitute.For<ICommandService>();
            var loadingViewModel = CreateLoadingViewModel(viewModelNavigationService: navigationServiceMock,
                interviewRepository: statefulInterviewRepository, commandService: commandService);

            await loadingViewModel.RestoreInterviewAndNavigateThere();

            await navigationServiceMock.ReceivedWithAnyArgs().NavigateToInterviewAsync(null);
            await commandService.ReceivedWithAnyArgs().ExecuteAsync(Moq.It.IsAny<RestartInterviewCommand>());
        }
        protected static LoadingViewModel CreateLoadingViewModel(
          IViewModelNavigationService viewModelNavigationService = null,
          IStatefulInterviewRepository interviewRepository = null,
          ICommandService commandService = null,
          IPrincipal principal = null)
        {
            return new LoadingViewModel(
                principal ?? Substitute.For<IPrincipal>(),
                viewModelNavigationService ?? Substitute.For<IViewModelNavigationService>(), 
                interviewRepository ?? Substitute.For<IStatefulInterviewRepository>(),
                commandService ?? Substitute.For<ICommandService>());
        }
    }
}