﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Moq;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.SurveyManagement.Web.Api;
using WB.Core.SharedKernels.SurveyManagement.Web.Utils.Membership;
using WB.Core.SharedKernels.SurveyManagement.Web.Views.User;
using It = Machine.Specifications.It;

namespace WB.UI.Supervisor.Tests.SyncControllerTests
{
    internal class when_pushing_file_with_valid_file : SyncControllerTestContext
    {
        Establish context = () =>
        {
            var userLight = new UserLight() { Name = "test" };
            var globalInfo = Mock.Of<IGlobalInfoProvider>(x => x.GetCurrentUser() == userLight);

            var user = new UserView();
            var userFactory = Mock.Of<IViewFactory<UserViewInputModel, UserView>>(x => x.Load(Moq.It.IsAny<UserViewInputModel>()) == user);
            plainFileRepository = new Mock<IPlainInterviewFileStorage>();
            controller = CreateSyncControllerWithFile(viewFactory: userFactory, stream: new MemoryStream(), plainFileRepository: plainFileRepository.Object, fileName: fileName, globalInfo: globalInfo);
            
        };

        Because of = () => result = controller.PostFile(interviewId).Result;

        It should_have_NotAcceptable_status_code = () =>
            result.StatusCode.ShouldEqual(HttpStatusCode.OK);

        It should_file_be_Saved_in_plain_file_storage = () =>
            plainFileRepository.Verify(x => x.StoreInterviewBinaryData(interviewId, fileName, Moq.It.IsAny<byte[]>()), Times.Once);

        private static HttpResponseMessage result;
        private static InterviewerSyncController controller;
        
        private static Mock<IPlainInterviewFileStorage> plainFileRepository;
        private static Guid interviewId = Guid.Parse("11111111111111111111111111111111");
        private static string fileName = "file.test";
        private static FormDataCollection formdata;
    }
}
