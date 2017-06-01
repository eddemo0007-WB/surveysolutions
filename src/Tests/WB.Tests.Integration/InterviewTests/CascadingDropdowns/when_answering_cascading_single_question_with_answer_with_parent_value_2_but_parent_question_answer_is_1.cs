using System;
using System.Collections.Generic;
using AppDomainToolkit;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Tests.Abc;

namespace WB.Tests.Integration.InterviewTests.CascadingDropdowns
{
    internal class when_answering_cascading_single_question_with_answer_with_parent_value_2_but_parent_question_answer_is_1 :
        InterviewTestsContext
    {
        Establish context = () =>
        {
            appDomainContext = AppDomainContext.Create();
        };

        Because of = () =>
            results = Execute.InStandaloneAppDomain(appDomainContext.Domain, () =>
            {
                var parentSingleOptionQuestionId = Guid.Parse("00000000000000000000000000000000");
                var childCascadedComboboxId = Guid.Parse("11111111111111111111111111111111");
                var questionnaireId = Guid.Parse("22222222222222222222222222222222");
                var actorId = Guid.Parse("33333333333333333333333333333333");

                Setup.MockedServiceLocator();

                var questionnaire = Create.Entity.QuestionnaireDocumentWithOneChapter(questionnaireId,
                    Create.Entity.SingleQuestion(parentSingleOptionQuestionId, "q1", options: new List<Answer>
                    {
                         Create.Entity.Option("1", "parent option 1"),
                         Create.Entity.Option("2", "parent option 2")
                    }),
                    Create.Entity.SingleQuestion(childCascadedComboboxId, "q2", cascadeFromQuestionId: parentSingleOptionQuestionId,
                        options: new List<Answer>
                        {
                             Create.Entity.Option("1.1", "child 1 for parent option 1", "1"),
                             Create.Entity.Option("1.2", "child 2 for parent option 1", "1"),
                             Create.Entity.Option("2.1", "child 1 for parent option 2", "2"),
                             Create.Entity.Option("2.2", "child 2 for parent option 2", "2"),
                             Create.Entity.Option("2.3", "child 3 for parent option 2", "2")
                        })
                    );

                var interview = SetupInterviewWithExpressionStorage(questionnaire, new List<object>
                {
                    Create.Event.SingleOptionQuestionAnswered(
                        parentSingleOptionQuestionId, new decimal[] { }, 1, null, null
                    ),
                    Create.Event.QuestionsEnabled(Create.Identity(childCascadedComboboxId))
                });

                using (var eventContext = new EventContext())
                {
                    var exception = Catch.Exception(() =>
                        interview.AnswerSingleOptionQuestion(actorId, childCascadedComboboxId, new decimal[] { }, DateTime.Now, 2.2m)
                        );

                    return new InvokeResults
                    {
                        ExceptionType = exception.GetType(),
                        ErrorMessage = exception.Message.ToLower()
                    };
                }
            });

        It should_throw_AnswerNotAcceptedException = () =>
            results.ExceptionType.ShouldEqual(typeof(AnswerNotAcceptedException));

        It should_throw_exception_with_message_containting__answer____parent_value____incorrect__ = () =>
            new[] { "answer", "parent value", "do not correspond" }.ShouldEachConformTo(
                keyword => results.ErrorMessage.Contains(keyword));

        Cleanup stuff = () =>
        {
            appDomainContext.Dispose();
            appDomainContext = null;
        };

        private static InvokeResults results;
        private static AppDomainContext<AssemblyTargetLoader, PathBasedAssemblyResolver> appDomainContext;

        [Serializable]
        internal class InvokeResults
        {
            public Type ExceptionType { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
}