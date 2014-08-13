﻿using System;
using System.Collections.Generic;
using Main.Core.Documents;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.SharedKernel.Structures.Synchronization;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Synchronization;
using WB.Core.Synchronization.SyncStorage;

namespace WB.Core.Synchronization
{
    public interface ISynchronizationDataStorage
    {
        void SaveInterview(InterviewSynchronizationDto doc, Guid responsibleId, DateTime timestamp);
        void SaveQuestionnaire(QuestionnaireDocument doc, long version, bool allowCensusMode, DateTime timestamp);
        void MarkInterviewForClientDeleting(Guid id, Guid? responsibleId, DateTime timestamp);
        void SaveImage(Guid publicKey, string title, string desc, string origData, DateTime timestamp);
        void SaveUser(UserDocument doc, DateTime timestamp);
        SyncItem GetLatestVersion(Guid id);
        IEnumerable<SynchronizationChunkMeta> GetChunkPairsCreatedAfter(DateTime timestamp, Guid userId);
        void DeleteQuestionnaire(Guid questionnaireId, long questionnaireVersion, DateTime timestamp);
    }
}