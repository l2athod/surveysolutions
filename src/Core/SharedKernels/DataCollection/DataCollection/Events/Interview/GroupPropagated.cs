﻿using System;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Base;

namespace WB.Core.SharedKernels.DataCollection.Events.Interview
{
    public class GroupPropagated : InterviewPassiveEvent
    {
        public Guid GroupId { get; private set; }
        public decimal[] OuterScopeRosterVector { get; private set; }
        public int Count { get; private set; }

        public GroupPropagated(Guid groupId, decimal[] outerScopeRosterVector, int count)
        {
            this.GroupId = groupId;
            this.OuterScopeRosterVector = outerScopeRosterVector;
            this.Count = count;
        }
    }
}