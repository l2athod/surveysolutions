﻿using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;

namespace WB.Core.BoundedContexts.Headquarters.WebInterview
{
    public interface IPauseResumeQueue
    {
        void EnqueuePause(PauseInterviewCommand command);

        void EnqueueResume(ResumeInterviewCommand command);

        void EnqueueOpenBySupervisor(OpenInterviewBySupervisorCommand command);

        void EnqueueCloseBySupervisor(CloseInterviewBySupervisorCommand command);

        List<TimestampedInterviewCommand> DeQueueForPublish();
    }
}