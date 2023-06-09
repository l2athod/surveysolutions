﻿using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Commands.Interview.Base;

namespace WB.Core.SharedKernels.DataCollection.Scenarios
{
    public interface IScenarioService
    {
        List<IScenarioCommand> ConvertFromInterview(IQuestionnaire questionnaire, IEnumerable<InterviewCommand> commands);
        List<InterviewCommand> ConvertFromScenario(IQuestionnaire questionnaire, IEnumerable<IScenarioCommand> commands);
    }
}
