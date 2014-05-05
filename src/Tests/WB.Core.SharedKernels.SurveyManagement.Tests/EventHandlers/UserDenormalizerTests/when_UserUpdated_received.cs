﻿using System;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Events.User;
using Moq;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.SurveyManagement.EventHandler;
using WB.Core.Synchronization;
using It = Machine.Specifications.It;

namespace WB.Core.SharedKernels.SurveyManagement.Tests.EventHandlers.UserDenormalizerTests
{
    internal class when_UserUpdated_received : UserDenormalizerContext
    {
        private Establish context = () =>
        {
            userId = Guid.Parse("22222222222222222222222222222222");
            commandExecutorId = Guid.Parse("33333333333333333333333333333333");

            syncStorage = new Mock<ISynchronizationDataStorage>();
            syncStorage.Setup(x => x.SaveUser(Moq.It.IsAny<UserDocument>()))
                .Callback((UserDocument userDoc) => userToSave = userDoc);
            
            var user = new UserDocument() { PublicKey = userId };

            var userDocumentMockStorage = new Mock<IReadSideRepositoryWriter<UserDocument>>();
            userDocumentMockStorage.Setup(x => x.GetById(Moq.It.IsAny<string>())).Returns(user);

            denormalizer = CreateUserDenormalizer(users: userDocumentMockStorage.Object, syncStorage: syncStorage.Object);

            userChangedEvnt = CreateUserChanged(userId);
        };

        private Because of = () =>
            denormalizer.Handle(userChangedEvnt);

        private It should_sync_storage_stores_new_state = () =>
            syncStorage.Verify(x => x.SaveUser(Moq.It.IsAny<UserDocument>()), Times.Once);

        private static UserDenormalizer denormalizer;
        private static Guid commandExecutorId;
        private static IPublishedEvent<UserChanged> userChangedEvnt;
        private static Guid userId;
        private static Mock<ISynchronizationDataStorage> syncStorage;

        private static UserDocument userToSave;

    }
}
