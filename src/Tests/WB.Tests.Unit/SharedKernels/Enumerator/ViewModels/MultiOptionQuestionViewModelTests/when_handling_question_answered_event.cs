﻿using System;
using System.Linq;
using Machine.Specifications;
using Moq;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Aggregates;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.MultiOptionQuestionViewModelTests
{
    internal class when_handling_question_answered_event : MultiOptionQuestionViewModelTestsContext
    {
        Establish context = () =>
        {
            questionGuid = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            questionId = Create.Identity(questionGuid, Empty.RosterVector);

            var questionnaire = Mock.Of<IQuestionnaire>(_
                => _.ShouldQuestionRecordAnswersOrder(questionId.Id) == true
                && _.GetMaxSelectedAnswerOptions(questionId.Id) == 1
                && _.ShouldQuestionSpecifyRosterSize(questionId.Id) == false
                && _.GetAnswerOptionsAsValues(questionId.Id) == new decimal[] { 1, 2 }
                && _.GetAnswerOptionTitle(questionId.Id, 1) == "item1"
                && _.GetAnswerOptionTitle(questionId.Id, 2) == "item2"
            );

            var multiOptionAnswer = Create.MultiOptionAnswer(questionGuid, Empty.RosterVector);
            multiOptionAnswer.SetAnswers(new[] { 2m });

            var interview = Mock.Of<IStatefulInterview>(x => x.GetMultiOptionAnswer(questionId) == multiOptionAnswer);

            var questionnaireStorage = new Mock<IPlainQuestionnaireRepository>();
            var interviewRepository = new Mock<IStatefulInterviewRepository>();

            questionnaireStorage.SetReturnsDefault(questionnaire);
            interviewRepository.SetReturnsDefault(interview);

            viewModel = CreateViewModel(questionnaireStorage: questionnaireStorage.Object,
                interviewRepository: interviewRepository.Object);

            viewModel.Init("blah", questionId, Create.NavigationState());
          
        };

        Because of = () =>
        {
            viewModel.Handle(new MultipleOptionsQuestionAnswered(Guid.NewGuid(), questionGuid, Empty.RosterVector, DateTime.Now, new []{2m, 1m}));
        };

        It should_set_checked_order_to_options = () => viewModel.Options.Second().CheckedOrder.ShouldEqual(1);

        It should_mark_options_as_checked = () => viewModel.Options.Count(x => x.Checked).ShouldEqual(2);

        It should_set_checked_order_to_options1 = () => viewModel.Options.First().CheckedOrder.ShouldEqual(2);

        static MultiOptionQuestionViewModel viewModel;
        static Identity questionId;
        private static Guid questionGuid;
    }
}

