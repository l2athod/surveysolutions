﻿namespace WB.Core.BoundedContexts.Headquarters.Views.Reposts.SurveyStatistics.Data
{
    /// <summary>
    /// Result of readside.get_categorical_report function
    /// </summary>
    public class GetCategoricalReportItem 
    {
        public string ResponsibleName { get; set; }
        public string TeamLeadName { get; set; }
        public long Answer { get; set; }
        public long Count { get; set; }
        public string AnswerText { get; set; }

        public override string ToString() 
            => $"{TeamLeadName} | {ResponsibleName} | {Answer} {AnswerText} = {Count}";
    }
}
