﻿namespace Main.Core.Events.Questionnaire
{
    using System;
    using Ncqrs.Eventing.Storage;

    [EventName("RavenQuestionnaire.Core:Events:QuestionChangeded")]
    public class QuestionChanged : FullQuestionDataEvent
    {
        public Guid TargetGroupKey { get; set; }
    }
}