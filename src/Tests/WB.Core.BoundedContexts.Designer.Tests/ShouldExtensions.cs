﻿using System;
using System.Linq.Expressions;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using Ncqrs.Spec;

namespace WB.Core.BoundedContexts.Designer.Tests
{
    internal static class ShouldExtensions
    {
        public static void ShouldContainEvent<TEvent>(this EventContext eventContext, Func<TEvent, bool> condition = null)
        {
            if (condition == null)
            {
                eventContext.Events.ShouldContain(@event
                    => @event.Payload is TEvent);
            }
            else
            {
                eventContext.Events.ShouldContain(@event
                    => @event.Payload is TEvent
                        && condition.Invoke((TEvent)@event.Payload));
            }
        }

        public static void ShouldNotContainEvent<TEvent>(this EventContext eventContext, Func<TEvent, bool> condition = null)
        {
            if (condition == null)
            {
                eventContext.Events.ShouldNotContain(@event
                    => @event.Payload is TEvent);
            }
            else
            {
                eventContext.Events.ShouldNotContain(@event
                    => @event.Payload is TEvent
                        && condition.Invoke((TEvent)@event.Payload));
            }
        }

        public static void ShouldContainGroup(this QuestionnaireDocument questionnaireDocument, Expression<Func<IGroup, bool>> condition)
        {
            questionnaireDocument.GetAllGroups().ShouldContain(condition);
        }
    }
}