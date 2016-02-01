﻿using System.Collections.Generic;
using System.Linq;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Resources;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.ValueObjects;

namespace WB.Core.BoundedContexts.Designer.Implementation.Services
{
    internal partial class QuestionnaireVerifier : IQuestionnaireVerifier
    {
        private const int MaxRecommendedAmountOfRosters = 20;

        private IEnumerable<QuestionnaireVerificationError> VerifyAmountOfRosters(CachedQuestionnaireDocument document, VerificationState state)
        {
            var rosters = document.Find<IGroup>(q => q.IsRoster).ToArray();

            if (rosters.Length <= MaxRecommendedAmountOfRosters)
                return Enumerable.Empty<QuestionnaireVerificationError>();

            return new[]
            {
                new QuestionnaireVerificationError("WB0200", VerificationMessages.WB0200_LargeNumberOfRostersIsCreated, VerificationErrorLevel.Warning)
            };
        }
    }
}
