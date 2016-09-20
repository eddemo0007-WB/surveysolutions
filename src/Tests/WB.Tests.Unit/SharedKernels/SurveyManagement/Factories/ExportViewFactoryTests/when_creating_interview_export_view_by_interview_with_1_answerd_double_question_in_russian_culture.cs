﻿using System;
using System.Globalization;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using WB.Core.BoundedContexts.Headquarters.DataExport.Denormalizers;
using System.Threading;
using Moq;
using WB.Core.BoundedContexts.Headquarters.Views.DataExport;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.Factories.ExportViewFactoryTests
{
    internal class when_creating_interview_export_view_by_interview_with_1_answerd_double_question_in_russian_culture : ExportViewFactoryTestsContext
    {
        Establish context = () =>
        {
            dateTimeQuestionId = Guid.Parse("10000000000000000000000000000000");

            interviewData =
                Create.Entity.InterviewData(Create.Entity.InterviewQuestion(questionId: dateTimeQuestionId,
                    answer: value));

            questionnaireDocument =
                Create.Entity.QuestionnaireDocument(children: Create.Entity.NumericRealQuestion(id: dateTimeQuestionId, variable: "real"));

            var questionnaireMockStorage = new Mock<IQuestionnaireStorage>();
            questionnaireMockStorage.Setup(x => x.GetQuestionnaire(Moq.It.IsAny<QuestionnaireIdentity>(), Moq.It.IsAny<string>())).Returns(new PlainQuestionnaire(questionnaireDocument, 1, null));
            questionnaireMockStorage.Setup(x => x.GetQuestionnaireDocument(Moq.It.IsAny<QuestionnaireIdentity>())).Returns(questionnaireDocument);
            exportViewFactory = CreateExportViewFactory(questionnaireMockStorage.Object);
        };

        Because of = () =>
        {
            var originalCulture = new
            {
                Culture = Thread.CurrentThread.CurrentCulture,
                UICulture = Thread.CurrentThread.CurrentUICulture
            };

            try
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                result = exportViewFactory.CreateInterviewDataExportView(
                            exportViewFactory.CreateQuestionnaireExportStructure(new QuestionnaireIdentity(questionnaireDocument.PublicKey, 1)),
                            interviewData);
            }
            finally 
            {
                Thread.CurrentThread.CurrentCulture = originalCulture.Culture;
                Thread.CurrentThread.CurrentUICulture = originalCulture.UICulture;
            }
            
        };


        It should_create_record__with_one_datetime_question_which_contains_composite_answer = () =>
          result.Levels[0].Records[0].GetPlainAnswers().First().ShouldEqual(new[] { value.ToString(CultureInfo.InvariantCulture)  });

        private static ExportViewFactory exportViewFactory;
        private static InterviewDataExportView result;
        private static Guid dateTimeQuestionId;
        private static QuestionnaireDocument questionnaireDocument;
        private static InterviewData interviewData;
        private static double value = 5.55;

        private static CultureInfo culture = CultureInfo.GetCultureInfo("ru-ru");
    }
}