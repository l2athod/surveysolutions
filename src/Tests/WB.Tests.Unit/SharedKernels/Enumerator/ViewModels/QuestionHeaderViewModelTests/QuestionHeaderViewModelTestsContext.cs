﻿using Moq;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;
using WB.Tests.Abc;

namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.QuestionHeaderViewModelTests
{
    [NUnit.Framework.TestOf(typeof(QuestionHeaderViewModel))]
    internal class QuestionHeaderViewModelTestsContext
    {
        public static QuestionHeaderViewModel CreateViewModel(IQuestionnaireStorage questionnaireRepository = null, 
            IStatefulInterviewRepository interviewRepository = null, 
            IViewModelEventRegistry registry = null)
        {
            var statefulInterviewRepository = interviewRepository ?? Mock.Of<IStatefulInterviewRepository>();
            var liteEventRegistry = registry ?? Create.Service.LiteEventRegistry();
            var questionnaireStorage = questionnaireRepository ?? Mock.Of<IQuestionnaireStorage>();

            return new QuestionHeaderViewModel(
                Create.ViewModel.DynamicTextViewModel(
                    interviewRepository: statefulInterviewRepository,
                    eventRegistry: liteEventRegistry,
                    questionnaireStorage: questionnaireStorage));
        }
    }
}
