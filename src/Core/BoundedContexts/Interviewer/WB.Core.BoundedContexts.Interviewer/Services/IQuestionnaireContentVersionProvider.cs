﻿using System;
namespace WB.Core.BoundedContexts.Interviewer.Services
{
    public interface IQuestionnaireContentVersionProvider
    {
        long GetSupportedQuestionnaireContentVersion();
    }
}
