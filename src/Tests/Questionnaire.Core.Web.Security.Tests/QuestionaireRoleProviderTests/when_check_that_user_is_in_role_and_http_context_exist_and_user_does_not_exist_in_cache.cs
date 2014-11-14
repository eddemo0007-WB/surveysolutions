﻿using System;
using System.Collections.Generic;
using System.Web;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Microsoft.Practices.ServiceLocation;
using Moq;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.SharedKernels.SurveyManagement.Web.Utils.Security;
using WB.Core.SharedKernels.SurveyManagement.Web.Views.User;
using It = Machine.Specifications.It;

namespace Questionnaire.Core.Web.Security.Tests.QuestionaireRoleProviderTests
{
    internal class when_check_that_user_is_in_role_and_http_context_exist_and_user_does_not_exist_in_cache : QuestionnaireRoleProviderTestsContext
    {
        Establish context = () =>
        {
            var userViewFactoryMock = new Mock<IViewFactory<UserViewInputModel, UserView>>();
            userViewFactoryMock.Setup(_ => _.Load(Moq.It.IsAny<UserViewInputModel>()))
                .Returns(new UserView() {Roles = new List<UserRoles>()});

            var serviceLocatorMock = new Mock<IServiceLocator> {DefaultValue = DefaultValue.Mock};
            serviceLocatorMock.Setup(_ =>_.GetInstance<IViewFactory<UserViewInputModel, UserView>>()).Returns(userViewFactoryMock.Object);
            ServiceLocator.SetLocatorProvider(() => serviceLocatorMock.Object);

            HttpContext.Current = new HttpContext(new HttpRequest(null, "http://tempuri.org", null), new HttpResponse(null));
            
            provider = CreateProvider();
        };

        Because of = () => 
            exception = Catch.Exception(() => provider.IsUserInRole("some_user_name", "some_role"));

        It should_exception_be_null = () =>
            exception.ShouldBeNull();

        private static QuestionnaireRoleProvider provider;
        private static Exception exception;
    }
}
