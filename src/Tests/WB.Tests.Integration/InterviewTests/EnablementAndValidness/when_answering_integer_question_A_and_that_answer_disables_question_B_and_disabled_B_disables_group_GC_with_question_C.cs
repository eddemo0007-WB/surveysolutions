﻿using System;
using System.Collections.Generic;
using System.Linq;
using AppDomainToolkit;
using Machine.Specifications;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;

namespace WB.Tests.Integration.InterviewTests.EnablementAndValidness
{
    internal class when_answering_integer_question_A_and_that_answer_disables_question_B_and_disabled_B_disables_group_GC_with_question_C_and_B_was_answered : InterviewTestsContext
    {
        Establish context = () =>
        {
            appDomainContext = AppDomainContext.Create();
        };

        Because of = () =>
            results = Execute.InStandaloneAppDomain(appDomainContext.Domain, () =>
            {
                Setup.MockedServiceLocator();

                var userId = Guid.Parse("11111111111111111111111111111111");

                var questionnaireId = Guid.Parse("77778888000000000000000000000000");
                var questionAId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
                var questionBId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
                var questionCId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
                var groupGCId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");

                var questionnaireDocument = Abc.Create.Entity.QuestionnaireDocumentWithOneChapter(questionnaireId,
                    Abc.Create.Entity.NumericIntegerQuestion(questionAId, "a"),
                    Abc.Create.Entity.NumericIntegerQuestion(questionBId, "b", enablementCondition: "a > 0"),
                    Abc.Create.Entity.Group(groupGCId, "Group X", null, "b > 0", false, new IComposite[] {
                        Abc.Create.Entity.NumericIntegerQuestion(questionCId)
                    })
                );

                var interview = SetupInterviewWithExpressionStorage(questionnaireDocument, new List<object>
                {
                    Abc.Create.Event.QuestionsEnabled(new []
                    {
                        Abc.Create.Identity(questionAId),
                        Abc.Create.Identity(questionBId),
                        Abc.Create.Identity(questionCId)
                    }),
                    Abc.Create.Event.GroupsEnabled(new [] { Abc.Create.Entity.Identity(groupGCId) }),
                    Abc.Create.Event.NumericIntegerQuestionAnswered(
                        questionBId, null, 0, null, null
                    )
                });

                using (var eventContext = new EventContext())
                {
                    interview.AnswerNumericIntegerQuestion(userId, questionAId, RosterVector.Empty, DateTime.Now, 0);

                    return new InvokeResults()
                    {
                        GroupGCDisabled = GetFirstEventByType<GroupsDisabled>(eventContext.Events).Groups.FirstOrDefault(g => g.Id == groupGCId) != null,
                        QuestionBDisabled = GetFirstEventByType<QuestionsDisabled>(eventContext.Events).Questions.FirstOrDefault(q => q.Id == questionBId) != null
                    };
                }
            });

        It should_disable_question_B = () =>
            results.GroupGCDisabled.ShouldBeTrue();

        It should_disable_question_C = () =>
            results.QuestionBDisabled.ShouldBeTrue();

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
            public bool GroupGCDisabled { get; set; }
            public bool QuestionBDisabled { get; set; }
        }
    }
}