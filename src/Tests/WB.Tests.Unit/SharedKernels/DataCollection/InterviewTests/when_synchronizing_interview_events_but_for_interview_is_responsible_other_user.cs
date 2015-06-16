﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Microsoft.Practices.ServiceLocation;
using Moq;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.DataCollection.InterviewTests
{
    internal class when_synchronizing_interview_events_but_for_interview_is_responsible_other_user : InterviewTestsContext
    {
        Establish context = () =>
        {
            eventContext = new EventContext();
            var questionnaire = Mock.Of<IQuestionnaire>(_
                => _.Version == questionnaireVersion);

            var questionnaireRepository = Mock.Of<IQuestionnaireRepository>(repository
                =>
                repository.GetHistoricalQuestionnaire(questionnaireId, questionnaireVersion) == questionnaire);

            Mock.Get(ServiceLocator.Current)
                .Setup(locator => locator.GetInstance<IQuestionnaireRepository>())
                .Returns(questionnaireRepository);
            interview = new Interview();
            interview.Apply(new InterviewStatusChanged(InterviewStatus.InterviewerAssigned, ""));
            interview.Apply(new InterviewerAssigned(userId, userId, DateTime.Now));
        };

        Cleanup stuff = () =>
        {
            eventContext.Dispose();
            eventContext = null;
        };

        Because of = () =>
            exception = Catch.Only<InterviewException>(() =>
               interview.SynchronizeInterviewEvents(Guid.NewGuid(), questionnaireId, questionnaireVersion,
               InterviewStatus.Completed, eventsToPublish, false));

        It should_raise_InterviewException = () =>
           exception.ShouldNotBeNull();

        It should_raise_InterviewException_with_type_OtherUserIsResponsible = () =>
            exception.ExceptionType.ShouldEqual(InterviewDomainExceptionType.OtherUserIsResponsible);

        private static EventContext eventContext;
        private static Guid questionnaireId = Guid.Parse("10000000000000000000000000000000");
        private static Guid userId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        private static long questionnaireVersion = 18;

        private static Interview interview;

        private static InterviewException exception;

        private static object[] eventsToPublish = new object[] { new AnswersDeclaredInvalid(new Identity[0]), new GroupsEnabled(new Identity[0]) };
    }
}
