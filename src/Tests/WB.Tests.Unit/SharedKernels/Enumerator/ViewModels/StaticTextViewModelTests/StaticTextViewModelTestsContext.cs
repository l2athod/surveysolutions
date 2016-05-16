﻿using Machine.Specifications;
using Moq;
using WB.Core.GenericSubdomains.Portable.Implementation.Services;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Implementation.Services;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Implementation.Services;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;

namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.StaticTextViewModelTests
{
    [Subject(typeof(StaticTextViewModel))]
    internal class StaticTextViewModelTestsContext
    {
        public static StaticTextViewModel CreateViewModel(IPlainQuestionnaireRepository questionnaireRepository = null, 
            IStatefulInterviewRepository interviewRepository = null, 
            ILiteEventRegistry registry = null,
            IRosterTitleSubstitutionService rosterTitleSubstitutionService = null,
            AttachmentViewModel attachmentViewModel = null,
            StaticTextStateViewModel questionState = null,
            SubstitutionReplacerService substitutionReplacerService = null)
        {
            if (rosterTitleSubstitutionService == null)
            {
                var substStub = new Mock<IRosterTitleSubstitutionService>();
                substStub.Setup(x => x.Substitute(Moq.It.IsAny<string>(), Moq.It.IsAny<Identity>(), Moq.It.IsAny<string>()))
                    .Returns<string, Identity, string>((title, id, interviewId) => title);
                rosterTitleSubstitutionService = substStub.Object;
            }

            return new StaticTextViewModel(
                questionnaireRepository: questionnaireRepository ?? Mock.Of<IPlainQuestionnaireRepository>(),
                interviewRepository: interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(),
                registry: registry ?? Create.LiteEventRegistry(),
                questionState: questionState ?? Mock.Of<StaticTextStateViewModel>(),
                attachmentViewModel: attachmentViewModel ?? Mock.Of<AttachmentViewModel>(),
                substitutionReplacerService: new SubstitutionReplacerService(
                    interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(),
                    questionnaireRepository ?? Mock.Of<IPlainQuestionnaireRepository>(),
                    new SubstitutionService(),
                    new AnswerToStringService(),
                    new VariableToUIStringService(),
                    rosterTitleSubstitutionService));
        }
    }
}