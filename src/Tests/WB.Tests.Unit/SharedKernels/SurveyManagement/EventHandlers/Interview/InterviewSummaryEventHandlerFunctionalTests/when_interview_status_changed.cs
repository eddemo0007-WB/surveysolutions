﻿using Machine.Specifications;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.SurveyManagement.EventHandler;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.EventHandlers.Interview.InterviewSummaryEventHandlerFunctionalTests
{
    internal class when_interview_status_changed : InterviewSummaryDenormalizerTestsContext
    {
        Establish context = () =>
        {
            viewModel = new InterviewSummary();
            viewModel.WasRejectedBySupervisor = true;
            denormalizer = CreateDenormalizer();
        };

        Because of = () => updatedModel = denormalizer.Update(viewModel, Create.PublishedEvent.InterviewStatusChangedEvent(InterviewStatus.InterviewerAssigned));

        It should_change_interview_status = () => updatedModel.Status.ShouldEqual(InterviewStatus.InterviewerAssigned);

        It should_not_mark_summary_as_deleted = () => updatedModel.IsDeleted.ShouldBeFalse();

        It should_not_change_WasRejectedBySupervisor_flag = () => updatedModel.WasRejectedBySupervisor.ShouldBeTrue();

        static InterviewSummary viewModel;
        static InterviewSummaryDenormalizer denormalizer;
        static InterviewSummary updatedModel;
    }
}

