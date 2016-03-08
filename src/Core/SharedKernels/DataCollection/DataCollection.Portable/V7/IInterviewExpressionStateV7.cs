using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection.V6;

namespace WB.Core.SharedKernels.DataCollection.V7
{
    public interface IInterviewExpressionStateV7: IInterviewExpressionStateV6
    {
        LinkedQuestionOptionsChanges ProcessLinkedQuestionFilters();
        new IInterviewExpressionStateV7 Clone();
    }
}