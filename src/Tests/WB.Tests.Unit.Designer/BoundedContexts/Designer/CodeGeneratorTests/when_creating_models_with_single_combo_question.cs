using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration.Model;
using WB.Tests.Abc;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.CodeGeneratorTests
{
    internal class when_creating_models_with_single_combo_question : CodeGeneratorTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            

            questionnaire = Create.QuestionnaireDocument(questionnaireId, children: new[]
            {
                Create.Chapter(children: new IComposite[]
                {
                    Create.SingleQuestion(Id.gA, variable: "singleCombo", isFilteredCombobox: true, validationExpression: "singleCombo validation", enablementCondition: "singleCombo condition", options: new List<Answer>
                    {
                        Create.Option("1", "Option 1"),
                        Create.Option("2", "Option 2")
                    })
                })
            });

            templateModelFactory = Create.QuestionnaireExecutorTemplateModelFactory();
            BecauseOf();
        }

        private void BecauseOf() =>
            model = templateModelFactory.CreateQuestionnaireExecutorTemplateModel(questionnaire, Create.CodeGenerationSettings());

        [NUnit.Framework.Test] public void should_create_model_with_1_question () =>
            model.AllQuestions.Count.Should().Be(1);

        [NUnit.Framework.Test] public void should_create_questionnaire_level_with_1_question () =>
            model.QuestionnaireLevelModel.Questions.Count.Should().Be(1);

        [NUnit.Framework.Test] public void should_reference_same_question_model_in_AllQuestions_and_questionnaire_level () =>
            model.QuestionnaireLevelModel.Questions.First().Should().Be(model.AllQuestions.First());

        [NUnit.Framework.Test] public void should_create_singleCombo_question_model () 
        {
            QuestionTemplateModel question = model.AllQuestions.Single(x => x.Id == Id.gA);
            question.VariableName.Should().Be("singleCombo");
            question.ValidationExpressions.FirstOrDefault().ValidationExpression.Should().Be("singleCombo validation");
            question.Condition.Should().Be("singleCombo condition");
            question.IsMultiOptionYesNoQuestion.Should().Be(false);
            question.AllMultioptionYesNoCodes.Should().BeEmpty();
            question.TypeName.Should().Be("decimal?");
            question.RosterScopeName.Should().Be(CodeGenerator.QuestionnaireScope);
            question.ParentScopeTypeName.Should().Be(CodeGenerator.QuestionnaireTypeName);
        }

        private static QuestionnaireExpressionStateModelFactory templateModelFactory;
        private static QuestionnaireExpressionStateModel model;
        private static QuestionnaireDocument questionnaire;
        private static readonly Guid questionnaireId = Guid.Parse("ffffffffffffffffffffffffffffffff");
    }
}
