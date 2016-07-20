﻿using System.Linq;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.InputModels;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.Views;
using WB.Core.SharedKernels.DataCollection.Repositories;

namespace WB.Core.BoundedContexts.Headquarters.Views.Reposts.Factories
{
    public interface IQuestionnaireQuestionInfoFactory
    {
        QuestionnaireQuestionInfoView Load(QuestionnaireQuestionInfoInputModel input);
    }

    public class QuestionnaireQuestionInfoFactory : IQuestionnaireQuestionInfoFactory
    {
        private readonly IQuestionnaireStorage questionnaireStorage;

        public QuestionnaireQuestionInfoFactory(IQuestionnaireStorage questionnaireStorage)
        {
            this.questionnaireStorage = questionnaireStorage;
        }

        public QuestionnaireQuestionInfoView Load(QuestionnaireQuestionInfoInputModel input)
        {
            var questionnaire = this.questionnaireStorage.GetQuestionnaireDocument(input.QuestionnaireId, input.QuestionnaireVersion);

            if (questionnaire == null)
                return new QuestionnaireQuestionInfoView();

            var questionnaireQuestionInfoItems = questionnaire
                .Find<IQuestion>(question => true)
                .Where(x => !input.QuestionType.HasValue || x.QuestionType == input.QuestionType.Value)
                .Select(x => new QuestionnaireQuestionInfoItem
                {
                    Variable = x.StataExportCaption,
                    Type = x.QuestionType,
                    Id = x.PublicKey
                })
                .ToList();

            return new QuestionnaireQuestionInfoView
            {
                Variables = questionnaireQuestionInfoItems
            };
        }
    }
}
