using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration.Model;

namespace WB.Tests.Unit.BoundedContexts.Designer.CodeGeneratorTests
{
    internal class when_creating_executor_template_model_for_questionnaire_with_multimedia_question : CodeGeneratorTestsContext
    {
        Establish context = () =>
        {
            questionnaire = CreateQuestionnaireDocument(
                new IComposite[]
                {
                    new Group
                    {
                        Title = "Chapter",
                        PublicKey = chapterId,
                        Children = new List<IComposite>
                        {
                            new TextQuestion
                            {
                                PublicKey = textId,
                                QuestionType = QuestionType.Text,
                                StataExportCaption = "text"
                            },
                            new MultimediaQuestion
                            {
                                PublicKey = multimediaId,
                                QuestionType = QuestionType.Multimedia,
                                StataExportCaption = "multimedia",
                                ConditionExpression = "text == \"a\""
                            }
                        }
                    },
                });

            executorTemplateModelFactory = Create.QuestionnaireExecutorTemplateModelFactory();
        };

        Because of = () =>
            templateModel = executorTemplateModelFactory.CreateQuestionnaireExecutorTemplateModel(questionnaire, new CodeGenerationSettings(
                    additionInterfaces: new string[0],
                    namespaces: new string[0],
                    areRosterServiceVariablesPresent: false,
                    abstractConditionalLevelClassName: "AbstractConditionalLevel",
                    isLookupTablesFeatureSupported: true), true);

        It should_generate_model_for_multimedia_question = () =>
            GetQuestion(multimediaId).ShouldNotBeNull();

        It should_generate_model_for_multimedia_question_with_specified_values = () =>
            GetQuestion(multimediaId).ShouldContainValues(
                id: multimediaId,
                variableName: "multimedia",
                conditions: "text == \"a\"",
                validations: null,
                questionType: QuestionType.Multimedia,
                generatedIdName: "@__multimedia_id",
                generatedTypeName: "string",
                generatedMemberName: "@__multimedia",
                generatedStateName: "@__multimedia_state",
                rosterScopeName: "@__questionnaire_scope",
                generatedValidationsMethodName: "IsValid_multimedia",
                generatedConditionsMethodName: "IsEnabled_multimedia");


        It should_generate_model_for_text_question = () =>
            GetQuestion(textId).ShouldNotBeNull();

        private static QuestionTemplateModel GetQuestion(Guid id)
        {
            return templateModel.AllQuestions.Single(x => x.Id == id);
        }

        private static QuestionnaireExecutorTemplateModelFactory executorTemplateModelFactory;
        private static QuestionnaireExecutorTemplateModel templateModel;
        private static QuestionnaireDocument questionnaire;
        private static readonly Guid chapterId = Guid.Parse("11111111111111111111111111111111");
        private static readonly Guid multimediaId = Guid.Parse("22222222222222222222222222222222");
        private static readonly Guid textId = Guid.Parse("33333333333333333333333333333333");
    }
}