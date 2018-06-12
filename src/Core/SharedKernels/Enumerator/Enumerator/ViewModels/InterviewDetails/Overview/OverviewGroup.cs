﻿using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Overview
{
    public class OverviewGroup : OverviewNode
    {
        public OverviewGroup(InterviewTreeGroup treeNode) : base(treeNode)
        {
            this.State = OverviewNodeState.Answered;
        }

        public sealed override OverviewNodeState State { get; set; }
    }
}
