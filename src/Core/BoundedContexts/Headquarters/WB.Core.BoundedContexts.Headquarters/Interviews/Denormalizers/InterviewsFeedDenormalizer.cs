﻿using System;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.GenericSubdomains.Utils;
using WB.Core.Infrastructure.EventBus;
using WB.Core.Infrastructure.FunctionalDenormalization.Implementation.ReadSide;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.SurveyManagement.Synchronization.Interview;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;

namespace WB.Core.BoundedContexts.Headquarters.Interviews.Denormalizers
{
    internal class InterviewsFeedDenormalizer : BaseDenormalizer,
        IEventHandler<SupervisorAssigned>,
        IEventHandler<InterviewDeleted>
    {
        private readonly IReadSideRepositoryWriter<InterviewFeedEntry> writer;
        private readonly IReadSideRepositoryWriter<ViewWithSequence<InterviewData>> interviews;

        public InterviewsFeedDenormalizer(IReadSideRepositoryWriter<InterviewFeedEntry> writer,
            IReadSideRepositoryWriter<ViewWithSequence<InterviewData>> interviews)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (interviews == null) throw new ArgumentNullException("interviews");
            this.writer = writer;
            this.interviews = interviews;
        }

        public void Handle(IPublishedEvent<SupervisorAssigned> evnt)
        {
            writer.Store(new InterviewFeedEntry
            {
                SupervisorId = evnt.Payload.SupervisorId.FormatGuid(),
                InterviewId = evnt.EventSourceId.FormatGuid(),
                EntryType = EntryType.SupervisorAssigned,
                Timestamp = evnt.EventTimeStamp,
                EntryId = evnt.EventIdentifier.FormatGuid(),
                UserId = evnt.Payload.UserId.FormatGuid()
            }, evnt.EventIdentifier);
        }

        public void Handle(IPublishedEvent<InterviewDeleted> evnt)
        {
            this.writer.Store(new InterviewFeedEntry
            {
                SupervisorId = evnt.Payload.UserId.FormatGuid(),
                EntryType = EntryType.InterviewUnassigned,
                Timestamp = evnt.EventTimeStamp,
                InterviewId = evnt.EventSourceId.FormatGuid(),
                EntryId = evnt.EventIdentifier.FormatGuid(),
                UserId = evnt.Payload.UserId.FormatGuid()
            }, evnt.EventIdentifier);
        }

        public override Type[] BuildsViews
        {
            get { return new[] { typeof (InterviewFeedEntry) }; }
        }
    }
}