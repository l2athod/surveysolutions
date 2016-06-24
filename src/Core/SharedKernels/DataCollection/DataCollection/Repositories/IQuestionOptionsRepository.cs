﻿using System;
using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Implementation.Providers;

namespace WB.Core.SharedKernels.DataCollection.Repositories
{
    public interface IQuestionOptionsRepository
    {
        IEnumerable<CategoricalOption> GetOptionsForQuestion(QuestionnaireIdentity qestionnaireIdentity, ICategoricalOptionsProvider categoricalOptionsProvider, Guid questionId, int? parentQuestionValue, string filter);

        CategoricalOption GetOptionForQuestionByOptionText(QuestionnaireIdentity qestionnaireIdentity, ICategoricalOptionsProvider categoricalOptionsProvider, Guid questionId, string optionText);
    }
}
