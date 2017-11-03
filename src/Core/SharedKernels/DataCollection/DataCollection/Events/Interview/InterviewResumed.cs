﻿using System;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Base;

namespace WB.Core.SharedKernels.DataCollection.Events.Interview
{
    public class InterviewResumed : InterviewActiveEvent
    {
        public InterviewResumed(Guid userId, DateTime localTime) : base(userId)
        {
            LocalTime = localTime;
        }

        public DateTime LocalTime { get; set; }
    }
}