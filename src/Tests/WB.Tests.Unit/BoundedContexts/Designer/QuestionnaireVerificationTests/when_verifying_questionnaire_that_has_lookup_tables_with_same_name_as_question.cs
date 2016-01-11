using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Specifications;

using Main.Core.Documents;

using Moq;

using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.Implementation.Services.LookupTableService;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.ValueObjects;

using It = Machine.Specifications.It;

namespace WB.Tests.Unit.BoundedContexts.Designer.QuestionnaireVerificationTests
{
    class when_verifying_questionnaire_that_has_lookup_tables_with_same_name_as_question : QuestionnaireVerifierTestsContext
    {
        Establish context = () =>
        {
            lookupTableServiceMock
                .Setup(x => x.GetLookupTableContent(Moq.It.IsAny<Guid>(), Moq.It.IsAny<Guid>()))
                .Returns(lookupTableContent);

            questionnaire = Create.QuestionnaireDocument(Guid.NewGuid(), Create.TextQuestion(questionId: questionId, variable: "var"));
            questionnaire.LookupTables.Add(table1Id, Create.LookupTable("var"));

            verifier = CreateQuestionnaireVerifier(lookupTableService: lookupTableServiceMock.Object);
        };

        Because of = () =>
            resultErrors = verifier.Verify(questionnaire);

        It should_return_1_error = () =>
            resultErrors.Count().ShouldEqual(1);

        It should_return_error_with_code__WB0029 = () =>
            resultErrors.Single().Code.ShouldEqual("WB0029");

        It should_return_error_with_Critical_level = () =>
            resultErrors.Single().ErrorLevel.ShouldEqual(VerificationErrorLevel.Critical);

        It should_return_error_with_1_reference = () =>
            resultErrors.Single().References.Count().ShouldEqual(2);

        It should_return_first_error_reference_with_type_Question = () =>
            resultErrors.Single().References.ElementAt(0).Type.ShouldEqual(QuestionnaireVerificationReferenceType.Question);

        It should_return_second_error_reference_with_type_LookupTable = () =>
            resultErrors.Single().References.ElementAt(1).Type.ShouldEqual(QuestionnaireVerificationReferenceType.LookupTable);

        It should_return_first_error_reference_with_id_of_question = () =>
            resultErrors.Single().References.ElementAt(0).Id.ShouldEqual(questionId);

        It should_return_second_error_reference_with_id_of_table = () =>
            resultErrors.Single().References.ElementAt(1).Id.ShouldEqual(table1Id);

        private static QuestionnaireVerifier verifier;
        private static QuestionnaireDocument questionnaire;

        private static IEnumerable<QuestionnaireVerificationError> resultErrors;

        private static readonly Mock<ILookupTableService> lookupTableServiceMock = new Mock<ILookupTableService>();
        private static readonly LookupTableContent lookupTableContent = Create.LookupTableContent(new[] { "min", "max" },
           Create.LookupTableRow(1, new decimal?[] { 1.15m, 10 }),
           Create.LookupTableRow(2, new decimal?[] { 1, 10 }),
           Create.LookupTableRow(3, new decimal?[] { 1, 10 })
        );

        private static readonly Guid table1Id = Guid.Parse("11111111111111111111111111111111");
        private static readonly Guid questionId = Guid.Parse("10000000000000000000000000000000");
    }
}