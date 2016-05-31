﻿using WB.Core.SharedKernels.DataCollection.Events.Interview.Base;

namespace WB.Core.SharedKernels.DataCollection.Events.Interview
{
    public class VariablesEnabled : InterviewPassiveEvent
    {
        public Identity[] Variables { get; private set; }

        public VariablesEnabled(Identity[] variables)
        {
            this.Variables = variables;
        }
    }
}