using System;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;

namespace WB.Core.BoundedContexts.Headquarters.Views.Interview
{
    public class AllInterviewsInputModel: ListViewModelBase
    {
        public Guid? QuestionnaireId { get; set; }

        public string TeamLeadName { get; set; }

        public Guid? InterviewId { get; set; }

        public InterviewStatus? Status { get; set; }

        public long? QuestionnaireVersion { get; set; }

        public string SearchBy { get; set; }

        public int? AssignmentId { get; set; }
    }

    public class InterviewsWithoutPrefilledInputModel : ListViewModelBase
    {
        public QuestionnaireIdentity QuestionnaireId { get; set; }

        public DateTime? ChangedFrom { get; set; }

        public DateTime? ChangedTo { get; set; }

        public Guid? InterviewerId { get; set; }

        public bool CensusOnly { get; set; } = false;

        public string SearchBy { get; set; }

        public string InterviewKey { get; set; }

        public Guid? InterviewId { get; set; }
        public Guid? SupervisorId { get; set; }
    }
}