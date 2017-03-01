using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection;
using static System.String;

namespace WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGenerationV2
{
    public class CodeGenerationModelsFactory : ICodeGenerationModelsFactory
    {
        public CodeGenerationModel CreateModel(ReadOnlyQuestionnaireDocument questionnaire)
        {
            var codeGenerationModel = new CodeGenerationModel
            {
                Id = questionnaire.PublicKey,
                ClassName = $"{CodeGeneratorV2.InterviewExpressionStatePrefix}_{Guid.NewGuid().FormatGuid()}",
            };

            Dictionary<RosterScope, Group[]> rosterScopes = questionnaire.Find<Group>()
                .GroupBy(questionnaire.GetRosterScope)
                .ToDictionary(x => x.Key, x => x.ToArray());

            Dictionary<RosterScope, string> levelClassNames = new Dictionary<RosterScope, string>();

            foreach (var rosterScopePairs in rosterScopes)
            {
                var rosterScope = rosterScopePairs.Key;
                var rosters = rosterScopePairs.Value;
                var firstRosterInScope = rosters.FirstOrDefault(x => x.IsRoster);

                var levelModel = new LevelModel
                {
                    RosterScope = rosterScope
                };

                string levelClassName = "";
                if (firstRosterInScope == null)
                {
                    levelClassName = CodeGeneratorV2.QuestionnaireLevel;
                    levelModel.Id = questionnaire.PublicKey;
                }
                else
                {
                    levelModel.Id = firstRosterInScope.PublicKey;
                    levelModel.Variable = firstRosterInScope.VariableName ?? "_" + firstRosterInScope.PublicKey.FormatGuid();
                    levelClassName = CodeGeneratorV2.LevelPrefix + levelModel.Variable;
                }

                levelModel.ClassName = levelClassName;
                levelClassNames.Add(rosterScope, levelClassName);

                codeGenerationModel.AllLevels.Add(levelModel);
            }

            foreach (var question in questionnaire.Find<IQuestion>())
            {
                string varName = !IsNullOrEmpty(question.StataExportCaption)
                    ? question.StataExportCaption
                    : "__" + question.PublicKey.FormatGuid();

                var rosterScope = questionnaire.GetRosterScope(question);
                var levelClassName = levelClassNames[rosterScope];
                var questionModel = new QuestionModel
                {
                    Id = question.PublicKey,
                    Variable = varName,
                    ClassName = levelClassName,
                    TypeName = GenerateQuestionTypeName(question, questionnaire),
                    RosterScope = rosterScope
                };
                codeGenerationModel.AllQuestions.Add(questionModel);
            }

            foreach (var level in codeGenerationModel.AllLevels)
            {
                foreach (var question in codeGenerationModel.AllQuestions)
                {
                    if (question.RosterScope.IsSameOrParentScopeFor(level.RosterScope))
                    {
                        level.Questions.Add(question);
                    }
                }
            }

            return codeGenerationModel;
        }

        private static string GenerateQuestionTypeName(IQuestion question, ReadOnlyQuestionnaireDocument questionnaire)
        {
            switch (question.QuestionType)
            {
                case QuestionType.Text:
                    return "string";

                case QuestionType.Numeric:
                    return ((question as NumericQuestion)?.IsInteger ?? false) ? "long?" : "double?";

                case QuestionType.QRBarcode:
                    return "string";

                case QuestionType.MultyOption:
                    var multiOtion = question as MultyOptionsQuestion;
                    if (multiOtion != null && multiOtion.YesNoView)
                        return typeof(YesNoAnswers).Name;

                    if (question.LinkedToQuestionId == null && question.LinkedToRosterId == null)
                        return "decimal[]";

                    if (question.LinkedToQuestionId.HasValue && questionnaire.Find<ITextListQuestion>(question.LinkedToQuestionId.Value) != null)
                    {
                        return "decimal[]";
                    }
                    return "decimal[][]";

                case QuestionType.DateTime:
                    return "DateTime?";

                case QuestionType.SingleOption:
                    if (question.LinkedToQuestionId == null && question.LinkedToRosterId == null) return "decimal?";

                    if (question.LinkedToQuestionId.HasValue && questionnaire.Find<ITextListQuestion>(question.LinkedToQuestionId.Value) != null)
                    {
                        return "decimal?";
                    }

                    return "decimal[]";
                case QuestionType.TextList:
                    return "ListAnswerRow[]";

                case QuestionType.GpsCoordinates:
                    return "GeoLocation";

                case QuestionType.Multimedia:
                    return "string";

                default:
                    throw new ArgumentException("Unknown question type.");
            }
        }

        public IEnumerable<LinkedFilterMethodModel> CreateLinkedFilterModels(ReadOnlyQuestionnaireDocument questionnaire, CodeGenerationModel model)
        {
            var linkedWithFilter = questionnaire.Find<IQuestion>().Where(x => !string.IsNullOrWhiteSpace(x.LinkedFilterExpression));

            foreach (var question in linkedWithFilter)
            {
                var questionModel = model.GetQuestionById(question.PublicKey);
                yield return
                    new LinkedFilterMethodModel(
                        ExpressionLocation.LinkedQuestionFilter(questionModel.Id),
                        questionModel.ClassName,
                        $"{CodeGeneratorV2.LinkedFilterPrefix}{questionModel.Variable}",
                        question.LinkedFilterExpression,
                        questionModel.Variable);
            }
        }

        public IEnumerable<OptionsFilterMethodModel> CreateCategoricalOptionsFilterModels(ReadOnlyQuestionnaireDocument questionnaire, CodeGenerationModel model)
        {
            var questionsWithFilter = questionnaire.Find<IQuestion>().Where(x => !string.IsNullOrWhiteSpace(x.Properties.OptionsFilterExpression));

            foreach (var question in questionsWithFilter)
            {
                var questionModel = model.GetQuestionById(question.PublicKey);
                yield return 
                    new OptionsFilterMethodModel(
                        ExpressionLocation.CategoricalQuestionFilter(questionModel.Id),
                        questionModel.ClassName, 
                        $"{CodeGeneratorV2.OptionsFilterPrefix}{questionModel.Variable}", 
                        question.Properties.OptionsFilterExpression,
                        questionModel.Variable);
            }
        }

        public IEnumerable<ConditionMethodModel> CreateMethodModels(ReadOnlyQuestionnaireDocument questionnaire, CodeGenerationModel model)
        {
            return Enumerable.Empty<ConditionMethodModel>();
        }
    }

    public class ConditionMethodModel
    {
        public ConditionMethodModel(ExpressionLocation location, 
            string className, 
            string methodName, 
            string expression, 
            bool generateSelf, 
            string variableName, 
            string returnType = "bool")
        {
            this.Location = location;
            this.ClassName = className;
            this.MethodName = methodName;
            this.Expression = expression;
            this.VariableName = variableName;
            this.GenerateSelf = generateSelf;
            this.ReturnType = returnType;
        }

        public ExpressionLocation Location { get; set; }
        public string ClassName { set; get; }
        public string MethodName { set; get; }
        public string Expression { set; get; }
        public string VariableName { set; get; }
        public bool GenerateSelf { set; get; }
        public string ReturnType { get; set; }
    }

    public class OptionsFilterMethodModel : ConditionMethodModel
    {
        public OptionsFilterMethodModel(ExpressionLocation location, string className, string methodName, string expression, string variableName)
            : base(location, className, methodName, expression, true, variableName, "bool")
        {
        }
    }

    public class LinkedFilterMethodModel : ConditionMethodModel
    {
        public string LinkedQuestionScopeName { get; }

        public bool IsSourceAndLinkedQuestionOnSameLevel => LinkedQuestionScopeName == this.ClassName;

        public LinkedFilterMethodModel(
            ExpressionLocation location, 
            string className, 
            string methodName, 
            string expression, 
            string linkedQuestionScopeName)
            : base(location, className, methodName, expression, false, string.Empty, "bool")
        {
            this.LinkedQuestionScopeName = linkedQuestionScopeName;
        }
    }

    public class QuestionModel
    {
        public Guid Id { set; get; }
        public string Variable { set; get; }
        public string ClassName { get; set; }

        public string TypeName { get; set; }
        public RosterScope RosterScope { get; set; }
    }

    public class LevelModel
    {
        public Guid Id { set; get; }
        public string Variable { set; get; }
        public string ClassName { get; set; }
        public RosterScope RosterScope { get; set; }

        public List<QuestionModel> Questions { get; set; } = new List<QuestionModel>();
    }
}