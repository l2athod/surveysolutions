﻿using System;
using WB.Core.BoundedContexts.Headquarters.Views.DataExport;

namespace WB.Core.BoundedContexts.Headquarters.Views.Reposts.InputModels
{
    public class QuantityBySupervisorsReportInputModel : ListViewModelBase
    {
        public QuantityBySupervisorsReportInputModel()
        {
            this.InterviewStatuses = new InterviewExportedAction[0];
        }

        public DateTime From { get; set; }
        public Guid QuestionnaireId { get; set; }
        public long QuestionnaireVersion { get; set; }
        public string Period { get; set; }
        public int ColumnCount { get; set; }
        public InterviewExportedAction[] InterviewStatuses { get; set; }
        public PeriodiceReportType ReportType { get; set; }
    }
}
