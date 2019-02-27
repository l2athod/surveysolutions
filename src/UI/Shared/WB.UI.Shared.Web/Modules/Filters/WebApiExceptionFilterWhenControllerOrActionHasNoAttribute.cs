﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using Autofac.Integration.WebApi;

namespace WB.UI.Shared.Web.Modules.Filters
{
    public class WebApiExceptionFilterWhenControllerOrActionHasNoAttribute<TFilter, TAttribute> : IAutofacExceptionFilter
        where TFilter : System.Web.Http.Filters.ExceptionFilterAttribute
        where TAttribute : Attribute
    {
        private readonly TFilter filter;

        public WebApiExceptionFilterWhenControllerOrActionHasNoAttribute(TFilter filter)
        {
            this.filter = filter;
        }

        public Task OnExceptionAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            var actionAttributes = actionExecutedContext.ActionContext.ActionDescriptor.GetCustomAttributes<TAttribute>();
            var controllerAttributes = actionExecutedContext.ActionContext.ControllerContext.ControllerDescriptor.GetCustomAttributes<TAttribute>();
            bool shouldExecute = (actionAttributes == null || actionAttributes.Count == 0)
                            && (controllerAttributes == null || controllerAttributes.Count == 0);

            if (shouldExecute)
            {
                return filter.OnExceptionAsync(actionExecutedContext, cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}