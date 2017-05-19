﻿using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.QuestionnaireEntities;
using WB.Tests.Abc;

namespace WB.Tests.Integration.InterviewTests.Rosters
{
    internal class when_adding_roster_instance_that_affects_variable : in_standalone_app_domain
    {
        Because of = () => result = Execute.InStandaloneAppDomain(appDomainContext.Domain, () =>
        {
            Setup.MockedServiceLocator();

            var answeredQuestionId = Guid.Parse("11111111111111111111111111111111");
            var listRosterId = Guid.Parse("22222222222222222222222222222222");
            Guid variableId = Guid.Parse("33333333333333333333333333333333");
            Guid singleOptionQuestionId = Guid.Parse("44444444444444444444444444444444");
            
            Guid userId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            var interview = SetupInterview(
                questionnaireDocument: Create.Entity.QuestionnaireDocumentWithOneChapter(
                    children: new IComposite[]
                    {
                        Create.Entity.TextListQuestion(answeredQuestionId, variable: "q1"),
                        Create.Entity.Roster(
                                rosterId: listRosterId,
                                variable: "lst",
                                rosterSizeQuestionId: answeredQuestionId,
                                rosterSizeSourceType: RosterSizeSourceType.Question,
                                children: new IComposite[]
                                {
                                    Create.Entity.SingleOptionQuestion(singleOptionQuestionId, variable: "sgl", linkedToQuestionId: answeredQuestionId),
                                    Create.Entity.Variable(variableId, VariableType.LongInteger, expression: "(long)sgl")
                                }
                            )
                    }));

            interview.AnswerTextListQuestion(userId, answeredQuestionId, RosterVector.Empty, DateTime.Now, new[] {Tuple.Create(1m, "A")});
            interview.AnswerSingleOptionQuestion(userId, singleOptionQuestionId, Create.RosterVector(1), DateTime.Now, 1);

            using (var eventContext = new EventContext())
            {
                interview.AnswerTextListQuestion(userId, answeredQuestionId, RosterVector.Empty, DateTime.Now, new[] { Tuple.Create(1m, "A"), Tuple.Create(2m, "B") });

                return new InvokeResults
                {
                    VariableChangedEventRaised = eventContext.GetSingleEventOrNull<VariablesChanged>()?.ChangedVariables?.Any(x => x.Identity.Id == variableId) ?? false,
                };
            }
        });

        It should_raise_variables_changd_event_if_related_question_has_answer_removed = () => result.VariableChangedEventRaised.ShouldBeTrue();

        private static InvokeResults result;

        [Serializable]
        internal class InvokeResults
        {
            public bool VariableChangedEventRaised { get; set; }
        }
    }

    [Ignore("KP-9181")]
    internal class when_adding_roster_instance_that_affects_variable_used_in_condition : in_standalone_app_domain
    {
        Because of = () => result = Execute.InStandaloneAppDomain(appDomainContext.Domain, () =>
        {
            Setup.MockedServiceLocator();

            var answeredQuestionId = Guid.Parse("11111111111111111111111111111111");
            var listRosterId = Guid.Parse("22222222222222222222222222222222");
            Guid variableId = Guid.Parse("33333333333333333333333333333333");
            Guid singleOptionQuestionId = Guid.Parse("44444444444444444444444444444444");
            Guid numQuestionId = Guid.Parse("55555555555555555555555555555555");

            Guid userId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            var interview = SetupInterview(
                questionnaireDocument: Create.Entity.QuestionnaireDocumentWithOneChapter(
                    children: new IComposite[]
                    {
                        Create.Entity.TextListQuestion(answeredQuestionId, variable: "q1"),
                        Create.Entity.Roster(
                                rosterId: listRosterId,
                                variable: "lst",
                                rosterSizeQuestionId: answeredQuestionId,
                                rosterSizeSourceType: RosterSizeSourceType.Question,
                                children: new IComposite[]
                                {
                                    Create.Entity.SingleOptionQuestion(singleOptionQuestionId, variable: "sgl", linkedToQuestionId: answeredQuestionId),
                                    Create.Entity.Variable(variableId, VariableType.LongInteger, expression: "(long)sgl", variableName: "v1"),
                                    Create.Entity.NumericIntegerQuestion(numQuestionId, variable: "num", enablementCondition: "v1 == null")
                                }
                            )
                    }));

            interview.AnswerTextListQuestion(userId, answeredQuestionId, RosterVector.Empty, DateTime.Now, new[] { Tuple.Create(1m, "A") });
            interview.AnswerSingleOptionQuestion(userId, singleOptionQuestionId, Create.RosterVector(1), DateTime.Now, 1);

            using (var eventContext = new EventContext())
            {
                interview.AnswerTextListQuestion(userId, answeredQuestionId, RosterVector.Empty, DateTime.Now, new[] { Tuple.Create(1m, "A"), Tuple.Create(2m, "B") });

                return new InvokeResults
                {
                    QuestionEnabledEventRaised = eventContext.GetSingleEventOrNull<QuestionsEnabled>()?.Questions?.Any(x => x.Id == numQuestionId) ?? false
                };
            }
        });

        It should_raise_question_enabled_event_if_related_question_has_answer_removed_and_variable_changed = () => result.QuestionEnabledEventRaised.ShouldBeTrue();

        private static InvokeResults result;

        [Serializable]
        internal class InvokeResults
        {
            public bool QuestionEnabledEventRaised { get; set; }
        }
    }
}