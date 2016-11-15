using System;
using System.Collections.Generic;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.SharedKernels.SurveySolutions.Documents;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.QuestionnaireVerificationTests
{
    internal class when_verifying_questionnaire_with_linked_question_reference_on_TextList_question_from_wrong_scope : QuestionnaireVerifierTestsContext
    {
        Establish context = () =>
        {
            questionnaire = Create.QuestionnaireDocumentWithOneChapter(children: new IComposite[]
            {
                Create.Roster(rosterId, fixedRosterTitles: new FixedRosterTitle[] { Create.FixedRosterTitle(1, "1")}, children: new IComposite[]
                {
                    Create.TextListQuestion(listQuestionId, variable: "var1"),
                }),
                Create.SingleQuestion(linkedQuestionId, variable: "var2", linkedToQuestionId: listQuestionId)
            });

            verifier = CreateQuestionnaireVerifier();
        };

        Because of = () =>
            verificationMessages = verifier.CheckForErrors(Create.QuestionnaireView(questionnaire));

        It should_return_WB0012_message = () =>
            verificationMessages.GetError("WB0116").ShouldNotBeNull();

        private static IEnumerable<QuestionnaireVerificationMessage> verificationMessages;
        private static QuestionnaireVerifier verifier;
        private static QuestionnaireDocument questionnaire;
        private static readonly Guid linkedQuestionId = Guid.Parse("10000000000000000000000000000000");
        private static readonly Guid listQuestionId = Guid.Parse("13333333333333333333333333333333");
        private static readonly Guid rosterId = Guid.Parse("99999999999999999999999999999999");
    }
}