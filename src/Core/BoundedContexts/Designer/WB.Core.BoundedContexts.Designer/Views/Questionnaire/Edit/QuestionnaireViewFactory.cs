﻿using System;
using Main.Core.Documents;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;

namespace WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit
{
    public class QuestionnaireViewFactory : IViewFactory<QuestionnaireViewInputModel, QuestionnaireView>, 
        IViewFactory<QuestionnaireViewInputModel, EditQuestionnaireView>
    {
        private readonly IReadSideRepositoryReader<QuestionnaireDocument> _questionnaireStorage;

        public QuestionnaireViewFactory(IReadSideRepositoryReader<QuestionnaireDocument> questionnaireStorage)
        {
            this._questionnaireStorage = questionnaireStorage;
        }

        QuestionnaireView IViewFactory<QuestionnaireViewInputModel, QuestionnaireView>.Load(QuestionnaireViewInputModel input)
        {
            var doc = GetQuestionnaireDocument(input);
            return doc == null ? null : new QuestionnaireView(doc);
        }

        public EditQuestionnaireView Load(QuestionnaireViewInputModel input)
        {
            var doc = GetQuestionnaireDocument(input);
            return doc == null ? null : new EditQuestionnaireView(doc);
        }

        private QuestionnaireDocument GetQuestionnaireDocument(QuestionnaireViewInputModel input)
        {
            try
            {
                return this._questionnaireStorage.GetById(input.QuestionnaireId);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

      
    }
}