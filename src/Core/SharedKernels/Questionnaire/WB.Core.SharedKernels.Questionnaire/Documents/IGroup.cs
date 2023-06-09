using System;
using Main.Core.Entities.Composite;
using WB.Core.SharedKernels.SurveySolutions.Documents;

namespace Main.Core.Entities.SubEntities
{
    public interface IGroup : IComposite, IConditional
    {
        string Title { get; set; } 

        string Description { get; set; }

        new string VariableName { get; set; } 

        bool IsRoster { get; }

        Guid? RosterSizeQuestionId { get; }

        RosterSizeSourceType RosterSizeSource { get; }

        FixedRosterTitle[] FixedRosterTitles { get; }

        Guid? RosterTitleQuestionId { get; }

        RosterDisplayMode DisplayMode { get; }

        bool CustomRosterTitle { get; }

        void ReplaceChildEntityById(Guid id, IComposite newEntity);
    }
}
