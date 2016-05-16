﻿using System;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;

namespace WB.Tests.Integration.InterviewTests.Variables
{
    internal class when_Answering_question_which_changes_value_of_a_variable : InterviewTestsContext
    {
        Establish context = () =>
        {
            var questionnaireId = Guid.Parse("10000000000000000000000000000000");
            userId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
            textQuetionId = Guid.Parse("21111111111111111111111111111111");
            variableId = Guid.Parse("22222222222222222222222222222222");

            QuestionnaireDocument questionnaire = Create.QuestionnaireDocument(id: questionnaireId,
                children: new IComposite[]
                {
                    Create.TextQuestion(id: textQuetionId, variable: "txt"),
                    Create.Variable(id: variableId, variableName: "v1", expression: "txt.Length")
                });

            interview = SetupInterview(questionnaireDocument: questionnaire);
            eventContext = new EventContext();
        };

        Cleanup stuff = () =>
        {
            eventContext.Dispose();
            eventContext = null;
        };

        Because of = () =>
            interview.AnswerTextQuestion(userId, textQuetionId, new decimal[0], DateTime.Now, "Nastya");

        It should_raise_VariablesValuesChanged_event_for_the_variable = () =>
            eventContext.ShouldContainEvent<VariablesChanged>(@event
                => (int?) @event.ChangedVariables[0].NewValue == 6 && @event.ChangedVariables[0].Identity.Id== variableId);

        private static EventContext eventContext;
        private static Interview interview;
        private static Guid userId;
        private static Guid variableId;
        private static Guid textQuetionId;
    }
}