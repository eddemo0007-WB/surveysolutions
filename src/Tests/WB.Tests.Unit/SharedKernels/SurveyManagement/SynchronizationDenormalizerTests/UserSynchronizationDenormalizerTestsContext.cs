﻿using System;
using System.Collections.Generic;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Moq;

using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection.Views;
using WB.Core.SharedKernels.SurveyManagement.EventHandler;
using WB.Core.SharedKernels.SurveyManagement.Services;
using WB.Core.Synchronization.SyncStorage;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.SynchronizationDenormalizerTests
{
    [Subject(typeof(UserSynchronizationDenormalizer))]
    internal class UserSynchronizationDenormalizerTestsContext
    {
        protected const string CounterId = "UserSyncPackageСounter";

        protected static UserSynchronizationDenormalizer CreateDenormalizer(
             IReadSideRepositoryWriter<UserDocument> users = null,
            ISerializer serializer = null,
            IReadSideRepositoryWriter<UserSyncPackageMeta> userPackageStorageWriter = null)
        {
            var result = new UserSynchronizationDenormalizer(
                users ?? Mock.Of<IReadSideRepositoryWriter<UserDocument>>(),
                serializer ?? Mock.Of<ISerializer>(),
                userPackageStorageWriter ?? Mock.Of<IReadSideRepositoryWriter<UserSyncPackageMeta>>());

            return result;
        }

        protected static UserDocument CreateUserDocument(Guid userId)
        {
            var userDocument = new UserDocument
                               {
                                   PublicKey = userId,
                                   Roles = new HashSet<UserRoles> { UserRoles.Operator }
                               };

            return userDocument;
        }
    }
}