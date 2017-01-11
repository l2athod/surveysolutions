using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection;

namespace WB.UI.Headquarters.Models.WebInterview
{
    public class InterviewTextQuestion : GenericQuestion
    {
    }

    public class InterviewSingleOptionQuestion : CategoricalQuestion
    {
        public int? Answer { get; set; }
    }

    public class CategoricalQuestion: GenericQuestion
    {
        public List<CategoricalOption> Options { get; set; }
    }

    public abstract class GenericQuestion : InterviewEntity
    {
        public string Instructions { get; set; }
        public bool HideInstructions { get; set; }
        public bool IsAnswered { get; set; }
    }

    public abstract class InterviewEntity
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }

    /// <summary>
    /// Used during dev, should be deleted when all types of questions are implemented
    /// </summary>
    public class StubEntity : GenericQuestion
    {
    }
}