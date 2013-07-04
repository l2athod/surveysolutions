﻿namespace WB.UI.Designer.Exceptions
{
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;

    using WB.Core.SharedKernel.Utils.Logging;

    /// <summary>
    /// The custom handle error attribute.
    /// </summary>
    public class CustomHandleErrorAttribute : FilterAttribute, IExceptionFilter
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomHandleErrorAttribute"/> class.
        /// </summary>
        public CustomHandleErrorAttribute()
        {
            this.Order = 1;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The on exception.
        /// </summary>
        /// <param name="filterContext">
        /// The filter context.
        /// </param>
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled || !filterContext.HttpContext.IsCustomErrorEnabled)
            {
                return;
            }

            if (new HttpException(null, filterContext.Exception).GetHttpCode() != 500)
            {
                return;
            }

            // if the request is AJAX return JSON else view.
            if (filterContext.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                filterContext.Result = new JsonResult
                                           {
                                               JsonRequestBehavior = JsonRequestBehavior.AllowGet, 
                                               Data =
                                                   new
                                                       {
                                                           error = true, 
                                                           message = filterContext.Exception.Message
                                                       }
                                           };
            }
            else
            {
                filterContext.Result =
                    new RedirectToRouteResult(
                        new RouteValueDictionary { { "controller", "Error" }, { "action", "Index" } });
            }

            // log the error 
            LogManager.GetLogger(this.GetType()).Error(filterContext.Exception.Message, filterContext.Exception);

            filterContext.ExceptionHandled = true;
            filterContext.HttpContext.Response.Clear();
            filterContext.HttpContext.Response.StatusCode = 500;

            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
        }

        #endregion
    }
}