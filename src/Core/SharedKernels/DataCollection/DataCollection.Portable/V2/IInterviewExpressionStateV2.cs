using System;

namespace WB.Core.SharedKernels.DataCollection
{
    public interface IInterviewExpressionStateV2 : IInterviewExpressionState
    {
        void UpdateRosterTitle(Guid rosterId, decimal[] outerRosterVector, decimal rosterInstanceId, string rosterTitle);
        IInterviewExpressionStateV2 CloneV2();
    }
}