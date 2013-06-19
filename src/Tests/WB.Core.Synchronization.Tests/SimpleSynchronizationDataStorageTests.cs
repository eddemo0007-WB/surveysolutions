﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Main.Core;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using Moq;
using NUnit.Framework;
using WB.Core.Infrastructure;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernel.Structures.Synchronization;
using WB.Core.Synchronization.SyncStorage;

namespace WB.Core.Synchronization.Tests
{
    [TestFixture]
    public class SimpleSynchronizationDataStorageTests
    {
        [Test]
        public void SaveQuestionnarie_When_questionnarie_is_valied_Then_questionnarie_returned()
        {
            // arrange
            var questionnarieId = Guid.Parse("23333333-3333-3333-3333-333333333333");

            var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var supervisorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            SimpleSynchronizationDataStorage target = CreateSimpleSynchronizationDataStorageWithOneSupervisorAndOneUser(supervisorId, userId);

            // act
            target.SaveQuestionnarie(new CompleteQuestionnaireStoreDocument(){PublicKey = questionnarieId}, userId);

            // assert
            var result = target.GetLatestVersion(questionnarieId);
            Assert.That(result.ItemType, Is.EqualTo(SyncItemType.Questionnare));
            Assert.That(result.Id, Is.EqualTo(questionnarieId));
            Assert.That(result.IsCompressed, Is.EqualTo(true));
        }

        [Test]
        public void SaveUser_When_quesr_is_valid_Then_user_is_returned()
        {
            // arrange
            var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var supervisorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var userName = "testUser";
            var testpassword = "testPassword";

            SimpleSynchronizationDataStorage target = CreateSimpleSynchronizationDataStorageWithOneSupervisor(supervisorId);

            // act
            target.SaveUser(new UserDocument()
                {
                    PublicKey = userId,
                    UserName = userName,
                    Password = testpassword,
                    Roles = new List<UserRoles>() {UserRoles.Operator},
                    Supervisor = new UserLight(supervisorId, "")
                });

            // assert
            var result = target.GetLatestVersion(userId);
            Assert.That(result.ItemType, Is.EqualTo(SyncItemType.User));
            Assert.That(result.Id, Is.EqualTo(userId));
            Assert.That(result.IsCompressed, Is.EqualTo(true));
        }

        [Test]
        public void DeleteQuestionnarie_When_questionnarie_is_valid_Then_last_stored_chunk_by_questionnarie_is_command_For_delete()
        {
            // arrange
            var questionnarieId = Guid.Parse("23333333-3333-3333-3333-333333333333");
            var supervisorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            SimpleSynchronizationDataStorage target = CreateSimpleSynchronizationDataStorageWithOneSupervisorAndOneUser(supervisorId, userId);
            
            // act
            target.DeleteQuestionnarie(questionnarieId, userId);

            // assert
            var result = target.GetLatestVersion(questionnarieId);
            Assert.That(result.ItemType, Is.EqualTo(SyncItemType.DeleteQuestionnare));
            Assert.That(result.Id, Is.EqualTo(questionnarieId));
            Assert.That(result.Content, Is.EqualTo(questionnarieId.ToString()));
        }
        private SimpleSynchronizationDataStorage CreateSimpleSynchronizationDataStorageWithOneSupervisor(Guid supervisorId)
        {
            return
                CreateSimpleSynchronizationDataStorageWithOneSupervisorAndOneUser(supervisorId, Guid.NewGuid());

        }
        private SimpleSynchronizationDataStorage CreateSimpleSynchronizationDataStorageWithOneSupervisorAndOneUser(Guid supervisorId, Guid userId)
        {
            var inmemoryChunkStorage = new InMemoryChunkStorage();
          
           
            var userStorageMock = new Mock<IQueryableReadSideRepositoryReader<UserDocument>>();

            var retval =
                new SimpleSynchronizationDataStorage(userStorageMock.Object, inmemoryChunkStorage);

            retval.SaveUser(new UserDocument()
            {
                PublicKey = userId,
                Roles = new List<UserRoles>() { UserRoles.Operator },
                Supervisor = new UserLight(supervisorId, "")
            });
            retval.SaveUser(new UserDocument()
            {
                PublicKey = supervisorId,
                Roles = new List<UserRoles>() { UserRoles.Supervisor }
            });
            return retval;

        }
    }
}
