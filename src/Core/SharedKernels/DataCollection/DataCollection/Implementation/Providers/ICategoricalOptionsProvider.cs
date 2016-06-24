﻿using System;
using System.Collections.Generic;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Providers
{
    public interface ICategoricalOptionsProvider
    {
        IEnumerable<CategoricalOption> GetOptionsForQuestionFromStructure(Guid questionId, int? parentQuestionValue, string filter);
        CategoricalOption GetOptionForQuestionFromStructureByOptionText(Guid questionId, string optionText);
    }
}