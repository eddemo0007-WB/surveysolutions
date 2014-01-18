using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.SharedKernels.QuestionnaireVerification.Implementation.Services;
using WB.Core.SharedKernels.QuestionnaireVerification.ValueObjects;
using It = Machine.Specifications.It;

namespace WB.Core.SharedKernels.QuestionnaireVerification.Tests.QuestionnaireVerifierTests
{
    internal class when_verifying_questionnaire_with_MultiAnswer_question_that_marked_as_prefilled : QuestionnaireVerifierTestsContext
    {
        Establish context = () =>
        {
            prefilledMultiAnswerquestionId = Guid.Parse("10000000000000000000000000000000");
            questionnaire = CreateQuestionnaireDocument();

            questionnaire.Children.Add(new MultiAnswerQuestion()
            {
                PublicKey = prefilledMultiAnswerquestionId,
                Featured = true
            });

            questionnaire.Children.Add(new MultiAnswerQuestion()
            {
                PublicKey = Guid.Parse("20000000000000000000000000000000")
            });

            verifier = CreateQuestionnaireVerifier();
        };

        Because of = () =>
            resultErrors = verifier.Verify(questionnaire);

        It should_return_1_error = () =>
            resultErrors.Count().ShouldEqual(1);

        It should_return_error_with_code__WB0039 = () =>
            resultErrors.Single().Code.ShouldEqual("WB0039");

        It should_return_error_with_1_references = () =>
            resultErrors.Single().References.Count().ShouldEqual(1);

        It should_return_error_reference_with_type_Question = () =>
            resultErrors.Single().References.First().Type.ShouldEqual(QuestionnaireVerificationReferenceType.Question);

        It should_return_error_reference_with_id_of_prefilledMultiAnswerquestionId = () =>
            resultErrors.Single().References.First().Id.ShouldEqual(prefilledMultiAnswerquestionId);

        private static IEnumerable<QuestionnaireVerificationError> resultErrors;
        private static QuestionnaireVerifier verifier;
        private static QuestionnaireDocument questionnaire;

        private static Guid prefilledMultiAnswerquestionId;
    }
}
