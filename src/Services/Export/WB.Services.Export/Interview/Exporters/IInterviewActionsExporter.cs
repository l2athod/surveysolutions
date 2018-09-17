﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WB.Services.Export.Questionnaire;
using WB.Services.Export.Tenant;

namespace WB.Services.Export.Interview.Exporters
{
    public interface IInterviewActionsExporter
    {
        Task ExportAsync(TenantInfo tenant, QuestionnaireId questionnaireIdentity, List<Guid> interviewIdsToExport,
            string basePath, IProgress<int> progress);
    }
}
