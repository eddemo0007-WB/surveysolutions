﻿using System;
using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Repositories;

namespace WB.Core.BoundedContexts.Headquarters.Implementation.Repositories
{
    public class QuestionnaireQuestionOptionsRepository : IQuestionOptionsRepository
    {
        public IEnumerable<CategoricalOption> GetOptionsForQuestion(IQuestionnaire questionnaire, Guid questionId, int? parentQuestionValue, string filter)
        {
            return questionnaire.GetOptionsForQuestionFromStructure(questionId, parentQuestionValue, filter);
        }

        public CategoricalOption GetOptionForQuestionByOptionText(IQuestionnaire questionnaire, Guid questionId, string optionText)
        {
            return questionnaire.GetOptionForQuestionFromStructureByOptionText(questionId, optionText);
        }
    }
}
