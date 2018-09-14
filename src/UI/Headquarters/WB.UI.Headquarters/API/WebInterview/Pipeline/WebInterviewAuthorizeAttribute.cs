using System;
using System.Linq;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Enumerator.Native.WebInterview;
using WB.UI.Shared.Enumerator.Services.Internals;

namespace WB.UI.Headquarters.API.WebInterview.Pipeline
{
    public class WebInterviewAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly IServiceLocator serviceLocator;
        
        public WebInterviewAuthorizeAttribute()
        {
            this.serviceLocator = ServiceLocator.Current;
        }

        public override bool AuthorizeHubConnection(HubDescriptor hubDescriptor, IRequest request)
        {
            serviceLocator.ExecuteActionInScope((serviceLocatorLocal) =>
            {
                CheckPermissions(request, serviceLocatorLocal);
            });

            return true;
        }

        public override bool AuthorizeHubMethodInvocation(IHubIncomingInvokerContext hubIncomingInvokerContext, 
            bool appliesToMethod)
        {
            return serviceLocator.ExecuteFunctionInScope((serviceLocatorLocal) =>
            {
                try
                {
                    if (hubIncomingInvokerContext.MethodDescriptor.Attributes.Any(x =>
                        x.TypeId.ToString() == ObserverNotAllowedAttribute.Id))
                    {
                        if (serviceLocator.GetInstance<IAuthorizedUser>().IsObserving)
                            return false;
                    }

                    CheckPermissions(hubIncomingInvokerContext.Hub.Context.Request, serviceLocatorLocal);

                    return true;
                }
                catch (InterviewAccessException)
                {
                    var interviewId = hubIncomingInvokerContext.Hub.Context.QueryString.Get(@"interviewId");
                    if (!interviewId.IsNullOrWhiteSpace())
                    {
                        serviceLocatorLocal.GetInstance<IWebInterviewNotificationService>().ReloadInterview(Guid.Parse(interviewId));
                    }
                }

                var authorizeHubMethodInvocation =
                    base.AuthorizeHubMethodInvocation(hubIncomingInvokerContext, appliesToMethod);

                return authorizeHubMethodInvocation;
            });

            
        }

        private void CheckPermissions(IRequest hub, IServiceLocator locator)
        {
            var isReview = hub.QueryString[@"review"].ToBool(false);
            var interviewId = hub.QueryString.Get(@"interviewId");

            if (!isReview)
            {
                locator.GetInstance<IWebInterviewAllowService>().CheckWebInterviewAccessPermissions(interviewId);
            }
            else
            {
                locator.GetInstance<IReviewAllowedService>().CheckIfAllowed(Guid.Parse(interviewId));
            }
        }
    }
}
