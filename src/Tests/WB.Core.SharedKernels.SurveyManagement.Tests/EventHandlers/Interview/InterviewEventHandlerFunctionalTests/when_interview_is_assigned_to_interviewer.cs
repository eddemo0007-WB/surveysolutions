using System;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Moq;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.Infrastructure.FunctionalDenormalization.Implementation.ReadSide;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Synchronization;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.SurveyManagement.EventHandler;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;
using WB.Core.Synchronization;
using It = Machine.Specifications.It;

namespace WB.Core.SharedKernels.SurveyManagement.Tests.EventHandlers.Interview.InterviewEventHandlerFunctionalTests
{
    internal class when_interview_is_assigned_to_interviewer : InterviewEventHandlerFunctionalTestContext
    {
        Establish context = () =>
        {
            viewState = CreateViewWithSequenceOfInterviewData();
            interviewEventHandlerFunctional = CreateInterviewEventHandlerFunctional();
            viewState.Document.Status = InterviewStatus.ReadyForInterview;

            interviewerId = Guid.NewGuid();
            interviewerAssignedEvent = CreatePublishableEvent(new InterviewerAssigned(Guid.NewGuid(), interviewerId));
        };

        Because of = () => interviewEventHandlerFunctional.Update(viewState, interviewerAssignedEvent);


        It should_change_responsible_to_interviewer = () => viewState.Document.ResponsibleId.ShouldEqual(interviewerId);

        It should_set_responsible_role_to_Operator = () => viewState.Document.ResponsibleRole.ShouldEqual(UserRoles.Operator);


        static InterviewEventHandlerFunctional interviewEventHandlerFunctional;
        static ViewWithSequence<InterviewData> viewState;
        static Guid interviewerId;
        static IPublishedEvent<InterviewerAssigned> interviewerAssignedEvent;
    }
}