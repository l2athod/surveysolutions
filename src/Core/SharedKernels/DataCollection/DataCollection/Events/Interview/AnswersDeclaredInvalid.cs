﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Base;

namespace WB.Core.SharedKernels.DataCollection.Events.Interview
{
    public class AnswersDeclaredInvalid : InterviewPassiveEvent
    {
        private IReadOnlyDictionary<Identity, IReadOnlyList<FailedValidationCondition>> failedValidationConditions;

        public Identity[] Questions { get; protected set; } = new Identity[]{};

        public List<KeyValuePair<Identity, IReadOnlyList<FailedValidationCondition>>> FailedConditionsStorage { get; protected set; } 

        [JsonIgnore]
        public IReadOnlyDictionary<Identity, IReadOnlyList<FailedValidationCondition>> FailedValidationConditions
        {
            get
            {
                return this.failedValidationConditions ?? 
                        (this.failedValidationConditions = this.FailedConditionsStorage.ToDictionary(x => x.Key, x => x.Value));
            }
            protected set
            {
                this.FailedConditionsStorage = value.ToList();
                this.failedValidationConditions = null;
            }
        }

        protected AnswersDeclaredInvalid(DateTimeOffset originDate) : base(originDate)
        {
            this.Questions = new Identity[] {};
            this.FailedValidationConditions = new Dictionary<Identity, IReadOnlyList<FailedValidationCondition>>();
        }

        public AnswersDeclaredInvalid(Identity[] questions, DateTimeOffset originDate): base(originDate)
        {
            this.Questions = questions;

            var dictionary = new Dictionary<Identity, IReadOnlyList<FailedValidationCondition>>();
            foreach (var question in questions)
            {
                dictionary.Add(question, new List<FailedValidationCondition>());
            }

            this.FailedValidationConditions = new Dictionary<Identity, IReadOnlyList<FailedValidationCondition>>(dictionary);
        }

        public AnswersDeclaredInvalid(IDictionary<Identity, IReadOnlyList<FailedValidationCondition>> failedValidationConditions, 
            DateTimeOffset originDate) : base(originDate)
        {
            this.Questions = failedValidationConditions.Keys.ToArray();
            this.FailedValidationConditions = new Dictionary<Identity, IReadOnlyList<FailedValidationCondition>>(failedValidationConditions);
        }
    }
}
