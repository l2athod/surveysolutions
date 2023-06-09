﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WB.Services.Export.Interview.Entities;
using WB.Services.Export.Questionnaire;
using WB.Services.Infrastructure.Tenant;

namespace WB.Services.Export.Interview
{
    public interface IInterviewFactory
    {
        IEnumerable<InterviewEntity> GetInterviewEntities(Guid[] interviewsId,
            QuestionnaireDocument questionnaire);
        Dictionary<string, InterviewLevel> GetInterviewDataLevels(QuestionnaireDocument questionnaire, List<InterviewEntity> interviewEntities);
        List<MultimediaAnswer> GetMultimediaAnswersByQuestionnaire(QuestionnaireDocument questionnaire, Guid[] interviewIds, CancellationToken cancellationToken);
        Task<List<AudioAuditInfo>> GetAudioAuditInfos(TenantInfo tenant, Guid[] interviewIds, CancellationToken cancellationToken);
    }

    public struct MultimediaAnswer
    {
        public Guid InterviewId { get; set; }
        public string Answer { get; set; }
        public MultimediaType Type { get; set; }
    }

    public enum MultimediaType
    {
        Image, Audio
    }

    public struct AudioAuditInfo
    {
        public Guid InterviewId { get; set; }
        public string[] FileNames { get; set; }
    }
}
