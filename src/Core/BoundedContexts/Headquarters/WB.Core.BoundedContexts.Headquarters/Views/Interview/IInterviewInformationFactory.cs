using System;
using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Synchronization;

namespace WB.Core.BoundedContexts.Headquarters.Views.Interview
{
    public interface IInterviewInformationFactory
    {
        IEnumerable<InterviewInformation> GetInProgressInterviews(Guid interviewerId);
        IEnumerable<InterviewInformation> GetInterviewsByIds(Guid[] interviewIds);
        [Obsolete("Since 18.08 KP-11379")]
        InterviewSynchronizationDto GetInProgressInterviewDetails(Guid interviewId);
    }
}
