﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Services;
using Autofac.Integration.WebApi;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;

namespace WB.UI.Shared.Web.Modules.Filters
{
    public class WebApiActionFilterWhenControllerOrActionHasAttribute<TFilter, TAttribute> : IAutofacActionFilter
        where TFilter : System.Web.Http.Filters.ActionFilterAttribute
        where TAttribute : Attribute
    {
        public WebApiActionFilterWhenControllerOrActionHasAttribute(TFilter filter)
        {
            this.filter = filter;
        }

        private bool shouldExecute = false;
        private readonly TFilter filter;

        public Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            var actionAttributes = actionContext.ActionDescriptor.GetCustomAttributes<TAttribute>();
            var controllerAttributes = actionContext.ControllerContext.ControllerDescriptor.GetCustomAttributes<TAttribute>();
            shouldExecute = (actionAttributes != null && actionAttributes.Count > 0)
                            || (controllerAttributes != null && controllerAttributes.Count > 0);

            if (shouldExecute)
            {
                return filter.OnActionExecutingAsync(actionContext, cancellationToken);
            }

            return Task.CompletedTask;
        }

        public Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            if (shouldExecute)
            {
                return filter.OnActionExecutedAsync(actionExecutedContext, cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}