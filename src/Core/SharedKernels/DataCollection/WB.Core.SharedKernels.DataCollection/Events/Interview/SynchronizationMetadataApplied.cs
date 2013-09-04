using System;
using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Synchronization;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Base;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;

namespace WB.Core.SharedKernels.DataCollection.Events.Interview
{
    public class SynchronizationMetadataApplied : InterviewActiveEvent
    {
        public SynchronizationMetadataApplied(Guid userId, Guid questionnaireId, long questionnaireVersion, InterviewStatus status, AnsweredQuestionSynchronizationDto[] featuredQuestionsMeta)
            : base(userId)
        {
            QuestionnaireId = questionnaireId;
            QuestionnaireVersion = questionnaireVersion;
            Status = status;
            FeaturedQuestionsMeta = featuredQuestionsMeta;
        }

        public Guid QuestionnaireId { get; private set; }

        public long QuestionnaireVersion { get; private set; }

        public InterviewStatus Status { get; private set; }

        public AnsweredQuestionSynchronizationDto[] FeaturedQuestionsMeta { get; private set; }
    }
}
