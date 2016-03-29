﻿using System;
using Machine.Specifications;
using Main.Core.Documents;
using WB.Core.BoundedContexts.Headquarters.DataExport.Denormalizers;
using WB.Core.SharedKernels.SurveyManagement.Views.DataExport;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.Factories.ExportViewFactoryTests
{
    [Ignore("KP-6925")]
    internal class when_creating_interview_export_view_by_interview_with_1_answerd_text_question : ExportViewFactoryTestsContext
    {
        Establish context = () =>
        {
            textQuestionId = Guid.Parse("10000000000000000000000000000000");

            interviewData =
                Create.InterviewData(Create.InterviewQuestion(questionId: textQuestionId,
                    answer: text));

            questionnaireDocument =
                Create.QuestionnaireDocument(children: Create.DateTimeQuestion(questionId: textQuestionId, variable: "txt"));

            exportViewFactory = CreateExportViewFactory();
        };

        Because of = () =>
            result = exportViewFactory.CreateInterviewDataExportView(exportViewFactory.CreateQuestionnaireExportStructure(questionnaireDocument, 1),
                interviewData);

        It should_create_record__with_one_text_question = () =>
            result.Levels[0].Records[0].GetQuestions()[0].Answers.Length.ShouldEqual(1);

        It should_create_record__with_one_text_question_which_answer_on_text_question = () =>
          result.Levels[0].Records[0].GetQuestions()[0].Answers.ShouldEqual(new[] { text });

        private static ExportViewFactory exportViewFactory;
        private static InterviewDataExportView result;
        private static Guid textQuestionId;
        private static QuestionnaireDocument questionnaireDocument;
        private static InterviewData interviewData;
        private static string text = "231 Pietermaritz St\n\rPietermaritzburg\n\r3201";
    }
}