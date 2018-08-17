﻿using Microsoft.AspNet.SignalR.Hubs;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.Transactions;

namespace WB.UI.Headquarters.API.WebInterview.Pipeline
{
    public class PlainSignalRTransactionManager : HubPipelineModule
    {
        protected override bool OnBeforeIncoming(IHubIncomingInvokerContext context)
        {
            // TODO CHECK HOW TO HANDLE TRANSACTIONS HERE
            return base.OnBeforeIncoming(context);
        }

        protected override object OnAfterIncoming(object result, IHubIncomingInvokerContext context)
        {
            return base.OnAfterIncoming(result, context);
        }

        protected override void OnIncomingError(ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
        {
            base.OnIncomingError(exceptionContext, invokerContext);
        }
    }
}
