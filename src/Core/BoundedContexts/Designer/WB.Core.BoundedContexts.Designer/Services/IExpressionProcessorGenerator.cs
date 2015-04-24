﻿using System;
using System.Collections.Generic;
using Main.Core.Documents;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.SharedKernels.SurveySolutions;

namespace WB.Core.BoundedContexts.Designer.Services
{
    public interface IExpressionProcessorGenerator
    {
        GenerationResult GenerateProcessorStateAssembly(QuestionnaireDocument questionnaire, Version targetVersion,
          out string generatedAssembly);
        Dictionary<string, string> GenerateProcessorStateClasses(QuestionnaireDocument questionnaire, Version targetVersion);
        string GenerateProcessorStateSingleClass(QuestionnaireDocument questionnaire, Version targetVersion);
    }
}
