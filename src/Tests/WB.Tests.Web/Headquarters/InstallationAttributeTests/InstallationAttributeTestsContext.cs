﻿using System.Collections.Generic;
using Amazon.Runtime.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using WB.Core.BoundedContexts.Headquarters.Users;
using WB.Tests.Abc;

using WB.UI.Headquarters.Filters;

namespace WB.Tests.Unit.Applications.Headquarters.FilterTests.InstallationAttributeTests
{
    internal class InstallationAttributeTestsContext
    {
        protected static InstallationFilter CreateInstallationAttribute(IUserRepository userRepository = null)
        {
            var installationAttribute = new InstallationFilter(userRepository ?? Create.Storage.UserRepository());
            InstallationFilter.Installed = false;
            return installationAttribute;
        }

        protected static ActionExecutingContext CreateFilterContext(ControllerBase specifiedController = null)
        {
            return new ActionExecutingContext(new ActionContext
                {
                    HttpContext = new DefaultHttpContext(),
                    RouteData = new RouteData(),
                    ActionDescriptor = new ActionDescriptor()
                }, 
                new List<IFilterMetadata>(), 
                new Dictionary<string, object>(), 
                specifiedController);
        }
    }
}
