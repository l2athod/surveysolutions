﻿using System;
using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.SurveySolutions.Documents;

namespace WB.Core.SharedKernels.Enumerator.Implementation.Services
{
    public class QuestionOptionsRepository : IQuestionOptionsRepository
    {
        private IOptionsRepository optionsRepository;

        public QuestionOptionsRepository(IOptionsRepository optionsRepository)
        {
            if(optionsRepository == null)
                throw new ArgumentException(nameof(optionsRepository));

            this.optionsRepository = optionsRepository;
        }

        public IEnumerable<CategoricalOption> GetOptionsForQuestion(QuestionnaireIdentity qestionnaireIdentity,
            Guid questionId, int? parentQuestionValue, string filter, Translation translation)
        {
            return this.optionsRepository.GetFilteredQuestionOptions(qestionnaireIdentity, questionId, parentQuestionValue, filter, translation?.Id);
        }

        public CategoricalOption GetOptionForQuestionByOptionText(QuestionnaireIdentity qestionnaireIdentity,
             Guid questionId, string optionText, Translation translation)
        {
            return this.optionsRepository.GetQuestionOption(qestionnaireIdentity, questionId, optionText, translation?.Id);
        }

        public CategoricalOption GetOptionForQuestionByOptionValue(QuestionnaireIdentity qestionnaireIdentity,
            Guid questionId, decimal optionValue, Translation translation)
        {
            return this.optionsRepository.GetQuestionOptionByValue(qestionnaireIdentity, questionId, optionValue, translation?.Id);
        }
    }
}
