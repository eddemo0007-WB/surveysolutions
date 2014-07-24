﻿using System;
using Machine.Specifications;
using Main.Core.Documents;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Practices.ServiceLocation;
using Moq;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration;
using WB.Core.BoundedContexts.Designer.Services;
using It = Machine.Specifications.It;

namespace WB.Core.BoundedContexts.Designer.Tests.CodeGenerationTests
{
    internal class when_generating_assembly_with_evaluatorgenerator
    {

        Establish context = () =>
        {
            var serviceLocatorMock = new Mock<IServiceLocator> { DefaultValue = DefaultValue.Mock };
            ServiceLocator.SetLocatorProvider(() => serviceLocatorMock.Object);

            expressionProcessorGenerator = new QuestionnireExpressionProcessorGenerator();

            questionnaireDocument = new QuestionnaireDocument() { PublicKey = id};
        };

        private Because of = () =>
            emitResult = expressionProcessorGenerator.GenerateProcessor(questionnaireDocument, out resultAssembly);


        private It should_result_succeded = () =>
            emitResult.Success.ShouldEqual(true);

        private It should_result_errors_count = () =>
            emitResult.Diagnostics.Length.ShouldEqual(0);

        private It should_ = () =>
            resultAssembly.Length.ShouldBeGreaterThan(0);
        
        private static Guid id = Guid.Parse("11111111111111111111111111111111");
        private static string resultAssembly;
        private static EmitResult emitResult;

        private static QuestionnaireDocument questionnaireDocument;

        private static IExpressionProcessorGenerator expressionProcessorGenerator;
    }
}
