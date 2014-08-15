using System;
using Main.Core.Entities.SubEntities;

namespace WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Base
{
    public abstract class FullQuestionDataCommand : AbstractQuestionCommand
    {
        protected FullQuestionDataCommand(Guid questionnaireId, Guid questionId,
            string title, QuestionType type, string variableName, string variableLabel, string mask, bool isMandatory, bool isPreFilled,
            QuestionScope scope, string enablementCondition, string validationExpression, string validationMessage, string instructions,
            Option[] options, Guid responsibleId, Guid? linkedToQuestionId, bool areAnswersOrdered, int? maxAllowedAnswers, bool isFilteredCombobox)
            : base(
                questionnaireId, questionId, title, variableName, mask, isMandatory, isPreFilled, scope, enablementCondition,
                validationExpression, validationMessage, instructions, responsibleId, variableLabel, isFilteredCombobox)
        {
            this.Options = options;
            this.LinkedToQuestionId = linkedToQuestionId;
            this.Type = type;

            this.AreAnswersOrdered = areAnswersOrdered;
            this.MaxAllowedAnswers = maxAllowedAnswers;

        }

        public QuestionType Type { get; private set; }
        public Option[] Options { get; private set; }
        public Guid? LinkedToQuestionId { get; private set; }

        public bool AreAnswersOrdered { get; private set; }
        public int? MaxAllowedAnswers { get; private set; }
    }
}