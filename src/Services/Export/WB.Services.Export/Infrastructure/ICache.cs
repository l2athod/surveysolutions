﻿using WB.Services.Export.Questionnaire;
using WB.Services.Infrastructure.Tenant;

namespace WB.Services.Export.Infrastructure
{
    public interface ICache
    {
        object Get(QuestionnaireId key, TenantId tenantId);
        void Set(string key, object value, TenantId tenantId);
    }
}
