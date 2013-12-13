﻿using System;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Base;

namespace WB.Core.SharedKernels.DataCollection.Events.Interview
{
    public class SingleOptionLinkedQuestionAnswered : QuestionAnswered
    {
        public decimal[] SelectedPropagationVector { get; private set; }

        public SingleOptionLinkedQuestionAnswered(Guid userId, Guid questionId, decimal[] propagationVector, DateTime answerTime, decimal[] selectedPropagationVector)
            : base(userId, questionId, propagationVector, answerTime)
        {
            this.SelectedPropagationVector = selectedPropagationVector;
        }
    }
}