﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ncqrs.Commanding.CommandExecution.Mapping.Attributes;
using WB.Core.SharedKernels.DataCollection.Commands.Interview.Base;

namespace WB.Core.SharedKernels.DataCollection.Commands.Interview
{
    [MapsToAggregateRootConstructor(typeof(Implementation.Aggregates.Interview))]
    public class CreateInterviewCommand : InterviewCommand
    {
        public Guid QuestionnaireId { get; private set; }
        public Guid SupervisorId { get; private set; }
        public Dictionary<Guid, object> AnswersToFeaturedQuestions { get; private set; }
        public DateTime AnswersTime { get; private set; }

        public CreateInterviewCommand(Guid interviewId, Guid userId, Guid questionnaireId, Dictionary<Guid, object> answersToFeaturedQuestions, DateTime answersTime, Guid supervisorId)
            : base(interviewId, userId)
        {
            this.QuestionnaireId = questionnaireId;
            this.AnswersToFeaturedQuestions = answersToFeaturedQuestions;
            this.AnswersTime = answersTime;
            this.SupervisorId = supervisorId;
        }
    }
}
