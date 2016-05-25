﻿using System;
using System.Collections.Generic;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.SurveyManagement.Views.DataExport;
using WB.Core.SharedKernels.SurveyManagement.Views.Reposts;

namespace WB.Core.SharedKernels.SurveyManagement.Web.Models
{
    public class SpeedByInterviewersReportModel
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public IEnumerable<OrderRequestItem> SortOrder { get; set; }

        public Guid? SupervisorId { get; set; }
        public DateTime From { get; set; }
        public Guid QuestionnaireId { get; set; }
        public long QuestionnaireVersion { get; set; }
        public string Period { get; set; }
        public int ColumnCount { get; set; }
        public InterviewExportedAction[] InterviewStatuses { get; set; }
        public PeriodiceReportType ReportType { get; set; }
    }
}
