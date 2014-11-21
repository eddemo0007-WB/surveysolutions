﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web;
using Machine.Specifications;
using Moq;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.SurveyManagement.Web.Api;
using WB.Core.SharedKernels.SurveyManagement.Web.Models.User;
using It = Machine.Specifications.It;

namespace WB.UI.Supervisor.Tests.SyncControllerTests
{
    internal class when_pushing_file_which_rise_exception_during_saving : SyncControllerTestContext
    {
        Establish context = () =>
        {
            var user = new UserView();
            var userFactory = Mock.Of<IViewFactory<UserViewInputModel, UserView>>(x => x.Load(Moq.It.IsAny<UserViewInputModel>()) == user);
            plainFileRepository = new Mock<IPlainInterviewFileStorage>();
            plainFileRepository.Setup(x => x.StoreInterviewBinaryData(interviewId, Moq.It.IsAny<string>(), Moq.It.IsAny<byte[]>()))
                .Throws<ArgumentException>();

            controller = CreateSyncControllerWithFile(viewFactory: userFactory, stream: new MemoryStream(), plainFileRepository: plainFileRepository.Object, fileName: fileName);
        
        };

        Because of = () =>
            result = controller.PostFile(interviewId ).Result;

        It should_have_ServiceUnavailable_status_code = () =>
            result.StatusCode.ShouldEqual(HttpStatusCode.ServiceUnavailable);


        private static HttpResponseMessage result;
        private static InterviewerSyncController controller;
        private static HttpException exception;
        private static Mock<IPlainInterviewFileStorage> plainFileRepository;
        private static string fileName = "file name";

        private static Guid interviewId = Guid.Parse("11111111111111111111111111111111");
    }
}
