﻿using Moq;
using WB.Core.BoundedContexts.Interviewer.Implementation.Services;
using WB.Core.BoundedContexts.Interviewer.Views;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;

namespace WB.Tests.Unit.BoundedContexts.Interviewer.Services.InterviewerPlainInterviewFileStorageTests
{
    internal class InterviewerPlainInterviewFileStorageTestsContext
    {
        public static InterviewerImageQuestionFileStorage CreateInterviewerPlainInterviewFileStorage(
            IPlainStorage<InterviewMultimediaView> imageViewStorage = null,
            IPlainStorage<InterviewFileView> fileViewStorage = null)
        {
            return new InterviewerImageQuestionFileStorage(
                imageViewStorage: imageViewStorage ?? Mock.Of<IPlainStorage<InterviewMultimediaView>>(),
                fileViewStorage: fileViewStorage ?? Mock.Of<IPlainStorage<InterviewFileView>>());
        }
    }
}
