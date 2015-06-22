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
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Services;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.DataCollection.InterviewTests
{
    internal class when_interview_is_creating_but_max_number_of_allowed_interviews_is_reached : InterviewTestsContext
    {
        private Establish context = () =>
        {
            questionnaireId = Guid.Parse("33333333333333333333333333333333");

            SetupInstanceToMockedServiceLocator<IInterviewPreconditionsService>(
                Mock.Of<IInterviewPreconditionsService>(
                        _ => _.GetMaxAllowedInterviewsCount() == maxNumberOfInterviews && _.GetInterviewsCountAllowedToCreateUntilLimitReached() == 0));

        };

        Because of = () =>
            exception = Catch.Only<InterviewException>(() => CreateInterview(questionnaireId: questionnaireId));

        It should_raise_InterviewException = () =>
            exception.ShouldNotBeNull();

        It should_raise_InterviewException_with_type_InterviewLimitReached = () =>
           exception.ExceptionType.ShouldEqual(InterviewDomainExceptionType.InterviewLimitReached);

        It should_throw_exception_that_contains_such_words = () =>
            exception.Message.ToLower().ToSeparateWords().ShouldContain("max", "number", "interviews", "'" + maxNumberOfInterviews + "'", "reached");

        Cleanup stuff = () =>
        {
            SetupInstanceToMockedServiceLocator<IInterviewPreconditionsService>(Mock.Of<IInterviewPreconditionsService>());
        };

        private static Guid questionnaireId;
        private static long maxNumberOfInterviews = 1;
        private static InterviewException exception;
    }
}
