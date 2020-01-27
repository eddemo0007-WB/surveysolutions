﻿using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NLog.Web.LayoutRenderers;
using UAParser;

namespace WB.UI.Headquarters.Filters
{
    public class BrowsersRestrictionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.ActionDescriptor.DisplayName != "OutdatedBrowser" && filterContext.HttpContext.Request.Headers.ContainsKey("User-Agent"))
            {
                string userAgentString = filterContext.HttpContext.Request.Headers["User-Agent"].ToString();
                var parser = Parser.GetDefault();
                var userAgent = parser.ParseUserAgent(userAgentString);
                if (IsInternetExplorer(userAgent) && IsAllowGetMajorVersion(userAgent))
                {
                    var routeValueDictionary = new RouteValueDictionary(new
                    {
                        controller = "WebInterview",
                        action = "OutdatedBrowser"
                    });
                    filterContext.Result = new RedirectToRouteResult(routeValueDictionary);
                }
            }
        }

        private bool IsAllowGetMajorVersion(UserAgent userAgent)
        {
            if (int.TryParse(userAgent.Major, out int version))
                return version < 10;
            return true;
        }

        public static bool IsInternetExplorer(UserAgent userAgent)
        {
            if (userAgent.Family.Contains("MSIE") || userAgent.Family.Contains("Trident"))
            {
                return true;
            }
            return false;
        }
    }
}
