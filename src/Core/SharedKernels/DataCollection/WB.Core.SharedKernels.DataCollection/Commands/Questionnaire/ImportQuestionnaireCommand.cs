using System;
using Main.Core.Documents;
using Main.Core.Domain;
using Ncqrs.Commanding;
using Ncqrs.Commanding.CommandExecution.Mapping.Attributes;
using WB.Core.SharedKernels.DataCollection.Aggregates;

namespace WB.Core.SharedKernels.DataCollection.Commands.Questionnaire
{
    [MapsToAggregateRootMethodOrConstructor(typeof(Aggregates.Questionnaire), "ImportQuestionnaire")]
    public class ImportQuestionnaireCommand : CommandBase
    {
        public ImportQuestionnaireCommand(Guid createdBy, IQuestionnaireDocument source)
            : base(source.PublicKey)
        {
            CreatedBy = createdBy;
            Source = source;
            QuestionnaireId = source.PublicKey;
        }

        public Guid CreatedBy { get; private set; }

        public IQuestionnaireDocument Source { get; private set; }

        [AggregateRootId]
        public Guid QuestionnaireId { get; private set; }
    }
}
