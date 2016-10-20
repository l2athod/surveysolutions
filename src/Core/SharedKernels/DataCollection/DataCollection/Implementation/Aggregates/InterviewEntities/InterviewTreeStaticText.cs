using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.GenericSubdomains.Portable;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities
{
    public class InterviewTreeStaticText : InterviewTreeLeafNode
    {
        public InterviewTreeStaticText(Identity identity)
            : this(identity, false) { }

        public InterviewTreeStaticText(Identity identity, bool disabled)
            : base(identity, disabled) { }

        public bool IsValid => !this.FailedValidations?.Any() ?? true;
        public IReadOnlyList<FailedValidationCondition> FailedValidations { get; private set; }
        public void MarkAsInvalid(IEnumerable<FailedValidationCondition> failedValidations)
        {
            if (failedValidations == null) throw new ArgumentNullException(nameof(failedValidations));
            this.FailedValidations = failedValidations.ToReadOnlyCollection();
        }
        public void MarkAsValid()
            => this.FailedValidations = Enumerable.Empty<FailedValidationCondition>().ToList();

        public override string ToString() => $"Text ({this.Identity})";
    }
}