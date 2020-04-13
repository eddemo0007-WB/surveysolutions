﻿namespace WB.Core.SharedKernels.DataCollection.Views.InterviewerAuditLog.Entities
{
    public class SynchronizationCompletedAuditLogEntity : BaseAuditLogEntity
    {
        public int NewAssignmentsCount { get; }
        public int RemovedAssignmentsCount { get; }
        public int NewInterviewsCount { get; }
        public int SuccessfullyUploadedInterviewsCount { get; }
        public int RejectedInterviewsCount { get; }
        public int DeletedInterviewsCount { get; }
        public int SuccessfullyDownloadedPatchesForInterviewsCount { get; }
        public int ReopenedInterviewsAfterReceivedCommentsCount { get; }

        public SynchronizationCompletedAuditLogEntity(
            int newAssignmentsCount, 
            int removedAssignmentsCount, 
            int newInterviewsCount, 
            int successfullyUploadedInterviewsCount, 
            int rejectedInterviewsCount, 
            int deletedInterviewsCount,
            int successfullyDownloadedPatchesForInterviewsCount = 0,
            int reopenedInterviewsAfterReceivedCommentsCount = 0) 
            : base(AuditLogEntityType.SynchronizationCompleted)
        {
            NewAssignmentsCount = newAssignmentsCount;
            RemovedAssignmentsCount = removedAssignmentsCount;
            NewInterviewsCount = newInterviewsCount;
            SuccessfullyUploadedInterviewsCount = successfullyUploadedInterviewsCount;
            RejectedInterviewsCount = rejectedInterviewsCount;
            DeletedInterviewsCount = deletedInterviewsCount;
            SuccessfullyDownloadedPatchesForInterviewsCount = successfullyDownloadedPatchesForInterviewsCount;
            ReopenedInterviewsAfterReceivedCommentsCount = reopenedInterviewsAfterReceivedCommentsCount;
        }
    }
}
