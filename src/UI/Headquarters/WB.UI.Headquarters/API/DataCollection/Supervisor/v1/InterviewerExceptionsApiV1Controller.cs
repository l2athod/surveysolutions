﻿using System;
using System.Collections.Generic;
using System.Web.Http;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Headquarters.Views.SynchronizationLog;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.UI.Headquarters.Code;

namespace WB.UI.Headquarters.API.DataCollection.Supervisor.v1
{
    [ApiBasicAuth(new[] { UserRoles.Supervisor })]
    public class InterviewerExceptionsApiV1Controller : ApiController
    {
        private readonly IPlainStorageAccessor<SynchronizationLogItem> syncLogRepository;

        public InterviewerExceptionsApiV1Controller(IPlainStorageAccessor<SynchronizationLogItem> syncLogRepository)
        {
            this.syncLogRepository = syncLogRepository;
        }

        [HttpPost]
        public IHttpActionResult Post(List<InterviewerExceptionInfo> exceptions)
        {
            foreach (var exception in exceptions)
            {
                this.syncLogRepository.Store(new SynchronizationLogItem
                {
                    InterviewerId = exception.InterviewerId,
                    LogDate = DateTime.UtcNow,
                    Type = SynchronizationLogType.DeviceUnexpectedException,
                    Log = $@"<pre><font color=""red"">{exception.StackTrace.Replace("\r\n", "<br />")}</font></pre>"
                }, Guid.NewGuid());
            }

            return this.Ok();
        }
    }
}
