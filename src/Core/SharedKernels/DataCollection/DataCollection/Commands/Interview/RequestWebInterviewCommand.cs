﻿using System;
using WB.Core.SharedKernels.DataCollection.Commands.Interview.Base;

namespace WB.Core.SharedKernels.DataCollection.Commands.Interview
{
    public class RequestWebInterviewCommand : InterviewCommand
    {
        public RequestWebInterviewCommand(Guid interviewId, Guid userId) : base(interviewId, userId)
        {
            
        }
    }
}
