﻿using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using WB.Core.Infrastructure.Implementation.ReadSide;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.SurveyManagement.EventHandler;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;

namespace WB.Core.SharedKernels.SurveyManagement.Tests.EventHandlers.Interview.InterviewEventHandlerFunctionalTests
{
    internal class when_NumericRealQuestionAnswered_recived_and_answered_question_is_roster_title : InterviewEventHandlerFunctionalTestContext
    {
        Establish context = () =>
        {
            rosterGroupId = Guid.Parse("10000000000000000000000000000000");
            rosterScopeId = Guid.Parse("12222222222222222222222222222222");
            rosterTitleQuestionId = Guid.Parse("13333333333333333333333333333333");
            viewState = CreateViewWithSequenceOfInterviewData();
            var questionnaireRosterStructure = CreateQuestionnaireRosterStructure(rosterScopeId,
                new Dictionary<Guid, Guid?>() { { rosterGroupId, rosterTitleQuestionId } });

            interviewEventHandlerFunctional = CreateInterviewEventHandlerFunctional(questionnaireRosterStructure);
            viewState = interviewEventHandlerFunctional.Update(viewState,
                CreatePublishableEvent(new RosterInstancesAdded(new[] { new AddedRosterInstance(rosterGroupId, new decimal[0], 0.0m, null) })));
        };

        Because of = () =>
            viewState = interviewEventHandlerFunctional.Update(viewState,
                CreatePublishableEvent(new NumericRealQuestionAnswered(Guid.NewGuid(), rosterTitleQuestionId, new decimal[] { 0 }, DateTime.Now,
                    decimalAnswer)));

        It should_roster_title_be_equal_to_recived_answer = () =>
            viewState.Document.Levels["0"].RosterRowTitles[rosterGroupId].ShouldEqual(decimalAnswer.ToString());

        It should_answer_on_head_question_be_equal_to_recived_answer = () =>
            viewState.Document.Levels["0"].GetAllQuestions().First(q => q.Id == rosterTitleQuestionId).Answer.ShouldEqual(decimalAnswer);

        private static InterviewEventHandlerFunctional interviewEventHandlerFunctional;
        private static ViewWithSequence<InterviewData> viewState;
        private static Guid rosterGroupId;
        private static Guid rosterScopeId;
        private static Guid rosterTitleQuestionId;
        private static decimal decimalAnswer = 3.52m;
    }
}
