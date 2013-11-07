﻿using System;
using Main.Core.Domain.Exceptions;

namespace WB.Core.BoundedContexts.Designer.Exceptions
{
    public class QuestionnaireException : Exception
    {
        public readonly DomainExceptionType ErrorType;

        public QuestionnaireException(DomainExceptionType errorType, string message)
            : base(message)
        {
            ErrorType = errorType;
        }
    }
}