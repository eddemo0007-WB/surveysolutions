﻿using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using NSubstitute;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities.Answers;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;
using WB.Core.SharedKernels.QuestionnaireEntities;

namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.ValidityViewModelTests
{
    [Ignore("KP-8159")]
    [Subject(typeof(ValidityViewModel))]
    public class when_question_became_invalid_and_has_more_then_one_validation
    { 
        Establish context = () =>
        {
            questionIdentity = Create.Entity.Identity(Guid.NewGuid(), RosterVector.Empty);
            QuestionnaireDocument questionnaire = Create.Entity.QuestionnaireDocumentWithOneChapter(
                Create.Entity.TextQuestion(questionId: questionIdentity.Id,
                validationConditions: new List<ValidationCondition>
                {
                    new ValidationCondition {Expression = "validation 1", Message = "message 1"},
                    new ValidationCondition {Expression = "validation 2", Message = "message 2"}
                }));
            
            var plainQuestionnaire = Create.Entity.PlainQuestionnaire(questionnaire);

            var interview = Setup.StatefulInterview(questionnaire);
            interview.CreateInterview(questionnaire.PublicKey, 1, Guid.NewGuid(), new Dictionary<Guid, AbstractAnswer>(), DateTime.Now, Guid.NewGuid());
            interview.Apply(Create.Event.TextQuestionAnswered(questionIdentity.Id, questionIdentity.RosterVector, "aaa"));
            interview.Apply(answersDeclaredInvalid);

            var statefulInterviewRepository = Substitute.For<IStatefulInterviewRepository>();
            statefulInterviewRepository.Get(null).ReturnsForAnyArgs(interview);

            viewModel = Create.ViewModel.ValidityViewModel(questionnaire: plainQuestionnaire,
                interviewRepository: statefulInterviewRepository,
                entityIdentity: questionIdentity);
            viewModel.Init("interviewid", questionIdentity);
        };

        Because of = () => 
            viewModel.Handle(answersDeclaredInvalid);

        It should_show_all_failed_validation_messages = () => viewModel.Error.ValidationErrors.Count.ShouldEqual(2);

        It should_show_error_messages_according_to_failed_validation_indexes_with_postfix_added = () =>
        {
            viewModel.Error.ValidationErrors.First()?.PlainText.ShouldEqual("message 2 [2]");
            viewModel.Error.ValidationErrors.Second()?.PlainText.ShouldEqual("message 1 [1]");
        };

        private static AnswersDeclaredInvalid answersDeclaredInvalid =
            Create.Event.AnswersDeclaredInvalid(new Dictionary<Identity, IReadOnlyList<FailedValidationCondition>>
            {
                {
                    questionIdentity,
                    new List<FailedValidationCondition>
                    {
                        new FailedValidationCondition(1),
                        new FailedValidationCondition(0)
                    }
                }
            });
        static ValidityViewModel viewModel;
        static Identity questionIdentity;
    }
}